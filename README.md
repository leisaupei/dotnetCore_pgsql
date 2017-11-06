# dotnetCore_pgsql CodeMarker
>感谢两位给予的帮助</br>
>https://github.com/2881099</br>
>https://github.com/lianggx

## 使用: 
### 环境配置: 
#### Windows
1. 直接进入dotnetCore_pgsql/bin/Debug/netcoreapp2.0/
2. 编辑build.bat 运行 (参照以下命令)
#### Mac OS
1. 打开终端terminal
2. cd 到目录 dotnetCore_pgsql/bin/Debug/netcoreapp2.0/
3. 编辑执行命令
`dotnet dotnetCore_pgsql.dll -h 127.0.0.1 -p 5432 -u postgres -pw 123456 -d postgres -pool 50 -o /Users/mac/Projects -proj Test`

> 注意: Mac OS用的是路径用的是'/', Windows用的是'\\'

#### 参数
- -h host
- -p port
- -u pgsql用户名
- -pw pgsql密码
- -d datebase
- -pool 数据库连接池
- -o 输出路径output directory
- -proj 项目名称
## 说明:
* 由于此框架为了防止数据库注入问题全面参数化, 所有where条件都是如: `Where("expression1 = {0} and expression2 = {1}",param1,param2)`的重载, 并不能直接`Where("expression1 = param1")`的写法 UpdateDiy与DeleteDiy必须带Where条件,
* 框架生成了DAL层与Model层, Common.db是通用的逻辑封装, 若生成器没有自动生成(生成过db层没有完全删掉的情况是不会复制的), 请自行拷贝一份到项目中. 
* 生成器是自动生成解决方案, 但也可直接生成到项目中, 注意项目间的引用.
* 建数据库与字段名称最好用小写字母加'_'创建

## 数据库支持: 
#### 字段
| PostgreSQL type | 转化的.net类型 | 备注          |
| :-------------: | :-----------: | :-----------: |     
| uuid            | Guid          |               | 
| int2            | short         |               | 
| int4            | int           |               | 
| int8            | long          |               | 
| xml             | string        |               | 
| text            | string        |               | 
| varchar         | string        |               | 
| bpchar(char)    | string        |               | 
| float4          | decimal       |               | 
| float8          | decimal       |               | 
| numeric         | decimal       |               | 
| money           | decimal       |               | 
| json            | JToken        |               | 
| jsonb           | JToken        |               | 
| date            | DateTime      |               | 
| timetz          | DateTime      |               | 
| timestamp       | DateTime      |               | 
| timestamptz     | DateTime      |               | 
| time            | TimeSpan      |               | 
| interval        | TimeSpan      |               | 
| bool            | bool          |               | 
| line            | NpgsqlLine    |               | 
| point           | NpgsqlPoint   |               | 
| polygon         | NpgsqlPolygon |               | 
| box             | Npgsqlbox     |               | 
| circle          | NpgsqlCircle  |               | 
| geometry        | 支持二维地理信息 |  自动生成{字段名}\_x/y与 {字段名}\_srid不需要自己定义 | 
| (enum type)     | -             | 自定义枚举类型 | 
| (array type)    | -             | 支持二维数组 | 
#### 功能
| 功能     | 支持    |
| :-----: | :-----: | 
| 事务     | √      |
| 主键     | √      |
| 外键     | √      |
| 一对多   | √      |
| 多对一   | √      |
| 多对多   | √      |
| 存储过程  | -       |
#### 其他:
## 用法:
> 以下的写法只是列举一部分, 自己很容易能重载, 拓展

