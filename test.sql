
create SCHEMA "class";
ALTER SCHEMA "class" OWNER TO "postgres";

create extension hstore;

CREATE TYPE "public"."e_data_state" AS ENUM ('正常','已删除');
ALTER TYPE "public"."e_data_state" OWNER TO "postgres";

CREATE TYPE "public"."info" AS (
  "id" uuid,
  "name" varchar COLLATE "pg_catalog"."default"
);
ALTER TYPE "public"."info" OWNER TO "postgres";

CREATE TABLE "classmate" (
"teacher_id" uuid NOT NULL,
"student_id" uuid NOT NULL,
"grade_id" uuid NOT NULL,
"create_time" timestamp(6),
CONSTRAINT "classmate_pkey" PRIMARY KEY ("teacher_id", "grade_id", "student_id") 
)
WITHOUT OIDS;
ALTER TABLE "classmate" OWNER TO "postgres";

CREATE TABLE "people" (
"id" uuid NOT NULL,
"age" int4 NOT NULL,
"name" varchar(255) COLLATE "default" NOT NULL,
"sex" bool,
"create_time" timestamp(6) NOT NULL,
"address" varchar(255) COLLATE "default",
"address_detail" jsonb NOT NULL DEFAULT '{}'::jsonb,
"state" "public"."e_data_state" NOT NULL DEFAULT '正常'::e_data_state,
CONSTRAINT "people_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
COMMENT ON COLUMN "people"."age" IS '年龄';
COMMENT ON COLUMN "people"."name" IS '姓名';
COMMENT ON COLUMN "people"."sex" IS '性别';
COMMENT ON COLUMN "people"."address" IS '家庭住址';
COMMENT ON COLUMN "people"."address_detail" IS '详细住址';
ALTER TABLE "people" OWNER TO "postgres";

CREATE TABLE "student" (
"stu_no" varchar(32) COLLATE "default" NOT NULL,
"grade_id" uuid NOT NULL,
"people_id" uuid NOT NULL,
"create_time" timestamp(6) NOT NULL,
"id" uuid NOT NULL,
CONSTRAINT "student_pkey" PRIMARY KEY ("id") ,
CONSTRAINT "unique_stu_no" UNIQUE ("stu_no"),
CONSTRAINT "unique_people_id" UNIQUE ("people_id")
)
WITHOUT OIDS;
CREATE INDEX "fki_fk_grade" ON "student" USING btree ("grade_id" "pg_catalog"."uuid_ops" ASC NULLS LAST);
COMMENT ON COLUMN "student"."stu_no" IS '学号';
ALTER TABLE "student" OWNER TO "postgres";

CREATE TABLE "teacher" (
"teacher_no" varchar(32) COLLATE "default" NOT NULL,
"people_id" uuid NOT NULL,
"create_time" timestamp(6) NOT NULL,
"id" uuid NOT NULL,
CONSTRAINT "student_copy1_pkey" PRIMARY KEY ("id") ,
CONSTRAINT "teacher_no_key" UNIQUE ("teacher_no"),
CONSTRAINT "people_id_key" UNIQUE ("people_id")
)
WITHOUT OIDS;
COMMENT ON COLUMN "teacher"."teacher_no" IS '学号';
ALTER TABLE "teacher" OWNER TO "postgres";

CREATE TABLE "type_test" (
"id" uuid NOT NULL,
"bit_type" bit(1),
"bool_type" bool,
"box_type" box,
"bytea_type" bytea,
"char_type" char(1) COLLATE "default",
"cidr_type" cidr,
"circle_type" circle,
"date_type" date,
"decimal_type" numeric,
"float4_type" float4,
"float8_type" float8,
"inet_type" inet,
"int2_type" int2,
"int4_type" int4,
"int8_type" int8,
"interval_type" interval(6),
"json_type" json,
"jsonb_type" jsonb,
"line_type" line,
"lseg_type" lseg,
"macaddr_type" macaddr,
"money_type" money,
"path_type" path,
"point_type" point,
"polygon_type" polygon,
"serial2_type" int2 NOT NULL DEFAULT nextval('type_test_serial2_type_seq'::regclass),
"serial4_type" int4 NOT NULL DEFAULT nextval('type_test_serial4_type_seq'::regclass),
"serial8_type" int8 NOT NULL DEFAULT nextval('type_test_serial8_type_seq'::regclass),
"text_type" text COLLATE "default",
"time_type" time(6),
"timestamp_type" timestamp(6),
"timestamptz_type" timestamptz(6),
"timetz_type" timetz(6),
"tsquery_type" tsquery,
"tsvector_type" tsvector,
"varbit_type" varbit,
"varchar_type" varchar COLLATE "default",
"xml_type" xml,
"hstore_type" "public"."hstore",
"enum_type" "public"."e_data_state",
"composite_type" "public"."info",
"bit_length_type" bit(8),
"array_type" int4[],
CONSTRAINT "type_test_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
ALTER TABLE "type_test" OWNER TO "postgres";

CREATE TABLE "grade" (
"id" uuid NOT NULL,
"name" varchar(255) COLLATE "default" NOT NULL,
"create_time" timestamp(6) NOT NULL,
CONSTRAINT "grade_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
COMMENT ON COLUMN "grade"."name" IS '班级名称';
ALTER TABLE "grade" OWNER TO "postgres";


ALTER TABLE "classmate" ADD CONSTRAINT "fk_classmate_grade_1" FOREIGN KEY ("grade_id") REFERENCES "grade" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE "classmate" ADD CONSTRAINT "fk_classmate_teacher_1" FOREIGN KEY ("teacher_id") REFERENCES "teacher" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE "student" ADD CONSTRAINT "fk_grade" FOREIGN KEY ("grade_id") REFERENCES "grade" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE "student" ADD CONSTRAINT "fk_student_people_1" FOREIGN KEY ("people_id") REFERENCES "people" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE "teacher" ADD CONSTRAINT "teacher_people_id_fkey" FOREIGN KEY ("people_id") REFERENCES "people" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;

