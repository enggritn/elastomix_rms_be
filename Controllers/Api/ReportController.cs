using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WMS_BE.Models;
using WMS_BE.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;

namespace WMS_BE.Controllers.Api
{
    public class ReportController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableExpiredStock(int year, int month, int day)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<StockRMDetailed> list = Enumerable.Empty<StockRMDetailed>();
        //    IEnumerable<ExpiredStockDTO> pagedData = Enumerable.Empty<ExpiredStockDTO>();

        //    IQueryable<StockRMDetailed> query = db.StockRMDetaileds.Where(s => s.ExpDate.Year == year).AsQueryable();

        //    if (month > 0)
        //    {
        //        query = query.Where(s => s.ExpDate.Month == month).AsQueryable();

        //        if (day > 0)
        //        {
        //            query = query.Where(s => s.ExpDate.Day == day).AsQueryable();
        //        }
        //    }

        //    int recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        query = query
        //                .Where(m => m.MaterialCode.Contains(search)
        //                || m.MaterialName.Contains(search)
        //                || m.Barcode.Contains(search)
        //                || m.LotNo.Contains(search)
        //                );

        //        Dictionary<string, Func<StockRMDetailed, object>> cols = new Dictionary<string, Func<StockRMDetailed, object>>();
        //        cols.Add("Barcode", x => x.Barcode);
        //        cols.Add("MaterialCode", x => x.MaterialCode);
        //        cols.Add("MaterialName", x => x.MaterialName);
        //        cols.Add("LotNo", x => x.LotNo);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpDate", x => x.ExpDate);
        //        cols.Add("ReceivedAt", x => x.ReceivedAt);
        //        cols.Add("BinRackCode", x => x.BinRackCode);
        //        cols.Add("BinRackName", x => x.BinRackName);
        //        cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
        //        cols.Add("BinRackAreaName", x => x.BinRackAreaName);
        //        cols.Add("WarehouseCode", x => x.WarehouseCode);
        //        cols.Add("WarehouseName", x => x.WarehouseName);

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {

        //            pagedData = from x in list
        //                        select new ExpiredStockDTO
        //                        {
        //                            Barcode = x.Barcode,
        //                            MaterialCode = x.MaterialCode,
        //                            MaterialName = x.MaterialName,
        //                            LotNo = x.LotNo,
        //                            InDate = Helper.NullDateToString2(x.InDate),
        //                            ExpDate = Helper.NullDateToString2(x.ExpDate),
        //                            ReceivedAt = Helper.NullDateToString2(x.ReceivedAt),
        //                            BinRackCode = x.BinRackCode,
        //                            BinRackName = x.BinRackName,
        //                            BinRackAreaCode = x.BinRackAreaCode,
        //                            BinRackAreaName = x.BinRackAreaName,
        //                            WarehouseCode = x.WarehouseCode,
        //                            WarehouseName = x.WarehouseName
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
        //    }
        //    catch (HttpRequestException reqpEx)
        //    {
        //        message = reqpEx.Message;
        //        return BadRequest();
        //    }
        //    catch (HttpResponseException respEx)
        //    {
        //        message = respEx.Message;
        //        return NotFound();
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        [HttpPost]
        public async Task<IHttpActionResult> DatatableActualStock()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            #region set search filter
            HttpRequest request = HttpContext.Current.Request;
            string warehouseCode = request["warehouseCode"];
            string areaCode = request["areaCode"];
            string binRackCode = request["binRackCode"];
            #endregion

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            IEnumerable<ActualStockDTO> pagedData = Enumerable.Empty<ActualStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(m => m.Quantity > 0).AsQueryable();

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                query = query.Where(m => m.WarehouseCode.Equals(warehouseCode));
            }

            if (!string.IsNullOrEmpty(areaCode))
            {
                query = query.Where(m => m.BinRackAreaCode.Equals(areaCode));
            }

            if (!string.IsNullOrEmpty(binRackCode))
            {
                query = query.Where(m => m.BinRackCode.Equals(binRackCode));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                         || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("LotNo", x => x.LotNumber);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
                cols.Add("WarehouseCode", x => x.WarehouseCode);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("InDate", x => Helper.NullDateToString(x.InDate));
                cols.Add("ExpDate", x => Helper.NullDateToString(x.ExpiredDate));
                cols.Add("BagQty", x => x.BagQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("TotalQty", x => x.BagQty * x.QtyPerBag);
                cols.Add("IsExpired", x => x.IsExpired);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new ActualStockDTO
                                {
                                    Barcode = x.Code,
                                    LotNo = x.LotNumber,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    BinRackAreaCode = x.BinRackAreaCode,
                                    BinRackAreaName = x.BinRackAreaName,
                                    WarehouseCode = x.WarehouseCode,
                                    WarehouseName = x.WarehouseName,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                    BagQty = Helper.FormatThousand(x.BagQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                    IsExpired = Convert.ToBoolean(x.IsExpired)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableActualStockRePrintLabel()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            #region set search filter
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            string lotNo = request["LotNo"].ToString();
            string materialName = request["MaterialName"].ToString();
            DateTime filterInDate = new DateTime();
            DateTime filterExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;
            #endregion

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false; 

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            IEnumerable<ActualStockDTO> pagedData = Enumerable.Empty<ActualStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(m => m.Quantity > 0).AsQueryable();

            if (!string.IsNullOrEmpty(materialName))
            {
                query = query.Where(m => m.MaterialName.Equals(materialName));
            }
            if (!string.IsNullOrEmpty(lotNo))
            {
                query = query.Where(m => m.LotNumber.Equals(lotNo));
            }
            if (DateTime.TryParse(inDate, out temp))
            {;
                filterInDate = Convert.ToDateTime(inDate);
                query = query.Where(m => DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(filterInDate));
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                filterExpDate = Convert.ToDateTime(expDate);
                query = query.Where(m => DbFunctions.TruncateTime(m.ExpiredDate) == DbFunctions.TruncateTime(filterExpDate));                
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                         || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("LotNo", x => x.LotNumber);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
                cols.Add("WarehouseCode", x => x.WarehouseCode);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("InDate", x => Helper.NullDateToString(x.InDate));
                cols.Add("ExpDate", x => Helper.NullDateToString(x.ExpiredDate));
                cols.Add("BagQty", x => x.BagQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("TotalQty", x => x.BagQty * x.QtyPerBag);
                cols.Add("IsExpired", x => x.IsExpired);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new ActualStockDTO
                                {
                                    ID = x.ID,
                                    Barcode = x.Code,
                                    LotNo = x.LotNumber,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    BinRackAreaCode = x.BinRackAreaCode,
                                    BinRackAreaName = x.BinRackAreaName,
                                    WarehouseCode = x.WarehouseCode,
                                    WarehouseName = x.WarehouseName,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                    BagQty = Helper.FormatThousand(x.BagQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                    IsExpired = Convert.ToBoolean(x.IsExpired)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
