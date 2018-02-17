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
		}

		public void Publishing(IModel channel)
		{
			_publishSeqNo = channel.NextPublishSeqNo;
		}

		public void Published(TaskCompletionSource<object> tcs)
		{
			_pending[_publishSeqNo] = tcs;
		}

		public void Dispose()
		{
			UnackPending();
		}

		private void BasicNacks(object sender, BasicNackEventArgs args)
		{
			foreach (var seqNo in _pending.Keys)
			{
				if (IsMatch(args.DeliveryTag, args.Multiple, seqNo) && _pending.TryRemove(seqNo, out var tcs))
				{
					tcs.TrySetException(new RabbitNackException());
				}
			}
		}

		private void BasicAcks(object sender, BasicAckEventArgs args)
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