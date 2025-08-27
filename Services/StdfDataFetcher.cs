using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using TSMCMapGenerator.Models;
using System.Text.Json;
using System.Configuration;

namespace TSMCMapGenerator.Services
{
    public class StdfDataFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public StdfDataFetcher()
        {
            _httpClient = new HttpClient();
            _baseUrl = ConfigurationManager.AppSettings["StdfApiBaseUrl"];
        }

        public async Task<List<Stdf_BinsGroupModel>> GetWafersDataAsync(string lotId, string cp, string rp, string wfno)
        {
            var url = $"{_baseUrl}?LotId={Uri.EscapeDataString(lotId)}&Cp={cp}&Rp={rp}&Wfno={wfno}";
            var response = await _httpClient.GetStringAsync(url);

            Console.WriteLine("API 原始响应内容：");
            Console.WriteLine(response);

            var wafers = new List<Stdf_BinsGroupModel>();
            
            // 使用 JsonSerializer 反序列化 JSON 响应
            var apiResponse = JsonSerializer.Deserialize<StdfApiResponse>(response);

            if (apiResponse?.stdf_BinsModels != null && apiResponse.stdf_BinsModels.Any())
            {
                // 假设所有 bin 都属于一个晶圆组，基于 JSON 结构
                // 如果 API 返回多个晶圆组，则需要根据 LotId, CP, Wf_No 进行分组
                wafers.Add(new Stdf_BinsGroupModel
                {
                    LotId = lotId, // 假设 LotId 是传递的并且与所有 bin 一致
                    CP = cp,       // 假设 CP 是传递的并且与所有 bin 一致
                    Wf_No = wfno,  // 假设 Wf_No 是传递的并且与所有 bin 一致
                    stdf_BinsModels = apiResponse.stdf_BinsModels
                });
            }

            return wafers;
        }
    }
}
