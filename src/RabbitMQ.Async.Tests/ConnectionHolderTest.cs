using System;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

namespace RabbitMQ.Async.Tests
{
	[TestFixture]
	public class ConnectionHolderTest
	{
		private Mock<IConnectionFactory> _connectionFactory1;
		private Mock<IConnectionFactory> _connectionFactory2;
		private Mock<IConnectionFactory> _connectionFactory3;
		private Mock<IConfirmStrategy> _confirmStrategy;
		private ConnectionHolder _sut;

		[SetUp]
		public void SetUp()
		{
			_connectionFactory1 = new Mock<IConnectionFactory>();
			_connectionFactory2 = new Mock<IConnectionFactory>();
			_connectionFactory3 = new Mock<IConnectionFactory>();
			_confirmStrategy = new Mock<IConfirmStrategy>();
			_sut = new ConnectionHolder(new[] { _connectionFactory1.Object, _connectionFactory2.Object, _connectionFactory3.Object }, _confirmStrategy.Object);
		}

		[Test]
		public void Should_shuffle_connections()
		{
			var actual = ConnectionHolder.Shuffle(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
			Assert.That(actual, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
			Assert.That(actual, Is.Not.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
		}

		[Test]
		public void Should_invoke_action_with_the_working_channel()
		{
			_connectionFactory1.Setup(x => x.CreateConnection()).Throws<Exception>();

			var conn2 = new Mock<IConnection>();
			_connectionFactory2.Setup(x => x.CreateConnection()).Returns(conn2.Object);
			conn2.Setup(x => x.CreateModel()).Throws<Exception>();
			
			var conn3 = new Mock<IConnection>();
			var chan3 = new Mock<IModel>();
			_connectionFactory3.Setup(x => x.CreateConnection()).Returns(conn3.Object);
			conn3.Setup(x => x.CreateModel()).Returns(chan3.Object);

			IModel passedChan = null;
			Exception passedExc = null;
			_sut.Try(c => passedChan = c, e => passedExc = e);

			Assert.That(passedChan, Is.SameAs(chan3.Object));
			Assert.That(passedExc, Is.Null);
			_confirmStrategy.Verify(x => x.ChannelCreated(chan3.Object));
		}

		[Test]
		public void Should_invoke_failure_action_when_all_connection_fail()
		{
			_connectionFactory1.Setup(x => x.CreateConnection()).Throws<Exception>();

			_connectionFactory2.Setup(x => x.CreateConnection()).Throws<Exception>();
			
			var conn3 = new Mock<IConnection>();
			_connectionFactory3.Setup(x => x.CreateConnection()).Returns(conn3.Object);
			conn3.Setup(x => x.CreateModel()).Throws<Exception>();

			IModel passedChan = null;
			Exception passedExc = null;
			_sut.Try(c => passedChan = c, e => passedExc = e);

			Assert.That(passedChan, Is.Null);
			Assert.That(passedExc, Is.InstanceOf<AggregateException>());
			_confirmStrategy.Verify(x => x.ChannelCreated(It.IsAny<IModel>()), Times.Never);
		}
	}
}