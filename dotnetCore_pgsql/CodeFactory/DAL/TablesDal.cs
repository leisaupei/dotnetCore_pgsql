using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Common.db.DBHelper;
using NpgsqlTypes;
using System.Data;
namespace dotnetCore_pgsql_DevVersion.CodeFactory.DAL
{
    public class TablesDal
    {

        private string projectName;
        private string modelPath;
        private string dalPath;
        private string schemaName;
        private TableViewModel table;

        public List<FieldInfo> fieldList = new List<FieldInfo>();
        public List<PrimarykeyInfo> pkList = new List<PrimarykeyInfo>();
        public List<ConstraintInfo> consList = new List<ConstraintInfo>();
        public TablesDal(string projectName, string modelPath, string dalPath, string schemaName, TableViewModel table)
        {
            this.projectName = projectName;
            this.modelPath = modelPath;
            this.dalPath = dalPath;
            this.schemaName = schemaName;
            this.table = table;
            GetPrimaryKey();
            GetConstraint();
            GetFieldList();

        }

        public void GetFieldList()
        {
            string sqlText = $@"SELECT a.oid,
                c.attnum as num,
                c.attname as field,
                 (case when f.character_maximum_length is null then c.attlen else f.character_maximum_length end) as length,
                c.attnotnull as notnull,
                d.description as comment,
                (case when e.typelem = 0 then e.typname else e2.typname end) as type,
                (case when e.typelem = 0 then e.typtype else e2.typtype end) as data_type,
                e.typcategory,
                f.is_identity
                from  pg_class a 
                inner join pg_namespace b on a.relnamespace=b.oid
                inner join pg_attribute c on attrelid = a.oid
                LEFT OUTER JOIN pg_description d ON c.attrelid = d.objoid AND c.attnum = d.objsubid and c.attnum > 0
                inner join pg_type e on e.oid=c.atttypid
                left join pg_type e2 on e2.oid=e.typelem
                inner join information_schema.columns f on f.table_schema = b.nspname and f.table_name=a.relname and column_name = c.attname
                WHERE b.nspname='{schemaName}' and a.relname='{table.Name}';";
            PgSqlHelper.ExecuteDataReader(dr =>
         {
             FieldInfo fi = new FieldInfo();
             fi.Oid = Convert.ToInt32(dr["oid"]);
             fi.Field = dr["field"].ToString();
             fi.Length = Convert.ToInt32(dr["length"].ToString());
             fi.Is_not_null = Convert.ToBoolean(dr["notnull"]);
             fi.Comment = dr["comment"].ToString();
             fi.Data_Type = dr["data_type"].ToString();
             fi.Db_type = dr["type"].ToString();
             fi.Db_type = fi.Db_type.StartsWith("_") ? fi.Db_type.Remove(0, 1) : fi.Db_type;
             fi.PgDbType = TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(fi.Data_Type, fi.Db_type);
             fi.Is_identity = dr["is_identity"].ToString() == "YES";
             fi.Is_array = dr["typcategory"].ToString() == "A";
             fi.Is_enum = fi.Data_Type == "e";

             string _type = TypeHelper.PgDbTypeConvertToCSharpString(fi.Db_type);

             if (fi.Is_enum) _type = _type.ToUpperPascal() + "ENUM";
             string _notnull = "";
             if (_type != "string" && _type != "JToken"&& !fi.Is_array)
                 _notnull = fi.Is_not_null ? "" : "?";

             string _array = fi.Is_array ? "[]" : "";
             fi.RelType = $"{_type}{_notnull}{_array}";
             // dal
             this.fieldList.Add(fi);
         }, sqlText);
        }

        public void GetConstraint()
        {
            string sqlText = $@"
                SELECT(select attname from pg_attribute where attrelid = a.conrelid and attnum = any(a.conkey)) as conname
                ,b.relname as table_name,c.nspname,d.attname as ref_column,e.typname as contype
                FROM pg_constraint a 
                left JOIN  pg_class b on b.oid= a.confrelid
                inner join pg_namespace c on b.relnamespace = c.oid
                INNER JOIN pg_attribute d on d.attrelid =a.confrelid and d.attnum=any(a.confkey)
                inner join pg_type e on e.oid = d.atttypid
                WHERE conrelid in 
                (
                SELECT a.oid FROM pg_class a 
                inner join pg_namespace b on a.relnamespace=b.oid
                WHERE b.nspname='{schemaName}' and a.relname='{table.Name}'
                );";
            consList = GenericHelper<ConstraintInfo>.Generic.ToList<ConstraintInfo>(PgSqlHelper.ExecuteDataReader(sqlText));
        }

