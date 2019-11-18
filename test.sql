CREATE TABLE "people" (
"id" uuid NOT NULL,
"age" int4 NOT NULL,
"name" varchar(255) COLLATE "default" NOT NULL,
"sex" bool NOT NULL,
"create_time" timestamp(6) NOT NULL,
CONSTRAINT "people_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
COMMENT ON COLUMN "people"."age" IS '年龄';
COMMENT ON COLUMN "people"."name" IS '姓名';
COMMENT ON COLUMN "people"."sex" IS '性别';
ALTER TABLE "people" OWNER TO "postgres";

CREATE TABLE "student" (
"id" uuid NOT NULL,
"people_id" uuid NOT NULL,
"stu_no" varchar(32) COLLATE "default" NOT NULL,
"grade_id" uuid NOT NULL,
"create_time" timestamp(6) NOT NULL,
CONSTRAINT "student_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
CREATE INDEX "fki_fk_grade" ON "student" USING btree ("grade_id" "pg_catalog"."uuid_ops" ASC NULLS LAST);
COMMENT ON COLUMN "student"."stu_no" IS '学号';
ALTER TABLE "student" OWNER TO "postgres";

CREATE TABLE "grade" (
"id" uuid NOT NULL,
"name" varchar(255) COLLATE "default" NOT NULL,
"create_time" timestamp(6) NOT NULL,
CONSTRAINT "grade_pkey" PRIMARY KEY ("id") 
)
WITHOUT OIDS;
COMMENT ON COLUMN "grade"."name" IS '班级名称';
ALTER TABLE "grade" OWNER TO "postgres";


ALTER TABLE "student" ADD CONSTRAINT "fk_grade" FOREIGN KEY ("grade_id") REFERENCES "grade" ("id") ON DELETE NO ACTION ON UPDATE NO ACTION;
ALTER TABLE "student" ADD CONSTRAINT "fk_student_people_1" FOREIGN KEY ("people_id") REFERENCES "people" ("id");

