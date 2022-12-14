//using ExcelDataReader;
//using System;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Data;
//using System.Data.Entity;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Http;
//using WMS_BE.Models;
//using WMS_BE.Utils;
//using OfficeOpenXml;
//using OfficeOpenXml.Style;
//using System.Drawing;

//namespace WMS_BE.Controllers.Api
//{
//    public class IssueSlipController : ApiController
//    {
//        private EIN_WMSEntities db = new EIN_WMSEntities();

//        [HttpPost]
//        public async Task<IHttpActionResult> Datatable()
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<IssueSlipHeader> list = Enumerable.Empty<IssueSlipHeader>();
//            IEnumerable<IssueSlipHeaderDTO> pagedData = Enumerable.Empty<IssueSlipHeaderDTO>();

//            IQueryable<IssueSlipHeader> query = db.IssueSlipHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

//            int recordsTotal = db.IssueSlipHeaders.Where(s => !s.TransactionStatus.Equals("CLOSED")).Count();
//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Code.Contains(search)
//                         || m.Name.Contains(search)
//                         //|| m.TotalRequestedQty.Contains(search)
//                         || m.CreatedBy.Contains(search)
//                         || m.ModifiedBy.Contains(search)
//                        );

//                Dictionary<string, Func<IssueSlipHeader, object>> cols = new Dictionary<string, Func<IssueSlipHeader, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Code", x => x.Code);
//                cols.Add("Name", x => x.Name);
//                cols.Add("TotalRequestedQty", x => x.TotalRequestedQty);
//                cols.Add("TransactionStatus", x => x.TransactionStatus);
//                cols.Add("CreatedBy", x => x.CreatedBy);
//                cols.Add("CreatedOn", x => x.CreatedOn);
//                cols.Add("ModifiedBy", x => x.ModifiedBy);
//                cols.Add("ModifiedOn", x => x.ModifiedOn);

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    pagedData = from x in list
//                                select new IssueSlipHeaderDTO
//                                {
//                                    ID = x.ID,
//                                    Code = x.Code,
//                                    Name = x.Name,
//                                    TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                                    TransactionStatus = x.TransactionStatus,
//                                    CreatedBy = x.CreatedBy,
//                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
//                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn),
//                                };
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        public async Task<IHttpActionResult> Datatable2(string transactionStatus)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<IssueSlipHeader> list = Enumerable.Empty<IssueSlipHeader>();
//            IEnumerable<IssueSlipHeaderDTO> pagedData = Enumerable.Empty<IssueSlipHeaderDTO>();

//            IQueryable<IssueSlipHeader> query = null;

//            int recordsTotal = 0;

//            if (transactionStatus.Equals("OPEN") || transactionStatus.Equals("PROGRESS"))
//            {
//                query = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();

//                recordsTotal = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).Count();
//            }
//            else
//            {
//                query = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();

//                recordsTotal = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).Count();
//            }

//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Code.Contains(search)
//                        || m.Name.Contains(search)
//                        //|| m.TotalRequestedQty.Contains(search)
//                        || m.TransactionStatus.Contains(search)
//                        || m.CreatedBy.Contains(search)
//                        || m.ModifiedBy.Contains(search)
//                        );

//                Dictionary<string, Func<IssueSlipHeader, object>> cols = new Dictionary<string, Func<IssueSlipHeader, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Code", x => x.Code);
//                cols.Add("Name", x => x.Name);
//                cols.Add("TotalRequestedQty", x => x.TotalRequestedQty);
//                cols.Add("TransactionStatus", x => x.TransactionStatus);
//                cols.Add("CreatedBy", x => x.CreatedBy);
//                cols.Add("CreatedOn", x => x.CreatedOn);
//                cols.Add("ModifiedBy", x => x.ModifiedBy);
//                cols.Add("ModifiedOn", x => x.ModifiedOn);

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    pagedData = from x in list
//                                select new IssueSlipHeaderDTO
//                                {
//                                    ID = x.ID,
//                                    Code = x.Code,
//                                    Name = x.Name,
//                                    TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                                    TransactionStatus = x.TransactionStatus,
//                                    CreatedBy = x.CreatedBy,
//                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
//                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn),
//                                };
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableStock(string rawMaterialCode, string binRackID, string binRackAreaID, string warehouseID)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
//            IEnumerable<StockRMDTO> pagedData = Enumerable.Empty<StockRMDTO>();

//            IQueryable<vStockAll> query = null;
//            int recordsTotal = 0;

//            query = db.vStockAlls.Where(s => s.MaterialCode.Equals(rawMaterialCode) && s.Quantity > 0).OrderBy(s => s.ReceivedAt).ThenBy(s => s.InDate).ThenBy(s => s.ExpiredDate).AsQueryable();
//            recordsTotal = db.vStockAlls.Where(s => s.MaterialCode.Equals(rawMaterialCode) && s.Quantity > 0).OrderBy(s => s.ReceivedAt).ThenBy(s => s.InDate).ThenBy(s => s.ExpiredDate).Count();

//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Barcode.Contains(search) 
//                        || m.MaterialCode.Contains(search) 
//                        || m.MaterialName.Contains(search)
//                        || m.LotNumber.Contains(search)
//                        //|| m.InDate.Contains(search)
//                        //|| m.ExpiredDate.Contains(search)
//                        //|| m.Qty.Contains(search)
//                        || m.BinRackCode.Contains(search)
//                        || m.BinRackName.Contains(search)
//                        || m.BinRackAreaCode.Contains(search)
//                        || m.BinRackAreaName.Contains(search)
//                        || m.WarehouseCode.Contains(search)
//                        || m.WarehouseName.Contains(search)
//                        //|| m.ReceivedAt.Contains(search)
//                        );

//                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Barcode", x => x.Barcode);
//                cols.Add("RawMaterialID", x => x.MaterialID);
//                cols.Add("MaterialCode", x => x.MaterialCode);
//                cols.Add("MaterialName", x => x.MaterialName);
//                cols.Add("LotNo", x => x.LotNumber);
//                cols.Add("InDate", x => x.InDate);
//                cols.Add("ExpDate", x => x.ExpiredDate);
//                cols.Add("Qty", x => x.Quantity);
//                cols.Add("BinRackID", x => x.BinRackID);
//                cols.Add("BinRackCode", x => x.BinRackCode);
//                cols.Add("BinRackName", x => x.BinRackName);
//                cols.Add("BinRackAreaID", x => x.BinRackAreaID);
//                cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
//                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
//                cols.Add("WarehouseID", x => x.WarehouseID);
//                cols.Add("WarehouseCode", x => x.WarehouseCode);
//                cols.Add("WarehouseName", x => x.WarehouseName);
//                cols.Add("ReceivedAt", x => x.ReceivedAt);
//                cols.Add("IsExpired", x => DateTime.Now >= x.ExpiredDate);

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    pagedData = from x in list
//                                select new StockRMDTO
//                                {
//                                    ID = x.ID,
//                                    Barcode = x.Barcode,
//                                    RawMaterialID = x.MaterialID,
//                                    MaterialCode = x.MaterialCode,
//                                    MaterialName = x.MaterialName,
//                                    Qty = Helper.FormatThousand(x.Quantity),
//                                    LotNo = x.LotNumber,
//                                    InDate = Helper.NullDateToString2(x.InDate),
//                                    ExpDate = Helper.NullDateToString2(x.ExpiredDate),
//                                    BinRackID = x.BinRackID,
//                                    BinRackCode = x.BinRackCode,
//                                    BinRackName = x.BinRackName,
//                                    BinRackAreaID = x.BinRackAreaID,
//                                    BinRackAreaCode = x.BinRackAreaCode,
//                                    BinRackAreaName = x.BinRackAreaName,
//                                    WarehouseID = x.WarehouseID,
//                                    WarehouseCode = x.WarehouseCode,
//                                    WarehouseName = x.WarehouseName,
//                                    ReceivedAt = Helper.NullDateTimeToString(x.ReceivedAt),
//                                    IsExpired = DateTime.Now >= x.ExpiredDate
//                                };
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableStock2(string rawMaterialCode)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
//            IEnumerable<StockRMDTO> pagedData = Enumerable.Empty<StockRMDTO>();

//            IQueryable<vStockAll> query = null;
//            int recordsTotal = 0;

//            query = db.vStockAlls.Where(s => s.MaterialCode.Equals(rawMaterialCode)).OrderBy(s => s.ReceivedAt).ThenBy(s => s.InDate).ThenBy(s => s.ExpiredDate).AsQueryable();
//            recordsTotal = db.vStockAlls.Where(s => s.MaterialCode.Equals(rawMaterialCode)).OrderBy(s => s.ReceivedAt).ThenBy(s => s.InDate).ThenBy(s => s.ExpiredDate).Count();

//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Barcode.Contains(search)
//                        || m.MaterialCode.Contains(search)
//                        || m.MaterialName.Contains(search)
//                        || m.LotNumber.Contains(search)
//                        //|| m.InDate.Contains(search)
//                        //|| m.ExpiredDate.Contains(search)
//                        //|| m.Qty.Contains(search)
//                        || m.BinRackCode.Contains(search)
//                        || m.BinRackName.Contains(search)
//                        || m.BinRackAreaCode.Contains(search)
//                        || m.BinRackAreaName.Contains(search)
//                        || m.WarehouseCode.Contains(search)
//                        || m.WarehouseName.Contains(search)
//                        //|| m.ReceivedAt.Contains(search)
//                        );

//                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Barcode", x => x.Barcode);
//                cols.Add("RawMaterialID", x => x.MaterialID);
//                cols.Add("MaterialCode", x => x.MaterialCode);
//                cols.Add("MaterialName", x => x.MaterialName);
//                cols.Add("LotNo", x => x.LotNumber);
//                cols.Add("InDate", x => x.InDate);
//                cols.Add("ExpDate", x => x.ExpiredDate);
//                cols.Add("Qty", x => x.Quantity);
//                cols.Add("BinRackID", x => x.BinRackID);
//                cols.Add("BinRackCode", x => x.BinRackCode);
//                cols.Add("BinRackName", x => x.BinRackName);
//                cols.Add("BinRackAreaID", x => x.BinRackAreaID);
//                cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
//                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
//                cols.Add("WarehouseID", x => x.WarehouseID);
//                cols.Add("WarehouseCode", x => x.WarehouseCode);
//                cols.Add("WarehouseName", x => x.WarehouseName);
//                cols.Add("ReceivedAt", x => x.ReceivedAt);
//                cols.Add("IsExpired", x => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(x.ExpiredDate));

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    pagedData = from x in list
//                                select new StockRMDTO
//                                {
//                                    ID = x.ID,
//                                    Barcode = x.Barcode,
//                                    RawMaterialID = x.MaterialID,
//                                    MaterialCode = x.MaterialCode,
//                                    MaterialName = x.MaterialName,
//                                    Qty = Helper.FormatThousand(x.Quantity),
//                                    LotNo = x.LotNumber,
//                                    InDate = Helper.NullDateToString2(x.InDate),
//                                    ExpDate = Helper.NullDateToString2(x.ExpiredDate),
//                                    BinRackID = x.BinRackID,
//                                    BinRackCode = x.BinRackCode,
//                                    BinRackName = x.BinRackName,
//                                    BinRackAreaID = x.BinRackAreaID,
//                                    BinRackAreaCode = x.BinRackAreaCode,
//                                    BinRackAreaName = x.BinRackAreaName,
//                                    WarehouseID = x.WarehouseID,
//                                    WarehouseCode = x.WarehouseCode,
//                                    WarehouseName = x.WarehouseName,
//                                    ReceivedAt = Helper.NullDateTimeToString(x.ReceivedAt),
//                                    IsExpired = DateTime.Now >= x.ExpiredDate
//                                };
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableDetail(string HeaderID)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<IssueSlipDetail> list = Enumerable.Empty<IssueSlipDetail>();
//            List<IssueSlipDetailDTO> pagedData = new List<IssueSlipDetailDTO>();

