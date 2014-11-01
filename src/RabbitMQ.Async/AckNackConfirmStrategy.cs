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
		private readonly Statistics _stats;
		private ulong _publishSeqNo;

		public AckNackConfirmStrategy(Statistics stats)
		{
			_pending = new ConcurrentDictionary<ulong, TaskCompletionSource<object>>();
			_stats = stats;
		}

		public void ChannelCreated(IModel channel)
		{
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
			foreach (ulong seqNo in GetSeqNos(args.DeliveryTag, args.Multiple))
			{
				TaskCompletionSource<object> tcs;
				if (_pending.TryRemove(seqNo, out tcs))
				{
					tcs.TrySetException(new RabbitNackException());
					_stats.NotifyNacked();
				}
			}
		}

		private void BasicAcks(IModel model, BasicAckEventArgs args)
		{
			foreach (ulong seqNo in GetSeqNos(args.DeliveryTag, args.Multiple))
			{
				TaskCompletionSource<object> tcs;
				if (_pending.TryRemove(seqNo, out tcs))
				{
					tcs.TrySetResult(null);
					_stats.NotifyAcked();
				}
			}
		}

		private IEnumerable<ulong> GetSeqNos(ulong deliveryTag, bool multiple)
		{
			if (multiple)
			{
				return _pending.Keys.Where(x => x <= deliveryTag);
			}
			return new[] { deliveryTag };
		}

		private void ModelShutdown(IModel channel, ShutdownEventArgs reason)
		{
			channel.BasicAcks -= BasicAcks;
			channel.BasicNacks -= BasicNacks;
			channel.ModelShutdown -= ModelShutdown;
			foreach (var kvp in _pending)
			{
				kvp.Value.TrySetException(new RabbitUnackException());
				_stats.NotifyUnacked();
			}
			_pending.Clear();
		}
	}
}