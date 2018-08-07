/* ##########################################################
 * #         .net core 2.1+postgres Code Maker              #
 * #                author by leisaupei                     #
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
			Console.WriteLine(@"
##########################################################
#         .net core 2.1+postgres Code Maker              #
#                author by leisaupei                     #
##########################################################
> Parameters description:
  - host	host
  - port	port
  - user	pgsql username
  - pwd		pgsql password
  - db		database name
  - path	output path
  - name	project name
> Example: host=localhost;port=5432;user=postgres;pwd=123456;db=superapp;name=testnew1;path=d:\workspace
");
			LetsGo.Produce(args);
			Console.WriteLine("successful...");
			Console.ReadLine();
		}
	}
}
