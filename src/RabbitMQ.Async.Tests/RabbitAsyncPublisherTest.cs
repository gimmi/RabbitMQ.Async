using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
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
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri }, Exchange);

			var tasks = Enumerable.Range(0, 10000)
				.AsParallel()
				.Select(i => sut.PublishAsync(_message))
				.ToArray();

			Task.WaitAll(tasks);

			sut.Dispose();

			var messages = TestUtils.GetAllMessages(Exchange).ToArray();
			Assert.That(messages, Has.Length.EqualTo(10000));
			Assert.That(sut.Statistics.ToString(), Is.EqualTo("enqueued: 10000, sent: 10000, failed: 0, canceled: 0, acked: 10000, nacked: 0, unacked: 0"));
			Assert.That(tasks.All(x => x.IsCompleted), Is.True);
		}

		[Test]
		public void Should_cancel_pending_and_unack_sent_when_disposing()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri }, Exchange);

			for (int i = 0; i < 10000; i++)
			{
				sut.PublishAsync(_message);
			}

			sut.Dispose();

			Console.Write(sut.Statistics);

			Assert.That(sut.Statistics.Canceled, Is.GreaterThanOrEqualTo(9990));
			Assert.That(sut.Statistics.Sent, Is.LessThanOrEqualTo(10));
			Assert.That(sut.Statistics.Sent + sut.Statistics.Canceled, Is.EqualTo(10000));

		}

		[Test]
		public void Should_fail_task_when_fail_send()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = "amqp://fake:5672/" }, Exchange);
			try
			{
				sut.PublishAsync(_message).Wait();
				Assert.Fail();
			}
			catch (AggregateException ae)
			{
				Assert.That(ae.Flatten().InnerExceptions, Has.Count.EqualTo(1));
				Assert.That(ae.Flatten().InnerException, Is.InstanceOf<BrokerUnreachableException>());
			}
			sut.Dispose();

			Assert.That(sut.Statistics.ToString(), Is.EqualTo("enqueued: 1, sent: 0, failed: 1, canceled: 0, acked: 0, nacked: 0, unacked: 0"));
		}

		[Test]
		public void Should_discard_messages_that_failed_send()
		{
			var sut = new RabbitAsyncPublisher(new ConnectionFactory { Uri = Uri }, Exchange);

			var m1 = sut.PublishAsync(Encoding.UTF8.GetBytes("M1"));
			Assert.That(m1.IsCompleted, Is.False);

			Process.Start("SC", "STOP RabbitMQ");
			Thread.Sleep(TimeSpan.FromSeconds(3));

			var m2 = sut.PublishAsync(Encoding.UTF8.GetBytes("M2"));
			Assert.That(m2.IsCompleted, Is.False);

			Process.Start("SC", "START RabbitMQ");
			Thread.Sleep(TimeSpan.FromSeconds(3));

			var m3 = sut.PublishAsync(Encoding.UTF8.GetBytes("M3"));
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
			Assert.That(sut.Statistics.ToString(), Is.EqualTo("enqueued: 3, sent: 2, failed: 1, canceled: 0, acked: 2, nacked: 0, unacked: 0"));
		}
	}
}