//            IQueryable<IssueSlipDetail> query = db.IssueSlipDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

//            int recordsTotal = db.IssueSlipDetails.Where(s => s.HeaderID.Equals(HeaderID)).Count();
//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.MaterialCode.Contains(search)
//                        || m.MaterialName.Contains(search)
//                        || m.VendorName.Contains(search)
//                        //|| m.RequestedQty.Contains(search)
//                        );

//                Dictionary<string, Func<IssueSlipDetail, object>> cols = new Dictionary<string, Func<IssueSlipDetail, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("MaterialCode", x => x.MaterialCode);
//                cols.Add("MaterialName", x => x.MaterialName);
//                cols.Add("VendorName", x => x.VendorName);
//                cols.Add("RequestedQty", x => x.RequestedQty);

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    foreach (IssueSlipDetail detail in list)
//                    {
//                        decimal total = 0;

//                        IssueSlipDetailDTO issueSlipDetailDTO = new IssueSlipDetailDTO
//                        {
//                            ID = detail.ID,
//                            MaterialCode = detail.MaterialCode,
//                            MaterialName = detail.MaterialName,
//                            VendorName = detail.VendorName,
//                            RequestedQty = Helper.FormatThousand(detail.RequestedQty)
//                        };

//                        foreach (IssueSlipList x in detail.IssueSlipLists)
//                        {
//                            total += x.UsageSupplyQty.HasValue ? x.UsageSupplyQty.Value : 0;
//                        }

//                        issueSlipDetailDTO.PickedQty = Helper.FormatThousand(total);

//                        pagedData.Add(issueSlipDetailDTO);
//                    }
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableIssueSlip(string HeaderID)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<IssueSlipList> list = Enumerable.Empty<IssueSlipList>();
//            List<IssueSlipListDTO> pagedData = new List<IssueSlipListDTO>();

//            IQueryable<IssueSlipList> query = db.IssueSlipLists.Where(s => s.IssueSlipDetail.HeaderID.Equals(HeaderID)).AsQueryable();

//            int recordsTotal = db.IssueSlipLists.Where(s => s.IssueSlipDetail.HeaderID.Equals(HeaderID)).Count();
//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Barcode.Contains(search)
//                        || m.LotNo.Contains(search)
//                        //|| m.UsageSupplyQty.Contains(search)
//                        || m.UsageBinRackCode.Contains(search)
//                        || m.UsageBinRackName.Contains(search)
//                        || m.UsagePickingMethod.Contains(search)
//                        || m.UsagePickedBy.Contains(search)
//                        //|| m.UsageQRLabel.Contains(search)
//                        //|| m.UsagePackage.Contains(search)
//                        //|| m.UsageInspectionMethod.Contains(search)
//                        || m.UsageInspectedBy.Contains(search)
//                        //|| m.UsageExpDate.Contains(search)
//                        //|| m.UsageApproveStamp.Contains(search)
//                        || m.UsageJudgementMethod.Contains(search)
//                        || m.UsageJudgeBy.Contains(search)
//                        //|| m.UsageJudgeOn.Contains(search)
//                        //|| m.ReturnActualQty.Contains(search)
//                        || m.ReturnBinRackCode.Contains(search)
//                        || m.ReturnBinRackName.Contains(search)
//                        || m.ReturnPutawayMethod.Contains(search)
//                        || m.ReturnPutBy.Contains(search)
//                        //|| m.ReturnPutOn.Contains(search)
//                        //|| m.ReturnQRLabel.Contains(search)
//                        //|| m.ReturnPackage.Contains(search)
//                        || m.ReturnInspectionMethod.Contains(search)
//                        || m.ReturnInspectedBy.Contains(search)
//                        //|| m.ReturnInspectedOn.Contains(search)
//                        );

//                Dictionary<string, Func<IssueSlipList, object>> cols = new Dictionary<string, Func<IssueSlipList, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Barcode", x => x.Barcode);
//                cols.Add("LotNo", x => x.LotNo);
//                cols.Add("UsageSupplyQty", x => Helper.FormatThousand(x.UsageSupplyQty));
//                cols.Add("UsageBinRackID", x => x.UsageBinRackID);
//                cols.Add("UsageBinRackCode", x => x.UsageBinRackCode);
//                cols.Add("UsageBinRackName", x => x.UsageBinRackName);
//                cols.Add("UsagePickingMethod", x => x.UsagePickingMethod);
//                cols.Add("UsagePickedBy", x => x.UsagePickedBy);
//                cols.Add("UsagePickedOn", x => Helper.NullDateTimeToString(x.UsagePickedOn));
//                cols.Add("UsageQRLabel", x => x.UsageQRLabel.HasValue ? (bool)x.UsageQRLabel : false);
//                cols.Add("UsagePackage", x => x.UsagePackage.HasValue ? (bool)x.UsagePackage : false);
//                cols.Add("UsageInspectionMethod", x => x.UsageInspectionMethod);
//                cols.Add("UsageInspectedBy", x => x.UsageInspectedBy);
//                cols.Add("UsageInspectedOn", x => Helper.NullDateTimeToString(x.UsageInspectedOn));
//                cols.Add("UsageExpDate", x => x.UsageExpDate.HasValue ? (bool)x.UsageExpDate : false);
//                cols.Add("UsageApproveStamp", x => x.UsageApproveStamp.HasValue ? (bool)x.UsageApproveStamp : false);
//                cols.Add("UsageJudgementMethod", x => x.UsageJudgementMethod);
//                cols.Add("UsageJudgeBy", x => x.UsageJudgeBy);
//                cols.Add("UsageJudgeOn", x => Helper.NullDateTimeToString(x.UsageJudgeOn));
//                cols.Add("ReturnActualQty", x => Helper.FormatThousand(x.ReturnActualQty));
//                cols.Add("ReturnBinRackID", x => x.ReturnBinRackID);
//                cols.Add("ReturnBinRackCode", x => x.ReturnBinRackCode);
//                cols.Add("ReturnBinRackName", x => x.ReturnBinRackName);
//                cols.Add("ReturnPutawayMethod", x => x.ReturnPutawayMethod);
//                cols.Add("ReturnPutBy", x => x.ReturnPutBy);
//                cols.Add("ReturnPutOn", x => Helper.NullDateTimeToString(x.ReturnPutOn));
//                cols.Add("ReturnQRLabel", x => x.ReturnQRLabel.HasValue ? (bool)x.ReturnQRLabel : false);
//                cols.Add("ReturnPackage", x => x.ReturnPackage.HasValue ? (bool)x.ReturnPackage : false);
//                cols.Add("ReturnInspectionMethod", x => x.ReturnInspectionMethod);
//                cols.Add("ReturnInspectedBy", x => x.ReturnInspectedBy);
//                cols.Add("ReturnInspectedOn", x => Helper.NullDateTimeToString(x.ReturnInspectedOn));

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    foreach (IssueSlipList x in list)
//                    {
//                        IssueSlipListDTO issueSlipListDTO = new IssueSlipListDTO
//                        {
//                            ID = x.ID,
//                            DetailID = x.DetailID,
//                            Barcode = x.Barcode,
//                            LotNo = x.LotNo,
//                            MaterialCode = x.IssueSlipDetail.MaterialCode,
//                            MaterialName = x.IssueSlipDetail.MaterialName,
//                            UsageSupplyQty = Helper.FormatThousand(x.UsageSupplyQty),
//                            UsageBinRackID = x.UsageBinRackID,
//                            UsageBinRackCode = x.UsageBinRackCode,
//                            UsageBinRackName = x.UsageBinRackName,
//                            UsagePickingMethod = x.UsagePickingMethod,
//                            UsagePickedBy = x.UsagePickedBy,
//                            UsagePickedOn = Helper.NullDateTimeToString(x.UsagePickedOn),
//                            UsageQRLabel = x.UsageQRLabel.HasValue ? (bool)x.UsageQRLabel : false,
//                            UsagePackage = x.UsagePackage.HasValue ? (bool)x.UsagePackage : false,
//                            UsageInspectionMethod = x.UsageInspectionMethod,
//                            UsageInspectedBy = x.UsageInspectedBy,
//                            UsageInspectedOn = Helper.NullDateTimeToString(x.UsageInspectedOn),
//                            UsageExpDate = x.UsageExpDate.HasValue ? (bool)x.UsageExpDate : false,
//                            UsageApproveStamp = x.UsageApproveStamp.HasValue ? (bool)x.UsageApproveStamp : false,
//                            UsageJudgementMethod = x.UsageJudgementMethod,
//                            UsageJudgeBy = x.UsageJudgeBy,
//                            UsageJudgeOn = Helper.NullDateTimeToString(x.UsageJudgeOn),
//                            ReturnActualQty = Helper.FormatThousand(x.ReturnActualQty),
//                            ReturnBinRackID = x.ReturnBinRackID,
//                            ReturnBinRackCode = x.ReturnBinRackCode,
//                            ReturnBinRackName = x.ReturnBinRackName,
//                            ReturnPutawayMethod = x.ReturnPutawayMethod,
//                            ReturnPutBy = x.ReturnPutBy,
//                            ReturnPutOn = Helper.NullDateTimeToString(x.ReturnPutOn),
//                            ReturnQRLabel = x.ReturnQRLabel.HasValue ? (bool)x.ReturnQRLabel : false,
//                            ReturnPackage = x.ReturnPackage.HasValue ? (bool)x.ReturnPackage : false,
//                            ReturnInspectionMethod = x.ReturnInspectionMethod,
//                            ReturnInspectedBy = x.ReturnInspectedBy,
//                            ReturnInspectedOn = Helper.NullDateTimeToString(x.ReturnInspectedOn)
//                        };

//                        pagedData.Add(issueSlipListDTO);
//                    }
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableReturn(string HeaderID)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IEnumerable<IssueSlipList> list = Enumerable.Empty<IssueSlipList>();
//            List<IssueSlipListDTO> pagedData = new List<IssueSlipListDTO>();

//            IQueryable<IssueSlipList> query = db.IssueSlipLists.Where(s => s.IssueSlipDetail.HeaderID.Equals(HeaderID)).AsQueryable();

//            int recordsTotal = db.IssueSlipLists.Where(s => s.IssueSlipDetail.HeaderID.Equals(HeaderID)).Count();
//            int recordsFiltered = 0;

