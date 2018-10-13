using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Npgsql;
namespace DBHelper
{
	public class ConnectionPool
	{
		/// <summary>
		/// 响应队列锁
		/// </summary>
		static readonly object _lock = new object();
		/// <summary>
		/// 连接池锁
		/// </summary>
		static readonly object _lockGetConn = new object();
		/// <summary>
		/// 默认连接池大小
		/// </summary>
		const int DEFAULT_POOL_SIZE = 32;
		/// <summary>
		/// 连接池队列
		/// </summary>
		readonly Queue<NpgsqlConnection> _poolFree;
		/// <summary>
		/// 连接池总数
		/// </summary>
		readonly List<NpgsqlConnection> _poolAll;
		/// <summary>
		/// connection string
		/// </summary>
		readonly string _connectionString;
		/// <summary>
		/// 当前连接池大小
		/// </summary>
		readonly int _poolSize = 0;
		/// <summary>
		/// 等待响应队列
		/// </summary>
		readonly Queue<ManualResetEvent> _wait;
		/// <summary>
		/// 初始化连接池大小
		/// </summary>
		/// <param name="poolSize"></param>
		public ConnectionPool(int poolSize, string connectionString)
		{
			_connectionString = connectionString;
			if (poolSize > 0) _poolSize = poolSize;
			else _poolSize = DEFAULT_POOL_SIZE;
			_poolFree = new Queue<NpgsqlConnection>(_poolSize);
			_poolAll = new List<NpgsqlConnection>(_poolSize);
			_wait = new Queue<ManualResetEvent>(_poolSize);
		}
		/// <summary>
		/// 获取数据库连接字符串Maximum Pool Size的值
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public static int GetConnectionPoolSize(string connectionString)
		{
			var poolSize = 32;
			var pattern = @"Maximum\s+Pool\s+Size\s*=\s*(\d+)";
			Match match = Regex.Match(connectionString, pattern, RegexOptions.IgnoreCase);
			if (match.Success)
				int.TryParse(match.Groups[1].Value, out poolSize);
			return poolSize;
		}
		/// <summary>
		/// 释放连接池
		/// </summary>
		/// <param name="conn"></param>
		public void ReleaseConnection(NpgsqlConnection conn)
		{
			if (conn != null)
			{
				CloseConnection(conn);
				lock (_lockGetConn)
					_poolFree.Enqueue(conn); //放回连接池
				lock (_lock)
					if (_wait.Count > 0)
						_wait.Dequeue()?.Set();
			}
		}
		/// <summary>
		/// Get连接
		/// </summary>
		/// <returns></returns>
		public NpgsqlConnection GetConnection()
		{
			NpgsqlConnection conn = null;
			var canGet = false;
			lock (_lockGetConn)
				canGet = _poolFree.TryDequeue(out conn);
			if (!canGet)
			{
				if (_poolAll.Count < _poolSize) //如果当前连接池没有满 
					lock (_lockGetConn)
						_poolAll.Add(conn = CreateConnection());
				else
				{
					var wait = new ManualResetEvent(false);
					lock (_lock)
						_wait.Enqueue(wait);
					wait.WaitOne(5000); //无通知等待5秒
					lock (_lockGetConn)
						canGet = _poolFree.TryDequeue(out conn);
					if (!canGet)
						return GetConnection();
				}
			}
			if (conn != null) OpenConnection(conn);
			else throw new ArgumentNullException("Npgsql Connection is null.");
			return conn;
		}
		/// <summary>
		/// 创建连接
		/// </summary>
		/// <returns></returns>
		NpgsqlConnection CreateConnection() => !_connectionString.IsNullOrEmpty() ? new NpgsqlConnection(_connectionString) : throw new ArgumentNullException("Connection String is null.");

		/// <summary>
		/// 打开连接
		/// </summary>
		/// <param name="conn"></param>
		public void OpenConnection(NpgsqlConnection conn)
		{
			if (conn?.State == ConnectionState.Broken) conn.Close();
			if (conn?.State != ConnectionState.Open) conn.Open();
		}
		/// <summary>
		/// 关闭连接
		/// </summary>
		/// <param name="conn"></param>
		public void CloseConnection(NpgsqlConnection conn)
		{
			if (conn?.State != ConnectionState.Closed) conn.Close();
		}
	}
}
