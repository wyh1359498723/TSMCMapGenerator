using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using TSMCMapGenerator.Models;
using System.Text.Json;
using System.Configuration;
using Serilog;

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
            try
            {
                var url = $"{_baseUrl}?LotId={Uri.EscapeDataString(lotId)}&Cp={cp}&Rp={rp}&Wfno={wfno}";
                Log.Debug("正在从 API 获取晶圆数据：{Url}", url);
                var response = await _httpClient.GetStringAsync(url);

                Log.Debug("API 已响应");
                
                var wafers = new List<Stdf_BinsGroupModel>();
                
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
            catch (Exception ex)
            {
                Log.Error(ex, "从 STDF API 获取晶圆数据失败。LotId: {LotId}, CP: {Cp}, RP: {Rp}, Wfno: {Wfno}", lotId, cp, rp, wfno);
                return null; 
            }
        }
    }
}
