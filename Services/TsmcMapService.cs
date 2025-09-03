using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSMCMapGenerator.Models;

namespace TSMCMapGenerator.Services
{
    public class TsmcMapService
    {
        private readonly TsmcRepository _repo;
        private readonly string _outputDir;
        private readonly StdfDataFetcher _dataFetcher;

        public TsmcMapService(TsmcRepository repo, string outputDir, StdfDataFetcher dataFetcher)
        {
            _repo = repo;
            _outputDir = outputDir;
            _dataFetcher = dataFetcher;
        }

        /// <summary>
        /// 生成TSMC Map文件
        /// </summary>
        /// <param name="cust">客户代码</param>
        /// <param name="device">Device</param>
        /// <param name="rpForCurrentWafer">当前晶圆的RP</param>
        /// <param name="wafers">wafer数据</param>
        public async Task Generate(string cust, string device, string rpForCurrentWafer, List<Stdf_BinsGroupModel> wafers)
        {
            Console.WriteLine($"[INFO] {cust}-{device} 在 MMS_TSMC_DEVICE 表中，开始生成 TSMC Map 文件...");

            foreach (var wafer in wafers)
            {
                await CreateTsmcMap(cust, device, rpForCurrentWafer, wafer);
            }
        }

        private async Task CreateTsmcMap(string cust, string device, string rpForCurrentWafer, Stdf_BinsGroupModel wafer)
        {
            // === 查询 Split 信息 ===
            var splitDt = _repo.GetSplit(wafer.LotId, wafer.Wf_No);
            if (splitDt.Rows.Count == 0) return;

            var lotNo = splitDt.Rows[0]["LOT_ID"].ToString();
            var tester = splitDt.Rows[0]["TESTER"].ToString();
            var prober = splitDt.Rows[0]["PROBER"].ToString();
            var cardId = splitDt.Rows[0]["CARD_ID"].ToString();
            var testProgram = splitDt.Rows[0]["TEST_P"].ToString();

            // === 查询 Head 信息 ===
            var headDt = _repo.GetHead(wafer.LotId, wafer.CP);
            var cp = headDt.Rows[0]["CP_NO"].ToString();
            var wfLot = headDt.Rows[0]["WF_LOT"].ToString();
            var headDevice = headDt.Rows[0]["DEVICE"].ToString();

            // === P_DATA_DETAIL 获取时间 & flat ===


            var details = _repo.GetDetail(wafer.LotId, wafer.CP,wafer.Wf_No);
            if (details.Count==0) return;
            
            var detail = details.First();
            
            var startTime = detail.START_DATE;
            var endTime = detail.END_DATE;
            var flat = detail.FLAT;

            // 获取 PDataDetailTestInfo
            
            var pDataDetailTestInfo = _repo.GetPDataDetailTestInfo(detail.ID);

            // === device 特殊替换规则 ===
            if (device == "MBB201QA") device = "TMPY06A";
            if (device == "MBB201QB") device = "TMPY06B";

            // === 生成 bdfile & testProgram 规则 ===
            string bdfile = new string(' ', 20);
            string ttestprogram = testProgram + ".xlsm";
            if(cust is "ALG")
            {
                ttestprogram = testProgram; // 不加.xlsm
            }

            if (cust is "TLK" or "NTX" or "HTX" or "XTW" )
            {
                ttestprogram = testProgram; // 不加.xlsm
                bdfile = "TMTH51_HTSH_1_CP1".PadRight(20);
            }

            if (device is "TMPY06A" or "TMPY06B")
            {
                ttestprogram = testProgram; // 不加.xlsm
                bdfile = "TMPY06_HTSH_1_1".PadRight(18);
            }

            if (cust == "BKT")
            {
                bdfile = $"{wfLot}-{cp.Substring(cp.Length - 1)}-{wafer.Wf_No.PadLeft(2, '0')}".PadRight(20);
            }

            // === testProgram 长度限制 ===
            string tmptestprogram;
            if (ttestprogram.Length <= 30)
                tmptestprogram = ttestprogram.PadRight(30);
            else
                tmptestprogram = ttestprogram.Substring(0, 30);

            // === 拼接 header ===
            var tmplotid = wfLot.PadRight(12);
            var tmpwfid = wafer.Wf_No.PadLeft(2, '0');
            var tmpdevice = device.PadRight(32);
            var tmptester = tester.PadRight(8);
            var tmpoper = splitDt.Rows[0]["OPEN_EMPLOYEE"].ToString().PadRight(8);
            var tmpoper_lastFour=tmpoper.Substring(tmpoper.Length - 4).PadRight(8);
            var tmpcardid = cardId.PadRight(12);
            var tmpnorth = flat.Substring(0, 1).ToUpper();

            string header = $"{tmplotid}{tmpwfid}{tmpdevice}{"".PadRight(10)}{tmptester}{tmpoper_lastFour}{tmptestprogram}{startTime}{endTime}{tmpcardid}{"".PadRight(12)}{bdfile}{tmpnorth}{cp.Substring(2, 1)}{"HTSH    "}{"".PadRight(20)}";
            

            // === 写文件 ===
            string mapPath = Path.Combine(_outputDir, lotNo.Substring(0, 3), device.Trim(), wfLot.Trim(), "FINAL", "TSMC");
            Directory.CreateDirectory(mapPath);

            string fileName = $"{wfLot}-{cp.Substring(cp.Length - 1)}-{wafer.Wf_No.PadLeft(2, '0')}";
            string filePath = Path.Combine(mapPath, fileName);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(fs);

            writer.WriteLine(header);

            if (ttestprogram.Length > 30)
            {
                writer.WriteLine($"#TEST_PROGRAM={ttestprogram}");
            }

            // === Die 坐标写入 ===
            int maxX = 0;
            int maxY = 0;
            int minX = int.MaxValue; // Initialize with max value
            int minY = int.MaxValue; // Initialize with max value
            bool hasDatCoordinates = false;
            bool hasApiCoordinates = false;

            // 1. 从 PositionBin 数据确定地图边界
            List<Tuple<int, int, string, int>> datCoords = new List<Tuple<int, int, string, int>>();
            if (pDataDetailTestInfo != null && !string.IsNullOrWhiteSpace(pDataDetailTestInfo.PositionBin))
            {
                List<string> listcoords1 = pDataDetailTestInfo.PositionBin.Split('|').ToList();
                foreach (var v in listcoords1)
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        List<string> vs = v.Split(',').ToList();
                        if (vs.Count >= 4 && int.Parse(vs[3])!=0)
                        {
                            int x = int.Parse(vs[0]);
                            int y = int.Parse(vs[1]);
                            string hardBin = vs[2];
                            int testResult = int.Parse(vs[4]);
                            datCoords.Add(Tuple.Create(x, y, hardBin, testResult));

                            maxX = Math.Max(maxX, x);
                            maxY = Math.Max(maxY, y);
                            minX = Math.Min(minX, x);
                            minY = Math.Min(minY, y);
                            hasDatCoordinates = true;
                        }
                    }
                }
            }

