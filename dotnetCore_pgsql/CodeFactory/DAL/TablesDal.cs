using DBHelper;
using Newtonsoft.Json;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace DBHelper.CodeFactory.DAL
{
	public class TablesDal
	{

		private string _projectName;
		private string _modelPath;
		private string _dalPath;
		private string _schemaName;
		private TableViewModel _table;

		public List<FieldInfo> fieldList = new List<FieldInfo>();
		public List<PrimarykeyInfo> pkList = new List<PrimarykeyInfo>();
		public List<ConstraintMoreToOne> consListMoreToOne = new List<ConstraintMoreToOne>();
		public List<ConstraintOneToMore> consListOneToMore = new List<ConstraintOneToMore>();
		public List<ConstraintMoreToMore> consListMoreToMore = new List<ConstraintMoreToMore>();
		public TablesDal(string projectName, string modelPath, string dalPath, string schemaName, TableViewModel table)
		{
			this._projectName = projectName;
			this._modelPath = modelPath;
			this._dalPath = dalPath;
			this._schemaName = schemaName;
			this._table = table;
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
			string sqlText = $@"
				SELECT
					A .oid,
					C .attnum AS num,
					C .attname AS field,
					(
						CASE
						WHEN f.character_maximum_length IS NULL THEN
							C .attlen
						ELSE
							f.character_maximum_length
						END
					) AS LENGTH,
					C .attnotnull AS NOTNULL,
					d.description AS COMMENT,
					(
						CASE
						WHEN e.typelem = 0 THEN
							e.typname
						WHEN e.typcategory = 'G' THEN
							format_type (C .atttypid, C .atttypmod)
						ELSE
							e2.typname
						END
					) AS TYPE,
					format_type (C .atttypid, C .atttypmod) AS type_comment,
					(
						CASE
						WHEN e.typelem = 0 THEN
							e.typtype
						ELSE
							e2.typtype
						END
					) AS data_type,
					e.typcategory,
					f.is_identity
				FROM
					pg_class A
				INNER JOIN pg_namespace b ON A .relnamespace = b.oid
				INNER JOIN pg_attribute C ON attrelid = A .oid
				LEFT OUTER JOIN pg_description d ON C.attrelid = d.objoid AND C .attnum = d.objsubid AND C .attnum > 0
				INNER JOIN pg_type e ON e.oid = C .atttypid
				LEFT JOIN pg_type e2 ON e2.oid = e.typelem
				INNER JOIN information_schema. COLUMNS f ON f.table_schema = b.nspname AND f. TABLE_NAME = A .relname AND COLUMN_NAME = C .attname
				WHERE
				  b.nspname='{_schemaName}' and a.relname='{_table.Name}';
			";
			PgSqlHelper.ExecuteDataReader(dr =>
		 {
			 FieldInfo fi = new FieldInfo();
			 fi.Oid = Convert.ToInt32(dr["oid"]);
			 fi.Field = dr["field"].ToString();
			 fi.Length = Convert.ToInt32(dr["length"].ToString());
			 fi.IsNotNull = Convert.ToBoolean(dr["notnull"]);
			 fi.Comment = dr["comment"].ToNullOrString();
			 fi.DataType = dr["data_type"].ToString();
			 fi.DbType = dr["type"].ToString();
			 fi.DbType = fi.DbType.StartsWith("_") ? fi.DbType.Remove(0, 1) : fi.DbType;
			 fi.PgDbType = TypeHelper.ConvertDbTypeToNpgsqlDbTypeEnum(fi.DataType, fi.DbType);
			 fi.IsIdentity = dr["is_identity"].ToString() == "YES";
			 fi.IsArray = dr["typcategory"].ToString() == "A";
			 fi.IsEnum = fi.DataType == "e";
			 fi.Typcategory = dr["typcategory"].ToString();
			 string _type = TypeHelper.ConvertPgDbTypeToCSharpType(fi.DbType);

			 if (fi.IsEnum) _type = _type.ToUpperPascal() + "ENUM";
			 string _notnull = "";
			 if (_type != "string" && _type != "JToken" && !fi.IsArray)
				 _notnull = fi.IsNotNull ? "" : "?";

			 string _array = fi.IsArray ? "[]" : "";
			 fi.RelType = $"{_type}{_notnull}{_array}";
			 // dal
			 this.fieldList.Add(fi);
		 }, sqlText);
		}

		public void GetConstraint()
		{
			//多对一关系
			string sqlText_MoreToOne = $@"
				SELECT
					(
						SELECT
							attname
						FROM
							pg_attribute
						WHERE
							attrelid = A .conrelid AND attnum = ANY (A .conkey)
					) AS conname,
					b.relname AS tablename,
					C .nspname,
					d.attname AS refcolumn,
					e.typname AS contype
				FROM
					pg_constraint A
				LEFT JOIN pg_class b ON b.oid = A .confrelid
				INNER JOIN pg_namespace C ON b.relnamespace = C .oid
				INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
				INNER JOIN pg_type e ON e.oid = d.atttypid
				WHERE
					conrelid IN (
						SELECT
							A .oid
						FROM
							pg_class A
						INNER JOIN pg_namespace b ON A .relnamespace = b.oid
						WHERE
							b.nspname = '{_schemaName}' AND A .relname = '{_table.Name}'
					);
			";
			consListMoreToOne = PgSqlHelper.ExecuteDataReaderList<ConstraintMoreToOne>(sqlText_MoreToOne);

			//一对多关系
			string sqlText_OneToMore = $@"
				SELECT DISTINCT 
					x. TABLE_NAME as tablename,
					x. COLUMN_NAME as refcolumn,
					x. CONSTRAINT_SCHEMA as nspname,
					tp.typname as contype,
					tp.attname as conname
				FROM
					information_schema.key_column_usage x
				INNER JOIN (
					SELECT
						T .relname,
						A .conname,
						f.typname,
						d.attname
					FROM
						pg_constraint A
					INNER JOIN pg_class T ON T .oid = A .conrelid
					INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
					INNER JOIN pg_type f ON f.oid = d.atttypid
					WHERE
						A .contype = 'f'
					AND A .confrelid = (
						SELECT
							e.oid
						FROM
							pg_class e
						INNER JOIN pg_namespace b ON e.relnamespace = b.oid
						WHERE
							b.nspname = '{_schemaName}' AND e.relname = '{_table.Name}'
					)
				) tp ON (
					x. TABLE_NAME = tp.relname AND x. CONSTRAINT_NAME = tp.conname
				)
			";
			consListOneToMore = PgSqlHelper.ExecuteDataReaderList<ConstraintOneToMore>(sqlText_OneToMore);

			//多对多关系
			if (consListOneToMore.Count > 0)
			{
				foreach (var item in consListOneToMore)
				{

					string union_table_sql = $@"
						SELECT
						    b.attname AS field,
						    format_type (b.atttypid, b.atttypmod) AS typename
						FROM
						    pg_index A
						INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
						AND b.attnum = ANY (A .indkey)
						WHERE
						    A .indrelid = '{item.Nspname}.{item.TableName}' :: regclass
						AND A .indisprimary                
					";
					List<PrimarykeyInfo> pk = new List<PrimarykeyInfo>();
					PgSqlHelper.ExecuteDataReader(dr =>
					{
						if (dr["field"]?.ToString() != item.RefColumn)
							pk.Add(new PrimarykeyInfo { Field = dr["field"].ToString(), TypeName = dr["typename"].ToString() });
					}, union_table_sql);
					if (pk.Count > 0)
					{
						foreach (var p in pk)
						{
							string sql1 = $@"
								SELECT
								    (
								        SELECT
								            attname
								        FROM
								            pg_attribute
								        WHERE
								            attrelid = A .conrelid AND attnum = ANY (A .conkey)
								    ) AS conname,
								    b.relname AS TABLE_NAME,
								    C .nspname,
								    d.attname AS ref_column,
								    e.typname AS contype
								FROM
								    pg_constraint A
								LEFT JOIN pg_class b ON b.oid = A .confrelid
								INNER JOIN pg_namespace C ON b.relnamespace = C .oid
								INNER JOIN pg_attribute d ON d.attrelid = A .confrelid AND d.attnum = ANY (A .confkey)
								INNER JOIN pg_type e ON e.oid = d.atttypid
								WHERE
								    conrelid IN (
								        SELECT
								            A .oid
								        FROM
								            pg_class A
								        INNER JOIN pg_namespace b ON A .relnamespace = b.oid
								        WHERE
								            b.nspname = '{item.Nspname}' AND A .relname = '{item.TableName}'
								    );                            
							";
							List<ConstraintMoreToOne> moretoone = new List<ConstraintMoreToOne>();
							PgSqlHelper.ExecuteDataReader(dr =>
							{
								if (dr["conname"]?.ToString() == p.Field)
								{
									moretoone.Add(new ConstraintMoreToOne
									{
										Conname = dr["conname"].ToString(),
										TableName = dr["table_name"].ToString(),
										RefColumn = dr["ref_column"].ToString(),
										Contype = dr["contype"].ToString(),
										Nspname = dr["nspname"].ToString()

									});
								}
							}, sql1);
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
			string sqlText = $@"
				SELECT
					b.attname AS field,
					format_type (b.atttypid, b.atttypmod) AS typename
				FROM
					pg_index A
				INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
				AND b.attnum = ANY (A .indkey)
				WHERE
					A .indrelid = '{_schemaName}.{_table.Name}' :: regclass
				AND A .indisprimary;
			";
			pkList = PgSqlHelper.ExecuteDataReaderList<PrimarykeyInfo>(sqlText);
		}
		#endregion


		private string DeletePublic(string schemaName, string tableName, bool isTableName = false)
		{
			if (isTableName)
				return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToLower() + "." + tableName;
			else
				return schemaName.ToLower() == "public" ? tableName.ToUpperPascal() : schemaName.ToUpperPascal() + "_" + tableName;
		}
		private string ModelClassName => DalClassName + "Model";
		private string DalClassName => $"{DeletePublic(_schemaName, _table.Name)}";
		private string TableName => $"{DeletePublic(_schemaName, _table.Name, true).ToLowerPascal()}";

		public void Generate()
		{
			string _filename = $"{_modelPath}/{ModelClassName}.cs";

			using (StreamWriter writer = new StreamWriter(File.Create(_filename)))
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
				writer.WriteLine($"\t[EntityMapping(TableName = \"{TableName}\"), JsonObject(MemberSerialization.OptIn)]");
				writer.WriteLine($"\tpublic partial class {ModelClassName}");
				writer.WriteLine("\t{");

				writer.WriteLine("\t\t#region Properties");
				foreach (var item in fieldList)
				{
					if (TypeHelper.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
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
						List<FieldInfo> str_field = new List<FieldInfo>
						{
							new FieldInfo { Comment = item.Field + "经度", Field = item.Field + "_y", RelType = "decimal" },
							new FieldInfo { Comment = item.Field + "纬度", Field = item.Field + "_x", RelType = "decimal" },
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
						writer.WriteLine($"\t\tprivate {tablename}Model {tmp_var} = null;");
						writer.WriteLine($"\t\t[ForeignKeyProperty]");
						writer.WriteLine($"\t\tpublic {tablename}Model {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.GetItem({DotValueHelper(item.Conname, fieldList)});");

						ht.Add(propertyName, "");
					}
					foreach (var item in consListOneToMore)
					{
						string tablename = DeletePublic(item.Nspname, item.TableName);
						string propertyName = $"Obj_{tablename.ToLowerPascal()}s";
						if (consListOneToMore.Count(a => a.Nspname == item.Nspname && a.TableName == item.TableName) > 1) propertyName = propertyName + $"_by{ item.RefColumn.ToLowerPascal().Split('_')[0]}";
						string tmp_var = $"_{propertyName.ToLowerPascal()}";
						writer.WriteLine();
						writer.WriteLine($"\t\tprivate List<{tablename}Model> {tmp_var} = null;");
						writer.WriteLine($"\t\t[ForeignKeyProperty]");
						writer.WriteLine($"\t\tpublic List<{tablename}Model> {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.Select.Where{item.RefColumn.ToUpperPascal()}({item.Conname.ToUpperPascal()}).ToList();");
					}
					foreach (var item in consListMoreToMore)
					{
						string minorTableName = DeletePublic(item.MinorNspname, item.MinorTable);
						string propertyName = $"Uni_{minorTableName.ToLowerPascal()}s";
						string centerTableName = DeletePublic(item.CenterNspname, item.CenterTable);
						string mianTableName = DeletePublic(item.MainNspname, item.MainTable);
						if (consListMoreToMore.Count(a => a.MinorTable == item.MinorTable) > 1)
							propertyName = propertyName + $"_by{centerTableName}{ item.CenterMainField.ToLowerPascal().Split('_')[0]}";

						string tmp_var = $"_{propertyName.ToLowerPascal()}";

						writer.WriteLine();
						writer.WriteLine($"\t\tprivate List<{minorTableName}Model> {tmp_var} = null;");
						writer.WriteLine("\t\t[ForeignKeyProperty]");
						writer.WriteLine($"\t\tpublic List<{minorTableName}Model> {propertyName} => {tmp_var} = {tmp_var} ?? {minorTableName}.Select.InnerJoin<{centerTableName}>(\"b\", \"b.{item.CenterMinorField} = a.{item.MinorField}\").InnerJoin<{mianTableName}>(\"c\", \"c.{item.MainField} = b.{item.CenterMainField}\").Where(\"c.{item.MainField} = {{0}}\", {item.MainField.ToUpperPascal()}).ToList();");
					}
					writer.WriteLine("\t\t#endregion");

					//List<string> d_Key = new List<string>();
					//foreach (var item in pkList)
					//{
					//    FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == item.Field);
					//    d_Key.Add("this." + fs.Field.ToUpperPascal());
					//}
					writer.WriteLine();
					writer.WriteLine("\t\t#region Update/Insert");
					if (pkList.Count > 0)
						writer.WriteLine($"\t\t[MethodProperty] public {DalClassName}.{DalClassName}UpdateBuilder Update => {DalClassName}.Update(this);");
					writer.WriteLine();
					writer.WriteLine($"\t\tpublic {ModelClassName} Insert() => {DalClassName}.Insert(this);");
					writer.WriteLine("\t\t#endregion");
				}
				writer.WriteLine("\t}");
				writer.WriteLine("}");

				writer.Flush();

				CreateDal();
			}
		}

		private void CreateDal()
		{
			string _filename = $"{_dalPath}/{DalClassName}.cs";

			using (StreamWriter writer = new StreamWriter(File.Create(_filename)))
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

				writer.WriteLine();
				writer.WriteLine($"namespace {_projectName}.DAL");
				writer.WriteLine("{");
				writer.WriteLine($"\t[EntityMapping(TableName = \"{TableName}\")]");
				writer.WriteLine($"\tpublic class {DalClassName} : Query<{ModelClassName}>");
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
			if (_table.Type == "table")
				writer.WriteLine($"\t\tconst string insertSqlText = \"INSERT INTO {TableName} ({sb_field.ToString()}) VALUES({sb_param}) RETURNING {sb_query.ToString().Replace("a.", "")}\";");
			writer.WriteLine($"\t\tconst string _field = \"{sb_query.ToString()}\";");
			writer.WriteLine($"\t\tpublic {DalClassName}() => Field = _field;");
			writer.WriteLine($"\t\tpublic static {DalClassName} Select => new {DalClassName}();");

			if (_table.Type == "table")
			{
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder UpdateDiy => new {DalClassName}UpdateBuilder();");
				writer.WriteLine($"\t\tpublic static DeleteBuilder<{ModelClassName}> DeleteDiy => new DeleteBuilder<{ModelClassName}>();");

			}
		}
		#endregion

		#region Delete
		private void DeleteGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{

				List<string> d_key = new List<string>();
				string where = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					d_key.Add(fs.RelType + " " + fs.Field);
					where += $".Where(\"{fs.Field} = {{0}}\", {fs.Field})";
				}

				writer.WriteLine($"\t\tpublic static int Delete({string.Join(",", d_key)}) => DeleteDiy{where}.Commit();");
			}
		}
		#endregion

		#region Insert
		private void InsertGenerator(StreamWriter writer)
		{
			var valuename = DalClassName.ToLowerPascal();
			writer.WriteLine($"\t\tpublic static {ModelClassName} Insert({ModelClassName} model)");
			writer.WriteLine("\t\t{");
			writer.WriteLine($"\t\t\t{DalClassName} {valuename} = Select;");
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				NpgsqlDbType _dbtype = TypeHelper.ConvertDbTypeToNpgsqlDbTypeEnum(item.DataType, item.DbType);
				string ap = item.IsArray ? " | NpgsqlDbType.Array" : "";
				string cSharpType = TypeHelper.ConvertPgDbTypeToCSharpType(item.DbType);
				if (TypeHelper.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, model.{item.Field.ToUpperPascal()}{SetInsertDefaultValue(item.Field, cSharpType, item.IsNotNull)}, {item.Length}, {GetspecificType(item)});");
				if (item.DbType == "geometry")
				{
					writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}_point0\", NpgsqlDbType.Varchar, $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\", -1, null);");
					writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}_srid0\", NpgsqlDbType.Integer, model.{item.Field.ToUpperPascal()}_srid, -1, null);");
				}
			}
			writer.WriteLine($"\t\t\treturn {valuename}.ExecuteNonQueryReader(insertSqlText);");
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Select
		private void SelectGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>();
				string where = string.Empty;
				foreach (var item in pkList)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == item.Field);
					d_key.Add(fs.RelType + " " + fs.Field);
					where += $".Where{fs.Field.ToUpperPascal()}({fs.Field})";
				}
				writer.WriteLine($"\t\tpublic static {ModelClassName} GetItem({string.Join(",", d_key)}) => Select{where}.ToOne();");
			}
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				string cSharpType = TypeHelper.ConvertPgDbTypeToCSharpType(item.RelType).Replace("?", "");
				if (TypeHelper.MakeWhereOrExceptType(cSharpType))
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} = {{0}}\", {item.Field}) as {DalClassName};");
				switch (cSharpType.ToLower())
				{
					case "string":
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Like({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} LIKE {{{0}}}\", {item.Field}.Select(a => \"%\" + a + \"%\").ToArray()) as {DalClassName};");
						break;
					case "datetime":
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Before(DateTime datetime, bool isEquals = false) => Where($\"a.{item.Field} <{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", datetime) as {DalClassName};");
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}After(DateTime datetime, bool isEquals = false) => Where($\"a.{item.Field} >{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", datetime) as {DalClassName};");
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Between(DateTime datetime1, DateTime datetime2) => Where(\"a.{item.Field} between {{0}} and {{1}}\", datetime1, datetime2) as {DalClassName};");
						break;
					case "int":
					case "long":
					case "decimal":
					case "float":
					case "double":
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}GreaterThan({cSharpType} value, bool isEquals = false) => Where($\"a.{item.Field} >{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", value) as {DalClassName};");
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}LessThan({cSharpType} value, bool isEquals = false) => Where($\"a.{item.Field} <{{(isEquals ? \"=\" : \"\")}} {{{{0}}}}\", value) as {DalClassName};");
						break;
					default: break;
				}
				if (item.IsArray)
				{
					// mark: Db_type有待确认
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Any({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} @> array[{{0}}::{item.DbType}]\", {item.Field}) as {DalClassName};");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}IsArrayNull() => Where(\"a.{item.Field} = '{{}}' OR a.{item.Field} = {{0}}\", null) as {DalClassName};");
				}
			}
		}
		#endregion

		#region Update
		private void UpdateGenerator(StreamWriter writer)
		{
			if (pkList.Count > 0)
			{
				List<string> d_key = new List<string>();
				string where1 = string.Empty, where2 = string.Empty;
				for (int i = 0; i < pkList.Count; i++)
				{
					FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == pkList[i].Field);
					d_key.Add(fs.RelType + " " + fs.Field);
					where1 += $".Where(\"{fs.Field} = {{0}}\", model.{fs.Field.ToUpperPascal()})";
					where2 += $".Where(\"{fs.Field} = {{0}}\", {fs.Field})";
				}
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({ModelClassName} model) => UpdateDiy{where1} as {DalClassName}UpdateBuilder;");
				writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update({string.Join(",", d_key)}) => UpdateDiy{where2} as {DalClassName}UpdateBuilder;");
			}

			writer.WriteLine($"\t\tpublic class {DalClassName}UpdateBuilder : UpdateBuilder<{ModelClassName}>");
			writer.WriteLine("\t\t{");
			writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder() => Field = _field;");
			writer.WriteLine($"\t\t\tpublic new {DalClassName}UpdateBuilder Where(string filter, params object[] value) => base.Where(filter, value) as {DalClassName}UpdateBuilder;");
			// set
			foreach (var item in fieldList)
			{
				NpgsqlDbType _dbtype = TypeHelper.ConvertDbTypeToNpgsqlDbTypeEnum(item.DataType, item.DbType);
				string ap = item.IsArray ? " | NpgsqlDbType.Array" : "";
				if (TypeHelper.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}({item.RelType} {item.Field}) => SetField(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, {item.Field}, {item.Length}, {GetspecificType(item)}) as {DalClassName}UpdateBuilder;");
				string cSharpType = TypeHelper.ConvertPgDbTypeToCSharpType(item.DbType);

				if (item.IsArray)
				{
					if (_dbtype == NpgsqlDbType.Enum)
						cSharpType = $"{cSharpType.ToUpperPascal()}ENUM";
					//join
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Join(params {cSharpType}[] {item.Field}) => SetFieldJoin(\"{item.Field}\", NpgsqlDbType.{TypeHelper.ConvertDbTypeToNpgsqlDbTypeEnum(item.DataType, item.DbType)}, {item.Field}, 0,{GetspecificType(item)}) as {DalClassName}UpdateBuilder;");

					//remove
					writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Remove({cSharpType} {item.Field}) => SetFieldRemove(\"{item.Field}\", NpgsqlDbType.{TypeHelper.ConvertDbTypeToNpgsqlDbTypeEnum(item.DataType, item.DbType) }, {item.Field}, 0,{GetspecificType(item)}) as {DalClassName}UpdateBuilder;");
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
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment({cSharpType} {item.Field}) => SetFieldIncrement(\"{item.Field}\", {item.Field}, {item.Length}) as {DalClassName}UpdateBuilder;");
							break;
						case "datetime":
						case "timespan":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Plus(TimeSpan timeSpan) => SetFieldDateTimeOperation(\"{item.Field}\", timeSpan, true, {item.Length}) as {DalClassName}UpdateBuilder;");
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Minus(TimeSpan timeSpan) => SetFieldDateTimeOperation(\"{item.Field}\", timeSpan, false, {item.Length}) as {DalClassName}UpdateBuilder;");
							break;
						case "geometry":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}(decimal x, decimal y, int SRID = 4326)");
							writer.WriteLine("\t\t\t{");
							writer.WriteLine($"\t\t\t\tAddParameter(\"point\", NpgsqlDbType.Varchar, $\"POINT({{x}} {{y}})\", -1, null);");
							writer.WriteLine($"\t\t\t\tAddParameter(\"srid\", NpgsqlDbType.Integer, SRID, -1, null);");
							writer.WriteLine($"\t\t\t\tsetList.Add(\"{item.Field} = ST_GeomFromText(@point,@srid)\");");
							writer.WriteLine($"\t\t\t\treturn this;");
							writer.WriteLine("\t\t\t}");
							break;
						default: break;
					}
				}
			}
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Private Method
		private string GetspecificType(FieldInfo fi)
		{
			string specificType = "null";
			if (fi.DataType == "e")
				specificType = $"typeof({fi.RelType.Replace("?", "")})";

			return specificType;
		}
		public static string DotValueHelper(string conname, List<FieldInfo> fields)
		{
			conname = conname.ToUpperPascal();
			foreach (var item in fields)
			{
				if (item.Field.ToLower() == conname.ToLower())
					if (item.RelType.Contains("?"))
						conname += ".Value";
			}
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
			var defaultValue = ht.ContainsKey(field) ? " = " + ht[field].ToString() + ";" : "";
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
