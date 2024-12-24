using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;
using System.Drawing;
using ZXing;
using ZXing.QrCode;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Web.Routing;
using iText.Kernel.Pdf;
using System.Configuration;
using iText.Kernel.Utils;
using NPOI.Util;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Html2pdf;

namespace WMS_BE.Controllers.Api
{
    public class TransformController : ApiController
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

            IEnumerable<Transform> list = Enumerable.Empty<Transform>();
            IEnumerable<TransformHeaderDTO> pagedData = Enumerable.Empty<TransformHeaderDTO>();

            IQueryable<Transform> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.Transforms.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.Transforms.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else if (transactionStatus.Equals("OPEN/PROGRESS"))
            {
                query = db.Transforms.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
            }
            else
            {
                query = db.Transforms.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.MaterialCodeTarget.Contains(search)
                        || m.MaterialNameTarget.Contains(search)
                        );

                Dictionary<string, Func<Transform, object>> cols = new Dictionary<string, Func<Transform, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("MaterialCodeTarget", x => x.MaterialCodeTarget);
                cols.Add("MaterialNameTarget", x => x.MaterialNameTarget);
                cols.Add("MaterialTypeTarget", x => x.MaterialTypeTarget);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("TransactionStatus", x => x.TransactionStatus);


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new TransformHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.MaterialType,
                                    MaterialCodeTarget = x.MaterialCodeTarget,
                                    MaterialNameTarget = x.MaterialNameTarget,
                                    MaterialTypeTarget = x.MaterialTypeTarget,
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
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
        public async Task<IHttpActionResult> DatatableDetails(string HeaderID)
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

            IEnumerable<Transform> list = Enumerable.Empty<Transform>();
            IEnumerable<TransformHeaderDTO> pagedData = Enumerable.Empty<TransformHeaderDTO>();

