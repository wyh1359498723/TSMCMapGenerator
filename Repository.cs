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

        public List<LotInfoModel> GetLotInfoForMapGeneration()
        {
            string sql = @"
                select 
                    main.lotid,
                    main.cp,
                    main.rp,
                    main.tester_count,
                    main.allwfno,
                    nvl(b.DEVICE, a.product_no) as device,
                    b.cust_code
                from 
                    (
                        select 
                            lotid,
                            cp,
                            rp,
                            count(1) as tester_count,
                            listagg(wfno, ',') within group (order by to_number(wfno)) as allwfno 
                        from 
                            wip_lot_testerstate  
                        where 
                             step >= 3
                        group by 
                            lotid, cp, rp
                    ) main
                    left join htmms_get_lotinfo b on main.lotid = b.lot_id
                    left join wip_lot a on a.order_no || '.' || a.order_sub_lot = b.lot_id
                where 
                    exists (
                        select 1 
                        from MMS_TSMC_DEVICE mms
                        where mms.MTD_DEVICE = nvl(b.DEVICE, a.product_no)
                          and mms.MTD_CUST = b.cust_code and mms.MTD_CUST='MCB' and mms.MTD_DEVICE='MBB201QB'
                    )";

            var dt = Query(sql);
            return dt.AsEnumerable().Select(r => new LotInfoModel
            {
                LotId = r["LOTID"].ToString(),
                Cp = r["CP"].ToString(),
                Rp = r["RP"].ToString(),
                Tester_Count = Convert.ToInt32(r["TESTER_COUNT"]),
                AllWfNo = r["ALLWFNO"].ToString(),
                Device = r["DEVICE"].ToString(),
                Cust_Code = r["CUST_CODE"].ToString()
            }).ToList();
        }
    }
}