        public void GetPrimaryKey()
        {
            string sqlText = $@"
                SELECT b.attname as field, format_type(b.atttypid, b.atttypmod) AS typename
                FROM pg_index a
                INNER JOIN pg_attribute b ON b.attrelid = a.indrelid AND b.attnum = ANY(a.indkey)
                WHERE a.indrelid = '{schemaName}.{table.Name}'::regclass AND a.indisprimary;";
            pkList = GenericHelper<PrimarykeyInfo>.Generic.ToList<PrimarykeyInfo>(PgSqlHelper.ExecuteDataReader(sqlText));
        }

        private string DeletePublic(string _schemaName, string table_name, bool isTableName = false)
        {
            if (isTableName)
                return _schemaName.ToLower() == "public" ? table_name.ToUpperPascal() : _schemaName.ToLower() + "." + table_name;
            else
                return _schemaName.ToLower() == "public" ? table_name.ToUpperPascal() : _schemaName.ToUpperPascal() + "_" + table_name;
        }
        private string modelClassName => dalClassName + "Model";
        private string dalClassName => $"{DeletePublic(schemaName, table.Name)}";
        private string tableName => $"{DeletePublic(schemaName, table.Name, true)}";
        public void Generate()
        {
            string _filename = $"{modelPath}/{modelClassName}.cs";

            using (StreamWriter writer = new StreamWriter(File.Create(_filename)))
            {
                writer.WriteLine("using Common.db.Common;");
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("using System.Linq;");
                writer.WriteLine("using System.Threading.Tasks;");
                writer.WriteLine("using Common.db.DBHelper;");
                writer.WriteLine("using NpgsqlTypes;");
                writer.WriteLine("using Newtonsoft.Json;");
                writer.WriteLine($"using {projectName}.DAL;");
                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}.Model");
                writer.WriteLine("{");
                writer.WriteLine($"\t[EntityMapping(TableName = \"{tableName}\"), JsonObject(MemberSerialization.OptIn)]");
                writer.WriteLine($"\tpublic partial class {modelClassName}");
                writer.WriteLine("\t{");
                foreach (var item in fieldList)
                {
                    if (!string.IsNullOrEmpty(item.Comment))
                    {
                        writer.WriteLine("\t\t/// <summary>");
                        writer.WriteLine($"\t\t/// {item.Comment}");
                        writer.WriteLine("\t\t/// </summary>");
                    }

                    writer.WriteLine($"\t\t[JsonProperty] public {item.RelType} {item.Field.ToUpperPascal()} {{ get; set; }}");
                }
                Hashtable ht = new Hashtable();
                foreach (var item in consList)
                {
                    //string pname = $"{item.Table_name.ToUpperPascal()}";
                    string propertyName = $"Obj_{item.Nspname.ToLowerPascal()}_{item.Table_name}";

                    if (ht.ContainsKey(propertyName))
                        propertyName += "By" + item.Conname;

                    string f_dalName = $"{item.Nspname.ToUpperPascal()}_{item.Table_name}";
                    string tmp_var = $"_{propertyName.ToLowerPascal()}";
                    writer.WriteLine();
                    writer.WriteLine($"\t\tprivate {f_dalName}Model {tmp_var} = null;");
                    writer.WriteLine($"\t\t[ForeignKeyProperty]");
                    writer.WriteLine($"\t\tpublic {f_dalName}Model {propertyName} => {tmp_var} = {tmp_var} == null ? {f_dalName}.GetItem({DotValueHelper(item.Conname, fieldList)}) : {tmp_var};");

                    ht.Add(propertyName, "");
                }
                List<string> d_Key = new List<string>();
                foreach (var item in pkList)
                {
                    FieldInfo fs = fieldList.FirstOrDefault(f => f.Field == item.Field);
                    d_Key.Add("this." + fs.Field.ToUpperPascal());
                }
                writer.WriteLine();
                writer.WriteLine($"\t\t[MethodProperty] public {dalClassName}.{dalClassName}UpdateBuilder Update => {dalClassName}.Update(this);");
                writer.WriteLine();
                writer.WriteLine($"\t\tpublic {modelClassName} Insert() => {dalClassName}.Insert(this);");

                writer.WriteLine("\t}");
                writer.WriteLine("}");
                writer.Flush();

                CreateDal();
            }
        }

