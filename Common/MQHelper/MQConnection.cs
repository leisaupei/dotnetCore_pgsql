using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.MQHelper
{
	public class MQConnection
	{
		private string _host = string.Empty;
		private string _username = string.Empty;
		private string _password = string.Empty;
		private int _port = -1;
		private string _vhost = string.Empty;
		private IConnection _connection = null;
		private ILogger _logger = null;
		public MQConnection(string host, string username, string password, int port, string vhost, ILogger logger)
		{
			_host = host;
			_username = username;
			_password = password;
			_port = port;
			_vhost = vhost;
			_logger = logger;
		}
		/// <summary>
		/// 初始化connection
		/// </summary>
		public IConnection Connection =>
			_connection = _connection ?? new ConnectionFactory
			{
				UserName = _username,
				Password = _password,
				HostName = _host,
				VirtualHost = _vhost,
				Port = _port,
			}.CreateConnection();

		/// <summary>
		/// 关闭连接
		/// </summary>
		public void Close()
		{
			if (Connection != null && Connection.IsOpen)
				Connection.Close();
		}
	}
}
