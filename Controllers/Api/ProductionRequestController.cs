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
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class ProductionRequestController : ApiController
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

            IEnumerable<ProductionRequestHeader> list = Enumerable.Empty<ProductionRequestHeader>();
            IEnumerable<ProductionRequestHeaderDTO> pagedData = Enumerable.Empty<ProductionRequestHeaderDTO>();

            IQueryable<ProductionRequestHeader> query = db.ProductionRequestHeaders.AsQueryable();

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.ProductionRequestHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

                recordsTotal = db.ProductionRequestHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).Count();
            }
            else
            {
                query = db.ProductionRequestHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();

                recordsTotal = db.ProductionRequestHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).Count();
            }

            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search) ||
                           m.IssuedNumber.Contains(search));

                Dictionary<string, Func<ProductionRequestHeader, object>> cols = new Dictionary<string, Func<ProductionRequestHeader, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("IssuedNumber", x => x.IssuedNumber);
                cols.Add("IssuedDate", x => x.IssuedDate);
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
                                select new ProductionRequestHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    IssuedNumber = x.IssuedNumber,
                                    IssuedDate = Helper.NullDateToString2(x.IssuedDate),
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
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
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<ProductionRequestDetail> list = Enumerable.Empty<ProductionRequestDetail>();
            IEnumerable<ProductionRequestDetailDTO> pagedData = Enumerable.Empty<ProductionRequestDetailDTO>();

            IQueryable<ProductionRequestDetail> query = db.ProductionRequestDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = db.ProductionRequestDetails.Count();
            int recordsFiltered = 0;

            try
            {
                list = await query.ToListAsync();

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new ProductionRequestDetailDTO
                                {
                                    ID = x.ID,
                                    HeaderID = x.HeaderID,
                                    OrderNumber = x.OrderNumber,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    CustomerCode = x.CustomerCode,
                                    CustomerName = x.CustomerName,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    ETA = Helper.NullDateToString2(x.ETA),
                                    ATA = Helper.NullDateToString2(x.ATA),
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
                                };
                }

                Dictionary<string, Func<ProductionRequestDetail, object>> cols = new Dictionary<string, Func<ProductionRequestDetail, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("OrderNumber", x => x.OrderNumber);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("CustomerCode", x => x.CustomerCode);
                cols.Add("CustomerName", x => x.CustomerName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("ETA", x => x.ETA);
                cols.Add("ATA", x => x.ATA);
                cols.Add("IsActive", x => x.IsActive);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

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
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ProductionRequestHeaderDTO productionRequestHeaderDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                ProductionRequestHeader productionRequestHeader = await db.ProductionRequestHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if(productionRequestHeader == null || productionRequestHeader.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }
                productionRequestHeaderDTO = new ProductionRequestHeaderDTO
                {
                    ID = productionRequestHeader.ID,
                    Code = productionRequestHeader.Code,
                    IssuedNumber = productionRequestHeader.IssuedNumber,
                    IssuedDate = Helper.NullDateToString2(productionRequestHeader.IssuedDate),
                    TransactionStatus = productionRequestHeader.TransactionStatus,
                    Details = new List<ProductionRequestDetailDTO>(),
                    CreatedBy = productionRequestHeader.CreatedBy,
                    CreatedOn = Helper.NullDateToString2(productionRequestHeader.CreatedOn),
                    ModifiedBy = productionRequestHeader.ModifiedBy != null ? productionRequestHeader.ModifiedBy : "",
                    ModifiedOn = Helper.NullDateToString2(productionRequestHeader.ModifiedOn)
                };

                foreach (ProductionRequestDetail detail in productionRequestHeader.ProductionRequestDetails.OrderBy(s => s.OrderNumber))
                {
                    ProductionRequestDetailDTO detailDTO = new ProductionRequestDetailDTO()
                    {
                        ID = detail.ID,
                        HeaderID = detail.HeaderID,
                        OrderNumber = detail.OrderNumber,
                        MaterialCode = detail.MaterialCode,
                        MaterialName = detail.MaterialName,
                        CustomerCode = detail.CustomerCode,
                        CustomerName = detail.CustomerName,
                        Qty = Helper.FormatThousand(detail.Qty),
                        ETA = detail.ETA.ToString("dd/MM/yyyy"),
                        ATA = detail.ATA.ToString() != null ? detail.ATA.ToString() : "",
                        IsActive = detail.IsActive,
                        CreatedBy = detail.CreatedBy,
                        CreatedOn = detail.CreatedOn.ToString(),
                        ModifiedBy = detail.ModifiedBy != null ? detail.ModifiedBy : "",
                        ModifiedOn = detail.ModifiedOn.ToString() != null ? detail.ModifiedOn.ToString() : "",
                        Remarks = detail.Remarks

                    };

                    productionRequestHeaderDTO.Details.Add(detailDTO);
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

            obj.Add("data", productionRequestHeaderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataDetailById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ProductionRequestDetailDTO productionRequestDetailDTO = null;

            try
            {
                ProductionRequestDetail productionRequestDetail = await db.ProductionRequestDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                productionRequestDetailDTO = new ProductionRequestDetailDTO
                {
                    ID = productionRequestDetail.ID,
                    HeaderID = productionRequestDetail.HeaderID,
                    OrderNumber = productionRequestDetail.OrderNumber,
                    MaterialCode = productionRequestDetail.MaterialCode,
                    MaterialName = productionRequestDetail.MaterialName,
                    CustomerCode = productionRequestDetail.CustomerCode,
                    CustomerName = productionRequestDetail.CustomerName,
                    Qty = productionRequestDetail.Qty.ToString(),
                    ETA = productionRequestDetail.ETA.ToString(),
                    ATA = productionRequestDetail.ATA.Value.ToString(),
                    IsActive = productionRequestDetail.IsActive,
                    CreatedBy = productionRequestDetail.CreatedBy,
                    CreatedOn = productionRequestDetail.CreatedOn.ToString(),
                    ModifiedBy = productionRequestDetail.ModifiedBy != null ? productionRequestDetail.ModifiedBy : "",
                    ModifiedOn = productionRequestDetail.ModifiedOn.ToString()
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

            obj.Add("data", productionRequestDetailDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> Create(ProductionRequestHeaderVM productionRequestHeaderVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
        //    Dictionary<int, object> error_details = new Dictionary<int, object>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;
        //    string orderNumber = null;
        //    string productionRequestID = null;

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
        //            if (string.IsNullOrEmpty(productionRequestHeaderVM.IssuedNumber))
        //            {
        //                ModelState.AddModelError("ProductionRequest.IssuedNumber", "Issued Number required.");
        //            }

        //            if (productionRequestHeaderVM.IssuedDate == null)
        //            {
        //                ModelState.AddModelError("ProductionRequest.IssuedDate", "Issued Date required.");
        //            }

        //            if (productionRequestHeaderVM.Details == null)
        //            {
        //                ModelState.AddModelError("ProductionRequest.Details", "PRF is required.");
        //            }
        //            else 
        //            {
        //                if (productionRequestHeaderVM.Details.Count == 0)
        //                {
        //                    ModelState.AddModelError("ProductionRequest.Details", "PRF can not be empty.");
        //                }
        //                else
        //                {
        //                    int i = 0;

        //                    foreach (ProductionRequestDetailVM productionRequestDetailVM in productionRequestHeaderVM.Details)
        //                    {
        //                        List<CustomValidationMessage> validationDetails = new List<CustomValidationMessage>();

        //                        if (string.IsNullOrEmpty(productionRequestDetailVM.OrderNumber))
        //                        {
        //                            validationDetails.Add(new CustomValidationMessage("OrderNumber", "Order Number is required."));
        //                        }

        //                        if (string.IsNullOrEmpty(productionRequestDetailVM.FGID))
        //                        {
        //                            validationDetails.Add(new CustomValidationMessage("FGID", "Finish Good is required."));
        //                        }
        //                        else
        //                        {
        //                            FinishGood fg = await db.FinishGoods.Where(s => s.ID.Equals(productionRequestDetailVM.FGID)).FirstOrDefaultAsync();
        //                            if (fg == null)
        //                            {
        //                                validationDetails.Add(new CustomValidationMessage("FGID", "Finish Good is not recognized."));
        //                            }
        //                        }

        //                        if (string.IsNullOrEmpty(productionRequestDetailVM.CustomerID))
        //                        {
        //                            validationDetails.Add(new CustomValidationMessage("CustomerID", "Customer is required."));
        //                        }
        //                        else
        //                        {
        //                            Customer customer = await db.Customers.Where(s => s.ID.Equals(productionRequestDetailVM.CustomerID)).FirstOrDefaultAsync();

        //                            if (customer == null)
        //                            {
        //                                validationDetails.Add(new CustomValidationMessage("CustomerID", "Customer is not recognized."));
        //                            }
        //                        }

        //                        if (productionRequestDetailVM.Qty <= 0)
        //                        {
        //                            validationDetails.Add(new CustomValidationMessage("Qty", "Qty is required."));
        //                        }

        //                        if (productionRequestDetailVM.ETA == null)
        //                        {
        //                            validationDetails.Add(new CustomValidationMessage("ETA", "ETA is required."));
        //                        }

        //                        if (validationDetails.Count() > 0)
        //                        {
        //                            error_details.Add(i, validationDetails);
        //                            ModelState.AddModelError("PurchaseRequest.Details", "Receiving Plan not validated.");
        //                        }

        //                        i++;
        //                    }
        //                }
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

        //            var CreatedAt = DateTime.Now;
        //            var TransactionId = Helper.CreateGuid("PRF");

        //            string prefix = TransactionId.Substring(0, 3);
        //            int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
        //            int month = CreatedAt.Month;
        //            string romanMonth = Helper.ConvertMonthToRoman(month);

        //            // get last number, and do increment.
        //            string lastNumber = db.ProductionRequestHeaders.AsQueryable().OrderByDescending(x => x.Code)
        //                .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
        //                .AsEnumerable().Select(x => x.Code).FirstOrDefault();
        //            int currentNumber = 0;

        //            if (!string.IsNullOrEmpty(lastNumber))
        //            {
        //                currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //            }

        //            string runningNumber = string.Format("{0:D3}", currentNumber + 1);

        //            productionRequestHeaderVM.Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

        //            ProductionRequestHeader productionRequestHeader = new ProductionRequestHeader
        //            {
        //                ID = TransactionId,
        //                Code = productionRequestHeaderVM.Code,
        //                IssuedNumber = productionRequestHeaderVM.IssuedNumber,
        //                IssuedDate = productionRequestHeaderVM.IssuedDate,
        //                TransactionStatus = "OPEN",
        //                CreatedBy = activeUser,
        //                CreatedOn = CreatedAt
        //            };

        //            foreach (ProductionRequestDetailVM productionRequestDetailVM in productionRequestHeaderVM.Details)
        //            {
        //                FinishGood fg = await db.FinishGoods.Where(s => s.ID.Equals(productionRequestDetailVM.FGID)).FirstOrDefaultAsync();
        //                Customer customer = await db.Customers.Where(s => s.ID.Equals(productionRequestDetailVM.CustomerID)).FirstOrDefaultAsync();

        //                productionRequestID = Helper.CreateGuid("PRFD");
        //                orderNumber = productionRequestDetailVM.OrderNumber;

        //                ProductionRequestDetail productionRequestDetail = new ProductionRequestDetail
        //                {
        //                    ID = productionRequestID,
        //                    HeaderID = TransactionId,
        //                    OrderNumber = productionRequestDetailVM.OrderNumber,
        //                    FGID = productionRequestDetailVM.FGID,
        //                    FGCode = fg.Code,
        //                    FGMaterialCode = fg.MaterialCode,
        //                    FGMaterialName = fg.MaterialName,
        //                    CustomerID = productionRequestDetailVM.CustomerID,
        //                    CustomerCode = customer.Code,
        //                    CustomerName = customer.Name,
        //                    Qty = productionRequestDetailVM.Qty,
        //                    ETA = productionRequestDetailVM.ETA,
        //                    Remarks = productionRequestDetailVM.Remarks,
        //                    IsActive = true,
        //                    CreatedBy = activeUser,
        //                    CreatedOn = CreatedAt,
        //                    UoM = "KG"
        //                };

        //                productionRequestHeader.ProductionRequestDetails.Add(productionRequestDetail);
        //            }

        //            db.ProductionRequestHeaders.Add(productionRequestHeader);
        //            await db.SaveChangesAsync();
        //            status = true;
        //            message = "Create data succeeded.";
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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

        //    obj.Add("productionRequestID", productionRequestID);
        //    obj.Add("orderNumber", orderNumber);
        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Update(ProductionRequestHeaderVM productionRequestHeaderVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
        //    Dictionary<int, object> error_details = new Dictionary<int, object>();

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
        //            ProductionRequestHeader productionRequestHeader = null;

        //            if (string.IsNullOrEmpty(productionRequestHeaderVM.ID))
        //            {
        //                ModelState.AddModelError("ProductionRequest.ID", "PRF ID is required.");
        //            } 
        //            else
        //            {
        //                productionRequestHeader = await db.ProductionRequestHeaders.Where(x => x.ID.Equals(productionRequestHeaderVM.ID)).FirstOrDefaultAsync();

        //                if (productionRequestHeader == null)
        //                {
        //                    ModelState.AddModelError("ProductionRequest.ID", "PRF ID is not recognized.");
        //                }
        //                else
        //                {
        //                    if (!productionRequestHeader.TransactionStatus.Equals("OPEN"))
        //                    {
        //                        ModelState.AddModelError("ProductionRequest.TransactionStatus", "PRF can no longer be edited.");
        //                    }
        //                }
        //            }

        //            if (string.IsNullOrEmpty(productionRequestHeaderVM.IssuedNumber))
        //            {
        //                ModelState.AddModelError("ProductionRequest.IssuedNumber", "Issued Number required.");
        //            }

        //            if (productionRequestHeaderVM.IssuedDate == null)
        //            {
        //                ModelState.AddModelError("ProductionRequest.IssuedDate", "Issued Date required.");
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


        //            productionRequestHeader.IssuedNumber = productionRequestHeaderVM.IssuedNumber;
        //            productionRequestHeader.IssuedDate = productionRequestHeaderVM.IssuedDate;
        //            productionRequestHeader.ModifiedBy = activeUser;
        //            productionRequestHeader.ModifiedOn = DateTime.Now;

        //            await db.SaveChangesAsync();
        //            status = true;
        //            message = "Create data succeeded.";
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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

        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(activeUser))
                {
                    throw new Exception("Token is no longer valid. Please re-login.");
                }
                ProductionRequestHeader productionRequestHeader = await db.ProductionRequestHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                bool isAllowed = false;

                if (productionRequestHeader.TransactionStatus.Equals("OPEN"))
                {
                    if(transactionStatus.Equals("CONFIRMED") || transactionStatus.Equals("CANCELLED"))
                    {
                        isAllowed = true;
                    }
                }

                if (!isAllowed)
                {
                    throw new Exception("Update status failed, status not recognized.");
                }
                

                productionRequestHeader.TransactionStatus = transactionStatus;


                if (productionRequestHeader.TransactionStatus.Equals("CONFIRMED"))
                {
                    //loop each order
                    //check in production plan, is there any order inside ? if yes, then delete order from production plan here
                    //check all remarks cancelled
                    //ProductionPlanOrder planOrder = await db.ProductionPlanOrders.Where(s => s.OrderNumber.Equals(orderNumber)).FirstOrDefaultAsync();
                    //if (planOrder != null)
                    //{

                    //}
                }

                productionRequestHeader.ModifiedBy = activeUser;
                productionRequestHeader.ModifiedOn = DateTime.Now;

                await db.SaveChangesAsync();
                status = true;

                if (transactionStatus.Equals("CONFIRMED"))
                {
                    message = "Data confirmation succeeded.";
                }
                else if (transactionStatus.Equals("CANCELLED"))
                {
                    message = "Data cancellation succeeded.";
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

        //[HttpPost]
        //public async Task<IHttpActionResult> UpdateDetail(ProductionRequestDetailVM productionRequestDetailVM)
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
        //            ProductionRequestDetail productionRequestDetail = await db.ProductionRequestDetails.Where(x => x.ID.Equals(productionRequestDetailVM.ID)).FirstOrDefaultAsync();

        //            if (productionRequestDetail != null)
        //            {
        //                Formula formula = await db.Formulae.Where(s => s.ID.Equals(productionRequestDetailVM.FormulaID)).FirstOrDefaultAsync();

        //                if (formula == null)
        //                {
        //                    ModelState.AddModelError("ProductionRequestDetail.FormulaID", "Formula does not exist.");
        //                }

        //                Customer customer = await db.Customers.Where(s => s.ID.Equals(productionRequestDetailVM.CustomerID)).FirstOrDefaultAsync();

        //                if (customer == null)
        //                {
        //                    ModelState.AddModelError("ProductionRequestDetail.CustomerID", "Customer does not exist.");
        //                }

        //                if (!ModelState.IsValid)
        //                {
        //                    foreach (var state in ModelState)
        //                    {
        //                        string field = state.Key.Split('.')[1];
        //                        string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                        customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                    }

        //                    throw new Exception("Input is not valid");
        //                }

        //                productionRequestDetail.ATA = productionRequestDetailVM.ATA;
        //                productionRequestDetail.Remarks = productionRequestDetailVM.Remarks;
        //                productionRequestDetail.IsActive = productionRequestDetailVM.IsActive;
        //                productionRequestDetail.ModifiedBy = activeUser;
        //                productionRequestDetail.ModifiedOn = DateTime.Now;

        //                db.ProductionRequestDetails.Add(productionRequestDetail);
        //                await db.SaveChangesAsync();
        //                status = true;
        //                message = "Create data succeeded.";
        //            }
        //            else
        //            {
        //                message = "Production Request Detail is no longer exist.";
        //            }
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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

        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> Upload()
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    HttpRequest request = HttpContext.Current.Request;

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;
        //    var TransactionId = "";

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
        //            if (request.Files.Count > 0)
        //            {
        //                HttpPostedFile file = request.Files[0];
        //                if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
        //                {
        //                    if (file.ContentLength < (10 * 1024 * 1024))
        //                    {

        //                        Stream stream = file.InputStream;
        //                        IExcelDataReader reader = null;
        //                        if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
        //                        {
        //                            reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

        //                        }
        //                        else
        //                        {
        //                            reader = ExcelReaderFactory.CreateBinaryReader(stream);
        //                        }

        //                        DataSet result = reader.AsDataSet();
        //                        reader.Close();

        //                        int totalSheet = result.Tables.Count;

        //                        for(int i = 0; i < totalSheet; i++)
        //                        {
        //                            DataTable dt = result.Tables[i];

        //                            string issuedDate = dt.Rows[1][6].ToString();
        //                            string issuedNumber = dt.Rows[2][6].ToString();

        //                            DateTime IssuedDate = new DateTime();

        //                            if (!string.IsNullOrEmpty(issuedDate))
        //                            {
        //                                try
        //                                {

        //                                    IssuedDate = Convert.ToDateTime(issuedDate);
        //                                }
        //                                catch (Exception)
        //                                {
        //                                    throw new Exception("Bad Issued Date Format.");
        //                                }
        //                            }
        //                            else
        //                            {
        //                                throw new Exception("Issued Date cannot empty.");
        //                            }

        //                            if (string.IsNullOrEmpty(issuedNumber))
        //                            {
        //                                throw new Exception("Issued Number cannot empty.");
        //                            }



        //                            var CreatedAt = DateTime.Now;

        //                            ProductionRequestHeader productionRequestHeader = await db.ProductionRequestHeaders.Where(s => s.IssuedNumber.Equals(issuedNumber) && !s.TransactionStatus.Equals("CANCELLED")).FirstOrDefaultAsync();
        //                            if (productionRequestHeader != null)
        //                            {
        //                                //unique
        //                                throw new Exception("Issued Number already exist. Please check your worksheet ");
        //                            }


        //                            TransactionId = Helper.CreateGuid("PRF");

        //                            string prefix = TransactionId.Substring(0, 3);
        //                            int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
        //                            int month = CreatedAt.Month;
        //                            string romanMonth = Helper.ConvertMonthToRoman(month);

        //                            // get last number, and do increment.
        //                            string lastNumber = db.ProductionRequestHeaders.AsQueryable().OrderByDescending(x => x.Code)
        //                                .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
        //                                .AsEnumerable().Select(x => x.Code).FirstOrDefault();
        //                            int currentNumber = 0;

        //                            if (!string.IsNullOrEmpty(lastNumber))
        //                            {
        //                                currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //                            }

        //                            string runningNumber = string.Format("{0:D3}", currentNumber + 1);

        //                            string code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

        //                            productionRequestHeader = new ProductionRequestHeader
        //                            {
        //                                ID = TransactionId,
        //                                Code = code,
        //                                IssuedNumber = issuedNumber,
        //                                IssuedDate = IssuedDate,
        //                                TransactionStatus = "OPEN",
        //                                CreatedBy = activeUser,
        //                                CreatedOn = CreatedAt
        //                            };

        //                            db.ProductionRequestHeaders.Add(productionRequestHeader);
        //                            await db.SaveChangesAsync();



        //                            foreach (DataRow row in dt.AsEnumerable().Skip(5))
        //                            {
        //                                string stockName = row[1].ToString();
        //                                string customerName = row[4].ToString();
        //                                FinishGood fg = await db.FinishGoods.Where(s => s.MaterialName.Equals(stockName)).FirstOrDefaultAsync();

        //                                Customer customer = await db.Customers.Where(s => s.Name.Equals(customerName)).OrderBy(m => m.Code).FirstOrDefaultAsync();

        //                                if (fg != null && customer != null)
        //                                {
        //                                    string productionRequestID = Helper.CreateGuid("PRFD");
        //                                    string orderNumber = row[0].ToString();
        //                                    string remark = row[8].ToString();
        //                                    //check OrderNumber already exist
        //                                    //if already exist, check remarks.
        //                                    decimal qty = Convert.ToDecimal(row[5].ToString());
        //                                    DateTime eta = Convert.ToDateTime(row[6].ToString());

        //                                    ProductionRequestDetail requestDetail = await db.ProductionRequestDetails.Where(s => s.OrderNumber.Equals(orderNumber)).FirstOrDefaultAsync();
        //                                    if (requestDetail != null && !requestDetail.ProductionRequestHeader.TransactionStatus.Equals("CANCELLED"))
        //                                    {
        //                                        if (requestDetail.Remarks.Equals("CANCELED"))
        //                                        {
        //                                            remark = requestDetail.Remarks;
        //                                        }
        //                                        else
        //                                        {
        //                                            if (remark.Equals("CANCELED"))
        //                                            {
        //                                                requestDetail.Remarks = remark;
        //                                            }
        //                                        }

        //                                    }


        //                                    ProductionRequestDetail productionRequestDetail = new ProductionRequestDetail
        //                                    {
        //                                        ID = productionRequestID,
        //                                        HeaderID = TransactionId,
        //                                        OrderNumber = orderNumber,
        //                                        FGID = fg.ID,
        //                                        FGCode = fg.Code,
        //                                        FGMaterialCode = fg.MaterialCode,
        //                                        FGMaterialName = fg.MaterialName,
        //                                        CustomerID = customer.ID,
        //                                        CustomerCode = customer.Code,
        //                                        CustomerName = customer.Name,
        //                                        Qty = qty,
        //                                        ETA = eta,
        //                                        Remarks = remark,
        //                                        IsActive = true,
        //                                        CreatedBy = activeUser,
        //                                        CreatedOn = CreatedAt,
        //                                        UoM = "KG"
        //                                    };

        //                                    productionRequestHeader.ProductionRequestDetails.Add(productionRequestDetail);
        //                                    await db.SaveChangesAsync();

        //                                }

        //                            }

        //                            if (productionRequestHeader.ProductionRequestDetails.Count() < 1)
        //                            {
        //                                throw new Exception("No new order submitted.");
        //                            }
        //                        }



        //                    }
        //                    else
        //                    {
        //                        message = "Upload failed. Maximum allowed file size : 10MB ";
        //                    }
        //                }
        //                else
        //                {
        //                    message = "Upload item failed. File is invalid.";
        //                }


        //                message = "Upload succeeded.";
        //                status = true;

        //            }
        //            else
        //            {
        //                message = "No file uploaded.";
        //            }
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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
        //        message = string.Format("Upload item failed. {0}", ex.Message);

        //        ProductionRequestHeader header = await db.ProductionRequestHeaders.Where(s => s.ID.Equals(TransactionId)).FirstOrDefaultAsync();
        //        if (header != null)
        //        {
        //            db.ProductionRequestHeaders.Remove(header);
        //            await db.SaveChangesAsync();
        //        }

        //    }

        //    //obj.Add("ID", TransactionId);
        //    obj.Add("status", status);
        //    obj.Add("message", message);


        //    return Ok(obj);
        //}


        [HttpPost]
        public async Task<IHttpActionResult> Upload()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            var TransactionId = "";

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
                    if (request.Files.Count > 0)
                    {
                        HttpPostedFile file = request.Files[0];
                        if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                        {
                            if (file.ContentLength < (10 * 1024 * 1024))
                            {

                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                int totalSheet = result.Tables.Count;

                                List<ProductionRequestHeader> productionRequestHeaders = new List<ProductionRequestHeader>();

                                for (int i = 0; i < totalSheet; i++)
                                {
                                    DataTable dt = result.Tables[i];

                                    string issuedDate = dt.Rows[1][6].ToString();
                                    string issuedNumber = dt.Rows[2][6].ToString();

                                    DateTime IssuedDate = new DateTime();

                                    if (!string.IsNullOrEmpty(issuedDate))
                                    {
                                        try
                                        {

                                            IssuedDate = Convert.ToDateTime(issuedDate);
                                        }
                                        catch (Exception)
                                        {
                                            throw new Exception("Bad Issued Date Format.");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Issued Date cannot empty.");
                                    }

                                    if (string.IsNullOrEmpty(issuedNumber))
                                    {
                                        throw new Exception("Issued Number cannot empty.");
                                    }



                                    var CreatedAt = DateTime.Now;

                                    ProductionRequestHeader productionRequestHeader = await db.ProductionRequestHeaders.Where(s => s.IssuedNumber.Equals(issuedNumber) && !s.TransactionStatus.Equals("CANCELLED")).FirstOrDefaultAsync();

                                    ProductionRequestHeader productionRequestHeader1 = productionRequestHeaders.Where(s => s.IssuedNumber.Equals(issuedNumber)).FirstOrDefault();

                                    if (productionRequestHeader != null || productionRequestHeader1 != null)
                                    {
                                        //unique
                                        //throw new Exception(string.Format("Issued Number already exist. Please check your worksheet sheet {0}", i + 1));
                                    }
                                    else
                                    {

                                        TransactionId = Helper.CreateGuid("PRF");



                                        productionRequestHeader = new ProductionRequestHeader
                                        {
                                            ID = TransactionId,
                                            IssuedNumber = issuedNumber,
                                            IssuedDate = IssuedDate,
                                            TransactionStatus = "OPEN",
                                            CreatedBy = activeUser,
                                            CreatedOn = CreatedAt
                                        };



                                        foreach (DataRow row in dt.AsEnumerable().Skip(5))
                                        {
                                            string stockName = row[1].ToString();
                                            string customerName = row[4].ToString();
                                            FinishGood fg = await db.FinishGoods.Where(s => s.MaterialName.Equals(stockName)).FirstOrDefaultAsync();

                                            Customer customer = await db.Customers.Where(s => s.Name.Equals(customerName)).OrderBy(m => m.Code).FirstOrDefaultAsync();

                                            if (fg != null && customer != null)
                                            {
                                                string productionRequestID = Helper.CreateGuid("PRFD");
                                                string orderNumber = row[0].ToString();
                                                string remark = row[8].ToString();
                                                //check OrderNumber already exist
                                                //if already exist, check remarks.
                                                decimal qty = Convert.ToDecimal(row[5].ToString());
                                                DateTime eta = Convert.ToDateTime(row[6].ToString());

                                                ProductionRequestDetail requestDetail = await db.ProductionRequestDetails.Where(s => s.OrderNumber.Equals(orderNumber)).FirstOrDefaultAsync();
                                                if (requestDetail != null && !requestDetail.ProductionRequestHeader.TransactionStatus.Equals("CANCELLED"))
                                                {
                                                    if (requestDetail.Remarks.Equals("CANCELED"))
                                                    {
                                                        remark = requestDetail.Remarks;
                                                    }
                                                    else
                                                    {
                                                        if (remark.Equals("CANCELED"))
                                                        {
                                                            requestDetail.Remarks = remark;
                                                        }
                                                    }

                                                }


                                                ProductionRequestDetail productionRequestDetail = new ProductionRequestDetail
                                                {
                                                    ID = productionRequestID,
                                                    HeaderID = TransactionId,
                                                    OrderNumber = orderNumber,
                                                    MaterialCode = fg.MaterialCode,
                                                    MaterialName = fg.MaterialName,
                                                    CustomerCode = customer.Code,
                                                    CustomerName = customer.Name,
                                                    Qty = qty,
                                                    ETA = eta,
                                                    Remarks = remark,
                                                    IsActive = true,
                                                    CreatedBy = activeUser,
                                                    CreatedOn = CreatedAt,
                                                    UoM = "KG"
                                                };

                                                productionRequestHeader.ProductionRequestDetails.Add(productionRequestDetail);

                                            }

                                        }

                                        productionRequestHeaders.Add(productionRequestHeader);

                                        if (productionRequestHeader.ProductionRequestDetails.Count() < 1)
                                        {
                                            //throw new Exception("No new order submitted.");
                                        }

                                    }
                                   
                                }


                                foreach(ProductionRequestHeader prf in productionRequestHeaders)
                                {
                                    var CreatedAt = DateTime.Now;
                                    string prefix = TransactionId.Substring(0, 3);
                                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                                    int month = CreatedAt.Month;
                                    string romanMonth = Helper.ConvertMonthToRoman(month);

                                    // get last number, and do increment.
                                    string lastNumber = db.ProductionRequestHeaders.AsQueryable().OrderByDescending(x => x.Code)
                                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    int currentNumber = 0;

                                    if (!string.IsNullOrEmpty(lastNumber))
                                    {
                                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                                    }

                                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                                    string code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);
                                    prf.Code = code;


                                    //auto confirmed
                                    prf.TransactionStatus = "CONFIRMED";
                                    prf.ModifiedBy = activeUser;
                                    prf.ModifiedOn = DateTime.Now;

                                    db.ProductionRequestHeaders.Add(prf);
                                    await db.SaveChangesAsync();
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


                        message = "Upload succeeded.";
                        status = true;

                    }
                    else
                    {
                        message = "No file uploaded.";
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
                message = string.Format("Upload item failed. {0}", ex.Message);
            }

            //obj.Add("ID", TransactionId);
            obj.Add("status", status);
            obj.Add("message", message);


            return Ok(obj);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
