# Meta dotnetCore+pgsql CodeMarker
## 生成环境:
.net core 3.0+npsql-4.1.1.dll
## 使用: 
### 环境配置: 
#### Windows
1. 直接进入`Meta.App/bin/Debug/netcoreapp3.0/`
2. 编辑`build.bat` 运行 (参照以下命令)
#### Mac OS
1. 打开终端terminal
2. cd 到目录`Meta.App/bin/Debug/netcoreapp3.0/`
3. 编辑执行命令
`dotnet Meta.App.dll host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test`
> 注意: Mac OS用的是路径用的是'/', Windows用的是'\\'

#### 多库
- 使用','分隔
- `dotnet Meta.App.dll host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test;type=Master,host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;maxpool=50;name=Test;path=d:\workspace\Test;Type=xxx`
#### 参数
- `host` host
- `port` port
- `user` pgsql用户名
- `pwd` pgsql密码
- `db` 数据库名称
- `maxpool` 数据库连接池
- `path` 输出路径
- `name` 项目名称
- `type` 多库数据,留空默认master
