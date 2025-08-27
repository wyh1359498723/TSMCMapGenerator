using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSMCMapGenerator.Models;

namespace TSMCMapGenerator
{
    public class TsmcRepository
    {
        private readonly string _connStr;

        public TsmcRepository(string connStr)
        {
            _connStr = connStr;
        }

        private DataTable Query(string sql)
        {
            using var conn = new OracleConnection(_connStr);
            using var cmd = new OracleCommand(sql, conn);
            using var adapter = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public bool ExistsInMmsTsmcDevice(string cust, string device)
        {
            string sql = $"SELECT COUNT(1) FROM MMS_TSMC_DEVICE WHERE MTD_CUST='{cust}' AND MTD_DEVICE='{device}'";
            var dt = Query(sql);
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public DataTable GetSplit(string lotId, string wfNo)
        {
            string sql = $"SELECT * FROM RTM_ADMIN.RTM_P_DATA_SPLIT WHERE LOT_ID='{lotId}' AND SPLIT_WF_NO LIKE '%{wfNo}%'";
            return Query(sql);
        }

        public DataTable GetHead(string lotId, string cp)
        {
            string sql = $"SELECT * FROM RTM_ADMIN.RTM_P_DATA_HEAD WHERE LOT_ID='{lotId}' AND CP_NO='{cp}'";
            return Query(sql);
        }

        public List<PDataDetail> GetDetail(string lotId, string cp)
        {
            string sql = $"SELECT * FROM P_DATA_DETAIL WHERE LOT_ID='{lotId}' AND CP_NO='{cp}' ORDER BY TO_NUMBER(WF_NO)";
            var dt = Query(sql);
            return dt.AsEnumerable().Select(r => new PDataDetail
            {
                LOT_ID = r["LOT_ID"].ToString(),
                CP_NO = r["CP_NO"].ToString(),
                WF_NO = r["WF_NO"].ToString(),
                RP_NO = r["RP_NO"].ToString(),
                START_DATE = r["START_DATE"].ToString(),
                END_DATE = r["END_DATE"].ToString(),
                FLAT = r["FLAT"].ToString()
            }).ToList();
        }
    }
}
