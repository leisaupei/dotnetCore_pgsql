# dotnetCore_pgsql CodeMarker
>感谢两位给予的帮助</br>
>https://github.com/2881099</br>
>https://github.com/lianggx
## 生成环境:
.net core 2.1+npsql-4.0.2.dll
## 使用: 
### 环境配置: 
#### Windows
1. 直接进入`dotnetCore_pgsql/bin/Debug/netcoreapp2.1/`
2. 编辑`build.bat` 运行 (参照以下命令)
#### Mac OS
1. 打开终端terminal
2. cd 到目录`dotnetCore_pgsql/bin/Debug/netcoreapp2.1/`
3. 编辑执行命令
`dotnet dotnetCore_pgsql.dll host=localhost;port=5432;user=postgres;pwd=123456;db=superapp;maxpool=50;name=testnew1;path=d:\workspace`

> 注意: Mac OS用的是路径用的是'/', Windows用的是'\\'

#### 参数
- `host` host
- `port` port
- `user` pgsql用户名
- `pwd` pgsql密码
- `db` 数据库名称
- `maxpool` 数据库连接池
- `path` 输出路径
- `name` 项目名称

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
| float4          | float         |               | 
| float8          | double        |               | 
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
| 一对一   | √      |
| 多对一   | √      |
| 视图     | √      |
| 存储过程  | -       |
