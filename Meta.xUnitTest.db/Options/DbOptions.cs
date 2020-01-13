using Meta.xUnitTest.Model;
using System;
using Microsoft.Extensions.Logging;
using Meta.Common.Model;
using Meta.Common.DbHelper;
using Newtonsoft.Json.Linq;
using Npgsql.TypeMapping;
using Meta.Common.Extensions;
using Npgsql;
using Meta.Common.Interface; 

namespace Meta.xUnitTest.Options
{
	#region DbTypeName
	/// <summary>
	/// 主库
	/// </summary>
	public struct DbMaster : IDbName { }
	/// <summary>
	/// 从库
	/// </summary>
	public struct DbMasterSlave : IDbName { }
	#endregion
	/// <summary>
	/// 由生成器生成, 会覆盖
	/// </summary>
	public static class DbOptions
	{

		#region Master
		public class MasterDbOption : BaseDbOption<DbMaster, DbMasterSlave>
		{
			public MasterDbOption(string masterConnectionString, string[] slaveConnectionStrings, ILogger logger) : base(masterConnectionString, slaveConnectionStrings, logger)
			{
				Options.MapAction = conn =>
				{
					conn.TypeMapper.UseJsonNetForJtype();
					conn.TypeMapper.UseCustomXml();
					conn.TypeMapper.MapEnum<EDataState>("public.e_data_state", _translator);
					conn.TypeMapper.MapComposite<Info>("public.info");
				};
			}
		}
		#endregion

		#region Private Method And Field
		private static readonly NpgsqlNameTranslator _translator = new NpgsqlNameTranslator();
		private static void UseJsonNetForJtype(this INpgsqlTypeMapper mapper)
		{
			var jtype = new[] { typeof(JToken), typeof(JObject), typeof(JArray) };
			mapper.UseJsonNet(jtype);
		}
		private class NpgsqlNameTranslator : INpgsqlNameTranslator
		{
			public string TranslateMemberName(string clrName) => clrName;
			public string TranslateTypeName(string clrName) => clrName;
		}
		#endregion
	}
}
