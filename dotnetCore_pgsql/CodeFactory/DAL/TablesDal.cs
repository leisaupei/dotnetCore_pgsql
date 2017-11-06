using Common.db.DBHelper;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public List<ConstraintMoreToOne> consListMoreToOne = new List<ConstraintMoreToOne>();
        public List<ConstraintOneToMore> consListOneToMore = new List<ConstraintOneToMore>();
        public List<ConstraintMoreToMore> consListMoreToMore = new List<ConstraintMoreToMore>();
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

        #region Get
        public void GetFieldList()
        {
<<<<<<< HEAD
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
  b.nspname='{schemaName}' and a.relname='{table.Name}';
";
=======
            string sqlText = $@"SELECT a.oid,
                c.attnum as num,
                c.attname as field,
                 (case when f.character_maximum_length is null then c.attlen else f.character_maximum_length end) as length,
                c.attnotnull as notnull,
                d.description as comment,
                (case when e.typelem = 0 then e.typname when e.typcategory = 'G' then format_type(c.atttypid, c.atttypmod) else e2.typname end) as type,
                format_type(c.atttypid, c.atttypmod) AS type_comment,
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
>>>>>>> 15422cdd88155bbcf36d78f7fe96d36fa458071b
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
             fi.Typcategory = dr["typcategory"].ToString();
             string _type = TypeHelper.PgDbTypeConvertToCSharpString(fi.Db_type);

             if (fi.Is_enum) _type = _type.ToUpperPascal() + "ENUM";
             string _notnull = "";
             if (_type != "string" && _type != "JToken" && !fi.Is_array)
                 _notnull = fi.Is_not_null ? "" : "?";

             string _array = fi.Is_array ? "[]" : "";
             fi.RelType = $"{_type}{_notnull}{_array}";
             // dal
             this.fieldList.Add(fi);
         }, sqlText);
        }

        public void GetConstraint()
        {
<<<<<<< HEAD
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
			b.nspname = '{schemaName}' AND A .relname = '{table.Name}'
	);
";
            consListMoreToOne = GenericHelper<ConstraintMoreToOne>.Generic.ReaderToList<ConstraintMoreToOne>(PgSqlHelper.ExecuteDataReader(sqlText_MoreToOne));
            //一对多关系
            string sqlText_OneToMore = $@"
SELECT
	x. TABLE_NAME as table_name,
	x. COLUMN_NAME as ref_column,
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
			b.nspname = '{schemaName}' AND e.relname = '{table.Name}'
	)
) tp ON (
	x. TABLE_NAME = tp.relname AND x. CONSTRAINT_NAME = tp.conname
)
";
            consListOneToMore = GenericHelper<ConstraintOneToMore>.Generic.ReaderToList<ConstraintOneToMore>(PgSqlHelper.ExecuteDataReader(sqlText_OneToMore));
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
    A .indrelid = '{item.Nspname}.{item.Table_name}' :: regclass
AND A .indisprimary                
";
                    List<PrimarykeyInfo> pk = new List<PrimarykeyInfo>();
                    PgSqlHelper.ExecuteDataReader(dr=>{
                        if (dr["field"]?.ToString() != item.Ref_column)
                            pk.Add(new PrimarykeyInfo { Field = dr["field"].ToString(), TypeName = dr["typename"].ToString() });
                    },union_table_sql);
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
            b.nspname = '{item.Nspname}' AND A .relname = '{item.Table_name}'
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
                                        Table_name = dr["table_name"].ToString(),
                                        Ref_column = dr["ref_column"].ToString(),
                                        Contype = dr["contype"].ToString(),
                                        Nspname = dr["nspname"].ToString()

                                    });
                                }
                            }, sql1);

                        
                            foreach (var item2 in moretoone)
                            {
                                consListMoreToMore.Add(new ConstraintMoreToMore
                                {
                                    Main_nspname = schemaName,
                                    Main_table = table.Name,
                                    Center_main_field = item.Ref_column,
                                    Center_main_type = item.Contype,
                                    Center_minor_field = item2.Conname,
                                    Center_minor_type = item2.Contype,
                                    Center_nspname = item.Nspname,
                                    Center_table = item.Table_name,
                                    Main_field = item.Conname,
                                    Minor_field = item2.Ref_column,
                                    Minor_nspname = item2.Nspname,
                                    Minor_table = item2.Table_name

                                });
                            }
                        }
                    }
                }
            }