            IQueryable<Transform> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(HeaderID))
            {
                query = db.Transforms.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.Transforms.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else
            {
                query = db.Transforms.Where(s => s.ID.Equals(HeaderID)).AsQueryable();
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {

                Dictionary<string, Func<Transform, object>> cols = new Dictionary<string, Func<Transform, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("MaterialCodeTarget", x => x.MaterialCodeTarget);
                cols.Add("MaterialNameTarget", x => x.MaterialNameTarget);
                cols.Add("MaterialTypeTarget", x => x.MaterialTypeTarget);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new TransformHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.MaterialType,
                                    MaterialCodeTarget = x.MaterialCodeTarget,
                                    MaterialNameTarget = x.MaterialNameTarget,
                                    MaterialTypeTarget = x.MaterialTypeTarget,
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
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
        public async Task<IHttpActionResult> Create(TransformHeaderVM dataVM)
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
                    vStockProduct product = null;
                    vStockProduct productTarget = null;
                    if (string.IsNullOrEmpty(dataVM.MaterialCode))
                    {
                        ModelState.AddModelError("Transform.MaterialCode", "Source Material Code is required.");
                    }
                    else
                    {
                        product = db.vStockProducts.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode) && m.TotalQty > 0).FirstOrDefault();
                        if (product == null)
                        {
                            throw new Exception("Source Material Code not found.");
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.MaterialCodeTarget))
                    {
                        ModelState.AddModelError("Transform.MaterialCodeTarget", "Target Material Code is required.");
                    }
                    else
                    {
                        productTarget = db.vStockProducts.Where(m => m.MaterialCode.Equals(dataVM.MaterialCodeTarget)).FirstOrDefault();
                        if (productTarget == null)
                        {
                            throw new Exception("Target Material Code not found.");
                        }
                    }

                    if (dataVM.TotalQty <= 0)
                    {
                        ModelState.AddModelError("Transform.QtyTransform", "Transform Qty is required.");
                    }
                    else
                    {
                        //validation available qty
                        if (dataVM.TotalQty > product.TotalQty)
                        {
                            ModelState.AddModelError("Transform.QtyTransform", string.Format("Transform Qty exceeded. Available Qty : {0}", product.TotalQty));
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

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("TRF");

                    string prefix = TransactionId.Substring(0, 3);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.Transforms.AsQueryable().OrderByDescending(x => x.Code)
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

                    Transform header = new Transform
                    {
                        ID = TransactionId,
                        Code = Code,
                        MaterialCode = product.MaterialCode,
                        MaterialName = product.MaterialName,
                        MaterialType = product.ProdType,
                        MaterialCodeTarget = productTarget.MaterialCode,
                        MaterialNameTarget = productTarget.MaterialName,
                        MaterialTypeTarget = productTarget.ProdType,
                        TotalQty = dataVM.TotalQty,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                    };

                    id = header.ID;

                    db.Transforms.Add(header);

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
            TransformHeaderDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                Transform header = await db.Transforms.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null || header.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new TransformHeaderDTO
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    MaterialType = header.MaterialType,
                    MaterialCodeTarget = header.MaterialCodeTarget,
                    MaterialNameTarget = header.MaterialNameTarget,
                    MaterialTypeTarget = header.MaterialTypeTarget,
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = header.CreatedOn.ToString(),
                    TotalQty = Helper.FormatThousand(header.TotalQty)
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
        public async Task<IHttpActionResult> DatatableProduct(string type)
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
            IEnumerable<MaterialInfo> pagedData = Enumerable.Empty<MaterialInfo>();
                      
            IQueryable<vStockProduct> query = db.vStockProducts.AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            query = query.Where(m => warehouses.Contains(m.WarehouseCode));

            if (type.Equals("source"))
            {
                query = query.Where(m => m.TotalQty > 0);
            }
            //query = query.GroupBy(g => g.MaterialCode);
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
                                select new MaterialInfo
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
        public async Task<IHttpActionResult> DatatableVProductStock(string type)
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

            IEnumerable<vProductStock> list = Enumerable.Empty<vProductStock>();
            IEnumerable<MaterialInfo> pagedData = Enumerable.Empty<MaterialInfo>();

            IQueryable<vProductStock> query = db.vProductStocks.AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            
            if (type.Equals("source"))
            {
                query = query.Where(m => m.TotalQty > 0);
            }
            
            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vProductStock, object>> cols = new Dictionary<string, Func<vProductStock, object>>();
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
                                select new MaterialInfo
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
        public async Task<IHttpActionResult> DatatableProductMaster()
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
            IEnumerable<MaterialMaster> pagedData = Enumerable.Empty<MaterialMaster>();

            IQueryable<vProductMaster> query = db.vProductMasters.AsQueryable();

            query = query.Where(m => m.QtyPerBag > 0);

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
                cols.Add("QtyPerBag", x => x.QtyPerBag);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new MaterialMaster
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.ProdType,
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag)
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
                    Transform header = await db.Transforms.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

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

                    if (transactionStatus.Equals("CANCELLED"))
                    {
                        message = "Cancel data succeeded.";
                    }

                    if (transactionStatus.Equals("CLOSED"))
                    {
                        //check detail
                        if (header.TransformDetails.Count() < 1)
                        {
                            throw new Exception("Picking can not be empty.");
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
        public async Task<IHttpActionResult> Picking(StockTransformPickingWebReq req)
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
                    if (string.IsNullOrEmpty(req.HeaderId))
                    {
                        throw new Exception("Header Id is required.");
                    }

                    Transform trf = db.Transforms.Where(m => m.ID.ToString().Equals(req.HeaderId)).FirstOrDefault();

                    if (trf == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Data tidak ditemukan."));
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Material tidak dikenali."));
                    }

                    vProductMaster vProductMasterTarget = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCodeTarget)).FirstOrDefaultAsync();
                    if (vProductMasterTarget == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Material tidak dikenali."));
                    }

                    string StockCode = "";

                    if (vProductMaster == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Material tidak dikenali."));
                    }

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("BinRack harus diisi."));

                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Transform.BagQtyE", string.Format("BinRack tidak ditemukan."));
                        }

                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.ID.Equals(req.StockId) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Stock tidak ditemukan."));
                    }

                    //restriction 1 : AREA TYPE
                    User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
                    string userAreaType = userData.AreaType;

                    string materialAreaType = stockAll.BinRackAreaType;

                    if (!userAreaType.Equals(materialAreaType))
                    {
                        throw new Exception(string.Format("FIFO Restriction, tidak dapat mengambil material di area {0}", materialAreaType));
                    }


                    string[] warehouseCodes = { };
                    warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();

                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(trf.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType) && warehouseCodes.Contains(s.WarehouseCode))
                       .OrderByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                       .ThenBy(s => s.InDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();
                    //.ThenBy(s => s.Quantity).FirstOrDefault();

                    if (stkAll == null)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Stock tidak tersedia."));
                    }

                    //restriction 2 : REMAINDER QTY
                    if (stockAll.QtyPerBag > stkAll.QtyPerBag)
                    {
                        throw new Exception(string.Format("FIFO Restriction, harus mengambil material dengan keterangan = LotNo : {0} & Qty/Bag : {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag), stkAll.BinRackCode));
                    }

                    //restriction 3 : IN DATE
                    if (stockAll.InDate.Value.Date > stkAll.InDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, harus mengambil material dengan keterangan = LotNo : {0} & In Date: {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.NullDateToString(stkAll.InDate), stkAll.BinRackCode));
                    }

                    //restriction 4 : EXPIRED DATE
                    if (DateTime.Now.Date >= stkAll.ExpiredDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, harus melakukan QC Inspection untuk material dengan keterangan = LotNo : {0} & Qty/Bag : {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag), stkAll.BinRackCode));
                    }

                    if (req.Qty <= 0)
                    {
                        ModelState.AddModelError("Transform.BagQtyE", string.Format("Qty tidak boleh kosong atau tidak boleh 0."));
                    }
                    else
                    {
                        decimal Qty = stockAll.Quantity;

                        if (req.Qty > Qty)
                        {
                            ModelState.AddModelError("Transform.BagQtyE", string.Format("Bag Qty Transform. Available : {0}", Helper.FormatThousand(Qty)));
                        }
                        else
                        {
                            decimal requestedQty = trf.TotalQty;
                            decimal pickedQty = trf.TransformDetails.Where(m => m.MaterialCode.Equals(vProductMasterTarget.MaterialCode)).Sum(i => i.Qty);
                            decimal availableQty = requestedQty - pickedQty;

                            if (req.Qty > availableQty)
                            {
                                ModelState.AddModelError("Transform.BagQtyE", string.Format("Bag Qty Transform. Available : {0}", Helper.FormatThousand(availableQty)));
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

                    //check current stock
                    decimal curQty = stockAll.Quantity - req.Qty;
                    decimal remainderQty = curQty % stockAll.QtyPerBag;
                    decimal curBagQty = Convert.ToInt32(Math.Floor(curQty / stockAll.QtyPerBag));
                    decimal curTotalQty = curBagQty * stockAll.QtyPerBag;
                    //check remainder

                    //reduce current stock
                    if (stockAll.Type.Equals("RM"))
                    {
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity = curTotalQty;

                        if (remainderQty > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMaster.MaterialCode, Helper.FormatThousand(remainderQty), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockRM stk = db.StockRMs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += remainderQty;
                            }
                            else
                            {
                                stk = new StockRM();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMaster.MaterialCode;
                                stk.MaterialName = vProductMaster.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = remainderQty;
                                stk.QtyPerBag = remainderQty;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockRMs.Add(stk);
                            }

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries + 1;
                            }
                            lastSeries = startSeries;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = stockAll.MaterialCode;
                            detail.MaterialName = stockAll.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = remainderQty;
                            detail.QtyPerBag = remainderQty;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = stockAll.MaterialCode;
                            logPrintRM.MaterialName = stockAll.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }

                        //new stock
                        //qty per bag based on target
                        //check target stock
                        decimal remainderQtyTarget = req.Qty % vProductMasterTarget.QtyPerBag;
                        int curBagQtyTarget = Convert.ToInt32(Math.Floor(req.Qty / vProductMasterTarget.QtyPerBag));

                        if (curBagQtyTarget > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMasterTarget.MaterialCode, Helper.FormatThousand(vProductMasterTarget.QtyPerBag), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockRM stk = db.StockRMs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                            }
                            else
                            {
                                stk = new StockRM();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMasterTarget.MaterialCode;
                                stk.MaterialName = vProductMasterTarget.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                                stk.QtyPerBag = vProductMasterTarget.QtyPerBag;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockRMs.Add(stk);
                            }

                            int startSeries = 0;                            
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;                                
                            }
                            else
                            {
                                startSeries = lastSeries + 1;
                            }
                            lastSeries = startSeries + curBagQtyTarget - 1;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = vProductMasterTarget.MaterialCode;
                            detail.MaterialName = vProductMasterTarget.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                            detail.QtyPerBag = vProductMasterTarget.QtyPerBag;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = vProductMasterTarget.MaterialCode;
                            logPrintRM.MaterialName = vProductMasterTarget.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }

                        if (remainderQtyTarget > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMasterTarget.MaterialCode, Helper.FormatThousand(remainderQtyTarget), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockRM stk = db.StockRMs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += remainderQtyTarget;
                            }
                            else
                            {
                                stk = new StockRM();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMasterTarget.MaterialCode;
                                stk.MaterialName = vProductMasterTarget.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = remainderQtyTarget;
                                stk.QtyPerBag = remainderQtyTarget;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockRMs.Add(stk);
                            }

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries + 1;
                            }
                            lastSeries = startSeries;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = vProductMasterTarget.MaterialCode;
                            detail.MaterialName = vProductMasterTarget.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = remainderQtyTarget;
                            detail.QtyPerBag = remainderQtyTarget;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = vProductMasterTarget.MaterialCode;
                            logPrintRM.MaterialName = vProductMasterTarget.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity = curTotalQty;

                        if (remainderQty > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMaster.MaterialCode, Helper.FormatThousand(remainderQty), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockSFG stk = db.StockSFGs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += remainderQty;
                            }
                            else
                            {
                                stk = new StockSFG();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMaster.MaterialCode;
                                stk.MaterialName = vProductMaster.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = remainderQty;
                                stk.QtyPerBag = remainderQty;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockSFGs.Add(stk);
                            }

                            //new material, print barcode

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries;
                            }
                            lastSeries = startSeries + 1;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = stockAll.MaterialCode;
                            detail.MaterialName = stockAll.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = remainderQty;
                            detail.QtyPerBag = remainderQty;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = stockAll.MaterialCode;
                            logPrintRM.MaterialName = stockAll.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }

                        //new stock target
                        //qty per bag based on target
                        //check target stock
                        decimal remainderQtyTarget = req.Qty % vProductMasterTarget.QtyPerBag;
                        int curBagQtyTarget = Convert.ToInt32(Math.Floor(req.Qty / vProductMasterTarget.QtyPerBag));

                        if (curBagQtyTarget > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMasterTarget.MaterialCode, Helper.FormatThousand(vProductMasterTarget.QtyPerBag), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockSFG stk = db.StockSFGs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                            }
                            else
                            {
                                stk = new StockSFG();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMasterTarget.MaterialCode;
                                stk.MaterialName = vProductMasterTarget.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                                stk.QtyPerBag = vProductMasterTarget.QtyPerBag;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockSFGs.Add(stk);
                            }

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries + 1;
                            }
                            lastSeries = startSeries + curBagQtyTarget - 1;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = vProductMasterTarget.MaterialCode;
                            detail.MaterialName = vProductMasterTarget.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = curBagQtyTarget * vProductMasterTarget.QtyPerBag;
                            detail.QtyPerBag = vProductMasterTarget.QtyPerBag;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = vProductMasterTarget.MaterialCode;
                            logPrintRM.MaterialName = vProductMasterTarget.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }

                        if (remainderQtyTarget > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMasterTarget.MaterialCode, Helper.FormatThousand(remainderQtyTarget), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockSFG stk = db.StockSFGs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if (stk != null)
                            {
                                stk.Quantity += remainderQtyTarget;
                            }
                            else
                            {
                                stk = new StockSFG();
                                stk.ID = Helper.CreateGuid("S");
                                stk.MaterialCode = vProductMasterTarget.MaterialCode;
                                stk.MaterialName = vProductMasterTarget.MaterialName;
                                stk.Code = stkCode;
                                stk.LotNumber = stockAll.LotNumber;
                                stk.InDate = stockAll.InDate;
                                stk.ExpiredDate = stockAll.ExpiredDate;
                                stk.Quantity = remainderQtyTarget;
                                stk.QtyPerBag = remainderQtyTarget;
                                stk.BinRackID = binRack.ID;
                                stk.BinRackCode = stockAll.BinRackCode;
                                stk.BinRackName = stockAll.BinRackName;
                                stk.ReceivedAt = DateTime.Now;

                                db.StockSFGs.Add(stk);
                            }

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries + 1;
                            }
                            lastSeries = startSeries;

                            TransformDetail detail = new TransformDetail();
                            detail.ID = Helper.CreateGuid("TRFd");
                            detail.TransformID = trf.ID;
                            detail.MaterialCode = vProductMasterTarget.MaterialCode;
                            detail.MaterialName = vProductMasterTarget.MaterialName;
                            detail.StockCode = stkCode;
                            detail.LotNo = stockAll.LotNumber;
                            detail.InDate = stockAll.InDate.Value;
                            detail.ExpDate = stockAll.ExpiredDate.Value;
                            detail.Qty = remainderQtyTarget;
                            detail.QtyPerBag = remainderQtyTarget;
                            detail.TransformMethod = "SCAN";
                            detail.BinRackCode = stockAll.BinRackCode;
                            detail.BinRackName = stockAll.BinRackName;
                            detail.LastSeries = lastSeries;

                            db.TransformDetails.Add(detail);

                            LogPrintRM logPrintRM = new LogPrintRM();
                            logPrintRM.ID = Helper.CreateGuid("LOG");
                            logPrintRM.Remarks = "Stock Transform";
                            logPrintRM.StockCode = stkCode;
                            logPrintRM.MaterialCode = vProductMasterTarget.MaterialCode;
                            logPrintRM.MaterialName = vProductMasterTarget.MaterialName;
                            logPrintRM.LotNumber = stockAll.LotNumber;
                            logPrintRM.InDate = stockAll.InDate.Value;
                            logPrintRM.ExpiredDate = stockAll.ExpiredDate;
                            logPrintRM.StartSeries = startSeries;
                            logPrintRM.LastSeries = lastSeries;
                            logPrintRM.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM);
                        }
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Picking berhasil.";
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

            if (string.IsNullOrEmpty(HeaderID))
            {
                throw new Exception("Header Id is required");
            }
            Transform trf = db.Transforms.Where(m => m.ID.ToString().Equals(HeaderID)).FirstOrDefault();
            if (trf == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            IEnumerable<TransformPickingStockDTO> pagedData = Enumerable.Empty<TransformPickingStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(trf.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            decimal requestedQty = trf.TotalQty;
            decimal pickedQty = trf.TransformDetails.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).Sum(i => i.Qty);
            decimal availableQty = requestedQty - pickedQty;

            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;


            try
            {
                query = query.OrderByDescending(s => s.BinRackAreaType)
                        .ThenByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                        .ThenBy(s => s.InDate)
                        .ThenBy(s => s.QtyPerBag)
                        .ThenBy(s => s.Quantity);
                Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNumber);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpiredDate);
                cols.Add("BagQty", x => x.BagQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("WarehouseName", x => x.WarehouseName);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("BinRackAreaName", x => x.BinRackAreaName);
                cols.Add("Quantity", x => x.Quantity);
                cols.Add("TotalQty", x => x.BagQty * x.QtyPerBag);
                //cols.Add("PickedOn", x => x.PickedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new TransformPickingStockDTO
                                {
                                    TransformID = trf.ID,
                                    ID = x.ID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    StockCode = x.Code,
                                    LotNo = x.LotNumber,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                    BagQty = Helper.FormatThousand(x.BagQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                    //PickingMethod = x.PickingMethod,

                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    BinRackAreaCode = x.BinRackAreaCode,
                                    BinRackAreaName = x.BinRackAreaName,
                                    WarehouseCode = x.WarehouseCode,
                                    WarehouseName = x.WarehouseName,
                                    IsExpired = DateTime.Now.Date >= x.ExpiredDate.Value.Date,
                                    QCInspected = x.OnInspect,
                                    OutstandingQty = Helper.FormatThousand(trf.TotalQty - (trf.TransformDetails.Sum(m => m.Qty * m.QtyPerBag))),
                                    OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((trf.TotalQty - (trf.TransformDetails.Sum(i => i.Qty * i.QtyPerBag))) / x.QtyPerBag))),

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
            obj.Add("outstanding_qty", availableQty);
            obj.Add("picked_bag_qty", pickedQty);
            obj.Add("material_type", MaterialType);
            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
        [HttpPost]
        public async Task<IHttpActionResult> DatatableTransformDetail(string HeaderID)
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

            if (string.IsNullOrEmpty(HeaderID))
            {
                throw new Exception("Header Id is required");
            }
            Transform trf = db.Transforms.Where(m => m.ID.Equals(HeaderID)).FirstOrDefault();
            if (trf == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }
            IEnumerable<TransformDetail> list = Enumerable.Empty<TransformDetail>();
            IEnumerable<StockTransformDetailResp> pagedData = Enumerable.Empty<StockTransformDetailResp>();

            IQueryable<TransformDetail> query = db.TransformDetails.Where(m => m.TransformID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            decimal requestedQty = trf.TotalQty;
            decimal pickedQty = trf.TransformDetails.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).Sum(i => i.Qty);
            decimal availableQty = requestedQty - pickedQty;

            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;

            try
            {
                query = query
                       .Where(m => m.MaterialCode.Contains(search)
                       || m.MaterialName.Contains(search)
                       );

                Dictionary<string, Func<TransformDetail, object>> cols = new Dictionary<string, Func<TransformDetail, object>>();
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("BagQty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQtyX", x => Convert.ToInt32(Math.Ceiling(x.Qty / x.QtyPerBag)));
                cols.Add("PrintedAt", x => x.PrintedAt);
                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new StockTransformDetailResp
                                {
                                    ID = x.ID,
                                    TransformID = x.TransformID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    StockCode = x.StockCode,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    BagQty = Helper.FormatThousand(x.Qty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQtyX = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling(x.Qty / x.QtyPerBag))),
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    LastSeries = x.LastSeries.ToString(),
                                    PrintedAt = Helper.NullDateTimeToString(x.PrintedAt),
                                    PrintedBy = x.PrintedBy,
                                    PrintBarcodeAction = string.IsNullOrEmpty(x.PrintedBy) && x.LastSeries > 0,
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
        public async Task<IHttpActionResult> Print(StockTransformPrintReq req)
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

                    TransformDetail detail = await db.TransformDetails.Where(s => s.ID.Equals(req.TransformDetailId)).FirstOrDefaultAsync();

                    if (detail == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(detail.MaterialCode)).FirstOrDefault();
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

                    int fullBag = Convert.ToInt32(detail.Qty / detail.QtyPerBag);
                    int lastSeries = detail.LastSeries - fullBag;

                    //get last series
                    seq = Convert.ToInt32(lastSeries + 1);

                    List<string> bodies = new List<string>();

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = detail.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(detail.QtyPerBag).PadLeft(6) + " " + detail.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);

                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = detail.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = detail.ExpDate;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");

                        string qr2 = detail.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = detail.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = detail.MaterialName;
                        dto.Field9 = Helper.FormatThousand(detail.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = detail.MaterialCode;
                        dto.Field13 = inDate3;
                        dto.Field14 = expiredDate2;
                        dto.Field15 = runningNumber;
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

                    detail.PrintedAt = DateTime.Now;
                    detail.PrintedBy = activeUser;
                    db.SaveChanges();
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

            Transform transform = db.Transforms.Where(m => m.ID.ToString().Equals(OrderId)).FirstOrDefault();

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(transform.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            query = query.Where(m => warehouses.Contains(m.WarehouseCode));
            int totalRow = query.Count();

            decimal requestedQty = transform.TotalQty;
            decimal pickedQty = transform.TransformDetails.Where(x => x.Transform.MaterialCodeTarget == x.MaterialCode).Sum(i => i.Qty);
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
                            if (DateTime.Now.Date < stock.ExpiredDate.Value.Date)
                            {
                                searchQty += stock.Quantity;
                            }

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
            obj.Add("outstandingQty", availableQty);
            //obj.Add("outstandingBagQty", )

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> DatatableDetailTransform()
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

            IEnumerable<vTransform> list = Enumerable.Empty<vTransform>();
            IEnumerable<TransformDTOReport> pagedData = Enumerable.Empty<TransformDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vTransform> query;

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                query = db.vTransforms.Where(s => DbFunctions.TruncateTime(s.PutawayOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.PutawayOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouseCode));
            }
            else
            {               
                query = db.vTransforms.Where(s => DbFunctions.TruncateTime(s.PutawayOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.PutawayOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.SourceRMCode.Contains(search)
                        || m.SourceRMName.Contains(search)
                        );

                Dictionary<string, Func<vTransform, object>> cols = new Dictionary<string, Func<vTransform, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("SourceRMCode", x => x.SourceRMCode);
                cols.Add("SourceRMName", x => x.SourceRMName);
                cols.Add("TransformQty", x => x.TransformQty);
                cols.Add("TargetRMCode", x => x.TargetRMCode);
                cols.Add("TargetRMName", x => x.TargetRMName);
                cols.Add("SourceBinRack", x => x.SourceBinRack);
                cols.Add("SourceInDate", x => x.SourceInDate);
                cols.Add("SourceExpDate", x => x.SourceExpDate);
                cols.Add("SourceLotNo", x => x.SourceLotNo);
                cols.Add("SourceBag", x => x.SourceBag);
                cols.Add("SourceFullBag", x => x.SourceFullBag);
                cols.Add("SourceTotal", x => x.SourceTotal);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("TargetBinRack", x => x.TargetBinRack);
                cols.Add("TargetInDate", x => x.TargetInDate);
                cols.Add("TargetExpDate", x => x.TargetExpDate);
                cols.Add("TargetLotNo", x => x.TargetLotNo);
                cols.Add("TargetBag", x => x.TargetBag);
                cols.Add("TargetFullBag", x => x.TargetFullBag);
                cols.Add("TargetTotal", x => x.TargetTotal);
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
                                select new TransformDTOReport
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    SourceRMCode = detail.SourceRMCode,
                                    SourceRMName = detail.SourceRMName,
                                    TransformQty = Helper.FormatThousand(Convert.ToInt32(detail.TransformQty)),
                                    TargetRMCode = detail.TargetRMCode,
                                    TargetRMName = detail.TargetRMName,
                                    SourceBinRack = detail.SourceBinRack,
                                    SourceInDate = Helper.NullDateToString2(detail.SourceInDate),
                                    SourceExpDate = Helper.NullDateToString2(detail.SourceExpDate),
                                    SourceLotNo = detail.SourceLotNo != null ? detail.SourceLotNo : "",
                                    SourceBag = Helper.FormatThousand(detail.SourceBag),
                                    SourceFullBag = Helper.FormatThousand(detail.SourceFullBag),
                                    SourceTotal = Helper.FormatThousand(detail.SourceTotal),
                                    PickingBy = detail.PickingBy,
                                    PickingOn = Convert.ToDateTime(detail.PickingOn),
                                    TargetBinRack = detail.TargetBinRack,
                                    TargetInDate = Helper.NullDateToString2(detail.TargetInDate),
                                    TargetExpDate = Helper.NullDateToString2(detail.TargetExpDate),
                                    TargetLotNo = detail.TargetLotNo != null ? detail.TargetLotNo : "",
                                    TargetBag = Helper.FormatThousand(detail.TargetBag),
                                    TargetFullBag = Helper.FormatThousand(detail.TargetFullBag),
                                    TargetTotal = Helper.FormatThousand(detail.TargetTotal),
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
        public async Task<IHttpActionResult> GetDataReportTransform(string date, string enddate, string warehouse)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate) && string.IsNullOrEmpty(warehouse))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vTransform> list = Enumerable.Empty<vTransform>();
            IEnumerable<TransformDTOReport> pagedData = Enumerable.Empty<TransformDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vTransform> query;

            if (!string.IsNullOrEmpty(warehouse))
            {
                query = db.vTransforms.Where(s => DbFunctions.TruncateTime(s.PutawayOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.PutawayOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouse));
            }
            else
            {
                query = db.vTransforms.Where(s => DbFunctions.TruncateTime(s.PutawayOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.PutawayOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vTransform, object>> cols = new Dictionary<string, Func<vTransform, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("SourceRMCode", x => x.SourceRMCode);
                cols.Add("SourceRMName", x => x.SourceRMName);
                cols.Add("TransformQty", x => x.TransformQty);
                cols.Add("TargetRMCode", x => x.TargetRMCode);
                cols.Add("TargetRMName", x => x.TargetRMName);
                cols.Add("SourceBinRack", x => x.SourceBinRack);
                cols.Add("SourceInDate", x => x.SourceInDate);
                cols.Add("SourceExpDate", x => x.SourceExpDate);
                cols.Add("SourceLotNo", x => x.SourceLotNo);
                cols.Add("SourceBag", x => x.SourceBag);
                cols.Add("SourceFullBag", x => x.SourceFullBag);
                cols.Add("SourceTotal", x => x.SourceTotal);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("TargetBinRack", x => x.TargetBinRack);
                cols.Add("TargetInDate", x => x.TargetInDate);
                cols.Add("TargetExpDate", x => x.TargetExpDate);
                cols.Add("TargetLotNo", x => x.TargetLotNo);
                cols.Add("TargetBag", x => x.TargetBag);
                cols.Add("TargetFullBag", x => x.TargetFullBag);
                cols.Add("TargetTotal", x => x.TargetTotal);
                cols.Add("PutawayBy", x => x.PutawayBy);
                cols.Add("PutawayOn", x => x.PutawayOn);
                cols.Add("Status", x => x.Status);
                cols.Add("Memo", x => x.Memo);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new TransformDTOReport
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    SourceRMCode = detail.SourceRMCode,
                                    SourceRMName = detail.SourceRMName,
                                    TransformQty = Helper.FormatThousand(Convert.ToInt32(detail.TransformQty)),
                                    TargetRMCode = detail.TargetRMCode,
                                    TargetRMName = detail.TargetRMName,
                                    SourceBinRack = detail.SourceBinRack,
                                    SourceInDate = Helper.NullDateToString2(detail.SourceInDate),
                                    SourceExpDate = Helper.NullDateToString2(detail.SourceExpDate),
                                    SourceLotNo = detail.SourceLotNo != null ? detail.SourceLotNo : "",
                                    SourceBag = Helper.FormatThousand(detail.SourceBag),
                                    SourceFullBag = Helper.FormatThousand(detail.SourceFullBag),
                                    SourceTotal = Helper.FormatThousand(detail.SourceTotal),
                                    PickingBy = detail.PickingBy,
                                    PickingOn = Convert.ToDateTime(detail.PickingOn),
                                    TargetBinRack = detail.TargetBinRack,
                                    TargetInDate = Helper.NullDateToString2(detail.TargetInDate),
                                    TargetExpDate = Helper.NullDateToString2(detail.TargetExpDate),
                                    TargetLotNo = detail.TargetLotNo != null ? detail.TargetLotNo : "",
                                    TargetBag = Helper.FormatThousand(detail.TargetBag),
                                    TargetFullBag = Helper.FormatThousand(detail.TargetFullBag),
                                    TargetTotal = Helper.FormatThousand(detail.TargetTotal),
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
