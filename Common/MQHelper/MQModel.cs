using System;
using RabbitMQ.Client.Events;

namespace Common.MQHelper
{
	public class Message
	{
		public string Body { get; set; }
		public EventingBasicConsumer Consumer { get; set; }
		public BasicDeliverEventArgs EventArgs { get; set; }
		public string ErrorMsg { get; set; }
		public Exception Exception { get; set; }
		public bool IsError { get; set; }
		public int Code { get; set; } = 0;
	}
}