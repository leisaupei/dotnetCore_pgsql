using Meta.xUnitTest.Model;
using System;
using Microsoft.Extensions.Logging;
using Meta.Common.Model;
using Meta.Common.DbHelper;
using Newtonsoft.Json.Linq;
using Npgsql.TypeMapping;
using Npgsql;
using NpgsqlTypes;
using Npgsql.TypeHandling;
using System.Xml;
using Npgsql.PostgresTypes;
using Meta.Common.Extensions;

namespace Meta.xUnitTest.Options
{
    public static class DbOptions
    {
        #region DbTypeName
        /// <summary>
        /// 主库
        /// </summary>
        public const string Master = "master";
        /// <summary>
        /// 从库
        /// </summary>
        public const string Slave = "master-slave";
        #endregion

        #region Master
        public class MasterDbOption : BaseDbOption
        {
            public MasterDbOption(string masterConnectionString, string[] slaveConnectionStrings, ILogger logger) : base(Master, masterConnectionString, slaveConnectionStrings, logger)
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
