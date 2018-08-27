/*
 * ##########################################################
 * #     .net core 2.1+npsql 4.0.2 Postgresql Code Maker    #
 * #                author by leisaupei                     #
 * #      https://github.com/leisaupei/dotnetCore_pgsql     #
 * ##########################################################
 */
using System;
using DBHelper;
using System.Text;
using CodeFactory;

namespace dotnetCore_pgsql
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
#     .net core 2.1+npsql 4.0.2 Postgresql Code Maker    #
#                author by leisaupei                     #
#      https://github.com/leisaupei/dotnetCore_pgsql     #
##########################################################
> Parameters description:
  - host	host
  - port	port
  - user	pgsql username
  - pwd		pgsql password
  - db		database name
  - path	output path
  - name	project name
> Example: host=localhost;port=5432;user=postgres;pwd=123456;db=superapp;name=test;path=d:\workspace
");

			if (!args.IsNullOrEmpty())
				LetsGo.Produce(args[0]);
			else
				LetsGo.Produce(Console.ReadLine());
			Console.WriteLine("successful...");
			Console.ReadLine();
		}
	}
}
