using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Npgsql;
namespace DBHelper
{
	public class ConnectionPool
	{
		/// <summary>
		/// Lock of Queue<ManualResetEvent>.
		/// </summary>
		static readonly object _lock = new object();
		/// <summary>
		/// Lock of Queue<NpgsqlConnection>.
		/// </summary>
		static readonly object _lockGetConn = new object();
		/// <summary>
		/// Default pool size.
		/// </summary>
		const int DEFAULT_POOL_SIZE = 32;
		/// <summary>
		/// Connection pool queue.
		/// </summary>
		readonly Queue<NpgsqlConnection> _poolFree;
		/// <summary>
		/// All connection of the pool.
		/// </summary>
		readonly List<NpgsqlConnection> _poolAll;
		/// <summary>
		/// Connection string
		/// </summary>
		readonly string _connectionString;
		/// <summary>
		/// Current connection pool size.
		/// </summary>
		readonly int _poolSize = 0;
		/// <summary>
		/// ManualResetEvent queue.
		/// </summary>
		readonly Queue<ManualResetEvent> _wait;
		/// <summary>
		/// Initialize connection pool size and connection string.
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
		/// Release connection.
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
		/// Get connection.
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
				if (_poolAll.Count < _poolSize) //if the connection pool is not full
					lock (_lockGetConn)
						_poolAll.Add(conn = CreateConnection());
				else
				{
					var wait = new ManualResetEvent(false);
					lock (_lock)
						_wait.Enqueue(wait);
					wait.WaitOne(5000); //wait 5 seconds without notification
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
		/// Create connection.
		/// </summary>
		/// <returns></returns>
		NpgsqlConnection CreateConnection() => !_connectionString.IsNullOrEmpty() ? new NpgsqlConnection(_connectionString) : throw new ArgumentNullException("Connection String is null.");

		/// <summary>
		/// Open connection.
		/// </summary>
		/// <param name="conn"></param>
		public void OpenConnection(NpgsqlConnection conn)
		{
			if (conn?.State == ConnectionState.Broken) conn.Close();
			if (conn?.State != ConnectionState.Open) conn.Open();
		}
		/// <summary>
		/// Close connection.
		/// </summary>
		/// <param name="conn"></param>
		public void CloseConnection(NpgsqlConnection conn)
		{
			if (conn?.State != ConnectionState.Closed) conn.Close();
		}
	}
}