//            try
//            {
//                query = query
//                        .Where(m => m.Barcode.Contains(search)
//                        || m.LotNo.Contains(search)
//                        //|| m.UsageSupplyQty.Contains(search)
//                        || m.UsageBinRackCode.Contains(search)
//                        || m.UsageBinRackName.Contains(search)
//                        || m.UsagePickingMethod.Contains(search)
//                        || m.UsagePickedBy.Contains(search)
//                        //|| m.UsageQRLabel.Contains(search)
//                        //|| m.UsagePackage.Contains(search)
//                        //|| m.UsageInspectionMethod.Contains(search)
//                        || m.UsageInspectedBy.Contains(search)
//                        //|| m.UsageExpDate.Contains(search)
//                        //|| m.UsageApproveStamp.Contains(search)
//                        || m.UsageJudgementMethod.Contains(search)
//                        || m.UsageJudgeBy.Contains(search)
//                        //|| m.UsageJudgeOn.Contains(search)
//                        //|| m.ReturnActualQty.Contains(search)
//                        || m.ReturnBinRackCode.Contains(search)
//                        || m.ReturnBinRackName.Contains(search)
//                        || m.ReturnPutawayMethod.Contains(search)
//                        || m.ReturnPutBy.Contains(search)
//                        //|| m.ReturnPutOn.Contains(search)
//                        //|| m.ReturnQRLabel.Contains(search)
//                        //|| m.ReturnPackage.Contains(search)
//                        || m.ReturnInspectionMethod.Contains(search)
//                        || m.ReturnInspectedBy.Contains(search)
//                        //|| m.ReturnInspectedOn.Contains(search)
//                        );

//                Dictionary<string, Func<IssueSlipList, object>> cols = new Dictionary<string, Func<IssueSlipList, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Barcode", x => x.Barcode);
//                cols.Add("LotNo", x => x.LotNo);
//                cols.Add("UsageSupplyQty", x => Helper.FormatThousand(x.UsageSupplyQty));
//                cols.Add("UsageBinRackID", x => x.UsageBinRackID);
//                cols.Add("UsageBinRackCode", x => x.UsageBinRackCode);
//                cols.Add("UsageBinRackName", x => x.UsageBinRackName);
//                cols.Add("UsagePickingMethod", x => x.UsagePickingMethod);
//                cols.Add("UsagePickedBy", x => x.UsagePickedBy);
//                cols.Add("UsagePickedOn", x => Helper.NullDateTimeToString(x.UsagePickedOn));
//                cols.Add("UsageQRLabel", x => x.UsageQRLabel.HasValue ? (bool)x.UsageQRLabel : false);
//                cols.Add("UsagePackage", x => x.UsagePackage.HasValue ? (bool)x.UsagePackage : false);
//                cols.Add("UsageInspectionMethod", x => x.UsageInspectionMethod);
//                cols.Add("UsageInspectedBy", x => x.UsageInspectedBy);
//                cols.Add("UsageInspectedOn", x => Helper.NullDateTimeToString(x.UsageInspectedOn));
//                cols.Add("UsageExpDate", x => x.UsageExpDate.HasValue ? (bool)x.UsageExpDate : false);
//                cols.Add("UsageApproveStamp", x => x.UsageApproveStamp.HasValue ? (bool)x.UsageApproveStamp : false);
//                cols.Add("UsageJudgementMethod", x => x.UsageJudgementMethod);
//                cols.Add("UsageJudgeBy", x => x.UsageJudgeBy);
//                cols.Add("UsageJudgeOn", x => Helper.NullDateTimeToString(x.UsageJudgeOn));
//                cols.Add("ReturnActualQty", x => Helper.FormatThousand(x.ReturnActualQty));
//                cols.Add("ReturnBinRackID", x => x.ReturnBinRackID);
//                cols.Add("ReturnBinRackCode", x => x.ReturnBinRackCode);
//                cols.Add("ReturnBinRackName", x => x.ReturnBinRackName);
//                cols.Add("ReturnPutawayMethod", x => x.ReturnPutawayMethod);
//                cols.Add("ReturnPutBy", x => x.ReturnPutBy);
//                cols.Add("ReturnPutOn", x => Helper.NullDateTimeToString(x.ReturnPutOn));
//                cols.Add("ReturnQRLabel", x => x.ReturnQRLabel.HasValue ? (bool)x.ReturnQRLabel : false);
//                cols.Add("ReturnPackage", x => x.ReturnPackage.HasValue ? (bool)x.ReturnPackage : false);
//                cols.Add("ReturnInspectionMethod", x => x.ReturnInspectionMethod);
//                cols.Add("ReturnInspectedBy", x => x.ReturnInspectedBy);
//                cols.Add("ReturnInspectedOn", x => Helper.NullDateTimeToString(x.ReturnInspectedOn));

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    foreach (IssueSlipList x in list)
//                    {
//                        IssueSlipListDTO issueSlipListDTO = new IssueSlipListDTO
//                        {
//                            ID = x.ID,
//                            DetailID = x.DetailID,
//                            Barcode = x.Barcode,
//                            LotNo = x.LotNo,
//                            MaterialCode = x.IssueSlipDetail.MaterialCode,
//                            MaterialName = x.IssueSlipDetail.MaterialName,
//                            UsageSupplyQty = Helper.FormatThousand(x.UsageSupplyQty),
//                            UsageBinRackID = x.UsageBinRackID,
//                            UsageBinRackCode = x.UsageBinRackCode,
//                            UsageBinRackName = x.UsageBinRackName,
//                            UsagePickingMethod = x.UsagePickingMethod,
//                            UsagePickedBy = x.UsagePickedBy,
//                            UsagePickedOn = Helper.NullDateTimeToString(x.UsagePickedOn),
//                            UsageQRLabel = x.UsageQRLabel.HasValue ? (bool)x.UsageQRLabel : false,
//                            UsagePackage = x.UsagePackage.HasValue ? (bool)x.UsagePackage : false,
//                            UsageInspectionMethod = x.UsageInspectionMethod,
//                            UsageInspectedBy = x.UsageInspectedBy,
//                            UsageInspectedOn = Helper.NullDateTimeToString(x.UsageInspectedOn),
//                            UsageExpDate = x.UsageExpDate.HasValue ? (bool)x.UsageExpDate : false,
//                            UsageApproveStamp = x.UsageApproveStamp.HasValue ? (bool)x.UsageApproveStamp : false,
//                            UsageJudgementMethod = x.UsageJudgementMethod,
//                            UsageJudgeBy = x.UsageJudgeBy,
//                            UsageJudgeOn = Helper.NullDateTimeToString(x.UsageJudgeOn),
//                            ReturnActualQty = Helper.FormatThousand(x.ReturnActualQty),
//                            ReturnBinRackID = x.ReturnBinRackID,
//                            ReturnBinRackCode = x.ReturnBinRackCode,
//                            ReturnBinRackName = x.ReturnBinRackName,
//                            ReturnPutawayMethod = x.ReturnPutawayMethod,
//                            ReturnPutBy = x.ReturnPutBy,
//                            ReturnPutOn = Helper.NullDateTimeToString(x.ReturnPutOn),
//                            ReturnQRLabel = x.ReturnQRLabel.HasValue ? (bool)x.ReturnQRLabel : false,
//                            ReturnPackage = x.ReturnPackage.HasValue ? (bool)x.ReturnPackage : false,
//                            ReturnInspectionMethod = x.ReturnInspectionMethod,
//                            ReturnInspectedBy = x.ReturnInspectedBy,
//                            ReturnInspectedOn = Helper.NullDateTimeToString(x.ReturnInspectedOn)
//                        };

//                        pagedData.Add(issueSlipListDTO);
//                    }
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetDataById(string id)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IssueSlipHeaderDTO issueSlipHeaderDTO = null;

//            try
//            {
//                IssueSlipHeader header = await db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
//                issueSlipHeaderDTO = new IssueSlipHeaderDTO
//                {
//                    ID = header.ID,
//                    Code = header.Code,
//                    Name = header.Name,
//                    TotalRequestedQty = Helper.FormatThousand(header.TotalRequestedQty),
//                    TransactionStatus = header.TransactionStatus,
//                    CreatedBy = header.CreatedBy,
//                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
//                    ModifiedBy = header.ModifiedBy != null ? header.ModifiedBy : "",
//                    ModifiedOn = Helper.NullDateTimeToString(header.ModifiedOn),
//                    Details = new List<IssueSlipDetailDTO>()
//                };

//                foreach (IssueSlipDetail detail in header.IssueSlipDetails)
//                {
//                    decimal total = 0;

//                    IssueSlipDetailDTO issueSlipDetailDTO = new IssueSlipDetailDTO
//                    {
//                        ID = detail.ID,
//                        MaterialCode = detail.MaterialCode,
//                        MaterialName = detail.MaterialName,
//                        VendorName = detail.VendorName,
//                        RequestedQty = Helper.FormatThousand(detail.RequestedQty)
//                    };

//                    foreach (IssueSlipList list in detail.IssueSlipLists)
//                    {
//                        total += list.UsageSupplyQty.HasValue ? list.UsageSupplyQty.Value : 0;
//                    }

//                    issueSlipDetailDTO.PickedQty = Helper.FormatThousand(total);

//                    issueSlipHeaderDTO.Details.Add(issueSlipDetailDTO);
//                }

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", issueSlipHeaderDTO);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetDetailByHeaderId(string id)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            List<IssueSlipListDTO> listDTO = new List<IssueSlipListDTO>();

//            try
//            {
//                IssueSlipHeader header = await db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

//                foreach (IssueSlipDetail detail in header.IssueSlipDetails)
//                {
//                    foreach (IssueSlipList list in detail.IssueSlipLists)
//                    {
//                        IssueSlipListDTO issueSlipListDTO = new IssueSlipListDTO
//                        {
//                            ID = list.ID,
//                            DetailID = list.DetailID,
//                            Barcode = list.Barcode,
//                            LotNo = list.LotNo,
//                            MaterialCode = list.IssueSlipDetail.MaterialCode,
//                            MaterialName = list.IssueSlipDetail.MaterialName,
//                            UsageSupplyQty = Helper.FormatThousand(list.UsageSupplyQty),
//                            UsageBinRackID = list.UsageBinRackID ?? "",
//                            UsageBinRackCode = list.UsageBinRackCode ?? "",
//                            UsageBinRackName = list.UsageBinRackName ?? "",
//                            UsagePickingMethod = list.UsagePickingMethod ?? "",
//                            UsagePickedBy = list.UsagePickedBy ?? "",
//                            UsagePickedOn = Helper.NullDateTimeToString(list.UsagePickedOn),
//                            UsageQRLabel = list.UsageQRLabel.HasValue ? (bool)list.UsageQRLabel : false,
//                            UsagePackage = list.UsagePackage.HasValue ? (bool)list.UsagePackage : false,
//                            UsageInspectionMethod = list.UsageInspectionMethod ?? "",
//                            UsageInspectedBy = list.UsageInspectedBy ?? "",
//                            UsageInspectedOn = Helper.NullDateTimeToString(list.UsageInspectedOn),
//                            UsageExpDate = list.UsageExpDate.HasValue ? (bool)list.UsageExpDate : false,
//                            UsageApproveStamp = list.UsageApproveStamp.HasValue ? (bool)list.UsageApproveStamp : false,
//                            UsageJudgementMethod = list.UsageJudgementMethod ?? "",
//                            UsageJudgeBy = list.UsageJudgeBy ?? "",
//                            UsageJudgeOn = Helper.NullDateTimeToString(list.UsageJudgeOn),
//                            ReturnActualQty = Helper.FormatThousand(list.ReturnActualQty),
//                            ReturnBinRackID = list.ReturnBinRackID ?? "",
//                            ReturnBinRackCode = list.ReturnBinRackCode ?? "",
//                            ReturnBinRackName = list.ReturnBinRackName ?? "",
//                            ReturnPutawayMethod = list.ReturnPutawayMethod ?? "",
//                            ReturnPutBy = list.ReturnPutBy ?? "",
//                            ReturnPutOn = Helper.NullDateTimeToString(list.ReturnPutOn),
//                            ReturnQRLabel = list.ReturnQRLabel.HasValue ? (bool)list.ReturnQRLabel : false,
//                            ReturnPackage = list.ReturnPackage.HasValue ? (bool)list.ReturnPackage : false,
//                            ReturnInspectionMethod = list.ReturnInspectionMethod ?? "",
//                            ReturnInspectedBy = list.ReturnInspectedBy ?? "",
//                            ReturnInspectedOn = Helper.NullDateTimeToString(list.ReturnInspectedOn)
//                        };

