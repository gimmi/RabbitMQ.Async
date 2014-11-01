using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitMQ.Async.Tests
{
	public class TestUtils
	{
		public static IEnumerable<byte[]> GetAllMessages(string queue)
		{
			using (var connection = new ConnectionFactory().CreateConnection())
			{
				using (var channel = connection.CreateModel())
				{
					BasicGetResult basicGetResult;
					while ((basicGetResult = channel.BasicGet(queue, true)) != null)
					{
						yield return basicGetResult.Body;
					}
				}
			}
		}

		public static void DeleteBinding(string exchange, string queue)
		{
			using (var connection = new ConnectionFactory().CreateConnection())
			{
				using (var channel = connection.CreateModel())
				{
					channel.QueueUnbind(queue, exchange, "", null);
					channel.QueueDelete(queue);
					channel.ExchangeDelete(exchange);
				}
			}
		}

		public static void CreateBinding(string exchange, string queue)
		{
			using (var connection = new ConnectionFactory().CreateConnection())
			{
				using (var channel = connection.CreateModel())
				{
					channel.ExchangeDeclare(exchange, ExchangeType.Fanout, true, false, null);
					channel.QueueDeclare(queue, true, false, false, null);
					channel.QueueBind(exchange, queue, "");
				}
			}
		}
	}
}