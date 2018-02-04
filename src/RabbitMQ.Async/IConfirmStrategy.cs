using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitMQ.Async
{
	internal interface IConfirmStrategy : IDisposable
	{
		void ChannelCreated(IModel channel);
		void Publishing(IModel channel);
		void Published(TaskCompletionSource<object> tcs);
	}
}