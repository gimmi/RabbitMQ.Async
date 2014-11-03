using System;
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

		public void WithChan(Action<IModel> action)
		{
			int next = 0;
			try
			{
				if (_conn == null || _chan == null)
				{
					Connect(_connectionFactories[next++]);
				}
				action.Invoke(_chan);
			}
			catch
			{
				SafeDispose();
				if (next >= _connectionFactories.Length)
				{
					throw;
				}
			}
		}

		public void Dispose()
		{
			SafeDispose();
		}

		private void Connect(IConnectionFactory connectionFactory)
		{
			SafeDispose();
			_conn = connectionFactory.CreateConnection();
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