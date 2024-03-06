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
using OfficeOpenXml;
using OfficeOpenXml.Style;
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
    public class InboundController : ApiController
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

            IEnumerable<InboundHeader> list = Enumerable.Empty<InboundHeader>();
            IEnumerable<InboundHeaderDTO> pagedData = Enumerable.Empty<InboundHeaderDTO>();

            IQueryable<InboundHeader> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.InboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.InboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else if (transactionStatus.Equals("OPEN/CONFIRMED"))
            {
                query = db.InboundHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("CONFIRMED")).AsQueryable();
            }
            else
            {
                query = db.InboundHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
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

                Dictionary<string, Func<InboundHeader, object>> cols = new Dictionary<string, Func<InboundHeader, object>>();
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
                                select new InboundHeaderDTO
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
        public async Task<IHttpActionResult> Create(InboundHeaderVM dataVM)
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
                        ModelState.AddModelError("Inbound.WarehouseCode", "Warehouse is required.");
                    }
                    else
                    {
                        var temp = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();

                        if (temp == null)
                        {
                            ModelState.AddModelError("Inbound.WarehouseCode", "Warehouse is not recognized.");
                        }
                    }

                    //if (string.IsNullOrEmpty(dataVM.Remarks))
                    //{
                    //    ModelState.AddModelError("Inbound.Remarks", "Remarks is required.");
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
                    var TransactionId = Helper.CreateGuid("IN");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.InboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
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

                    InboundHeader header = new InboundHeader
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

                    db.InboundHeaders.Add(header);

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
        public async Task<IHttpActionResult> Update(InboundHeaderVM dataVM)
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
                        throw new Exception("Inbound ID is required.");
                    }

                    InboundHeader header = await db.InboundHeaders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
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
                        ModelState.AddModelError("Inbound.Remarks", "Remarks is required.");
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
            InboundHeaderDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                InboundHeader InboundHeader = await db.InboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (InboundHeader == null || InboundHeader.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new InboundHeaderDTO
                {
                    ID = InboundHeader.ID,
                    Code = InboundHeader.Code,
                    WarehouseCode = InboundHeader.WarehouseCode,
                    WarehouseName = InboundHeader.WarehouseName,
                    Remarks = InboundHeader.Remarks,
                    TransactionStatus = InboundHeader.TransactionStatus,
                    CreatedBy = InboundHeader.CreatedBy,
                    CreatedOn = InboundHeader.CreatedOn.ToString(),
                    ModifiedBy = InboundHeader.ModifiedBy != null ? InboundHeader.ModifiedBy : "",
                    ModifiedOn = InboundHeader.ModifiedOn.ToString()
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
        public async Task<IHttpActionResult> DatatableProduct()
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

            IEnumerable<vProductMaster> list = Enumerable.Empty<vProductMaster>();
            IEnumerable<InboundMaterialDTO> pagedData = Enumerable.Empty<InboundMaterialDTO>();

            IQueryable<vProductMaster> query = db.vProductMasters.Where(m => m.ProdType.Equals("RM")).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vProductMaster, object>> cols = new Dictionary<string, Func<vProductMaster, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
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
                                select new InboundMaterialDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
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

            IEnumerable<InboundOrder> list = Enumerable.Empty<InboundOrder>();
            IEnumerable<InboundOrderDTO> pagedData = Enumerable.Empty<InboundOrderDTO>();

            IQueryable<InboundOrder> query = db.InboundOrders.Where(s => s.InboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<InboundOrder, object>> cols = new Dictionary<string, Func<InboundOrder, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("FullBag", x => Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)));
                cols.Add("Remainder", x => x.Qty - (Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)) * x.QtyPerBag));
                cols.Add("OutstandingQty", x => x.Qty - (x.InboundReceives.Sum(m => m.Qty)));
                //cols.Add("DiffQty", x => x.InboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - x.TotalQty);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("OutstandingBagQty", x => Convert.ToInt32(Math.Ceiling((x.Qty - (x.InboundReceives.Sum(i => i.Qty * i.QtyPerBag))) / x.QtyPerBag)));
                cols.Add("OutstandingRemainderQty", x => Convert.ToInt32(Math.Ceiling((x.Qty - (x.InboundReceives.Sum(i => i.Qty * i.QtyPerBag))) / x.QtyPerBag)));


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new InboundOrderDTO
                                {
                                    ID = x.ID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.MaterialType,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    FullBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag))),
                                    RemainderQty = Helper.FormatThousand(x.Qty - (Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)) * x.QtyPerBag)),
                                    //PickedQty = Helper.FormatThousand(x.InboundPickings.Sum(m => m.BagQty * m.QtyPerBag)),
                                    //DiffQty = Helper.FormatThousand(x.InboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - x.TotalQty),
                                    OutstandingQty = Helper.FormatThousand(x.Qty - (x.InboundReceives.Sum(m => m.Qty))),
                                    OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((x.Qty - (x.InboundReceives.Sum(i => i.Qty * i.QtyPerBag))) / x.QtyPerBag))),
                                    OutstandingRemainderQty = Helper.FormatThousand((x.Qty - (Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)) * x.QtyPerBag)) - (Math.Ceiling(x.InboundReceives.Sum(i => i.Qty * i.QtyPerBag)) / x.QtyPerBag)),
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ReceiveAction = x.InboundHeader.TransactionStatus.Equals("CONFIRMED") && (x.InboundReceives.Sum(m => m.Qty) - x.Qty) != 0
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
        public async Task<IHttpActionResult> CreateOrder(InboundOrderVM dataVM)
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

                    InboundHeader header = null;
                    vStockProduct stockProduct = null;
                    if (string.IsNullOrEmpty(dataVM.HeaderID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        header = await db.InboundHeaders.Where(s => s.ID.Equals(dataVM.HeaderID)).FirstOrDefaultAsync();

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
                        //ModelState.AddModelError("Inbound.MaterialCode", "Material Code is required.");
                        throw new Exception("Material Code is required.");
                    }
                    else
                    {
                        stockProduct = await db.vStockProducts.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode)).FirstOrDefaultAsync();
                        if (stockProduct == null)
                        {
                            //ModelState.AddModelError("Inbound.MaterialCode", "Material is not recognized.");
                            throw new Exception("Material is not recognized.");
                        }
                        else
                        {
                            InboundOrder InboundOrder = await db.InboundOrders.Where(m => m.InboundID.Equals(dataVM.HeaderID) && m.MaterialCode.Equals(dataVM.MaterialCode)).FirstOrDefaultAsync();
                            if (InboundOrder != null)
                            {
                                throw new Exception("Material is already exist.");
                            }
                        }

                    }

                    if (dataVM.Qty <= 0)
                    {
                        ModelState.AddModelError("Inbound.Qty", "Receive Qty is required.");
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

                    InboundOrder order = new InboundOrder()
                    {
                        ID = Helper.CreateGuid("Io"),
                        InboundID = dataVM.HeaderID,
                        MaterialCode = stockProduct.MaterialCode,
                        MaterialName = stockProduct.MaterialName,
                        MaterialType = stockProduct.ProdType,
                        Qty = dataVM.Qty,
                        QtyPerBag = productMaster.QtyPerBag,
                        CreatedBy = activeUser,
                        CreatedOn = transactionDate
                    };


                    header.InboundOrders.Add(order);



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
        public async Task<IHttpActionResult> RemoveOrder(InboundOrderVM dataVM)
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
                        InboundOrder InboundOrder = await db.InboundOrders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
                        if (InboundOrder == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }

                        if (!InboundOrder.InboundHeader.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Edit data is not allowed.");
                        }

                        db.InboundOrders.Remove(InboundOrder);

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
                    InboundHeader header = await db.InboundHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

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
                        db.InboundOrders.RemoveRange(header.InboundOrders);

                        message = "Cancel data succeeded.";
                    }

                    if (transactionStatus.Equals("CONFIRMED"))
                    {
                        //check detail
                        if (header.InboundOrders.Count() < 1)
                        {
                            throw new Exception("Inbound Order can not be empty.");
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
        [HttpPost]
        public async Task<IHttpActionResult> Receive(InboundReceiveWebReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

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

                    if (string.IsNullOrEmpty(req.OrderId))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    InboundOrder order = await db.InboundOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }


                    string StockCode = "";

                    vStockAll stock = null;

                    int lastSeries = 0;
                    int startSeries = 0;

                    if (req.ScanBarcode)
                    {
                        if (string.IsNullOrEmpty(req.LotNumber))
                        {
                            ModelState.AddModelError("req.LotNumber", "Lot Number is required. ");
                        }
                        if (req.InDate == DateTime.MinValue)
                        {
                            ModelState.AddModelError("req.InDate", "In Date is required. ");
                        }
                        if (req.ExpDate == DateTime.MinValue)
                        {
                            ModelState.AddModelError("req.ExpDate", "Expired Date is required. ");
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
                        if (req.BagQty <= 0)
                        {
                            ModelState.AddModelError("req.bagQty", "Bag Qty can not be empty or below zero.");
                        }
                        else
                        {
                            decimal TotalQty = req.BagQty * vProductMaster.QtyPerBag;
                            decimal receivedQty = order.InboundReceives.Sum(s => s.Qty);
                            decimal allowedQty = order.Qty - receivedQty;

                            if (TotalQty > allowedQty)
                            {
                                ModelState.AddModelError("req.bagQty", string.Format("Total Qty exceeded. Available Qty : {0}", Helper.FormatThousand(allowedQty)));
                            }
                        }

                        string MaterialCode = vProductMaster.MaterialCode;
                        string QtyPerBag = req.QtyPerBag.ToString().Replace(',', '.');
                        string LotNumber = string.Empty;

                        string ExpiredDate = string.Empty;
                        if (order.InboundHeader.WarehouseCode != "2003" || order.InboundHeader.WarehouseCode != "2004")
                        {
                            LotNumber = req.LotNumber;

                            ExpiredDate = Convert.ToDateTime(req.ExpDate).ToString("yyyyMMdd").Substring(2);
                        }

                        string InDate = Convert.ToDateTime(req.InDate).ToString("yyyyMMdd").Substring(1);


                        StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                        stock = db.vStockAlls.Where(m => m.Code.Equals(StockCode)).FirstOrDefault();
                        if (stock == null)
                        {
                            throw new Exception("Stock tidak ditemukan.");
                        }

                        req.RemainderQty = 0;
                    }
                    else
                    {

                        decimal TotalQty = (req.BagQty * vProductMaster.QtyPerBag) + req.RemainderQty;

                        req.BagQty = Convert.ToInt32(Math.Floor(TotalQty / vProductMaster.QtyPerBag));
                        req.RemainderQty = TotalQty % vProductMaster.QtyPerBag;


                        if (TotalQty <= 0)
                        {
                            ModelState.AddModelError("req.bagQty", "Bag Qty can not be empty or below zero.");
                        }
                        else
                        {
                            decimal receivedQty = order.InboundReceives.Sum(s => s.Qty);
                            decimal allowedQty = order.Qty - receivedQty;

                            if (TotalQty > allowedQty)
                            {
                                ModelState.AddModelError("req.bagQty", string.Format("Total Qty exceeded. Available Qty : {0}", Helper.FormatThousand(allowedQty)));
                            }
                        }

                        //fullbag
                        int totalFullBag = Convert.ToInt32(req.BagQty);
                        decimal totalQty = totalFullBag * vProductMaster.QtyPerBag;

                        //logic jika tidak ada stock ambil data dari receiving terakhir, jika ada stock ambil stock paling awal

                        stock = db.vStockAlls.Where(s => s.MaterialCode.Equals(vProductMaster.MaterialCode) && s.Quantity > 0)
                       .OrderBy(s => s.InDate)
                       .ThenBy(s => s.ExpiredDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();

                        if (stock == null)
                        {
                            //diambil bukan dari stock, tapi dari receiving paling latest
                            ReceivingDetail latestReceiving = db.ReceivingDetails.Where(s => s.Receiving.MaterialCode.Equals(vProductMaster.MaterialCode))
                            .OrderByDescending(s => s.InDate)
                           .ThenBy(s => s.ExpDate)
                           .ThenBy(s => s.QtyPerBag).FirstOrDefault();

                            if (latestReceiving == null)
                            {
                                throw new Exception("Receiving tidak ditemukan.");
                            }

                            stock = db.vStockAlls.Where(s => s.Code.Equals(latestReceiving.StockCode))
                           .OrderBy(s => s.InDate)
                           .ThenBy(s => s.ExpiredDate)
                           .ThenBy(s => s.QtyPerBag).FirstOrDefault();

                            if (stock == null)
                            {
                                throw new Exception("Stock tidak ditemukan.");
                            }
                        }

                        //log print RM
                        //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate

                        lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stock.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + totalFullBag - 1;
                    }

                    DateTime TransactionDate = DateTime.Now;

                    InboundReceive rec = new InboundReceive();
                    rec.ID = Helper.CreateGuid("Ir");
                    rec.InboundOrderID = order.ID;
                    rec.StockCode = stock.Code;
                    if (!order.InboundHeader.WarehouseCode.Equals("2003") || !order.InboundHeader.WarehouseCode.Equals("2004"))
                    {
                        rec.LotNo = stock.LotNumber;
                        rec.ExpDate = stock.ExpiredDate;
                    }
                    rec.InDate = stock.InDate.Value;
                    rec.Qty = req.BagQty * vProductMaster.QtyPerBag;
                    rec.QtyPerBag = vProductMaster.QtyPerBag;
                    rec.ReceivedBy = activeUser;
                    rec.ReceivedOn = TransactionDate;
                    rec.LastSeries = lastSeries;

                    if (rec.Qty > 0)
                    {
                        db.InboundReceives.Add(rec);
                    }

                    if (lastSeries > 0)
                    {
                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Other Inbound Receive";
                        logPrintRM.StockCode = rec.StockCode;
                        logPrintRM.MaterialCode = order.MaterialCode;
                        logPrintRM.MaterialName = order.MaterialName;
                        logPrintRM.LotNumber = rec.LotNo;
                        logPrintRM.InDate = rec.InDate.Value;
                        logPrintRM.ExpiredDate = rec.ExpDate.Value;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }

                    if (req.RemainderQty > 0)
                    {
                        StockCode = string.Format("{0}{1}{2}{3}{4}", vProductMaster.MaterialCode, Helper.FormatThousand(req.RemainderQty), rec.LotNo, rec.InDate.Value.ToString("yyyyMMdd").Substring(1), rec.ExpDate.Value.ToString("yyyyMMdd").Substring(2));
                        rec = new InboundReceive();
                        if (!order.InboundHeader.WarehouseCode.Equals("2003") || !order.InboundHeader.WarehouseCode.Equals("2004"))
                        {
                            rec.LotNo = stock.LotNumber;
                            rec.ExpDate = stock.ExpiredDate;
                        }
                        rec.ID = Helper.CreateGuid("Ir");
                        rec.InboundOrderID = order.ID;
                        rec.StockCode = StockCode;

                        rec.InDate = stock.InDate.Value;

                        rec.Qty = req.RemainderQty;
                        rec.QtyPerBag = req.RemainderQty;
                        rec.ReceivedBy = activeUser;
                        rec.ReceivedOn = TransactionDate;
                        rec.LastSeries = lastSeries;

                        db.InboundReceives.Add(rec);

                        lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries;

                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Other Inbound Receive";
                        logPrintRM.StockCode = rec.StockCode;
                        logPrintRM.MaterialCode = order.MaterialCode;
                        logPrintRM.MaterialName = order.MaterialName;
                        logPrintRM.LotNumber = rec.LotNo;
                        logPrintRM.InDate = rec.InDate.Value;
                        logPrintRM.ExpiredDate = rec.ExpDate.Value;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }
                    if (order.InboundHeader.WarehouseCode == "2003" || order.InboundHeader.WarehouseCode == "2004")
                    {
                        BinRack binRack = await db.BinRacks.Where(m => m.WarehouseCode.Equals(order.InboundHeader.WarehouseCode)).FirstOrDefaultAsync();
                        InboundPutaway putaway = new InboundPutaway();
                        if (!order.InboundHeader.WarehouseCode.Equals("2003") || !order.InboundHeader.WarehouseCode.Equals("2004"))
                        {
                            putaway.LotNo = stock.LotNumber;
                            putaway.ExpDate = stock.ExpiredDate;
                        }
                        putaway.ID = Helper.CreateGuid("Ip");
                        putaway.InboundReceiveID = rec.ID;
                        putaway.PutawayMethod = "MANUAL";
                        putaway.InDate = rec.InDate;
                        putaway.QtyPerBag = rec.QtyPerBag;
                        putaway.StockCode = rec.StockCode;
                        putaway.PutOn = DateTime.Now;
                        putaway.PutBy = activeUser;
                        putaway.BinRackID = binRack.ID;
                        putaway.BinRackCode = binRack.Code;
                        putaway.BinRackName = binRack.Name;
                        putaway.PutawayQty = req.BagQty * rec.QtyPerBag;

                        db.InboundPutaways.Add(putaway);

                        if (vProductMaster.ProdType.Equals("RM"))
                        {
                            //insert to Stock if not exist, update quantity if barcode, indate and location is same

                            StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(rec.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                            if (stockRM != null)
                            {
                                stockRM.Quantity += putaway.PutawayQty;
                            }
                            else
                            {

                                stockRM = new StockRM();
                                if (!order.InboundHeader.WarehouseCode.Equals("2003") || !order.InboundHeader.WarehouseCode.Equals("2004"))
                                {
                                    stockRM.LotNumber = rec.LotNo;
                                    stockRM.ExpiredDate = rec.ExpDate;
                                }
                                stockRM.ID = Helper.CreateGuid("S");
                                stockRM.MaterialCode = vProductMaster.MaterialCode;
                                stockRM.MaterialName = vProductMaster.MaterialName;
                                stockRM.Code = rec.StockCode;                
                                stockRM.InDate = rec.InDate;
                                stockRM.Quantity = putaway.PutawayQty;
                                stockRM.QtyPerBag = rec.QtyPerBag;
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

                            StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(rec.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                            if (stockSFG != null)
                            {
                                stockSFG.Quantity += putaway.PutawayQty;
                            }
                            else
                            {
                                stockSFG = new StockSFG();
                                if (!order.InboundHeader.WarehouseCode.Equals("2003") || !order.InboundHeader.WarehouseCode.Equals("2004"))
                                {
                                    stockSFG.LotNumber = rec.LotNo;
                                    stockSFG.ExpiredDate = rec.ExpDate;
                                }
                                stockSFG.ID = Helper.CreateGuid("S");
                                stockSFG.MaterialCode = vProductMaster.MaterialCode;
                                stockSFG.MaterialName = vProductMaster.MaterialName;
                                stockSFG.Code = rec.StockCode;
                                stockSFG.LotNumber = rec.LotNo;
                                stockSFG.InDate = rec.InDate;
                                stockSFG.ExpiredDate = rec.ExpDate;
                                stockSFG.Quantity = putaway.PutawayQty;
                                stockSFG.QtyPerBag = rec.QtyPerBag;
                                stockSFG.BinRackID = putaway.BinRackID;
                                stockSFG.BinRackCode = putaway.BinRackCode;
                                stockSFG.BinRackName = putaway.BinRackName;
                                stockSFG.ReceivedAt = putaway.PutOn;

                                db.StockSFGs.Add(stockSFG);
                            }
                        }
                    }

                    //find stock code
                    await db.SaveChangesAsync();

                    status = true;
                    message = "Receive berhasil.";
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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }
        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableReceive(string HeaderID)
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

        //    IEnumerable<InboundReceive> list = Enumerable.Empty<InboundReceive>();
        //    IEnumerable<InboundReceiveDTO> pagedData = Enumerable.Empty<InboundReceiveDTO>();
        //    InboundOrderDTO orderDTO = new InboundOrderDTO();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(HeaderID))
        //        {
        //            throw new Exception("Id is required.");
        //        }
        //        InboundOrder order = db.InboundOrders.Where(m => m.InboundID.Equals(HeaderID)).FirstOrDefault();

        //        if (order == null)
        //        {
        //            throw new Exception("Data tidak ditemukan.");

        //        }
        //        bool receiveAction = false;
        //        if (order.InboundHeader.TransactionStatus.Equals("CONFIRMED") && (order.InboundReceives.Sum(m => m.Qty) - order.Qty) > 0)
        //        {
        //            receiveAction = true;
        //        }
        //        orderDTO = new InboundOrderDTO
        //        {
        //            ID = order.ID,
        //            MaterialCode = order.MaterialCode,
        //            MaterialName = order.MaterialName,
        //            MaterialType = order.MaterialType,
        //            Qty = Helper.FormatThousand(order.Qty),
        //            QtyPerBag = Helper.FormatThousand(order.QtyPerBag),
        //            ReceiveQty = Helper.FormatThousand(order.InboundReceives.Sum(m => m.Qty)),
        //            DiffQty = Helper.FormatThousand(order.InboundReceives.Sum(m => m.Qty) - order.Qty),
        //            OutstandingQty = Helper.FormatThousand(order.Qty - (order.InboundReceives.Sum(m => m.Qty))),
        //            CreatedBy = order.CreatedBy,
        //            CreatedOn = Helper.NullDateTimeToString(order.CreatedOn),
        //            ReceiveAction = receiveAction,


        //        };
        //        IQueryable<InboundReceive> query = order.InboundReceives.AsQueryable();
        //        IEnumerable<InboundReceive> tempList = await query.OrderBy(m => m.ReceivedOn).ToListAsync();
        //        Dictionary<string, Func<InboundReceive, object>> cols = new Dictionary<string, Func<InboundReceive, object>>();
        //        cols.Add("ID", x => x.ID);
        //        cols.Add("InboundOrderID", x => x.InboundOrderID);
        //        cols.Add("StockCode", x => x.StockCode);
        //        cols.Add("LotNo", x => x.LotNo);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpDate", x => x.ExpDate);
        //        cols.Add("Qty", x => x.Qty);
        //        cols.Add("QtyPerBag", x => x.QtyPerBag);
        //        cols.Add("BagQty", x => Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)));
        //        cols.Add("ReceivedBy", x => x.ReceivedBy);
        //        cols.Add("ReceivedOn", x => x.ReceivedOn);
        //        cols.Add("PutawayQty", x => x.InboundPutaways.Sum(m => m.PutawayQty));
        //        cols.Add("PutawayBagQty", x => x.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag));
        //        cols.Add("OutstandingQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
        //        cols.Add("OutstandingBagQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
        //        cols.Add("PutawayAction", x => x.Qty > x.InboundPutaways.Sum(m => m.PutawayQty));

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from data in list
        //                        select new InboundReceiveDTO
        //                        {
        //                            ID = data.ID,
        //                            InboundOrderID = data.InboundOrderID,
        //                            StockCode = data.StockCode,
        //                            LotNo = data.LotNo,
        //                            InDate = Helper.NullDateToString(data.InDate),
        //                            ExpDate = Helper.NullDateToString(data.ExpDate),
        //                            Qty = Helper.FormatThousand(data.Qty),
        //                            QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
        //                            BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
        //                            ReceivedBy = data.ReceivedBy,
        //                            ReceivedOn = Helper.NullDateTimeToString(data.ReceivedOn),
        //                            PutawayQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty)),
        //                            PutawayBagQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag)),
        //                            OutstandingQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty)),
        //                            OutstandingBagQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty) / data.QtyPerBag),
        //                            //PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
        //                            PutawayAction = data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
        //                            PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),

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
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}
        [HttpPost]
        public async Task<IHttpActionResult> DatatableReceive(string HeaderID)
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

            IEnumerable<InboundReceive> list = Enumerable.Empty<InboundReceive>();
            IEnumerable<InboundReceiveDTO> pagedData = Enumerable.Empty<InboundReceiveDTO>();

            IQueryable<InboundReceive> query = db.InboundReceives.Where(s => s.InboundOrder.InboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.InboundOrder.MaterialCode.Contains(search)
                        || m.InboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<InboundReceive, object>> cols = new Dictionary<string, Func<InboundReceive, object>>();
                cols.Add("InboundOrderID", x => x.InboundOrderID);
                cols.Add("StockCode", x => x.StockCode);
                cols.Add("MaterialCode", x => x.InboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.InboundOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)));
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);
                cols.Add("PutawayQty", x => x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("PutawayBagQty", x => x.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag));
                cols.Add("OutstandingQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("OutstandingBagQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("PutawayAction", x => x.Qty > x.InboundPutaways.Sum(m => m.PutawayQty));

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from data in list
                                select new InboundReceiveDTO
                                {
                                    ID = data.ID,
                                    MaterialCode = data.InboundOrder.MaterialCode,
                                    MaterialName = data.InboundOrder.MaterialName,
                                    InboundOrderID = data.InboundOrderID,
                                    StockCode = data.StockCode,
                                    LotNo = data.LotNo,
                                    InDate = Helper.NullDateToString(data.InDate),
                                    ExpDate = Helper.NullDateToString(data.ExpDate),
                                    Qty = Helper.FormatThousand(data.Qty),
                                    QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                                    BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
                                    ReceivedBy = data.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(data.ReceivedOn),
                                    PutawayQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty)),
                                    PutawayBagQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag)),
                                    OutstandingQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty)),
                                    OutstandingBagQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty) / data.QtyPerBag),
                                    //PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                                    PutawayAction = data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                                    PrintBarcodeAction = data.LastSeries > 0 && data.InboundOrder.InboundHeader.WarehouseCode != "2003" && data.InboundOrder.InboundHeader.WarehouseCode != "2004",
                                    LastSeries = data.LastSeries.ToString()
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
        public async Task<IHttpActionResult> Putaway(InboundPutawayWebReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string binRackCode = string.Empty;


            int receiveBagQty = 0;
            int putBagQty = 0;
            int availableBagQty = 0;

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
                    if (string.IsNullOrEmpty(req.ReceiveId))
                    {
                        throw new Exception("Receive Id is required.");
                    }

                    InboundReceive receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (!receive.InboundOrder.InboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi karena transaksi sudah selesai.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.InboundOrder.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string MaterialCode = vProductMaster.MaterialCode;
                    string QtyPerBag = receive.QtyPerBag.ToString();
                    string LotNumber = receive.LotNo;
                    string InDate = Convert.ToDateTime(receive.InDate).ToString("yyyyMMdd").Substring(1);
                    string ExpiredDate = Convert.ToDateTime(receive.ExpDate).ToString("yyyyMMdd").Substring(1);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    if (req.BagQty <= 0)
                    {
                        ModelState.AddModelError("req.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        receiveBagQty = Convert.ToInt32(receive.Qty / receive.QtyPerBag);
                        putBagQty = Convert.ToInt32(receive.InboundPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
                        availableBagQty = receiveBagQty - putBagQty;

                        if (req.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("req.PutawayQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                        }
                    }



                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        ModelState.AddModelError("req.BinRackCode", "BinRack is required.");
                    }
                    else
                    {
                        string binRankCode = req.BinRackCode.Split('-')[0];
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(binRankCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("req.BinRackCode", "BinRack is not recognized.");
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

                    InboundPutaway putaway = new InboundPutaway();
                    putaway.ID = Helper.CreateGuid("Ip");
                    putaway.InboundReceiveID = receive.ID;
                    putaway.PutawayMethod = "MANUAL";
                    putaway.LotNo = receive.LotNo;
                    putaway.InDate = receive.InDate;
                    putaway.ExpDate = receive.ExpDate;
                    putaway.QtyPerBag = receive.QtyPerBag;
                    putaway.StockCode = receive.StockCode;
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * receive.QtyPerBag;

                    db.InboundPutaways.Add(putaway);

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receive.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = vProductMaster.MaterialCode;
                            stockRM.MaterialName = vProductMaster.MaterialName;
                            stockRM.Code = receive.StockCode;
                            stockRM.LotNumber = receive.LotNo;
                            stockRM.InDate = receive.InDate;
                            stockRM.ExpiredDate = receive.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = receive.QtyPerBag;
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

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(receive.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = vProductMaster.MaterialCode;
                            stockSFG.MaterialName = vProductMaster.MaterialName;
                            stockSFG.Code = receive.StockCode;
                            stockSFG.LotNumber = receive.LotNo;
                            stockSFG.InDate = receive.InDate;
                            stockSFG.ExpiredDate = receive.ExpDate;
                            stockSFG.Quantity = putaway.PutawayQty;
                            stockSFG.QtyPerBag = receive.QtyPerBag;
                            stockSFG.BinRackID = putaway.BinRackID;
                            stockSFG.BinRackCode = putaway.BinRackCode;
                            stockSFG.BinRackName = putaway.BinRackName;
                            stockSFG.ReceivedAt = putaway.PutOn;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway berhasil.";


                    receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();
                    receiveBagQty = Convert.ToInt32(receive.Qty / receive.QtyPerBag);
                    putBagQty = Convert.ToInt32(receive.InboundPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
                    availableBagQty = receiveBagQty - putBagQty;

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

            obj.Add("receive_qty", Helper.FormatThousand(receiveBagQty));
            obj.Add("put_qty", Helper.FormatThousand(putBagQty));
            obj.Add("available_qty", Helper.FormatThousand(availableBagQty));
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }
        [HttpPost]
        public async Task<IHttpActionResult> DatatablePutaway(string HeaderID)
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

            IEnumerable<InboundPutaway> list = Enumerable.Empty<InboundPutaway>();
            IEnumerable<InboundPutawayDTO> pagedData = Enumerable.Empty<InboundPutawayDTO>();

            IQueryable<InboundPutaway> query = db.InboundPutaways.Where(s => s.InboundReceive.InboundOrder.InboundID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.InboundReceive.InboundOrder.MaterialCode.Contains(search)
                        || m.InboundReceive.InboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<InboundPutaway, object>> cols = new Dictionary<string, Func<InboundPutaway, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("MaterialCode", x => x.InboundReceive.InboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.InboundReceive.InboundOrder.MaterialName);

                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("PutawayQty", x => x.PutawayQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
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
                    pagedData = from data in list
                                select new InboundPutawayDTO
                                {
                                    BinRackCode = data.BinRackCode,
                                    MaterialCode = data.InboundReceive.InboundOrder.MaterialCode,
                                    MaterialName = data.InboundReceive.InboundOrder.MaterialName,
                                    PutawayMethod = data.PutawayMethod,
                                    ID = data.ID,
                                    InboundOrderID = data.InboundReceive.InboundOrderID,
                                    LotNo = data.LotNo,
                                    InDate = Helper.NullDateToString(data.InDate),
                                    ExpDate = Helper.NullDateToString(data.ExpDate),
                                    Qty = Helper.FormatThousand(data.PutawayQty),
                                    QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                                    BagQty = Helper.FormatThousand(data.PutawayQty / data.QtyPerBag),
                                    PutBy = data.PutBy,
                                    PutOn = Helper.NullDateTimeToString(data.PutOn),
                                    //commented by bhov
                                    //INI APA COBA MAKSUDNYA ELAH, GOBLOOOOOOOOOK
                                    //PutawayQty = Helper.FormatThousand(data.InboundReceive.InboundPutaways.Sum(m => m.PutawayQty)),
                                    //PutawayBagQty = Helper.FormatThousand(data.InboundReceive.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag)),
                                    //OutstandingQty = Helper.FormatThousand(data.InboundReceive.Qty - data.InboundReceive.InboundPutaways.Sum(m => m.PutawayQty)),
                                    //OutstandingBagQty = Helper.FormatThousand(data.InboundReceive.Qty - data.InboundReceive.InboundPutaways.Sum(m => m.PutawayQty) / data.QtyPerBag),
                                    //PrintBarcodeAction = data.InboundReceive.LastSeries > 0
                                    //PutawayAction = data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty)
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
        public async Task<IHttpActionResult> Print(InboundReceivePrintReq req)
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

                    InboundReceive receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.InboundOrder.MaterialCode)).FirstOrDefault();
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

                    int fullBag = Convert.ToInt32(receive.Qty / receive.QtyPerBag);

                    int lastSeries = receive.LastSeries;


                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = receive.InboundOrder.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(receive.QtyPerBag).PadLeft(6) + " " + receive.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);

                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = receive.InDate.Value;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = receive.ExpDate.Value;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = receive.InboundOrder.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = receive.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = receive.InboundOrder.MaterialName;
                        dto.Field9 = Helper.FormatThousand(receive.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = receive.InboundOrder.MaterialCode;
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

                            string folder_name = "";
                            foreach (PrinterDTO printerDTO in printers)
                            {
                                if (printerDTO.PrinterIP.Equals(req.Printer))
                                {
                                    folder_name = printerDTO.PrinterName;
                                }
                            }

                            string file_name = string.Format("{0}.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"));

                            using (Stream fileStream = new FileStream(string.Format(@"C:\RMI_PRINTER\{0}\{1}", folder_name, file_name), FileMode.CreateNew))
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

                List<InboundPutaway> receivingDetail = await db.InboundPutaways.Where(m => m.InboundReceiveID.Equals(inboundDetailId)).ToListAsync();

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
                           BagQty = Helper.FormatThousand(Convert.ToInt32(dat.PutawayQty / dat.QtyPerBag)),
                           QtyPerBag = Helper.FormatThousand(dat.QtyPerBag),
                           TotalQty = Helper.FormatThousand(dat.PutawayQty),
                           Series = string.Format("{0} - {1}", dat.InboundReceive.LastSeries - Convert.ToInt32(dat.PutawayQty / dat.QtyPerBag) + 1, dat.InboundReceive.LastSeries)
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
        public async Task<IHttpActionResult> DatatableDetailOtherInbound()
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

            string date = request["date"].ToString();
            string warehouseCode = request["warehouseCode"].ToString();

            IEnumerable<InboundReceive> list = Enumerable.Empty<InboundReceive>();
            IEnumerable<InboundReceiveDTOReport> pagedData = Enumerable.Empty<InboundReceiveDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<InboundReceive> query;

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                query = db.InboundReceives.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate)
                        && s.InboundOrder.InboundHeader.WarehouseCode.Equals(warehouseCode));
            }
            else
            {
                query = db.InboundReceives.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.InboundOrder.MaterialCode.Contains(search)
                        || m.InboundOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<InboundReceive, object>> cols = new Dictionary<string, Func<InboundReceive, object>>();
                cols.Add("InboundOrderID", x => x.InboundOrderID); 
                cols.Add("StockCode", x => x.StockCode);
                cols.Add("MaterialCode", x => x.InboundOrder.MaterialCode);
                cols.Add("MaterialName", x => x.InboundOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => Convert.ToInt32(Math.Floor(x.Qty / x.QtyPerBag)));
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);
                cols.Add("PutawayQty", x => x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("PutawayBagQty", x => x.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag));
                cols.Add("OutstandingQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("OutstandingBagQty", x => x.Qty - x.InboundPutaways.Sum(m => m.PutawayQty));
                cols.Add("PutawayAction", x => x.Qty > x.InboundPutaways.Sum(m => m.PutawayQty));

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from data in list
                                select new InboundReceiveDTOReport
                                {
                                    ID = data.ID,
                                    ReceiptDate = Helper.NullDateTimeToString(data.ReceivedOn),
                                    ReceiptNo = data.InboundOrder.InboundHeader.Code,
                                    WarehouseCode = data.InboundOrder.InboundHeader.WarehouseCode,
                                    WarehouseName = data.InboundOrder.InboundHeader.WarehouseName,
                                    MaterialCode = data.InboundOrder.MaterialCode,
                                    MaterialName = data.InboundOrder.MaterialName,
                                    Uom = data.InboundOrder.InboundHeader.Remarks,
                                    QtyL = Helper.FormatThousand(data.Qty),
                                    Qty = Helper.FormatThousand(data.Qty),
                                    Memo = data.InboundOrder.InboundHeader.Remarks,
                                    InboundOrderID = data.InboundOrderID,
                                    StockCode = data.StockCode,
                                    LotNo = data.LotNo,
                                    InDate = Helper.NullDateToString(data.InDate),
                                    ExpDate = Helper.NullDateToString(data.ExpDate),
                                    QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                                    BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
                                    ReceivedBy = data.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(data.ReceivedOn),
                                    PutawayQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty)),
                                    PutawayBagQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag)),
                                    OutstandingQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty)),
                                    OutstandingBagQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty) / data.QtyPerBag),
                                    //PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                                    PutawayAction = data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                                    PrintBarcodeAction = data.LastSeries > 0 && data.InboundOrder.InboundHeader.WarehouseCode != "2003" && data.InboundOrder.InboundHeader.WarehouseCode != "2004",
                                    LastSeries = data.LastSeries.ToString()
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
        public async Task<IHttpActionResult> GetDataReportOtherInbound(string date, string warehouse)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(warehouse))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vOtherInbound> list = Enumerable.Empty<vOtherInbound>();
            IEnumerable<InboundReceiveDTOReport> pagedData = Enumerable.Empty<InboundReceiveDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<vOtherInbound> query;

            if (!string.IsNullOrEmpty(warehouse))
            {
                query = db.vOtherInbounds.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate)
                        && s.WarehouseCode.Equals(warehouse));
            }
            else
            {
                query = db.vOtherInbounds.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vOtherInbound, object>> cols = new Dictionary<string, Func<vOtherInbound, object>>();
                cols.Add("ReceiptNo", x => x.Code);
                cols.Add("WarehouseCode", x => x.WarehouseCode);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Uom", x => x.Uom);
                cols.Add("QtyL", x => x.QtyL);
                cols.Add("Qty", x => x.Qty);
                cols.Add("Memo", x => x.Remarks);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from data in list
                                select new InboundReceiveDTOReport
                                {
                                    ReceiptDate = data.ReceivedOn.ToString("yyyy-MM-dd"),
                                    ReceiptNo = data.Code != null ? data.Code : "",
                                    WarehouseCode = data.WarehouseCode,
                                    WarehouseName = data.WarehouseName,
                                    MaterialCode = data.MaterialCode,
                                    MaterialName = data.MaterialName,
                                    Uom = data.Uom,
                                    QtyL = Helper.FormatThousand(data.QtyL),
                                    Qty = Helper.FormatThousand(data.Qty),
                                    Memo = data.Remarks != null ? data.Remarks : "",
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
