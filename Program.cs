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
        var dataFetcher = new StdfDataFetcher();

        // 获取需要生成map的批次信息
        List<LotInfoModel> lotsToProcess = repo.GetLotInfoForMapGeneration();

        if (lotsToProcess == null || !lotsToProcess.Any())
        {
            Console.WriteLine("没有找到需要生成 TSMC Map 的批次信息。");
            return;
        }

        foreach (var lotInfo in lotsToProcess)
        {
            Console.WriteLine($"[INFO] 开始处理批次: {lotInfo.LotId}, 设备: {lotInfo.Device}, CP: {lotInfo.Cp}, RP: {lotInfo.Rp}");

            string[] wfNos = lotInfo.AllWfNo.Split(',');

            foreach (string wfno in wfNos)
            {
                Console.WriteLine($"[INFO]   正在获取晶圆数据 LotId: {lotInfo.LotId}, CP: {lotInfo.Cp}, RP: {lotInfo.Rp}, Wfno: {wfno}");
                var wafers = await dataFetcher.GetWafersDataAsync(lotInfo.LotId, lotInfo.Cp, lotInfo.Rp, wfno);

                if (wafers != null && wafers.Any())
                {
                    // 使用从数据库获取的 cust_code 和 device
                    service.Generate(lotInfo.Cust_Code, lotInfo.Device, wafers);
                }
                else
                {
                    Console.WriteLine($"[WARNING] 未能从 API 获取到晶圆数据或数据为空。LotId: {lotInfo.LotId}, CP: {lotInfo.Cp}, RP: {lotInfo.Rp}, Wfno: {wfno}");
                }
            }
        }

        Console.WriteLine("所有 TSMC Map 生成任务完成。");
    }
}
