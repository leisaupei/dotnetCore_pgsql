using System;
using Npgsql;
using System.Data;
namespace Common.db.DBHelper
{
    public partial class DBConnection
    {
        public  NpgsqlConnection _conn = null;
		public static string ConnectionString { get; set; }
        protected  NpgsqlConnection GetConnection()
		{
            if (_conn == null)
                _conn = new NpgsqlConnection(ConnectionString);
			_conn.Open();
            return _conn;
		}
        protected  void OpenConnection(NpgsqlConnection _conn)
        {
            if (_conn.State == ConnectionState.Broken)
                _conn.Close();
            if (_conn.State != ConnectionState.Open)
                _conn.Open();
        }
        protected  void StopConnection(NpgsqlConnection _conn)
        {
            if (_conn.State != ConnectionState.Closed)
                _conn.Close();
        }
        protected  void RestartConnection(NpgsqlConnection _conn)
        {
            StopConnection(_conn);OpenConnection(_conn);
        }
		
    }
}
