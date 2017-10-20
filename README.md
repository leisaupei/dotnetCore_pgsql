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
### Insert:
### Delete:
### Update:
## 版本更新: 
### v-1.0.0
1. 支持Insert Query Update Delete