### Select:
```c#
// 条件查询 '()'里面是或(or)关系 where之间是且(and)关系
// select * from people.student where (age =10 or age = 12) and name = ('name1' or name = 'name2')
People_studentModel stu1 = People_student.Query.WhereAge(10, 12).WhereName("name1","name2").ToOne(); 
// select * from people.student where age = 10
People_studentModel stu2 = People_student.Query.Where("age = {0}", 10).ToOne();
// select * from people.student where (age = 10 or age = 20)
People_studentModel stu3 = People_student.Query.WhereOr("age = {0}", new int[] { 10, 20 }).ToOne();
// ToOne()返回单条; ToList()返回多条
List<People_studentModel> stu4 = People_student.Query.WhereAge(10).ToList();

// 转化为dictionary类型 格式化数据
// ToBsonOne()用于单个模型 ToBson()用于模型列表List<Model> 与 Model[]
System.Collections.IDictionary[]  idict  = stu3.ToBson();

// 多表查询 支持 INNER JOIN, LEFT JOIN, RIGHT JOIN, LEFT OUTER JOIN, RIGHT OUTER JOIN
//select *from people.student a join people.teacher b on a.teacher_id = b.id limit 1
People_studentModel stu5 = People_student.Query
    .InnerJoin<People_teacher>("b","a.teacher_id = b.id").ToOne();
//通用
People_student.Query
    .Union<People_teacher>(UnionType.INNER_JOIN,"b","a.teacher_id = b.id").ToOne();

// 自定义查询 返回Tuple <>内支持单/多个属性
(int,string) age1 = People_student.Query.ToTuple<int>("age,name"); // 单条
List<(int,string)> items = People_student.Query.ToTupleList<(int,string)>("age,name"); // 多条

// 导出sql语句(调试用)
People_student.Query.WhereAge(10).ToString(); // 重写ToString()方法 ()内可以写数据库字段名称

// 一对多, 多对一, 多对多关系 详情看Model层
stu1.Obj_people_teacher; //多对一: student表里面有一个teacher_id的外键连接teacher表的主键
teacher1.Obj_people_students; //一对多: 查出该班主任所带的学生
//多对多: student之间course表用中间表 electives的复合主键连接两个表的主键 
stu1.Obj_people_courses; //查出该学生选择了课程
```
### Insert:
```c#
// insert方式 返回实体类
// 1. Model.Insert();
new People_studentModel{ Id = Guid.NewGuid(), Name = "name", Age = 12 }.Insert();
// 2. Insert(Model);
People_student.Insert(new People_studentModel{ Id = Guid.NewGuid(), Name = "name", Age = 12 });
```
### Delete:
```c#
// delete两种方式 返回修改行数
// --------------
// 1. Delete(主键)
People_student.Delete(主键值);
// 2. DeleteDiy 自定义where条件删除
People_student.DeleteDiy.Where("name = {0}","name").Commit();
```
### Update:
```c#
// update两种方式 
// 返回类型也有两种---返回修改的行数.Commit()/返回实体类(需要字段接收).CommitRet()
// --------------
// 1. Update(模型)
People_student.Update(stu1).SetAge(12).SetName("name").Commit(); 
// 2. Update(主键)
People_student.Update(主键值).SetAge(12).SetName("name").Commit(); 
// 3. 自定义where条件Update
People_student.UpdateDiy.SetAge(12).SetName("name1").Where("name = {0}","name").Commit();
// --------------
// 更新数据库并且把更新后的值赋给stu1
stu1 = stu1.UpdateDiy.SetAge(12).CommitRet();
```
### 其他:
```c#
// OrderBy(string) 排序
People_student.Query.OrderBy("name desc").ToList();
// GroupBy(string) 分组
People_student.Query.GroupBy("name").ToList();
// Skip(int) 跳过
People_student.Query.Skip(5).ToList();
// Page(int pageIndex,int pageSize) 分页
People_student.Query.Page(1,10).ToList();
// Limit(int) 返回前x向
People_student.Query.Limit(1).ToList();
// Count(); count
People_student.Query.Count();
```
## 版本更新: 
### v1.0.0
### v1.0.1 -Oct. 24th, 2017 
1. 新增支持line、point、polygon、box,、circle属性，geometry(postgis)空间数据二维存取。

### v1.0.1 -Nov. 6th, 2017 
1. 新增表间一对多、多对一、多对多关系的查询。
2. 新增Redis。见生成后db层RedisHelper.cs。
| jsonb           | JToken        |               | 
