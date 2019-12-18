using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meta.Common.Model
{
	public class DbFieldModel
	{
		public DbFieldModel()
		{

		}

		public NpgsqlDbType? NpgsqlDbType { get; set; } = null;
		public int Size { get; set; } = -1;

	}

}
