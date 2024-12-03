using ExcelDataReader;
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
using System.Drawing;
using System.Configuration;
using ZXing;
using ZXing.QrCode;
using Rectangle = iText.Kernel.Geom.Rectangle;
using System.Web.Routing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf;
using System.Security.Cryptography;
using iText.Kernel.Utils;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Html2pdf;
using NPOI.Util;

namespace WMS_BE.Controllers.Api
{
    public class OutboundController : ApiController
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

            IEnumerable<OutboundHeader> list = Enumerable.Empty<OutboundHeader>();
            IEnumerable<OutboundHeaderDTO> pagedData = Enumerable.Empty<OutboundHeaderDTO>();

            IQueryable<OutboundHeader> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.OutboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.OutboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else if (transactionStatus.Equals("OPEN/CONFIRMED"))
            {
                query = db.OutboundHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("CONFIRMED")).AsQueryable();
            }
            else
            {
                query = db.OutboundHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
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

                Dictionary<string, Func<OutboundHeader, object>> cols = new Dictionary<string, Func<OutboundHeader, object>>();
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
                                select new OutboundHeaderDTO
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
        public async Task<IHttpActionResult> Create(OutboundHeaderVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string id = null;

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
                        var temp = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();

                        if (temp == null)
                        {
                            ModelState.AddModelError("Outbound.WarehouseCode", "Warehouse is not recognized.");
                        }
                    }

                    //if (string.IsNullOrEmpty(dataVM.Remarks))
                    //{
                    //    ModelState.AddModelError("Outbound.Remarks", "Remarks is required.");
                    //}

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
                    var TransactionId = Helper.CreateGuid("OUT");

                    string prefix = TransactionId.Substring(0, 3);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.OutboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
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

                    OutboundHeader header = new OutboundHeader
                    {
                        ID = TransactionId,
                        Code = Code,
                        Remarks = dataVM.Remarks,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                    };

                    Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();
                    header.WarehouseCode = wh.Code;
                    header.WarehouseName = wh.Name;

                    id = header.ID;

                    db.OutboundHeaders.Add(header);

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

        [HttpPost]
        public async Task<IHttpActionResult> Update(OutboundHeaderVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string id = null;

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
                    if (string.IsNullOrEmpty(dataVM.ID))
                    {
                        throw new Exception("Outbound ID is required.");
                    }

                    OutboundHeader header = await db.OutboundHeaders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
                    if (header == null)
                    {
                        throw new Exception("Data not found.");
                    }

                    if (!header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Edit data is not allowed.");
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


                    header.ModifiedBy = activeUser;
                    header.ModifiedOn = transactionDate;
                    header.Remarks = dataVM.Remarks;

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Update data succeeded.";

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
            OutboundHeaderDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                OutboundHeader outboundHeader = await db.OutboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (outboundHeader == null || outboundHeader.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new OutboundHeaderDTO
                {
                    ID = outboundHeader.ID,
                    Code = outboundHeader.Code,
                    WarehouseCode = outboundHeader.WarehouseCode,
                    WarehouseName = outboundHeader.WarehouseName,
                    Remarks = outboundHeader.Remarks,
                    TransactionStatus = outboundHeader.TransactionStatus,
                    CreatedBy = outboundHeader.CreatedBy,
                    CreatedOn = outboundHeader.CreatedOn.ToString(),
                    ModifiedBy = outboundHeader.ModifiedBy != null ? outboundHeader.ModifiedBy : "",
                    ModifiedOn = outboundHeader.ModifiedOn.ToString()
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

        [HttpPost]
        public async Task<IHttpActionResult> DatatableProduct(string WarehouseCode)
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

            IEnumerable<vStockProduct> list = Enumerable.Empty<vStockProduct>();
            IEnumerable<OutboundOrderDTO> pagedData = Enumerable.Empty<OutboundOrderDTO>();

            Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(WarehouseCode)).FirstOrDefaultAsync();
            string[] warehouseCodes = { };
            if (!wh.Type.Equals("EMIX"))
            {
                warehouseCodes = new string[1] { WarehouseCode};
            }
            else
            {
                warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
            }

            IQueryable<vStockProduct> query = db.vStockProducts.Where(m => warehouseCodes.Contains(m.WarehouseCode) && m.TotalQty > 0).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vStockProduct, object>> cols = new Dictionary<string, Func<vStockProduct, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("MaterialType", x => x.ProdType);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();


                if (list != null && list.Count() > 0)
                {


                    pagedData = from x in list
                                select new OutboundOrderDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    MaterialType = x.ProdType
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
        public async Task<IHttpActionResult> DatatableOrder(string HeaderID)
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

            IEnumerable<OutboundOrder> list = Enumerable.Empty<OutboundOrder>();
            IEnumerable<OutboundOrderDTO> pagedData = Enumerable.Empty<OutboundOrderDTO>();

            IQueryable<OutboundOrder> query = db.OutboundOrders.Where(s => s.OutboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<OutboundOrder, object>> cols = new Dictionary<string, Func<OutboundOrder, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("PickedQty", x => x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag));
                cols.Add("DiffQty", x => x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - x.TotalQty);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new OutboundOrderDTO
                                {
                                    ID = x.ID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.MaterialType,
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    PickedQty = Helper.FormatThousand(x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag)),
                                    DiffQty = Helper.FormatThousand(x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - x.TotalQty),
                                    OutstandingQty = Helper.FormatThousand(x.TotalQty - (x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag))),
                                    OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((x.TotalQty - (x.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / x.QtyPerBag))),
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    //ReturnAction = x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - x.TotalQty > 0,
                                    ReturnAction = x.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) > 0,

                                    //ReturnAction = true,
                                    WarehouseCode = x.OutboundHeader.WarehouseCode,
                                    WarehouseName = x.OutboundHeader.WarehouseName
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
        public async Task<IHttpActionResult> CreateOrder(OutboundOrderVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

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

                    OutboundHeader header = null;
                    vStockProduct stockProduct = null;
                    if (string.IsNullOrEmpty(dataVM.HeaderID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        header = await db.OutboundHeaders.Where(s => s.ID.Equals(dataVM.HeaderID)).FirstOrDefaultAsync();

                        if (header == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }
                        else
                        {
                            if (!header.TransactionStatus.Equals("OPEN"))
                            {
                                throw new Exception("Edit is not allowed.");
                            }
                        }
                    }



                    if (string.IsNullOrEmpty(dataVM.MaterialCode))
                    {
                        //ModelState.AddModelError("Outbound.MaterialCode", "Material Code is required.");
                        throw new Exception("Material Code is required.");
                    }
                    else
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(header.WarehouseCode)).FirstOrDefaultAsync();
                        string[] warehouseCodes = { };
                        if (!wh.Type.Equals("EMIX"))
                        {
                            warehouseCodes = new string[1] { header.WarehouseCode};
                        }
                        else
                        {
                            warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                        }

                        stockProduct = await db.vStockProducts.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode) && warehouseCodes.Contains(m.WarehouseCode) && m.TotalQty > 0).FirstOrDefaultAsync();
                        if (stockProduct == null)
                        {
                            //ModelState.AddModelError("Outbound.MaterialCode", "Material is not recognized.");
                            throw new Exception("Material is not recognized.");
                        }
                        else
                        {
                            OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.OutboundID.Equals(dataVM.HeaderID) && m.MaterialCode.Equals(dataVM.MaterialCode)).FirstOrDefaultAsync();
                            if (outboundOrder != null)
                            {
                                throw new Exception("Material is already exist.");
                            }
                        }

                    }

                    if (dataVM.TotalQty <= 0)
                    {
                        ModelState.AddModelError("Outbound.TotalQty", "Request Qty is required.");
                    }
                    else
                    {
                        //stock validation, check available qty
                        decimal? availableStock = 0;

                        availableStock = stockProduct.TotalQty;
                        //if (productMaster.ProdType.Equals("RM"))
                        //{
                        //    IEnumerable<StockRM> stock = db.StockRMs.Where(m => m.BinRack.Warehouse.Code.Equals(header.WarehouseCode) && m.MaterialCode.Equals(productMaster.MaterialCode)).ToList();
                        //    if(stock != null && stock.Count() > 0)
                        //    {
                        //        availableStock = stock.Sum(m => m.Quantity);
                        //    }

                        //}else if (productMaster.ProdType.Equals("SFG"))
                        //{
                        //    IEnumerable<StockSFG> stock = db.StockSFGs.Where(m => m.BinRack.Warehouse.Code.Equals(header.WarehouseCode) && m.MaterialCode.Equals(productMaster.MaterialCode)).ToList();
                        //    if (stock != null && stock.Count() > 0)
                        //    {
                        //        availableStock = stock.Sum(m => m.Quantity);
                        //    }
                        //}

                        if (dataVM.TotalQty > availableStock)
                        {
                            ModelState.AddModelError("Outbound.TotalQty", string.Format("Request Qty exceeded. Available Qty : {0}", availableStock));
                        }
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

                    vProductMaster productMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(stockProduct.MaterialCode)).FirstOrDefaultAsync();

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

                    OutboundOrder order = new OutboundOrder()
                    {
                        ID = Helper.CreateGuid("Oo"),
                        OutboundID = dataVM.HeaderID,
                        MaterialCode = stockProduct.MaterialCode,
                        MaterialName = stockProduct.MaterialName,
                        MaterialType = stockProduct.ProdType,
                        TotalQty = dataVM.TotalQty,
                        QtyPerBag = productMaster.QtyPerBag,
                        CreatedBy = activeUser,
                        CreatedOn = transactionDate
                    };


                    header.OutboundOrders.Add(order);



                    await db.SaveChangesAsync();

                    status = true;
                    message = "Add Detail succeeded.";
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

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Print(OutboundReturnPrintReq req)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(req.Printer))
                    {
                        throw new Exception("Printer harus dipilih.");
                    }

                    OutboundReturn outboundReturn = await db.OutboundReturns.Where(s => s.ID.Equals(req.ReturnId)).FirstOrDefaultAsync();

                    if (outboundReturn == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(outboundReturn.OutboundOrder.MaterialCode)).FirstOrDefault();
                    if (material == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string Maker = "";

                    if (material.ProdType.Equals("RM"))
                    {
                        RawMaterial raw = db.RawMaterials.Where(m => m.MaterialCode.Equals(material.MaterialCode)).FirstOrDefault();
                        Maker = raw.Maker;
                    }

                    //create pdf file to specific printer folder for middleware printing

                    decimal totalQty = 0;
                    decimal qtyPerBag = 0;

                    int seq = 0;

                    int len = 7;

                    if (material.MaterialCode.Length > 7)
                    {
                        len = material.MaterialCode.Length;
                    }

                    int fullBag = Convert.ToInt32(outboundReturn.ReturnQty / outboundReturn.QtyPerBag);

                    int lastSeries = outboundReturn.LastSeries;


                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = outboundReturn.OutboundOrder.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(outboundReturn.QtyPerBag).PadLeft(6) + " " + outboundReturn.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);

                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = outboundReturn.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = outboundReturn.ExpDate;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = outboundReturn.OutboundOrder.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = outboundReturn.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = outboundReturn.OutboundOrder.MaterialName;
                        dto.Field9 = Helper.FormatThousand(outboundReturn.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = outboundReturn.OutboundOrder.MaterialCode;
                        dto.Field13 = inDate3;
                        dto.Field14 = expiredDate2;
                        String body = RenderViewToString("Values", "~/Views/Receiving/Label.cshtml", dto);
                        bodies.Add(body);

                        //delete 
                    }


                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (var pdfWriter = new PdfWriter(stream))
                        {

                            PdfDocument pdf = new PdfDocument(pdfWriter);
                            PdfMerger merger = new PdfMerger(pdf);
                            //loop in here, try
                            foreach (string body in bodies)
                            {
                                ByteArrayOutputStream baos = new ByteArrayOutputStream();
                                PdfDocument temp = new PdfDocument(new PdfWriter(baos));
                                Rectangle rectangle = new Rectangle(283.464566928f, 212.598425232f);
                                PageSize pageSize = new PageSize(rectangle);
                                Document document = new Document(temp, pageSize);
                                PdfPage page = temp.AddNewPage(pageSize);
                                HtmlConverter.ConvertToPdf(body, temp, null);
                                temp = new PdfDocument(new PdfReader(new ByteArrayInputStream(baos.ToByteArray())));
                                merger.Merge(temp, 1, temp.GetNumberOfPages());
                                temp.Close();
                            }
                            pdf.Close();

                            byte[] file = stream.ToArray();
                            MemoryStream output = new MemoryStream();
                            output.Write(file, 0, file.Length);
                            output.Position = 0;

                            List<PrinterDTO> printers = new List<PrinterDTO>();

                            PrinterDTO printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_1_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_1_name"].ToString();

                            printers.Add(printer);

                            printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_2_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_2_name"].ToString();

                            printers.Add(printer);

                            printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_3_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_3_name"].ToString();

                            printers.Add(printer);

                            printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_4_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_4_name"].ToString();

                            printers.Add(printer);

                            string folder_name = "";
                            foreach (PrinterDTO printerDTO in printers)
                            {
                                if (printerDTO.PrinterIP.Equals(req.Printer))
                                {
                                    folder_name = printerDTO.PrinterName;
                                }
                            }

                            string file_name = string.Format("{0}.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"));

                            using (Stream fileStream = new FileStream(string.Format(@"C:\RMI_PRINTER_SERVICE\{0}\{1}", folder_name, file_name), FileMode.CreateNew))
                            {
                                output.CopyTo(fileStream);
                            }
                        }
                    }

                    status = true;
                    message = "Print barcode berhasil. Mohon menunggu.";

                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
        public static string RenderViewToString(string controllerName, string viewName, object viewData)
        {
            using (var writer = new StringWriter())
            {
                var routeData = new RouteData();
                routeData.Values.Add("controller", controllerName);
                var fakeControllerContext = new System.Web.Mvc.ControllerContext(new HttpContextWrapper(new HttpContext(new HttpRequest(null, "http://google.com", null), new HttpResponse(null))), routeData, new FakeController());
                var razorViewEngine = new System.Web.Mvc.RazorViewEngine();
                var razorViewResult = razorViewEngine.FindView(fakeControllerContext, viewName, "", false);

                var viewContext = new System.Web.Mvc.ViewContext(fakeControllerContext, razorViewResult.View, new System.Web.Mvc.ViewDataDictionary(viewData), new System.Web.Mvc.TempDataDictionary(), writer);
                razorViewResult.View.Render(viewContext, writer);
                return writer.ToString();
            }
        }
        private string GenerateQRCode(string value)
        {
            string qr_path = MD5Hash(value);
            string folderPath = "/Content/img/qr_codes";
            string imagePath = folderPath + "/" + string.Format("{0}.png", qr_path);
            // If the directory doesn't exist then create it.
            if (!Directory.Exists(HttpContext.Current.Server.MapPath("~" + folderPath)))
            {
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~" + folderPath));
            }

            var barcodeWriter = new BarcodeWriter();
            barcodeWriter.Format = BarcodeFormat.QR_CODE;
            barcodeWriter.Options = new QrCodeEncodingOptions
            {
                Width = 300,
                Height = 300
            };
            var result = barcodeWriter.Write(value);

            string barcodePath = HttpContext.Current.Server.MapPath("~" + imagePath);
            var barcodeBitmap = new Bitmap(result);
            using (MemoryStream memory = new MemoryStream())
            {
                using (FileStream fs = new FileStream(barcodePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    barcodeBitmap.Save(memory, ImageFormat.Png);
                    byte[] bytes = memory.ToArray();
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            return imagePath;
        }
        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        [HttpPost]
        public async Task<IHttpActionResult> RemoveOrder(OutboundOrderVM dataVM)
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
                    if (string.IsNullOrEmpty(dataVM.ID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
                        if (outboundOrder == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }

                        if (!outboundOrder.OutboundHeader.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Edit data is not allowed.");
                        }

                        db.OutboundOrders.Remove(outboundOrder);

                        await db.SaveChangesAsync();

                        status = true;
                        message = "Remove Detail succeeded.";
                    }

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
                    OutboundHeader header = await db.OutboundHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                    if (header.TransactionStatus.Equals("CANCELLED"))
                    {
                        throw new Exception("Can not change transaction status. Transaction is already cancelled.");
                    }

                    if (transactionStatus.Equals("CANCELLED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaction can not be cancelled.");
                    }

                    if (transactionStatus.Equals("CONFIRMED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaction can not be confirmed.");
                    }

                    if (transactionStatus.Equals("CLOSED") && !header.TransactionStatus.Equals("CONFIRMED"))
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
                        db.OutboundOrders.RemoveRange(header.OutboundOrders);

                        message = "Cancel data succeeded.";
                    }

                    if (transactionStatus.Equals("CONFIRMED"))
                    {
                        //check detail
                        if (header.OutboundOrders.Count() < 1)
                        {
                            throw new Exception("Outbound Order can not be empty.");
                        }


                        //automated logic check
                        //warehouse type = outsource will auto generate picking -> auto picked

                        message = "Confirm data succeeded.";
                    }

                    if (transactionStatus.Equals("CLOSED"))
                    {
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

        public async Task<IHttpActionResult> Confirm(string id)
        {
            return await UpdateStatus(id, "CONFIRMED");
        }

        public async Task<IHttpActionResult> Close(string id)
        {
            return await UpdateStatus(id, "CLOSED");
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetFifo()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string OrderId = request["orderId"].ToString();

            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            OutboundOrder order = db.OutboundOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();


            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();

            string warehouseCode = order.OutboundHeader.WarehouseCode;
            Warehouse warehouse = db.Warehouses.Where(x => x.Code.Equals(warehouseCode)).FirstOrDefault();
            if (warehouse.Type.Equals("OUTSOURCE"))
            {
                query = query.Where(d => d.WarehouseCode.Equals(warehouseCode));
            }
            else
            {
                List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
                query = query.Where(a => warehouses.Contains(a.WarehouseCode));
            }

            int totalRow = query.Count();


            decimal requestedQty = order.TotalQty;
            decimal pickedQty = order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag);
            decimal availableQty = requestedQty - pickedQty;

            try
            {
                query = query.OrderByDescending(s => s.BinRackAreaType)
                .ThenByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                .ThenBy(s => s.InDate)
                .ThenBy(s => s.QtyPerBag)
                .ThenBy(s => s.Quantity);
                list = query.ToList();
                //find outstanding quantity
                //do looping until quantity reach
                if (list != null && list.Count() > 0)
                {
                    decimal searchQty = 0;

                    foreach (vStockAll stock in list)
                    {
                        if (searchQty <= availableQty)
                        {
                            //if (DateTime.Now.Date < stock.ExpiredDate.Value.Date)
                            //{
                            //    searchQty += stock.Quantity;
                            //}

                            FifoStockDTO dat = new FifoStockDTO
                            {
                                ID = stock.ID,
                                StockCode = stock.Code,
                                LotNo = stock.LotNumber,
                                BinRackCode = stock.BinRackCode,
                                BinRackName = stock.BinRackName,
                                BinRackAreaCode = stock.BinRackAreaCode,
                                BinRackAreaName = stock.BinRackAreaName,
                                WarehouseCode = stock.WarehouseCode,
                                WarehouseName = stock.WarehouseName,
                                MaterialCode = stock.MaterialCode,
                                MaterialName = stock.MaterialName,
                                InDate = Helper.NullDateToString(stock.InDate),
                                ExpDate = Helper.NullDateToString(stock.ExpiredDate),
                                BagQty = Helper.FormatThousand(Convert.ToInt32(stock.BagQty)),
                                QtyPerBag = Helper.FormatThousand(stock.QtyPerBag),
                                TotalQty = Helper.FormatThousand(stock.Quantity),
                                IsExpired = DateTime.Now.Date >= stock.ExpiredDate.Value.Date,
                                QCInspected = stock.OnInspect
                            };

                            data.Add(dat);
                        }
                        else
                        {
                            break;
                        }

                    }
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

            obj.Add("totalRow", totalRow);
            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Picking(OutboundPickingVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vStockAll stockAll = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }


                    if (!order.OutboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Picking is not allowed.");
                    }


                    if (string.IsNullOrEmpty(dataVM.StockID))
                    {
                        throw new Exception("Stock is required.");
                    }

                    //check stock quantity
                    stockAll = db.vStockAlls.Where(m => m.ID.Equals(dataVM.StockID)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock is not recognized.");
                    }


                    //restriction 1 : AREA TYPE

                    User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
                    string userAreaType = userData.AreaType;

                    string materialAreaType = stockAll.BinRackAreaType;

                    if (!userAreaType.Equals(materialAreaType))
                    {
                        throw new Exception(string.Format("FIFO Restriction, do not allowed to pick material in area {0}", materialAreaType));
                    }

                    string warehouseCode = order.OutboundHeader.WarehouseCode;
                    List<string> warehouses = new List<string>();
                    Warehouse warehouse = db.Warehouses.Where(x => x.Code.Equals(warehouseCode)).FirstOrDefault();

                    if (warehouse.Type.Equals("OUTSOURCE"))
                    {
                        warehouses.Add(warehouseCode);

                    }
                    else
                    {
                        warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();

                    }

                    //restriction 2 : REMAINDER QTY

                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType) && warehouses.Contains(s.WarehouseCode))
                       .OrderByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                       .ThenBy(s => s.InDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();
                    //.ThenBy(s => s.Quantity).FirstOrDefault();


                    if (stkAll == null)
                    {
                        throw new Exception("Stock is not available.");
                    }

                    //if (stockAll.QtyPerBag > stkAll.QtyPerBag)
                    //{
                    //    throw new Exception(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
                    //}

                    //restriction 3 : IN DATE

                    //if (stockAll.InDate.Date > stkAll.InDate.Date)
                    //{
                    //    throw new Exception(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & In Date: {1}", stkAll.LotNumber, Helper.NullDateToString(stkAll.InDate)));
                    //}

                    //restriction 4 : EXPIRED DATE

                    if (DateTime.Now.Date >= stkAll.ExpiredDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, must execute QC Inspection for material with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
                    }

                    if (dataVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Outbound.BagQty", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int bagQty = Convert.ToInt32(stockAll.Quantity / stockAll.QtyPerBag);

                        if (dataVM.BagQty > bagQty)
                        {
                            ModelState.AddModelError("Outbound.BagQty", string.Format("Bag Qty exceeded. Bag Qty : {0}", bagQty));
                        }
                        else
                        {
                            decimal requestedQty = order.TotalQty;
                            decimal pickedQty = order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag);
                            decimal availableQty = requestedQty - pickedQty;
                            int availableBagQty = Convert.ToInt32(Math.Ceiling(availableQty / stockAll.QtyPerBag));

                            if (dataVM.BagQty > availableBagQty)
                            {
                                ModelState.AddModelError("Outbound.BagQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                            }
                        }
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

                    BinRack binRack = db.BinRacks.Where(m => m.Code.Equals(stockAll.BinRackCode)).FirstOrDefault();

                    OutboundPicking picking = new OutboundPicking();
                    picking.ID = Helper.CreateGuid("P");
                    picking.OutboundOrderID = order.ID;
                    picking.PickingMethod = "MANUAL";
                    picking.PickedOn = DateTime.Now;
                    picking.PickedBy = activeUser;
                    picking.BinRackID = binRack.ID;
                    picking.BinRackCode = stockAll.BinRackCode;
                    picking.BinRackName = stockAll.BinRackName;
                    picking.BagQty = dataVM.BagQty;
                    picking.QtyPerBag = stockAll.QtyPerBag;
                    picking.StockCode = stockAll.Code;
                    picking.LotNo = stockAll.LotNumber;
                    picking.InDate = stockAll.InDate.Value;
                    picking.ExpDate = stockAll.ExpiredDate.Value;

                    db.OutboundPickings.Add(picking);

                    //reduce stock

                    if (stockAll.Type.Equals("RM"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }


                    await db.SaveChangesAsync();

                    OutboundOrderDTO orderDTO = new OutboundOrderDTO
                    {
                        ID = order.ID,
                        MaterialCode = order.MaterialCode,
                        MaterialName = order.MaterialName,
                        OutstandingQty = Helper.FormatThousand(order.TotalQty - (order.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag))),
                        OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.TotalQty - (order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag))),

                    };

                    obj.Add("data", orderDTO);

                    status = true;
                    message = "Picking succeeded.";

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


            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> Return(OutboundPutawayReturnReq dataVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            vStockAll stockAll = null;

        //            if (string.IsNullOrEmpty(dataVM.OrderID))
        //            {
        //                throw new Exception("Order Id is required.");
        //            }

        //            OutboundOrder order = db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefault();

        //            if (order == null)
        //            {
        //                throw new Exception("Order is not recognized.");
        //            }
        //            OutboundHeader outboundHeader = db.OutboundHeaders.Where(s => s.ID.Equals(order.OutboundHeader.ID)).FirstOrDefault();
        //            //check stock quantity
        //            stockAll = db.vStockAlls.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefault();
        //            if (stockAll == null)
        //            {
        //                throw new Exception("Stock is not recognized.");
        //            }
        //            vProductMaster productMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

        //            TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
        //            DateTime now = DateTime.Now;
        //            DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

        //            //restriction 1 : AREA TYPE

        //            //User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
        //            //string userAreaType = userData.AreaType;

        //            //string materialAreaType = stockAll.BinRackAreaType;

        //            //if (!userAreaType.Equals(materialAreaType))
        //            //{
        //            //    throw new Exception(string.Format("FIFO Restriction, do not allowed to pick material in area {0}", materialAreaType));
        //            //}

        //            string warehouseCode = order.OutboundHeader.WarehouseCode;
        //            List<string> warehouses = new List<string>();
        //            Warehouse warehouse = db.Warehouses.Where(x => x.Code.Equals(warehouseCode)).FirstOrDefault();

        //            if (warehouse.Type.Equals("OUTSOURCE"))
        //            {
        //                warehouses.Add(warehouseCode);

        //            }
        //            else
        //            {
        //                warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();

        //            }

        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Input is not valid");
        //            }
        //            var binRackCodes = dataVM.BinrackCode.Split('-');
        //            var binRackCode = binRackCodes[0];
        //            BinRack binRack = db.BinRacks.Where(m => m.Code.Equals(binRackCode)).FirstOrDefault();
        //            OutboundReturn ret = new OutboundReturn();
        //            ret.ID = Helper.CreateGuid("R");
        //            ret.OutboundOrderID = dataVM.OrderID;
        //            ret.ReturnMethod = "MANUAL";
        //            ret.ReturnedOn = DateTime.Now;
        //            ret.ReturnedBy = activeUser;
        //            ret.ReturnQty = dataVM.Qty;
        //            ret.QtyPerBag = dataVM.Qty;
        //            //create new stock code
        //            ret.StockCode = string.Format("{0}{1}{2}{3}{4}", stockAll.MaterialCode, Helper.FormatThousand(dataVM.Qty), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
        //            ret.LotNo = stockAll.LotNumber;
        //            ret.InDate = stockAll.InDate.Value;
        //            ret.ExpDate = stockAll.ExpiredDate.Value;
        //            ret.Remarks = dataVM.Remarks;

        //            //log print RM
        //            //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
        //            int startSeries = 0;
        //            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(ret.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
        //            if (lastSeries == 0)
        //            {
        //                startSeries = 1;
        //            }
        //            else
        //            {
        //                startSeries = lastSeries + 1;
        //            }

        //            lastSeries = startSeries + (Convert.ToInt32(dataVM.Qty / dataVM.Qty));

        //            ret.LastSeries = lastSeries;

        //            db.OutboundReturns.Add(ret);

        //            //reduce stock

        //            if (stockAll.Type.Equals("RM"))
        //            {
        //                decimal pickQty = dataVM.Qty;
        //                StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
        //                stock.Quantity -= pickQty;
        //            }
        //            else if (stockAll.Type.Equals("SFG"))
        //            {
        //                decimal pickQty = dataVM.Qty;
        //                StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
        //                stock.Quantity -= pickQty;
        //            }


        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Return succeeded.";

        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
        //    }
        //    catch (HttpRequestException reqpEx)
        //    {
        //        message = reqpEx.Message;
        //    }
        //    catch (HttpResponseException respEx)
        //    {
        //        message = respEx.Message;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }


        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        [HttpGet]
        public async Task<IHttpActionResult> GetPickingDetail(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            OutboundPickingDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                vOutboundPickingSummary outboundPicking = await db.vOutboundPickingSummaries.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (outboundPicking == null)
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new OutboundPickingDTO
                {
                    OrderID = outboundPicking.ID,
                    StockCode = outboundPicking.StockCode,
                    MaterialCode = outboundPicking.MaterialCode,
                    MaterialName = outboundPicking.MaterialName,
                    QtyPerBag = Helper.FormatThousand(outboundPicking.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(outboundPicking.TotalQty / outboundPicking.QtyPerBag)),
                    LotNo = outboundPicking.LotNo,
                    InDate = Helper.NullDateToString(outboundPicking.InDate),
                    ExpDate = Helper.NullDateToString(outboundPicking.ExpDate),
                    TotalQty = Helper.FormatThousand(outboundPicking.TotalQty),
                    ReturnedTotalQty = Helper.FormatThousand(outboundPicking.ReturnQty),
                    AvailableReturnQty = Helper.FormatThousand(outboundPicking.TotalQty - outboundPicking.ReturnQty)
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


        [HttpPost]
        public async Task<IHttpActionResult> Return(OutboundReturnVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vOutboundPickingSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    summary = await db.vOutboundPickingSummaries.Where(s => s.ID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Item is not recognized.");
                    }

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!order.OutboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Return not allowed.");
                    }


                    if (dataVM.Qty <= 0)
                    {
                        ModelState.AddModelError("Outbound.ReturnQty", "Return Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.ReturnQty.Value;

                        if (dataVM.Qty > availableQty)
                        {
                            ModelState.AddModelError("Outbound.ReturnQty", string.Format("Return Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableQty)));
                        }
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

                    //fullbag
                    decimal RemainderQty = dataVM.Qty / summary.QtyPerBag;
                    RemainderQty = dataVM.Qty - (Math.Floor(RemainderQty) * summary.QtyPerBag);
                    int BagQty = Convert.ToInt32((dataVM.Qty - RemainderQty) / summary.QtyPerBag);
                    decimal totalQty = BagQty * summary.QtyPerBag;

                    int lastSeries = 0;
                    int startSeries = 0;

                    OutboundReturn ret = new OutboundReturn();

                    if (BagQty > 0)
                    {
                        lastSeries = await db.OutboundReturns.Where(m => m.StockCode.Equals(summary.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + BagQty - 1;

                        ret.ID = Helper.CreateGuid("R");
                        ret.OutboundOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = totalQty;
                        ret.StockCode = summary.StockCode;
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.QtyPerBag = summary.QtyPerBag;
                        ret.PrevStockCode = summary.StockCode;
                        ret.LastSeries = lastSeries;

                        db.OutboundReturns.Add(ret);
                    }

                    if (RemainderQty > 0)
                    {
                        int lastSeries1 = 0;
                        int startSeries1 = 0;

                        lastSeries1 = await db.OutboundReturns.Where(m => m.StockCode.Equals(summary.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries1 == 0)
                        {
                            startSeries1 = 1;
                        }
                        else
                        {
                            startSeries1 = lastSeries1 + 1;
                        }
                        lastSeries1 = startSeries1;

                        ret = new OutboundReturn();
                        ret.ID = Helper.CreateGuid("R");
                        ret.OutboundOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = RemainderQty;
                        ret.QtyPerBag = RemainderQty;
                        ret.StockCode = string.Format("{0}{1}{2}{3}{4}", summary.MaterialCode, Helper.FormatThousand(RemainderQty), summary.LotNo, summary.InDate.ToString("yyyyMMdd").Substring(1), summary.ExpDate.ToString("yyyyMMdd").Substring(2));
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.PrevStockCode = summary.StockCode;
                        ret.LastSeries = lastSeries1;

                        db.OutboundReturns.Add(ret);
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Return succeeded.";

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

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetReturnDetail(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            OutboundReturnDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                vOutboundReturnSummary outboundReturn = await db.vOutboundReturnSummaries.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (outboundReturn == null)
                {
                    throw new Exception("Data not found.");
                }

                OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.ID.Equals(outboundReturn.ID)).FirstOrDefaultAsync();

                dataDTO = new OutboundReturnDTO
                {
                    OrderID = outboundReturn.ID,
                    StockCode = outboundReturn.StockCode,
                    MaterialCode = outboundReturn.MaterialCode,
                    MaterialName = outboundReturn.MaterialName,
                    WarehouseCode = outboundOrder.OutboundHeader.WarehouseCode,
                    WarehouseName = outboundOrder.OutboundHeader.WarehouseName,
                    QtyPerBag = Helper.FormatThousand(outboundReturn.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(outboundReturn.TotalQty / outboundReturn.QtyPerBag)),
                    LotNo = outboundReturn.LotNo,
                    InDate = Helper.NullDateToString(outboundReturn.InDate),
                    ExpDate = Helper.NullDateToString(outboundReturn.ExpDate),
                    TotalQty = Helper.FormatThousand(outboundReturn.TotalQty),
                    TotalPutawayQty = Helper.FormatThousand(outboundReturn.PutawayQty)
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

        [HttpPost]
        public async Task<IHttpActionResult> Putaway(OutboundPutawayReturnReq dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vOutboundReturnSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    summary = await db.vOutboundReturnSummaries.Where(s => s.ID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Item is not recognized.");
                    }

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();
                    OutboundReturn outboundreturn = await db.OutboundReturns.Where(s => s.OutboundOrderID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!order.OutboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Return not allowed.");
                    }


                    if (dataVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Outbound.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.PutawayQty.Value;
                        int availableBagQty = Convert.ToInt32(availableQty / summary.QtyPerBag);

                        if (dataVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Outbound.PutawayQTY", string.Format("Bag Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableBagQty)));
                        }
                    }

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(dataVM.BinRackID))
                    {
                        ModelState.AddModelError("Outbound.BinRackID", "BinRack is required.");
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.ID.Equals(dataVM.BinRackID)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Outbound.BinRackID", "BinRack is not recognized.");
                        }
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

                    OutboundPutaway putaway = new OutboundPutaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.OutboundOrderID = order.ID;
                    putaway.PutawayMethod = "MANUAL";
                    putaway.LotNo = summary.LotNo;
                    putaway.InDate = summary.InDate;
                    putaway.ExpDate = summary.ExpDate;
                    putaway.QtyPerBag = summary.QtyPerBag;
                    putaway.StockCode = summary.StockCode;
                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    putaway.PutOn = transactionDate;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = dataVM.BagQty * summary.QtyPerBag;

                    db.OutboundPutaways.Add(putaway);

                    outboundreturn.Remarks = Convert.ToString(outboundreturn.LastSeries);

                    if (order.MaterialType.Equals("RM"))
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = order.MaterialCode;
                            stockRM.MaterialName = order.MaterialName;
                            stockRM.Code = summary.StockCode;
                            stockRM.LotNumber = summary.LotNo;
                            stockRM.InDate = summary.InDate;
                            stockRM.ExpiredDate = summary.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = summary.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);
                        }

                    }
                    else
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same
                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = order.MaterialCode;
                            stockSFG.MaterialName = order.MaterialName;
                            stockSFG.Code = summary.StockCode;
                            stockSFG.LotNumber = summary.LotNo;
                            stockSFG.InDate = summary.InDate;
                            stockSFG.ExpiredDate = summary.ExpDate;
                            stockSFG.Quantity = putaway.PutawayQty;
                            stockSFG.QtyPerBag = summary.QtyPerBag;
                            stockSFG.BinRackID = putaway.BinRackID;
                            stockSFG.BinRackCode = putaway.BinRackCode;
                            stockSFG.BinRackName = putaway.BinRackName;
                            stockSFG.ReceivedAt = putaway.PutOn;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }

                   

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway succeeded.";

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

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> Putaway(OutboundPutawayReturnReq dataVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            vStockAll stockAll = null;

        //            if (string.IsNullOrEmpty(dataVM.OrderID))
        //            {
        //                throw new Exception("Order Id is required.");
        //            }

        //            OutboundOrder order = db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefault();

        //            if (order == null)
        //            {
        //                throw new Exception("Order is not recognized.");
        //            }
        //            OutboundHeader outboundHeader = db.OutboundHeaders.Where(s => s.ID.Equals(order.OutboundHeader.ID)).FirstOrDefault();
        //            //check stock quantity
        //            stockAll = db.vStockAlls.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefault();
        //            if (stockAll == null)
        //            {
        //                throw new Exception("Stock is not recognized.");
        //            }
        //            vProductMaster productMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

        //            TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
        //            DateTime now = DateTime.Now;
        //            DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

        //            //restriction 1 : AREA TYPE

        //            User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
        //            string userAreaType = userData.AreaType;

        //            string materialAreaType = stockAll.BinRackAreaType;

        //            if (!userAreaType.Equals(materialAreaType))
        //            {
        //                throw new Exception(string.Format("FIFO Restriction, do not allowed to pick material in area {0}", materialAreaType));
        //            }

        //            string warehouseCode = order.OutboundHeader.WarehouseCode;
        //            List<string> warehouses = new List<string>();
        //            Warehouse warehouse = db.Warehouses.Where(x => x.Code.Equals(warehouseCode)).FirstOrDefault();

        //            if (warehouse.Type.Equals("OUTSOURCE"))
        //            {
        //                warehouses.Add(warehouseCode);

        //            }
        //            else
        //            {
        //                warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();

        //            }

        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Input is not valid");
        //            }
        //            var binRackCodes = dataVM.BinrackCode.Split('-');
        //            var binRackCode = binRackCodes[0];
        //            BinRack binRack = db.BinRacks.Where(m => m.Code.Equals(binRackCode)).FirstOrDefault();
        //            OutboundPutaway ret = new OutboundPutaway();
        //            ret.ID = Helper.CreateGuid("R");
        //            ret.OutboundOrderID = dataVM.OrderID;
        //            ret.PutawayMethod = "MANUAL";
        //            ret.PutOn = DateTime.Now;
        //            ret.PutBy = activeUser;
        //            ret.PutawayQty = dataVM.Qty;
        //            ret.QtyPerBag = dataVM.Qty;
        //            //create new stock code
        //            ret.StockCode = string.Format("{0}{1}{2}{3}{4}", stockAll.MaterialCode, Helper.FormatThousand(dataVM.Qty), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
        //            ret.LotNo = stockAll.LotNumber;
        //            ret.InDate = stockAll.InDate.Value;
        //            ret.ExpDate = stockAll.ExpiredDate.Value;
        //            ret.BinRackCode = binRack.Code;
        //            ret.BinRackID = binRack.ID;
        //            ret.BinRackName = binRack.Name;
        //            db.OutboundPutaways.Add(ret);

        //            //reduce stock

        //            //if (stockAll.Type.Equals("RM"))
        //            //{
        //            //    decimal pickQty = dataVM.Qty;
        //            //    StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
        //            //    stock.Quantity -= pickQty;
        //            //}
        //            //else if (stockAll.Type.Equals("SFG"))
        //            //{
        //            //    decimal pickQty = dataVM.Qty;
        //            //    StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
        //            //    stock.Quantity -= pickQty;
        //            //}


        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Putaway succeeded.";

        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
        //    }
        //    catch (HttpRequestException reqpEx)
        //    {
        //        message = reqpEx.Message;
        //    }
        //    catch (HttpResponseException respEx)
        //    {
        //        message = respEx.Message;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }


        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePicking(string HeaderID)
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

            IEnumerable<OutboundPicking> list = Enumerable.Empty<OutboundPicking>();
            IEnumerable<OutboundPickingDTO> pagedData = Enumerable.Empty<OutboundPickingDTO>();

            IQueryable<OutboundPicking> query = db.OutboundPickings.Where(s => s.OutboundOrder.OutboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.OutboundOrder.MaterialCode.Contains(search)
                        || m.OutboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<OutboundPicking, object>> cols = new Dictionary<string, Func<OutboundPicking, object>>();
                cols.Add("MaterialCode", x => x.OutboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.OutboundOrder.MaterialName);
                cols.Add("StockCode", x => x.StockCode);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("BagQty", x => x.BagQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("PickingMethod", x => x.PickingMethod);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("PickedOn", x => x.PickedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new OutboundPickingDTO
                                {
                                    ID = x.ID,
                                    MaterialCode = x.OutboundOrder.MaterialCode,
                                    MaterialName = x.OutboundOrder.MaterialName,
                                    StockCode = x.StockCode,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    BagQty = Helper.FormatThousand(x.BagQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                    PickingMethod = x.PickingMethod,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    PickedBy = x.PickedBy,
                                    PickedOn = Helper.NullDateTimeToString(x.PickedOn)
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
        public async Task<IHttpActionResult> DatatableReturn(string HeaderID)
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

            IEnumerable<OutboundReturn> list = Enumerable.Empty<OutboundReturn>();
            IEnumerable<OutboundReturnDTO> pagedData = Enumerable.Empty<OutboundReturnDTO>();

            IQueryable<OutboundReturn> query = db.OutboundReturns.Where(s => s.OutboundOrder.OutboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.OutboundOrder.MaterialCode.Contains(search)
                        || m.OutboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<OutboundReturn, object>> cols = new Dictionary<string, Func<OutboundReturn, object>>();
                cols.Add("MaterialCode", x => x.OutboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.OutboundOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("ReturnQty", x => x.ReturnQty);
                cols.Add("ReturnMethod", x => x.ReturnMethod);
                cols.Add("ReturnedBy", x => x.ReturnedBy);
                cols.Add("ReturnedOn", x => x.ReturnedOn);



                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new OutboundReturnDTO
                                {
                                    ID = x.ID,
                                    OrderID = x.OutboundOrderID,
                                    MaterialCode = x.OutboundOrder.MaterialCode,
                                    MaterialName = x.OutboundOrder.MaterialName,
                                    StockCode = x.StockCode,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    ReturnQty = Helper.FormatThousand(x.ReturnQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    ReturnMethod = x.ReturnMethod,
                                    ReturnedBy = x.ReturnedBy,
                                    WarehouseCode = x.OutboundOrder.OutboundHeader.WarehouseCode,
                                    WarehouseName = x.OutboundOrder.OutboundHeader.WarehouseName,
                                    ReturnedOn = Helper.NullDateTimeToString(x.ReturnedOn),
                                    PutawayReturnAction = x.ReturnQty > x.OutboundOrder.OutboundPutaways.Sum(d => d.PutawayQty),
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
        public async Task<IHttpActionResult> DatatablePutawayReturn(string HeaderID)
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

            IEnumerable<OutboundPutaway> list = Enumerable.Empty<OutboundPutaway>();
            IEnumerable<OutboundReturnPutawayDTO> pagedData = Enumerable.Empty<OutboundReturnPutawayDTO>();

            IQueryable<OutboundPutaway> query = db.OutboundPutaways.Where(s => s.OutboundOrder.OutboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.OutboundOrder.MaterialCode.Contains(search)
                        || m.OutboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<OutboundPutaway, object>> cols = new Dictionary<string, Func<OutboundPutaway, object>>();
                cols.Add("MaterialCode", x => x.OutboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.OutboundOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("PutawayQty", x => x.PutawayQty);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("PutawayMethod", x => x.PutawayMethod);
                cols.Add("PutBy", x => x.PutBy);
                cols.Add("PutOn", x => x.PutOn);



                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new OutboundReturnPutawayDTO
                                {
                                    ID = x.ID,
                                    MaterialCode = x.OutboundOrder.MaterialCode,
                                    MaterialName = x.OutboundOrder.MaterialName,
                                    StockCode = x.StockCode,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    PutawayQty = Helper.FormatThousand(x.PutawayQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    PutawayMethod = x.PutawayMethod,
                                    PutBy = x.PutBy,
                                    PutOn = Helper.NullDateTimeToString(x.PutOn),
                                    BinRackName = x.BinRackName,
                                    BinRackCode = x.BinRackCode
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
        //inboundreceiveId
        public async Task<IHttpActionResult> GetListStockCode(string inboundDetailId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<ReceivingDetailBarcodeDTO> list = new List<ReceivingDetailBarcodeDTO>();
            try
            {
                if (string.IsNullOrEmpty(inboundDetailId))
                {
                    throw new Exception("Id is required.");
                }

                List<OutboundReturn> receivingDetail = await db.OutboundReturns.Where(m => m.OutboundOrderID.Equals(inboundDetailId)).ToListAsync();

                if (receivingDetail == null)
                {
                    throw new Exception("Receiving not recognized.");
                }

                IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
                data = from dat in receivingDetail.OrderBy(m => m.ID)
                       select new ReceivingDetailBarcodeDTO
                       {
                           ID = dat.ID,
                           Type = "Inspection",
                           BagQty = Helper.FormatThousand(Convert.ToInt32(dat.ReturnQty / dat.QtyPerBag)),
                           QtyPerBag = Helper.FormatThousand(dat.QtyPerBag),
                           TotalQty = Helper.FormatThousand(dat.ReturnQty),
                           Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.ReturnQty / dat.QtyPerBag) + 1, dat.LastSeries)
                       };

                list.AddRange(data.ToList());

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

            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> DatatableDetailOtherOutbound()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            HttpRequest request = HttpContext.Current.Request;
            string date = request["date"].ToString();
            string enddate = request["enddate"].ToString();
            string warehouseCode = request["warehouseCode"].ToString();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<vOutbound> list = Enumerable.Empty<vOutbound>();
            IEnumerable<OutbounReportDTO> pagedData = Enumerable.Empty<OutbounReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vOutbound> query;

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                query = db.vOutbounds.Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouseCode));
            }
            else
            {
                query = db.vOutbounds.Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.RMCode.Contains(search)
                        || m.RMName.Contains(search)
                        );

                Dictionary<string, Func<vOutbound, object>> cols = new Dictionary<string, Func<vOutbound, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Bag", x => x.Bag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("CreateBy", x => x.CreateBy);
                cols.Add("CreateOn", x => x.CreateOn);
                cols.Add("PickingBag", x => x.PickingBag);
                cols.Add("PickingFullBag", x => x.PickingFullBag);
                cols.Add("PickingTotal", x => x.Total);
                cols.Add("PickingBinRack", x => x.PickingBinRack);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("PutawayBag", x => x.PutawayBag);
                cols.Add("PutawayFullBag", x => x.PutawayFullBag);
                cols.Add("PutawayTotal", x => x.Total);
                cols.Add("PutawayBinRack", x => x.PutawayBinRack);
                cols.Add("PutawayBy", x => x.PutawayBy);
                cols.Add("PutawayOn", x => x.PutawayOn);
                cols.Add("Status", x => x.Status);
                cols.Add("Memo", x => x.Memo);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new OutbounReportDTO
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    Bag = Helper.FormatThousand(detail.Bag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(detail.Total),
                                    CreateBy = detail.CreateBy,
                                    CreateOn = detail.CreateOn,
                                    PickingBag = Helper.FormatThousand(detail.PickingBag),
                                    PickingFullBag = Helper.FormatThousand(detail.PickingFullBag),
                                    PickingTotal = Helper.FormatThousand(detail.Total),
                                    PickingBinRack = detail.PickingBinRack,
                                    PickingBy = detail.PickingBy,
                                    PickingOn = detail.PickingOn,
                                    PutawayBag = Helper.FormatThousand(detail.PutawayBag),
                                    PutawayFullBag = Helper.FormatThousand(detail.PutawayFullBag),
                                    PutawayTotal = Helper.FormatThousand(detail.Total),
                                    PutawayBinRack = detail.PutawayBinRack,
                                    PutawayBy = detail.PutawayBy,
                                    PutawayOn = Convert.ToDateTime(detail.PutawayOn),
                                    Status = detail.Status,
                                    Memo = detail.Memo,
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
        public async Task<IHttpActionResult> GetDataReportOtherOutbound(string date, string enddate, string warehouse)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate) && string.IsNullOrEmpty(warehouse))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vOutbound> list = Enumerable.Empty<vOutbound>();
            IEnumerable<OutbounReportDTO> pagedData = Enumerable.Empty<OutbounReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vOutbound> query;

            if (!string.IsNullOrEmpty(warehouse))
            {
                query = db.vOutbounds.Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouse));
            }
            else
            {
                query = db.vOutbounds.Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {       
                Dictionary<string, Func<vOutbound, object>> cols = new Dictionary<string, Func<vOutbound, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Bag", x => x.Bag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("CreateBy", x => x.CreateBy);
                cols.Add("CreateOn", x => x.CreateOn);
                cols.Add("PickingBag", x => x.PickingBag);
                cols.Add("PickingFullBag", x => x.PickingFullBag);
                cols.Add("PickingTotal", x => x.Total);
                cols.Add("PickingBinRack", x => x.PickingBinRack);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("PutawayBag", x => x.PutawayBag);
                cols.Add("PutawayFullBag", x => x.PutawayFullBag);
                cols.Add("PutawayTotal", x => x.Total);
                cols.Add("PutawayBinRack", x => x.PutawayBinRack);
                cols.Add("PutawayBy", x => x.PutawayBy);
                cols.Add("PutawayOn", x => x.PutawayOn);
                cols.Add("Status", x => x.Status);
                cols.Add("Memo", x => x.Memo);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new OutbounReportDTO
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    Bag = Helper.FormatThousand(detail.Bag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(detail.Total),
                                    CreateBy = detail.CreateBy,
                                    CreateOn = detail.CreateOn,
                                    PickingBag = Helper.FormatThousand(detail.PickingBag),
                                    PickingFullBag = Helper.FormatThousand(detail.PickingFullBag),
                                    PickingTotal = Helper.FormatThousand(detail.Total),
                                    PickingBinRack = detail.PickingBinRack,
                                    PickingBy = detail.PickingBy,
                                    PickingOn = detail.PickingOn,
                                    PutawayBag = Helper.FormatThousand(detail.PutawayBag),
                                    PutawayFullBag = Helper.FormatThousand(detail.PutawayFullBag),
                                    PutawayTotal = Helper.FormatThousand(detail.Total),
                                    PutawayBinRack = detail.PutawayBinRack,
                                    PutawayBy = detail.PutawayBy,
                                    PutawayOn = Convert.ToDateTime(detail.PutawayOn),
                                    Status = detail.Status,
                                    Memo = detail.Memo,
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

            obj.Add("list", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
