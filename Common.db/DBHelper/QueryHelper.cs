using Common.db.Common;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Common.db.Extension;
using System.Text.RegularExpressions;

namespace Common.db.DBHelper
{
    public class QueryHelper<T>
    {
        #region 属性
        private int _params_count = 0;
        protected string ParamsIndex { get { return "parameter_" + _params_count++; } }
        protected string GroupByText { get; set; }
        protected string OrderByText { get; set; }
        protected string LimitText { get; set; }
        protected string OffsetText { get; set; }
        protected string HavingText { get; set; }
        protected string CommandText { get; set; }
        protected string MasterAliasName { get; } = "a";
        protected string UnionAliasName { get; set; }
        protected string Field { get; set; }
        protected List<UnionModel> UnionList { get; set; } = new List<UnionModel>();
        protected List<NpgsqlParameter> CommandParams { get; set; } = new List<NpgsqlParameter>();
        protected List<string> WhereList { get; set; } = new List<string>();
        #endregion

        #region Method

        #region where
        protected QueryHelper<T> Where(string filter, Array values)
        {
            if (values == null) values = new object[] { null };
            if (values.Length == 0) return this;
            if (values.Length == 1) return Where(filter, values.GetValue(0));
            string filters = string.Empty;
            for (int a = 0; a < values.Length; a++) filters = string.Concat(filters, " OR ", string.Format(filter, "{" + a + "}"));
            object[] parms = new object[values.Length];
            values.CopyTo(parms, 0);
            return Where(filters.Substring(4), parms);
        }
        protected QueryHelper<T> Where(string filter, params object[] value)
        {
            if (value == null) value = new object[] { null };
            if (new Regex(@"\{\d\}").Matches(filter).Count != value.Length)//参数个数不匹配
                throw new Exception("where 参数错误");
            if (value.IsNullOrEmpty())//参数不能为空
                throw new Exception("where 参数错误");

            List<string> str_where = new List<string>();
            for (int i = 0; i < value.Length; i++)
            {
                var params_name = ParamsIndex;
                var index = string.Concat("{", i, "}");
                if (filter.IndexOf(index, StringComparison.Ordinal) == -1) throw new Exception("where 参数错误");
                if (value[i] == null) //支持 Where("id = {0}", null); 写法
                    filter = Regex.Replace(filter, @"\s+=\s+\{" + i + @"\}", " IS NULL");
                else
                {
                    filter = filter.Replace(index, "@" + params_name);
                    AddSelectParameter(params_name, value[i]);
                }
            }
            WhereList.Add(string.Concat("(", filter, ")"));
            return this;
        }
        #endregion

        #region union
        protected QueryHelper<T> Union<TModel>(UnionType union_type, string alias_name, string on)
        {
            if (new Regex(@"\{\d\}").Matches(on).Count > 0)//参数个数不匹配
                throw new ArgumentException("on 参数不支持存在参数");

            UnionModel us = new UnionModel
            {
                Model = typeof(TModel),
                Expression = on,
                UnionType = union_type,
                AliasName = alias_name
            };
            UnionList.Add(us);
            return this;
        }
        #endregion

        #region db 
        /// <summary>
        /// 修改第一行
        /// </summary>
        /// <param name="cmdText"></param>
        /// <returns></returns>
        protected T ExecuteNonQueryReader(string cmdText)
        {
            List<T> info = ReaderToList<T>(PgSqlHelper.ExecuteDataReader(CommandType.Text, cmdText, CommandParams.ToArray()));
            return info.Count > 0 ? info[0] : default(T);
        }
        /// <summary>
        /// 返回多行
        /// </summary>
        protected List<TResult> ExecuteReader<TResult>()
        {
            GetSqlString<TResult>();
            return ReaderToList<TResult>(PgSqlHelper.ExecuteDataReader(CommandType.Text, CommandText, CommandParams.ToArray()));
        }
        protected int ExecuteNonQuery(string cmdText)
        {
            return PgSqlHelper.ExecuteNonQuery(CommandType.Text, cmdText, CommandParams.ToArray());
        }

        #endregion
        /// <summary>
        /// 返回列表
        /// </summary>
        public List<TResult> ToList<TResult>(string fields = null)
        {
            if (!fields.IsNullOrEmpty())
                Field = fields;
            return ExecuteReader<TResult>();
        }

        /// <summary>
        /// 返回一行
        /// </summary>
        protected TResult ToOne<TResult>(string fields = null)
        {
            LimitText = "LIMIT 1";
            List<TResult> list = ToList<TResult>(fields);
            if (list.Count > 0)
                return list[0];
            return default(TResult);
        }

        /// <summary>
        /// 返回一个元素
        /// </summary>
        protected TResult ToScalar<TResult>(string fields)
        {
            Field = fields;
            GetSqlString<TResult>();
            object obj = PgSqlHelper.ExecuteScalar(CommandType.Text, CommandText, CommandParams.ToArray());
            return (TResult)obj;
        }