//                        listDTO.Add(issueSlipListDTO);
//                    }
//                }

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", listDTO);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetReturnByHeaderId(string id)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            List<IssueSlipListDTO> listDTO = new List<IssueSlipListDTO>();

//            try
//            {
//                IssueSlipHeader header = await db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

//                foreach (IssueSlipDetail detail in header.IssueSlipDetails)
//                {
//                    foreach (IssueSlipList list in detail.IssueSlipLists.Where(s => s.ReturnInspectedOn != null))
//                    {
//                        IssueSlipListDTO issueSlipListDTO = new IssueSlipListDTO
//                        {
//                            ID = list.ID,
//                            DetailID = list.DetailID,
//                            Barcode = list.Barcode,
//                            LotNo = list.LotNo,
//                            MaterialCode = list.IssueSlipDetail.MaterialCode,
//                            MaterialName = list.IssueSlipDetail.MaterialName,
//                            UsageSupplyQty = Helper.FormatThousand(list.UsageSupplyQty),
//                            UsageBinRackID = list.UsageBinRackID ?? "",
//                            UsageBinRackCode = list.UsageBinRackCode ?? "",
//                            UsageBinRackName = list.UsageBinRackName ?? "",
//                            UsagePickingMethod = list.UsagePickingMethod ?? "",
//                            UsagePickedBy = list.UsagePickedBy ?? "",
//                            UsagePickedOn = Helper.NullDateTimeToString(list.UsagePickedOn),
//                            UsageQRLabel = list.UsageQRLabel.HasValue ? (bool)list.UsageQRLabel : false,
//                            UsagePackage = list.UsagePackage.HasValue ? (bool)list.UsagePackage : false,
//                            UsageInspectionMethod = list.UsageInspectionMethod ?? "",
//                            UsageInspectedBy = list.UsageInspectedBy ?? "",
//                            UsageInspectedOn = Helper.NullDateTimeToString(list.UsageInspectedOn),
//                            UsageExpDate = list.UsageExpDate.HasValue ? (bool)list.UsageExpDate : false,
//                            UsageApproveStamp = list.UsageApproveStamp.HasValue ? (bool)list.UsageApproveStamp : false,
//                            UsageJudgementMethod = list.UsageJudgementMethod ?? "",
//                            UsageJudgeBy = list.UsageJudgeBy ?? "",
//                            UsageJudgeOn = Helper.NullDateTimeToString(list.UsageJudgeOn),
//                            ReturnActualQty = Helper.FormatThousand(list.ReturnActualQty),
//                            ReturnBinRackID = list.ReturnBinRackID ?? "",
//                            ReturnBinRackCode = list.ReturnBinRackCode ?? "",
//                            ReturnBinRackName = list.ReturnBinRackName ?? "",
//                            ReturnPutawayMethod = list.ReturnPutawayMethod ?? "",
//                            ReturnPutBy = list.ReturnPutBy ?? "",
//                            ReturnPutOn = Helper.NullDateTimeToString(list.ReturnPutOn),
//                            ReturnQRLabel = list.ReturnQRLabel.HasValue ? (bool)list.ReturnQRLabel : false,
//                            ReturnPackage = list.ReturnPackage.HasValue ? (bool)list.ReturnPackage : false,
//                            ReturnInspectionMethod = list.ReturnInspectionMethod ?? "",
//                            ReturnInspectedBy = list.ReturnInspectedBy ?? "",
//                            ReturnInspectedOn = Helper.NullDateTimeToString(list.ReturnInspectedOn)
//                        };

//                        listDTO.Add(issueSlipListDTO);
//                    }
//                }

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", listDTO);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetDetailById(string id)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IssueSlipDetailDTO issueSlipDetailDTO = null;

//            try
//            {
//                IssueSlipDetail detail = await db.IssueSlipDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
//                issueSlipDetailDTO = new IssueSlipDetailDTO
//                {
//                    ID = detail.ID,
//                    MaterialCode = detail.MaterialCode,
//                    MaterialName = detail.MaterialName,
//                    VendorName = detail.VendorName,
//                    RequestedQty = Helper.FormatThousand(detail.RequestedQty),
//                    Lists = new List<IssueSlipListDTO>()
//                };

//                foreach (IssueSlipList list in detail.IssueSlipLists)
//                {
//                    IssueSlipListDTO issueSlipListDTO = new IssueSlipListDTO
//                    {
//                        ID = list.ID,
//                        DetailID = list.DetailID,
//                        Barcode = list.Barcode,
//                        MaterialCode = list.IssueSlipDetail.MaterialCode,
//                        MaterialName = list.IssueSlipDetail.MaterialName,
//                        UsageSupplyQty = Helper.FormatThousand(list.UsageSupplyQty),
//                        UsageBinRackID = list.UsageBinRackID ?? "",
//                        UsageBinRackCode = list.UsageBinRackCode ?? "",
//                        UsageBinRackName = list.UsageBinRackName ?? "",
//                        UsagePickingMethod = list.UsagePickingMethod ?? "",
//                        UsagePickedBy = list.UsagePickedBy ?? "",
//                        UsagePickedOn = Helper.NullDateTimeToString(list.UsagePickedOn),
//                        UsageQRLabel =  list.UsageQRLabel.HasValue ? (bool)list.UsageQRLabel : false,
//                        UsagePackage = list.UsagePackage.HasValue ? (bool)list.UsagePackage : false,
//                        UsageInspectionMethod = list.UsageInspectionMethod ?? "",
//                        UsageInspectedBy = list.UsageInspectedBy ?? "",
//                        UsageInspectedOn = Helper.NullDateTimeToString(list.UsageInspectedOn),
//                        UsageExpDate = list.UsageExpDate.HasValue ? (bool)list.UsageExpDate : false,
//                        UsageApproveStamp = list.UsageApproveStamp.HasValue ? (bool)list.UsageApproveStamp : false,
//                        UsageJudgementMethod = list.UsageJudgementMethod ?? "",
//                        UsageJudgeBy = list.UsageJudgeBy ?? "",
//                        UsageJudgeOn = Helper.NullDateTimeToString(list.UsageJudgeOn),
//                        ReturnActualQty = Helper.FormatThousand(list.ReturnActualQty),
//                        ReturnBinRackID = list.ReturnBinRackID ?? "",
//                        ReturnBinRackCode = list.ReturnBinRackCode ?? "",
//                        ReturnBinRackName = list.ReturnBinRackName ?? "",
//                        ReturnPutawayMethod = list.ReturnPutawayMethod ?? "",
//                        ReturnPutBy = list.ReturnPutBy ?? "",
//                        ReturnPutOn = Helper.NullDateTimeToString(list.ReturnPutOn),
//                        ReturnQRLabel = list.ReturnQRLabel.HasValue ? (bool)list.ReturnQRLabel : false,
//                        ReturnPackage = list.ReturnPackage.HasValue ? (bool)list.ReturnPackage : false,
//                        ReturnInspectionMethod = list.ReturnInspectionMethod ?? "",
//                        ReturnInspectedBy = list.ReturnInspectedBy ?? "",
//                        ReturnInspectedOn = Helper.NullDateTimeToString(list.ReturnInspectedOn)
//                    };

//                    issueSlipDetailDTO.Lists.Add(issueSlipListDTO);
//                }

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", issueSlipDetailDTO);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetDataUsage()
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//            try
//            {
//                IEnumerable<IssueSlipHeader> tempList = await db.IssueSlipHeaders
//                    //.Where(s => s.UsageInspectedOn == null || s.UsageJudgeOn == null || UsagePickedOn == null)
//                    .ToListAsync();

//                list = from x in tempList
//                       select new IssueSlipHeaderDTO
//                       {
//                           ID = x.ID,
//                           Code = x.Code,
//                           Name = x.Name,
//                           TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                           TransactionStatus = x.TransactionStatus,
//                           CreatedBy = x.CreatedBy,
//                           CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                           ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
//                           ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//                       };

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", list);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        //[HttpGet]
//        //public async Task<IHttpActionResult> GetDataUsage(string transactionStatus)
//        //{
//        //    Dictionary<string, object> obj = new Dictionary<string, object>();
//        //    string message = "";
//        //    bool status = false;
//        //    HttpRequest request = HttpContext.Current.Request;
//        //    IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//        //    try
//        //    {
//        //        IEnumerable<IssueSlipHeader> tempList = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus))
//        //            //.Where(s => s.UsageInspectedOn == null || s.UsageJudgeOn == null || UsagePickedOn == null)
//        //            .ToListAsync();

//        //        list = from x in tempList
//        //               select new IssueSlipHeaderDTO
//        //               {
//        //                   ID = x.ID,
//        //                   Code = x.Code,
//        //                   Name = x.Name,
//        //                   TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//        //                   TransactionStatus = x.TransactionStatus,
//        //                   CreatedBy = x.CreatedBy,
//        //                   CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//        //                   ModifiedBy = x.ModifiedBy,
//        //                   ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//        //               };

//        //        status = true;
//        //        message = "Fetch data succeded.";
//        //    }
//        //    catch (HttpRequestException reqpEx)
//        //    {
//        //        message = reqpEx.Message;
//        //        return BadRequest();
//        //    }
//        //    catch (HttpResponseException respEx)
//        //    {
//        //        message = respEx.Message;
//        //        return NotFound();
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        message = ex.Message;
//        //    }

//        //    obj.Add("data", list);
//        //    obj.Add("status", status);
//        //    obj.Add("message", message);

//        //    return Ok(obj);
//        //}

//        /*** PROTOTYPE ***/
//        [HttpGet]
//        public async Task<IHttpActionResult> GetDataUsage2(string transactionStatus)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//            try
//            {
//                List<IssueSlipHeader> tempList = new List<IssueSlipHeader>();
//                //List<IssueSlipHeader> tempList = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus))
//                //    //.Where(s => s.InspectedOn != null && s.JudgeOn != null && PickedOn != null)
//                //    .ToListAsync();

//                List<string> matchList = await db.IssueSlipLists.Where(s => s.UsagePickedOn == null || s.UsageInspectedOn == null || s.UsageJudgeOn == null && s.ReturnInspectedOn == null && s.ReturnPutOn == null).Select(s => s.IssueSlipDetail.HeaderID).Distinct().ToListAsync();

//                foreach (string z in matchList)
//                {
//                    IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ID.Equals(z)).FirstOrDefaultAsync();

//                    if (header != null)
//                    {
//                        tempList.Add(header);
//                    }
//                }

