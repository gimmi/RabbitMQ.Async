using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitMQ.Async
{
	internal class NoConfirmStrategy : IConfirmStrategy
	{
		public void ChannelCreated(IModel channel)
		{
		}

		public void DisposingChannel(IModel channel)
		{
		}

		public void Publishing(IModel channel)
		{
		}

		public void Published(TaskCompletionSource<object> tcs)
		{
			tcs.SetResult(null);
		}

		public void Dispose()
		{
		}
	}
}