        /// <summary>
        /// 
        /// </summary>
        protected string GetSqlString<TResult>()
        {
            //get table name
            Type mastertype = typeof(TResult);
            if (mastertype != typeof(T))
                mastertype = typeof(T);
            string tableName = MappingHelper.GetMapping(mastertype);

            StringBuilder sqlText = new StringBuilder();
            //sqlText.AppendLine($"SELECT {string.Join(',', Fields).ToLower()} FROM  {tableName} {MasterAliasName}");
            sqlText.AppendLine($"SELECT {Field} FROM  {tableName} {MasterAliasName}");
            foreach (var item in UnionList)
            {
                string union_alias_name = item.Model == mastertype ? MasterAliasName : item.AliasName;
                string union_table_name = MappingHelper.GetMapping(item.Model);
                sqlText.AppendLine(item.UnionType.ToString().Replace("_", " ") + " " + union_table_name + " " + union_alias_name + " ON " + item.Expression);
            }

            // other
            if (WhereList.Count > 0)
                sqlText.AppendLine("WHERE " + string.Join("\nAND ", WhereList));
            if (!string.IsNullOrEmpty(GroupByText))
                sqlText.AppendLine(GroupByText);
            if (!string.IsNullOrEmpty(GroupByText) && !string.IsNullOrEmpty(HavingText))
                sqlText.AppendLine(HavingText);
            if (!string.IsNullOrEmpty(OrderByText))
                sqlText.AppendLine(OrderByText);
            if (!string.IsNullOrEmpty(LimitText))
                sqlText.AppendLine(LimitText);
            if (!string.IsNullOrEmpty(OffsetText))
                sqlText.AppendLine(OffsetText);
            CommandText = sqlText.ToString();
            return CommandText;
        }

        #region ToList
        //Tresult 必须是能实例化的类型  不支持基本数据类型  例如List<string> 用ToListSingle<TResult>()
        public List<TResult> ReaderToList<TResult>(IDataReader objReader)
        {
            using (objReader)
            {
                List<TResult> list = new List<TResult>();

                //获取传入的数据类型
                Type modelType = typeof(TResult);
                bool isTuple = modelType.Namespace == "System" && modelType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
                //遍历DataReader对象
                while (objReader.Read())
                {
                    //if(modelType.Namespace == "System" && modelType.Name == "String)
                    //使用与指定参数匹配最高的构造函数，来创建指定类型的实例
                    TResult model = Activator.CreateInstance<TResult>();
                    FieldInfo[] fs = modelType.GetFields();
                    Type[] type = new Type[fs.Length];
                    object[] parms = new object[fs.Length];
                    for (int i = 0; i < objReader.FieldCount; i++)
                    {
                        if (isTuple)
                        {
                            type[i] = fs[i].FieldType;
                            parms[i] = objReader[i];
                        }
                        else
                        {
                            //判断字段值是否为空或不存在的值
                            if (!TypeExtension.IsNullOrDBNull(objReader[i]))
                            {
                                //todo: 
                                //应该输出全出的字段
                                //匹配字段名
                                PropertyInfo pi = modelType.GetProperty(objReader.GetName(i), BindingFlags.Default | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (pi != null)
                                {
                                    //绑定实体对象中同名的字段  
                                    pi.SetValue(model, CheckType(objReader[i], pi.PropertyType), null);
                                }
                            }
                        }
                    }
                    if (isTuple)
                    {
                        ConstructorInfo constructor = modelType.GetConstructor(type.ToArray());
                        model = (TResult)constructor.Invoke(parms);
                    }
                    list.Add(model);
                }
                return list;
            }
        }
        //重写支持一列  支持List<string>
        public List<TResult> ToListSingle<TResult>(IDataReader objReader)
        {
            using (objReader)
            {
                List<TResult> list = new List<TResult>();

                while (objReader.Read())
                {
                    TResult model = default(TResult);
                    //判断字段值是否为空或不存在的值
                    if (!TypeExtension.IsNullOrDBNull(objReader[objReader.GetName(0)]))
                    {
                        model = (TResult)CheckType(objReader[objReader.GetName(0)], typeof(TResult));
                    }
                    list.Add(model);
                }

                return list;
            }

        }

        /// <summary>
        /// 对可空类型进行判断转换(*要不然会报错)
        /// </summary>
        /// <param name="value">DataReader字段的值</param>
        /// <param name="conversionType">该字段的类型</param>
        /// <returns></returns>
        private object CheckType(object value, Type conversionType)
        {
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                    return null;
                NullableConverter nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            }
            return Convert.ChangeType(value, conversionType);
        }
        #endregion

        public QueryHelper<T> AddParameter(string field, NpgsqlDbType dbType, object value, int size, Type specificType)
        {
            NpgsqlParameter p = new NpgsqlParameter(field, dbType, size);
            if (specificType != null)
                p.SpecificType = specificType;
            p.Value = value;
            CommandParams.Add(p);
            return this;
        }
        private void AddSelectParameter(string field, object value)
        {
            var value_type = value.GetType();
            Type specificType = null;
            var dbType = TypeHelper.GetDbType(value_type);
            if (dbType == NpgsqlDbType.Enum)
                specificType = value_type;
            NpgsqlParameter p = new NpgsqlParameter(field, value);
            if (dbType != null)
                p.NpgsqlDbType = dbType.Value;
            if (specificType != null)
                p.SpecificType = specificType;
            CommandParams.Add(p);
        }
        #endregion
    }
}