//                list = from x in tempList
//                       select new IssueSlipHeaderDTO
//                       {
//                           ID = x.ID,
//                           Code = x.Code,
//                           Name = x.Name,
//                           TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                           TransactionStatus = x.TransactionStatus,
//                           CreatedBy = x.CreatedBy,
//                           CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                           ModifiedBy = x.ModifiedBy ?? "",
//                           ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//                       };

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", list);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetDataReturn()
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//            try
//            {
//                IEnumerable<IssueSlipHeader> tempList = await db.IssueSlipHeaders
//                    //.Where(s => s.InspectedOn != null && s.JudgeOn != null && PickedOn != null)
//                    .ToListAsync();

//                list = from x in tempList
//                       select new IssueSlipHeaderDTO
//                       {
//                           ID = x.ID,
//                           Code = x.Code,
//                           Name = x.Name,
//                           TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                           TransactionStatus = x.TransactionStatus,
//                           CreatedBy = x.CreatedBy,
//                           CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                           ModifiedBy = x.ModifiedBy ?? "",
//                           ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//                       };

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", list);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        //[HttpGet]
//        //public async Task<IHttpActionResult> GetDataReturn(string transactionStatus)
//        //{
//        //    Dictionary<string, object> obj = new Dictionary<string, object>();
//        //    string message = "";
//        //    bool status = false;
//        //    HttpRequest request = HttpContext.Current.Request;
//        //    IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//        //    try
//        //    {
//        //        IEnumerable<IssueSlipHeader> tempList = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus))
//        //            //.Where(s => s.InspectedOn != null && s.JudgeOn != null && PickedOn != null)
//        //            .ToListAsync();

//        //        list = from x in tempList
//        //               select new IssueSlipHeaderDTO
//        //               {
//        //                   ID = x.ID,
//        //                   Code = x.Code,
//        //                   Name = x.Name,
//        //                   TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//        //                   TransactionStatus = x.TransactionStatus,
//        //                   CreatedBy = x.CreatedBy,
//        //                   CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//        //                   ModifiedBy = x.ModifiedBy,
//        //                   ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//        //               };

//        //        status = true;
//        //        message = "Fetch data succeded.";
//        //    }
//        //    catch (HttpRequestException reqpEx)
//        //    {
//        //        message = reqpEx.Message;
//        //        return BadRequest();
//        //    }
//        //    catch (HttpResponseException respEx)
//        //    {
//        //        message = respEx.Message;
//        //        return NotFound();
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        message = ex.Message;
//        //    }

//        //    obj.Add("data", list);
//        //    obj.Add("status", status);
//        //    obj.Add("message", message);

//        //    return Ok(obj);
//        //}


//        /*** PROTOTYPE ***/
//        [HttpGet]
//        public async Task<IHttpActionResult> GetDataReturn2(string transactionStatus)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

//            try
//            {
//                List<IssueSlipHeader> tempList = new List<IssueSlipHeader>();
//                //List<IssueSlipHeader> tempList = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus))
//                //    //.Where(s => s.InspectedOn != null && s.JudgeOn != null && PickedOn != null)
//                //    .ToListAsync();

//                List<string> matchList = await db.IssueSlipLists.Where(s => s.UsagePickedOn != null && s.ReturnPutOn == null ).Select(s => s.IssueSlipDetail.HeaderID).Distinct().ToListAsync();

//                foreach (string z in matchList)
//                {
//                    IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ID.Equals(z)).FirstOrDefaultAsync();

//                    if (header != null)
//                    {
//                        tempList.Add(header);
//                    }
//                }

//                list = from x in tempList
//                       select new IssueSlipHeaderDTO
//                       {
//                           ID = x.ID,
//                           Code = x.Code,
//                           Name = x.Name,
//                           TotalRequestedQty = Helper.FormatThousand(x.TotalRequestedQty),
//                           TransactionStatus = x.TransactionStatus,
//                           CreatedBy = x.CreatedBy,
//                           CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
//                           ModifiedBy = x.ModifiedBy ?? "",
//                           ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
//                       };

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", list);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> Upload()
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            HttpRequest request = HttpContext.Current.Request;

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;
//            string TransactionId = "";
//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    if (request.Files.Count > 0)
//                    {
//                        HttpPostedFile file = request.Files[0];

//                        if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
//                        {
//                            if (file.ContentLength < (10 * 1024 * 1024))
//                            {
//                                try
//                                {
//                                    Stream stream = file.InputStream;
//                                    IExcelDataReader reader = null;
//                                    if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
//                                    {
//                                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

//                                    }
//                                    else
//                                    {
//                                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
//                                    }

//                                    DataSet result = reader.AsDataSet();
//                                    reader.Close();

//                                    DataTable dt = result.Tables[0];

//                                    IssueSlipHeader header = null;


//                                    foreach (DataRow row in dt.Rows)
//                                    {
//                                        if (dt.Rows.IndexOf(row) == 1)
//                                        {
//                                            var dummy = row[0].ToString();
//                                            IssueSlipHeader temp = await db.IssueSlipHeaders.Where(s => s.Name.Equals(dummy)).FirstOrDefaultAsync();

//                                            if (temp == null)
//                                            {
//                                                var CreatedAt = DateTime.Now;
//                                                TransactionId = Helper.CreateGuid("IS");

//                                                string prefix = TransactionId.Substring(0, 2);
//                                                int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
//                                                int month = CreatedAt.Month;
//                                                string romanMonth = Helper.ConvertMonthToRoman(month);

//                                                // get last number, and do increment.
//                                                string lastNumber = db.IssueSlipHeaders.AsQueryable().OrderByDescending(x => x.Code)
//                                                    .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
//                                                    .AsEnumerable().Select(x => x.Code).FirstOrDefault();
//                                                int currentNumber = 0;

//                                                if (!string.IsNullOrEmpty(lastNumber))
//                                                {
//                                                    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
//                                                }

//                                                string runningNumber = string.Format("{0:D3}", currentNumber + 1);

//                                                var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

//                                                header = new IssueSlipHeader()
//                                                {
//                                                    ID = TransactionId,
//                                                    Code = Code,
//                                                    Name = row[0].ToString(),
//                                                    TransactionStatus = "OPEN",
//                                                    CreatedBy = activeUser,
//                                                    CreatedOn = CreatedAt
//                                                };
//                                            }
//                                            else
//                                            {
//                                                throw new Exception("Issue Slip already exist.");
//                                            }
//                                        }
//                                        else if (dt.Rows.IndexOf(row) == 3)
//                                        {
//                                            header.ExcelCreatedBy = row[14].ToString();
//                                        }
//                                        else if (dt.Rows.IndexOf(row) == 4)
//                                        {
//                                            header.TotalRequestedQty = string.IsNullOrEmpty(row[5].ToString()) ? 0 : decimal.Parse(row[5].ToString());
//                                        }
//                                        else if (dt.Rows.IndexOf(row) > 8)
//                                        {
//                                            IssueSlipDetail detail = new IssueSlipDetail()
//                                            {
//                                                ID = Helper.CreateGuid(""),
//                                                HeaderID = header.ID,
//                                                MaterialCode = row[1].ToString(),
//                                                MaterialName = row[2].ToString(),
//                                                VendorName = row[3].ToString(),
//                                                RequestedQty = string.IsNullOrEmpty(row[4].ToString()) ? 0 : decimal.Parse(row[4].ToString())
//                                            };

//                                            header.IssueSlipDetails.Add(detail);
//                                        }
//                                    }

//                                    db.IssueSlipHeaders.Add(header);

//                                    await db.SaveChangesAsync();
//                                    message = "Upload succeeded.";
//                                    status = true;


//                                }
//                                catch (Exception e)
//                                {
//                                    message = string.Format("Upload item failed. {0}", e.Message);
//                                }
//                            }
//                            else
//                            {
//                                message = "Upload failed. Maximum allowed file size : 10MB ";
//                            }
//                        }
//                        else
//                        {
//                            message = "Upload item failed. File is invalid.";
//                        }
//                    }
//                    else
//                    {
//                        message = "No file uploaded.";
//                    }
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("ID", TransactionId);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        public async Task<IHttpActionResult> UpdateStatus(string id, string transactionStatus)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";

//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipHeader header = await db.IssueSlipHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

//                    if (transactionStatus.Equals("CLOSED"))
//                    {
//                        if (!header.TransactionStatus.Equals("PROGRESS"))
//                        {
//                            throw new Exception("Transaction can not be closed.");
//                        }

//                        message = "Data closing succeeded.";
//                    }
//                    else if (transactionStatus.Equals("CANCELLED"))
//                    {
//                        if (!header.TransactionStatus.Equals("OPEN"))
//                        {
//                            throw new Exception("Transaction can not be cancelled.");
//                        }

//                        message = "Data cancellation succeeded.";
//                    }
//                    else
//                    {
//                        throw new Exception("Transaction Status is not recognized.");
//                    }

//                    header.TransactionStatus = transactionStatus;
//                    header.ModifiedBy = activeUser;
//                    header.ModifiedOn = DateTime.Now;

//                    db.IssueSlipDetails.RemoveRange(header.IssueSlipDetails);


//                    await db.SaveChangesAsync();
//                    status = true;
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            return Ok(obj);
//        }

//        public async Task<IHttpActionResult> Cancel(string id)
//        {
//            return await UpdateStatus(id, "CANCELLED");
//        }

//        //public async Task<IHttpActionResult> Confirm(string id)
//        //{
//        //    return await UpdateStatus(id, "CONFIRMED");
//        //}

//        public async Task<IHttpActionResult> Close(string id)
//        {
//            return await UpdateStatus(id, "CLOSED");
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsagePickingManual(UsagePickingVM usagePickingVM)
//        {
//            usagePickingVM.PickingMethod = "Manual";
//            return await UsagePicking(usagePickingVM);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsagePickingScan(UsagePickingVM usagePickingVM)
//        {
//            usagePickingVM.PickingMethod = "Scan";
//            return await UsagePicking(usagePickingVM);
//        }

//        public async Task<IHttpActionResult> UsagePicking(UsagePickingVM usagePickingVM)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipHeader header = null;
//                    IssueSlipDetail detail = null;
//                    vStockAll stock = null;
//                    vStockAll fifoStock = null;
//                    IssueSlipList list = null;
//                    StockRM stockRM = null;
//                    StockSFG stockSFG = null;

//                    if (string.IsNullOrEmpty(usagePickingVM.HeaderID))
//                    {
//                        ModelState.AddModelError("IssueSlip.HeaderID", "Issue Slip ID is required.");
//                    }
//                    else
//                    {
//                        header = await db.IssueSlipHeaders.Where(s => s.ID.Equals(usagePickingVM.HeaderID)).FirstOrDefaultAsync();

//                        if (header == null)
//                        {
//                            ModelState.AddModelError("IssueSlip.HeaderID", "Issue Slip is not recognized.");
//                        }
//                        else
//                        {
//                            if (header.TransactionStatus.Equals("CLOSED"))
//                            {
//                                ModelState.AddModelError("IssueSlip.TransactionStatus", "Issue Slip is is already Closed.");
//                            }
//                            else if (header.TransactionStatus.Equals("CANCELLED"))
//                            {
//                                ModelState.AddModelError("IssueSlip.TransactionStatus", "Issue Slip is is already Cancelled.");
//                            }
//                        }
//                    }

//                    if (string.IsNullOrEmpty(usagePickingVM.Barcode))
//                    {
//                        ModelState.AddModelError("IssueSlip.Barcode", "Barcode is required.");
//                    }

