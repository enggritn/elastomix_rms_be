using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
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
using System.IO;

namespace WMS_BE.Controllers.Api
{
    public class StockOpnameController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> DatatableHeader(string transactionStatus)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<StockOpnameHeader> list = Enumerable.Empty<StockOpnameHeader>();
            IEnumerable<StockOpnameHeaderDTO> pagedData = Enumerable.Empty<StockOpnameHeaderDTO>();

            IQueryable<StockOpnameHeader> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.StockOpnameHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.StockOpnameHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else if (transactionStatus.Equals("OPEN/CONFIRMED"))
            {
                query = db.StockOpnameHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("CONFIRMED") || s.TransactionStatus.Equals("CLOSED")).AsQueryable();
            }
            else
            {
                query = db.StockOpnameHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.WarehouseCode.Contains(search)
                        || m.WarehouseName.Contains(search)
                        || m.Remarks.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<StockOpnameHeader, object>> cols = new Dictionary<string, Func<StockOpnameHeader, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("WarehouseCode", x => x.WarehouseCode);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new StockOpnameHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    Remarks = x.Remarks,
                                    WarehouseCode = x.WarehouseCode,
                                    WarehouseName = x.WarehouseName,
                                    CreatedBy = x.CreatedBy,
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy ?? "",
                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
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
        public async Task<IHttpActionResult> Create(StockOpnameHeaderVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string id = null;
            string warehouseName = string.Empty;
            string warehouseCode = string.Empty;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.WarehouseCode))
                    {
                        ModelState.AddModelError("Outbound.WarehouseCode", "Warehouse is required.");
                    }
                    else
                    {
                        if (dataVM.WarehouseCode == "ALL")
                        {
                            warehouseCode = "ALL";
                            warehouseName = "All Emix Warehouses";
                        }
                        else
                        {
                            var temp = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();

                            if (temp == null)
                            {
                                ModelState.AddModelError("Outbound.WarehouseCode", "Warehouse is not recognized.");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.Remarks))
                    {
                        ModelState.AddModelError("Outbound.Remarks", "Remarks is required.");
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                   

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("STO");

                    string prefix = TransactionId.Substring(0, 3);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.StockOpnameHeaders.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                    // Ref Number necessities
                    string yearMonth = CreatedAt.Year.ToString() + month.ToString();

                    //check prev STO

                    StockOpnameHeader stockOpname = await db.StockOpnameHeaders.Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month) && x.TransactionStatus.Equals("OPEN")).FirstOrDefaultAsync();

                    if(stockOpname != null) 
                    {
                        //throw new Exception("Stock Opname still on progress.");
                        stockOpname.TransactionStatus = "CLOSED";
                        stockOpname.ModifiedOn = CreatedAt;
                        stockOpname.ModifiedBy = activeUser;
                    }

                    
                    if (string.IsNullOrEmpty(warehouseCode))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();
                        warehouseCode = wh.Code;
                        warehouseName = wh.Name;
                    }

                    StockOpnameHeader header = new StockOpnameHeader
                    {
                        ID = TransactionId,
                        Code = Code,
                        Remarks = dataVM.Remarks,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        WarehouseCode = warehouseCode,
                        WarehouseName = warehouseName
                    };


                    id = header.ID;

                    db.StockOpnameHeaders.Add(header);

                    //get warehouse codes
                    string[] warehouseCodes = { };
                    if(!warehouseCode.Equals("ALL"))
                    {
                        warehouseCodes = new string[1] { warehouseCode};
                    }
                    else
                    {
                        warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                    }
                    //select all stock based on warehouse code, if all, get only emix warehouses
                    List<vStockAll> stocks = db.vStockAlls.Where(m => m.Quantity > 0 && warehouseCodes.Contains(m.WarehouseCode)).ToList();

                    //insert stocks to stock opname detail
                    foreach(vStockAll stock in stocks)
                    {
                        StockOpnameDetail detail = new StockOpnameDetail();
                        detail.ID = Helper.CreateGuid("STOd");
                        detail.HeaderID = header.ID;
                        detail.MaterialCode = stock.MaterialCode;
                        detail.MaterialName = stock.MaterialName;
                        detail.MaterialType = stock.Type;
                        detail.LotNo = stock.LotNumber;
                        detail.InDate = stock.InDate.Value;
                        detail.ExpDate = stock.ExpiredDate.Value;
                        detail.BagQty = stock.BagQty.Value;
                        if (detail.BagQty == 1 )
                        {
                            detail.StockCode = stock.Code.Replace(',','.');
                            detail.QtyPerBag = stock.QtyPerBag;
                        }
                        else
                        {
                            detail.StockCode = stock.Code;
                            detail.QtyPerBag = stock.QtyPerBag;
                        }
                        detail.BinRackCode = stock.BinRackCode;
                        detail.BinRackName = stock.BinRackName;

                        db.StockOpnameDetails.Add(detail);
                    }


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Create data succeeded.";

                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("id", id);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            StockOpnameHeaderDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null || header.TransactionStatus != "OPEN")
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new StockOpnameHeaderDTO
                {
                    ID = header.ID,
                    Code = header.Code,
                    WarehouseCode = header.WarehouseCode,
                    WarehouseName = header.WarehouseName,
                    Remarks = header.Remarks,
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = header.CreatedOn.ToString(),
                    ModifiedBy = header.ModifiedBy != null ? header.ModifiedBy : "",
                    ModifiedOn = header.ModifiedOn.ToString()
                };


                status = true;
                message = "Fetch data succeded.";

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

            obj.Add("data", dataDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        public async Task<IHttpActionResult> UpdateStatus(string id, string transactionStatus)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    StockOpnameHeader header = await db.StockOpnameHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                    if (header.TransactionStatus.Equals("CANCELLED"))
                    {
                        throw new Exception("Can not change transaction status. Transaction is already cancelled.");
                    }

                    if (transactionStatus.Equals("CANCELLED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaction can not be cancelled.");
                    }

                    if (transactionStatus.Equals("CLOSED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaction can not be closed.");
                    }

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    if (tokenDate.LoginDate < header.CreatedOn.Date)
                    {
                        throw new Exception("Bad Login date.");
                    }

                    header.TransactionStatus = transactionStatus;
                    header.ModifiedBy = activeUser;
                    header.ModifiedOn = transactionDate;

                    if (transactionStatus.Equals("CANCELLED"))
                    {
                        db.StockOpnameDetails.RemoveRange(header.StockOpnameDetails);

                        message = "Cancel data succeeded.";
                    }

                    if (transactionStatus.Equals("CLOSED"))
                    {
                        //check detail
                        if (header.StockOpnameDetails.Count() < 1)
                        {
                            throw new Exception("Stock opname can not be empty.");
                        }

                        message = "Close data succeeded.";
                    }

                    await db.SaveChangesAsync();
                    status = true;
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
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

            obj.Add("status", status);
            obj.Add("message", message);
            return Ok(obj);
        }

        public async Task<IHttpActionResult> Cancel(string id)
        {
            return await UpdateStatus(id, "CANCELLED");
        }

        public async Task<IHttpActionResult> Close(string id)
        {
            return await UpdateStatus(id, "CLOSED");
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableDetail(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            string token = "";

            if (headers.Contains("token"))
            {
                token = headers.GetValues("token").First();
            }

            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

            User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
            string userAreaType = userData.AreaType;

            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<StockOpnameDetail> list = Enumerable.Empty<StockOpnameDetail>();
            IEnumerable<StockOpnameDetailDTO> pagedData = Enumerable.Empty<StockOpnameDetailDTO>();

            IQueryable<StockOpnameDetail> query = db.StockOpnameDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            if (userAreaType == "PRODUCTION")
            {
                List<string> binrackarea = db.BinRackAreas.Where(x => x.Type.Equals(userAreaType)).Select(d => d.Code).ToList();
                query = query.Where(a => binrackarea.Contains(a.BinRackCode.Substring(0, 5)));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<StockOpnameDetail, object>> cols = new Dictionary<string, Func<StockOpnameDetail, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("HeaderID", x => x.HeaderID);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("TotalBagQty", x => x.BagQty);
                cols.Add("ScannedBagQty", x => x.ActualBagQty);
                cols.Add("UnscannedBagQty", x => x.BagQty - x.ActualBagQty);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("IsScanned", x => x.ActualBagQty >= x.BagQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from data in list
                                select new StockOpnameDetailDTO
                                {
                                    ID = data.ID,
                                    HeaderID = data.HeaderID,
                                    MaterialCode = data.MaterialCode,
                                    MaterialName = data.MaterialName,
                                    MaterialType = data.MaterialType,
                                    LotNo = data.LotNo,
                                    InDate = Helper.NullDateToString(data.InDate),
                                    ExpDate = Helper.NullDateToString(data.ExpDate),
                                    //TotalQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    //ScannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    //UnscannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    TotalBagQty = Helper.FormatThousand(data.BagQty),
                                    ScannedBagQty = Helper.FormatThousand(data.ActualBagQty),
                                    UnscannedBagQty = Helper.FormatThousand(data.BagQty - data.ActualBagQty),
                                    BinRackCode = data.BinRackCode,
                                    BinRackName = data.BinRackName,
                                    IsScanned = data.ActualBagQty >= data.BagQty
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

        [HttpGet]
        public async Task<IHttpActionResult> GetDataStockOpname(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<vStockOpnameDTO> list = new List<vStockOpnameDTO>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                IQueryable<vStockOpname> query = db.vStockOpnames.Where(m => m.ID.Equals(id)).AsQueryable();
                IEnumerable<vStockOpname> tempList = query.ToList();

                foreach (vStockOpname rec in tempList)
                {
                    vStockOpnameDTO data = new vStockOpnameDTO();
                    data.ID = rec.ID;
                    data.Code = rec.Code;
                    data.BinRackCode = rec.BinRackCode;
                    data.MaterialCode = rec.MaterialCode;
                    data.MaterialName = rec.MaterialName;
                    data.LotNo = rec.LotNo;
                    data.InDate = rec.InDate;
                    data.ExpDate = rec.ExpDate;
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(rec.BagQty));
                    data.QtyPerBag = Helper.FormatThousand(rec.QtyPerBag);
                    data.TotalQty = Helper.FormatThousand(rec.TotalQty);
                    data.MaterialType = rec.MaterialType;
                    data.ScannedBy = rec.ScannedBy;
                    data.ScannedOn = rec.ScannedOn;

                    list.Add(data);
                }

                status = true;
                message = "Fetch data succeded.";

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

            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableExport(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<StockOpnameDetail> list = Enumerable.Empty<StockOpnameDetail>();
            IEnumerable<StockOpnameDetailDTO> pagedData = Enumerable.Empty<StockOpnameDetailDTO>();

            IQueryable<StockOpnameDetail> query = db.StockOpnameDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<StockOpnameDetail, object>> cols = new Dictionary<string, Func<StockOpnameDetail, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("HeaderID", x => x.HeaderID);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("TotalBagQty", x => x.BagQty);
                cols.Add("ScannedBagQty", x => x.ActualBagQty);
                cols.Add("UnscannedBagQty", x => x.BagQty - x.ActualBagQty);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("IsScanned", x => x.ActualBagQty >= x.BagQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from data in list
                                select new StockOpnameDetailDTO
                                {
                                    ID = data.ID,
                                    HeaderID = data.HeaderID,
                                    MaterialCode = data.MaterialCode,
                                    MaterialName = data.MaterialName,
                                    MaterialType = data.MaterialType,
                                    LotNo = data.LotNo,
                                    InDate = Helper.NullDateToString(data.InDate),
                                    ExpDate = Helper.NullDateToString(data.ExpDate),
                                    //TotalQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    //ScannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    //UnscannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                                    TotalBagQty = Helper.FormatThousand(data.BagQty),
                                    ScannedBagQty = Helper.FormatThousand(data.ActualBagQty),
                                    UnscannedBagQty = Helper.FormatThousand(data.BagQty - data.ActualBagQty),
                                    BinRackCode = data.BinRackCode,
                                    BinRackName = data.BinRackName,
                                    IsScanned = data.ActualBagQty >= data.BagQty
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