            // 2. 从 API 数据确定边界，并合并到总边界
            if (wafer.stdf_BinsModels != null && wafer.stdf_BinsModels.Any())
            {
                maxX = Math.Max(maxX, wafer.stdf_BinsModels.Select(u => int.Parse(u.X)).Max());
                maxY = Math.Max(maxY, wafer.stdf_BinsModels.Select(u => int.Parse(u.Y)).Max());
                minX = Math.Min(minX, wafer.stdf_BinsModels.Select(u => int.Parse(u.X)).Min());
                minY = Math.Min(minY, wafer.stdf_BinsModels.Select(u => int.Parse(u.Y)).Min());
                hasApiCoordinates = true;
            }


            // 如果没有任何坐标数据，则无法生成地图
            if (!hasDatCoordinates && !hasApiCoordinates)
            {
                Console.WriteLine($"[WARNING] 无法获取 {wafer.LotId}-{wafer.Wf_No} 的任何坐标数据，跳过生成 TSMC Map。");
                return;
            }
            
            string[,] arr = new string[maxX + 1, maxY + 1];

            // 3. 写入 PositionBin 数据 (来自 PDataDetailTestInfo)
            foreach (var coord in datCoords)
            {
                int X_COORD = coord.Item1;
                int Y_COORD = coord.Item2;
                string HARD_BIN = coord.Item3;
                int _testResult = coord.Item4;

                // 应用 HardBin 调整逻辑
                if (_testResult > 0 && pDataDetailTestInfo.FirstBin == 0)
                {
                    if (!string.IsNullOrWhiteSpace(HARD_BIN))
                    {
                        HARD_BIN = (Convert.ToInt32(HARD_BIN) + 1).ToString();
                    }
                }
                
                // 确保坐标在数组范围内
                if (X_COORD >= minX && X_COORD <= maxX && Y_COORD >= minY && Y_COORD <= maxY)
                {
                    arr[X_COORD, Y_COORD] = HARD_BIN;
                }
            }