        private void CreateDal()
        {
            string _filename = $"{dalPath}/{dalClassName}.cs";

            using (StreamWriter writer = new StreamWriter(File.Create(_filename)))
            {
                writer.WriteLine("using Common.db.DBHelper;");
                writer.WriteLine($"using {projectName}.Model;");
                writer.WriteLine("using NpgsqlTypes;");
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("using System.Linq;");
                writer.WriteLine("using System.Linq.Expressions;");
                writer.WriteLine("using System.Threading.Tasks;");
                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}.DAL");
                writer.WriteLine("{");
                writer.WriteLine($"\tpublic class {dalClassName} : Query<{modelClassName}>");
                writer.WriteLine("\t{");


                if (table.Type == "table")
                {
                    writer.WriteLine("\t\t#region Properties");
                    PropertiesGenerator(writer);
                    writer.WriteLine("\t\t#endregion");
                    writer.WriteLine();

                    writer.WriteLine("\t\t#region Delete");
                    DeleteGenerator(writer);
                    writer.WriteLine("\t\t#endregion");
                    writer.WriteLine();

                    writer.WriteLine("\t\t#region Insert");
                    InsertGenerator(writer);
                    writer.WriteLine("\t\t#endregion");
                    writer.WriteLine();

                    writer.WriteLine("\t\t#region Query");
                    QueryGenerator(writer);
                    writer.WriteLine("\t\t#endregion");
                    writer.WriteLine();

                    writer.WriteLine("\t\t#region Update");
                    UpdateGenerator(writer);
                    writer.WriteLine("\t\t#endregion");
                    writer.WriteLine();
                }
                writer.WriteLine("\t}");
                writer.WriteLine("}");
            }
        }

        private void PropertiesGenerator(StreamWriter writer)
        {
            StringBuilder sb_field = new StringBuilder();
            StringBuilder sb_param = new StringBuilder();

            for (int i = 0; i < fieldList.Count; i++)
            {
                var item = fieldList[i];
                if (item.Is_identity) continue;
                sb_field.Append($"{item.Field}");
                sb_param.Append($"@{item.Field}");
                if (fieldList.Count > i + 1)
                {
                    sb_field.Append(",");
                    sb_param.Append(",");
                }
            }
            writer.WriteLine($"\t\tconst string insertSqlText = \"INSERT INTO {tableName}({sb_field.ToString()}) VALUES({sb_param}) RETURNING {sb_field.ToString()};\";");
            writer.WriteLine($"\t\tpublic static {dalClassName} Query => new {dalClassName}();");
            writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder UpdateDiy => new {dalClassName}UpdateBuilder();");
            writer.WriteLine($"\t\tpublic static DeleteBuilder<{modelClassName}> DeleteDiy => new DeleteBuilder<{modelClassName}>();");
        }

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
                    where += $".Where(\"{fs.Field} = {{{i}}}\", {fs.Field})";
                }

                writer.WriteLine($"\t\tpublic static int Delete({string.Join(",", d_key)}) => DeleteDiy{where}.Commit();");
                writer.WriteLine();
            }
        }

        private void InsertGenerator(StreamWriter writer)
        {
            var valuename = dalClassName.ToLowerPascal();
            writer.WriteLine($"\t\tpublic static {modelClassName} Insert({modelClassName} model)");
            writer.WriteLine("\t\t{");
            writer.WriteLine($"\t\t\t{dalClassName} {valuename} = Query;");
            foreach (var item in fieldList)
            {
                if (item.Is_identity) continue;
                NpgsqlDbType _dbtype = TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type);
                string ap = item.Is_array ? " | NpgsqlDbType.Array" : "";
                writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, model.{item.Field.ToUpperPascal()}, {item.Length}, {GetspecificType(item)});");
            }
            writer.WriteLine($"\t\t\treturn {valuename}.ExecuteNonQueryReader(insertSqlText);");
            writer.WriteLine("\t\t}");
        }
        private void QueryGenerator(StreamWriter writer)
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
                writer.WriteLine($"\t\tpublic static {modelClassName} GetItem({string.Join(",", d_key)}) => Query{where}.ToOne();");
            }
            foreach (var item in fieldList)
            {
                if (item.Is_identity) continue;
                string cSharpType = TypeHelper.PgDbTypeConvertToCSharpString(item.RelType).Replace("?", "");
                if (TypeHelper.MakeWhereOrExceptType(cSharpType))
                    writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} = {{{0}}}\", {item.Field}) as {dalClassName};");
                switch (cSharpType.ToLower())
                {
                    case "string":
                        writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}Like({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} LIKE {{{0}}}\", {item.Field}.Select(a => \"%\" + a + \"%\").ToArray()) as {dalClassName};");
                        break;
                    case "datetime":
                        writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}Earlier(DateTime datetime) => Where(\"a.{item.Field} <= {{0}}\", datetime) as {dalClassName};");
                        writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}Later(DateTime datetime) => Where(\"a.{item.Field} >= {{0}}\", datetime) as {dalClassName};");
                        writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}Between(DateTime datetime1, DateTime datetime2) => Where(\"a.{item.Field} between {{0}} and {{1}}\", datetime1, datetime2) as {dalClassName};");
                        break;
                    default: break;
                }
                if (item.Is_array)
                {
                    // mark: Db_type有待确认
                    writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}Any({TypeHelper.GetWhereTypeFromDbType(item.RelType)} {item.Field}) => WhereOr(\"a.{item.Field} @> array[{{0}}::{item.Db_type}]\", {item.Field}) as {dalClassName};");
                    writer.WriteLine($"\t\tpublic {dalClassName} Where{item.Field.ToUpperPascal()}IsArrayNull() => Where(\"a.{item.Field} = '{{}}' OR a.{item.Field} = {{0}}\", null) as {dalClassName};");
                }
            }
        }
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
                writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder Update({modelClassName} model) => UpdateDiy{where1} as {dalClassName}UpdateBuilder;");
                writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder Update({string.Join(",", d_key)}) => UpdateDiy{where2} as {dalClassName}UpdateBuilder;");
            }

            writer.WriteLine($"\t\tpublic class {dalClassName}UpdateBuilder : UpdateBuilder<{modelClassName}>");
            writer.WriteLine("\t\t{");
            writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder() {{ }}");
            writer.WriteLine($"\t\t\tpublic new {dalClassName}UpdateBuilder Where(string filter, params object[] value) => base.Where(filter, value) as {dalClassName}UpdateBuilder;");
            // set
            foreach (var item in fieldList)
            {
                NpgsqlDbType _dbtype = TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type);
                string ap = item.Is_array ? " | NpgsqlDbType.Array" : "";
                writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}({item.RelType} {item.Field}) => SetField(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, {item.Field}, {item.Length}, {GetspecificType(item)}) as {dalClassName}UpdateBuilder;");
                string cSharpType = TypeHelper.PgDbTypeConvertToCSharpString(item.Db_type);

                if (item.Is_array)
                {
                    //join
                    writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Join(params {cSharpType}[] {item.Field}) => SetFieldJoin(\"{item.Field}\", NpgsqlDbType.{TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type)}, {item.Field}, 0,{GetspecificType(item)}) as {dalClassName}UpdateBuilder;");
                    //remove
                    writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Remove({cSharpType} {item.Field}) => SetFieldRemove(\"{item.Field}\", NpgsqlDbType.{TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type) }, {item.Field}, 0,{GetspecificType(item)}) as {dalClassName}UpdateBuilder;");
                }
                else
                {
                    switch (cSharpType.ToLower())
                    {
                        case "int":
                        case "short":
                        case "decimal":
                        case "long":
                            writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}Increment({cSharpType} {item.Field}) => SetFieldIncrement(\"{item.Field}\", {item.Field}, {item.Length}) as {dalClassName}UpdateBuilder;");
                            break;
                        case "datetime":
                            break;
                        default: break;
                    }
                }
            }
            writer.WriteLine("\t\t}");

        }

        private string GetspecificType(FieldInfo fi)
        {
            string specificType = "null";
            if (fi.Data_Type == "e")
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
    }
}
