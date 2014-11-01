namespace RabbitMQ.Async
{
	public interface IEventNotifier
	{
		void NotifyEnqueued();
		void NotifySent();
		void NotifyFailed();
		void NotifyAcked();
		void NotifyNacked();
		void NotifyUnacked();
		void NotifyCanceled();
	}
}