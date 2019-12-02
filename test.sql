
create SCHEMA "class";
ALTER SCHEMA "class" OWNER TO "postgres";
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

