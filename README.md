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
| geometry        | 支持二维地理信息 |  自动生成{字段名}\_x/y与 {字段名}\_srid<br />不需要自己定义 | 
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
| 视图     | √      |
| 存储过程  | -       |
