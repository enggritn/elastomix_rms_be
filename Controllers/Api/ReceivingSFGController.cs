using ExcelDataReader;
using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using WMS_BE.Models;
using WMS_BE.Utils;
using ZXing;
using ZXing.QrCode;

namespace WMS_BE.Controllers.Api
{
    public class ReceivingSFGController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [HttpPost]
        public async Task<IHttpActionResult> Upload()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            string warehouseID = "3001";
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
                    if (!string.IsNullOrEmpty(warehouseID))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(warehouseID)).FirstOrDefaultAsync();

                        if (wh != null)
                        {
                            if (request.Files.Count > 0)
                            {
                                HttpPostedFile file = request.Files[0];

                                if (file != null && file.ContentLength > 0 && (System.IO.Path.GetExtension(file.FileName).ToLower() == ".xlsx" || System.IO.Path.GetExtension(file.FileName).ToLower() == ".xls"))
                                {
                                    if (file.ContentLength < (10 * 1024 * 1024))
                                    {
                                        try
                                        {
                                            Stream stream = file.InputStream;
                                            IExcelDataReader reader = null;
                                            if ((System.IO.Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                            {
                                                reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                                            }
                                            else
                                            {
                                                reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                            }

                                            DataSet result = reader.AsDataSet();
                                            reader.Close();

                                            DataTable dt = result.Tables[0];

                                            string prefix = "RSFG";

                                            List<ReceivingSFG> updatedList = new List<ReceivingSFG>();

                                            string sfgCode = "";
                                            foreach (DataRow row in dt.AsEnumerable().Skip(1))
                                            {
                                                ReceivingSFG receiving = new ReceivingSFG();


                                                string x = row[1].ToString();
                                                sfgCode = x;
                                                vProduct product = await db.vProducts.Where(s => s.MaterialCode.Equals(x)).FirstOrDefaultAsync();

                                                if (product != null)
                                                {
                                                    if (product.ProdType.Equals("SFG"))
                                                    {
                                                        SemiFinishGood sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(x)).FirstOrDefaultAsync();
                                                        if (sfg.AB.Equals("B"))
                                                        {
                                                            continue;
                                                        }
                                                        string InDate = row[0].ToString();
                                                        if (!string.IsNullOrEmpty(InDate))
                                                        {
                                                            try
                                                            {
                                                                receiving.InDate = DateTime.ParseExact(InDate, "yyyy-MM-dd", null);
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }
                                                        }
                                                        receiving.ProductCode = row[1].ToString();
                                                        receiving.ProductName = row[2].ToString();
                                                        string qty = row[3].ToString();
                                                        int index = qty.LastIndexOf(".");
                                                        if (index > 0)
                                                        {
                                                            qty = qty.Substring(0, index);
                                                        }
                                                        receiving.Qty = Decimal.Parse(qty);
                                                        //receiving.Qty = Math.Floor(Convert.ToDecimal(qty));
                                                        receiving.QtyPerBag = sfg.WeightPerBag;
                                                        receiving.UoM = row[4].ToString();
                                                        receiving.LotNo = row[5].ToString();
                                                        string ProductionDate = row[6].ToString();
                                                        if (!string.IsNullOrEmpty(ProductionDate))
                                                        {
                                                            try
                                                            {
                                                                receiving.ProductionDate = DateTime.ParseExact(ProductionDate, "yyyy-MM-dd", null);
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }
                                                        }
                                                        
                                                        string ExpDate = row[7].ToString();
                                                        if (!string.IsNullOrEmpty(ExpDate))
                                                        {
                                                            try
                                                            {
                                                                receiving.ExpDate = DateTime.ParseExact(ExpDate, "yyyy-MM-dd", null);
                                                            }
                                                            catch (Exception)
                                                            {

                                                            }
                                                        }

                                                        receiving.ID = Helper.CreateGuid(prefix);
                                                        receiving.TransactionStatus = "OPEN";
                                                        receiving.WarehouseCode = wh.Code;
                                                        receiving.WarehouseName = wh.Name;
                                                        receiving.CreatedBy = activeUser;
                                                        receiving.CreatedOn = DateTime.Now;

                                                        //ReceivingSFG checker = await db.ReceivingSFGs.Where(s => s.Barcode.Equals(receiving.Barcode) && s.LotNo.Equals(receiving.LotNo)).FirstOrDefaultAsync();

                                                        //if (checker == null)
                                                        db.ReceivingSFGs.Add(receiving);
                                                    }
                                                }
                                                else
                                                {
                                                    throw new Exception(string.Format("Item Number {0} not recognized, please check WIP Master Data.", sfgCode));
                                                }
                                            }

                                            await db.SaveChangesAsync();
                                            message = "Upload succeeded.";
                                            status = true;


                                        }
                                        catch (Exception e)
                                        {
                                            message = string.Format("Upload item failed. {0}", e.Message);
                                        }
                                    }
                                    else
                                    {
                                        message = "Upload failed. Maximum allowed file size : 10MB ";
                                    }
                                }
                                else
                                {
                                    message = "Upload item failed. File is invalid.";
                                }
                            }
                            else
                            {
                                message = "No file uploaded.";
                            }
                        }
                        else
                        {
                            message = "Warehouse is not recognized.";
                        }
                    }
                    else
                    {
                        message = "Warehouse is required.";
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

        public async Task<IHttpActionResult> UpdateReceiving(ReceivingSFGVM dataVM)
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

                    ReceivingSFG receiving = null;

                    if (string.IsNullOrEmpty(dataVM.PurchaseRequestID))
                    {
                        throw new Exception("Request ID is required.");
                    }
                    else
                    {
                        receiving = await db.ReceivingSFGs.Where(m => m.PurchaseRequestID.Equals(dataVM.PurchaseRequestID)).FirstOrDefaultAsync();
                        if (receiving == null)
                        {
                            throw new Exception("ID is not recognized.");
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.LotNo))
                    {
                        ModelState.AddModelError("Receiving.LotNo", "Lot Number can not be empty.");
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
                    receiving.InDate = transactionDate;
                    receiving.ProductionDate = transactionDate;


                    //get expired date
                    SemiFinishGood sfg = db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(receiving.ProductCode)).FirstOrDefault();
                    if(sfg == null)
                    {
                        throw new Exception("Material not recognized.");
                    }

                    int ShelfLife = Convert.ToInt32(Regex.Match(sfg.ExpiredDate, @"\d+").Value);
                    int days = 0;

                    string LifeRange = Regex.Replace(sfg.ExpiredDate, @"[\d-]", string.Empty).ToString();


                    if (LifeRange.ToLower().Contains("year"))
                    {
                        days = (ShelfLife * (Convert.ToInt32(12 * 30))) - 1;
                    }
                    else if (LifeRange.ToLower().Contains("month"))
                    {
                        days = (Convert.ToInt32(ShelfLife * 30)) - 1;
                    }
                    else
                    {
                        days = ShelfLife - 1;
                    }

                    receiving.ExpDate = Convert.ToDateTime(receiving.InDate).AddDays(days);
                    receiving.LotNo = dataVM.LotNo.Trim().ToString();


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Update receiving succeeded.";
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
        public async Task<IHttpActionResult> DatatableReceiving()
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

            IEnumerable<ReceivingSFG> list = Enumerable.Empty<ReceivingSFG>();
            IEnumerable<ReceivingSFGDTO> pagedData = Enumerable.Empty<ReceivingSFGDTO>();

            IQueryable<ReceivingSFG> query = db.ReceivingSFGs.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))).AsQueryable();

            //query.GroupBy(a => new { a.ProductCode, a.LotNo, a.InDate, a.ExpDate })
            //    .Select(a => new { ProductCode = a.Key.ProductCode, LotNo = a.Key.LotNo, InDate = a.Key.InDate, ExpDate = a.Key.ExpDate, Qty = a.Sum(b => b.Qty)})
            //    .ToList();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.ProductCode.Contains(search) || m.ProductName.Contains(search));

                Dictionary<string, Func<ReceivingSFG, object>> cols = new Dictionary<string, Func<ReceivingSFG, object>>();
                cols.Add("MaterialRequestCode", x => x.PurchaseRequestDetail.PurchaseRequestHeader.Code);
                cols.Add("ProductCode", x => x.ProductCode);
                cols.Add("ProductName", x => x.ProductName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("OKQty", x => x.Qty);
                //cols.Add("NGQty", x => x.NGQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from receiving in list
                                select new ReceivingSFGDTO
                                {
                                    ID = receiving.ID,
                                    MaterialRequestID = receiving.PurchaseRequestID,
                                    MaterialRequestCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                                    ProductCode = receiving.ProductCode,
                                    ProductName = receiving.ProductName,
                                    Qty = Helper.FormatThousand(receiving.Qty),
                                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                                    UoM = receiving.UoM,
                                    LotNo = receiving.LotNo,
                                    InDate = Helper.NullDateToString2(receiving.InDate),
                                    ExpDate = Helper.NullDateToString2(receiving.ExpDate),
                                    //ReceivedOn = Helper.NullDateTimeToString(receiving.ReceivedOn),
                                    OKQty = Helper.FormatThousand(receiving.Qty),
                                    //OKQty = Helper.FormatThousand(receiving.Qty - receiving.NGQty),
                                    //NGQty = Helper.FormatThousand(receiving.NGQty)
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
        public async Task<IHttpActionResult> DatatableReceivingGroup()
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

            IEnumerable<vReceivingSFG2> list = Enumerable.Empty<vReceivingSFG2>();
            IEnumerable<vReceivingSFGDTO> pagedData = Enumerable.Empty<vReceivingSFGDTO>();

            IQueryable<vReceivingSFG2> query = db.vReceivingSFG2.AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            string DefaultLot = DateTime.Now.ToString("yyyMMdd").Substring(1);

            try
            {
                query = query
                        .Where(m => m.ProductCode.Contains(search) || m.ProductName.Contains(search));

                Dictionary<string, Func<vReceivingSFG2, object>> cols = new Dictionary<string, Func<vReceivingSFG2, object>>();
                cols.Add("WarehouseCode", x => x.WarehouseCode);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("MaterialRequestCode", x => x.Code);
                cols.Add("ProductCode", x => x.ProductCode);
                cols.Add("ProductName", x => x.ProductName);
                cols.Add("TotalOrder", x => x.TotalOrder);
                cols.Add("AvailableReceive", x => x.AvailableReceive);
                cols.Add("TotalReceive", x => x.TotalReceive);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from receiving in list
                                select new vReceivingSFGDTO
                                {
                                    WarehouseCode = receiving.WarehouseCode,
                                    WarehouseName = receiving.WarehouseName,
                                    MaterialRequestCode = receiving.Code,
                                    PurchaseRequestDetailID = receiving.PurchaseRequestID,
                                    ProductCode = receiving.ProductCode,
                                    ProductName = receiving.ProductName,
                                    TotalOrder = Helper.FormatThousand(receiving.TotalOrder),
                                    AvailableReceive = Helper.FormatThousand(receiving.AvailableReceive),
                                    TotalReceive = Helper.FormatThousand(receiving.TotalReceive),
                                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.TotalOrder / receiving.QtyPerBag)),
                                    UoM = receiving.UoM,
                                    LotNo = receiving.LotNo,
                                    InDate = Helper.NullDateToString2(receiving.InDate),
                                    ExpDate = Helper.NullDateToString2(receiving.ExpDate),
                                    ReceiveAction = !string.IsNullOrEmpty(receiving.LotNo) && (receiving.InDate != null) && receiving.TotalOrder != receiving.TotalReceive,
                                    PrintBarcodeAction = true,
                                    PutawayAction = true,
                                    EditReceiveAction = string.IsNullOrEmpty(receiving.LotNo) && !(receiving.InDate != null),
                                    DefaultLotNo = DefaultLot
                                    
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
        //public async Task<IHttpActionResult> DatatableBarcode(ReceivingSFGList barcode)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];


        //    HttpRequest request = HttpContext.Current.Request;
        //    string inDate = request["InDate"].ToString();
        //    string expDate = request["ExpDate"].ToString();
        //    DateTime xInDate = new DateTime();
        //    DateTime xExpDate = new DateTime();

        //    string message = "";
        //    bool status = false;

        //    IEnumerable<vListBarcodeSFG> templist = Enumerable.Empty<vListBarcodeSFG>();            
        //    IQueryable<vListBarcodeSFG> query = db.vListBarcodeSFGs.AsQueryable();
        //    IEnumerable<BinRackArea> tempList = await db.BinRackAreas.OrderBy(m => m.Code).ToListAsync();

        //    IEnumerable<vListBarcodeSFGDTO> data = Enumerable.Empty<vListBarcodeSFGDTO>();
        //    //query =  query
        //    //    .Where(s => s.ProductCode.Equals(barcode.ProductCode) && s.LotNo.Equals(barcode.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).ToListAsync();

        //    var list = query.Select(x => new vListBarcodeSFGDTO()
        //    {
        //        StockCode = x.StockCode
        //    });

        //    return Ok(list);
        //}


        [HttpPost]
        public async Task<IHttpActionResult> Receive(ReceivingSFGVM receivingVM)
        {
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

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
                    if (DateTime.TryParse(inDate, out temp))
                    {
                        xInDate = Convert.ToDateTime(inDate);
                    }
                    if (DateTime.TryParse(expDate, out temp1))
                    {
                        xExpDate = Convert.ToDateTime(expDate);
                    }

                    //ReceivingSFG receiving = await db.ReceivingSFGs.Where(s => s.ProductCode.Equals(receivingVM.ProductCode) && s.LotNo.Equals(receivingVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefaultAsync();

                    vReceivingSFG2 receiving = await db.vReceivingSFG2.Where(s => s.ProductCode.Equals(receivingVM.ProductCode) && s.LotNo.Equals(receivingVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefaultAsync();

                    SemiFinishGood sfg = null;

                    if (receiving == null)
                    {
                        ModelState.AddModelError("ReceivingSFG.ReceiveOKQty", string.Format("Receiving is not recognized."));
                    }
                    else
                    {
                        sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(receiving.ProductCode)).FirstOrDefaultAsync();
                    }

                    if (receivingVM.OKQty <= 0)
                    {
                        ModelState.AddModelError("ReceivingSFG.ReceiveOKQty", string.Format("Qty can not be empty or below zero."));
                    }
                    else
                    {
                        int availableQty = Convert.ToInt32(receiving.TotalOrder) - Convert.ToInt32(receiving.TotalReceive);
                        if (receivingVM.OKQty > availableQty)
                        {
                            ModelState.AddModelError("ReceivingSFG.ReceiveOKQty", string.Format("Qty exceeded. Available Qty : {0}", availableQty));
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

                    decimal sisa = receivingVM.OKQty / receiving.QtyPerBag;
                    sisa = receivingVM.OKQty - (Math.Floor(sisa) * receiving.QtyPerBag);

                    //receiving.ATA = DateTime.Now;
                    //receiving.Barcode = receiving.ProductCode.PadRight(7) + receiving.InDate.ToString("yyyyMMdd").Substring(1) + receiving.ExpDate.ToString("yyyyMMdd").Substring(2)+receivingVM.OKQty.ToString();
                    //receiving.TransactionStatus = "PROGRESS";
                    //receiving.ReceivedBy = activeUser;
                    //receiving.ReceivedOn = DateTime.Now;
                    string bcd = receiving.ProductCode.PadRight(7) + receiving.InDate.ToString("yyyyMMdd").Substring(1) + receiving.ExpDate.ToString("yyyyMMdd").Substring(2);
                    bcd = bcd + sisa.ToString();

                    // tambahan disini
                    string barcodeOK = "";
                    int actBag = Decimal.ToInt32(receivingVM.OKQty / receiving.QtyPerBag);
                    //if (actBag < 1)
                    //{
                    //    actBag = 1;
                    //    sisa = 0;
                    //    //barcodeOK = receiving.ProductCode.PadRight(7) + actBag.ToString("0") + receiving.LotNo;
                    //    barcodeOK = string.Format("{0}{1}{2}{3}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(actBag).PadLeft(6), receiving.LotNo, receiving.InDate.ToString("yyyyMMdd").Substring(1));
                    //    receiving.QtyPerBag = receivingVM.OKQty;
                    //}
                    //else
                    //{
                    //    //barcodeOK = receiving.ProductCode.PadRight(7) + receiving.QtyPerBag.ToString("0") + receiving.LotNo;
                    //    //barcodeOK = string.Format("{0}{1}{2}", receiving.ProductCode.PadRight(7), receiving.QtyPerBag.ToString().PadLeft(6), receiving.LotNo);
                    //    barcodeOK = string.Format("{0}{1}{2}{3}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(receiving.QtyPerBag).PadLeft(6), receiving.LotNo, receiving.InDate.ToString("yyyyMMdd").Substring(1));
                    //}

                    string eDate, iDate = "";
                    DateTime dt1 = xInDate;
                    DateTime dt2 = xExpDate;
                    iDate = dt1.ToString("yyyy-MM-dd");
                    eDate = dt2.ToString("yyyy-MM-dd");

                    if (actBag > 0)
                    {
                        barcodeOK = string.Format("{0}{1}{2}{3}{4}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(receiving.QtyPerBag).PadLeft(6), receiving.LotNo, receiving.InDate.ToString("yyyyMMdd").Substring(1), receiving.ExpDate.ToString("yyyyMMdd").Substring(2));
                        int startSeries = 0;
                        int lastSeries = await db.ReceivingSFGDetails.Where(m => m.StockCode.Equals(barcodeOK.Replace(" ", ""))).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }
                        lastSeries = startSeries + actBag - 1;

                        ReceivingSFGDetail receivingSFGDetail = new ReceivingSFGDetail();
                        string strID = Helper.CreateGuid("RCd");
                        receivingSFGDetail.ID = strID;
                        receivingSFGDetail.StockCode = barcodeOK.Replace(" ", "");
                        receivingSFGDetail.ProductCode = receiving.ProductCode;
                        receivingSFGDetail.LotNo = receiving.LotNo;
                        receivingSFGDetail.InDate = DateTime.ParseExact(iDate, "yyyy-MM-dd", null);
                        receivingSFGDetail.ExpDate = DateTime.ParseExact(eDate, "yyyy-MM-dd", null);
                        receivingSFGDetail.LastSeries = lastSeries;
                        receivingSFGDetail.Qty = receivingVM.OKQty - sisa;
                        receivingSFGDetail.ReceivedBy = activeUser;
                        receivingSFGDetail.QtyPerBag = receiving.QtyPerBag;
                        receivingSFGDetail.ReceivedOn = DateTime.Now;
                        db.ReceivingSFGDetails.Add(receivingSFGDetail);
                    }

                    if (sisa > 0)
                    {
                        string barcodeReceh = string.Format("{0}{1}{2}{3}{4}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(sisa).PadLeft(6), receiving.LotNo, receiving.InDate.ToString("yyyyMMdd").Substring(1), receiving.ExpDate.ToString("yyyyMMdd").Substring(2));
                        string strID_receh = Helper.CreateGuid("RCd");

                        int startSeries1 = 0;
                        int lastSeries1 = await db.ReceivingSFGDetails.Where(m => m.StockCode.Equals(barcodeReceh.Replace(" ", ""))).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries1 == 0)
                        {
                            startSeries1 = 1;
                        }
                        else
                        {
                            startSeries1 = lastSeries1 + 1;
                        }
                        lastSeries1 = startSeries1;

                        ReceivingSFGDetail receivingSFGDetail_receh = new ReceivingSFGDetail();
                        receivingSFGDetail_receh.ID = strID_receh;
                        receivingSFGDetail_receh.StockCode = barcodeReceh.Replace(" ", "");
                        receivingSFGDetail_receh.ProductCode = receiving.ProductCode;
                        receivingSFGDetail_receh.LotNo = receiving.LotNo;
                        receivingSFGDetail_receh.InDate = DateTime.ParseExact(iDate, "yyyy-MM-dd", null);
                        receivingSFGDetail_receh.ExpDate = DateTime.ParseExact(eDate, "yyyy-MM-dd", null);
                        receivingSFGDetail_receh.LastSeries = lastSeries1;
                        receivingSFGDetail_receh.Qty = sisa;
                        receivingSFGDetail_receh.ReceivedBy = activeUser;
                        receivingSFGDetail_receh.QtyPerBag = sisa;
                        receivingSFGDetail_receh.ReceivedOn = DateTime.Now;
                        db.ReceivingSFGDetails.Add(receivingSFGDetail_receh);
                    }
                    await db.SaveChangesAsync();

                    status = true;
                    message = "Receiving succeeded.";

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
        public async Task<IHttpActionResult> ListBarcodeSFGPutaway(vListBarcodeSFG req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

            if (DateTime.TryParse(inDate, out temp))
            {
                xInDate = Convert.ToDateTime(inDate);
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                xExpDate = Convert.ToDateTime(expDate);
            }

            IEnumerable<vListBarcodeSFG> list = Enumerable.Empty<vListBarcodeSFG>();
            List<vListBarcodeSFGDTO> data = new List<vListBarcodeSFGDTO>();

            IQueryable<vListBarcodeSFG> query = db.vListBarcodeSFGs.Where(s => s.ProductCode.Equals(req.ProductCode) && s.LotNo.Equals(req.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).AsQueryable();

            try
            {
                list = query.ToList();
                if (list != null && list.Count() > 0)
                {
                    foreach (vListBarcodeSFG detail in list)
                    {
                        decimal totstockqty = 0;
                        decimal QtyActualAvailable = 0;
                        int count = 1;
                        IEnumerable<ReceivingSFGDetail> listreceive = Enumerable.Empty<ReceivingSFGDetail>();
                        IQueryable<ReceivingSFGDetail> querya = db.ReceivingSFGDetails.Where(s => s.StockCode.Equals(detail.StockCode)).OrderByDescending(s => s.ReceivedOn).AsQueryable();
                        listreceive = querya.ToList();
                        if (listreceive != null && listreceive.Count() > 0)
                        {
                            foreach (ReceivingSFGDetail stocka in listreceive)
                            {
                                if (count == 1)
                                {
                                    QtyActualAvailable = stocka.LastSeries * stocka.QtyPerBag;
                                    count++;
                                }
                            }
                        }

                        IEnumerable<StockSFG> liststock = Enumerable.Empty<StockSFG>();
                        IQueryable<StockSFG> querystock = db.StockSFGs.Where(s => s.Code.Equals(detail.StockCode)).OrderByDescending(s => s.InDate).AsQueryable();
                        liststock = querystock.ToList();
                        if (liststock != null && liststock.Count() > 0)
                        {
                            foreach (StockSFG stock in liststock)
                            {
                                if (stock.Quantity >= 0)
                                {
                                    totstockqty = totstockqty + stock.Quantity;
                                }
                            }
                        }

                        QtyActualAvailable = QtyActualAvailable - totstockqty;

                        if (QtyActualAvailable > 0)
                        {
                            vListBarcodeSFGDTO dat = new vListBarcodeSFGDTO
                            {
                                StockCode = detail.StockCode,
                                ProductCode = detail.ProductCode,
                                ProductName = detail.MaterialName,
                                InDate = Helper.NullDateToString(detail.InDate),
                                ExpDate = Helper.NullDateToString(detail.ExpDate),
                                LotNo = detail.LotNo,
                                UoM = detail.UoM,
                                QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                            };

                            data.Add(dat);
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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> AreaList()
        {
            List<Dictionary<string, string>> obj = new List<Dictionary<string, string>>();
            IEnumerable<BinRackArea> tempList = await db.BinRackAreas.Where(s => s.WarehouseCode.Equals("3001")).OrderBy(m => m.Code).ToListAsync();
            var list = tempList.Select(x => new BinRackAreaDTO()
            {
                ID = x.ID,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });

            return Ok(list);
        }

        [HttpPost]
        public async Task<IHttpActionResult> ListBarcodeSFGPrint(vListBarcodeSFG req)
        {
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

            if (DateTime.TryParse(inDate, out temp))
            {
                xInDate = Convert.ToDateTime(inDate);
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                xExpDate = Convert.ToDateTime(expDate);
            }

            var items = await db.vListBarcodeSFGs.Where(s => s.ProductCode.Equals(req.ProductCode) && s.LotNo.Equals(req.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).ToListAsync();
            IEnumerable<vListBarcodeSFGDTO> data = Enumerable.Empty<vListBarcodeSFGDTO>();

            if (items != null && items.Count() > 0)
            {

                data = from detail in items
                       select new vListBarcodeSFGDTO
                       {
                           StockCode = detail.StockCode,
                       };
            }

            return Ok(data);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetBarcode(string StockCode)
        {
            decimal totstockqty = 0;
            decimal QtyActualAvailable = 0;
            decimal QtyBagAvailable = 0;
            decimal QtyPerBag = 0;
            int count = 1;
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            try
            {
                IEnumerable<ReceivingSFGDetail> lista = Enumerable.Empty<ReceivingSFGDetail>();
                IQueryable<ReceivingSFGDetail> querya = db.ReceivingSFGDetails.Where(s => s.StockCode.Equals(StockCode)).OrderByDescending(s => s.ReceivedOn).AsQueryable();
                lista = querya.ToList();
                if (lista != null && lista.Count() > 0)
                {
                    foreach (ReceivingSFGDetail stocka in lista)
                    {
                        if (count == 1)
                        {
                            QtyActualAvailable = stocka.LastSeries * stocka.QtyPerBag;
                            QtyPerBag = stocka.QtyPerBag;
                            count++;
                        }
                    }
                }

                IEnumerable<StockSFG> list = Enumerable.Empty<StockSFG>();
                IQueryable<StockSFG> query = db.StockSFGs.Where(s => s.Code.Equals(StockCode)).OrderByDescending(s => s.InDate).AsQueryable();          
                list = query.ToList();
                if (list != null && list.Count() > 0)
                {
                    foreach (StockSFG stock in list)
                    {
                        if (stock.Quantity >= 0)
                        {
                            totstockqty = totstockqty + stock.Quantity;
                        }
                    }
                }

                QtyActualAvailable = QtyActualAvailable - totstockqty;
                QtyBagAvailable = QtyActualAvailable / QtyPerBag;

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

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("QtyActualAvailable", QtyActualAvailable);
            obj.Add("QtyBagAvailable", QtyBagAvailable);
            obj.Add("QtyPerBag", QtyPerBag);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetBarcodePrint(string StockCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            ReceivingSFGDetailDTO dataDTO = null;

            ReceivingSFGDetail data = await db.ReceivingSFGDetails.Where(s => s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();
            dataDTO = new ReceivingSFGDetailDTO
            {
                ID = data.ID,
                ProductCode = data.ProductCode,
                LotNo = data.LotNo,
                Barcode = data.StockCode,
                QtyActual = data.Qty,
                QtyPerBag = data.QtyPerBag,
                LastSeries = data.LastSeries,
                InDate = data.InDate
            };

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("LastSeriesBarcode", data.LastSeries);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> GetDataBarcode(vListBarcodeSFG receivingVM)
        {
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();

            var items = await db.vListBarcodeSFGs.Where(s => s.ProductCode.Equals(receivingVM.ProductCode) && s.LotNo.Equals(receivingVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).ToListAsync();
            IEnumerable<vListBarcodeSFGDTO> data = Enumerable.Empty<vListBarcodeSFGDTO>();

            if (items != null && items.Count() > 0)
            {

                data = from detail in items
                       select new vListBarcodeSFGDTO
                       {
                           StockCode = detail.StockCode,
                       };
            }

            return Ok(data);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Putaway(PutawaySFGVM putawayVM)
        {
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

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
                    if (DateTime.TryParse(inDate, out temp))
                    {
                        xInDate = Convert.ToDateTime(inDate);
                    }
                    if (DateTime.TryParse(expDate, out temp1))
                    {
                        xExpDate = Convert.ToDateTime(expDate);
                    }

                    if (string.IsNullOrEmpty(putawayVM.StockCode))
                    {
                        ModelState.AddModelError("Receiving.PutawayQTY", "Barcode is required.");
                    }

                    ReceivingSFGDetail receivingDetail = await db.ReceivingSFGDetails.Where(s => s.StockCode.Equals(putawayVM.StockCode) && s.ProductCode.Equals(putawayVM.ProductCode) && s.LotNo.Equals(putawayVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefaultAsync();
                    vReceivingSFG receiving = null;
                    if (receivingDetail == null)
                    {
                        ModelState.AddModelError("Receiving.AreaList", "Barcode is required.");
                    }
                    else
                    {
                        //check status already closed
                        receiving = db.vReceivingSFGs.Where(s => s.ProductCode.Equals(putawayVM.ProductCode) && s.LotNo.Equals(putawayVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefault();

                    }

                    if (string.IsNullOrEmpty(putawayVM.AreaList))
                    {
                        ModelState.AddModelError("Receiving.AreaList", "Area is required.");
                    }

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(putawayVM.BinRackID))
                    {
                        ModelState.AddModelError("Receiving.BinRackID", "BinRack is required.");
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.ID.Equals(putawayVM.BinRackID)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Receiving.BinRackID", "BinRack is not recognized.");
                        }
                    }

                    if (putawayVM.PutAwayQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        ReceivingSFGDetail data = await db.ReceivingSFGDetails.Where(s => s.StockCode.Equals(receivingDetail.StockCode)).FirstOrDefaultAsync();

                        if (putawayVM.PutAwayQty > (putawayVM.TotalAvailableQty / data.QtyPerBag))
                        {
                            ModelState.AddModelError("Receiving.PutawayQTY", "Bag Qty exceeded.");
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

                    PutawaySFG putaway = new PutawaySFG();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.ReceivingSFGDetailID = receivingDetail.ID;
                    putaway.PutawayMethod = "MANUAL";
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = putawayVM.PutAwayQty * receivingDetail.QtyPerBag;

                    db.PutawaySFGs.Add(putaway);

                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                    StockSFG stock = db.StockSFGs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.MaterialCode.Equals(receiving.ProductCode) && m.LotNumber.Equals(receiving.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(m.ExpiredDate) == DbFunctions.TruncateTime(xExpDate) && m.BinRackID.Equals(putaway.BinRackID)).FirstOrDefault();
                    if (stock != null)
                    {
                        stock.Quantity += putaway.PutawayQty;
                    }
                    else
                    {
                        stock = new StockSFG();
                        stock.ID = Helper.CreateGuid("S");
                        stock.MaterialCode = receiving.ProductCode;
                        stock.MaterialName = receiving.ProductName;
                        stock.Code = receivingDetail.StockCode;
                        stock.LotNumber = receiving.LotNo;
                        stock.InDate = receiving.InDate;
                        stock.ExpiredDate = receiving.ExpDate;
                        stock.Quantity = putawayVM.PutAwayQty * receivingDetail.QtyPerBag;
                        stock.QtyPerBag = receivingDetail.QtyPerBag;
                        stock.BinRackID = putaway.BinRackID;
                        stock.BinRackCode = putaway.BinRackCode;
                        stock.BinRackName = putaway.BinRackName;
                        stock.ReceivedAt = putaway.PutOn;

                        db.StockSFGs.Add(stock);
                    }


                    await db.SaveChangesAsync();

                    ////update receiving plan status if all quantity have been received and putaway
                    //Receiving rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();


                    //decimal totalReceive = rec.Qty;
                    //decimal totalPutaway = receivingDetail.Putaways.Sum(i => i.PutawayQty);

                    //if (totalReceive == totalPutaway)
                    //{
                    //    rec.TransactionStatus = "CLOSED";
                    //}


                    //await db.SaveChangesAsync();

                    //decimal availQty = receivingDetail.QtyActual - receivingDetail.PutawaySFGs.Sum(i => i.PutawayQty);
                    //int availBagQty = Convert.ToInt32(availQty / receivingDetail.QtyPerBag);

                    //obj.Add("availableTotalQty", availQty);
                    //obj.Add("availableBagQty", availBagQty);

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


        [HttpPost]
        public async Task<IHttpActionResult> Print(ReceiveSFGPrintReq req)
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
                    if (string.IsNullOrEmpty(req.StockCode))
                    {
                        ModelState.AddModelError("Receiving.BarcodeListPrint", "Barcode harus dipilih.");
                    }

                    ReceivingSFGDetail stk = db.ReceivingSFGDetails.Where(m => m.StockCode.Equals(req.StockCode)).FirstOrDefault();
                    if (stk == null)
                    {
                        ModelState.AddModelError("Receiving.PrintRawMateCode", "Material tidak dikenali.");
                    }

                    if (string.IsNullOrEmpty(req.Printer))
                    {
                        ModelState.AddModelError("Receiving.PrinterList", "Printer harus dipilih.");
                    }                                      

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(stk.ProductCode)).FirstOrDefault();
                    if (material == null)
                    {
                        ModelState.AddModelError("Receiving.PrintRawMateCode", "Material tidak dikenali.");
                    }

                    string Maker = "";

                    if (material.ProdType.Equals("RM"))
                    {
                        RawMaterial raw = db.RawMaterials.Where(m => m.MaterialCode.Equals(material.MaterialCode)).FirstOrDefault();
                        Maker = raw.Maker;
                    }

                    if (req.PrintQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.PrintQty", "Print Qty kosong atau kurang dari 1.");
                    }

                    if (req.PrintQty > req.LastSeriesBarcode)
                    {
                        ModelState.AddModelError("Receiving.PrintQty", "Print Qty melebihi total series barcode");
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

                    int seq = 0;
                    int len = 7;

                    if (material.MaterialCode.Length > 7)
                    {
                        len = material.MaterialCode.Length;
                    }

                    int startSeries = 0;
                    //ambil dari table nya langsung
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();

                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }

                    lastSeries = startSeries + req.PrintQty - 1;

                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < req.PrintQty; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = stk.ProductCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(stk.QtyPerBag).PadLeft(6) + " " + stk.LotNo;
                        dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = stk.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = stk.ExpDate.Value;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = stk.ProductCode.PadRight(len) + inDate + expiredDate;
                        dto.Field5 = stk.LotNo;
                        dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                        dto.Field7 = Maker;
                        dto.Field8 = material.MaterialName;
                        dto.Field9 = Helper.FormatThousand(stk.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = stk.ProductCode;
                        dto.Field13 = inDate3;
                        dto.Field14 = expiredDate2;
                        String body = RenderViewToString("Values", "~/Views/Receiving/Label.cshtml", dto);
                        bodies.Add(body);
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
                                iText.Kernel.Geom.Rectangle rectangle = new iText.Kernel.Geom.Rectangle(283.464566928f, 212.598425232f);
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

                    //update log print rm here
                    LogPrintRM logPrintRM = new LogPrintRM();
                    logPrintRM.ID = Helper.CreateGuid("LOG");
                    logPrintRM.Remarks = "Receiving SFG";
                    logPrintRM.StockCode = stk.StockCode;
                    logPrintRM.MaterialCode = stk.ProductCode;
                    logPrintRM.MaterialName = material.MaterialName;
                    logPrintRM.LotNumber = stk.LotNo;
                    logPrintRM.InDate = stk.InDate;
                    logPrintRM.ExpiredDate = stk.ExpDate;
                    logPrintRM.StartSeries = startSeries;
                    logPrintRM.LastSeries = lastSeries;
                    logPrintRM.PrintDate = DateTime.Now;

                    db.LogPrintRMs.Add(logPrintRM);

                    LogReprint reprint = new LogReprint();
                    reprint.ID = Helper.CreateGuid("LOG");
                    reprint.StockCode = stk.StockCode;
                    reprint.MaterialCode = stk.ProductCode;
                    reprint.MaterialName = material.MaterialName;
                    reprint.LotNumber = stk.LotNo;
                    reprint.InDate = stk.InDate;
                    reprint.ExpiredDate = stk.ExpDate;
                    reprint.StartSeries = startSeries;
                    reprint.LastSeries = lastSeries;
                    reprint.PrintDate = DateTime.Now;
                    reprint.PrintedBy = activeUser;
                    reprint.PrintQty = req.PrintQty;

                    db.LogReprints.Add(reprint);

                    await db.SaveChangesAsync();

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
            obj.Add("error_validation", customValidationMessages);

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
        public async Task<IHttpActionResult> DatatableJudgement()
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

            IEnumerable<ReceivingSFG> list = Enumerable.Empty<ReceivingSFG>();
            IEnumerable<ReceivingSFGDTO> pagedData = Enumerable.Empty<ReceivingSFGDTO>();



            //IQueryable<ReceivingSFG> query = db.ReceivingSFGs.Where(s => s.NGQty > 0 && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))).AsQueryable();
            IQueryable<ReceivingSFG> query = db.ReceivingSFGs.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.ProductCode.Contains(search)
                        || m.ProductName.Contains(search)
                        );

                Dictionary<string, Func<ReceivingSFG, object>> cols = new Dictionary<string, Func<ReceivingSFG, object>>();
                cols.Add("ProductCode", x => x.ProductCode);
                cols.Add("ProductName", x => x.ProductName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                //cols.Add("NGQty", x => x.NGQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from receiving in list
                                select new ReceivingSFGDTO
                                {
                                    ID = receiving.ID,
                                    ProductCode = receiving.ProductCode,
                                    ProductName = receiving.ProductName,
                                    Qty = Helper.FormatThousand(receiving.Qty),
                                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                                    UoM = receiving.UoM,
                                    LotNo = receiving.LotNo,
                                    InDate = Helper.NullDateToString2(receiving.InDate),
                                    ExpDate = Helper.NullDateToString2(receiving.ExpDate),
                                    //NGQty = Helper.FormatThousand(receiving.NGQty)
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
        public async Task<IHttpActionResult> PutawayMobile(PutawayMobileSFGVM putawayVM)
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

                    if (string.IsNullOrEmpty(putawayVM.Barcode))
                    {
                        throw new Exception("Barcode is required.");
                    }

                    string[] barcodeValues = putawayVM.Barcode.Split(';');

                    string StockCode = "";

                    //ReceivingSFGDetail receivingDetail = await db.ReceivingSFGDetails.Where(s => s.Barcode.Equals(StockCode)).FirstOrDefaultAsync();

                    //if (receivingDetail == null)
                    //{
                    //    throw new Exception("Receiving is not recognized.");
                    //}
                    //else
                    //{
                    //    //check status already closed
                    //}


                    if (putawayVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        //decimal availableQty = receivingDetail.QtyActual - receivingDetail.PutawaySFGs.Sum(i => i.PutawayQty);
                        //int availableBagQty = Convert.ToInt32(availableQty / receivingDetail.QtyPerBag);
                        //if (putawayVM.BagQty > availableBagQty)
                        //{
                        //    ModelState.AddModelError("Receiving.PutawayQTY", "Bag Qty exceeded.");
                        //}
                    }

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(putawayVM.BinRackCode))
                    {
                        throw new Exception("BinRack is required.");
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(putawayVM.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack is not recognized.");
                        }
                    }


                    //PutawaySFG putaway = new PutawaySFG();
                    //putaway.ID = Helper.CreateGuid("P");
                    //putaway.ReceivingSFGDetailID = receivingDetail.ID;
                    //putaway.PutawayMethod = "MANUAL";
                    //putaway.PutOn = DateTime.Now;
                    //putaway.PutBy = activeUser;
                    //putaway.BinRackID = binRack.ID;
                    //putaway.BinRackCode = binRack.Code;
                    //putaway.BinRackName = binRack.Name;
                    //putaway.PutawayQty = putawayVM.BagQty * receivingDetail.QtyPerBag;

                    //db.PutawaySFGs.Add(putaway);

                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                    //StockSFG stock = db.StockSFGs.Where(m => m.Code.Equals(receivingDetail.Barcode) && m.InDate.Equals(receivingDetail.ReceivingSFG.InDate.Date) && m.BinRackID.Equals(putaway.BinRackID)).FirstOrDefault();
                    //if (stock != null)
                    //{
                    //    stock.Quantity += putaway.PutawayQty;
                    //}
                    //else
                    //{
                    //    stock = new StockSFG();
                    //    stock.ID = Helper.CreateGuid("S");
                    //    stock.MaterialCode = receivingDetail.ReceivingSFG.ProductCode;
                    //    stock.MaterialName = receivingDetail.ReceivingSFG.ProductName;
                    //    stock.Code = receivingDetail.Barcode;
                    //    stock.LotNumber = receivingDetail.ReceivingSFG.LotNo;
                    //    stock.InDate = receivingDetail.ReceivingSFG.InDate;
                    //    stock.ExpiredDate = receivingDetail.ReceivingSFG.ExpDate;
                    //    stock.Quantity = putaway.PutawayQty;
                    //    stock.QtyPerBag = receivingDetail.QtyPerBag;
                    //    stock.BinRackID = putaway.BinRackID;
                    //    stock.BinRackCode = putaway.BinRackCode;
                    //    stock.BinRackName = putaway.BinRackName;
                    //    stock.ReceivedAt = putaway.PutOn;

                    //    db.StockSFGs.Add(stock);
                    //}


                    await db.SaveChangesAsync();

                    ////update receiving plan status if all quantity have been received and putaway
                    //Receiving rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();


                    //decimal totalReceive = rec.Qty;
                    //decimal totalPutaway = receivingDetail.Putaways.Sum(i => i.PutawayQty);

                    //if (totalReceive == totalPutaway)
                    //{
                    //    rec.TransactionStatus = "CLOSED";
                    //}


                    //await db.SaveChangesAsync();

                    //decimal availQty = receivingDetail.QtyActual - receivingDetail.PutawaySFGs.Sum(i => i.PutawayQty);
                    //int availBagQty = Convert.ToInt32(availQty / receivingDetail.QtyPerBag);

                    //obj.Add("availableTotalQty", availQty);
                    //obj.Add("availableBagQty", availBagQty);

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

            return Ok(obj);
        }
    }
}
