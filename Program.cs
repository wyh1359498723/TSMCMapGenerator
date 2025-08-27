using TSMCMapGenerator;
using TSMCMapGenerator.Models;
using TSMCMapGenerator.Services;
using System.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        string connStr = ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
        string outputDir = ConfigurationManager.AppSettings["TsmcMapOutputDir"];

        var repo = new TsmcRepository(connStr);
        var service = new TsmcMapService(repo, outputDir);

        // 从 API 获取 wafers 数据
        var dataFetcher = new StdfDataFetcher();
        string lotId = "ALGT10042.1";
        string cp = "CP1";
        string rp = "RP1";
        string wfno = "1";
        var wafers = await dataFetcher.GetWafersDataAsync(lotId, cp, rp, wfno);

        if (wafers != null && wafers.Any())
        {
            service.Generate("ALG", "GXLX3_B", wafers);
        }
        else
        {
            Console.WriteLine("未能从 API 获取到晶圆数据或数据为空。");
        }

        Console.WriteLine("完成");
    }
}
