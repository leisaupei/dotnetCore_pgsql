using System;
using DBHelper;
using System.Text;
using Common.CodeFactory;

namespace dotnetCore_pgsql
{
	class Program
	{
		static void Main(string[] args)
		{
			//@"host=localhost;port=5432;user=postgres;pwd=123456;db=superapp;maxpool=50;name=testnew1;path=d:\workspace"
			LetsGo.Produce(args);
			Console.WriteLine("successful...");
			Console.ReadLine();
		}
	}
}
