using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers
{
    public class PurchaseOrderController : ApiController
    {
        //EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableHeader(string purchaseRequestHeaderID)
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

        //    IEnumerable<PurchaseOrderHeader> list = Enumerable.Empty<PurchaseOrderHeader>();
        //    IEnumerable<PurchaseOrderHeaderDTO> pagedData = Enumerable.Empty<PurchaseOrderHeaderDTO>();

        //    IQueryable<PurchaseOrderHeader> query = db.PurchaseOrderHeaders.Where(s => s.PurchaseRequestID.Equals(purchaseRequestHeaderID)).AsQueryable();

        //    int recordsTotal = db.PurchaseOrderHeaders.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        query = query
        //                .Where(m => m.PONumber.Contains(search));

        //        list = await query.ToListAsync();

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
                   
        //            pagedData = from x in list
        //                        select new PurchaseOrderHeaderDTO
        //                        {
        //                            ID = x.ID,
        //                            PONumber = x.PONumber,
        //                            PODate = Helper.NullDateToString2(x.PODate),
        //                            PurchaseRequestID = x.PurchaseRequestID,
        //                            SupplierID = x.SupplierID,
        //                            SupplierCode = x.SupplierCode,
        //                            SupplierName = x.SupplierName,
        //                            ETA = Helper.NullDateToString2(x.ETA),
        //                            TransactionStatus = x.TransactionStatus,
        //                            CreatedBy = x.CreatedBy,
        //                            CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
        //                            ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
        //                            ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
        //                        };
        //        }

        //        Dictionary<string, Func<PurchaseOrderHeader, object>> cols = new Dictionary<string, Func<PurchaseOrderHeader, object>>();
        //        cols.Add("PONumber", x => x.PONumber);
        //        cols.Add("PODate", x => x.PODate);
        //        cols.Add("SupplierCode", x => x.SupplierCode);
        //        cols.Add("SupplierName", x => x.SupplierName);
        //        cols.Add("TransactionStatus", x => x.TransactionStatus);
        //        cols.Add("CreatedBy", x => x.CreatedBy);
        //        cols.Add("CreatedOn", x => x.CreatedOn);
        //        cols.Add("ModifiedBy", x => x.ModifiedBy);
        //        cols.Add("ModifiedOn", x => x.ModifiedOn);

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

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

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableDetail(string HeaderID)
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

        //    IEnumerable<PurchaseOrderDetail> list = Enumerable.Empty<PurchaseOrderDetail>();
        //    IEnumerable<PurchaseOrderDetailDTO> pagedData = Enumerable.Empty<PurchaseOrderDetailDTO>();

        //    IQueryable<PurchaseOrderDetail> query = db.PurchaseOrderDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

        //    int recordsTotal = db.PurchaseOrderDetails.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        list = await query.ToListAsync();

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
                    
        //            pagedData = from x in list
        //                        select new PurchaseOrderDetailDTO
        //                        {
        //                            ID = x.ID,
        //                            HeaderID = x.HeaderID,
        //                            RawMaterialID = x.RawMaterialID,
        //                            MaterialCode = x.MaterialCode,
        //                            //QtyNeeded = Helper.FormatThousand(x.QtyNeeded),
        //                            OrderQty = Helper.FormatThousand(x.OrderQty)
        //                        };
        //        }

        //        Dictionary<string, Func<PurchaseOrderDetail, object>> cols = new Dictionary<string, Func<PurchaseOrderDetail, object>>();
        //        cols.Add("ID", x => x.ID);
        //        cols.Add("HeaderID", x => x.HeaderID);
        //        cols.Add("MaterialCode", x => x.MaterialCode);

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

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

        //[HttpGet]
        //public async Task<IHttpActionResult> GetDataById(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    PurchaseOrderHeaderDTO purchaseOrderHeaderDTO = null;

        //    try
        //    {
        //        PurchaseOrderHeader purchaseOrderHeader = await db.PurchaseOrderHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
        //        purchaseOrderHeaderDTO = new PurchaseOrderHeaderDTO
        //        {
        //            ID = purchaseOrderHeader.ID,
        //            PONumber = purchaseOrderHeader.PONumber,
        //            PODate = Helper.NullDateToString2(purchaseOrderHeader.PODate),
        //            SupplierID = purchaseOrderHeader.SupplierID,
        //            SupplierCode = purchaseOrderHeader.SupplierCode,
        //            SupplierName = purchaseOrderHeader.SupplierName,
        //            ETA = Helper.NullDateToString2(purchaseOrderHeader.ETA),
        //            Details = new List<PurchaseOrderDetailDTO>(),
        //            TransactionStatus = purchaseOrderHeader.TransactionStatus,
        //            CreatedBy = purchaseOrderHeader.CreatedBy,
        //            CreatedOn = Helper.NullDateTimeToString(purchaseOrderHeader.CreatedOn),
        //            ModifiedBy = purchaseOrderHeader.ModifiedBy != null ? purchaseOrderHeader.ModifiedBy : "",
        //            ModifiedOn = Helper.NullDateTimeToString(purchaseOrderHeader.ModifiedOn)
        //        };

        //        foreach (PurchaseOrderDetail detail in purchaseOrderHeader.PurchaseOrderDetails)
        //        {
        //            decimal outstanding = detail.OrderQty;
        //            PurchaseOrderDetailDTO detailDTO = new PurchaseOrderDetailDTO()
        //            {
        //                ID = detail.ID,
        //                HeaderID = detail.HeaderID,
        //                RawMaterialID = detail.RawMaterialID,
        //                MaterialName = detail.RawMaterial.Name,
        //                MaterialCode = detail.MaterialCode,
        //                OrderQty = Helper.FormatThousand(detail.OrderQty),
        //                FullbagQty = Helper.FormatThousand(Convert.ToInt32(detail.RawMaterial.Qty))
        //            };

        //            foreach (PurchaseOrderHeader orderHeader in purchaseOrderHeader.PurchaseRequestHeader.PurchaseOrderHeaders)
        //            {
        //                foreach (PurchaseOrderDetail orderDetail in orderHeader.PurchaseOrderDetails)
        //                {
        //                    if (orderDetail.RawMaterialID.Equals(detailDTO.RawMaterialID))
        //                    {
        //                        outstanding = outstanding - orderDetail.OrderQty > 0 ? outstanding - orderDetail.OrderQty : 0;
        //                    }
        //                }
        //            }

        //            detailDTO.RequestedQty = Helper.FormatThousand(purchaseOrderHeader.PurchaseRequestHeader.PurchaseRequestDetails
        //                .Where(s => s.RawMaterialID.Equals(detail.RawMaterialID)).Select(s => s.OrderQty).FirstOrDefault());
        //            detailDTO.OutstandingQty = Helper.FormatThousand(outstanding);
        //            detailDTO.Fullbag = Helper.FormatThousand(Convert.ToInt32(Math.Floor(detail.OrderQty / detail.RawMaterial.Qty)));
        //            detailDTO.RemainderQty = Helper.FormatThousand(detail.OrderQty % detail.RawMaterial.Qty);

        //            purchaseOrderHeaderDTO.Details.Add(detailDTO);
        //        }

        //        status = true;
        //        message = "Fetch data succeded.";
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

        //    obj.Add("data", purchaseOrderHeaderDTO);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Create(PurchaseOrderHeaderVM purchaseOrderHeaderVM)
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
        //            var CreatedAt = DateTime.Now;
        //            var TransactionId = Helper.CreateGuid("PO");

        //            string prefix = TransactionId.Substring(0, 2);
        //            DateTime datetime = Convert.ToDateTime(purchaseOrderHeaderVM.PODate);

        //            int year = Convert.ToInt32(datetime.Year.ToString().Substring(2));
        //            int month = datetime.Month;
        //            string romanMonth = Helper.ConvertMonthToRoman(month);

        //            // get last number, and do increment.
        //            string lastNumber = db.PurchaseOrderHeaders.AsQueryable().OrderByDescending(x => x.PONumber)
        //                .Where(x => x.CreatedOn.Year.Equals(datetime.Year) && x.CreatedOn.Month.Equals(datetime.Month))
        //                .AsEnumerable().Select(x => x.PONumber).FirstOrDefault();
        //            int currentNumber = 0;

        //            if (!string.IsNullOrEmpty(lastNumber))
        //            {
        //                currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //            }

        //            string runningNumber = string.Format("{0:D3}", currentNumber + 1);

        //            purchaseOrderHeaderVM.PONumber = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);
        //            Supplier supplier = await db.Suppliers.Where(s => s.ID.Equals(purchaseOrderHeaderVM.SupplierID)).FirstOrDefaultAsync();

        //            PurchaseOrderHeader purchaseOrderHeader = new PurchaseOrderHeader
        //            {
        //                ID = TransactionId,
        //                PONumber = purchaseOrderHeaderVM.PONumber,
        //                PODate = purchaseOrderHeaderVM.PODate,
        //                PurchaseRequestID = purchaseOrderHeaderVM.PurchaseRequestID,
        //                SupplierID = purchaseOrderHeaderVM.SupplierID,
        //                SupplierCode = supplier.Code,
        //                SupplierName = supplier.Name,
        //                ETA = purchaseOrderHeaderVM.ETA,
        //                TransactionStatus = "OPEN",
        //                CreatedBy = activeUser,
        //                CreatedOn = CreatedAt
        //            };

        //            foreach (PurchaseOrderDetailVM detail in purchaseOrderHeaderVM.Details)
        //            {
        //                if (detail.OrderQty > 0)
        //                {
        //                    PurchaseOrderDetail purchaseOrderDetail = new PurchaseOrderDetail()
        //                    {
        //                        ID = Helper.CreateGuid("POD"),
        //                        HeaderID = purchaseOrderHeader.ID,
        //                        RawMaterialID = detail.RawMaterialID,
        //                        MaterialCode = detail.MaterialCode,
        //                        OrderQty = detail.OrderQty,
        //                        QtyNeeded = detail.QtyNeeded
        //                    };

        //                    purchaseOrderHeader.PurchaseOrderDetails.Add(purchaseOrderDetail);
        //                }
        //            }

        //            //if (purchaseOrderHeader.PurchaseRequestHeader.TransactionStatus.Equals("OPEN"))
        //            //    purchaseOrderHeader.PurchaseRequestHeader.TransactionStatus = "PROGRESS";

        //            db.PurchaseOrderHeaders.Add(purchaseOrderHeader);
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

        //public async Task<IHttpActionResult> CheckStatus(string PurchaseRequestID)
        //{
        //    bool status = false;

        //    PurchaseRequestHeader purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(x => x.ID.Equals(PurchaseRequestID)).FirstOrDefaultAsync();
        //    List<RequestOrderQty> listRequestOrderQty = await db.RequestOrderQties.Where(s => s.PurchaseRequestID.Equals(PurchaseRequestID)).ToListAsync();

        //    if (listRequestOrderQty.Count > 0)
        //    {
        //        status = true;
        //        // Check Qty
        //        foreach (RequestOrderQty requestOrderQty in listRequestOrderQty)
        //        {
        //            if (requestOrderQty.OutstandingQty > 0)
        //            {
        //                status = false;
        //            }
        //        }

        //        if (status)
        //        {
        //            // Check Order Status
        //            foreach (PurchaseOrderHeader purchaseOrderHeader in purchaseRequestHeader.PurchaseOrderHeaders)
        //            {
        //                if (!purchaseOrderHeader.TransactionStatus.Equals("APPROVED"))
        //                {
        //                    status = false;
        //                }
        //            }

        //            // Edit Request Status
        //            if (status)
        //            {
        //                purchaseRequestHeader.TransactionStatus = "CLOSED";
        //                await db.SaveChangesAsync();
        //            }
        //        }
        //    }

        //    return Ok();
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> UpdateStatus(string PurchaseRequestID, string PurchaseOrderID, string transactionStatus)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
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
        //            PurchaseOrderHeader purchaseOrderHeader = await db.PurchaseOrderHeaders.Where(x => x.ID.Equals(PurchaseOrderID)).FirstOrDefaultAsync();
        //            purchaseOrderHeader.TransactionStatus = transactionStatus;

        //            //if (transactionStatus.Equals("APPROVED"))
        //            //{
        //            //    await CheckStatus(PurchaseRequestID);

        //            //    //create receiving plan
        //            //    var CreatedAt = DateTime.Now;
        //            //    var TransactionId = Helper.CreateGuid("RW");

        //            //    string prefix = TransactionId.Substring(0, 2);
        //            //    DateTime datetime = Convert.ToDateTime(CreatedAt);

        //            //    int year = Convert.ToInt32(datetime.Year.ToString().Substring(2));
        //            //    int month = datetime.Month;
        //            //    string romanMonth = Helper.ConvertMonthToRoman(month);

        //            //    // get last number, and do increment.
        //            //    string lastNumber = db.ReceivingHeaders.AsQueryable().OrderByDescending(x => x.Code)
        //            //        .Where(x => x.CreatedOn.Year.Equals(datetime.Year) && x.CreatedOn.Month.Equals(datetime.Month))
        //            //        .AsEnumerable().Select(x => x.PONumber).FirstOrDefault();
        //            //    int currentNumber = 0;

        //            //    if (!string.IsNullOrEmpty(lastNumber))
        //            //    {
        //            //        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //            //    }

        //            //    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

        //            //    string Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

        //            //    ReceivingHeader receivingHeader = new ReceivingHeader
        //            //    {
        //            //        ID = TransactionId,
        //            //        Code = Code,
        //            //        PONumber = purchaseOrderHeader.PONumber,
        //            //        PurchaseOrderId = purchaseOrderHeader.ID,
        //            //        TransactionStatus = "OPEN",
        //            //        CreatedBy = activeUser,
        //            //        CreatedOn = CreatedAt
        //            //    };

        //            //    foreach (PurchaseOrderDetail detail in purchaseOrderHeader.PurchaseOrderDetails)
        //            //    {
        //            //        if (detail.OrderQty > 0)
        //            //        {
        //            //            ReceivingDetail receivingDetail = new ReceivingDetail()
        //            //            {
        //            //                ID = Helper.CreateGuid("RWD"),
        //            //                HeaderID = purchaseOrderHeader.ID,
        //            //                RawMaterialID = detail.RawMaterialID,
        //            //                MaterialCode = detail.MaterialCode,
        //            //                MaterialName = detail.RawMaterial.Name,
        //            //                PlannedQty = detail.OrderQty
        //            //            };

        //            //            receivingHeader.ReceivingDetails.Add(receivingDetail);
        //            //        }
        //            //    }

        //            //    db.ReceivingHeaders.Add(receivingHeader);
        //            //}

        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Purchase Order " + Helper.ToLower(transactionStatus) + ".";
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
        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Cancel(PurchaseOrderStatusVM purchaseOrderStatusVM)
        //{
        //    return await UpdateStatus(purchaseOrderStatusVM.PurchaseRequestID, purchaseOrderStatusVM.PurchaseOrderID, "CANCELLED");
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Approve(PurchaseOrderStatusVM purchaseOrderStatusVM)
        //{
        //    return await UpdateStatus(purchaseOrderStatusVM.PurchaseRequestID, purchaseOrderStatusVM.PurchaseOrderID, "APPROVED");
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Close(PurchaseOrderStatusVM purchaseOrderStatusVM)
        //{
        //    return await UpdateStatus(purchaseOrderStatusVM.PurchaseRequestID, purchaseOrderStatusVM.PurchaseOrderID, "CLOSED");
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Progress(PurchaseOrderStatusVM purchaseOrderStatusVM)
        //{
        //    return await UpdateStatus(purchaseOrderStatusVM.PurchaseRequestID, purchaseOrderStatusVM.PurchaseOrderID, "PROGRESS");
        //}
    }
}
