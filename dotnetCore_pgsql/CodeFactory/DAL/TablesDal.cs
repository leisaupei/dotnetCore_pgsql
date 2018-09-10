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
		readonly string _projectName;
		readonly string _modelPath;
		readonly string _dalPath;
		readonly string _schemaName;
		TableViewModel _table;
		bool _isGeometryTable = false;
		bool _isView = false;
		public List<FieldInfo> fieldList = new List<FieldInfo>();
		public List<PrimarykeyInfo> pkList = new List<PrimarykeyInfo>();
		public List<ConstraintMoreToOne> consListMoreToOne = new List<ConstraintMoreToOne>();
		public List<ConstraintOneToMore> consListOneToMore = new List<ConstraintOneToMore>();
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
			if (table.Type == "view")
				_isView = true;
			GetFieldList();
		}


		/// <summary>
		/// Model postfix
		/// </summary>
		readonly string _modelSuffix = "Model";
		readonly string _foreignKeyPrefix = "Get";
		/// <summary>
		/// Model name.
		/// </summary>
		string ModelClassName => DalClassName + _modelSuffix;
		/// <summary>
		/// DAL name.
		/// </summary>
		string DalClassName => $"{TypeHelper.DeletePublic(_schemaName, _table.Name)}";
		/// <summary>
		/// table name.
		/// </summary>
		string TableName => $"{TypeHelper.DeletePublic(_schemaName, _table.Name, _isView).ToLowerPascal()}";
		#region Get

		public void GetFieldList()
		{
			fieldList = SQL.Select(@"a.oid,c.attnum as num,c.attname AS field,c.attnotnull AS isnotnull,d.description AS comment,e.typcategory,
				(f.is_identity = 'YES') as isidentity,format_type(c.atttypid,c.atttypmod) AS type_comment,
				(CASE WHEN f.character_maximum_length IS NULL THEN	c.attlen ELSE f.character_maximum_length END) AS length,
				(CASE WHEN e.typelem = 0 THEN e.typname WHEN e.typcategory = 'G' THEN format_type (c.atttypid, c.atttypmod) ELSE e2.typname END ) AS dbtype,
				(CASE WHEN e.typelem = 0 THEN e.typtype ELSE e2.typtype END) AS datatype, ns.nspname").From("pg_class")
			   .InnerJoin("pg_namespace", "b", "a.relnamespace = b.oid")
			   .InnerJoin("pg_attribute", "c", "attrelid = a.oid")
			   .Join(UnionEnum.LEFT_OUTER_JOIN, "pg_description", "d", "c.attrelid = d.objoid AND c.attnum = d.objsubid AND c.attnum > 0")
			   .InnerJoin("pg_type", "e", "e.oid = c.atttypid")
			   .LeftJoin("pg_type", "e2", "e2.oid = e.typelem")
			   .InnerJoin("information_schema.COLUMNS", "f", "f.table_schema = b.nspname AND f.TABLE_NAME = a.relname AND COLUMN_NAME = c.attname")
			   .LeftJoin("pg_namespace", "ns", "ns.oid = e.typnamespace and ns.nspname <> 'pg_catalog'")
			   .Where($"b.nspname='{_schemaName}' and a.relname='{_table.Name}'").ToList<FieldInfo>(fi =>
			   {
				   fi.IsArray = fi.Typcategory == "A";
				   fi.DbType = fi.DbType.StartsWith("_") ? fi.DbType.Remove(0, 1) : fi.DbType;
				   fi.PgDbType = Types.ConvertDbTypeToNpgsqlDbTypeEnum(fi.DataType, fi.DbType);
				   fi.IsEnum = fi.DataType == "e";
				   string _type = Types.ConvertPgDbTypeToCSharpType(fi.DbType);
				   if (fi.IsEnum) _type = TypeHelper.DeletePublic(fi.Nspname, _type);
				   if (fi.DataType == "c") fi.RelType = TypeHelper.DeletePublic(fi.Nspname, _type);
				   else
				   {
					   string _notnull = "";
					   if (_type != "string" && _type != "JToken" && _type != "byte[]" && !fi.IsArray && _type != "object")
						   _notnull = fi.IsNotNull ? "" : "?";
					   string _array = fi.IsArray ? "[]" : "";
					   fi.RelType = $"{_type}{_notnull}{_array}";
				   }
				   return fi;

			   });
		}

		public void GetConstraint()
		{
			//多对一关系
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
			//一对一关系
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
				.ToList<ConstraintOneToMore>(fi =>
				{
					if (fi.IsOneToOne) return fi;//只添加一对一关系
					else return null;
				});
		}

		public void GetPrimaryKey()
		{
			pkList = SQL.Select("b.attname AS field,format_type (b.atttypid, b.atttypmod) AS typename").From("pg_index")
				.InnerJoin("pg_attribute", "b", "b.attrelid = a.indrelid AND b.attnum = ANY (a.indkey)")
				.Where($"a.indrelid = '{_schemaName}.{_table.Name}'::regclass AND a.indisprimary").ToList<PrimarykeyInfo>();
		}
		#endregion

		#region Generator
		/// <summary>
		/// Generate model files(Model.cs). 
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
					writer.WriteLine("\t\t#region Foreign Key");
					Hashtable ht = new Hashtable();

					void WriteForeignKey(ConstraintMoreToOne item)
					{
						string tablename = TypeHelper.DeletePublic(item.Nspname, item.TableName);
						string propertyName = $"{_foreignKeyPrefix}{tablename}";

						if (ht.ContainsKey(propertyName))
							propertyName = propertyName + "By" + TypeHelper.ExceptUnderlineToUpper(item.Conname);

						string tmp_var = $"_{propertyName.ToLowerPascal()}";

						writer.WriteLine();
						writer.WriteLine($"\t\tprivate {tablename}{_modelSuffix} {tmp_var} = null;");
						writer.WriteLine($"\t\t[ForeignKeyProperty]");
						writer.WriteLine($"\t\tpublic {tablename}{_modelSuffix} {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.GetItem({DotValueHelper(item.Conname, fieldList)});");

						ht.Add(propertyName, "");
					}
					foreach (var item in consListMoreToOne.Where(f => $"{f.TableName}_{f.RefColumn}" == f.Conname))
						WriteForeignKey(item);

					consListMoreToOne.RemoveAll(f => $"{f.TableName}_{f.RefColumn}" == f.Conname);
					foreach (var item in consListMoreToOne)
						WriteForeignKey(item);


					foreach (var item in consListOneToMore)
					{
						string tablename = TypeHelper.DeletePublic(item.Nspname, item.TableName);
						string propertyName = $"{_foreignKeyPrefix}{tablename}s";
						if (ht.Contains(propertyName))
							propertyName = propertyName + "By" + TypeHelper.ExceptUnderlineToUpper(item.Conname);
						string tmp_var = $"_{propertyName.ToLowerPascal()}";
						if (item.IsOneToOne)
						{
							writer.WriteLine();
							tmp_var = tmp_var.TrimEnd('s');
							propertyName = propertyName.TrimEnd('s');
							writer.WriteLine($"\t\tprivate {tablename}{_modelSuffix} {tmp_var} = null;");
							writer.WriteLine($"\t\t[ForeignKeyProperty]");
							writer.WriteLine($"\t\tpublic {tablename}{_modelSuffix} {propertyName} => {tmp_var} = {tmp_var} ?? {tablename}.GetItem({DotValueHelper(item.Conname, fieldList)});");
							ht.Add(propertyName, "");
						}
					}
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

				DalGenerator();
			}
		}
		/// <summary>
		/// Generate dal files(DAL.cs). 
		/// </summary>
		private void DalGenerator()
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
		#endregion

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
			if (_isGeometryTable)
			{
				writer.WriteLine($"\t\tconst string _field = \"{sb_query.ToString()}\";");
				writer.WriteLine($"\t\tpublic {DalClassName}() => _fields = _field;");
			}
			writer.WriteLine($"\t\tpublic static {DalClassName} Select => new {DalClassName}();");
			writer.WriteLine($"\t\tpublic static {DalClassName} SelectDiy(string fields) => new SelectExchange<{DalClassName}, {ModelClassName}>(fields) as {DalClassName};");
			writer.WriteLine($"\t\tpublic static {DalClassName} SelectDiy(string fields, string alias) => new SelectExchange<{DalClassName}, {ModelClassName}>(fields, alias) as {DalClassName};");
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
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{_modelSuffix}> models) => Delete(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{types}> {s_key[0]}) => DeleteDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}).Commit();");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static int Delete(IEnumerable<{DalClassName}{_modelSuffix}> models) =>  Delete(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
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
			writer.WriteLine($"\t\tpublic static {ModelClassName} Insert({ModelClassName} model)");
			writer.WriteLine("\t\t{");
			writer.WriteLine($"\t\t\tInsertBuilder insert = InsertDiy;");
			foreach (var item in fieldList)
			{
				if (item.IsIdentity) continue;
				string cSharpType = Types.ConvertPgDbTypeToCSharpType(item.DbType);
				if (Types.NotCreateModelFieldDbType(item.DbType, item.Typcategory))
					writer.WriteLine($"\t\t\tinsert.Set(\"{item.Field}\", model.{item.Field.ToUpperPascal()}{SetInsertDefaultValue(item.Field, cSharpType, item.IsNotNull)}, {item.Length});");

				if (item.DbType == "geometry")
				{
					writer.WriteLine($"\t\t\tinsert.Set(\"{item.Field}\", \"ST_GeomFromText(@{item.Field}_point0, @{item.Field}_srid0)\",");
					writer.WriteLine($"\t\t\t\tnew List<NpgsqlParameter> {{ new NpgsqlParameter(\"{item.Field}_point0\", $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\"),new NpgsqlParameter(\"{item.Field}_srid0\", model.{item.Field.ToUpperPascal()}_srid) }});");
				}
			}
			writer.WriteLine($"\t\t\treturn insert.Commit<{ModelClassName}>();");
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Select
		private void SelectGenerator(StreamWriter writer)
		{
			StringBuilder sbEx = new StringBuilder();
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
					string cSharpType = Types.ConvertPgDbTypeToCSharpType(fs.RelType).Replace("?", "");
					if (cSharpType.ToLower() == "datetime")
						sbEx.AppendLine($"\t\tpublic {DalClassName} Where{fs.Field.ToUpperPascal()}({Types.GetWhereTypeFromDbType(fs.RelType, fs.IsNotNull)} {fs.Field}) => WhereOr(\"a.{fs.Field} = {{0}}\", {fs.Field});");
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
				if (item.DataType == "c") continue;
				if (!item.IsArray)
				{
					string cSharpType = Types.ConvertPgDbTypeToCSharpType(item.RelType).Replace("?", "");
					if (Types.MakeWhereOrExceptType(cSharpType))
						writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} = {{0}}\", {item.Field});");
					switch (cSharpType.ToLower())
					{
						case "string":
							writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Like({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} LIKE {{0}}\", {item.Field}.Select(a => $\"%{{a}}%\"));");
							break;
						case "datetime":
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
				}
				else
				{
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull).Substring(7)} {item.Field}) => WhereArray(\"a.{item.Field} = {{0}}\", {item.Field});");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Any({Types.GetWhereTypeFromDbType(item.RelType, item.IsNotNull)} {item.Field}) => WhereOr(\"a.{item.Field} @> array[{{0}}::{item.DbType}]\", {item.Field});");
					writer.WriteLine($"\t\tpublic {DalClassName} Where{item.Field.ToUpperPascal()}Length(string sqlOperator, object value) => Where($\"array_length(a.{item.Field}, 1) {{sqlOperator}} {{{{0}}}}\", value);");
				}

			}
			writer.WriteLine(sbEx);
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
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{_modelSuffix}> models) => Update(models.Select(a => a.{s_key[0].ToUpperPascal()}));");
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{types}> {s_key[0]}s) => UpdateDiy.WhereOr(\"{s_key[0]} = {{0}}\", {s_key[0]}s);");
				}
				else if (pkList.Count > 1)
				{
					writer.WriteLine($"\t\tpublic static {DalClassName}UpdateBuilder Update(IEnumerable<{DalClassName}{_modelSuffix}> models) => Update(models.Select(a => ({s_key.Select(a => $"a.{a.ToUpperPascal()}").Join(", ")})));");
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
							break;
						case "geometry":
							writer.WriteLine($"\t\t\tpublic {DalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}(float x, float y, int SRID = 4326) => SetGeometry(\"{item.Field}\", x, y, SRID);");
							break;
						default: break;
					}
				}
			}
			writer.WriteLine("\t\t}");

		}
		#endregion

		#region Private Method

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
		/// optional field improve .Value postfix
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
		/// Give a default value when this field is null and the NotNull property is false too.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="cSharpType"></param>
		/// <param name="isNotNull"></param>
		/// <returns></returns>
		public static string SetInsertDefaultValue(string field, string cSharpType, bool isNotNull)
		{
			if (isNotNull) return "";
			if (cSharpType == "JToken") return " ?? JToken.Parse(\"{}\") ";
			if (field == "create_time" && cSharpType == "DateTime") return " ?? DateTime.Now";
			if (field == "id" && cSharpType == "Guid") return " ?? Guid.NewGuid()";
			return "";
		}
		#endregion
	}
}