//                    if (string.IsNullOrEmpty(usagePickingVM.LotNo))
//                    {
//                        ModelState.AddModelError("IssueSlip.LotNo", "Lot Number is required.");
//                    }
//                    else 
//                    {
//                        if (string.IsNullOrEmpty(usagePickingVM.BinRackCode))
//                        {
//                            ModelState.AddModelError("IssueSlip.BinRackCode", "Bin Rack Code is required.");
//                        }
//                        else
//                        {
//                            stock = await db.vStockAlls.Where(s => s.Barcode.Equals(usagePickingVM.Barcode) && s.LotNumber.Equals(usagePickingVM.LotNo) && s.BinRackCode.Equals(usagePickingVM.BinRackCode) && s.WarehouseType.Equals("EMIX")).FirstOrDefaultAsync();

//                            if (stock == null)
//                            {
//                                ModelState.AddModelError("IssueSlip.Barcode", "Raw Material in the given Barcode & Bin Rack is not recognized.");
//                            }
//                            else
//                            {
//                                if (usagePickingVM.SupplyQty <= 0)
//                                {
//                                    ModelState.AddModelError("IssueSlip.SupplyQty", "Picking quantity can not be zero or below.");
//                                }
//                                else if (usagePickingVM.SupplyQty * stock.QtyPerBag > stock.Quantity)
//                                {
//                                    ModelState.AddModelError("IssueSlip.SupplyQty", "Picking quantity can not exceed current stock quantity.");
//                                }

//                                detail = header.IssueSlipDetails.Where(s => s.MaterialCode.Equals(stock.MaterialCode)).FirstOrDefault();

//                                if (detail == null)
//                                {
//                                    ModelState.AddModelError("IssueSlip.Barcode", "Scanned Raw Material is not in current Issue Slip.");
//                                }
//                                else
//                                {
//                                    fifoStock = await db.vStockAlls
//                                        .Where(s => s.MaterialID.Equals(stock.MaterialID) 
//                                        && s.Quantity > 0
//                                        && s.WarehouseType.Equals("EMIX"))
//                                        .OrderByDescending(s => s.BinRackAreaType)
//                                        .ThenBy(s => s.ReceivedAt)
//                                        .ThenBy(s => s.InDate)
//                                        .ThenBy(s => s.ExpiredDate)
//                                        .FirstOrDefaultAsync();

//                                    if (fifoStock != null)
//                                    {
//                                        bool isFIFO = true;

//                                        if (!stock.Barcode.Equals(fifoStock.Barcode))
//                                        {
//                                            ModelState.AddModelError("IssueSlip.Barcode", string.Format("Scanned Barcode is not the same as the FIFO recommendation. Recommended Stock: {0}.", fifoStock.Barcode));
//                                            isFIFO = false;
//                                        }

//                                        if (!stock.LotNumber.Equals(fifoStock.LotNumber))
//                                        {
//                                            ModelState.AddModelError("IssueSlip.LotNo", string.Format("Scanned Lot Number is not the same as the FIFO recommendation. Recommended Stock: {0}.", fifoStock.LotNumber));
//                                            isFIFO = false;
//                                        }

//                                        if (!stock.BinRackID.Equals(fifoStock.BinRackID))
//                                        {
//                                            ModelState.AddModelError("IssueSlip.BinRackCode", string.Format("Scanned Bin Rack is not the same as the FIFO recommendation. Recommended: {0}.", fifoStock.BinRackCode));
//                                            isFIFO = false;
//                                        }

//                                        if (isFIFO)
//                                        {
//                                            if (DateTime.Now >= fifoStock.ExpiredDate)
//                                            {
//                                                ModelState.AddModelError("IssueSlip.Barcode", "Scanned Raw Material is already expired");
//                                            }
//                                        }
//                                    }
//                                    else
//                                    {
//                                        ModelState.AddModelError("IssueSlip.Barcode", "No available stock.");
//                                    }

//                                    if (usagePickingVM.SupplyQty * stock.QtyPerBag > detail.RequestedQty)
//                                    {
//                                        ModelState.AddModelError("IssueSlip.SupplyQty", "Picking quantity can not exceed current stock quantity.");
//                                    }

//                                    list = detail.IssueSlipLists.Where(s => s.Barcode.Equals(usagePickingVM.Barcode)).FirstOrDefault();

//                                    if (list != null)
//                                    {
//                                        if (list.UsageInspectedOn != null || list.UsageJudgeOn != null)
//                                        {
//                                            ModelState.AddModelError("IssueSlip.Barcode", "Material can no longer be Picked. Already inspected or judged.");
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    if (!ModelState.IsValid)
//                    {
//                        foreach (var state in ModelState)
//                        {
//                            string field = state.Key.Split('.')[1];
//                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
//                            customValidationMessages.Add(new CustomValidationMessage(field, value));
//                        }

//                        throw new Exception("Input is not valid");
//                    }

//                    bool isNew = false;

//                    if (list == null)
//                    {
//                        list = new IssueSlipList()
//                        {
//                            ID = Helper.CreateGuid(""),
//                            DetailID = detail.ID,
//                            Barcode = usagePickingVM.Barcode,
//                            LotNo = usagePickingVM.LotNo,
//                            UsageSupplyQty = usagePickingVM.SupplyQty * stock.QtyPerBag
//                        };

//                        isNew = true;
//                    }
//                    else
//                    {
//                        list.UsageSupplyQty += usagePickingVM.SupplyQty * stock.QtyPerBag;
//                    }
                    
//                    list.UsagePickingMethod = usagePickingVM.PickingMethod;
//                    list.UsagePickedOn = DateTime.Now;
//                    list.UsagePickedBy = activeUser;
//                    list.UsageBinRackID = stock.BinRackID;
//                    list.UsageBinRackCode = stock.BinRackCode;
//                    list.UsageBinRackName = stock.BinRackName;

//                    if (isNew)
//                    {
//                        db.IssueSlipLists.Add(list);
//                    }

//                    if (stock.Type.Equals("RM"))
//                    {
//                        stockRM = await db.StockRMs.Where(s => s.Barcode.Equals(stock.Barcode) && s.LotNumber.Equals(stock.LotNumber) && s.BinRackID.Equals(stock.BinRackID)).FirstOrDefaultAsync();

//                        stockRM.Quantity -= (list.UsageSupplyQty.HasValue ? list.UsageSupplyQty.Value : 0);
//                    }
//                    else if (stock.Type.Equals("SFG"))
//                    {
//                        stockSFG = await db.StockSFGs.Where(s => s.Barcode.Equals(stock.Barcode) && s.LotNumber.Equals(stock.LotNumber) && s.BinRackID.Equals(stock.BinRackID)).FirstOrDefaultAsync();

//                        stockSFG.Quantity -= (list.UsageSupplyQty.HasValue ? list.UsageSupplyQty.Value : 0);
//                    }

//                    if (header.TransactionStatus.Equals("OPEN"))
//                    {
//                        header.TransactionStatus = "PROGRESS";
//                    }

//                    await db.SaveChangesAsync();
//                    status = true;
//                    message = "Picking succeeded.";
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            obj.Add("error_validation", customValidationMessages);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsageInspectionManual(UsageInspectionVM usageInspectionVM)
//        {
//            usageInspectionVM.InspectionMethod = "Manual";
//            return await UsageInspect(usageInspectionVM);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsageInspectionScan(UsageInspectionVM usageInspectionVM)
//        {
//            usageInspectionVM.InspectionMethod = "Scan";
//            return await UsageInspect(usageInspectionVM);
//        }

//        public async Task<IHttpActionResult> UsageInspect(UsageInspectionVM usageInspectionVM)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipList list = null;

//                    if (string.IsNullOrEmpty(usageInspectionVM.HeaderID))
//                    {
//                        ModelState.AddModelError("Inspection.HeaderID", "Issue Slip is not recognized.");
//                    }
//                    else
//                    {
//                        IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.ID.Equals(usageInspectionVM.HeaderID)).FirstOrDefaultAsync();

//                        if (header == null)
//                        {
//                            ModelState.AddModelError("Inspection.HeaderID", "Issue Slip is not recognized.");
//                        }
//                        else
//                        {
//                            if (header.TransactionStatus.Equals("CLOSED"))
//                            {
//                                ModelState.AddModelError("Inspection.TransactionStatus", "Issue Slip is is already Closed.");
//                            }
//                        }
//                    }

//                    if (string.IsNullOrEmpty(usageInspectionVM.Barcode))
//                    {
//                        ModelState.AddModelError("Inspection.Barcode", "Barcode is required.");
//                    }

//                    if (string.IsNullOrEmpty(usageInspectionVM.LotNo))
//                    {
//                        ModelState.AddModelError("Inspection.LotNo", "Lot Number is required.");
//                    }
//                    else
//                    {
//                        if (!string.IsNullOrEmpty(usageInspectionVM.Barcode))
//                        {
//                            if (!string.IsNullOrEmpty(usageInspectionVM.HeaderID))
//                            {
//                                list = await db.IssueSlipLists.Where(s => s.Barcode.Equals(usageInspectionVM.Barcode) && s.IssueSlipDetail.HeaderID.Equals(usageInspectionVM.HeaderID)).FirstOrDefaultAsync();

//                                if (list == null)
//                                {
//                                    ModelState.AddModelError("Inspection.Barcode", "Barcode is not recognized.");
//                                }
//                                else
//                                {
//                                    if (list.UsagePickedOn == null)
//                                    {
//                                        ModelState.AddModelError("Inspection.Barcode", "Barcode has not been Picked.");
//                                    }

//                                    if (list.UsageInspectedOn != null)
//                                    {
//                                        ModelState.AddModelError("Judgement.Barcode", "Barcode has already been Usage Inspected.");
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    if (!ModelState.IsValid)
//                    {
//                        foreach (var state in ModelState)
//                        {
//                            string field = state.Key.Split('.')[1];
//                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
//                            customValidationMessages.Add(new CustomValidationMessage(field, value));
//                        }

//                        throw new Exception("Input is not valid");
//                    }

//                    list.UsageInspectionMethod = usageInspectionVM.InspectionMethod;
//                    list.UsageInspectedOn = DateTime.Now;
//                    list.UsageInspectedBy = activeUser;
//                    list.UsageQRLabel = usageInspectionVM.QRLabel;
//                    list.UsagePackage = usageInspectionVM.Package;

//                    await db.SaveChangesAsync();
//                    status = true;
//                    message = "Inspection succeeded.";
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            obj.Add("error_validation", customValidationMessages);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsageJudgementManual(UsageJudgementVM usageJudgementVM)
//        {
//            usageJudgementVM.JudgementMethod = "Manual";
//            return await UsageJudge(usageJudgementVM);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> UsageJudgementScan(UsageJudgementVM usageJudgementVM)
//        {
//            usageJudgementVM.JudgementMethod = "Scan";
//            return await UsageJudge(usageJudgementVM);
//        }

//        public async Task<IHttpActionResult> UsageJudge(UsageJudgementVM usageJudgementVM)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipList list = null;

//                    if (string.IsNullOrEmpty(usageJudgementVM.HeaderID))
//                    {
//                        ModelState.AddModelError("Judgement.HeaderID", "Issue Slip is not recognized.");
//                    }
//                    else
//                    {
//                        IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.ID.Equals(usageJudgementVM.HeaderID)).FirstOrDefaultAsync();

