using Meta.Common.DbHelper;
using Meta.Common.Model;
using Meta.Common.SqlBuilder;
using Meta.Postgres.Generator.CodeFactory.Extension;
using NLog.LayoutRenderers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Meta.Postgres.Generator.CodeFactory.DAL
{
	/// <summary>
	/// 
	/// </summary>
	public class TablesDal
	{
		/// <summary>
		/// 项目名称
		/// </summary>
		readonly string _projectName;
		/// <summary>
		/// 模型路径
		/// </summary>
		readonly string _modelPath;
		/// <summary>
		/// DAL路径
		/// </summary>
		readonly string _dalPath;

		/// <summary>
		/// schema 名称
		/// </summary>
		readonly string _schemaName;

		/// <summary>
		/// 表/视图
		/// </summary>
		readonly TableViewModel _table;
		/// <summary>
		/// 是不是空间表
		/// </summary>
		bool _isGeometryTable = false;
		string _pkParameter;
		/// <summary>
		/// 是否视图
		/// </summary>
		readonly bool _isView = false;
		/// <summary>
		/// 字段列表
		/// </summary>
		List<FieldInfo> _fieldList = new List<FieldInfo>();
		/// <summary>
		/// 主键
		/// </summary>
		List<PrimarykeyInfo> _pkList = new List<PrimarykeyInfo>();
		/// <summary>
		/// 多对一外键
		/// </summary>
		List<ConstraintMoreToOne> _consListMoreToOne = new List<ConstraintMoreToOne>();
		/// <summary>
		/// 一对多外键(包含一对一)
		/// </summary>
		List<ConstraintOneToMore> _consListOneToMore = new List<ConstraintOneToMore>();
		///// <summary>
		///// 多对多外键
		///// </summary>
		//List<ConstraintMoreToMore> _consListMoreToMore = new List<ConstraintMoreToMore>();
		/// <summary>
		/// Model后缀
		/// </summary>
		const string ModelSuffix = "Model";
		/// <summary>
		/// 外键前缀
		/// </summary>
		const string ForeignKeyPrefix = "Get";
		/// <summary>
		/// 生成项目多库
		/// </summary>
		readonly string _dataBaseTypeName;
		/// <summary>
		/// 命名空间后缀
		/// </summary>
		string NamespaceSuffix => _dataBaseTypeName == GenerateModel.MASTER_DATABASE_TYPE_NAME && LetsGo.FinalType == _dataBaseTypeName ? "" : "." + _dataBaseTypeName;
		/// <summary>
		/// 多库枚举 *需要在目标项目添加枚举以及创建该库实例
		/// </summary>
		string DataSelectString => _dataBaseTypeName == GenerateModel.MASTER_DATABASE_TYPE_NAME ? "" : $".By<Db{_dataBaseTypeName}>()";
		/// <summary>
		/// Model名称
		/// </summary>
		string ModelClassName => DalClassName + ModelSuffix;
		/// <summary>
		/// DAL名称
		/// </summary>
		string DalClassName => Types.DeletePublic(_schemaName, _table.Name, isView: _isView);
		/// <summary>
		/// 表名
		/// </summary>
		string TableName => Types.DeletePublic(_schemaName, _table.Name, true, _isView).ToLowerPascal();

		static readonly string[] NotAddQues = { "string", "JToken", "byte[]", "object", "IPAddress", "Dictionary<string, string>", "NpgsqlTsQuery", "NpgsqlTsVector", "BitArray", "PhysicalAddress", "XmlDocument", "PostgisGeometry" };
		/// <summary>
		/// 构建函数
		/// </summary>
		/// <param name="projectName"></param>
		/// <param name="modelPath"></param>
		/// <param name="dalPath"></param>
		/// <param name="schemaName"></param>
		/// <param name="table"></param>
		/// <param name="type"></param>
		public TablesDal(string projectName, string modelPath, string dalPath, string schemaName, TableViewModel table, string type)
		{
			_dataBaseTypeName = type.ToUpperPascal();
			_projectName = projectName;
			_modelPath = modelPath;
			_dalPath = dalPath;
			_schemaName = schemaName;
			_table = table;
			Console.WriteLine($"Generating {_schemaName}.{_table.Name}...");
			GetFieldList();
			if (table.Type == "table")
			{
				GetPrimaryKey();
				GetConstraint();
			}
			if (table.Type == "view")
				_isView = true;

		}

		/// <summary>
		/// 获取字段
		/// </summary>
		void GetFieldList()
		{
			var sql = $@"
SELECT a.oid, 
	c.attnum as num, 
	c.attname AS field,
	c.attnotnull AS isnotnull, 
	d.description AS comment, 
	e.typcategory, 
	(f.is_identity = 'YES') as isidentity, 
	format_type(c.atttypid,c.atttypmod) AS type_comment, 
	c.attndims as dimensions,  
	(CASE WHEN f.character_maximum_length IS NULL THEN c.attlen ELSE f.character_maximum_length END) AS length,  
	(CASE WHEN e.typelem = 0 THEN e.typname WHEN e.typcategory = 'G' THEN format_type (c.atttypid, c.atttypmod) ELSE e2.typname END ) AS dbtype,  
	(CASE WHEN e.typelem = 0 THEN e.typtype ELSE e2.typtype END) AS datatype, ns.nspname, COALESCE(pc.contype = 'u',false) as isunique 
FROM pg_class a  
INNER JOIN pg_namespace b ON a.relnamespace = b.oid  
INNER JOIN pg_attribute c ON attrelid = a.oid  
LEFT OUTER JOIN pg_description d ON c.attrelid = d.objoid AND c.attnum = d.objsubid AND c.attnum > 0  
INNER JOIN pg_type e ON e.oid = c.atttypid  
LEFT JOIN pg_type e2 ON e2.oid = e.typelem  
INNER JOIN information_schema.COLUMNS f ON f.table_schema = b.nspname AND f.TABLE_NAME = a.relname AND COLUMN_NAME = c.attname  
LEFT JOIN pg_namespace ns ON ns.oid = e.typnamespace and ns.nspname <> 'pg_catalog'  
LEFT JOIN pg_constraint pc ON pc.conrelid = a.oid and pc.conkey[1] = c.attnum and pc.contype = 'u'  
WHERE (b.nspname='{_schemaName}' and a.relname='{_table.Name}')  
";
			_fieldList = PgsqlHelper.ExecuteDataReaderList<FieldInfo>(sql);

			foreach (var f in _fieldList)
			{
				f.IsArray = f.Dimensions > 0;
				f.DbType = f.DbType.StartsWith("_", StringComparison.Ordinal) ? f.DbType.Remove(0, 1) : f.DbType;
				f.PgDbType = Types.ConvertDbTypeToNpgsqlDbType(f.DataType, f.DbType, f.IsArray);
				f.PgDbTypeString = Types.ConvertDbTypeToNpgsqlDbTypeString(f.DbType, f.IsArray);
				f.IsEnum = f.DataType == "e";
				string _type = Types.ConvertPgDbTypeToCSharpType(f.DataType, f.DbType);
				if (f.DbType == "xml")
					EnumsDal.XmlTypeName.Add(_dataBaseTypeName);
				if (f.DbType == "geometry")
				{
					_isGeometryTable = true;
					EnumsDal.GeometryTableTypeName.Add(_dataBaseTypeName);
				}//if (f.DbType == "bit" && f.Length == 1)
				 //	_type = "bool";

				if (f.IsEnum)
					_type = Types.DeletePublic(f.Nspname, _type);
				f.CSharpType = _type;

				if (f.DataType == "c")
					f.RelType = Types.DeletePublic(f.Nspname, _type);
				else
				{
					string _notnull = "";
					if (!NotAddQues.Contains(_type) && !f.IsArray)
					{
						_notnull = f.IsNotNull ? "" : "?";
					}
					string _array = f.IsArray ? "[".PadRight(Math.Max(0, f.Dimensions), ',') + "]" : "";
					f.RelType = $"{_type}{_notnull}{_array}";
				}
			}
		}

		/// <summary>
		/// 获取约束
		/// </summary>
		void GetConstraint()
		{
			var sqlMoreToOne = $@"
SELECT f.attname conname, b.relname tablename, c.nspname, d.attname refcolumn, e.typname contype, g.indisprimary ispk 
FROM pg_constraint a  
LEFT JOIN pg_class b ON b.oid = a.confrelid  
INNER JOIN pg_namespace c ON b.relnamespace = c.oid  
INNER JOIN pg_attribute d ON d.attrelid = a.confrelid and d.attnum = any(a.confkey)  
INNER JOIN pg_type e ON e.oid = d.atttypid  
INNER JOIN pg_attribute f ON f.attrelid = a.conrelid and f.attnum = any(a.conkey)  
LEFT JOIN pg_index g ON d.attrelid = g.indrelid and d.attnum = any(g.indkey) and g.indisprimary  
WHERE conrelid IN (SELECT a.oid FROM pg_class a  INNER JOIN pg_namespace b ON a.relnamespace = b.oid  WHERE b.nspname = '{_schemaName}' AND A .relname = '{_table.Name}') 
";
			_consListMoreToOne = PgsqlHelper.ExecuteDataReaderList<ConstraintMoreToOne>(sqlMoreToOne);

			var sqlOneToMore = $@"
SELECT DISTINCT 
	x.TABLE_NAME tablename, 
	x.COLUMN_NAME refcolumn, 
	x.CONSTRAINT_SCHEMA nspname, 
	tp.typname contype, 
	tp.attname conname,  
	(SELECT COUNT(1)=1 FROM pg_index a  
		INNER JOIN pg_attribute b ON b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)  
		WHERE A .indrelid = (x.CONSTRAINT_SCHEMA||'.'||x. TABLE_NAME)::regclass
		AND a.indisprimary 
		AND b.attname = x.COLUMN_NAME 
		AND int2vectorout(A.indkey)::TEXT = '1'
	) as isonetoone 
FROM information_schema.key_column_usage x  
INNER JOIN (
	SELECT t.relname,a.conname,f.typname,d.attname 
	FROM pg_constraint a  
	INNER JOIN pg_class t ON t.oid = a.conrelid  
	INNER JOIN pg_attribute d ON d.attrelid = a.confrelid AND d.attnum = ANY (a.confkey)  
	INNER JOIN pg_type f ON f.oid = d.atttypid  
	WHERE a.contype = 'f' 
	AND a.confrelid = (
		SELECT e.oid FROM pg_class e  
		INNER JOIN pg_namespace b ON e.relnamespace = b.oid  
		WHERE b.nspname = '{_schemaName}' AND e.relname = '{_table.Name}'
	) 
) tp ON x. TABLE_NAME = tp.relname AND x. CONSTRAINT_NAME = tp.conname
";
			_consListOneToMore = PgsqlHelper.ExecuteDataReaderList<ConstraintOneToMore>(sqlOneToMore);

			#region	MoreToMore	
			//			if (_consListOneToMore.Count == 0) return;

			//			foreach (var item in _consListOneToMore)
			//			{
			//				var pkSql = $@"
			//SELECT  b.attname AS field,format_type (b.atttypid, b.atttypmod) AS typename 
			//FROM pg_index a 
			//INNER JOIN pg_attribute b ON b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)  
			//WHERE a.indrelid = '{item.Nspname}.{item.TableName}' :: regclass AND a.indisprimary and b.attname::text <> '{item.RefColumn}' 
			//";
			//				var pk = PgSqlHelper.ExecuteDataReaderList<PrimarykeyInfo>(pkSql);

			//				if (pk.Count == 0) continue;

			//				foreach (var p in pk)
			//				{
			//					var sqlMoreToMore = $@"
			//SELECT f.attname conname, b.relname tablename, c.nspname, d.attname refcolumn, e.typname contype, g.indisprimary ispk 
			//FROM pg_constraint a  
			//LEFT JOIN pg_class b ON b.oid = a.confrelid  
			//INNER JOIN pg_namespace c ON b.relnamespace = c.oid  
			//INNER JOIN pg_attribute d ON d.attrelid = a.confrelid and d.attnum = any(a.confkey)  
			//INNER JOIN pg_type e ON e.oid = d.atttypid  INNER JOIN pg_attribute f ON f.attrelid = a.conrelid and f.attnum = any(a.conkey)  
			//LEFT JOIN pg_index g ON d.attrelid = g.indrelid and d.attnum = any(g.indkey) and g.indisprimary  
			//WHERE conrelid IN (SELECT a.oid FROM pg_class a  INNER JOIN pg_namespace b ON a.relnamespace = b.oid WHERE b.nspname = '{item.Nspname}' and a.relname = '{item.TableName}') AND f.attname::text = '{p.Field}'
			//";
			//					var moretoone = PgSqlHelper.ExecuteDataReaderList<ConstraintMoreToOne>(sqlMoreToMore);
			//					foreach (var item2 in moretoone)
			//					{
			//						_consListMoreToMore.Add(new ConstraintMoreToMore
			//						{
			//							MainNspname = _schemaName,
			//							MainTable = _table.Name,
			//							CenterMainField = item.RefColumn,
			//							CenterMainType = item.Contype,
			//							CenterMinorField = item2.Conname,
			//							CenterMinorType = item2.Contype,
			//							CenterNspname = item.Nspname,
			//							CenterTable = item.TableName,
			//							MainField = item.Conname,
			//							MinorField = item2.RefColumn,
			//							MinorNspname = item2.Nspname,
			//							MinorTable = item2.TableName

			//						});
			//					}
			//				}
			//			}
			#endregion

		}

		/// <summary>
		/// 获取主键
		/// </summary>
		void GetPrimaryKey()
		{
			var sqlPk = $@"
SELECT b.attname AS field,format_type (b.atttypid, b.atttypmod) AS typename 
FROM pg_index a  
INNER JOIN pg_attribute b ON b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)  
WHERE a.indrelid = '{_schemaName}.{_table.Name}'::regclass AND a.indisprimary
";
			_pkList = PgsqlHelper.ExecuteDataReaderList<PrimarykeyInfo>(sqlPk);

			List<string> d_key = new List<string>();
			for (var i = 0; i < _pkList.Count; i++)
			{
				FieldInfo fs = _fieldList.FirstOrDefault(f => f.Field == _pkList[i].Field);
				d_key.Add(fs.RelType + " " + fs.Field);

			}

			_pkParameter = string.Join(", ", d_key);
		}

		/// <summary>
		/// 生成Model.cs文件
		/// </summary>
		public void ModelGenerator()
		{
			string _filename = $"{_modelPath}/{ModelClassName}.cs";

			using StreamWriter writer = new StreamWriter(File.Create(_filename), Encoding.UTF8);
			writer.WriteLine("using Meta.Common.Model;");
			writer.WriteLine("using System;");
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using System.Collections;");
			writer.WriteLine("using System.Net.NetworkInformation;");
			writer.WriteLine("using NpgsqlTypes;");
			writer.WriteLine("using Newtonsoft.Json;");
			writer.WriteLine("using Newtonsoft.Json.Linq;");
			writer.WriteLine("using Meta.Common.Interface;");
			writer.WriteLine("using System.Xml;");
			writer.WriteLine("using System.Net;");
			writer.WriteLine("using System.Threading.Tasks;");
			writer.WriteLine("using System.Threading;");
			if (_isGeometryTable)
				writer.WriteLine("using Npgsql.LegacyPostgis;");
			writer.WriteLine("using Meta.Common.SqlBuilder;");
			writer.WriteLine($"using {_projectName}.DAL{NamespaceSuffix};");
			writer.WriteLine();
			writer.WriteLine($"namespace {_projectName}.Model{NamespaceSuffix}");
			writer.WriteLine("{");
			writer.WriteLine($"\t[DbTable(\"{TableName}\")]");
			writer.WriteLine($"\tpublic partial class {ModelClassName} : IDbModel");
			writer.WriteLine("\t{");

			writer.WriteLine("\t\t#region Properties");
			foreach (var item in _fieldList)
			{
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
				{
					WriteComment(writer, item.Comment);
					writer.WriteLine($"\t\t[JsonProperty] public {item.RelType} {item.FieldUpCase} {{ get; set; }}");
				}

				if (item.DbType == "geometry")
				{
					WriteComment(writer, item.Comment);
					writer.WriteLine($"\t\t[JsonProperty] public {item.RelType} {item.FieldUpCase} {{ get; set; }}");
				}
			}
			writer.WriteLine("\t\t#endregion");
			writer.WriteLine();
			if (_table.Type == "table")
			{
				Hashtable ht = new Hashtable();
				var sb = new StringBuilder();


				void WriteForeignKey(string nspname, string tableName, string conname, bool? isPk, string refColumn)
				{
					var tableNameWithoutSuffix = Types.DeletePublic(nspname, tableName);
					string nspTableName = tableNameWithoutSuffix;
					string propertyName = $"{ForeignKeyPrefix}{tableNameWithoutSuffix}";
					if (ht.ContainsKey(propertyName))
						propertyName = propertyName + "By" + Types.ExceptUnderlineToUpper(conname);
					string tmp_var = $"_{propertyName.ToLowerPascal()}";
					if (ht.Keys.Count != 0)
						sb.AppendLine();

					sb.AppendFormat("\t\tprivate {0}{1} {2} = null;", nspTableName, ModelSuffix, tmp_var);
					sb.AppendLine();
					if (isPk == true)
						sb.AppendFormat("\t\tpublic {0}{1} {2} => {3} ??= {0}.GetItem({4});\n", nspTableName, ModelSuffix, propertyName, tmp_var, DotValueHelper(conname, _fieldList));
					else
						sb.AppendFormat("\t\tpublic {0}{1} {2} => {3} ??= {0}.Select.Where(a => a.{5} == {4}).ToOne();\n", nspTableName, ModelSuffix, propertyName, tmp_var, DotValueHelper(conname, _fieldList), refColumn.ToUpperPascal());

					if (propertyName.IsNotNullOrEmpty() && !ht.ContainsKey(propertyName))
						ht.Add(propertyName, "");
				}

				foreach (var item in _consListMoreToOne.Where(f => $"{f.TableName}_{f.RefColumn}" == f.Conname))
					WriteForeignKey(item.Nspname, item.TableName, item.Conname, item.IsPk, item.RefColumn);

				_consListMoreToOne.RemoveAll(f => $"{f.TableName}_{f.RefColumn}" == f.Conname);
				foreach (var item in _consListMoreToOne)
					WriteForeignKey(item.Nspname, item.TableName, item.Conname, item.IsPk, item.RefColumn);

				foreach (var item in _consListOneToMore.Where(f => f.IsOneToOne))
					WriteForeignKey(item.Nspname, item.TableName, item.Conname, true, item.RefColumn);

				if (!string.IsNullOrEmpty(sb.ToString()))
				{
					writer.WriteLine("\t\t#region Foreign Key");
					writer.Write(sb);
					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();
				}
				writer.WriteLine("\t\t#region Update/Insert");
				if (_pkList.Count > 0)
				{
					if (_pkList.Count == 1)
						writer.WriteLine("\t\tpublic UpdateBuilder<{0}> Update => DAL{2}.{1}.Update(this.{3});", ModelClassName, DalClassName, NamespaceSuffix, _pkList[0].FieldUpCase);
					else
						writer.WriteLine("\t\tpublic UpdateBuilder<{0}> Update => DAL{2}.{1}.Update(({3}));", ModelClassName, DalClassName, NamespaceSuffix, _pkList.Select(a => $"this.{a.FieldUpCase}").Join(", "));
				}
				writer.WriteLine();
				writer.WriteLine("\t\tpublic int Commit() => DAL{1}.{0}.Commit(this);", DalClassName, NamespaceSuffix);
				writer.WriteLine("\t\tpublic {0} Insert() => DAL{2}.{1}.Insert(this);", ModelClassName, DalClassName, NamespaceSuffix);
				writer.WriteLine("\t\tpublic ValueTask<int> CommitAsync(CancellationToken cancellationToken = default) => DAL{1}.{0}.CommitAsync(this, cancellationToken);", DalClassName, NamespaceSuffix);
				writer.WriteLine("\t\tpublic Task<{0}> InsertAsync(CancellationToken cancellationToken = default) => DAL{2}.{1}.InsertAsync(this, cancellationToken);", ModelClassName, DalClassName, NamespaceSuffix);
				writer.WriteLine("\t\t#endregion");
			}
			writer.WriteLine("\t}");
			writer.WriteLine("}");

			writer.Flush();

			DalGenerator();
		}

		/// <summary>
		/// 生成DAL.cs文件
		/// </summary>
		void DalGenerator()
		{
			string _filename = $"{_dalPath}/{DalClassName}.cs";

			using StreamWriter writer = new StreamWriter(File.Create(_filename), Encoding.UTF8);
			writer.WriteLine("using Meta.Common.SqlBuilder;");
			writer.WriteLine("using Meta.Common.Model;");
			writer.WriteLine($"using {_projectName}.{ModelSuffix}{NamespaceSuffix};");
			writer.WriteLine($"using {_projectName}.Options;");
			writer.WriteLine("using System.Collections;");
			writer.WriteLine("using System.Net.NetworkInformation;");
			writer.WriteLine("using NpgsqlTypes;");
			writer.WriteLine("using System;");
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using System.Linq;");
			writer.WriteLine("using Newtonsoft.Json.Linq;");
			writer.WriteLine("using System.Xml;");
			writer.WriteLine("using System.Net;");
			writer.WriteLine("using System.Threading.Tasks;");
			writer.WriteLine("using System.Threading;");
			writer.WriteLine("using Meta.Common.Interface;");
			if (_isGeometryTable)
				writer.WriteLine("using Npgsql;");
			writer.WriteLine();
			writer.WriteLine($"namespace {_projectName}.DAL{NamespaceSuffix}");
			writer.WriteLine("{");
			writer.WriteLine($"\tpublic sealed partial class {DalClassName} : SelectBuilder<{DalClassName}, {ModelClassName}>");
			writer.WriteLine("\t{");

			writer.WriteLine("\t\t#region Properties");
			PropertiesGenerator(writer);
			writer.WriteLine("\t\t#endregion");
			writer.WriteLine();

			if (_table.Type == "table")
			{
				writer.Write("\t\t#region Delete");
				DeleteGenerator(writer);
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine();

				writer.Write("\t\t#region Insert");
				InsertGenerator(writer);
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine();
			}
			writer.Write("\t\t#region Select");
			SelectGenerator(writer);
			writer.WriteLine("\t\t#endregion");
			writer.WriteLine();
			if (_table.Type == "table")
			{
				writer.Write("\t\t#region Update");
				UpdateGenerator(writer);
				writer.Write("\t\t#endregion");
				writer.WriteLine();
			}

			writer.WriteLine("\t}");
			writer.WriteLine("}");
		}

		/// <summary>
		/// 构建DAL属性
		/// </summary>
		/// <param name="writer"></param>
		void PropertiesGenerator(StreamWriter writer)
		{
			string parameterCount = string.Empty;
			for (int i = 0; i < _pkList.Count; i++)
			{
				parameterCount += string.Concat("_{", i, "}");
			}
			writer.WriteLine("\t\tpublic const string CacheKey = \"{0}\";", string.Concat(_projectName.Replace('.', '_').ToLower(), "_model_", ModelClassName.ToLower(), parameterCount));

			writer.WriteLine("\t\tprivate {0}() {{ }}", DalClassName);
			writer.WriteLine("\t\tpublic static {0} Select => new {0}(){1};", DalClassName, DataSelectString);
			if (_table.Type == "table")
			{
				writer.WriteLine("\t\tpublic static UpdateBuilder<{0}> UpdateBuilder => new UpdateBuilder<{0}>(){1};", ModelClassName, DataSelectString);
				writer.WriteLine("\t\tpublic static DeleteBuilder<{0}> DeleteBuilder => new DeleteBuilder<{0}>(){1};", ModelClassName, DataSelectString);
				writer.WriteLine("\t\tpublic static InsertBuilder<{0}> InsertBuilder => new InsertBuilder<{0}>(){1};", ModelClassName, DataSelectString);
			}
		}

		/// <summary>
		/// 构建删除方法
		/// </summary>
		/// <param name="writer"></param>
		void DeleteGenerator(StreamWriter writer)
		{
			if (_pkList.Count > 0)
			{
				List<string> d_key = new List<string>();
				string where = string.Empty, where1 = string.Empty, types = string.Empty;
				for (int i = 0; i < _pkList.Count; i++)
				{
					FieldInfo fs = _fieldList.FirstOrDefault(f => f.Field == _pkList[i].Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $"model.{fs.FieldUpCase}";
					where += $"{fs.Field}";
					if (i + 1 != _pkList.Count)
					{
						types += ", ";
						where1 += ", ";
						where += ", ";
					}
				}
				where1 = where1.Contains(",") ? $"({where1})" : where1;
				where = where.Contains(",") ? $"({where})" : where;
				if (_pkList.Count == 1)
				{
					writer.Write(@"
		public static int Delete(params {0}[] {1}s)
			=> DeleteAsync(false, CancellationToken.None, ids).ConfigureAwait(false).GetAwaiter().GetResult();

		public static ValueTask<int> DeleteAsync(CancellationToken cancellationToken = default, params {0}[] {1}s)
			=> DeleteAsync(true, cancellationToken, ids);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, params {0}[] {1}s)
		{{
			if ({1}s == null)
				throw new ArgumentNullException(nameof({1}s));
			if (DbConfig.DbCacheTimeOut != 0)
			{{
				var keys = {1}s.Select(f => string.Format(CacheKey, f)).ToArray();
				if(async)
					await RedisHelper.DelAsync(keys);
				else
					RedisHelper.Del(keys);
			}}
			if(async)
				return await DeleteBuilder.WhereAny(a => a.{2}, {1}s).ToRowsAsync(cancellationToken);
			return DeleteBuilder.WhereAny(a => a.{2}, {1}s).ToRows();
		}}
", types, _pkList[0].Field, _pkList[0].FieldUpCase);

				}
				else if (_pkList.Count > 1)
				{

					writer.Write(@"
		/// <summary>
		/// ({3})
		/// </summary>
		public static int Delete(params ({0})[] values)
			=> DeleteAsync(false, CancellationToken.None, values).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// ({3})
		/// </summary>
		public static ValueTask<int> DeleteAsync(CancellationToken cancellationToken = default, params ({0})[] values)
			=> DeleteAsync(true, cancellationToken, values);

		private static async ValueTask<int> DeleteAsync(bool async, CancellationToken cancellationToken, params ({0})[] values)
		{{
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (DbConfig.DbCacheTimeOut != 0)
			{{
				var keys = values.Select(f => string.Format(CacheKey{1})).ToArray();
				if(async)
					await RedisHelper.DelAsync(keys);
				else
					RedisHelper.Del(keys);
			}}
			if(async)
				return await DeleteBuilder.Where({2}, values).ToRowsAsync(cancellationToken);
			return DeleteBuilder.Where({2}, values).ToRows();
		}}
", types, string.Concat(_pkList.Select((f, index) => ", f.Item" + (index + 1))), _pkList.Select(a => $"a => a.{a.FieldUpCase}").Join(", "), _pkList.Select(a => $"{a.Field}").Join(", "));

				}
			}
		}

		/// <summary>
		/// 构建插入方法
		/// </summary>
		/// <param name="writer"></param>
		void InsertGenerator(StreamWriter writer)
		{
			if (_pkList.Count == 0) return;

			string where = ".Where(a => ";
			for (var i = 0; i < _pkList.Count; i++)
			{
				FieldInfo fs = _fieldList.FirstOrDefault(f => f.Field == _pkList[i].Field);
				where += $"a.{fs.FieldUpCase} == f.{fs.FieldUpCase}";
				if (i != _pkList.Count - 1)
					where += " && ";
			}
			where += ")";
			writer.Write(@"
		public static int Commit({0} model) 
			=> SetRedisCache(string.Format(CacheKey{1}), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows());

		public static {0} Insert({0} model)
		{{
			SetRedisCache(string.Format(CacheKey{1}), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRows(ref model));
			return model;
		}}

		public static int Commit(IEnumerable<{0}> models, bool isExceptionCancel = true)
		{{
			if (models == null)
				throw new ArgumentNullException(nameof(models));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultiple<Db{2}>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey{1}));
		}}

		public static Task<{0}> InsertAsync({0} model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey{1}), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToOneAsync(cancellationToken));

		public static ValueTask<int> CommitAsync({0} model, CancellationToken cancellationToken = default)
			=> SetRedisCacheAsync(string.Format(CacheKey{1}), model, DbConfig.DbCacheTimeOut, () => GetInsertBuilder(model).ToRowsAsync(cancellationToken), cancellationToken);

		public static ValueTask<int> CommitAsync(IEnumerable<{0}> models, bool isExceptionCancel = true, CancellationToken cancellationToken = default)
		{{
			if (models == null)
				return new ValueTask<int>(Task.FromException<int>(new ArgumentNullException(nameof(models))));
			var sqlbuilders = GetSqlBuilder(models, isExceptionCancel);
			return InsertMultipleAsync<Db{2}>(models, sqlbuilders, DbConfig.DbCacheTimeOut, (model) => string.Format(CacheKey{1}), cancellationToken);
		}}

		private static IEnumerable<ISqlBuilder> GetSqlBuilder(IEnumerable<{0}> models, bool isExceptionCancel)
		{{
			return isExceptionCancel
				? models.Select(f => GetInsertBuilder(f).ToRowsPipe())
				: models.Select(f => GetInsertBuilder(f).WhereNotExists(Select{3}).ToRowsPipe());
		}}

		private static InsertBuilder<{0}> GetInsertBuilder({0} model)
		{{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return InsertBuilder{4}
", ModelClassName, string.Concat(_pkList.Select(f => $", model.{f.FieldUpCase}")), _dataBaseTypeName, where, _fieldList.Count == 0 ? ";" : "");

			for (int i = 0; i < _fieldList.Count; i++)
			{
				FieldInfo item = _fieldList[i];
				string end = i + 1 == _fieldList.Count() ? ";" : "";
				if (item.IsIdentity) continue;
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\t\t.Set(a => a.{item.FieldUpCase}, model.{item.FieldUpCase}{SetInsertDefaultValue(item.Field, item.CSharpType, item.IsNotNull)}){end}");

				if (item.DbType == "geometry")
				{
					writer.WriteLine($"\t\t\t\t.Set(a => a.{item.FieldUpCase}, model.{item.FieldUpCase}{SetInsertDefaultValue(item.Field, item.CSharpType, item.IsNotNull)}){end}");

				}
			}
			writer.WriteLine("\t\t}");

		}

		/// <summary>
		/// 构建查询方法
		/// </summary>
		/// <param name="writer"></param>
		void SelectGenerator(StreamWriter writer)
		{
			StringBuilder sbEx = new StringBuilder();
			if (_pkList.Count > 0)
			{
				List<string> d_key = new List<string>();
				string where = string.Empty, types = string.Empty;
				for (var i = 0; i < _pkList.Count; i++)
				{
					FieldInfo fs = _fieldList.FirstOrDefault(f => f.Field == _pkList[i].Field);
					types += fs.RelType;

					if (i + 1 != _pkList.Count)
						types += ", ";

					d_key.Add(fs.RelType + " " + fs.Field);
					where += $"a.{fs.FieldUpCase} == {fs.Field}";
					if (i != _pkList.Count - 1)
						where += " && ";
				}
				writer.Write(@"
		public static {0} GetItem({1}) 
			=> GetRedisCache(string.Format(CacheKey{2}), DbConfig.DbCacheTimeOut, () => Select.Where(a =>{3}).ToOne());

		public static Task<{0}> GetItemAsync({1}, CancellationToken cancellationToken = default) 
			=> GetRedisCacheAsync(string.Format(CacheKey{2}), DbConfig.DbCacheTimeOut, () => Select.Where(a =>{3}).ToOneAsync(cancellationToken), cancellationToken);
", ModelClassName, string.Join(", ", d_key), string.Concat(_pkList.Select(f => $", {f.Field}")), where);

				if (_pkList.Count == 1)
				{
					writer.Write(@"
		public static List<{0}> GetItems(IEnumerable<{1}> {2}s) 
			=> Select.WhereAny(a => a.{3}, {2}s).ToList();

		public static Task<List<{0}>> GetItemsAsync(IEnumerable<{1}> {2}s, CancellationToken cancellationToken = default) 
			=> Select.WhereAny(a => a.{3}, {2}s).ToListAsync(cancellationToken);",
			ModelClassName, types, _pkList[0].Field, _pkList[0].FieldUpCase);

				}
				else if (_pkList.Count > 1)
				{
					writer.Write(@"
		/// <summary>
		/// ({3})
		/// </summary>
		public static List<{0}> GetItems(IEnumerable<({1})> values) 
			=> Select.Where({2}, values).ToList();

		/// <summary>
		/// ({3})
		/// </summary>
		public static Task<List<{0}>> GetItemsAsync(IEnumerable<({1})> values, CancellationToken cancellationToken = default) 
			=> Select.Where({2}, values).ToListAsync(cancellationToken);",
		ModelClassName, types, _pkList.Select(a => $"a => a.{a.FieldUpCase}").Join(", "), _pkList.Select(a => $"{a.Field}").Join(", "));

				}
			}
			foreach (var item in _fieldList)
			{
				if (item.IsIdentity) continue;
				if (item.DataType == "c") continue;
				if (item.Dimensions == 0)
				{
				}
				else if (item.Dimensions == 1)
				{
				}
				else if (item.Dimensions > 1)
				{

				}

			}
			writer.WriteLine(sbEx);
		}

		/// <summary>
		/// 构建更新方法
		/// </summary>
		/// <param name="writer"></param>
		void UpdateGenerator(StreamWriter writer)
		{
			if (_pkList.Count > 0)
			{
				List<string> d_key = new List<string>();
				string types = string.Empty;
				for (int i = 0; i < _pkList.Count; i++)
				{
					FieldInfo fs = _fieldList.FirstOrDefault(f => f.Field == _pkList[i].Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					if (i + 1 != _pkList.Count)
					{
						types += ", ";
					}
				}
				if (_pkList.Count == 1)
				{
					writer.Write(@"
		public static UpdateBuilder<{0}> Update(params {1}[] {2}s)
		{{
			if ({2}s == null)
				throw new ArgumentNullException(nameof({2}s));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del({2}s.Select(f => string.Format(CacheKey, f)).ToArray());
			return UpdateBuilder.WhereAny(a => a.{3}, {2}s);
		}}
",
		ModelClassName, types, _pkList[0].Field, _pkList[0].FieldUpCase);
				}
				else if (_pkList.Count > 1)
				{
					writer.Write(@"
		/// <summary>
		/// ({0})
		/// </summary>
		public static UpdateBuilder<{1}> Update(params ({2})[] values)
		{{
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if (DbConfig.DbCacheTimeOut != 0)
				RedisHelper.Del(values.Select(f => string.Format(CacheKey{3})).ToArray());
			return UpdateBuilder.Where({4}, values);
		}}
",
		_pkList.Select(a => $"{a.Field}").Join(", "), ModelClassName, types, string.Concat(_pkList.Select((f, index) => ", f.Item" + (index + 1))), _pkList.Select(a => $"a => a.{a.FieldUpCase}").Join(", "));
				}
			}

		}

		#region Private Method
		/// <summary>
		/// 写评论
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="comment"></param>
		private static void WriteComment(StreamWriter writer, string comment)
		{
			if (!string.IsNullOrEmpty(comment))
			{
				writer.WriteLine("\t\t/// <summary>");
				writer.WriteLine($"\t\t/// {comment}");
				writer.WriteLine("\t\t/// </summary>");
			}
		}
		/// <summary>
		/// optional字段加上.Value
		/// </summary>
		/// <param name="conname"></param>
		/// <param name="fields"></param>
		/// <returns></returns>
		public static string DotValueHelper(string conname, List<FieldInfo> fields)
		{
			conname = conname.ToUpperPascal();
			foreach (var item in fields)
				if (item.Field.ToLower() == conname.ToLower())
					if (item.RelType.Contains("?"))
						conname += ".Value";
			return conname;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cSharpType"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public static string WritePropertyGetSet(string cSharpType, string field)
		{
			string[] NotSet = { "x", "y" };
			Hashtable ht = new Hashtable { { "SRID", "4362" } };
			string[] NotJsonProperty = { "SRID" };
			var jsonproperty = NotJsonProperty.Contains(field) ? "" : "[JsonProperty] ";
			var set = NotSet.Contains(field) ? "" : "set;";
			var defaultValue = ht.ContainsKey(field) ? " = " + ht[field] + ";" : "";
			return $"{jsonproperty}public {cSharpType} {field.ToUpperPascal()} {{ get; {set} }}{defaultValue}";
		}
		/// <summary>
		/// 插入为空时
		/// </summary>
		/// <param name="field"></param>
		/// <param name="cSharpType"></param>
		/// <param name="isNotNull"></param>
		/// <returns></returns>
		public static string SetInsertDefaultValue(string field, string cSharpType, bool isNotNull)
		{
			return field switch
			{
				string f when f == "id" && cSharpType == "Guid" && isNotNull =>
					$" = model.{f.ToUpperPascal()} == Guid.Empty ? Guid.NewGuid() : model.{f.ToUpperPascal()}",
				string f when (f == "create_time" || f == "update_time") && cSharpType == "DateTime" && isNotNull =>
					$" = model.{f.ToUpperPascal()}.Ticks == 0 ? DateTime.Now : model.{f.ToUpperPascal()}",
				string f when (f == "create_time" || f == "update_time") && cSharpType == "DateTime" && !isNotNull =>
					" ??= DateTime.Now",
				string _ when cSharpType == "JToken" =>
					" ??= JToken.Parse(\"{}\")",
				_ => "",
			};
		}
		#endregion
	}
}