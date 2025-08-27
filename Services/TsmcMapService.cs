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

        public TsmcMapService(TsmcRepository repo, string outputDir)
        {
            _repo = repo;
            _outputDir = outputDir;
        }

        /// <summary>
        /// 生成TSMC Map文件
        /// </summary>
        /// <param name="cust">客户代码</param>
        /// <param name="device">Device</param>
        /// <param name="wafers">wafer数据</param>
        public void Generate(string cust, string device, List<Stdf_BinsGroupModel> wafers)
        {
            // ✅ 唯一判断逻辑：cust/device 是否在 MMS_TSMC_DEVICE
            if (!_repo.ExistsInMmsTsmcDevice(cust, device))
            {
                Console.WriteLine($"[SKIP] {cust}-{device} 不在 MMS_TSMC_DEVICE 表中，跳过生成 TSMC Map");
                return;
            }

            Console.WriteLine($"[INFO] {cust}-{device} 在 MMS_TSMC_DEVICE 表中，开始生成 TSMC Map 文件...");

            foreach (var wafer in wafers)
            {
                CreateTsmcMap(cust, device, wafer);
            }
        }

        private void CreateTsmcMap(string cust, string device, Stdf_BinsGroupModel wafer)
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
            var details = _repo.GetDetail(wafer.LotId, wafer.CP);
            if (!details.Any()) return;
            var last = details.Last();
            var startTime = last.START_DATE;
            var endTime = last.END_DATE;
            var flat = last.FLAT;

            // === device 特殊替换规则 ===
            if (device == "MBB201QA") device = "TMPY06A";
            if (device == "MBB201QB") device = "TMPY06B";

            // === 生成 bdfile & testProgram 规则 ===
            string bdfile = new string(' ', 20);
            string ttestprogram = testProgram + ".xlsm";

            if (cust is "TLK" or "NTX" or "HTX" or "XTW" or "ALG")
            {
                ttestprogram = testProgram; // 不加.xlsm
                bdfile = "TMTH51_HTSH_1_CP1".PadRight(20);
            }

            if (device is "TMPY06A" or "TMPY06B")
            {
                ttestprogram = testProgram; // 不加.xlsm
                bdfile = "TMPY06_HTSH_1_1".PadRight(20);
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
            var tmpcardid = cardId.PadRight(12);
            var tmpnorth = flat.Substring(0, 1).ToUpper();

            string header = $"{tmplotid}{tmpwfid}{tmpdevice}{"".PadRight(10)}{tmptester}{tmpoper}{tmptestprogram}{startTime}{endTime}{tmpcardid}{"".PadRight(12)}{bdfile}{tmpnorth}{cp.Substring(2, 1)}{"HTSH    "}{"".PadRight(20)}";

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
            int maxX = wafer.stdf_BinsModels.Select(u => int.Parse(u.X)).Max();
            int maxY = wafer.stdf_BinsModels.Select(u => int.Parse(u.Y)).Max();
            int minX = wafer.stdf_BinsModels.Select(u => int.Parse(u.X)).Min();
            int minY = wafer.stdf_BinsModels.Select(u => int.Parse(u.Y)).Min();

            string[,] arr = new string[maxX + 1, maxY + 1];
            foreach (var die in wafer.stdf_BinsModels)
            {
                arr[int.Parse(die.X), int.Parse(die.Y)] = die.SoftBin;
            }

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
