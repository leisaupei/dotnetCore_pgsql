using DBHelper;
using Newtonsoft.Json;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Common.CodeFactory.DAL
{
	public class TablesDal
	{
		readonly string _projectName;
		readonly string _modelPath;
		readonly string _dalPath;
		string _schemaName;
		TableViewModel _table;
		bool _isGeometryTable = false;
		public List<FieldInfo> fieldList = new List<FieldInfo>();
		public List<PrimarykeyInfo> pkList = new List<PrimarykeyInfo>();
		public List<ConstraintMoreToOne> consListMoreToOne = new List<ConstraintMoreToOne>();
		public List<ConstraintOneToMore> consListOneToMore = new List<ConstraintOneToMore>();
		public List<ConstraintMoreToMore> consListMoreToMore = new List<ConstraintMoreToMore>();
		public TablesDal(string projectName, string modelPath, string dalPath, string schemaName, TableViewModel table)
		{

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
			GetFieldList();
		}

		#region Get
		public void GetFieldList()
		{
			//string sqlText = $@"
			//	SELECT
			//		A .oid,
			//		C .attnum AS num,
			//		C .attname AS field,
			//		(
			//			CASE
			//			WHEN f.character_maximum_length IS NULL THEN
			//				C .attlen
			//			ELSE
			//				f.character_maximum_length
			//			END
			//		) AS LENGTH,
			//		C .attnotnull AS NOTNULL,
			//		d.description AS COMMENT,
			//		(
			//			CASE
			//			WHEN e.typelem = 0 THEN
			//				e.typname
			//			WHEN e.typcategory = 'G' THEN
			//				format_type (C .atttypid, C .atttypmod)
			//			ELSE
			//				e2.typname
			//			END
			//		) AS TYPE,
			//		format_type (C .atttypid, C .atttypmod) AS type_comment,
			//		(
			//			CASE
			//			WHEN e.typelem = 0 THEN
			//				e.typtype
			//			ELSE
			//				e2.typtype
			//			END
			//		) AS data_type,
			//		e.typcategory,
			//		f.is_identity
			//	FROM
			//		pg_class A
			//	INNER JOIN pg_namespace b ON A .relnamespace = b.oid
			//	INNER JOIN pg_attribute C ON attrelid = A .oid
			//	LEFT OUTER JOIN pg_description d ON C.attrelid = d.objoid AND C .attnum = d.objsubid AND C .attnum > 0
			//	INNER JOIN pg_type e ON e.oid = C .atttypid
			//	LEFT JOIN pg_type e2 ON e2.oid = e.typelem
			//	INNER JOIN information_schema. COLUMNS f ON f.table_schema = b.nspname AND f. TABLE_NAME = A .relname AND COLUMN_NAME = C .attname
			//	WHERE
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
			fieldList = SQL.Select(@"a.oid,c.attnum as num,c.attname AS field,c.attnotnull AS isnotnull,d.description AS comment,e.typcategory,
				(f.is_identity = 'YES') as isidentity,format_type(c.atttypid,c.atttypmod) AS type_comment,
				(CASE WHEN f.character_maximum_length IS NULL THEN	c.attlen ELSE f.character_maximum_length END) AS length,
				(CASE WHEN e.typelem = 0 THEN e.typname WHEN e.typcategory = 'G' THEN format_type (c.atttypid, c.atttypmod) ELSE e2.typname END ) AS dbtype,
				(CASE WHEN e.typelem = 0 THEN e.typtype ELSE e2.typtype END) AS datatype").From("pg_class")
			   .InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
			   .InnerJoin("pg_attribute", "c", "attrelid = a.oid")
			   .Join(UnionEnum.LEFT_OUTER_JOIN, "pg_description", "d", "c.attrelid = d.objoid AND c.attnum = d.objsubid AND c.attnum > 0")
			   .InnerJoin("pg_type", "e", "e.oid = c.atttypid")
			   .LeftJoin("pg_type", "e2", "e2.oid = e.typelem")
			   .InnerJoin("information_schema.COLUMNS", "f", "f.table_schema = b.nspname AND f.TABLE_NAME = a.relname AND COLUMN_NAME = c.attname")
			   .Where($"b.nspname='{_schemaName}' and a.relname='{_table.Name}'").ToList<FieldInfo>(fi =>
			   {
				   fi.IsArray = fi.Typcategory == "A";
				   fi.DbType = fi.DbType.StartsWith("_") ? fi.DbType.Remove(0, 1) : fi.DbType;
				   fi.PgDbType = Types.ConvertDbTypeToNpgsqlDbTypeEnum(fi.DataType, fi.DbType);
				   fi.IsEnum = fi.DataType == "e";
				   string _type = Types.ConvertPgDbTypeToCSharpType(fi.DbType);
				   if (fi.IsEnum) _type = _type.ToUpperPascal();
				   string _notnull = "";
				   if (_type != "string" && _type != "JToken" && _type != "byte[]" && !fi.IsArray && _type != "object")
					   _notnull = fi.IsNotNull ? "" : "?";
				   string _array = fi.IsArray ? "[]" : "";
				   fi.RelType = $"{_type}{_notnull}{_array}";
				   return fi;
			   });
		}
		public void GetConstraint()
		{
			//多对一关系
			//string sqlText_MoreToOne = $@"
			//	SELECT
			//		(
			//			SELECT
			//				attname
			//			FROM
			//				pg_attribute
			//			WHERE
			//				attrelid = A .conrelid AND attnum = ANY (A .conkey)
			//		) AS conname,
			//		b.relname AS tablename,
			//		C .nspname,
			//		d.attname AS refcolumn,
			//		e.typname AS contype
			//	FROM
			//		pg_constraint A
			//	LEFT JOIN pg_class b ON b.oid = A .confrelid
			//	INNER JOIN pg_namespace C ON b.relnamespace = C .oid
			//	INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
			//	INNER JOIN pg_type e ON e.oid = d.atttypid
			//	WHERE
			//		conrelid IN (
			//			SELECT
			//				A .oid
			//			FROM
			//				pg_class A
			//			INNER JOIN pg_namespace b ON A .relnamespace = b.oid
			//			WHERE
			//				b.nspname = '{_schemaName}' AND A .relname = '{_table.Name}'
			//		);
			//";
			//consListMoreToOne = PgSqlHelper.ExecuteDataReaderList<ConstraintMoreToOne>(sqlText_MoreToOne);
			consListMoreToOne = SQL.Select($@"({SQL.Select("attname").From("pg_attribute", "").Where("attrelid = a.conrelid AND attnum = ANY(A.conkey)")}) AS conname,
				b.relname AS tablename,c.nspname,d.attname AS refcolumn,e.typname AS contype").From("pg_constraint")
				.LeftJoin("pg_class", "b", "b.oid = a.confrelid")
				.InnerJoin("pg_namespace", "c", "b.relnamespace = c.oid")
				.InnerJoin("pg_attribute", "d", "d.attrelid = a.confrelid AND d.attnum = ANY (a.confkey)")
				.InnerJoin("pg_type", "e", "e.oid = d.atttypid")
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
						.Where($"a.indrelid = '{item.Nspname}.{item.TableName}' :: regclass AND a.indisprimary")
						.ToList<PrimarykeyInfo>(f =>
						{
							if (f.Field.ToNullOrString() == item.RefColumn) return null;
							else return f;
						});
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
							var moretoone = SQL.Select($@"({SQL.Select("attname").From("pg_attribute", "").Where("attrelid = a.conrelid AND attnum = ANY(a.conkey)")}) AS conname,
								b.relname AS TABLE_NAME,c.nspname,d.attname AS ref_column,e.typname AS contype").From("pg_constraint").LeftJoin("pg_class", "b", "b.oid = a.confrelid")
								.InnerJoin("pg_namespace", "c", "b.relnamespace = c.oid")
								.InnerJoin("pg_attribute", "d", "d.attrelid = a.confrelid AND d.attnum = ANY (a.confkey)")
								.InnerJoin("pg_type", "e", "e.oid = d.atttypid")
								.WhereIn($"conrelid", SQL.Select("a.oid").From("pg_class").InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
									.Where($"b.nspname = '{_schemaName}' AND A .relname = '{_table.Name}'"))
								.ToList<ConstraintMoreToOne>(f =>
								{
									if (f.Conname != p.Field) return null;
									else return f;
								});
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
		public void GetPrimaryKey()
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
		#endregion
		string DeletePublic(string schemaName, string tableName, bool isTableName = false)
		{
			if (isTableName)
				return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToLower() + "." + tableName;

			return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToUpperPascal() + "_" + tableName;
		}
		readonly string _modelName = "Model";
		string ModelClassName => DalClassName + _modelName;
		string DalClassName => $"{DeletePublic(_schemaName, _table.Name)}";
		string TableName => $"{DeletePublic(_schemaName, _table.Name, true).ToLowerPascal()}";
		/// <summary>
		/// 生成Model.cs文件
		/// </summary>
		public void Generate()
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
						if (!string.IsNullOrEmpty(item.Comment))
						{
							writer.WriteLine("\t\t/// <summary>");
							writer.WriteLine($"\t\t/// {item.Comment}");
							writer.WriteLine("\t\t/// </summary>");
						}

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
							if (!string.IsNullOrEmpty(item.Comment))
							{
								writer.WriteLine("\t\t/// <summary>");
								writer.WriteLine($"\t\t/// {field.Comment}");
								writer.WriteLine("\t\t/// </summary>");
							}
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
					writer.WriteLine("\t\t#region Foreign Key");
					Hashtable ht = new Hashtable();
					foreach (var item in consListMoreToOne)
					{
						string tablename = DeletePublic(item.Nspname, item.TableName);
						string propertyName = $"Obj_{tablename.ToLowerPascal()}";

						if (ht.ContainsKey(propertyName))
							propertyName += "By" + item.Conname;

						string tmp_var = $"_{propertyName.ToLowerPascal()}";

						writer.WriteLine();
						writer.WriteLine($"\t\tprivate {tablename}{_modelName} {tmp_var} = null;");
						writer.WriteLine($"\t\t[ForeignKeyProperty]");
						writer.WriteLine($"\t\tpublic {tablename}{_modelName} {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.GetItem({DotValueHelper(item.Conname, fieldList)});");

						ht.Add(propertyName, "");
					}
					foreach (var item in consListOneToMore)
					{
						string tablename = DeletePublic(item.Nspname, item.TableName);
						string propertyName = $"Obj_{tablename.ToLowerPascal()}s";
						if (consListOneToMore.Count(a => a.Nspname == item.Nspname && a.TableName == item.TableName && a.IsOneToOne == item.IsOneToOne) > 1) propertyName = propertyName + $"_by{ item.RefColumn.ToLowerPascal()/*.Split('_')[0]*/}";
						string tmp_var = $"_{propertyName.ToLowerPascal()}";
						if (item.IsOneToOne)
						{
							writer.WriteLine();
							tmp_var = tmp_var.Substring(0, tmp_var.Length - 1);
							propertyName = propertyName.Substring(0, propertyName.Length - 1);
							writer.WriteLine($"\t\tprivate {tablename}{_modelName} {tmp_var} = null;");
							writer.WriteLine($"\t\t[ForeignKeyProperty]");
							writer.WriteLine($"\t\tpublic {tablename}{_modelName} {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.GetItem({DotValueHelper(item.Conname, fieldList)});");
						}
						else
						{
							//	writer.WriteLine($"\t\tprivate List<{tablename}{_modelName}> {tmp_var} = null;");
							//	writer.WriteLine($"\t\t[ForeignKeyProperty]");
							//	writer.WriteLine($"\t\tpublic List<{tablename}{_modelName}> {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.Select.Where{item.RefColumn.ToUpperPascal()}({item.Conname.ToUpperPascal()}).ToList();");
							//writer.WriteLine($"\t\tpublic List<{tablename}{_modelName}> {propertyName}(int index, int size) =>  {tablename}.Select.Where{item.RefColumn.ToUpperPascal()}({item.Conname.ToUpperPascal()}).Page(index, size).ToList();");
						}
					}
					//foreach (var item in consListMoreToMore)
					//{
					//	string minorTableName = DeletePublic(item.MinorNspname, item.MinorTable);
					//	string propertyName = $"Uni_{minorTableName.ToLowerPascal()}s";
					//	string centerTableName = DeletePublic(item.CenterNspname, item.CenterTable);
					//	string mianTableName = DeletePublic(item.MainNspname, item.MainTable);
					//	if (consListMoreToMore.Count(a => a.MinorTable == item.MinorTable) > 1)
					//		propertyName = propertyName + $"_by{centerTableName}{ item.CenterMainField.ToLowerPascal().Split('_')[0]}";

					//	string tmp_var = $"_{propertyName.ToLowerPascal()}";

					//	writer.WriteLine();
					//	writer.WriteLine($"\t\tprivate List<{minorTableName}{_modelName}> {tmp_var} = null;");
					//	writer.WriteLine("\t\t[ForeignKeyProperty]");
					//	writer.WriteLine($"\t\tpublic List<{minorTableName}{_modelName}> {propertyName} => {tmp_var} = {tmp_var} ?? {minorTableName}.Select.InnerJoin<{centerTableName}>(\"b\", \"b.{item.CenterMinorField} = a.{item.MinorField}\").InnerJoin<{mianTableName}>(\"c\", \"c.{item.MainField} = b.{item.CenterMainField}\").Where(\"c.{item.MainField} = {{0}}\", {item.MainField.ToUpperPascal()}).ToList();");
					//}
					writer.WriteLine("\t\t#endregion");
					writer.WriteLine();
					writer.WriteLine("\t\t#region Update/Insert");
					if (pkList.Count > 0)
						writer.WriteLine($"\t\t[MethodProperty] public {DalClassName}.{DalClassName}UpdateBuilder Update => DAL.{DalClassName}.Update(this);");
					writer.WriteLine();
					writer.WriteLine($"\t\tpublic {ModelClassName} Insert() => DAL.{DalClassName}.Insert(this);");
					writer.WriteLine("\t\t#endregion");
				}
				writer.WriteLine("");
				writer.WriteLine("\t\tpublic override string ToString() => JsonConvert.SerializeObject(this);");
				writer.WriteLine($"\t\tpublic static {ModelClassName} Parse(string json) => json.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<{ModelClassName}>(json);");
				writer.WriteLine("\t}");
				writer.WriteLine("}");

				writer.Flush();

				CreateDal();
			}
		}
		/// <summary>
		/// 生成DAL.cs文件
		/// </summary>
		private void CreateDal()
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
				if (_isGeometryTable)
					writer.WriteLine("using Npgsql;");
				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.DAL");
				writer.WriteLine("{");
				writer.WriteLine($"\t[Mapping(\"{TableName}\")]");
				writer.WriteLine($"\tpublic class {DalClassName} : SelectExchange<{DalClassName}, {ModelClassName}>");
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

		#region Dal Property
		private void PropertiesGenerator(StreamWriter writer)
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
					sb_query.Append($"ST_X(a.{item.Field}) as {item.Field}_x, ST_Y(a.{item.Field}) as {item.Field}_y, ST_SRID(a.{item.Field}) as {item.Field}_srid");
					sb_field.Append($"{item.Field}");
					sb_param.Append($"ST_GeomFromText(@{item.Field}_point0, @{item.Field}_srid0)");
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
				writer.WriteLine($"\t\tpublic {DalClassName}() => _fields = _field;");
			}
			writer.WriteLine($"\t\tpublic static {DalClassName} Select => new {DalClassName}();");

			if (_table.Type == "table")
			{
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder UpdateDiy => new {DalClassName}UpdateBuilder();");
				writer.WriteLine($"\t\tpublic static DeleteBuilder DeleteDiy => new DeleteBuilder(\"{TableName}\");");

				writer.WriteLine($"\t\tpublic static InsertBuilder InsertDiy => new InsertBuilder(\"{TableName}\"{(_isGeometryTable ? ", _field" : "")});");
			}
		}
		#endregion

		#region Delete
		private void DeleteGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{

				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where = string.Empty, where1 = string.Empty, types = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $"model.{fs.Field.ToUpperPascal()}";
					where += $"{fs.Field}";
					if (i + 1 != pkList.Count)
					{
						types += ", "; where1 += ", "; where += ", ";
					}
				}
				where1 = where1.Contains(",") ? $"({where1})" : where1;
				where = where.Contains(",") ? $"({where})" : where;
				writer.WriteLine($"\t\tpublic static int Delete({ModelClassName} model) => Delete(new[] {{ {where1} }});");
				writer.WriteLine($"\t\tpublic static int Delete({string.Join(", ", d_key)}) => Delete(new[] {{ {where} }});");
				if (pkList.Count == 1)
				{
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{_modelName}> models) => Delete(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{types}> {s_key[0]}) => DeleteDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}).Commit();");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{_modelName}> models) =>  Delete(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<({types})> val) => DeleteDiy.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val).Commit();");
				}
			}
		}
		#endregion

		#region Insert
		private void InsertGenerator(StreamWriter writer)
		{
			var valuename = DalClassName.ToLowerPascal();
			writer.WriteLine($"\t\tpublic static {ModelClassName} Insert({ModelClassName} model)");
			writer.WriteLine("\t\t{");
			writer.WriteLine($"\t\t\tInsertBuilder {valuename} = InsertDiy;");
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				string cSharpType = Types.ConvertPgDbTypeToCSharpType(item.DbType);
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\t{valuename}.Set(\"{item.Field}\", model.{item.Field.ToUpperPascal()}{SetInsertDefaultValue(item.Field, cSharpType, item.IsNotNull)}, {item.Length});");

				if (item.DbType == "geometry")
				{
					//writer.WriteLine($"\t\t\t{valuename}.Set(\"{item.Field}_point0\", $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\", -1);");
					//writer.WriteLine($"\t\t\t{valuename}.Set(\"{item.Field}_srid0\", model.{item.Field.ToUpperPascal()}_srid, -1);");
					writer.WriteLine($"\t\t\tmyvcard_swap_vcard.Set(\"{item.Field}\", \"ST_GeomFromText(@{item.Field}_point0, @{item.Field}_srid0)\",");
					writer.WriteLine($"\t\t\t\tnew List<NpgsqlParameter> {{ new NpgsqlParameter(\"{item.Field}_point0\", $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\"),new NpgsqlParameter(\"{item.Field}_srid0\", model.{item.Field.ToUpperPascal()}_srid) }});");
				}
			}
			writer.WriteLine($"\t\t\treturn {valuename}.Commit<{ModelClassName}>();");
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Select
		private void SelectGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where = string.Empty, types = string.Empty; ;
				for (var i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					if (i + 1 != pkList.Count)
						types += ", ";
					d_key.Add(fs.RelType + " " + fs.Field);
					where += $".Where{fs.Field.ToUpperPascal()}({fs.Field})";
				}
				writer.WriteLine($"\t\tpublic static {ModelClassName} GetItem({string.Join(",", d_key)}) => Select{where}.ToOne();");

				if (pkList.Count == 1)
					writer.WriteLine($"\t\tpublic static List<{ModelClassName}> GetItems(IEnumerable<{types}> {s_key[0]}) => Select.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}).ToList();");
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static List<{ModelClassName}> GetItems(IEnumerable<({types})> val) => Select.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val).ToList();");
				}
			}
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				string cSharpType = Types.ConvertPgDbTypeToCSharpType(item.RelType).Replace("?", "");
				if (Types.MakeWhereOrExceptType(cSharpType))
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} = {{0}}\", {item.Field});");
				switch (cSharpType.ToLower())
				{
					case "string":
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Like({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} LIKE {{{0}}}\", {item.Field}.Select(a => $\"%{{a}}%\"));");
						break;
					case "datetime":
						//writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Before(DateTime dt, bool isEquals = false) => Where($\"a.{item.Field} <{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", dt);");
						//writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}After(DateTime dt, bool isEquals = false) => Where($\"a.{item.Field} >{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", dt);");
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Range(DateTime dt1, DateTime dt2) => Where(\"a.{item.Field} BETWEEN {{0}} AND {{1}}\", dt1, dt2);");
						break;
					case "int":
					case "short":
					case "long":
					case "decimal":
					case "float":
					case "double":
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Than({cSharpType} val, bool isGreater, bool isEquals = false) => Where($\"a.{item.Field} {{(isGreater ? \">\" : \"<\")}}{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", val);");
						break;
					default: break;
				}
				if (item.IsArray)
				{
					// mark: Db_type有待确认
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Any({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} @> array[{{0}}::{item.DbType}]\", {item.Field});");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Null(bool isNull = true) => Where($\"array_length(a.{item.Field}, 1) {{(isNull ? \"=\" : \"<>\")}} {{{{0}}}}\", null);");
				}
			}
		}
		#endregion

		#region Update
		private void UpdateGenerator(StreamWriter writer)
		{

			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>(), s_key = new List<string>();
				string where1 = string.Empty, where2 = string.Empty, types = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					s_key.Add(fs.Field);
					types += fs.RelType;
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $"model.{fs.Field.ToUpperPascal()}";
					where2 += $"{fs.Field}";
					if (i + 1 != pkList.Count)
					{
						types += ", "; where1 += ", "; where2 += ", ";
					}
				}
				where1 = where1.Contains(",") ? $"({where1})" : where1;
				where2 = where2.Contains(",") ? $"({where2})" : where2;
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({ModelClassName} model) => Update(new[] {{ {where1} }});");
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({string.Join(",", d_key)}) => Update(new[] {{ {where2} }});");
				if (pkList.Count == 1)
				{
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{_modelName}> models) => Update(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{types}> {s_key[0]}s) => UpdateDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}s);");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{_modelName}> models) => Update(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
					writer.WriteLine($"\t\t/// <summary>");
					writer.WriteLine($"\t\t/// ({s_key.Select(a => $"{a}").Join(", ")})");
					writer.WriteLine($"\t\t/// </summary>");
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<({types})> val) => UpdateDiy.Where(new[] {{ {s_key.Select(a => $"\"{a}\"").Join(", ")} }}, val);");
				}
			}

			writer.WriteLine($"\t\tpublic class {DalClassName}UpdateBuilder : UpdateExchange<{DalClassName}UpdateBuilder, {ModelClassName}>");
			writer.WriteLine("\t\t{");
			if (_isGeometryTable)
				writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder() => _fields = _field;");
			// set
			foreach (var item in fieldList)
			{
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}({item.RelType} {item.Field}) => Set(\"{item.Field}\", {item.Field}, {item.Length});");
				string cSharpType = Types.ConvertPgDbTypeToCSharpType(item.DbType);

				if (item.IsArray)
				{
					//join
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Join(params {cSharpType}[] {item.Field}) => SetJoin(\"{item.Field}\", {item.Field}, {item.Length});");

					//remove
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Remove({cSharpType} {item.Field}) => SetRemove(\"{item.Field}\", {item.Field}, {item.Length});");
				}
				else
				{
					switch (cSharpType.ToLower())
					{
						case "int":
						case "short":
						case "decimal":
						case "float":
						case "double":
						case "long":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment({cSharpType} {item.Field}) => SetIncrement(\"{item.Field}\", {item.Field}, {item.Length});");
							break;
						case "datetime":
						case "timespan":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment(TimeSpan timeSpan) => SetIncrement(\"{item.Field}\", timeSpan, {item.Length});");
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
			}
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Private Method
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
		public static string SetInsertDefaultValue(string field, string cSharpType, bool isNotNull)
		{
			if (!isNotNull)
			{
				if (cSharpType == "JToken") return " ?? JToken.Parse(\"{}\") ";
				switch (field)
				{
					case "create_time":
						if (cSharpType == "DateTime") return " ?? DateTime.Now";
						break;
					case "id":
						if (cSharpType == "Guid") return " ?? Guid.NewGuid()";
						break;
				}
			}
			return "";
		}
		#endregion
	}
}
