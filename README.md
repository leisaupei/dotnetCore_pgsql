# dotnetCore_pgsql CodeMarker
## 使用: 
### 环境配置: 
#### Mac OS
1. 打开终端terminal
2. cd 到目录 dotnetCore_pgsql/bin/Debug/netcoreapp2.0/
3. 编辑执行命令
dotnet dotnetCore_pgsql.dll -h 127.0.0.1 -p 5432 -u postgres -pw 123456 -d postgres -pool 50 -o /Users/mac/Projects -proj Test
#### Windows
1. 直接进入dotnetCore_pgsql/bin/Debug/netcoreapp2.0/
2. 编辑build.bat 运行
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
* 由于此框架为了防止数据库注入问题全面参数化所有where条件都是如: Where("expression1 = {0} and expression2 = {1}",param1,param2)的重载, 并不能直接Where("expression1 = param1")的写法
* 框架生成了DAL层与Model层, Common.db是通用的逻辑封装, 若生成器没有自动生成(生成过db层没有完全删掉的情况是不会复制的), 请自行拷贝一份到项目中. 
* 生成器是自动生成解决方案, 但也可直接生成到项目中, 注意项目间的引用. 
## 支持数据库字段类型: 
| PostgreSQL type | 转化的.net类型 |
| :-------------: | :-----------: | 
| uuid            | Guid          | 
| int2            | short         | 
| int4            | int           | 
| int8            | long          | 
| xml             | string        | 
| text            | string        | 
| varchar         | string        | 
| bpchar(char)    | string        | 
| float4          | decimal       | 
| float8          | decimal       | 
| numeric         | decimal       | 
| money           | decimal       | 
| json            | JToken        | 
| jsonb           | JToken        | 
| date            | DateTime      | 
| timetz          | DateTime      | 
| timestamp       | DateTime      | 
| timestamptz     | DateTime      | 
| time            | TimeSpan      | 
| interval        | TimeSpan      | 
| bool            | bool          | 
| (enum type)     | -             |
| (array type)    | -             |
## 用法:
### Select:
```
// 条件查询 '()'里面是或(or)关系 where之间是且(and)关系
// select * from people.student where (age =10 or age = 12) and name = ('name1' or name = 'name2')
People_studentModel stu1 = People_student.Query.WhereAge(10, 12).WhereName("name1","name2").ToOne(); 
// select * from people.student where age = 10
People_studentModel stu2 = People_student.Query.Where("age = {0}", 10).ToOne();
// select * from people.student where (age = 10 or age = 20)
People_studentModel stu3 = People_student.Query.WhereOr("age = {0}", new int[] { 10, 20 }).ToOne();
List<People_studentModel> stu4 = People_student.Query.WhereAge(10).ToList();// ToOne()返回单条;ToList()返回多条

// 转化为dictionary类型 格式化数据
// ToBsonOne()用于单个模型 ToBson()用于模型列表List<Model>
System.Collections.IDictionary[]  idict  = stu3.ToBson();

// 多表查询 支持 INNER JOIN, LEFT JOIN, RIGHT JOIN, LEFT OUTER JOIN, RIGHT OUTER JOIN
//select *from people.student a join people.teacher b on a.teacher_id = b.id limit 1
People_studentModel stu5 = People_student.Query.InnerJoin<People_teacherModel>("b","a.teacher_id = b.id").ToOne();
People_student.Query.Union<People_studentModel>(UnionType.INNER_JOIN,"b","a.teacher_id = b.id").ToOne();

// 返回元组
int age1 = People_student.Query.ToTuple<int>("age"); // 单条
List<int> age1 = People_student.Query.ToTupleList<int>("age"); // 多条
(int,string) age1 = People_student.Query.ToTuple<(int,string)>("age","name");
List<(int,string)> age1 = People_student.Query.ToTupleList<(int,string)>("age,name");

// 导出sql语句(调试用)
People_student.Query.WhereAge(10).ToString(); // 重写ToString()方法 ()内可以写数据库字段名称
```
### Insert:
```
// insert方式 返回实体类
// 1. Model.Insert();
new People_studentModel{ Id = Guid.NewGuid(), Name = "name", Age = 12 }.Insert();
// 2. Insert(Model);
People_student.Insert(new People_studentModel{ Id = Guid.NewGuid(), Name = "name", Age = 12 });
```
### Delete:
```
// delete两种方式 返回修改行数
// --------------
// 1. Delete(主键)
People_student.Delete(主键值);
// 2. DeleteDiy 自定义where条件删除
People_student.DeleteDiy.Where("name = {0}","name").Commit();
```
### Update:
```
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
Order By 排序
Group By 分组
Skip 跳过
Page(pageIndex,pageSize) 分页
等等
## 版本更新: 
### v-1.0.0
1. 支持Insert Query Update Delete
