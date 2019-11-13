using Meta.Common.Model;
using Microsoft.Extensions.Configuration;

namespace Meta.Common
{
	public class HostConfig
	{
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
