using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Async.Tests
{
	[TestFixture]
	public class RabbitAsyncPublisherTest
	{
		private static byte[] _message;
		private const string Exchange = "test-rabbit-utils";
		private const string Uri = "amqp://localhost:5672/";

		[SetUp]
		public void SetUp()
		{
			TestUtils.DeleteBinding(Exchange, Exchange);
			TestUtils.CreateBinding(Exchange, Exchange);

			_message = new byte[4096];
			new Random().NextBytes(_message);
		}

		[Test]
		public void Should_publish_all_messages_from_all_threads()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri });

			var tasks = Enumerable.Range(0, 10000)
				.AsParallel()
				.Select(i => sut.PublishAsync(Exchange, _message))
				.ToArray();

			Task.WaitAll(tasks);

			sut.Dispose();

			var messages = TestUtils.GetAllMessages(Exchange).ToArray();
			Assert.That(messages, Has.Length.EqualTo(10000));
			Assert.That(tasks.All(x => x.IsCompleted), Is.True);
		}

		[Test]
		public void Should_cancel_pending_and_unack_sent_when_disposing()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri });

			var tasks = new Task[10000];
			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = sut.PublishAsync(Exchange, _message);
			}

			Assert.That(tasks.Count(x => x.IsCompleted), Is.LessThan(10));

			sut.Dispose();

			Assert.That(tasks.Count(x => x.IsCanceled), Is.LessThan(10000));
			Assert.That(tasks.Count(x => x.IsFaulted), Is.EqualTo(0));
			Assert.That(tasks.Count(x => x.IsCompleted), Is.EqualTo(10000));
		}

		[Test]
		public void Should_fail_task_when_fail_send()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = "amqp://fake:5672/" });
			try
			{
				sut.PublishAsync(Exchange, _message).Wait();
				Assert.Fail();
			}
			catch (AggregateException ae)
			{
				Assert.That(ae.Flatten().InnerExceptions, Has.Count.EqualTo(1));
				Assert.That(ae.Flatten().InnerException, Is.InstanceOf<BrokerUnreachableException>());
			}
			sut.Dispose();
		}

		[Test, Ignore("need elevated user")]
		public void Should_discard_messages_that_failed_send()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri });

			var m1 = sut.PublishAsync(Exchange, Encoding.UTF8.GetBytes("M1"));
			Assert.That(m1.IsCompleted, Is.False);

			Process.Start("SC", "STOP RabbitMQ");
			Thread.Sleep(TimeSpan.FromSeconds(3));

			var m2 = sut.PublishAsync(Exchange, Encoding.UTF8.GetBytes("M2"));
			Assert.That(m2.IsCompleted, Is.False);

			Process.Start("SC", "START RabbitMQ");
			Thread.Sleep(TimeSpan.FromSeconds(3));

			var m3 = sut.PublishAsync(Exchange, Encoding.UTF8.GetBytes("M3"));
			Assert.That(m3.IsCompleted, Is.False);

			m1.Wait();

			Assert.That(m2.Wait, Throws.Exception.InstanceOf<AggregateException>());

			m3.Wait();

			sut.Dispose();

			var messages = TestUtils.GetAllMessages(Exchange).Select(m => Encoding.UTF8.GetString(m)).ToArray();
			Assert.That(messages, Is.EqualTo(new[] {
                "M1",
                "M3"
            }));
		}

		[Test, Explicit]
		public void Should_send_from_multiple_thread_with_no_confirm()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory(), false);

			var tasks = Enumerable.Range(0, 10000)
				.Select(i => sut.PublishAsync(Exchange, _message))
				.ToArray();

			Assert.That(tasks.Count(x => x.IsCompleted), Is.LessThan(10));

			Task.WaitAll(tasks);

			Assert.That(tasks.Count(x => x.IsCompleted), Is.EqualTo(10000));
		}
	}
}