//                        if (header == null)
//                        {
//                            ModelState.AddModelError("Judgement.HeaderID", "Issue Slip is not recognized.");
//                        }
//                        else
//                        {
//                            if (header.TransactionStatus.Equals("CLOSED"))
//                            {
//                                ModelState.AddModelError("Judgement.TransactionStatus", "Issue Slip is is already Closed.");
//                            }
//                        }
//                    }

//                    if (string.IsNullOrEmpty(usageJudgementVM.Barcode))
//                    {
//                        ModelState.AddModelError("Judgement.Barcode", "Barcode is required.");
//                    }

//                    if (string.IsNullOrEmpty(usageJudgementVM.LotNo))
//                    {
//                        ModelState.AddModelError("Judgement.LotNo", "Lot Number is required.");
//                    }
//                    else
//                    {
//                        if (!string.IsNullOrEmpty(usageJudgementVM.Barcode))
//                        {
//                            if (!string.IsNullOrEmpty(usageJudgementVM.HeaderID))
//                            {
//                                list = await db.IssueSlipLists.Where(s => s.Barcode.Equals(usageJudgementVM.Barcode) && s.LotNo.Equals(usageJudgementVM.LotNo) && s.IssueSlipDetail.HeaderID.Equals(usageJudgementVM.HeaderID)).FirstOrDefaultAsync();

//                                if (list == null)
//                                {
//                                    ModelState.AddModelError("Judgement.Barcode", "Barcode is not recognized.");
//                                }
//                                else
//                                {
//                                    if (list.UsagePickedOn == null)
//                                    {
//                                        ModelState.AddModelError("Judgement.Barcode", "Barcode has not been Picked.");
//                                    }

//                                    if (list.UsageJudgeOn != null)
//                                    {
//                                        ModelState.AddModelError("Judgement.Barcode", "Barcode has already been Judged.");
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    if (!ModelState.IsValid)
//                    {
//                        foreach (var state in ModelState)
//                        {
//                            string field = state.Key.Split('.')[1];
//                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
//                            customValidationMessages.Add(new CustomValidationMessage(field, value));
//                        }

//                        throw new Exception("Input is not valid");
//                    }

//                    list.UsageJudgementMethod = usageJudgementVM.JudgementMethod;
//                    list.UsageJudgeOn = DateTime.Now;
//                    list.UsageJudgeBy = activeUser;
//                    list.UsageExpDate = usageJudgementVM.ExpDate;
//                    list.UsageApproveStamp = usageJudgementVM.ApproveStamp;

//                    await db.SaveChangesAsync();
//                    status = true;
//                    message = "Judgement succeeded.";
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            obj.Add("error_validation", customValidationMessages);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> ReturnPutawayManual(ReturnPutawayVM returnPutawayVM)
//        {
//            returnPutawayVM.PutawayMethod = "Manual";
//            return await ReturnPutaway(returnPutawayVM);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> ReturnPutawayScan(ReturnPutawayVM returnPutawayVM)
//        {
//            returnPutawayVM.PutawayMethod = "Scan";
//            return await ReturnPutaway(returnPutawayVM);
//        }

//        public async Task<IHttpActionResult> ReturnPutaway(ReturnPutawayVM returnPutawayVM)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipList list = null;
//                    BinRack binRack = null;
//                    vStockAll stockTemp = null;
//                    StockRM stockRM = null;
//                    StockSFG stockSFG = null;

//                    if (string.IsNullOrEmpty(returnPutawayVM.HeaderID))
//                    {
//                        ModelState.AddModelError("Putaway.HeaderID", "Issue Slip is not recognized.");
//                    }
//                    else
//                    {
//                        IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.ID.Equals(returnPutawayVM.HeaderID)).FirstOrDefaultAsync();

//                        if (header == null)
//                        {
//                            ModelState.AddModelError("Putaway.HeaderID", "Issue Slip is not recognized.");
//                        }
//                        else
//                        {
//                            if (header.TransactionStatus.Equals("CLOSED"))
//                            {
//                                ModelState.AddModelError("Putaway.TransactionStatus", "Issue Slip is is already Closed.");
//                            }

//                            if (header.TransactionStatus.Equals("CANCELLED"))
//                            {
//                                ModelState.AddModelError("Putaway.TransactionStatus", "Issue Slip is is already Cancelled.");
//                            }
//                        }
//                    }

//                    if (string.IsNullOrEmpty(returnPutawayVM.BinRackCode))
//                    {
//                        ModelState.AddModelError("Putaway.BinRackCode", "Bin Rack Code is required.");
//                    }
//                    else
//                    {
//                        binRack = await db.BinRacks.Where(s => s.Code.Equals(returnPutawayVM.BinRackCode)).FirstOrDefaultAsync();

//                        if (binRack == null)
//                        {
//                            ModelState.AddModelError("Putaway.BinRackCode", "Bin Rack is not recognized.");
//                        }
//                    }

//                    if (string.IsNullOrEmpty(returnPutawayVM.Barcode))
//                    {
//                        ModelState.AddModelError("Putaway.Barcode", "Barcode is required.");
//                    }

//                    if (string.IsNullOrEmpty(returnPutawayVM.LotNo))
//                    {
//                        ModelState.AddModelError("Putaway.LotNo", "Lot Number is required.");
//                    }
//                    else
//                    {
//                        if (!string.IsNullOrEmpty(returnPutawayVM.Barcode))
//                        {
//                            if (!string.IsNullOrEmpty(returnPutawayVM.HeaderID))
//                            {
//                                list = await db.IssueSlipLists.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) && s.IssueSlipDetail.HeaderID.Equals(returnPutawayVM.HeaderID)).FirstOrDefaultAsync();

//                                if (list == null)
//                                {
//                                    ModelState.AddModelError("Putaway.Barcode", "Barcode is not recognized.");
//                                }
//                                else
//                                {
//                                    //if (list.ReturnPutOn != null)
//                                    //{
//                                    //    ModelState.AddModelError("Putaway.Barcode", "Raw Material in the given Barcode has already been Put Away.");
//                                    //}

//                                    stockTemp = await db.vStockAlls.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) & s.LotNumber.Equals(returnPutawayVM.LotNo)).FirstOrDefaultAsync();

//                                    if (stockTemp == null)
//                                    {
//                                        ModelState.AddModelError("Putaway.Barcode", "Barcode is not recognized.");
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    if (returnPutawayVM.ActualReturnQty <= 0)
//                    {
//                        ModelState.AddModelError("Putaway.ActualReturnQty", "Actual Return Qty can not be zero or below.");
//                    }
//                    else
//                    { 
//                        if (list != null)
//                        {
//                            if (returnPutawayVM.ActualReturnQty * stockTemp.QtyPerBag > (list.UsageSupplyQty.HasValue ? list.UsageSupplyQty.Value : 0))
//                            {
//                                ModelState.AddModelError("Putaway.ActualReturnQty", "Actual Return Qty can not exceed the Usage Supply Qty.");
//                            }
//                        }
//                    }

//                    if (!ModelState.IsValid)
//                    {
//                        foreach (var state in ModelState)
//                        {
//                            string field = state.Key.Split('.')[1];
//                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
//                            customValidationMessages.Add(new CustomValidationMessage(field, value));
//                        }

//                        throw new Exception("Input is not valid");
//                    }

//                    list.ReturnActualQty = returnPutawayVM.ActualReturnQty * stockTemp.QtyPerBag;
//                    list.ReturnPutawayMethod = returnPutawayVM.PutawayMethod;
//                    list.ReturnPutOn = DateTime.Now;
//                    list.ReturnPutBy = activeUser;
//                    list.ReturnBinRackID = binRack.ID;
//                    list.ReturnBinRackCode = binRack.Code;
//                    list.ReturnBinRackName = binRack.Name;

//                    bool isNew = false;

//                    if (stockTemp.Type.Equals("RM"))
//                    {
//                        stockRM = await db.StockRMs.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) && s.LotNumber.Equals(returnPutawayVM.LotNo) && s.BinRackID.Equals(binRack.ID)).FirstOrDefaultAsync();

//                        if (stockRM == null)
//                        {
//                            StockRM temp = await db.StockRMs.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) && s.LotNumber.Equals(returnPutawayVM.LotNo) && s.BinRackID.Equals(list.UsageBinRackID)).FirstOrDefaultAsync();

//                            isNew = true;
//                            stockRM = new StockRM()
//                            {
//                                ID = Helper.CreateGuid(""),
//                                RawMaterialID = temp.RawMaterialID,
//                                Barcode = returnPutawayVM.Barcode,
//                                QtyPerBag = temp.QtyPerBag,
//                                LotNumber = temp.LotNumber,
//                                InDate = temp.InDate,
//                                ExpiredDate = temp.ExpiredDate,
//                                Quantity = 0,
//                                BinRackID = binRack.ID,
//                                ReceivedAt = DateTime.Now
//                            };
//                        }

//                        stockRM.Quantity += returnPutawayVM.ActualReturnQty * stockTemp.QtyPerBag;

//                        if (isNew)
//                        {
//                            db.StockRMs.Add(stockRM);
//                        }
//                    }
//                    else if (stockTemp.Type.Equals("SFG"))
//                    {
//                        stockSFG = await db.StockSFGs.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) && s.LotNumber.Equals(returnPutawayVM.LotNo) && s.BinRackID.Equals(binRack.ID)).FirstOrDefaultAsync();

//                        if (stockSFG == null)
//                        {
//                            StockSFG temp = await db.StockSFGs.Where(s => s.Barcode.Equals(returnPutawayVM.Barcode) && s.LotNumber.Equals(returnPutawayVM.LotNo) && s.BinRackID.Equals(list.UsageBinRackID)).FirstOrDefaultAsync();

//                            isNew = true;
//                            stockSFG = new StockSFG()
//                            {
//                                ID = Helper.CreateGuid(""),
//                                SemiFinishGoodID = temp.SemiFinishGoodID,
//                                Barcode = returnPutawayVM.Barcode,
//                                QtyPerBag = temp.QtyPerBag,
//                                LotNumber = temp.LotNumber,
//                                InDate = temp.InDate,
//                                ExpiredDate = temp.ExpiredDate,
//                                Quantity = 0,
//                                BinRackID = binRack.ID,
//                                ReceivedAt = DateTime.Now
//                            };
//                        }

//                        stockSFG.Quantity += returnPutawayVM.ActualReturnQty * stockTemp.QtyPerBag;

//                        if (isNew)
//                        {
//                            db.StockSFGs.Add(stockSFG);
//                        }
//                    }

//                    await db.SaveChangesAsync();
//                    status = true;
//                    message = "Putaway succeeded.";
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            obj.Add("error_validation", customValidationMessages);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> ReturnInspectionManual(ReturnInspectionVM returnInspectionVM)
//        {
//            returnInspectionVM.InspectionMethod = "Manual";
//            return await ReturnInspect(returnInspectionVM);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> ReturnInspectionScan(ReturnInspectionVM returnInspectionVM)
//        {
//            returnInspectionVM.InspectionMethod = "Scan";
//            return await ReturnInspect(returnInspectionVM);
//        }

//        public async Task<IHttpActionResult> ReturnInspect(ReturnInspectionVM returnInspectionVM)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

//            string message = "";
//            bool status = false;
//            var re = Request;
//            var headers = re.Headers;

