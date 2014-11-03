using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitMQ.Async
{
	internal class ConnectionHolder : IDisposable
	{
		private readonly IConfirmStrategy _confirmStrategy;
		private readonly IConnectionFactory[] _connectionFactories;
		private IConnection _conn;
		private IModel _chan;

		internal ConnectionHolder(IConnectionFactory[] connectionFactories,IConfirmStrategy confirmStrategy)
		{
			_confirmStrategy = confirmStrategy;
			_connectionFactories = Shuffle(connectionFactories);
		}

		public void Try(Action<IModel> action, Action<AggregateException> catchAction)
		{
			var exceptions = new List<Exception>();
			var connectionIndex = 0;
			while (true)
			{
				try
				{
					if (_conn == null || _chan == null)
					{
						SafeDispose();
						Connect(connectionIndex++);
					}
					action.Invoke(_chan);
					return;
				}
				catch (Exception ex)
				{
					SafeDispose();
					exceptions.Add(ex);
					if (connectionIndex >= _connectionFactories.Length)
					{
						catchAction.Invoke(new AggregateException(exceptions));
						return;
					}
				}
			}
		}

		public void Dispose()
		{
			SafeDispose();
		}

		private void Connect(int connectionIndex)
		{
			_conn = _connectionFactories[connectionIndex].CreateConnection();
			_chan = _conn.CreateModel();
			_confirmStrategy.ChannelCreated(_chan);
		}

		private void SafeDispose()
		{
			if (_chan != null)
			{
				try
				{
					_chan.Dispose();
				}
				catch
				{
				}
				_chan = null;
			}
			if (_conn != null)
			{
				try
				{
					_conn.Dispose();
				}
				catch
				{
				}
				_conn = null;
			}
		}

		internal static IConnectionFactory[] Shuffle(IConnectionFactory[] inary)
		{
			var outary = new IConnectionFactory[inary.Length];
			inary.CopyTo(outary, 0);
			var rng = new Random();
			for (int n = outary.Length; n > 1; n--)
			{
				int k = rng.Next(n);
				var value = outary[k];
				outary[k] = outary[n - 1];
				outary[n - 1] = value;
			}
			return outary;
		}
	}
}