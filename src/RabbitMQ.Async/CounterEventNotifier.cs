using System.Threading;

namespace RabbitMQ.Async
{
	public class CounterEventNotifier : IEventNotifier
	{
		private long _acked;
		private long _enqueued;
		private long _failed;
		private long _nacked;
		private long _unacked;
		private long _sent;
		private long _canceled;

		public long Enqueued
		{
			get { return _enqueued; }
		}

		public long Sent
		{
			get { return _sent; }
		}

		public long Failed
		{
			get { return _failed; }
		}

		public long Acked
		{
			get { return _acked; }
		}

		public long Nacked
		{
			get { return _nacked; }
		}

		public long Unacked
		{
			get { return _unacked; }
		}

		public long Canceled
		{
			get { return _canceled; }
		}

		public void NotifyEnqueued()
		{
			Interlocked.Increment(ref _enqueued);
		}

		public void NotifySent()
		{
			Interlocked.Increment(ref _sent);
		}

		public void NotifyFailed()
		{
			Interlocked.Increment(ref _failed);
		}

		public void NotifyAcked()
		{
			Interlocked.Increment(ref _acked);
		}

		public void NotifyNacked()
		{
			Interlocked.Increment(ref _nacked);
		}

		public void NotifyUnacked()
		{
			Interlocked.Increment(ref _unacked);
		}

		public void NotifyCanceled()
		{
			Interlocked.Increment(ref _canceled);
		}

		public override string ToString()
		{
			return string.Format("enqueued: {0}, sent: {1}, failed: {2}, canceled: {3}, acked: {4}, nacked: {5}, unacked: {6}", _enqueued, _sent, _failed, _canceled, _acked, _nacked, _unacked);
		}
	}
}
