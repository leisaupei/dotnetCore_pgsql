using Microsoft.Extensions.Configuration;

namespace Meta.xUnitTest.Options
{
    public class DbConfig
    {
        public const int DbCacheTimeOut = 0;
        /// <summary>
        /// 全局配置
        /// </summary>
        public static IConfiguration Configuration { get; private set; }

        public static void SetConfiguration(IConfiguration cfg)
        {
            Configuration = cfg;
        }
    }
}