=======
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
            consList = GenericHelper<ConstraintInfo>.Generic.ReaderToList<ConstraintInfo>(PgSqlHelper.ExecuteDataReader(sqlText));
>>>>>>> 15422cdd88155bbcf36d78f7fe96d36fa458071b
        }

        public void GetPrimaryKey()
        {
            string sqlText = $@"
<<<<<<< HEAD
SELECT
	b.attname AS field,
	format_type (b.atttypid, b.atttypmod) AS typename
FROM
	pg_index A
INNER JOIN pg_attribute b ON b.attrelid = A .indrelid
AND b.attnum = ANY (A .indkey)
WHERE
	A .indrelid = '{schemaName}.{table.Name}' :: regclass
AND A .indisprimary;
";
=======
                SELECT b.attname as field, format_type(b.atttypid, b.atttypmod) AS typename
                FROM pg_index a
                INNER JOIN pg_attribute b ON b.attrelid = a.indrelid AND b.attnum = ANY(a.indkey)
                WHERE a.indrelid = '{schemaName}.{table.Name}'::regclass AND a.indisprimary;";
>>>>>>> 15422cdd88155bbcf36d78f7fe96d36fa458071b
            pkList = GenericHelper<PrimarykeyInfo>.Generic.ReaderToList<PrimarykeyInfo>(PgSqlHelper.ExecuteDataReader(sqlText));
        }
        #endregion


        private string DeletePublic(string _schemaName, string table_name, bool isTableName = false)
        {
            if (isTableName)
                return _schemaName.ToLower() == "public" ? table_name.ToUpperPascal() : _schemaName.ToLower() + "." + table_name;
            else
                return _schemaName.ToLower() == "public" ? table_name.ToUpperPascal() : _schemaName.ToUpperPascal() + "_" + table_name;
        }
        private string modelClassName => dalClassName + "Model";
        private string dalClassName => $"{DeletePublic(schemaName, table.Name)}";
        private string tableName => $"{DeletePublic(schemaName, table.Name, true).ToLowerPascal()}";
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
                writer.WriteLine("using Newtonsoft.Json.Linq;");
                writer.WriteLine($"using {projectName}.DAL;");
                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}.Model");
                writer.WriteLine("{");
                writer.WriteLine($"\t[EntityMapping(TableName = \"{tableName}\"), JsonObject(MemberSerialization.OptIn)]");
                writer.WriteLine($"\tpublic partial class {modelClassName}");
                writer.WriteLine("\t{");

                writer.WriteLine("\t\t#region Properties");
                foreach (var item in fieldList)
                {
                    if (TypeHelper.NotCreateModelFieldDbType(item.Db_type, item.Typcategory))
                    {
                        if (!string.IsNullOrEmpty(item.Comment))
                        {
                            writer.WriteLine("\t\t/// <summary>");
                            writer.WriteLine($"\t\t/// {item.Comment}");
                            writer.WriteLine("\t\t/// </summary>");
                        }

                        writer.WriteLine($"\t\t[JsonProperty] public {item.RelType} {item.Field.ToUpperPascal()} {{ get; set; }}");
                    }
                    if (item.Db_type == "geometry")
                    {
                        List<FieldInfo> str_field = new List<FieldInfo>();
                        str_field.Add(new FieldInfo { Comment = item.Field + "经度", Field = item.Field + "_y", RelType = "decimal" });
                        str_field.Add(new FieldInfo { Comment = item.Field + "纬度", Field = item.Field + "_x", RelType = "decimal" });
                        str_field.Add(new FieldInfo { Comment = item.Field + "空间坐标系唯一标识", Field = item.Field + "_srid", RelType = "int" });
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
                writer.WriteLine("\t\t#region Foreign Key");
                Hashtable ht = new Hashtable();
                foreach (var item in consListMoreToOne)
                {
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
                foreach (var item in consListOneToMore)
                {
                    string propertyName = $"Obj_{item.Nspname.ToLowerPascal()}_{item.Table_name}s";
                    string f_dalName = $"{item.Nspname.ToUpperPascal()}_{item.Table_name}";
                    string tmp_var = $"_{propertyName.ToLowerPascal()}";
                    writer.WriteLine();
                    writer.WriteLine($"\t\tprivate List<{f_dalName}Model> {tmp_var} = null;");
                    writer.WriteLine($"\t\t[ForeignKeyProperty]");
                    writer.WriteLine($"\t\tpublic List<{f_dalName}Model> {propertyName} => {tmp_var} = {tmp_var} == null ? {f_dalName}.Query.Where{item.Ref_column.ToUpperPascal()}({item.Conname.ToUpperPascal()}).ToList() : {tmp_var};");
                }
                foreach (var item in consListMoreToMore)
                {
                    string propertyName = $"Obj_{item.Minor_nspname.ToLowerPascal()}_{item.Minor_table}s";
                    string f_dalName = $"{item.Minor_nspname.ToUpperPascal()}_{item.Minor_table}";
                    string tmp_var = $"_{propertyName.ToLowerPascal()}";
                    string center_dal = $"{item.Center_nspname.ToUpperPascal()}_{item.Center_table}";
                    string mian_dal = $"{item.Main_nspname.ToUpperPascal()}_{item.Main_table}";

                    writer.WriteLine();
                    writer.WriteLine($"\t\tprivate List<{f_dalName}Model> {tmp_var} = null;");
                    writer.WriteLine("\t\t[ForeignKeyProperty]");
                    writer.WriteLine($"\t\tpublic List<{f_dalName}Model> {propertyName} => {tmp_var} = {tmp_var} == null ? {f_dalName}.Query.InnerJoin<{center_dal}>(\"b\", \"b.{item.Center_minor_field} = a.{item.Minor_field}\").InnerJoin<{mian_dal}>(\"c\", \"c.{item.Main_field} = b.{item.Center_main_field}\").Where(\"c.{item.Main_field} = {{0}}\", {item.Main_field.ToUpperPascal()}).ToList() : {tmp_var};");
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
                    writer.WriteLine($"\t\t[MethodProperty] public {dalClassName}.{dalClassName}UpdateBuilder Update => {dalClassName}.Update(this);");
                writer.WriteLine();
                writer.WriteLine($"\t\tpublic {modelClassName} Insert() => {dalClassName}.Insert(this);");
                writer.WriteLine("\t\t#endregion");
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
                writer.WriteLine("using Common.db.Common;");
                writer.WriteLine("using Common.db.DBHelper;");
                writer.WriteLine($"using {projectName}.Model;");
                writer.WriteLine("using NpgsqlTypes;");
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("using System.Linq;");
                writer.WriteLine("using System.Linq.Expressions;");
                writer.WriteLine("using System.Threading.Tasks;");
                writer.WriteLine("using Newtonsoft.Json.Linq;");

                writer.WriteLine();
                writer.WriteLine($"namespace {projectName}.DAL");
                writer.WriteLine("{");
                writer.WriteLine($"\t[EntityMapping(TableName = \"{tableName}\")]");
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

        #region Dal Property
        private void PropertiesGenerator(StreamWriter writer)
        {
            StringBuilder sb_field = new StringBuilder();
            StringBuilder sb_param = new StringBuilder();
            StringBuilder sb_query = new StringBuilder();
            for (int i = 0; i < fieldList.Count; i++)
            {
                var item = fieldList[i];
                if (item.Is_identity) continue;
                if (item.Db_type == "geometry")
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
            writer.WriteLine($"\t\tconst string insertSqlText = \"INSERT INTO {tableName} as a ({sb_field.ToString()}) VALUES({sb_param}) RETURNING \" + _field;");
            writer.WriteLine($"\t\tconst string _field = \"{sb_query.ToString()}\";");
            writer.WriteLine($"\t\tpublic static {dalClassName} Query => new {dalClassName}();");

            writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder UpdateDiy => new {dalClassName}UpdateBuilder();");
            writer.WriteLine($"\t\tpublic static DeleteBuilder<{modelClassName}> DeleteDiy => new DeleteBuilder<{modelClassName}>();");
            writer.WriteLine($"\t\tpublic {dalClassName}() => Field = _field;");
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
            var valuename = dalClassName.ToLowerPascal();
            writer.WriteLine($"\t\tpublic static {modelClassName} Insert({modelClassName} model)");
            writer.WriteLine("\t\t{");
            writer.WriteLine($"\t\t\t{dalClassName} {valuename} = Query;");
            foreach (var item in fieldList)
            {
                if (item.Is_identity) continue;
                NpgsqlDbType _dbtype = TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type);
                string ap = item.Is_array ? " | NpgsqlDbType.Array" : "";
                string cSharpType = TypeHelper.PgDbTypeConvertToCSharpString(item.Db_type);
                if (TypeHelper.NotCreateModelFieldDbType(item.Db_type, item.Typcategory))
<<<<<<< HEAD
                    writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, model.{item.Field.ToUpperPascal()}{SetInsertDefaultValue(item.Field, cSharpType)}, {item.Length}, {GetspecificType(item)});");
=======
                    writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}\", NpgsqlDbType.{_dbtype}{ap}, model.{item.Field.ToUpperPascal()}, {item.Length}, {GetspecificType(item)});");
>>>>>>> 15422cdd88155bbcf36d78f7fe96d36fa458071b
                if (item.Db_type == "geometry")
                {
                    writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}_point0\", NpgsqlDbType.Varchar, $\"POINT({{model.{item.Field.ToUpperPascal()}_x}} {{model.{item.Field.ToUpperPascal()}_y}})\", -1, null);");
                    writer.WriteLine($"\t\t\t{valuename}.AddParameter(\"{item.Field}_srid0\", NpgsqlDbType.Integer, model.{item.Field.ToUpperPascal()}_srid, -1, null);");
                }
            }
            writer.WriteLine($"\t\t\treturn {valuename}.ExecuteNonQueryReader(insertSqlText);");
            writer.WriteLine("\t\t}");

        }
        #endregion

        #region Query
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
                writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder Update({modelClassName} model) => UpdateDiy{where1} as {dalClassName}UpdateBuilder;");
                writer.WriteLine($"\t\tpublic static {dalClassName}UpdateBuilder Update({string.Join(",", d_key)}) => UpdateDiy{where2} as {dalClassName}UpdateBuilder;");
            }

            writer.WriteLine($"\t\tpublic class {dalClassName}UpdateBuilder : UpdateBuilder<{modelClassName}>");
            writer.WriteLine("\t\t{");
            writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder() => Field = _field;");
            writer.WriteLine($"\t\t\tpublic new {dalClassName}UpdateBuilder Where(string filter, params object[] value) => base.Where(filter, value) as {dalClassName}UpdateBuilder;");
            // set
            foreach (var item in fieldList)
            {
                NpgsqlDbType _dbtype = TypeHelper.ConvertFromDbTypeToNpgsqlDbTypeEnum(item.Data_Type, item.Db_type);
                string ap = item.Is_array ? " | NpgsqlDbType.Array" : "";
                if (TypeHelper.NotCreateModelFieldDbType(item.Db_type, item.Typcategory))
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
                        case "geometry":
                            writer.WriteLine($"\t\t\tpublic {dalClassName}UpdateBuilder Set{item.Field.ToUpperPascal()}(decimal x, decimal y, int SRID = 4326)");
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
        public static string SetInsertDefaultValue(string field, string cSharpType)
        {
            if (cSharpType == "JToken") return " == null ? JToken.Parse(\"{}\") : model." + field.ToUpperPascal();
            switch (field)
            {
                case "create_time":
                    if (cSharpType == "DateTime") return " == null ? DateTime.Now : model.Create_time";
                    break;
                case "id":
                    if (cSharpType == "Guid") return ".IsNullOrEmpty() ? Guid.NewGuid() : model.Id";
                    break;
            }
            return "";
        }
        #endregion
    }
}