//            try
//            {
//                string token = "";
//                if (headers.Contains("token"))
//                {
//                    token = headers.GetValues("token").First();
//                }

//                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

//                if (activeUser != null)
//                {
//                    IssueSlipList list = null;

//                    if (string.IsNullOrEmpty(returnInspectionVM.HeaderID))
//                    {
//                        ModelState.AddModelError("Inspection.HeaderID", "Issue Slip is not recognized.");
//                    }
//                    else
//                    {
//                        IssueSlipHeader header = await db.IssueSlipHeaders.Where(s => s.ID.Equals(returnInspectionVM.HeaderID)).FirstOrDefaultAsync();

//                        if (header == null)
//                        {
//                            ModelState.AddModelError("Inspection.HeaderID", "Issue Slip is not recognized.");
//                        }
//                        else
//                        {
//                            if (header.TransactionStatus.Equals("CLOSED"))
//                            {
//                                ModelState.AddModelError("Inspection.TransactionStatus", "Issue Slip is is already Closed.");
//                            }
//                        }
//                    }

//                    if (string.IsNullOrEmpty(returnInspectionVM.Barcode))
//                    {
//                        ModelState.AddModelError("Inspection.Barcode", "Barcode is required.");
//                    }

//                    if (string.IsNullOrEmpty(returnInspectionVM.LotNo))
//                    {
//                        ModelState.AddModelError("Inspection.LotNo", "Lot Number is required.");
//                    }
//                    else
//                    {
//                        if (!string.IsNullOrEmpty(returnInspectionVM.Barcode))
//                        {
//                            if (!string.IsNullOrEmpty(returnInspectionVM.HeaderID))
//                            {
//                                list = await db.IssueSlipLists.Where(s => s.Barcode.Equals(returnInspectionVM.Barcode) && s.IssueSlipDetail.HeaderID.Equals(returnInspectionVM.HeaderID)).FirstOrDefaultAsync();

//                                if (list == null)
//                                {
//                                    ModelState.AddModelError("Inspection.Barcode", "Barcode is not recognized.");
//                                }
//                                else
//                                {
//                                    if (list.ReturnPutOn == null)
//                                    {
//                                        ModelState.AddModelError("Inspection.Barcode", "Raw Material in the given Barcode has not been Put Away.");
//                                    }

//                                    if (list.ReturnInspectedOn != null)
//                                    {
//                                        ModelState.AddModelError("Inspection.Barcode", "Raw Material in the given Barcode has already been Return Inspected.");
//                                    }
//                                }
//                            }
//                        }
//                    }

//                    if (!ModelState.IsValid)
//                    {
//                        foreach (var state in ModelState)
//                        {
//                            string field = state.Key.Split('.')[1];
//                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
//                            customValidationMessages.Add(new CustomValidationMessage(field, value));
//                        }

//                        throw new Exception("Input is not valid");
//                    }

//                    list.ReturnInspectionMethod = returnInspectionVM.InspectionMethod;
//                    list.ReturnInspectedOn = DateTime.Now;
//                    list.ReturnInspectedBy = activeUser;
//                    list.ReturnQRLabel = returnInspectionVM.QRLabel;
//                    list.ReturnPackage = returnInspectionVM.Package;

//                    await db.SaveChangesAsync();
//                    status = true;
//                    message = "Inspection succeeded.";
//                }
//                else
//                {
//                    message = "Token is no longer valid. Please re-login.";
//                }
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("status", status);
//            obj.Add("message", message);
//            obj.Add("error_validation", customValidationMessages);

//            return Ok(obj);
//        }

//        [HttpGet]
//        public async Task<IHttpActionResult> GetRecommended(string rawMaterialCode)
//        {
//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;
//            IEnumerable<StockRMDTO> data = null;

//            try
//            {
//                List<StockRM> list = await db.StockRMs.Where(s => s.RawMaterial.Code.Equals(rawMaterialCode) && s.Quantity > 0).OrderBy(s => s.InDate).ThenBy(s => s.ReceivedAt).ToListAsync();

//                data = from x in list
//                       select new StockRMDTO
//                       {
//                           ID = x.ID,
//                           Barcode = x.Barcode,
//                           RawMaterialID = x.RawMaterialID,
//                           MaterialCode = x.RawMaterial.Code,
//                           MaterialName = x.RawMaterial.Name,
//                           Qty = Helper.FormatThousand(x.Quantity),
//                           LotNo = x.LotNumber,
//                           InDate = Helper.NullDateToString2(x.InDate),
//                           ExpDate = Helper.NullDateToString2(x.ExpiredDate),
//                           BinRackID = x.BinRackID,
//                           BinRackCode = x.BinRack.Code,
//                           BinRackName = x.BinRack.Name,
//                           BinRackAreaID = x.BinRack.BinRackAreaID,
//                           BinRackAreaCode = x.BinRack.BinRackAreaCode,
//                           BinRackAreaName = x.BinRack.BinRackAreaName,
//                           WarehouseID = x.BinRack.WarehouseID,
//                           WarehouseCode = x.BinRack.WarehouseCode,
//                           WarehouseName = x.BinRack.WarehouseName,
//                           ReceivedAt = Helper.NullDateTimeToString(x.ReceivedAt)
//                       };

//                status = true;
//                message = "Fetch data succeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("data", data);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }

//        [HttpPost]
//        public async Task<IHttpActionResult> DatatableRecommended(string detailId)
//        {
//            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
//            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
//            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
//            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
//            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
//            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
//            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

//            Dictionary<string, object> obj = new Dictionary<string, object>();
//            string message = "";
//            bool status = false;
//            HttpRequest request = HttpContext.Current.Request;

//            IssueSlipDetail det = await db.IssueSlipDetails.Where(s => s.ID.Equals(detailId)).FirstOrDefaultAsync();

//            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
//            IEnumerable<RecommendedStockDTO> pagedData = Enumerable.Empty<RecommendedStockDTO>();

//            int max = 10;
//            int count = 1;
//            int skip = 0;
//            decimal sum = 0;
//            bool isComplete = false;

//            while (!isComplete)
//            {
//                List<decimal> tempList = db.vStockAlls.Where(s => s.MaterialCode.Equals(det.MaterialCode) && s.Quantity > 0
//                && s.WarehouseType.Equals("EMIX"))
//                .OrderByDescending(s => s.BinRackAreaType)
//                .ThenBy(s => s.ReceivedAt)
//                .ThenBy(s => s.InDate)
//                .ThenBy(s => s.ExpiredDate)
//                .Skip(skip)
//                .Take(max).Select(s => s.Quantity).ToList();

//                foreach (decimal d in tempList)
//                {
//                    sum += d;

//                    if (sum >= det.RequestedQty)
//                    {
//                        isComplete = true;
//                        break;
//                    }
//                    else
//                    {
//                        count++;
//                    }
//                }

//                if (!isComplete)
//                {
//                    if (count < max)
//                    {
//                        isComplete = true;
//                    }
//                    else
//                    {
//                        skip = max;
//                        max += 10;
//                    }
//                }
//            }

//            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(det.MaterialCode) && s.Quantity > 0)
//                .OrderByDescending(s => s.BinRackAreaType)
//                .ThenBy(s => s.ReceivedAt)
//                .ThenBy(s => s.InDate)
//                .ThenBy(s => s.ExpiredDate)
//                .Take(count).AsQueryable();

//            int recordsTotal = db.vStockAlls.Where(s => s.MaterialCode.Equals(det.MaterialCode) && s.Quantity > 0)
//                .OrderByDescending(s => s.BinRackAreaType)
//                .ThenBy(s => s.ReceivedAt)
//                .ThenBy(s => s.InDate)
//                .ThenBy(s => s.ExpiredDate)
//                .Take(count).Count();
//            int recordsFiltered = 0;

//            try
//            {
//                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
//                cols.Add("ID", x => x.ID);
//                cols.Add("Barcode", x => x.Barcode);
//                cols.Add("BinRackID", x => x.BinRackID);
//                cols.Add("BinRackCode", x => x.BinRackCode);
//                cols.Add("BinRackName", x => x.BinRackName);
//                cols.Add("BinRackAreaID", x => x.BinRackAreaID);
//                cols.Add("BinRackAreaCode", x => x.BinRackAreaCode);
//                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
//                cols.Add("WarehouseID", x => x.WarehouseID);
//                cols.Add("WarehouseCode", x => x.WarehouseCode);
//                cols.Add("WarehouseName", x => x.WarehouseName);
//                cols.Add("RawMaterialID", x => x.MaterialID);
//                cols.Add("MaterialCode", x => x.MaterialCode);
//                cols.Add("MaterialName", x => x.MaterialName);
//                cols.Add("Qty", x => x.Quantity);
//                cols.Add("LotNo", x => x.LotNumber);
//                cols.Add("InDate", x => x.InDate);
//                cols.Add("ExpDate", x => x.ExpiredDate);
//                cols.Add("IsExpired", x => DateTime.Now >= x.ExpiredDate);
//                cols.Add("QCInspected", x => x.QCInspected);
//                cols.Add("ReceivedAt", x => x.ReceivedAt);

//                if (sortDirection.Equals("asc"))
//                    list = query.OrderBy(cols[sortName]);
//                else
//                    list = query.OrderByDescending(cols[sortName]);

//                recordsFiltered = list.Count();

//                list = list.Skip(start).Take(length).ToList();

//                if (list != null && list.Count() > 0)
//                {
//                    pagedData = from x in list
//                                select new RecommendedStockDTO
//                                {
//                                    ID = x.ID,
//                                    Barcode = x.Barcode,
//                                    BinRackID = x.BinRackID,
//                                    BinRackCode = x.BinRackCode,
//                                    BinRackName = x.BinRackName,
//                                    BinRackAreaID = x.BinRackAreaID,
//                                    BinRackAreaCode = x.BinRackAreaCode,
//                                    BinRackAreaName = x.BinRackAreaName,
//                                    WarehouseID = x.WarehouseID,
//                                    WarehouseCode = x.WarehouseCode,
//                                    WarehouseName = x.WarehouseName,
//                                    RawMaterialID = x.MaterialID,
//                                    MaterialCode = x.MaterialCode,
//                                    MaterialName = x.MaterialName,
//                                    Qty = Helper.FormatThousand(x.Quantity),
//                                    LotNo = x.LotNumber,
//                                    InDate = Helper.NullDateToString2(x.InDate),
//                                    ExpDate = Helper.NullDateToString2(x.ExpiredDate),
//                                    IsExpired = DateTime.Now.Date >= x.ExpiredDate.Date,
//                                    QCInspected = x.QCInspected.HasValue ? x.QCInspected.Value : false,
//                                    ReceivedAt = Helper.NullDateTimeToString(x.ReceivedAt)
//                                };
//                }

//                status = true;
//                message = "Fetch data succeeded.";
//            }
//            catch (HttpRequestException reqpEx)
//            {
//                message = reqpEx.Message;
//                return BadRequest();
//            }
//            catch (HttpResponseException respEx)
//            {
//                message = respEx.Message;
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//            }

//            obj.Add("draw", draw);
//            obj.Add("recordsTotal", recordsTotal);
//            obj.Add("recordsFiltered", recordsFiltered);
//            obj.Add("data", pagedData);
//            obj.Add("status", status);
//            obj.Add("message", message);

//            return Ok(obj);
//        }
//    }
//}
