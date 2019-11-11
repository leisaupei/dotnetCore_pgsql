using CodeFactory.Extension;
using DBHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace CodeFactory.DAL
{
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
		string _schemaName;
		/// <summary>
		/// 表/视图
		/// </summary>
		TableViewModel _table;
		/// <summary>
		/// 是不是空间表
		/// </summary>
		bool _isGeometryTable = false;
		/// <summary>
		/// 是否视图
		/// </summary>
		bool _isView = false;
		/// <summary>
		/// 字段列表
		/// </summary>
		List<FieldInfo> fieldList = new List<FieldInfo>();
		/// <summary>
		/// 主键
		/// </summary>
		List<PrimarykeyInfo> pkList = new List<PrimarykeyInfo>();
		/// <summary>
		/// 多对一外键
		/// </summary>
		List<ConstraintMoreToOne> consListMoreToOne = new List<ConstraintMoreToOne>();
		/// <summary>
		/// 一对多外键(包含一对一)
		/// </summary>
		List<ConstraintOneToMore> consListOneToMore = new List<ConstraintOneToMore>();
		/// <summary>
		/// 多对多外键
		/// </summary>
		List<ConstraintMoreToMore> consListMoreToMore = new List<ConstraintMoreToMore>();
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
		/// 多库前缀
		/// </summary>
		string ModelDalSuffix => _dataBaseTypeName == GenerateModel.MASTER_DATABASE_TYPE_NAME ? "" : _dataBaseTypeName;
		/// <summary>
		/// 多库枚举 *需要在目标项目添加枚举以及创建该库实例
		/// </summary>
		string DataSelectString => _dataBaseTypeName == GenerateModel.MASTER_DATABASE_TYPE_NAME ? "" : $".Data(DatabaseType.{_dataBaseTypeName})";
		/// <summary>
		/// Model名称
		/// </summary>
		string ModelClassName => DalClassName + ModelSuffix;
		/// <summary>
		/// DAL名称
		/// </summary>
		string DalClassName => ModelDalSuffix + Types.DeletePublic(_schemaName, _table.Name, isView: _isView);
		/// <summary>
		/// 表名
		/// </summary>
		string TableName => Types.DeletePublic(_schemaName, _table.Name, true, _isView).ToLowerPascal();
		/// <summary>
		/// 构建函数
		/// </summary>
		/// <param name="projectName"></param>
		/// <param name="modelPath"></param>
		/// <param name="dalPath"></param>
		/// <param name="schemaName"></param>
		/// <param name="table"></param>
		public TablesDal(string projectName, string modelPath, string dalPath, string schemaName, TableViewModel table, string type)
		{
			_dataBaseTypeName = type;
			_projectName = projectName;
			_modelPath = modelPath;
			_dalPath = dalPath;
			_schemaName = schemaName;
			_table = table;
			Console.WriteLine($"Generating {_schemaName}.{_table.Name}...");
			if (table.Type == "table")
			{
				GetPrimaryKey();
				GetConstraint();
			}
			if (table.Type == "view")
				_isView = true;
			GetFieldList();
		}

		/// <summary>
		/// 获取字段
		/// </summary>
		void GetFieldList()
		{
			//string sqlText = $@"
			// SELECT
			// 	A.oid,
			// 	C.attnum AS num,
			// 	C.attname AS field,
			// 	(CASE WHEN f.character_maximum_length IS NULL THEN c.attlen
			// 		ELSE f.character_maximum_length
			// 		END) AS LENGTH,
			// 	C .attnotnull AS NOTNULL,
			// 	d.description AS COMMENT,
			// 	(CASE WHEN e.typelem = 0 THEN e.typname
			// 		WHEN e.typcategory = 'G' THEN format_type(c.atttypid, c.atttypmod)
			// 		ELSE e2.typname
			// 		END) AS dbtype,
			// 	format_type (C.atttypid, C.atttypmod) AS type_comment,
			// 	(CASE WHEN e.typelem = 0 THEN e.typtype ELSE e2.typtype END) AS data_type,
			//   e.typcategory,
			// 	f.is_identity,
			// 	COALESCE(pc.contype = 'u', false) as isunique,c.attndims as dimensions,
			// 	pc.check
			// FROM
			// 	pg_class A
			// INNER JOIN pg_namespace b ON A .relnamespace = b.oid
			// INNER JOIN pg_attribute C ON attrelid = A.oid
			// LEFT OUTER JOIN pg_description d ON C.attrelid = d.objoid AND C .attnum = d.objsubid AND C .attnum > 0
			// INNER JOIN pg_type e ON e.oid = C.atttypid
			// LEFT JOIN pg_type e2 ON e2.oid = e.typelem
			// INNER JOIN information_schema.COLUMNS f ON f.table_schema = b.nspname AND f. TABLE_NAME = A.relname AND COLUMN_NAME = C.attname
			// left join(
			// 	select string_agg(consrc,' and ') as check,conrelid, contype, conkey
			// 	from pg_constraint
			// 	group by conrelid, contype, conkey
			// ) pc on pc.conrelid = a.oid and pc.conkey[1] = C.attnum
			// WHERE
			//	  b.nspname='{_schemaName}' and a.relname='{_table.Name}';
			//";
			//PgSqlHelper.ExecuteDataReader(dr =>
			//{
			//	FieldInfo fi = new FieldInfo
			//	{
			//		Oid = Convert.ToInt32(dr["oid"]),
			//		Field = dr["field"].ToString(),
			//		Length = Convert.ToInt32(dr["length"].ToString()),
			//		IsNotNull = Convert.ToBoolean(dr["notnull"]),
			//		Comment = dr["comment"].ToNullOrString(),
			//		DataType = dr["data_type"].ToString(),
			//		DbType = dr["type"].ToString(),
			//		IsIdentity = dr["is_identity"].ToString() == "YES",
			//		IsArray = dr["typcategory"].ToString() == "A",
			//		Typcategory = dr["typcategory"].ToString()
			//	};
			//	fi.DbType = fi.DbType.StartsWith("_") ? fi.DbType.Remove(0, 1) : fi.DbType;
			//	fi.PgDbType = Types.ConvertDbTypeToNpgsqlDbTypeEnum(fi.DataType, fi.DbType);
			//	fi.IsEnum = fi.DataType == "e";
			//	string _type = Types.ConvertPgDbTypeToCSharpType(fi.DbType);

			//	if (fi.IsEnum) _type = _type.ToUpperPascal();
			//	string _notnull = "";
			//	if (_type != "string" && _type != "JToken" && _type != "byte[]" && !fi.IsArray && _type != "object")
			//		_notnull = fi.IsNotNull ? "" : "?";

			//	string _array = fi.IsArray ? "[]" : "";
			//	fi.RelType = $"{_type}{_notnull}{_array}";
			//	// dal
			//	this.fieldList.Add(fi);
			//}, sqlText);
			fieldList = SQL.Select(@"a.oid, c.attnum as num, c.attname AS field, c.attnotnull AS isnotnull, d.description AS comment, e.typcategory,
				(f.is_identity = 'YES') as isidentity, format_type(c.atttypid,c.atttypmod) AS type_comment, c.attndims as dimensions,
				(CASE WHEN f.character_maximum_length IS NULL THEN c.attlen ELSE f.character_maximum_length END) AS length,
				(CASE WHEN e.typelem = 0 THEN e.typname WHEN e.typcategory = 'G' THEN format_type (c.atttypid, c.atttypmod) ELSE e2.typname END ) AS dbtype,
				(CASE WHEN e.typelem = 0 THEN e.typtype ELSE e2.typtype END) AS datatype, ns.nspname, COALESCE(pc.contype = 'u',false) as isunique").From("pg_class")
			   .InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
			   .InnerJoin("pg_attribute", "c", "attrelid = a.oid")
			   .Join(UnionEnum.LEFT_OUTER_JOIN, "pg_description", "d", "c.attrelid = d.objoid AND c.attnum = d.objsubid AND c.attnum > 0")
			   .InnerJoin("pg_type", "e", "e.oid = c.atttypid")
			   .LeftJoin("pg_type", "e2", "e2.oid = e.typelem")
			   .InnerJoin("information_schema.COLUMNS", "f", "f.table_schema = b.nspname AND f.TABLE_NAME = a.relname AND COLUMN_NAME = c.attname")
			   .LeftJoin("pg_namespace", "ns", "ns.oid = e.typnamespace and ns.nspname <> 'pg_catalog'")
			   .LeftJoin("pg_constraint", "pc", "pc.conrelid = a.oid and pc.conkey[1] = c.attnum and pc.contype = 'u'")
			   .Where($"b.nspname='{_schemaName}' and a.relname='{_table.Name}'").ToList<FieldInfo>();
			foreach (var f in fieldList)
			{
				f.IsArray = f.Dimensions > 0;
				f.DbType = f.DbType.StartsWith("_") ? f.DbType.Remove(0, 1) : f.DbType;
				f.PgDbType = Types.ConvertDbTypeToNpgsqlDbType(f.DataType, f.DbType, f.IsArray);
				f.PgDbTypeString = Types.ConvertDbTypeToNpgsqlDbTypeString(f.DataType, f.DbType, f.IsArray);
				f.IsEnum = f.DataType == "e";
				string _type = Types.ConvertPgDbTypeToCSharpType(f.DataType, f.DbType);

				if (f.IsEnum)
					_type = ModelDalSuffix + Types.DeletePublic(f.Nspname, _type);
				f.CSharpType = _type;

				if (f.DataType == "c")
					f.RelType = ModelDalSuffix + Types.DeletePublic(f.Nspname, _type);
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
		static readonly string[] NotAddQues = new[] { "string", "JToken", "byte[]", "object", "IPAddress" };
		/// <summary>
		/// 获取约束
		/// </summary>
		void GetConstraint()
		{
			//多对一关系
			//string sqlText_MoreToOne = $@"
			// SELECT
			// 	f.attname conname,
			// 	b.relname tablename,
			// 	C .nspname,
			// 	d.attname refcolumn,
			// 	e.typname contype,
			// 	(g is not null) ispk
			// FROM
			// 	pg_constraint A
			// LEFT JOIN pg_class b ON b.oid = A.confrelid
			// INNER JOIN pg_namespace C ON b.relnamespace = C.oid
			// INNER JOIN pg_attribute d ON d.attrelid = A.confrelid AND d.attnum = ANY(A.confkey)
			// INNER JOIN pg_type e ON e.oid = d.atttypid
			// inner join pg_attribute f on f.attrelid = A.conrelid AND f.attnum = ANY(A.conkey)
			// left join pg_index g on d.attrelid = g.indrelid AND d.attnum = ANY(g.indkey) and g.indisprimary
			// WHERE
			// 	conrelid IN(
			// 		SELECT
			// 			A .oid
			// 		FROM
			// 			pg_class A
			// 		INNER JOIN pg_namespace b ON A .relnamespace = b.oid
			// 		WHERE
			// 			b.nspname = 'channel' AND A .relname = 'gift_order'
			// 	);
			//consListMoreToOne = PgSqlHelper.ExecuteDataReaderList<ConstraintMoreToOne>(sqlText_MoreToOne);
			consListMoreToOne = SQL.Select($@"f.attname conname, b.relname tablename, c.nspname, d.attname refcolumn, e.typname contype, g.indisprimary ispk").From("pg_constraint")
				.LeftJoin("pg_class", "b", "b.oid = a.confrelid")
				.InnerJoin("pg_namespace", "c", "b.relnamespace = c.oid")
				.InnerJoin("pg_attribute", "d", "d.attrelid = a.confrelid and d.attnum = any(a.confkey)")
				.InnerJoin("pg_type", "e", "e.oid = d.atttypid")
				.InnerJoin("pg_attribute", "f", "f.attrelid = a.conrelid and f.attnum = any(a.conkey)")
				.LeftJoin("pg_index", "g", "d.attrelid = g.indrelid and d.attnum = any(g.indkey) and g.indisprimary")
				.WhereIn($"conrelid", SQL.Select("a.oid").From("pg_class")
					.InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
					.Where($"b.nspname = '{_schemaName}' AND A .relname = '{_table.Name}'"))
				.ToList<ConstraintMoreToOne>();


			////一对多关系
			//string sqlText_OneToMore = $@"
			//	SELECT DISTINCT 
			//		x. TABLE_NAME as tablename,
			//		x. COLUMN_NAME as refcolumn,
			//		x. CONSTRAINT_SCHEMA as nspname,
			//		tp.typname as contype,
			//		tp.attname as conname,
			//		(
			//			SELECT
			//				COUNT (1)
			//			FROM
			//				pg_index A
			//			INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
			//			AND b.attnum = ANY (A .indkey)
			//			WHERE
			//				A .indrelid = (
			//					x. CONSTRAINT_SCHEMA || '.' || x. TABLE_NAME
			//				) :: regclass
			//			AND A .indisprimary
			//			AND b.attname = x. COLUMN_NAME
			//			AND int2vectorout (A .indkey) :: TEXT = '1'
			//		) = 1 AS isonetoone
			//	FROM
			//		information_schema.key_column_usage x
			//	INNER JOIN (
			//		SELECT
			//			T .relname,
			//			A .conname,
			//			f.typname,
			//			d.attname
			//		FROM
			//			pg_constraint A
			//		INNER JOIN pg_class T ON T .oid = A .conrelid
			//		INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
			//		INNER JOIN pg_type f ON f.oid = d.atttypid
			//		WHERE
			//			A .contype = 'f'
			//		AND A .confrelid = (
			//			SELECT
			//				e.oid
			//			FROM
			//				pg_class e
			//			INNER JOIN pg_namespace b ON e.relnamespace = b.oid
			//			WHERE
			//				b.nspname = '{_schemaName}' AND e.relname = '{_table.Name}'
			//		)
			//	) tp ON (
			//		x. TABLE_NAME = tp.relname AND x. CONSTRAINT_NAME = tp.conname
			//	)
			//";
			//consListOneToMore = PgSqlHelper.ExecuteDataReaderList<ConstraintOneToMore>(sqlText_OneToMore);

			consListOneToMore = SQL.Select($@"DISTINCT x.TABLE_NAME as tablename, x.COLUMN_NAME as refcolumn, x.CONSTRAINT_SCHEMA as nspname, tp.typname as contype, tp.attname as conname,
				({SQL.Select("COUNT(1)=1").From("pg_index")
					.InnerJoin("pg_attribute", "b", "b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)")
					.Where("A .indrelid = (x. CONSTRAINT_SCHEMA || '.' || x. TABLE_NAME)::regclass")
					.Where("a.indisprimary AND b.attname = x.COLUMN_NAME AND int2vectorout(A.indkey) :: TEXT = '1'")}) as isonetoone")
				.From("information_schema.key_column_usage", "x")
				.InnerJoin(
					SQL.Select("t.relname,a.conname,f.typname,d.attname").From("pg_constraint")
					.InnerJoin("pg_class", "t", "t.oid = a.conrelid")
					.InnerJoin("pg_attribute", "d", "d.attrelid = a.confrelid AND d.attnum = ANY (a.confkey)")
					.InnerJoin("pg_type", "f", "f.oid = d.atttypid").Where("a.contype = 'f'")
					.Where($@"a.confrelid = ({SQL.Select("e.oid").From("pg_class", "e")
						.InnerJoin("pg_namespace", "b", "e.relnamespace = b.oid")
						.Where($"b.nspname = '{_schemaName}' AND e.relname = '{_table.Name}'")})"),
				"tp", "x. TABLE_NAME = tp.relname AND x. CONSTRAINT_NAME = tp.conname")
				.ToList<ConstraintOneToMore>();


			//多对多关系
			if (consListOneToMore.Count > 0)
			{
				foreach (var item in consListOneToMore)
				{
					//string union_table_sql = $@"
					//	SELECT
					//	    b.attname AS field,
					//	    format_type (b.atttypid, b.atttypmod) AS typename
					//	FROM
					//	    pg_index A
					//	INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
					//	AND b.attnum = ANY (A .indkey)
					//	WHERE
					//	    A .indrelid = '{item.Nspname}.{item.TableName}' :: regclass
					//	AND A .indisprimary                
					//";
					//List<PrimarykeyInfo> pk = new List<PrimarykeyInfo>();
					//PgSqlHelper.ExecuteDataReader(dr =>
					//{
					//	if (dr["field"]?.ToString() != item.RefColumn)
					//		pk.Add(new PrimarykeyInfo { Field = dr["field"].ToString(), TypeName = dr["typename"].ToString() });
					//}, union_table_sql);
					var pk = SQL.Select(" b.attname AS field,format_type (b.atttypid, b.atttypmod) AS typename").From("pg_index")
						.InnerJoin("pg_attribute", "b", "b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)")
						.Where($"a.indrelid = '{item.Nspname}.{item.TableName}' :: regclass AND a.indisprimary and b.attname::text <> '{item.RefColumn}'")
						.ToList<PrimarykeyInfo>();
					if (pk.Count > 0)
					{
						foreach (var p in pk)
						{
							//string sql1 = $@"
							//	SELECT
							//	    (
							//	        SELECT
							//	            attname
							//	        FROM
							//	            pg_attribute
							//	        WHERE
							//	            attrelid = A .conrelid AND attnum = ANY (A .conkey)
							//	    ) AS conname,
							//	    b.relname AS TABLE_NAME,
							//	    C .nspname,
							//	    d.attname AS ref_column,
							//	    e.typname AS contype
							//	FROM
							//	    pg_constraint A
							//	LEFT JOIN pg_class b ON b.oid = A .confrelid
							//	INNER JOIN pg_namespace C ON b.relnamespace = C .oid
							//	INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
							//	INNER JOIN pg_type e ON e.oid = d.atttypid
							//	WHERE
							//	    conrelid IN (
							//	        SELECT
							//	            A .oid
							//	        FROM
							//	            pg_class A
							//	        INNER JOIN pg_namespace b ON A .relnamespace = b.oid
							//	        WHERE
							//	            b.nspname = '{item.Nspname}' AND A .relname = '{item.TableName}'
							//	    );                            
							//";
							//List<ConstraintMoreToOne> moretoone = new List<ConstraintMoreToOne>();
							//PgSqlHelper.ExecuteDataReader(dr =>
							//{
							//	if (dr["conname"]?.ToString() == p.Field)
							//	{
							//		moretoone.Add(new ConstraintMoreToOne
							//		{
							//			Conname = dr["conname"].ToString(),
							//			TableName = dr["table_name"].ToString(),
							//			RefColumn = dr["ref_column"].ToString(),
							//			Contype = dr["contype"].ToString(),
							//			Nspname = dr["nspname"].ToString()

							//		});
							//	}
							//}, sql1);
							var moretoone = SQL.Select($@"f.attname conname, b.relname tablename, c.nspname, d.attname refcolumn, e.typname contype, g.indisprimary ispk").From("pg_constraint")
								.LeftJoin("pg_class", "b", "b.oid = a.confrelid")
								.InnerJoin("pg_namespace", "c", "b.relnamespace = c.oid")
								.InnerJoin("pg_attribute", "d", "d.attrelid = a.confrelid and d.attnum = any(a.confkey)")
								.InnerJoin("pg_type", "e", "e.oid = d.atttypid")
								.InnerJoin("pg_attribute", "f", "f.attrelid = a.conrelid and f.attnum = any(a.conkey)")
								.LeftJoin("pg_index", "g", "d.attrelid = g.indrelid and d.attnum = any(g.indkey) and g.indisprimary")
								.WhereIn($"conrelid", SQL.Select("a.oid").From("pg_class").InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
									.Where($"b.nspname = '{item.Nspname}' and a.relname = '{item.TableName}'"))
								.Where($"f.attname::text = '{p.Field}'")
								.ToList<ConstraintMoreToOne>();
							foreach (var item2 in moretoone)
							{
								consListMoreToMore.Add(new ConstraintMoreToMore
								{
									MainNspname = _schemaName,
									MainTable = _table.Name,
									CenterMainField = item.RefColumn,
									CenterMainType = item.Contype,
									CenterMinorField = item2.Conname,
									CenterMinorType = item2.Contype,
									CenterNspname = item.Nspname,
									CenterTable = item.TableName,
									MainField = item.Conname,
									MinorField = item2.RefColumn,
									MinorNspname = item2.Nspname,
									MinorTable = item2.TableName

								});
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// 获取主键
		/// </summary>
		void GetPrimaryKey()
		{
			//string sqlText = $@"
			//	SELECT
			//		b.attname AS field,
			//		format_type (b.atttypid, b.atttypmod) AS typename
			//	FROM
			//		pg_index A
			//	INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
			//	AND b.attnum = ANY (A .indkey)
			//	WHERE
			//		A .indrelid = '{_schemaName}.{_table.Name}' :: regclass
			//	AND A .indisprimary;
			//";
			//pkList = PgSqlHelper.ExecuteDataReaderList<PrimarykeyInfo>(sqlText);
			pkList = SQL.Select("b.attname AS field,format_type (b.atttypid, b.atttypmod) AS typename").From("pg_index")
				.InnerJoin("pg_attribute", "b", "b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)")
				.Where($"a.indrelid = '{_schemaName}.{_table.Name}'::regclass AND a.indisprimary").ToList<PrimarykeyInfo>();
		}

		/// <summary>
		/// 生成Model.cs文件
		/// </summary>
		public void ModelGenerator()
		{
			string _filename = $"{_modelPath}/{ModelClassName}.cs";

			using (StreamWriter writer = new StreamWriter(File.Create(_filename), Encoding.UTF8))
			{
				writer.WriteLine("using DBHelper;");
				writer.WriteLine("using System;");
				writer.WriteLine("using System.Collections.Generic;");
				writer.WriteLine("using System.Linq;");
				writer.WriteLine("using System.Threading.Tasks;");
				writer.WriteLine("using NpgsqlTypes;");
				writer.WriteLine("using Newtonsoft.Json;");
				writer.WriteLine("using Newtonsoft.Json.Linq;");
				writer.WriteLine("using System.Net;");
				if (ModelDalSuffix != "")
					writer.WriteLine($"using {_projectName}.Model.{ModelDalSuffix};");
				writer.WriteLine($"using {_projectName}.DAL;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.Model");
				writer.WriteLine("{");
				writer.WriteLine($"\t[Mapping(\"{TableName}\"), JsonObject(MemberSerialization.OptIn)]");
				writer.WriteLine($"\tpublic partial class {ModelClassName}");
				writer.WriteLine("\t{");

				writer.WriteLine("\t\t#region Properties");
				foreach (var item in fieldList)
				{
					if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					{
						WriteComment(writer, item.Comment);
						writer.WriteLine($"\t\t[JsonProperty] public {item.RelType} {item.Field.ToUpperPascal()} {{ get; set; }}");
					}
					if (item.DbType == "geometry")
					{
						_isGeometryTable = true;
						List<FieldInfo> str_field = new List<FieldInfo>
						{
							new FieldInfo { Comment = item.Field + "纬度", Field = item.Field + "_y", RelType = "float" },
							new FieldInfo { Comment = item.Field + "经度", Field = item.Field + "_x", RelType = "float" },
							new FieldInfo { Comment = item.Field + "空间坐标系唯一标识", Field = item.Field + "_srid", RelType = "int" }
						};
						foreach (var field in str_field)
						{
							WriteComment(writer, item.Comment);
							if (field.Field == item.Field + "_srid")
								writer.WriteLine($"\t\tpublic {field.RelType} {field.Field.ToUpperPascal()} {{ get; set;}} = 4326;");
							else
								writer.WriteLine($"\t\t[JsonProperty] public {field.RelType} {field.Field.ToUpperPascal()} {{ get; set;}}");
						}
					}
				}
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine();
				if (_table.Type == "table")
				{
					Hashtable ht = new Hashtable();
					writer.WriteLine("\t\t#region Foreign Key");
					void WriteForeignKey(string nspname, string tableName, string conname, bool? isPk, string refColumn)
					{
						var tableNameWithoutSuffix = Types.DeletePublic(nspname, tableName);
						string nspTableName = ModelDalSuffix + tableNameWithoutSuffix;
						string propertyName = $"{ForeignKeyPrefix}{tableNameWithoutSuffix}";
						if (ht.ContainsKey(propertyName))
							propertyName = propertyName + "By" + Types.ExceptUnderlineToUpper(conname);
						string tmp_var = $"_{propertyName.ToLowerPascal()}";
						if (ht.Keys.Count != 0)
							writer.WriteLine();

						writer.WriteLine("\t\tprivate {0}{1} {2} = null;", nspTableName, ModelSuffix, tmp_var);
						if (isPk == true)
							writer.WriteLine("\t\tpublic {0}{1} {2} => {3} ?? ({3} = {0}.GetItem({4}));", nspTableName, ModelSuffix, propertyName, tmp_var, DotValueHelper(conname, fieldList));
						else
							writer.WriteLine("\t\tpublic {0}{1} {2} => {3} ?? ({3} = {0}.Select.Where{5}({4}).ToOne());", nspTableName, ModelSuffix, propertyName, tmp_var, DotValueHelper(conname, fieldList), refColumn.ToUpperPascal());

						if (propertyName.IsNotNullOrEmpty() && !ht.ContainsKey(propertyName))
							ht.Add(propertyName, "");
					}

					foreach (var item in consListMoreToOne.Where(f => $"{f.TableName}_{f.RefColumn}" == f.Conname))
						WriteForeignKey(item.Nspname, item.TableName, item.Conname, item.IsPk, item.RefColumn);

					consListMoreToOne.RemoveAll(f => $"{f.TableName}_{f.RefColumn}" == f.Conname);
					foreach (var item in consListMoreToOne)
						WriteForeignKey(item.Nspname, item.TableName, item.Conname, item.IsPk, item.RefColumn);

					foreach (var item in consListOneToMore)
					{
						if (item.IsOneToOne)
						{
							WriteForeignKey(item.Nspname, item.TableName, item.Conname, true, item.RefColumn);
						}
						else
						{
							//propertyName = $"{_foreignKeyPrefix}{tablename}s";
							//if (ht.Contains(propertyName))
							//	propertyName = propertyName + "By" + Types.ExceptUnderlineToUpper(item.RefColumn);
							//string tmp_var = $"_{propertyName.ToLowerPascal()}";

							//writer.WriteLine();
							//writer.WriteLine($"\t\tprivate List<{tablename}{_modelSuffix}> {tmp_var} = null;");
							//writer.WriteLine($"\t\tpublic List<{tablename}{_modelSuffix}> {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.Select.Where{item.RefColumn.ToUpperPascal()}({item.Conname.ToUpperPascal()}).ToList();");
							//writer.WriteLine($"\t\tpublic List<{tablename}{_modelSuffix}> {propertyName}(int index, int size) =>  {tablename}.Select.Where{item.RefColumn.ToUpperPascal()}({item.Conname.ToUpperPascal()}).Page(index, size).ToList();");
							//if (propertyName.IsNotNullOrEmpty())
							//	ht.Add(propertyName, "");
						}


					}
					foreach (var item in consListMoreToMore)
					{
						//string minorTableName = Types.DeletePublic(item.MinorNspname, item.MinorTable);
						//string propertyName = $"{_foreignKeyPrefix}{minorTableName}s";
						//string centerTableName = Types.DeletePublic(item.CenterNspname, item.CenterTable);
						//string mainTableName = Types.DeletePublic(item.MainNspname, item.MainTable);
						//if (ht.Contains(propertyName))
						//	propertyName = propertyName + $"By{centerTableName}{Types.ExceptUnderlineToUpper(item.CenterMainField)}";

						//string tmp_var = $"_{propertyName.ToLowerPascal()}";

						//writer.WriteLine();
						//writer.WriteLine($"\t\tprivate List<{minorTableName}{_modelSuffix}> {tmp_var} = null;");
						//string foreignKeyField = $"{mainTableName.ToLowerPascal()}_{item.MainField}";
						//if (item.CenterMainField == foreignKeyField)
						//	writer.WriteLine($"\t\tpublic List<{minorTableName}{_modelSuffix}> {propertyName} => {tmp_var} = {tmp_var} ?? {minorTableName}.Select.InnerJoin<{centerTableName}>(\"b\", $\"b.{item.CenterMinorField} = a.{item.MinorField}\").Where(\"b.{foreignKeyField} = {{0}}\", {item.MainField.ToUpperPascal()}).ToList();");
						//else
						//	writer.WriteLine($"\t\tpublic List<{minorTableName}{_modelSuffix}> {propertyName} => {tmp_var} = {tmp_var} ?? {minorTableName}.Select.InnerJoin<{centerTableName}>(\"b\", \"b.{item.CenterMinorField} = a.{item.MinorField}\").InnerJoin<{mainTableName}>(\"c\", \"c.{item.MainField} = b.{item.CenterMainField}\").Where(\"c.{item.MainField} = {{0}}\", {item.MainField.ToUpperPascal()}).ToList();");
						//ht.Add(propertyName, "");
					}

					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();
					writer.WriteLine("\t\t#region Update/Insert");
					if (pkList.Count > 0)//[MethodProperty] 
						writer.WriteLine("\t\tpublic {0}.{0}UpdateBuilder Update => DAL.{0}.Update(this);", DalClassName);
					writer.WriteLine();
					if (pkList.Count > 0)
						writer.WriteLine("\t\tpublic int Delete() => DAL.{0}.Delete(this);", DalClassName);
					writer.WriteLine("\t\tpublic int Commit() => DAL.{0}.Commit(this);", DalClassName);
					writer.WriteLine("\t\tpublic {0} Insert() => DAL.{1}.Insert(this);", ModelClassName, DalClassName);
					writer.WriteLine("\t\t#endregion");
				}
				writer.WriteLine("");
				writer.WriteLine("\t\tpublic override string ToString() => JsonConvert.SerializeObject(this);");
				writer.WriteLine("\t\tpublic static {0} Parse(string json) => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<{0}>(json);", ModelClassName);
				writer.WriteLine("\t}");
				writer.WriteLine("}");

				writer.Flush();

				DalGenerator();
			}
		}

		/// <summary>
		/// 生成DAL.cs文件
		/// </summary>
		void DalGenerator()
		{
			string _filename = $"{_dalPath}/{DalClassName}.cs";

			using (StreamWriter writer = new StreamWriter(File.Create(_filename), Encoding.UTF8))
			{
				writer.WriteLine("using DBHelper;");
				writer.WriteLine($"using {_projectName}.Model;");
				writer.WriteLine("using NpgsqlTypes;");
				writer.WriteLine("using System;");
				writer.WriteLine("using System.Collections.Generic;");
				writer.WriteLine("using System.Linq;");
				writer.WriteLine("using System.Linq.Expressions;");
				writer.WriteLine("using System.Threading.Tasks;");
				writer.WriteLine("using Newtonsoft.Json.Linq;");
				writer.WriteLine("using System.Net;");
				if (ModelDalSuffix != "")
					writer.WriteLine($"using {_projectName}.Model.{ModelDalSuffix};");
				if (_isGeometryTable)
					writer.WriteLine("using Npgsql;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.DAL");
				writer.WriteLine("{");
				writer.WriteLine($"\t[Mapping(\"{TableName}\")]");
				writer.WriteLine($"\tpublic partial class {DalClassName} : SelectExchange<{DalClassName}, {ModelClassName}>");
				writer.WriteLine("\t{");

				writer.WriteLine("\t\t#region Properties");
				PropertiesGenerator(writer);
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine();

				if (_table.Type == "table")
				{
					writer.WriteLine("\t\t#region Delete");
					DeleteGenerator(writer);
					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();

					writer.WriteLine("\t\t#region Insert");
					InsertGenerator(writer);
					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();
				}
				writer.WriteLine("\t\t#region Select");
				SelectGenerator(writer);
				writer.WriteLine("\t\t#endregion");
				writer.WriteLine();
				if (_table.Type == "table")
				{
					writer.WriteLine("\t\t#region Update");
					UpdateGenerator(writer);
					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();
				}

				writer.WriteLine("\t}");
				writer.WriteLine("}");
			}
		}

		/// <summary>
		/// 构建DAL属性
		/// </summary>
		/// <param name="writer"></param>
		void PropertiesGenerator(StreamWriter writer)
		{
			StringBuilder sb_field = new StringBuilder();
			StringBuilder sb_param = new StringBuilder();
			StringBuilder sb_query = new StringBuilder();
			for (int i = 0; i < fieldList.Count; i++)
			{
				var item = fieldList[i];
				if (item.IsIdentity) continue;
				if (item.DbType == "geometry")
				{
					sb_query.AppendFormat("ST_X(a.{0}) as {0}_x, ST_Y(a.{0}) as {0}_y, ST_SRID(a.{0}) as {0}_srid", item.Field);
					sb_field.Append($"{item.Field}");
					sb_param.AppendFormat("ST_GeomFromText(@{0}_point0, @{0}_srid0)", item.Field);
				}
				else
				{
					sb_query.Append($"a.{item.Field}");
					sb_field.Append($"{item.Field}");
					sb_param.Append($"@{item.Field}");
				}
				if (fieldList.Count > i + 1)
				{
					sb_field.Append(", ");
					sb_param.Append(", ");
					sb_query.Append(", ");
				}
			}
			//if (_table.Type == "table")
			//	writer.WriteLine($"\t\tconst string insertSqlText = \"INSERT INTO {TableName} ({sb_field.ToString()}) VALUES({sb_param}) RETURNING {sb_query.ToString().Replace("a.", "")}\";");
			if (_isGeometryTable)
			{
				writer.WriteLine($"\t\tconst string _field = \"{sb_query.ToString()}\";");
				writer.WriteLine($"\t\tpublic {DalClassName}() => Fields = _field;");
			}
			writer.WriteLine("\t\tpublic static {0} Select => new {0}(){1};", DalClassName, DataSelectString);
			writer.WriteLine("\t\tpublic static {0} SelectDiy(string fields) => new {0} {{ Fields = fields }}{1};", DalClassName, DataSelectString);
			writer.WriteLine("\t\tpublic static {0} SelectDiy(string fields, string alias) => new {0} {{ Fields = fields, MainAlias = alias }}{1};", DalClassName, DataSelectString);
			if (_table.Type == "table")
			{
				writer.WriteLine("\t\tpublic static {0}UpdateBuilder UpdateDiy => new {0}UpdateBuilder(){1};", DalClassName, DataSelectString);
				writer.WriteLine("\t\tpublic static DeleteBuilder DeleteDiy => new DeleteBuilder(\"{0}\"){1};", TableName, DataSelectString);

				writer.WriteLine("\t\tpublic static InsertBuilder InsertDiy => new InsertBuilder(\"{0}\"{1}){2};", TableName, _isGeometryTable ? ", _field" : "", DataSelectString);
			}
		}

		/// <summary>
		/// 构建删除方法
		/// </summary>
		/// <param name="writer"></param>
		void DeleteGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where = string.Empty, where1 = string.Empty, types = string.Empty, pgStr = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $"model.{fs.Field.ToUpperPascal()}";
					where += $"{fs.Field}";
					pgStr += fs.PgDbTypeString.IsNullOrEmpty() ? "null" : fs.PgDbTypeString.TrimStart(' ', ',');
					if (i + 1 != pkList.Count)
					{
						types += ", ";
						where1 += ", ";
						where += ", ";
						pgStr += ", ";
					}
				}
				where1 = where1.Contains(",") ? $"({where1})" : where1;
				where = where.Contains(",") ? $"({where})" : where;
				writer.WriteLine($"\t\tpublic static int Delete({ModelClassName} model) => Delete(new[] {{ {where1} }});");
				writer.WriteLine($"\t\tpublic static int Delete({string.Join(", ", d_key)}) => Delete(new[] {{ {where} }});");
				if (pkList.Count == 1)
				{
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{ModelSuffix}> models) => Delete(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{types}> {s_key[0]}) => DeleteDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}{fieldList.FirstOrDefault(f => f.Field == pkList[0].Field).PgDbTypeString}).Commit();");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{ModelSuffix}> models) =>  Delete(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<({types})> val) => DeleteDiy.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val, new NpgsqlDbType?[]{{ {pgStr} }}).Commit();");
				}
			}
		}

		/// <summary>
		/// 构建插入方法
		/// </summary>
		/// <param name="writer"></param>
		void InsertGenerator(StreamWriter writer)
		{
			writer.WriteLine("\t\tpublic static int Commit({0} model) => GetInsertBuilder(model).Commit();", ModelClassName);
			writer.WriteLine("\t\tpublic static {0} Insert({0} model) => GetInsertBuilder(model).Commit<{0}>();", ModelClassName);
			writer.WriteLine($"\t\tprivate static InsertBuilder GetInsertBuilder({ModelClassName} model)");
			writer.WriteLine("\t\t{");
			writer.WriteLine($"\t\t\treturn InsertDiy");
			var i = 1;
			foreach (var item in fieldList)
			{
				string end = i == fieldList.Count() ? ";" : "";
				if (item.IsIdentity) continue;
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\t\t.Set(\"{item.Field}\", model.{item.Field.ToUpperPascal()}{SetInsertDefaultValue(item.Field, item.CSharpType, item.IsNotNull)}, {item.Length}{item.PgDbTypeString}){end}");

				if (item.DbType == "geometry")
				{
					//writer.WriteLine($"\t\t\t{valuename}.Set(\"{item.Field}_point0\", $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\", -1);");
					//writer.WriteLine($"\t\t\t{valuename}.Set(\"{item.Field}_srid0\", model.{item.Field.ToUpperPascal()}_srid, -1);");
					writer.WriteLine($"\t\t\t\t.Set(\"{item.Field}\", \"ST_GeomFromText(@{item.Field}_point0, @{item.Field}_srid0)\",");
					writer.WriteLine($"\t\t\t\tnew List<NpgsqlParameter> {{ new NpgsqlParameter(\"{item.Field}_point0\", $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\"),new NpgsqlParameter(\"{item.Field}_srid0\", model.{item.Field.ToUpperPascal()}_srid) }}){end}");
				}
				i++;
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
			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where = string.Empty, types = string.Empty, pgStr = string.Empty;
				for (var i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					//NpgsqlDbType String
					pgStr += fs.PgDbTypeString.IsNullOrEmpty() ? "null" : fs.PgDbTypeString.TrimStart(' ', ',');

					if (i + 1 != pkList.Count)
					{
						types += ", ";
						pgStr += ", ";
					}
					d_key.Add(fs.RelType + " " + fs.Field);
					where += $".Where{fs.Field.ToUpperPascal()}({fs.Field})";
					//if (fs.CSharpType.Replace("?", "").ToLower() == "datetime")
					//	sbEx.AppendLine($"\t\tpublic {DalClassName} Where{fs.Field.ToUpperPascal()}({Types.GetWhereTypeFromDbType(fs.RelType, fs.IsNotNull)} {fs.Field}) => WhereOr(\"a.{fs.Field} = {{0}}\", {fs.Field}{fs.PgDbTypeString});");

				}
				writer.WriteLine($"\t\tpublic static {ModelClassName} GetItem({string.Join(", ", d_key)}) => Select{where}.ToOne();");
				foreach (var u in fieldList.Where(f => f.IsUnique == true))
				{
					writer.WriteLine($"\t\tpublic static {ModelClassName} GetItemBy{u.Field.ToUpperPascal()}({u.RelType.Replace("?", "")} {u.Field}) => Select.Where{u.Field.ToUpperPascal()}({u.Field}).ToOne();");
				}


				if (pkList.Count == 1)
					writer.WriteLine($"\t\tpublic static List<{ModelClassName}> GetItems(IEnumerable<{types}> {s_key[0]}) => Select.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}{fieldList.FirstOrDefault(f => f.Field == pkList[0].Field).PgDbTypeString}).ToList();");
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static List<{ModelClassName}> GetItems(IEnumerable<({types})> val) => Select.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val, new NpgsqlDbType?[]{{ {pgStr} }}).ToList();");
				}

			}
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				if (item.DataType == "c") continue;
				if (item.Dimensions == 0)
				{
					string[] notCreateDbType = { "xml" };
					if (!notCreateDbType.Contains(item.DbType))
					{
						if (Types.MakeWhereOrExceptType(item.CSharpType) || item.DbType == "date")
						{
							writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}(params {item.RelType}[] {item.Field}) => WhereOr($\"{{MainAlias}}.{item.Field} = {{{{0}}}}\", {item.Field}{item.PgDbTypeString});");
							if (item.RelType.Contains("?"))
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}(params {item.RelType.Replace("?", "")}[] {item.Field}) => WhereOr($\"{{MainAlias}}.{item.Field} = {{{{0}}}}\", {item.Field}{item.PgDbTypeString});");
						}
						switch (item.CSharpType.ToLower())
						{
							case "string":
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Like(params {item.RelType}[] {item.Field}) => WhereOr($\"{{MainAlias}}.{item.Field} LIKE {{{{0}}}}\", {item.Field}.Select(a => $\"%{{a}}%\"){item.PgDbTypeString});");
								break;
							case "datetime":
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Range(DateTime? begin = null, DateTime? end = null) => Where($\"{{MainAlias}}.{item.Field} BETWEEN {{{{0}}}} AND {{{{1}}}}\", begin ?? DateTime.Parse(\"1970-1-1\"), end ?? DateTime.Now);");
								break;
							case "timespan":
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Range(TimeSpan? begin = null, TimeSpan? end = null) => Where($\"{{MainAlias}}.{item.Field} BETWEEN {{{{0}}}} AND {{{{1}}}}\", begin ?? TimeSpan.MinValue, end ?? TimeSpan.MaxValue);");
								break;
							case "int":
							case "short":
							case "long":
							case "decimal":
							case "float":
							case "double":
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Than({item.CSharpType} val, string sqlOperator = \">\") => Where($\"{{MainAlias}}.{item.Field} {{sqlOperator}} {{{{0}}}}\", new DbTypeValue(val{item.PgDbTypeString}));");
								break;
							case "byte[]":
								writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({item.CSharpType} {item.Field}) => WhereArray(\"{{MainAlias}}.{item.Field} = {{{{0}}}}\", {item.Field}{item.PgDbTypeString});");
								break;
							default: break;
						}
					}
				}
				else if (item.Dimensions == 1)
				{
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({item.RelType} {item.Field}) => WhereArray($\"{{MainAlias}}.{item.Field} = {{{{0}}}}\", {item.Field}{item.PgDbTypeString});");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Any(params {item.RelType} {item.Field}) => WhereOr($\"array_position({{MainAlias}}.{item.Field}, {{{{0}}}}) > 0\", {item.Field}{item.PgDbTypeString.Replace("NpgsqlDbType.Array", "").Replace("|", "").TrimEnd(',', ' ')});");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Length(int len, string sqlOperator = \"=\") => Where($\"array_length({{MainAlias}}.{item.Field}, 1) {{sqlOperator}} {{{{0}}}}\", len);");
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

			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where1 = string.Empty, where2 = string.Empty, types = string.Empty, pgStr = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $"model.{fs.Field.ToUpperPascal()}";
					where2 += $"{fs.Field}";
					pgStr += fs.PgDbTypeString.IsNullOrEmpty() ? "null" : fs.PgDbTypeString.TrimStart(' ', ',');
					if (i + 1 != pkList.Count)
					{
						types += ", "; where1 += ", "; where2 += ", ";
						pgStr += ", ";
					}
				}
				where1 = where1.Contains(",") ? $"({where1})" : where1;
				where2 = where2.Contains(",") ? $"({where2})" : where2;
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({ModelClassName} model) => Update(new[] {{ {where1} }});");
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({string.Join(",", d_key)}) => Update(new[] {{ {where2} }});");
				if (pkList.Count == 1)
				{
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{ModelSuffix}> models) => Update(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{types}> {s_key[0]}s) => UpdateDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}s{fieldList.FirstOrDefault(f => f.Field == pkList[0].Field).PgDbTypeString});");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{ModelSuffix}> models) => Update(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<({types})> val) => UpdateDiy.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val, new NpgsqlDbType?[]{{ {pgStr} }});");
				}
			}

			writer.WriteLine("\t\tpublic class {0}UpdateBuilder : UpdateBuilder<{0}UpdateBuilder, {1}>", DalClassName, ModelClassName);
			writer.WriteLine("\t\t{");
			if (_isGeometryTable)
				writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder() => Fields = _field;");
			// set
			foreach (var item in fieldList)
			{
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine("\t\t\tpublic {0}UpdateBuilder Set{1}({2} {3}) => Set(\"{3}\", {3}, {4}{5});", DalClassName, item.Field.ToUpperPascal(), item.RelType, item.Field, item.Length, item.PgDbTypeString);
				if (item.Dimensions == 0)
				{

					switch (item.CSharpType.ToLower())
					{
						case "int":
						case "short":
						case "decimal":
						case "float":
						case "double":
						case "long":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment({item.CSharpType} {item.Field}) => SetIncrement(\"{item.Field}\", {item.Field}, {item.Length}{item.PgDbTypeString});");
							break;
						case "datetime":
						case "timespan":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment(TimeSpan timeSpan) => SetIncrement(\"{item.Field}\", timeSpan, {item.Length}{item.PgDbTypeString});");
							//writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Minus(TimeSpan timeSpan) => SetDateTime(\"{item.Field}\", timeSpan, false, {item.Length});");
							break;
						case "geometry":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}(float x, float y, int SRID = 4326) => SetGeometry(\"{item.Field}\", x, y, SRID);");
							//writer.WriteLine("\t\t\t{");
							//writer.WriteLine($"\t\t\t\tAddParameter(\"point\", $\"POINT({{x}} {{y}})\", -1);");
							//writer.WriteLine($"\t\t\t\tAddParameter(\"srid\", SRID, -1);");
							//writer.WriteLine($"\t\t\t\t_setList.Add(\"{item.Field} = ST_GeomFromText(@point,@srid)\");");
							//writer.WriteLine($"\t\t\t\treturn this;");
							//writer.WriteLine("\t\t\t}");
							break;
						default: break;
					}
				}
				else if (item.Dimensions == 1)
				{
					//join
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Join(params {item.RelType} {item.Field}) => SetJoin(\"{item.Field}\", {item.Field}, {item.Length}{item.PgDbTypeString});");

					//remove
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Remove({item.RelType.Replace("[]", "")} {item.Field}) => SetRemove(\"{item.Field}\", {item.Field}, {item.Length}{item.PgDbTypeString.Replace("NpgsqlDbType.Array", "").Replace("|", "").TrimEnd(',', ' ')});");
				}
				else if (item.Dimensions > 1)
				{

				}
			}
			writer.WriteLine("\t\t}");

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
		public static string WritePropertyGetSet(string cSharpType, string field)
		{
			string[] NotSet = { "x", "y" };
			string[] DefaultValue = { "SRID", "4326" };
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
			//if (isNotNull && field != "id") return "";
			//if (cSharpType == "JToken") return " ?? JToken.Parse(\"{}\") ";
			//if (field == "create_time" && cSharpType == "DateTime") return " ?? DateTime.Now";
			//if (field == "update_time" && cSharpType == "DateTime") return " ?? DateTime.Now";
			//if (field == "id" && cSharpType == "Guid") return " == Guid.Empty ? Guid.NewGuid() : model.Id";//" ?? Guid.NewGuid()";
			switch (field)
			{
				case string f when f == "id" && cSharpType == "Guid" && isNotNull:
					return " == Guid.Empty ? Guid.NewGuid() : model.Id";
				case string f when (f == "create_time" || f == "update_time") && cSharpType == "DateTime" && isNotNull:
					return $".Ticks == 0 ? DateTime.Now : model.{f.ToUpperPascal()}";
				case string f when (f == "create_time" || f == "update_time") && cSharpType == "DateTime" && !isNotNull:
					return " ?? DateTime.Now";
				case string f when cSharpType == "JToken":
					return " ?? JToken.Parse(\"{}\")";
				default: return "";
			}
		}
		#endregion
	}
}
