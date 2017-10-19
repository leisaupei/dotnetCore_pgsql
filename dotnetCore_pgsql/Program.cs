using System;
using Common.db.DBHelper;
using System.Text;
using dotnetCore_pgsql_DevVersion.CodeFactory;

namespace dotnetCore_pgsql
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string projName = string.Empty, outputPath = string.Empty;
            StringBuilder connection = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                var item = args[i].ToLower();
                switch (item)
                {
                    //"Default": "host=localhost;port=5432;username=postgres;password=tanweijie;database=postgres;pooling=true;maximum pool size=100"
                    case "-h": connection.Append($"host={args[i + 1]};"); break;
                    case "-p": connection.Append($"port={args[i + 1]};"); break;
                    case "-u": connection.Append($"username={args[i + 1]};"); break;
                    case "-pw": connection.Append($"password={args[i + 1]};"); break;
                    case "-d": connection.Append($"database={args[i + 1]};"); break;
                    case "-pool": connection.Append($"maximum pool size={args[i + 1]};"); break;
                    case "-proj": projName = args[i + 1]; break;
                    case "-o": outputPath = args[i + 1]; break;
                }
                i++;
            }
            PgSqlHelper.InitDBConnection(null, connection.ToString());
            LetsGo.Build(outputPath, projName);
            Console.WriteLine("successful");
            Console.ReadLine();
        }
    }
}
