using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.MQHelper
{
	/// <summary>
	/// MQ线路
	/// </summary>
	public class MQLine
	{
		private ILogger _logger = null;
		private MQConnection _connection = null;
		private string _queueName = string.Empty;
		private string _exchange = string.Empty;
		private string _routingKey = string.Empty;
		private string _exchangeType = string.Empty;
		public Action<Message> OnReceivedCallback { get; set; }

		public event EventHandler<BasicDeliverEventArgs> Received;
		public event EventHandler<ConsumerEventArgs> Registered;
		public event EventHandler<ConsumerEventArgs> Unregistered;
		public event EventHandler<ShutdownEventArgs> Shutdown;
		public event EventHandler<ConsumerEventArgs> ConsumerCancelled;
		public MQLine(ILogger logger, MQConnection connecion, string exchange, string queueName, string routingKey, string exchangeType)
		{
			_logger = logger;
			_connection = connecion;
			_queueName = queueName;
			_exchange = exchange;
			_routingKey = routingKey;
			_exchangeType = exchangeType;
		}
		/// <summary>
		/// 创建通道
		/// </summary>
		/// <returns></returns>
		public IModel CreateChannel()
		{
			var channel = _connection.Connection.CreateModel();
			channel.QueueDeclare(_queueName, durable: true, autoDelete: false, exclusive: false, arguments: null);
			return channel;
		}
		/// <summary>
		/// 创建发送通道
		/// </summary>
		/// <returns></returns>
		public IModel CreatePublishChannel()
		{

			var channel = CreateChannel();
			channel.ExchangeDeclare(_exchange, _exchangeType, durable: true, autoDelete: false, arguments: null);
			channel.QueueBind(_queueName, _exchange, _routingKey);
			return channel;
		}
		/// <summary>
		/// 创建所接收通道
		/// </summary>
		public IModel CreateReceiveChannel()
		{
			var channel = CreateChannel();
			EventingBasicConsumer consumer = Receive(channel);
			consumer.Registered += (object sender, ConsumerEventArgs e) => { _logger.LogDebug($"已注册消息队列，{e.ConsumerTag}"); };
			consumer.Shutdown += (object sender, ShutdownEventArgs e) => { _logger.LogDebug($"已关闭消息队列，{e.ReplyCode}，{e.ReplyText}"); };
			consumer.ConsumerCancelled += (object sender, ConsumerEventArgs e) => { _logger.LogDebug($"已退出消息队列，{e.ConsumerTag}"); };
			return channel;
		}
		/// <summary>
		/// 发送队列消息
		/// </summary>
		/// <param name="message"></param>
		/// <param name="expireTimeTicks"></param>
		public void Publish(string message, long? expireTimeTicks = null)
		{
			if (string.IsNullOrEmpty(message)) return;
			var model = CreatePublishChannel();
			var props = model.CreateBasicProperties();
			props.Persistent = true;
			if (expireTimeTicks != null)
				props.Expiration = expireTimeTicks.ToString();
			var msgBody = Encoding.UTF8.GetBytes(message);
			model.BasicPublish(exchange: _exchange, routingKey: _routingKey, basicProperties: props, body: msgBody);
		}
		/// <summary>
		/// 添加接收时各个生命周期的事件
		/// </summary>
		/// <param name="model"></param>
		/// <param name="queue"></param>
		/// <returns></returns>
		public EventingBasicConsumer Receive(IModel model)
		{
			EventingBasicConsumer consumer = new EventingBasicConsumer(model);
			model.BasicConsume(_queueName, false, consumer);
			consumer.Received += Received;
			consumer.Received += OnReceived;
			consumer.Registered += Registered;
			consumer.Unregistered += Unregistered;
			consumer.Shutdown += Shutdown;
			consumer.ConsumerCancelled += ConsumerCancelled;
			return consumer;
		}

		internal void OnReceived(object sender, BasicDeliverEventArgs e)
		{
			Message msg = new Message();
			try
			{
				msg.Body = Encoding.UTF8.GetString(e.Body);
				msg.Consumer = (EventingBasicConsumer)sender;
				msg.EventArgs = e;
			}
			catch (Exception ex)
			{
				msg.ErrorMsg = $"订阅_出错: {ex.Message }";
				msg.Exception = ex;
				msg.IsError = true;
				msg.Code = 500;
				_logger.LogError(new EventId(1001), msg.Exception, msg.ErrorMsg);
			}
			OnReceivedCallback?.Invoke(msg);
		}
	}
}
