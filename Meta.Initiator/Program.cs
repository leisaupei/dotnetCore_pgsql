/*
 * ##########################################################
 * #     .net core 3.0 + npsql Postgresql Code Maker        #
 * #                author by leisaupei                     #
 * #      https://github.com/leisaupei/dotnetCore_pgsql     #
 * ##########################################################
 */
using Meta.Postgres.Generator.CodeFactory;
using System;
using System.Text;

namespace Meta.Initiator
{
	class Program
	{
		static void Main(string[] args)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Console.OutputEncoding = Encoding.GetEncoding("UTF-8");
			Console.InputEncoding = Encoding.GetEncoding("UTF-8");
			Console.WriteLine(@"
##########################################################
#     .net core 3.0 + ngpsql Postgresql Code Maker       #
#                author by leisaupei                     #
#          https://github.com/leisaupei/meta             #
##########################################################
> Parameters description:
  - host	host
  - port	port
  - user	pgsql username
  - pwd		pgsql password
  - db		database name
  - path	output path
  - name	project name
  - type    database enum type name, 'master' if only one. *not required
> Example: host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;name=test;path=d:\workspace\test;type=master

> Multiple Example, slipt from ',': 
	host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;name=test;path=d:\workspace\test;type=master,host=localhost;port=5432;user=postgres;pwd=123456;db=postgres;name=test;path=d:\workspace\test;type=xxx
");
			if (args?.Length > 0)
			{
				if (args[0].Contains(','))
				{
					var connStringArray = args[0].Split(',');
					if (connStringArray.Length == 0)
					{
						Console.WriteLine("length of the connection string array is 0");
						return;
					}
					var finalConnString = connStringArray[^1];
					LetsGo.FinalType = GetGenerateModel(finalConnString).TypeName;

					for (int i = 0; i < connStringArray.Length; i++)
					{
						var model = GetGenerateModel(connStringArray[i]);
						LetsGo.Produce(model);
					}
				}
				else
				{
					var model = GetGenerateModel(args[0]);
					LetsGo.FinalType = model.TypeName;
					LetsGo.Produce(model);
				}
			}
			else
			{
				LetsGo.Produce(GetGenerateModel(Console.ReadLine()));
			}
			Console.WriteLine("successful...");
		}

		/// <summary>
		/// 构建数据库连接实体
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>

		public static GenerateModel GetGenerateModel(string args)
		{
			GenerateModel model = new GenerateModel();
			var strings = args.Split(';');
			string ToUpperPascal(string s) => string.IsNullOrEmpty(s) ? s : $"{ s.Substring(0, 1).ToUpper()}{s.Substring(1)}";
			foreach (var item in strings)
			{
				var sp = item.Split('=');
				var left = sp[0];
				var right = sp[1];
				switch (left.ToLower())
				{
					case "host": model.ConnectionString += $"host={right};"; break;
					case "port": model.ConnectionString += $"port={right};"; break;
					case "user": model.ConnectionString += $"username={right};"; break;
					case "pwd": model.ConnectionString += $"password={right};"; break;
					case "db": model.ConnectionString += $"database={right};"; break;
					case "maxpool": model.ConnectionString += $"maximum pool size={right};pooling=true;"; break;
					case "name": model.ProjectName = right; break;
					case "path": model.OutputPath = right; break;
					case "type": model.TypeName = ToUpperPascal(string.IsNullOrEmpty(right) ? GenerateModel.MASTER_DATABASE_TYPE_NAME : right); break;
				}
			}
			FileInitHelper.GenerateCsproj(model.OutputPath, model.ProjectName);
			FileInitHelper.GenerateDbConfig(model.OutputPath, model.ProjectName);
			//FileInitHelper.CreateCsproj(model.OutputPath);
			FileInitHelper.CreateSln(model.OutputPath, model.ProjectName);
			return model;
		}
	}

}