            // 4. 覆盖历史 CP 的 SoftBin 数据
            // 从当前 wafer.CP 中提取 CP 编号 (例如，"CP5" 中的 5)
            int currentCpNumber = 0;
            if (wafer.CP.StartsWith("CP") && int.TryParse(wafer.CP.Substring(2), out currentCpNumber))
            {
                for (int i = 1; i < currentCpNumber; i++)
                {
                    string previousCpString = $"CP{i}";
                    // 获取此历史 CP 对应的最新 RP
                    string latestRpForHistoricalCp = _repo.GetLatestRpForCp(wafer.LotId, previousCpString);

                    if (!string.IsNullOrWhiteSpace(latestRpForHistoricalCp))
                    {
                        Console.WriteLine($"[INFO]   正在获取历史晶圆数据 LotId: {wafer.LotId}, CP: {previousCpString}, RP: {latestRpForHistoricalCp}, Wfno: {wafer.Wf_No}");
                        var historicalWafers = await _dataFetcher.GetWafersDataAsync(wafer.LotId, previousCpString, latestRpForHistoricalCp, wafer.Wf_No);

                        if (historicalWafers != null && historicalWafers.Any())
                        {
                            foreach (var historicalWafer in historicalWafers)
                            {
                                foreach (var die in historicalWafer.stdf_BinsModels)
                                {
                                    int dieX = int.Parse(die.X);
                                    int dieY = int.Parse(die.Y);
                                    if (dieX >= minX && dieX <= maxX && dieY >= minY && dieY <= maxY)
                                    {
                                        arr[dieX, dieY] = die.SoftBin;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[WARNING] 未能从 API 获取到历史晶圆数据或数据为空。LotId: {wafer.LotId}, CP: {previousCpString}, RP: {latestRpForHistoricalCp}, Wfno: {wafer.Wf_No}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[INFO] 未找到 LotId: {wafer.LotId}, CP: {previousCpString} 的最新 RP，跳过获取历史晶圆数据。");
                    }
                }
            }


            // 5. 覆盖当前 CP 的 SoftBin 数据
            if (wafer.stdf_BinsModels != null && wafer.stdf_BinsModels.Any())
            {
                foreach (var die in wafer.stdf_BinsModels)
                {
                    int dieX = int.Parse(die.X);
                    int dieY = int.Parse(die.Y);
                    // 确保坐标在数组范围内
                    if (dieX >= minX && dieX <= maxX && dieY >= minY && dieY <= maxY)
                    {
                        arr[dieX, dieY] = die.SoftBin;
                    }
                }
            }

            // 6. 写入文件
            for (int sRow = minX; sRow <= maxX; sRow++)
            {
                for (int sCol = minY; sCol <= maxY; sCol++)
                {
                    var bin = arr[sRow, sCol];
                    if (!string.IsNullOrWhiteSpace(bin))
                    {
                        int tdieX = sRow - minX;
                        int tdieY = (maxY - minY) - (sCol - minY);
                        writer.WriteLine($"{tdieX.ToString().PadLeft(4)}{tdieY.ToString().PadLeft(4)}{bin.PadLeft(4)}{"0".PadLeft(4)}");
                    }
                }
            }
        }
    }
}
