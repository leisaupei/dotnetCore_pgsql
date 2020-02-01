# Meta dotnetCore+pgsql CodeMarker

# Project for use
- TargetFramework: .net core 3.0+/netstand2.1+
- Npgsql Nuget: npsql 4.1.2
### Windows
1. 直接进入`Meta.Initiator/bin/Debug/netcoreapp3.0/`
2. 编辑`build.bat` 运行 (参照以下命令)
### Mac OS
1. 打开终端terminal
2. cd 到目录`Meta.Initiator/bin/Debug/netcoreapp3.0/`
3. 编辑执行命令

##### Single database
`dotnet Meta.Initiator.dll host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test`
##### Multiple database separated by commas(',')
- `dotnet Meta.Initiator.dll host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test;type=Master,host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test;Type=xxx`
> 注意: Mac OS用的是路径用的是'/', Windows用的是'\\'
##### Parameters
- `host` host
- `port` port
- `user` pgsql用户名
- `pwd` pgsql密码
- `db` 数据库名称
- `maxpool` 数据库连接池
- `path` 输出路径
- `name` 项目名称
- `type` 多库的数据库别名,留空默认master
