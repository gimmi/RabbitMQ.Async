using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitMQ.Async
{
	public class RabbitAsyncPublisher : IDisposable
	{
		private readonly BlockingCollection<EnqueuedMessage> _queue;
		private readonly IConfirmStrategy _confirmStrategy;
		private readonly Thread _thread;
		private readonly ConnectionHolder _connectionHolder;

		public RabbitAsyncPublisher(IConnectionFactory connectionFactory, bool publisherConfirms = true)
		{
			_queue = new BlockingCollection<EnqueuedMessage>();
			_confirmStrategy = publisherConfirms ? (IConfirmStrategy) new AckNackConfirmStrategy() : new NoConfirmStrategy();
			_connectionHolder = new ConnectionHolder(new[] {connectionFactory}, _confirmStrategy);

			_thread = new Thread(ThreadLoop) {Name = nameof(RabbitAsyncPublisher)};
			_thread.Start();
		}

		public void Dispose()
		{
			_queue.CompleteAdding();
			_thread.Join();
			_connectionHolder.Dispose();
			_queue.Dispose();
		}

		public Task PublishAsync(string exchange, byte[] body, string routingKey = "")
		{
			var tcs = new TaskCompletionSource<object>();
			_queue.Add(new EnqueuedMessage {
				Exchange = exchange,
				Body = body,
				RoutingKey = routingKey,
				Tcs = tcs
			});
			return tcs.Task;
		}

		private void ThreadLoop()
		{
			foreach (var msg in _queue.GetConsumingEnumerable())
			{
				if (_queue.IsAddingCompleted)
				{
					msg.Tcs.TrySetCanceled();
					continue;
				}
				_connectionHolder.Try(ch => {
					var basicProperties = ch.CreateBasicProperties();
					basicProperties.SetPersistent(true);
					_confirmStrategy.Publishing(ch);
					ch.BasicPublish(msg.Exchange, msg.RoutingKey, basicProperties, msg.Body);
					_confirmStrategy.Published(msg.Tcs);
				}, ex => {
					msg.Tcs.TrySetException(ex);
				});
			}
		}

		private class EnqueuedMessage
		{
			public byte[] Body { get; set; }
			public TaskCompletionSource<object> Tcs { get; set; }
			public string Exchange { get; set; }
			public string RoutingKey { get; set; }
		}
	}
}