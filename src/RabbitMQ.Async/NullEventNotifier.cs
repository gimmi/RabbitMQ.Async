namespace RabbitMQ.Async
{
	public class NullEventNotifier :IEventNotifier
	{
		public void NotifyEnqueued()
		{
		}

		public void NotifySent()
		{
		}

		public void NotifyFailed()
		{
		}

		public void NotifyAcked()
		{
		}

		public void NotifyNacked()
		{
		}

		public void NotifyUnacked()
		{
		}

		public void NotifyCanceled()
		{
		}
	}
}