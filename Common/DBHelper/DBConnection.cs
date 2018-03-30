using System;
using Npgsql;
using System.Data;
namespace DBHelper
{
	public partial class DBConnection
	{
		public NpgsqlConnection _connection = null;
		public static string ConnectionString { get; set; }
		protected NpgsqlConnection GetConnection()
		{
			if (_connection == null)
				_connection = new NpgsqlConnection(ConnectionString);
			_connection.Open();
			return _connection;
		}
		protected void OpenConnection(NpgsqlConnection connection)
		{
			if (connection.State == ConnectionState.Broken)
				connection.Close();
			if (connection.State != ConnectionState.Open)
				connection.Open();
		}
		protected void StopConnection(NpgsqlConnection connection)
		{
			if (connection.State != ConnectionState.Closed)
				connection.Close();
		}
		protected void RestartConnection(NpgsqlConnection connection)
		{
			StopConnection(connection);
			OpenConnection(connection);
		}

	}
}
