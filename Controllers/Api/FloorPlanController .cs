using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    [Route("api/floor-plan")]
    public class FloorPlanController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


 
        /// <summary>
        /// List PR Box Approval
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/floor-plan/data-table")]
        public IHttpActionResult GetData()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];


            Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
            cols.Add("MaterialCode", x => x.MaterialCode);
            cols.Add("MaterialName", x => x.MaterialName);
            cols.Add("LotNumber", x => x.LotNumber);
            cols.Add("InDate", x => x.InDate);
            cols.Add("ExpDate", x => x.ExpiredDate);
            cols.Add("QtyBag", x => x.BagQty);
            cols.Add("QtyPerBag", x => x.QtyPerBag);
            cols.Add("Qty", x => x.BagQty * x.QtyPerBag);
            cols.Add("IsExpired", x => x.IsExpired);
            cols.Add("OnInspect", x => x.OnInspect);



            IQueryable<vStockAll> query = db.vStockAlls.AsQueryable();

            string binRackName = "";
            var binrackParam = HttpContext.Current.Request.QueryString;
            if (binrackParam != null)
            {
                var param = binrackParam.GetValues("binrack");
                if (param != null && param.Length > 0 && !string.IsNullOrEmpty(param.FirstOrDefault()))
                {
                    binRackName = param.FirstOrDefault().ToString();
                }
            }

            if (!string.IsNullOrEmpty(binRackName))
            {
                query = query.Where(m => m.BinRackCode == binRackName && m.Quantity > 0);

            } else
            {
                query = query.Where(m => m.BinRackName == "XXXXXX");
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query
                    .Where(m => m.MaterialCode.Contains(search) || m.MaterialName.Contains(search));
            }


            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            if (sortDirection.Equals("asc"))
                query = query.OrderBy(cols[sortName]).AsQueryable();
            else
                query = query.OrderByDescending(cols[sortName]).AsQueryable();

            recordsFiltered = query.Count();
            var list = query.Skip(start).Take(length).ToList().Select(x => new TableStock()
            {
                Selected = false,
                ID = x.ID,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                InDate = Helper.NullDateToString(x.InDate),
                ExpDate = Helper.NullDateToString(x.ExpiredDate),
                StockCode = x.Code,
                LotNumber = x.LotNumber,
                WarehouseName = x.WarehouseName,
                BinRackCode = x.BinRackCode,
                BinRackName = x.BinRackName,
                BinRackAreaCode = x.BinRackAreaCode,
                BinRackAreaName = x.BinRackAreaName,
                QtyPerBag = x.QtyPerBag,
                Qty = x.Quantity,
                QtyBag = (x.Quantity/ x.QtyPerBag),
                QtyTransfer = 0,
                OnInspect = x.OnInspect,
                IsExpired = Convert.ToBoolean(x.IsExpired)
            }).ToList();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


    }
}
