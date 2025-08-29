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

            Console.WriteLine("API 已响应");
            

            var wafers = new List<Stdf_BinsGroupModel>();
            
            // 使用 JsonSerializer 反序列化 JSON 响应
            var apiResponse = JsonSerializer.Deserialize<StdfApiResponse>(response);

            if (apiResponse?.stdf_BinsModels != null && apiResponse.stdf_BinsModels.Any())
            {
                wafers.Add(new Stdf_BinsGroupModel
                {
                    LotId = lotId, 
                    CP = cp,       
                    Wf_No = wfno,  
                    stdf_BinsModels = apiResponse.stdf_BinsModels
                });
            }

            return wafers;
        }
    }
}
