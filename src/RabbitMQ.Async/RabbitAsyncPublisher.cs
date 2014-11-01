using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitMQ.Async
{
	public class RabbitAsyncPublisher : IDisposable
	{
		private readonly string _exchange;
		private readonly BlockingCollection<EnqueuedMessage> _queue;
		private readonly AckNackConfirmStrategy _confirmStrategy;
		private readonly Thread _thread;
		private readonly IConnectionFactory _connectionFactory;
		private readonly Statistics _stats;

		public RabbitAsyncPublisher(IConnectionFactory connectionFactory, string exchange)
		{
			_stats = new Statistics();
			_queue = new BlockingCollection<EnqueuedMessage>();
			_confirmStrategy = new AckNackConfirmStrategy(_stats);

			_connectionFactory = connectionFactory;
			_exchange = exchange;

			_thread = new Thread(ThreadLoop) { Name = typeof(RabbitAsyncPublisher).Name };
			_thread.Start();
		}

		public Statistics Statistics
		{
			get { return _stats; }
		}

		public void Dispose()
		{
			_queue.CompleteAdding();
			_thread.Join();
			_queue.Dispose();
		}

		public Task PublishAsync(byte[] body)
		{
			var tcs = new TaskCompletionSource<object>();
			_queue.Add(new EnqueuedMessage { Body = body, Tcs = tcs });
			_stats.NotifyEnqueued();
			return tcs.Task;
		}

		private void ThreadLoop()
		{
			IConnection connection = null;
			IModel channel = null;
			foreach (var msg in _queue.GetConsumingEnumerable())
			{
				if (_queue.IsAddingCompleted)
				{
					msg.Tcs.TrySetCanceled();
					_stats.NotifyCanceled();
				}
				else
				{
					try
					{
						EnsureConnected(ref connection, ref channel);
						IBasicProperties basicProperties = channel.CreateBasicProperties();
						basicProperties.SetPersistent(true);
						_confirmStrategy.Publishing(channel);
						channel.BasicPublish(_exchange, "", basicProperties, msg.Body);
						_confirmStrategy.Published(msg.Tcs);
						_stats.NotifySent();
					}
					catch (Exception e)
					{
						_stats.NotifyFailed();
						msg.Tcs.TrySetException(e);
						SafeDispose(ref connection, ref channel);
					}
				}
			}
			SafeDispose(ref connection, ref channel);
		}

		private void EnsureConnected(ref IConnection connection, ref IModel channel)
		{
			if (connection == null || !connection.IsOpen || channel == null || channel.IsClosed)
			{
				SafeDispose(ref connection, ref channel);
				connection = _connectionFactory.CreateConnection();
				channel = connection.CreateModel();
				_confirmStrategy.ChannelCreated(channel);
			}
		}

		private void SafeDispose(ref IConnection connection, ref IModel channel)
		{
			if (channel != null)
			{
				try
				{
					channel.Dispose();
				}
				catch
				{
				}
				channel = null;
			}
			if (connection != null)
			{
				try
				{
					connection.Dispose();
				}
				catch
				{
				}
				connection = null;
			}
		}

		private class EnqueuedMessage
		{
			public byte[] Body { get; set; }
			public TaskCompletionSource<object> Tcs { get; set; }
		}
	}
}
