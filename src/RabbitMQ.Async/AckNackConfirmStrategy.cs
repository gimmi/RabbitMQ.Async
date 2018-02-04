using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Async
{
	internal class AckNackConfirmStrategy : IConfirmStrategy
	{
		private readonly ConcurrentDictionary<ulong, TaskCompletionSource<object>> _pending;
		private ulong _publishSeqNo;

		public AckNackConfirmStrategy()
		{
			_pending = new ConcurrentDictionary<ulong, TaskCompletionSource<object>>();
		}

		public void ChannelCreated(IModel channel)
		{
			UnackPending();
			channel.ConfirmSelect();
			channel.BasicAcks += BasicAcks;
			channel.BasicNacks += BasicNacks;
			channel.ModelShutdown += ModelShutdown;
		}

		public void Publishing(IModel channel)
		{
			_publishSeqNo = channel.NextPublishSeqNo;
		}

		public void Published(TaskCompletionSource<object> tcs)
		{
			_pending[_publishSeqNo] = tcs;
		}

		private void BasicNacks(IModel model, BasicNackEventArgs args)
		{
			foreach (var seqNo in _pending.Keys)
			{
				if (IsMatch(args.DeliveryTag, args.Multiple, seqNo) && _pending.TryRemove(seqNo, out var tcs))
				{
					tcs.TrySetException(new RabbitNackException());
				}
			}
		}

		private void BasicAcks(IModel model, BasicAckEventArgs args)
		{
			foreach (var seqNo in _pending.Keys)
			{
				if (IsMatch(args.DeliveryTag, args.Multiple, seqNo) && _pending.TryRemove(seqNo, out var tcs))
				{
					tcs.TrySetResult(null);
				}
			}
		}

		private static bool IsMatch(ulong deliveryTag, bool multiple, ulong seqNo)
		{
			return multiple ? seqNo <= deliveryTag : seqNo == deliveryTag;
		}

		private void ModelShutdown(IModel channel, ShutdownEventArgs reason)
		{
			channel.BasicAcks -= BasicAcks;
			channel.BasicNacks -= BasicNacks;
			channel.ModelShutdown -= ModelShutdown;
			UnackPending();
		}

		private void UnackPending()
		{
			foreach (var tcs in _pending.Values)
			{
				tcs.TrySetException(new RabbitUnackException());
			}

			_pending.Clear();
		}
	}
}