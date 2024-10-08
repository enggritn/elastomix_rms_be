using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class MaterialRequestController : ApiController
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

            IEnumerable<PurchaseRequestHeader> list = Enumerable.Empty<PurchaseRequestHeader>();
            IEnumerable<PurchaseRequestHeaderDTO> pagedData = Enumerable.Empty<PurchaseRequestHeaderDTO>();

            IQueryable<PurchaseRequestHeader> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.PurchaseRequestHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();

            }
            else if (transactionStatus.Equals("CONFIRMED"))
            {
                query = db.PurchaseRequestHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
            }
            else
            {
                query = db.PurchaseRequestHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
            }

            recordsTotal = query.Count();

            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.RefNumber.Contains(search)
                        || m.SourceType.Contains(search)
                        || m.SourceCode.Contains(search)
                        || m.SourceName.Contains(search)
                        || m.SourceAddress.Contains(search)
                        || m.DestinationCode.Contains(search)
                        || m.DestinationName.Contains(search)
                        || m.DestinationType.Contains(search)
                        //|| m.TransactionStatus.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<PurchaseRequestHeader, object>> cols = new Dictionary<string, Func<PurchaseRequestHeader, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("RefNumber", x => x.RefNumber);
                cols.Add("SourceType", x => x.SourceType);
                cols.Add("SourceCode", x => x.SourceCode);
                cols.Add("SourceName", x => x.SourceName);
                cols.Add("SourceAddress", x => x.SourceName);
                cols.Add("DestinationCode", x => x.DestinationCode);
                cols.Add("DestinationName", x => x.DestinationName);
                cols.Add("DestinationType", x => x.DestinationName);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("TruckType", x => x.TruckType);
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
                                select new PurchaseRequestHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    RefNumber = x.RefNumber,
                                    SourceType = x.SourceType,
                                    SourceCode = x.SourceCode,
                                    SourceName = x.SourceName,
                                    SourceAddress = x.SourceAddress,
                                    DestinationCode = x.DestinationCode,
                                    DestinationName = x.DestinationName,
                                    DestinationType = x.DestinationType,
                                    TransactionStatus = x.TransactionStatus,
                                    TruckType = x.TruckType,
                                    CreatedBy = x.CreatedBy,
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

            IEnumerable<PurchaseRequestDetail> list = Enumerable.Empty<PurchaseRequestDetail>();
            IEnumerable<PurchaseRequestDetailDTO> pagedData = Enumerable.Empty<PurchaseRequestDetailDTO>();

            IQueryable<PurchaseRequestDetail> query = db.PurchaseRequestDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = db.PurchaseRequestDetails.Where(s => s.HeaderID.Equals(HeaderID)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        //|| m.Qty.Contains(search)
                        //|| m.ETA.Contains(search)
                        );

                Dictionary<string, Func<PurchaseRequestDetail, object>> cols = new Dictionary<string, Func<PurchaseRequestDetail, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Qty", x => x.MoQ > 0 ? x.Qty / x.MoQ : x.Qty / x.QtyPerBag);
                cols.Add("QtyBag", x => x.Qty / x.QtyPerBag);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("TotalQty", x => x.Qty);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ETA", x => x.ETA);
                cols.Add("MoQ", x => x.MoQ);
                cols.Add("Packaging", x => x.Packaging);
                cols.Add("Remarks", x => x.Remarks);
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
                                select new PurchaseRequestDetailDTO
                                {

                                    ID = x.ID,
                                    HeaderID = x.HeaderID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    Packaging = x.Packaging.ToString(),
                                    Qty = Helper.FormatThousand(x.MoQ > 0 ? Convert.ToInt32(x.Qty / x.MoQ) : Convert.ToInt32(x.Qty / x.QtyPerBag)),
                                    TotalQty = Helper.FormatThousand(x.Qty),
                                    MoQ = Helper.FormatThousand(x.MoQ),
                                    QtyBag = Helper.FormatThousand(Convert.ToInt32(x.Qty / x.QtyPerBag)),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    ETA = Helper.NullDateToString2(x.ETA),
                                    UoM = x.UoM,
                                    Remarks = x.Remarks,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    EditButtonAction = !x.PurchaseRequestHeader.SourceType.Equals("OUTSOURCE") && x.PurchaseRequestHeader.TransactionStatus == "CONFIRMED"

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
        public async Task<IHttpActionResult> DatatableRawMaterial()
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

            IEnumerable<vRawMaterialConvert> list = Enumerable.Empty<vRawMaterialConvert>();
            IEnumerable<RawMaterialDTO> pagedData = Enumerable.Empty<RawMaterialDTO>();

            IQueryable<vRawMaterialConvert> query = db.vRawMaterialConverts.AsQueryable();

            int recordsTotal = db.vRawMaterialConverts.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vRawMaterialConvert, object>> cols = new Dictionary<string, Func<vRawMaterialConvert, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ShelfLife", x => x.ShelfLife);
                cols.Add("MinPurchaseQty", x => x.MinPurchaseQty);
                cols.Add("Maker", x => x.Maker);
                cols.Add("Vendor", x => x.Vendor);
                cols.Add("PoRate", x => x.PoRate);
                cols.Add("ManfCd", x => x.ManfCd);
                cols.Add("VendorCode", x => x.VendorCode);
                cols.Add("IsActive", x => x.IsActive);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);
                cols.Add("IsConvertible", x => x.IsConvertible);
                cols.Add("MinPurchaseQtyLitre", x => x.MinPurchaseQtyLitre);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();


                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new RawMaterialDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    UoM = x.UoM,
                                    ShelfLife = x.ShelfLife,
                                    MinPurchaseQty = Helper.FormatThousand(x.MinPurchaseQty),
                                    Maker = x.Maker,
                                    Vendor = x.Vendor,
                                    PoRate = Helper.FormatThousand(x.PoRate),
                                    ManfCd = x.ManfCd,
                                    VendorCode = x.VendorCode,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = x.ModifiedOn.ToString(),
                                    IsConvertible = x.IsConvertible.HasValue ? x.IsConvertible.Value : false,
                                    MinPurchaseQtyLitre = Helper.FormatThousand(x.MinPurchaseQtyLitre.HasValue ? x.MinPurchaseQtyLitre.Value : 0)
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
        public async Task<IHttpActionResult> DatatableStockWarehouse(string warehouseCode)
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

            IEnumerable<vStockWarehouse> list = Enumerable.Empty<vStockWarehouse>();
            IEnumerable<ActualStockDTO> pagedData = Enumerable.Empty<ActualStockDTO>();

            Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(warehouseCode)).FirstOrDefaultAsync();
            string[] warehouseCodes = { };
            if (!wh.Type.Equals("EMIX"))
            {
                warehouseCodes = new string[1] { warehouseCode };
            }
            else
            {
                warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
            }

            IQueryable<vStockWarehouse> query = db.vStockWarehouses.Where(m => warehouseCodes.Contains(m.WarehouseCode)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vStockWarehouse, object>> cols = new Dictionary<string, Func<vStockWarehouse, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("TotalQuantity", x => x.TotalQuantity);
                cols.Add("MaterialType", x => x.Type);


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
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    MaterialType = x.Type,
                                    TotalQty = Helper.FormatThousand(x.TotalQuantity),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(x.TotalBagQty),
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
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            PurchaseRequestHeaderDTO purchaseRequestHeaderDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                PurchaseRequestHeader purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (purchaseRequestHeader == null || purchaseRequestHeader.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }

                purchaseRequestHeaderDTO = new PurchaseRequestHeaderDTO
                {
                    ID = purchaseRequestHeader.ID,
                    Code = purchaseRequestHeader.Code,
                    RefNumber = purchaseRequestHeader.RefNumber,
                    SourceType = purchaseRequestHeader.SourceType,
                    SourceCode = purchaseRequestHeader.SourceCode,
                    SourceName = purchaseRequestHeader.SourceName,
                    SourceAddress = purchaseRequestHeader.SourceAddress,
                    DestinationCode = purchaseRequestHeader.DestinationCode,
                    DestinationName = purchaseRequestHeader.DestinationName,
                    DestinationType = purchaseRequestHeader.DestinationType,
                    TruckType = purchaseRequestHeader.TruckType,
                    TransactionStatus = purchaseRequestHeader.TransactionStatus,
                    //Details = new List<PurchaseRequestDetailDTO>(),
                    CreatedBy = purchaseRequestHeader.CreatedBy,
                    CreatedOn = purchaseRequestHeader.CreatedOn.ToString(),
                    ModifiedBy = purchaseRequestHeader.ModifiedBy != null ? purchaseRequestHeader.ModifiedBy : "",
                    ModifiedOn = purchaseRequestHeader.ModifiedOn.ToString()
                };

                string deliveryDate = purchaseRequestHeader.PurchaseRequestDetails.Select(m => m.ETA).FirstOrDefault().ToString();
                purchaseRequestHeaderDTO.DeliveryDate = Convert.ToDateTime(deliveryDate).ToString("dd/MM/yyyy");

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

            obj.Add("data", purchaseRequestHeaderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> ExcelDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            PurchaseRequestHeaderDTO purchaseRequestHeaderDTO = null;

            try
            {
                PurchaseRequestHeader purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(m => m.ID.Equals(id)
                    && !m.TransactionStatus.Equals("CANCELLED")
                    ).FirstOrDefaultAsync();
                purchaseRequestHeaderDTO = new PurchaseRequestHeaderDTO
                {
                    ID = purchaseRequestHeader.ID,
                    Code = purchaseRequestHeader.Code,
                    RefNumber = purchaseRequestHeader.RefNumber,
                    SourceType = purchaseRequestHeader.SourceType,
                    SourceCode = purchaseRequestHeader.SourceCode,
                    SourceName = purchaseRequestHeader.SourceName,
                    SourceAddress = purchaseRequestHeader.SourceAddress,
                    DestinationCode = purchaseRequestHeader.DestinationCode,
                    DestinationName = purchaseRequestHeader.DestinationName,
                    DestinationType = purchaseRequestHeader.DestinationType,
                    TransactionStatus = purchaseRequestHeader.TransactionStatus,
                    TruckType = purchaseRequestHeader.TruckType,
                    Details = new List<PurchaseRequestDetailDTO>(),
                    CreatedBy = purchaseRequestHeader.CreatedBy,
                    CreatedOn = purchaseRequestHeader.SourceType.Equals("OUTSOURCE") ? purchaseRequestHeader.CreatedOn.ToString("dd MMMM yyyy") : string.Format("{0} {1}{2}, {3}", purchaseRequestHeader.CreatedOn.ToString("MMMM"), purchaseRequestHeader.CreatedOn.Day, GetDaySuffix(purchaseRequestHeader.CreatedOn.Day), purchaseRequestHeader.CreatedOn.Year),
                    ModifiedBy = purchaseRequestHeader.ModifiedBy != null ? purchaseRequestHeader.ModifiedBy : "",
                    ModifiedOn = purchaseRequestHeader.ModifiedOn.HasValue ? purchaseRequestHeader.ModifiedOn.Value.ToString("dd MMMM yyyy") : "-"
                };

                string deliveryDate = purchaseRequestHeader.PurchaseRequestDetails.Select(m => m.ETA).FirstOrDefault().ToString();
                purchaseRequestHeaderDTO.DeliveryDate = Convert.ToDateTime(deliveryDate).ToString("dd MMMM yyyy");

                foreach (PurchaseRequestDetail detail in purchaseRequestHeader.PurchaseRequestDetails.OrderBy(x => x.CreatedOn))
                {
                    decimal outstanding = detail.Qty;
                    PurchaseRequestDetailDTO detailDTO = new PurchaseRequestDetailDTO()
                    {
                        ID = detail.ID,
                        HeaderID = detail.HeaderID,
                        MaterialCode = detail.MaterialCode,
                        MaterialName = detail.MaterialName,
                        Packaging = detail.Packaging.ToString(),
                        Qty = Helper.FormatThousand(detail.Qty),
                        ETA = detail.PurchaseRequestHeader.SourceType.Equals("OUTSOURCE") ? detail.ETA.ToString("dd MMMM yyyy") : detail.ETA.ToString("yyyy-MM-dd"),
                        UoM = detail.UoM,
                        Remarks = detail.Remarks
                    };

                    purchaseRequestHeaderDTO.Details.Add(detailDTO);
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

            obj.Add("data", purchaseRequestHeaderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(PurchaseRequestHeaderVM purchaseRequestHeaderVM)
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
                    if (!string.IsNullOrEmpty(purchaseRequestHeaderVM.SourceType))
                    {
                        if (!Constant.SourceTypes().Contains(purchaseRequestHeaderVM.SourceType))
                        {
                            ModelState.AddModelError("PurchaseRequest.SourceType", "Source Type is not recognized.");
                        }

                        if (string.IsNullOrEmpty(purchaseRequestHeaderVM.TruckType))
                        {
                            if (purchaseRequestHeaderVM.SourceType.Equals("OUTSOURCE"))
                            {
                                ModelState.AddModelError("PurchaseRequest.TruckType", "Truck Type is required.");
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("PurchaseRequest.SourceType", "Source Type is required.");
                    }

                    if (!string.IsNullOrEmpty(purchaseRequestHeaderVM.SourceCode))
                    {
                        if (purchaseRequestHeaderVM.SourceType.Equals("OUTSOURCE"))
                        {
                            var temp = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                            if (temp == null)
                            {
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin is not recognized.");
                            }
                            else
                            {
                                if (temp.Code.Equals(purchaseRequestHeaderVM.DestinationCode))
                                {
                                    ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin can not be the same as Destination.");
                                }
                            }
                        }
                        else if (purchaseRequestHeaderVM.SourceType.Equals("CUSTOMER"))
                        {
                            var temp = await db.Customers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                            if (temp == null)
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Customer is not recognized.");

                        }
                        else if (purchaseRequestHeaderVM.SourceType.Equals("VENDOR") || purchaseRequestHeaderVM.SourceType.Equals("IMPORT"))
                        {
                            var temp = await db.Suppliers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                            if (temp == null)
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Supplier is not recognized.");
                        }
                    }
                    else
                    {
                        if (purchaseRequestHeaderVM.SourceType.Equals("OUTSOURCE"))
                        {
                            ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin is required.");
                        }
                        else if (purchaseRequestHeaderVM.SourceType.Equals("CUSTOMER"))
                        {
                            ModelState.AddModelError("PurchaseRequest.SourceCode", "Customer is required.");
                        }
                        else if (purchaseRequestHeaderVM.SourceType.Equals("VENDOR") || purchaseRequestHeaderVM.SourceType.Equals("IMPORT"))
                        {
                            ModelState.AddModelError("PurchaseRequest.SourceCode", "Supplier is required.");
                        }
                    }

                    if (string.IsNullOrEmpty(purchaseRequestHeaderVM.DestinationCode))
                    {
                        ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination is required.");
                    }
                    else
                    {
                        var temp = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.DestinationCode)).FirstOrDefaultAsync();

                        if (temp == null)
                        {
                            ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination is not recognized.");
                        }
                        else
                        {
                            //logic check
                            if (purchaseRequestHeaderVM.SourceType.Equals("OUTSOURCE") && temp.Code.Equals(purchaseRequestHeaderVM.SourceCode))
                            {
                                ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination can not be the same as Origin.");
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

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("PR");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.PurchaseRequestHeaders.AsQueryable().OrderByDescending(x => x.Code)
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

                    PurchaseRequestHeader purchaseRequestHeader = new PurchaseRequestHeader
                    {
                        ID = TransactionId,
                        Code = Code,
                        //RefNumber = purchaseRequestHeaderVM.RefNumber,
                        SourceType = purchaseRequestHeaderVM.SourceType,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                    };

                    if (purchaseRequestHeader.SourceType.ToUpper().Equals("VENDOR") || purchaseRequestHeader.SourceType.ToUpper().Equals("IMPORT"))
                    {
                        Supplier temp = await db.Suppliers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = temp.Code;
                        purchaseRequestHeader.SourceName = temp.Name;
                        purchaseRequestHeader.SourceAddress = temp.Address;

                        //purchaseRequestHeader.RefNumber = "PO-" + yearMonth;
                    }
                    else if (purchaseRequestHeader.SourceType.ToUpper().Equals("CUSTOMER"))
                    {
                        Customer temp = await db.Customers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = temp.Code;
                        purchaseRequestHeader.SourceName = temp.Name;
                        purchaseRequestHeader.SourceAddress = temp.Address;

                        //purchaseRequestHeader.RefNumber = "DN-" + yearMonth;
                        purchaseRequestHeader.RefNumber = "DN-" + CreatedAt.Year.ToString() + "-" + CreatedAt.Month.ToString("d2") + "-";
                    }
                    else if (purchaseRequestHeader.SourceType.ToUpper().Equals("OUTSOURCE"))
                    {
                        //purchaseRequestHeader.RefNumber = "DO-" + CreatedAt.Year.ToString() + "-" + CreatedAt.Month.ToString("d2") + "-";
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = wh.Code;
                        purchaseRequestHeader.SourceName = wh.Name;
                        purchaseRequestHeader.SourceAddress = "";
                        purchaseRequestHeader.TruckType = purchaseRequestHeaderVM.TruckType;
                    }
                    else if (purchaseRequestHeader.SourceType.ToUpper().Equals("OTHER"))
                    {
                        purchaseRequestHeader.SourceCode = "M1";
                        purchaseRequestHeader.SourceName = "OTHER RECEIVING";
                        purchaseRequestHeader.SourceAddress = "";
                        purchaseRequestHeader.RefNumber = "MN-" + CreatedAt.Year.ToString() + "-" + CreatedAt.Month.ToString("d2") + "-";
                    }
                    //else if (purchaseRequestHeader.SourceType.ToUpper().Equals("OTHER"))
                    //{
                    //    Supplier temp = await db.Suppliers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();
                    //    if (temp == null)
                    //    {
                    //        Customer temp1 = await db.Customers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();
                    //        purchaseRequestHeader.SourceCode = temp1.Code;
                    //        purchaseRequestHeader.SourceName = temp1.Name;
                    //        purchaseRequestHeader.SourceAddress = temp1.Address;
                    //        //purchaseRequestHeader.SourceType = "CUSTOMER";
                    //    }
                    //    else
                    //    {
                    //        purchaseRequestHeader.SourceCode = temp.Code;
                    //        purchaseRequestHeader.SourceName = temp.Name;
                    //        purchaseRequestHeader.SourceAddress = temp.Address;
                    //        //purchaseRequestHeader.SourceType = "VENDOR";
                    //    }

                    //}

                    if (purchaseRequestHeader.SourceType.ToUpper().Equals("CUSTOMER") || purchaseRequestHeader.SourceType.ToUpper().Equals("MANUAL"))
                    {
                        // Check Ref Number Last
                        lastNumber = db.PurchaseRequestHeaders.AsQueryable().OrderByDescending(x => x.RefNumber)
                        .Where(x => x.SourceType.Equals(purchaseRequestHeaderVM.SourceType) && x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.RefNumber).FirstOrDefault();

                        currentNumber = 0;


                        if (!string.IsNullOrEmpty(lastNumber))
                        {
                            currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 2));
                        }
                        runningNumber = string.Format("{0:D2}", currentNumber + 1);

                        // Ref Number format completion
                        purchaseRequestHeader.RefNumber = string.Format("{0}{1}", purchaseRequestHeader.RefNumber, runningNumber);
                    }
                    else
                    {
                        purchaseRequestHeader.RefNumber = "";
                    }

                    Warehouse warehouse = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.DestinationCode) && s.IsActive).FirstOrDefaultAsync();

                    purchaseRequestHeader.DestinationCode = warehouse.Code;
                    purchaseRequestHeader.DestinationName = warehouse.Name;
                    purchaseRequestHeader.DestinationType = warehouse.Type;

                    id = purchaseRequestHeader.ID;

                    db.PurchaseRequestHeaders.Add(purchaseRequestHeader);

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
        public async Task<IHttpActionResult> Update(PurchaseRequestHeaderVM purchaseRequestHeaderVM)
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
                    PurchaseRequestHeader purchaseRequestHeader = new PurchaseRequestHeader();
                    if (string.IsNullOrEmpty(purchaseRequestHeaderVM.ID))
                    {
                        throw new Exception("Purchase request ID is required.");
                    }
                    else
                    {
                        purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(s => s.ID.Equals(purchaseRequestHeaderVM.ID)).FirstOrDefaultAsync();

                        if (purchaseRequestHeader == null)
                        {
                            throw new Exception("Purchase request is not recognized.");
                        }

                        if (!purchaseRequestHeader.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Purchase Request is not open for edit.");
                        }

                        if (!string.IsNullOrEmpty(purchaseRequestHeaderVM.SourceCode))
                        {
                            if (purchaseRequestHeaderVM.SourceType.Equals("OUTSOURCE"))
                            {
                                var temp = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                                if (temp == null)
                                {
                                    ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin is not recognized.");
                                }
                                else
                                {
                                    if (temp.Code.Equals(purchaseRequestHeaderVM.DestinationCode))
                                    {
                                        ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin can not be the same as Destination.");
                                    }
                                }
                            }
                            else if (purchaseRequestHeader.SourceType.Equals("CUSTOMER"))
                            {
                                var temp = await db.Customers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                                if (temp == null)
                                    ModelState.AddModelError("PurchaseRequest.SourceCode", "Customer is not recognized.");

                            }
                            else if (purchaseRequestHeader.SourceType.Equals("VENDOR") || purchaseRequestHeader.SourceType.Equals("IMPORT"))
                            {
                                var temp = await db.Suppliers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                                if (temp == null)
                                    ModelState.AddModelError("PurchaseRequest.SourceCode", "Supplier is not recognized.");
                            }
                        }
                        else
                        {
                            if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                            {
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Origin is required.");
                            }
                            else if (purchaseRequestHeader.SourceType.Equals("CUSTOMER"))
                            {
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Customer is required.");
                            }
                            else if (purchaseRequestHeader.SourceType.Equals("VENDOR") || purchaseRequestHeader.SourceType.Equals("IMPORT"))
                            {
                                ModelState.AddModelError("PurchaseRequest.SourceCode", "Supplier is required.");
                            }
                        }

                        if (string.IsNullOrEmpty(purchaseRequestHeaderVM.DestinationCode))
                        {
                            ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination is required.");
                        }
                        else
                        {
                            var temp = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.DestinationCode)).FirstOrDefaultAsync();

                            if (temp == null)
                            {
                                ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination is not recognized.");
                            }
                            else
                            {
                                //logic check
                                if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE") && temp.Code.Equals(purchaseRequestHeaderVM.SourceCode))
                                {
                                    ModelState.AddModelError("PurchaseRequest.DestinationCode", "Destination can not be the same as Origin.");
                                }
                            }
                        }


                        if (string.IsNullOrEmpty(purchaseRequestHeaderVM.TruckType))
                        {
                            if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                            {
                                ModelState.AddModelError("PurchaseRequest.TruckType", "Truck Type is required.");
                            }
                        }

                        if (string.IsNullOrEmpty(purchaseRequestHeaderVM.DeliveryDate))
                        {
                            if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                            {
                                ModelState.AddModelError("PurchaseRequest.DeliveryDate", "Delivery Date is required.");
                            }
                        }
                        else
                        {
                            try
                            {
                                DateTime etaDate = Convert.ToDateTime(purchaseRequestHeaderVM.DeliveryDate);
                            }
                            catch (FormatException e)
                            {
                                ModelState.AddModelError("PurchaseRequest.DeliveryDate", "Bad format Delivery Date.");
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


                    purchaseRequestHeader.ModifiedBy = activeUser;
                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    if (tokenDate.LoginDate < purchaseRequestHeader.CreatedOn.Date)
                    {
                        throw new Exception("Bad Login date.");
                    }
                    purchaseRequestHeader.ModifiedOn = transactionDate;

                    if (purchaseRequestHeader.SourceType.ToUpper().Equals("VENDOR") || purchaseRequestHeader.SourceType.ToUpper().Equals("IMPORT"))
                    {
                        Supplier temp = await db.Suppliers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = temp.Code;
                        purchaseRequestHeader.SourceName = temp.Name;
                        purchaseRequestHeader.SourceAddress = temp.Address;

                    }
                    else if (purchaseRequestHeader.SourceType.ToUpper().Equals("CUSTOMER"))
                    {
                        Customer temp = await db.Customers.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode) && s.IsActive).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = temp.Code;
                        purchaseRequestHeader.SourceName = temp.Name;
                        purchaseRequestHeader.SourceAddress = temp.Address;

                    }
                    else if (purchaseRequestHeader.SourceType.ToUpper().Equals("OUTSOURCE"))
                    {

                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.SourceCode)).FirstOrDefaultAsync();

                        purchaseRequestHeader.SourceCode = wh.Code;
                        purchaseRequestHeader.SourceName = wh.Name;
                        purchaseRequestHeader.TruckType = purchaseRequestHeaderVM.TruckType;
                    }

                    Warehouse warehouse = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeaderVM.DestinationCode) && s.IsActive).FirstOrDefaultAsync();

                    purchaseRequestHeader.DestinationCode = warehouse.Code;
                    purchaseRequestHeader.DestinationName = warehouse.Name;
                    purchaseRequestHeader.DestinationType = warehouse.Type;

                    //update all ETA

                    if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                    {
                        foreach (PurchaseRequestDetail detail in purchaseRequestHeader.PurchaseRequestDetails)
                        {
                            detail.ETA = Convert.ToDateTime(purchaseRequestHeaderVM.DeliveryDate);
                        }
                    }
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

            obj.Add("id", id);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateDetail(PurchaseRequestDetailVM purchaseRequestDetailVM)
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
                    PurchaseRequestHeader purchaseRequestHeader = null;
                    //RawMaterial rawMaterial = null;
                    //commented by bhov, material request can handle RM & SFG
                    vProductMaster vProduct = null;
                    if (string.IsNullOrEmpty(purchaseRequestDetailVM.HeaderID))
                    {
                        throw new Exception("Purchase Request is required.");
                    }
                    else
                    {
                        purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(s => s.ID.Equals(purchaseRequestDetailVM.HeaderID)).FirstOrDefaultAsync();

                        if (purchaseRequestHeader == null)
                        {
                            throw new Exception("Purchase Request is not recognized.");
                        }
                        else
                        {
                            if (!purchaseRequestHeader.TransactionStatus.Equals("OPEN"))
                            {
                                if (!purchaseRequestHeader.TransactionStatus.Equals("CONFIRMED"))
                                {
                                    throw new Exception("Purchase Request is not open for edit.");
                                }
                            }
                        }

                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeader.DestinationCode)).FirstOrDefaultAsync();
                        string[] warehouseCodes = { };
                        if (purchaseRequestHeader.TransactionStatus.Equals("CONFIRMED") && wh.Type.Equals("OUTSOURCE")) // 
                        {
                            if (!purchaseRequestHeader.SourceType.Equals("VENDOR"))
                            {
                                if (!purchaseRequestHeader.SourceType.Equals("IMPORT"))
                                {
                                    throw new Exception("Source type " + purchaseRequestHeader.SourceType.ToString() + " and status CONFIRMED is not allowed for add detail.");
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(purchaseRequestDetailVM.MaterialCode))
                    {
                        //ModelState.AddModelError("PurchaseRequest.MaterialCode", "Raw Material is required.");
                        throw new Exception("Material Code is required.");
                    }
                    else
                    {
                        vProduct = await db.vProductMasters.Where(m => m.MaterialCode.Equals(purchaseRequestDetailVM.MaterialCode)).FirstOrDefaultAsync();
                        if (vProduct == null)
                        {
                            //ModelState.AddModelError("PurchaseRequest.MaterialCode", "Raw Material is not recognized.");
                            throw new Exception("Material is not recognized.");
                        }
                        else
                        {
                            //check if MaterialCode already inserted for current HeaderID
                            PurchaseRequestDetail requestDetail = await db.PurchaseRequestDetails.Where(m => m.HeaderID.Equals(purchaseRequestDetailVM.HeaderID) && m.MaterialCode.Equals(purchaseRequestDetailVM.MaterialCode)).FirstOrDefaultAsync();
                            if (requestDetail != null)
                            {
                                //ModelState.AddModelError("PurchaseRequest.MaterialCode", "Raw Material already exist.");
                                throw new Exception("Material is already exist.");
                            }
                        }
                    }

                    decimal minQty = 0;
                    decimal qtyPerBag = 0;
                    decimal PoRate = 0;

                    if (purchaseRequestDetailVM.Qty <= 0)
                    {
                        string qtyMsg = "";
                        if (vProduct.ProdType.Equals("RM"))
                        {
                            qtyMsg = "Bag Qty is required.";
                        }
                        else
                        {
                            qtyMsg = "Qty is required.";
                        }
                        ModelState.AddModelError("PurchaseRequest.Qty", qtyMsg);
                    }
                    else
                    {
                        //if rm, check convertion
                        if (vProduct.ProdType.Equals("RM"))
                        {
                            if (string.IsNullOrEmpty(purchaseRequestDetailVM.UoM))
                            {
                                ModelState.AddModelError("PurchaseRequest.UoM", "UoM is required.");
                            }
                            else
                            {
                                //check if outsource, validate order
                                if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                                {
                                    Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeader.SourceCode)).FirstOrDefaultAsync();
                                    string[] warehouseCodes = { };
                                    if (!wh.Type.Equals("EMIX"))
                                    {
                                        warehouseCodes = new string[1] { purchaseRequestHeader.SourceCode };
                                    }
                                    else
                                    {
                                        warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                                    }

                                    vStockWarehouse vStock = db.vStockWarehouses.Where(m => warehouseCodes.Contains(m.WarehouseCode) && m.MaterialCode.Equals(purchaseRequestDetailVM.MaterialCode) && m.QtyPerBag.Equals(purchaseRequestDetailVM.QtyPerBag)).FirstOrDefault();
                                    if (vStock == null)
                                        throw new Exception("Material not recognized.");

                                    decimal availableQty = vStock.TotalQuantity.Value;
                                    decimal requestedQty = purchaseRequestDetailVM.Qty * vStock.QtyPerBag;
                                    if (availableQty < requestedQty)
                                    {
                                        ModelState.AddModelError("PurchaseRequest.Qty", string.Format("Bag Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableQty)));
                                    }
                                }

                                if (purchaseRequestDetailVM.UoM != "KG" && purchaseRequestDetailVM.UoM != "L")
                                {
                                    ModelState.AddModelError("PurchaseRequest.UoM", "UoM is not recognized.");
                                }
                                else
                                {
                                    vRawMaterialConvert rm = await db.vRawMaterialConverts.Where(s => s.MaterialCode.Equals(purchaseRequestDetailVM.MaterialCode)).FirstOrDefaultAsync();

                                    if (purchaseRequestDetailVM.UoM.ToUpper().Equals("L") && rm.IsConvertible.HasValue ? true : false)
                                    {
                                        purchaseRequestDetailVM.UoM = "L";
                                        minQty = rm.MinPurchaseQtyLitre.HasValue ? rm.MinPurchaseQtyLitre.Value : 0;
                                        qtyPerBag = rm.MinPurchaseQtyLitre.Value;
                                        PoRate = rm.PoRate;
                                    }
                                    else if (purchaseRequestDetailVM.UoM.ToUpper().Equals("KG"))
                                    {
                                        purchaseRequestDetailVM.UoM = "KG";
                                        minQty = rm.MinPurchaseQty;
                                        qtyPerBag = rm.Qty;
                                        PoRate = 0;
                                    }

                                    if (!purchaseRequestDetailVM.UseMoQ)
                                    {
                                        minQty = qtyPerBag;
                                    }

                                    purchaseRequestDetailVM.Qty *= minQty > 0 ? minQty : rm.Qty;

                                    if (purchaseRequestDetailVM.Qty < minQty)
                                    {
                                        ModelState.AddModelError("PurchaseRequest.Qty", "Requested Qty does not reach the minimum order quantity.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            //check if outsource, validate order
                            if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                            {
                                Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeader.SourceCode)).FirstOrDefaultAsync();
                                string[] warehouseCodes = { };
                                if (!wh.Type.Equals("EMIX"))
                                {
                                    warehouseCodes = new string[1] { purchaseRequestHeader.SourceCode };
                                }
                                else
                                {
                                    warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                                }

                                vStockWarehouse vStock = db.vStockWarehouses.Where(m => warehouseCodes.Contains(m.WarehouseCode) && m.MaterialCode.Equals(purchaseRequestDetailVM.MaterialCode) && m.QtyPerBag.Equals(purchaseRequestDetailVM.QtyPerBag)).FirstOrDefault();
                                if (vStock == null)
                                    throw new Exception("Material not recognized.");

                                qtyPerBag = vStock.QtyPerBag;

                                decimal availableQty = vStock.TotalQuantity.Value;
                                decimal requestedQty = purchaseRequestDetailVM.Qty;
                                if (availableQty < requestedQty)
                                {
                                    ModelState.AddModelError("PurchaseRequest.Qty", string.Format("Total Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableQty)));
                                }
                            }
                        }
                    }


                    if (string.IsNullOrEmpty(purchaseRequestDetailVM.ETA))
                    {
                        ModelState.AddModelError("PurchaseRequest.ETA", "Receive Date is required.");
                    }
                    else
                    {
                        try
                        {
                            DateTime etaDate = Convert.ToDateTime(purchaseRequestDetailVM.ETA);
                        }
                        catch (FormatException e)
                        {
                            ModelState.AddModelError("PurchaseRequest.ETA", "Bad format Receive Date.");
                        }
                    }

                    if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE") && purchaseRequestDetailVM.Packaging <= 0)
                    {
                        ModelState.AddModelError("PurchaseRequest.Packaging", "Packaging is required.");
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

                    PurchaseRequestDetail purchaseRequestDetail = new PurchaseRequestDetail()
                    {
                        ID = Helper.CreateGuid("PRD"),
                        HeaderID = purchaseRequestHeader.ID,
                        MaterialCode = vProduct.MaterialCode,
                        MaterialName = vProduct.MaterialName,
                        MaterialType = vProduct.ProdType,
                        Qty = purchaseRequestDetailVM.Qty,
                        UoM = purchaseRequestDetailVM.UoM,
                        ETA = Convert.ToDateTime(purchaseRequestDetailVM.ETA),
                        Packaging = purchaseRequestDetailVM.Packaging,
                        Remarks = purchaseRequestDetailVM.Remarks,
                        QtyPerBag = qtyPerBag,
                        MoQ = minQty,
                        CreatedBy = activeUser,
                        CreatedOn = transactionDate,
                        PoRate = PoRate
                    };

                    purchaseRequestHeader.PurchaseRequestDetails.Add(purchaseRequestDetail);

                    if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                    {
                        foreach (PurchaseRequestDetail detail in purchaseRequestHeader.PurchaseRequestDetails)
                        {
                            detail.ETA = purchaseRequestDetail.ETA;
                        }
                    }

                    if (purchaseRequestHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(purchaseRequestHeader.DestinationCode)).FirstOrDefaultAsync();
                        string[] warehouseCodes = { };
                        if (wh.Type.Equals("EMIX") || (wh.Type.Equals("OUTSOURCE") && (purchaseRequestHeader.SourceType.Equals("VENDOR") || purchaseRequestHeader.SourceType.Equals("IMPORT"))))
                        {
                            if (vProduct.ProdType.Equals("RM"))
                            {
                                Receiving receiving = new Receiving
                                {
                                    ID = Helper.CreateGuid("RCp"),
                                    PurchaseRequestID = purchaseRequestDetail.ID,
                                    RefNumber = purchaseRequestHeader.RefNumber,
                                    MaterialCode = vProduct.MaterialCode,
                                    MaterialName = vProduct.MaterialName,
                                    Qty = purchaseRequestDetailVM.Qty,
                                    UoM = purchaseRequestDetailVM.UoM,
                                    TransactionStatus = "OPEN",
                                    QtyPerBag = qtyPerBag,
                                    ETA = Convert.ToDateTime(purchaseRequestDetailVM.ETA)
                                };

                                db.Receivings.Add(receiving);
                            }

                            if (vProduct.ProdType.Equals("SFG"))
                            {
                                //add SFG Receiving later
                                ReceivingSFG receiving = new ReceivingSFG
                                {
                                    ID = Helper.CreateGuid("RCp"),
                                    PurchaseRequestID = purchaseRequestDetail.ID,
                                    ProductCode = vProduct.MaterialCode,
                                    ProductName = vProduct.MaterialName,
                                    Qty = purchaseRequestDetailVM.Qty,
                                    UoM = purchaseRequestDetailVM.UoM,
                                    TransactionStatus = "OPEN",
                                    QtyPerBag = qtyPerBag,
                                    WarehouseCode = purchaseRequestHeader.DestinationCode,
                                    WarehouseName = purchaseRequestHeader.DestinationName,
                                    CreatedBy = activeUser,
                                    CreatedOn = DateTime.Now
                                };

                                db.ReceivingSFGs.Add(receiving);
                            }
                        }
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Add detail succeeded.";
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
        public async Task<IHttpActionResult> RemoveDetail(PurchaseRequestDetailVM purchaseRequestDetailVM)
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
                    if (string.IsNullOrEmpty(purchaseRequestDetailVM.ID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        PurchaseRequestDetail requestDetail = await db.PurchaseRequestDetails.Where(m => m.ID.Equals(purchaseRequestDetailVM.ID)).FirstOrDefaultAsync();
                        if (requestDetail == null)
                        {
                            throw new Exception("ID is not recognized.");
                        }

                        if (requestDetail.PurchaseRequestHeader.TransactionStatus.Equals("OPEN"))
                        {
                            db.PurchaseRequestDetails.Remove(requestDetail);
                        }

                        if (requestDetail.PurchaseRequestHeader.TransactionStatus.Equals("CONFIRMED"))
                        {
                            db.PurchaseRequestDetails.Remove(requestDetail);

                            Receiving receiving = await db.Receivings.Where(m => m.PurchaseRequestID.Equals(purchaseRequestDetailVM.ID)).FirstOrDefaultAsync();
                            db.Receivings.Remove(receiving);
                        }

                        await db.SaveChangesAsync();

                        status = true;
                        message = "Remove receiving plan succeeded.";
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
            int idx = 0;

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
                    PurchaseRequestHeader purchaseRequestHeader = await db.PurchaseRequestHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();
                    string[] noStockls = new string[purchaseRequestHeader.PurchaseRequestDetails.Count];

                    if (!purchaseRequestHeader.TransactionStatus.Equals("CANCELLED"))
                    {
                        if (transactionStatus.Equals("CANCELLED"))
                        {
                            if (!purchaseRequestHeader.TransactionStatus.Equals("OPEN"))
                            {
                                throw new Exception("Transaction can not be cancelled.");
                            }

                            message = "Data cancellation succeeded.";
                        }
                        else if (transactionStatus.Equals("CONFIRMED"))
                        {
                            if (!purchaseRequestHeader.TransactionStatus.Equals("OPEN"))
                            {
                                throw new Exception("Transaction can not be confirmed.");
                            }

                            message = "Data confirmation succeeded.";
                        }
                        else
                        {
                            throw new Exception("Transaction Status is not recognized.");
                        }

                        purchaseRequestHeader.TransactionStatus = transactionStatus;
                        purchaseRequestHeader.ModifiedBy = activeUser;

                        //logic check token date
                        TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                        DateTime now = DateTime.Now;
                        DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                        if (tokenDate.LoginDate < purchaseRequestHeader.CreatedOn.Date)
                        {
                            throw new Exception("Bad Login date.");
                        }

                        purchaseRequestHeader.ModifiedOn = transactionDate;


                        if (transactionStatus.Equals("CANCELLED"))
                        {
                            db.PurchaseRequestDetails.RemoveRange(purchaseRequestHeader.PurchaseRequestDetails);
                        }

                        if (transactionStatus.Equals("CONFIRMED"))
                        {
                            //check detail
                            if (purchaseRequestHeader.PurchaseRequestDetails.Count() < 1)
                            {
                                throw new Exception("Receiving Plan can not be empty.");
                            }

                            string originCode = purchaseRequestHeader.SourceCode;
                            string destinationCode = purchaseRequestHeader.DestinationCode;

                            Warehouse origin = await db.Warehouses.Where(s => s.Code.Equals(originCode)).FirstOrDefaultAsync();
                            Warehouse destination = await db.Warehouses.Where(s => s.Code.Equals(destinationCode)).FirstOrDefaultAsync();


                            //create receiving plan RM/SFG
                            foreach (PurchaseRequestDetail prd in purchaseRequestHeader.PurchaseRequestDetails)
                            {
                                //if RM insert to Receiving, if SG insert to ReceivingSFG
                                if (destination.Type.Equals("EMIX") || (destination.Type.Equals("OUTSOURCE") && (purchaseRequestHeader.SourceType.Equals("VENDOR") || purchaseRequestHeader.SourceType.Equals("IMPORT"))))
                                {
                                    if (prd.MaterialType.Equals("RM"))
                                    {
                                        Receiving receiving = new Receiving
                                        {
                                            ID = Helper.CreateGuid("RCp"),
                                            PurchaseRequestID = prd.ID,
                                            RefNumber = purchaseRequestHeader.RefNumber,
                                            MaterialCode = prd.MaterialCode,
                                            MaterialName = prd.MaterialName,
                                            Qty = prd.Qty,
                                            UoM = prd.UoM,
                                            TransactionStatus = "OPEN",
                                            QtyPerBag = prd.QtyPerBag,
                                            ETA = prd.ETA
                                        };

                                        db.Receivings.Add(receiving);
                                    }

                                    if (prd.MaterialType.Equals("SFG"))
                                    {
                                        //add SFG Receiving later
                                        ReceivingSFG receiving = new ReceivingSFG
                                        {
                                            ID = Helper.CreateGuid("RCp"),
                                            PurchaseRequestID = prd.ID,
                                            ProductCode = prd.MaterialCode,
                                            ProductName = prd.MaterialName,
                                            Qty = prd.Qty,
                                            UoM = prd.UoM,
                                            TransactionStatus = "OPEN",
                                            QtyPerBag = prd.QtyPerBag,
                                            WarehouseCode = destination.Code,
                                            WarehouseName = destination.Name,
                                            CreatedBy = activeUser,
                                            CreatedOn = DateTime.Now
                                        };

                                        db.ReceivingSFGs.Add(receiving);
                                    }
                                }
                            }

                            // pemotongan stock sourcenya dipindahin kembali ke saat receive
                            ////if type outsource
                            ////create outbound & inbound
                            //if (purchaseRequestHeader.SourceType.Equals("OUTSOURCE"))
                            //{
                            //    //check origin
                            //    //origin will create other outbound
                            //    //if emix, create outbound status confirmed, picking only
                            //    //if outsource create outbound status closed, autopicked (update stock automatically)

                            //    string tStatus = "";
                            //    bool autoPicking = false;

                            //    if (origin.Type.Equals("EMIX"))
                            //    {
                            //        autoPicking = false;
                            //        tStatus = "CONFIRMED";
                            //    }
                            //    else
                            //    {
                            //        autoPicking = true;
                            //        tStatus = "CLOSED";
                            //    }

                            //    #region outbound
                            //    var CreatedAt = transactionDate;
                            //    var TransactionId = Helper.CreateGuid("OUT");

                            //    string prefix = TransactionId.Substring(0, 3);
                            //    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                            //    int month = CreatedAt.Month;
                            //    string romanMonth = Helper.ConvertMonthToRoman(month);

                            //    // get last number, and do increment.
                            //    string lastNumber = db.OutboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
                            //        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                            //        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                            //    int currentNumber = 0;

                            //    if (!string.IsNullOrEmpty(lastNumber))
                            //    {
                            //        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                            //    }

                            //    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                            //    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                            //    // Ref Number necessities
                            //    string yearMonth = CreatedAt.Year.ToString() + month.ToString();

                            //    OutboundHeader outbound = new OutboundHeader
                            //    {
                            //        ID = TransactionId,
                            //        Code = Code,
                            //        Remarks = string.Format("Created from Material Request: {0}, Origin: {1}, Destination: {2}", purchaseRequestHeader.Code, origin.Code + " - " + origin.Name, destination.Code + " - " + destination.Name),
                            //        TransactionStatus = tStatus,
                            //        CreatedBy = activeUser,
                            //        CreatedOn = CreatedAt,
                            //        WarehouseCode = origin.Code,
                            //        WarehouseName = origin.Name
                            //    };

                            //    db.OutboundHeaders.Add(outbound);

                            //    //insert order
                            //    foreach (PurchaseRequestDetail prd in purchaseRequestHeader.PurchaseRequestDetails)
                            //    {
                            //        vProductMaster product = await db.vProductMasters.Where(m => m.MaterialCode.Equals(prd.MaterialCode)).FirstOrDefaultAsync();
                            //        if (product != null)
                            //        {
                            //            OutboundOrder order = new OutboundOrder()
                            //            {
                            //                ID = Helper.CreateGuid("Oo"),
                            //                OutboundID = outbound.ID,
                            //                MaterialCode = product.MaterialCode,
                            //                MaterialName = product.MaterialName,
                            //                MaterialType = product.ProdType,
                            //                TotalQty = prd.Qty,
                            //                QtyPerBag = prd.QtyPerBag,
                            //                CreatedBy = activeUser,
                            //                CreatedOn = transactionDate
                            //            };

                            //            db.OutboundOrders.Add(order);

                            //            //insert picking if auto picking == true
                            //            if (autoPicking)
                            //            {
                            //                //get available quantity
                            //                vStockAll stockAll = db.vStockAlls.Where(m => m.MaterialCode.Equals(prd.MaterialCode) && m.WarehouseCode.Equals(originCode)).FirstOrDefault();
                            //                if(stockAll != null)
                            //                {
                            //                    BinRack binRack = db.BinRacks.Where(m => m.WarehouseCode.Equals(originCode)).FirstOrDefault();

                            //                    decimal availableQty = order.TotalQty;
                            //                    if(order.TotalQty > stockAll.Quantity)
                            //                    {
                            //                        availableQty = stockAll.Quantity;
                            //                    }

                            //                    //check if have remainder
                            //                    int bagQty = Convert.ToInt32(availableQty / order.QtyPerBag);
                            //                    decimal remainder = availableQty - (bagQty * order.QtyPerBag);

                            //                    //picking limit based on stock qty

                            //                    OutboundPicking picking = new OutboundPicking();
                            //                    picking.ID = Helper.CreateGuid("P");
                            //                    picking.OutboundOrderID = order.ID;
                            //                    picking.PickingMethod = "MANUAL";
                            //                    picking.PickedOn = DateTime.Now;
                            //                    picking.PickedBy = activeUser;
                            //                    picking.BinRackID = binRack.ID;
                            //                    picking.BinRackCode = stockAll.BinRackCode;
                            //                    picking.BinRackName = stockAll.BinRackName;
                            //                    picking.BagQty = bagQty;
                            //                    picking.QtyPerBag = stockAll.QtyPerBag;

                            //                    db.OutboundPickings.Add(picking);

                            //                    if(remainder > 0)
                            //                    {
                            //                        picking = new OutboundPicking();
                            //                        picking.ID = Helper.CreateGuid("P");
                            //                        picking.OutboundOrderID = order.ID;
                            //                        picking.PickingMethod = "MANUAL";
                            //                        picking.PickedOn = DateTime.Now;
                            //                        picking.PickedBy = activeUser;
                            //                        picking.BinRackID = binRack.ID;
                            //                        picking.BinRackCode = stockAll.BinRackCode;
                            //                        picking.BinRackName = stockAll.BinRackName;
                            //                        picking.BagQty = Convert.ToInt32(remainder / remainder);
                            //                        picking.QtyPerBag = remainder;                                                   

                            //                        db.OutboundPickings.Add(picking);
                            //                    }

                            //                    if (product.ProdType.Equals("RM"))
                            //                    {
                            //                        decimal pickQty = availableQty;
                            //                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                            //                        stock.Quantity -= pickQty;
                            //                    }
                            //                    else if (product.ProdType.Equals("SFG"))
                            //                    {
                            //                        decimal pickQty = availableQty;
                            //                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                            //                        stock.Quantity -= pickQty;
                            //                    }
                            //                }                                       
                            //            }
                            //        }
                            //    }

                            //    #endregion outbound

                            //    #region inbound

                            //    //check destination
                            //    //destination will create other inbound
                            //    //if emix, create inbound status confirmed, putaway only
                            //    //if outsource, create inbound status closed, autoputaway (update stock automatically)

                            //    if (!destination.Type.Equals("EMIX"))
                            //    {
                            //        TransactionId = Helper.CreateGuid("IN");

                            //        prefix = TransactionId.Substring(0, 2);

                            //        // get last number, and do increment.
                            //        lastNumber = db.InboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
                            //            .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                            //            .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                            //        currentNumber = 0;

                            //        if (!string.IsNullOrEmpty(lastNumber))
                            //        {
                            //            currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                            //        }

                            //        runningNumber = string.Format("{0:D3}", currentNumber + 1);

                            //        Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                            //        // Ref Number necessities
                            //        yearMonth = CreatedAt.Year.ToString() + month.ToString();

                            //        InboundHeader inbound = new InboundHeader
                            //        {
                            //            ID = TransactionId,
                            //            Code = Code,
                            //            Remarks = string.Format("Created from Material Request: {0}, Origin: {1}, Destination: {2}", purchaseRequestHeader.Code, origin.Code + " - " + origin.Name, destination.Code + " - " + destination.Name),
                            //            TransactionStatus = "CLOSED",
                            //            WarehouseCode = destination.Code,
                            //            WarehouseName = destination.Name,
                            //            CreatedBy = activeUser,
                            //            CreatedOn = CreatedAt,
                            //        };

                            //        db.InboundHeaders.Add(inbound);

                            //        //insert order
                            //        foreach (PurchaseRequestDetail prd in purchaseRequestHeader.PurchaseRequestDetails)
                            //        {
                            //            vProductMaster product = await db.vProductMasters.Where(m => m.MaterialCode.Equals(prd.MaterialCode)).FirstOrDefaultAsync();
                            //            if (product != null)
                            //            {
                            //                InboundOrder order = new InboundOrder()
                            //                {
                            //                    ID = Helper.CreateGuid("Oo"),
                            //                    InboundID = inbound.ID,
                            //                    MaterialCode = product.MaterialCode,
                            //                    MaterialName = product.MaterialName,
                            //                    MaterialType = product.ProdType,
                            //                    Qty = prd.Qty,
                            //                    QtyPerBag = prd.QtyPerBag,
                            //                    CreatedBy = activeUser,
                            //                    CreatedOn = transactionDate
                            //                };

                            //                db.InboundOrders.Add(order);

                            //                InboundReceive rec = new InboundReceive();
                            //                rec.ID = Helper.CreateGuid("Ir");
                            //                rec.InboundOrderID = order.ID;
                            //                rec.Qty = order.Qty;
                            //                rec.QtyPerBag = order.QtyPerBag;
                            //                rec.ReceivedBy = activeUser;
                            //                rec.ReceivedOn = DateTime.Now;
                            //                rec.LastSeries = 0;

                            //                db.InboundReceives.Add(rec);

                            //                BinRack binRack = db.BinRacks.Where(m => m.WarehouseCode.Equals(destinationCode)).FirstOrDefault();

                            //                decimal availableQty = order.Qty;
                            //                //check if have remainder
                            //                int bagQty = Convert.ToInt32(availableQty / order.QtyPerBag);
                            //                decimal remainder = availableQty - (bagQty * order.QtyPerBag);

                            //                InboundPutaway putaway = new InboundPutaway();
                            //                putaway.ID = Helper.CreateGuid("Ip");
                            //                putaway.InboundReceiveID = rec.ID;
                            //                putaway.PutawayMethod = "MANUAL";
                            //                putaway.QtyPerBag = order.QtyPerBag;
                            //                putaway.PutOn = DateTime.Now;
                            //                putaway.PutBy = activeUser;
                            //                putaway.BinRackID = binRack.ID;
                            //                putaway.BinRackCode = binRack.Code;
                            //                putaway.BinRackName = binRack.Name;
                            //                putaway.PutawayQty = bagQty * order.QtyPerBag;

                            //                db.InboundPutaways.Add(putaway);

                            //                if (remainder > 0)
                            //                {
                            //                    putaway = new InboundPutaway();
                            //                    putaway.ID = Helper.CreateGuid("Ip");
                            //                    putaway.InboundReceiveID = rec.ID;
                            //                    putaway.PutawayMethod = "MANUAL";
                            //                    putaway.QtyPerBag = remainder;
                            //                    putaway.PutOn = DateTime.Now;
                            //                    putaway.PutBy = activeUser;
                            //                    putaway.BinRackID = binRack.ID;
                            //                    putaway.BinRackCode = binRack.Code;
                            //                    putaway.BinRackName = binRack.Name;
                            //                    putaway.PutawayQty = remainder;

                            //                    db.InboundPutaways.Add(putaway);
                            //                }

                            //                if (product.ProdType.Equals("RM"))
                            //                {
                            //                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                            //                    StockRM stockRM = await db.StockRMs.Where(m => m.MaterialCode.Equals(product.MaterialCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                            //                    if (stockRM != null)
                            //                    {
                            //                        stockRM.Quantity += availableQty;
                            //                    }
                            //                    else
                            //                    {
                            //                        stockRM = new StockRM();
                            //                        stockRM.ID = Helper.CreateGuid("S");
                            //                        stockRM.Code = product.MaterialCode;
                            //                        stockRM.MaterialCode = product.MaterialCode;
                            //                        stockRM.MaterialName = product.MaterialName;
                            //                        stockRM.Quantity = availableQty;
                            //                        stockRM.QtyPerBag = order.QtyPerBag;
                            //                        stockRM.BinRackID = binRack.ID;
                            //                        stockRM.BinRackCode = binRack.Code;
                            //                        stockRM.BinRackName = binRack.Name;
                            //                        stockRM.ReceivedAt = DateTime.Now;

                            //                        db.StockRMs.Add(stockRM);
                            //                    }
                            //                }
                            //                else
                            //                {
                            //                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                            //                    StockSFG stockSFG = await db.StockSFGs.Where(m => m.MaterialCode.Equals(product.MaterialCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                            //                    if (stockSFG != null)
                            //                    {
                            //                        stockSFG.Quantity += availableQty;
                            //                    }
                            //                    else
                            //                    {
                            //                        stockSFG = new StockSFG();
                            //                        stockSFG.ID = Helper.CreateGuid("S");
                            //                        stockSFG.Code = product.MaterialCode;
                            //                        stockSFG.MaterialCode = product.MaterialCode;
                            //                        stockSFG.MaterialName = product.MaterialName;
                            //                        stockSFG.Quantity = availableQty;
                            //                        stockSFG.QtyPerBag = order.QtyPerBag;
                            //                        stockSFG.BinRackID = binRack.ID;
                            //                        stockSFG.BinRackCode = binRack.Code;
                            //                        stockSFG.BinRackName = binRack.Name;
                            //                        stockSFG.ReceivedAt = DateTime.Now;

                            //                        db.StockSFGs.Add(stockSFG);
                            //                    }
                            //                }
                            //            }
                            //        }
                            //    }

                            //    #endregion inbound
                            //}

                        }
                        await db.SaveChangesAsync();
                        status = true;
                    }
                    else
                    {
                        message = "Can not change transaction status. Transaction is already cancelled.";
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

        public async Task<IHttpActionResult> Cancel(string id)
        {
            return await UpdateStatus(id, "CANCELLED");
        }

        public async Task<IHttpActionResult> Confirm(string id)
        {
            return await UpdateStatus(id, "CONFIRMED");
        }

        public IHttpActionResult GetSourceType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Source type found.";
            bool status = true;

            obj.Add("source_type", Constant.SourceTypes());
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        public IHttpActionResult GetTruckType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Truck type found.";
            bool status = true;

            obj.Add("source_type", Constant.TruckTypes());
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateRef(PurchaseRequestHeaderVM dataVM)
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
                    if (string.IsNullOrEmpty(dataVM.ID))
                    {
                        throw new Exception("Material Request ID is required.");
                    }

                    PurchaseRequestHeader header = await db.PurchaseRequestHeaders.Where(s => s.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();

                    if (header == null)
                    {
                        throw new Exception("Material Request is not recognized.");
                    }

                    if (!header.TransactionStatus.Equals("CONFIRMED") && !header.SourceType.Equals("OUTSOURCE"))
                    {
                        throw new Exception("Material Request Status is not valid.");
                    }

                    if (string.IsNullOrEmpty(dataVM.RefNumber))
                    {
                        ModelState.AddModelError("PurchaseRequest.PONumber", "Ref Number is required.");
                    }
                    bool isAllClosed = false;
                    foreach (var item in header.PurchaseRequestDetails)
                    {
                        foreach (var itemDetails in item.Receivings)
                        {
                            if (itemDetails.TransactionStatus == "CLOSED")
                            {
                                isAllClosed = true;
                            }
                            else
                            {
                                isAllClosed = false;
                                break;
                            }
                        }
                    }
                    if (isAllClosed)
                    {
                        throw new Exception("Ref Number cannot be updated. Receiving already closed.");
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

                    header.ModifiedBy = activeUser;
                    header.RefNumber = dataVM.RefNumber;

                    string[] detailList = header.PurchaseRequestDetails.Where(m => m.HeaderID.Equals(header.ID)).Select(x => x.ID).ToArray();

                    var receivings = db.Receivings.Where(m => detailList.Contains(m.PurchaseRequestID)).ToList();
                    receivings.ForEach(a => a.RefNumber = dataVM.RefNumber);

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Update Ref Number succeeded.";
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
        public async Task<IHttpActionResult> DatatableSupplier()
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

            IEnumerable<Supplier> list = Enumerable.Empty<Supplier>();
            IEnumerable<SupplierDTO> pagedData = Enumerable.Empty<SupplierDTO>();

            IQueryable<Supplier> query = db.Suppliers.Where(m => m.IsActive == true).AsQueryable();

            int recordsTotal = db.Suppliers.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.Name.Contains(search)
                        || m.Abbreviation.Contains(search)
                        || m.ClassificationName.Contains(search)
                        || m.Address.Contains(search)
                        || m.Telephone.Contains(search)
                        || m.Contact.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<Supplier, object>> cols = new Dictionary<string, Func<Supplier, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("Abbreviation", x => x.Abbreviation);
                cols.Add("ClassificationName", x => x.ClassificationName);
                cols.Add("Address", x => x.Address);
                cols.Add("DevelopmentDate", x => x.DevelopmentDate);
                cols.Add("Telephone", x => x.Telephone);
                cols.Add("Contact", x => x.Contact);
                cols.Add("IsActive", x => x.IsActive);
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
                                select new SupplierDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Abbreviation = x.Abbreviation,
                                    ClassificationName = x.ClassificationName,
                                    Address = x.Address,
                                    DevelopmentDate = Helper.NullDateToString2(x.DevelopmentDate),
                                    Telephone = x.Telephone,
                                    Contact = x.Contact,
                                    IsActive = x.IsActive,
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
        public async Task<IHttpActionResult> DatatableCustomer()
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

            IEnumerable<Customer> list = Enumerable.Empty<Customer>();
            IEnumerable<CustomerDTO> pagedData = Enumerable.Empty<CustomerDTO>();

            IQueryable<Customer> query = db.Customers.Where(m => m.IsActive == true).AsQueryable();

            int recordsTotal = db.Customers.Count();
            int recordsFiltered = 0;
            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.Name.Contains(search)
                        || m.Abbreviation.Contains(search)
                        || m.ClassificationName.Contains(search)
                        || m.Address.Contains(search)
                        || m.Telephone.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<Customer, object>> cols = new Dictionary<string, Func<Customer, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("Abbreviation", x => x.Abbreviation);
                cols.Add("ClassificationName", x => x.ClassificationName);
                cols.Add("Address", x => x.Address);
                cols.Add("Telephone", x => x.Telephone);
                cols.Add("IsActive", x => x.IsActive);
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
                                select new CustomerDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Abbreviation = x.Abbreviation,
                                    ClassificationName = x.ClassificationName,
                                    Address = x.Address,
                                    DevelopmentDate = Helper.NullDateToString2(x.DevelopmentDate),
                                    Telephone = x.Telephone,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = x.ModifiedOn.ToString()
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
        public async Task<IHttpActionResult> DatatableWarehouse()
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

            IEnumerable<Warehouse> list = Enumerable.Empty<Warehouse>();
            IEnumerable<WarehouseDTO> pagedData = Enumerable.Empty<WarehouseDTO>();

            IQueryable<Warehouse> query = db.Warehouses.Where(m => m.IsActive == true).AsQueryable();

            int recordsTotal = db.Warehouses.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                     .Where(m => m.Code.Contains(search)
                     || m.Name.Contains(search)
                     || m.CreatedBy.Contains(search)
                     || m.ModifiedBy.Contains(search)
                     );

                Dictionary<string, Func<Warehouse, object>> cols = new Dictionary<string, Func<Warehouse, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("IsActive", x => x.IsActive);
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
                                select new WarehouseDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Type = x.Type,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy,
                                    ModifiedOn = x.ModifiedOn.ToString()
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
        public async Task<IHttpActionResult> EditDetail(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            MaterialRequestDetailDTO detailDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                vMaterialRequestDetail vMaterialRequestDetail = await db.vMaterialRequestDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                if (vMaterialRequestDetail == null)
                {
                    throw new Exception("Data not found.");
                }

                detailDTO = new MaterialRequestDetailDTO
                {
                    ID = vMaterialRequestDetail.ID,
                    MaterialCode = vMaterialRequestDetail.MaterialCode,
                    MaterialName = vMaterialRequestDetail.MaterialName,
                    UoM = vMaterialRequestDetail.UoM,
                    RequestBagQty = Helper.FormatThousand(vMaterialRequestDetail.RequestBagQty),
                    ReceivedBagQty = Helper.FormatThousand(vMaterialRequestDetail.ReceivedBagQty),
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

            obj.Add("data", detailDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateDetail(PurchaseRequestDetailVM purchaseRequestDetailVM)
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
                    PurchaseRequestDetail requestDetail = null;
                    if (string.IsNullOrEmpty(purchaseRequestDetailVM.ID))
                    {
                        throw new Exception("Detail ID is required.");
                    }
                    else
                    {
                        requestDetail = await db.PurchaseRequestDetails.Where(m => m.ID.Equals(purchaseRequestDetailVM.ID)).FirstOrDefaultAsync();
                        if (requestDetail == null)
                        {
                            throw new Exception("ID is not recognized.");
                        }
                    }

                    vMaterialRequestDetail vMaterialRequestDetail = db.vMaterialRequestDetails.Where(m => m.ID.Equals(purchaseRequestDetailVM.ID)).FirstOrDefault();

                    // Cek Stock OUTSOURCE
                    // Start
                    PurchaseRequestHeader requestHeader = await db.PurchaseRequestHeaders.Where(s => s.ID.Equals(requestDetail.HeaderID)).FirstOrDefaultAsync();
                    if (requestHeader.SourceType.Equals("OUTSOURCE"))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(requestHeader.SourceCode)).FirstOrDefaultAsync();
                        string[] warehouseCodes = { };
                        if (!wh.Type.Equals("EMIX"))
                        {
                            warehouseCodes = new string[1] { requestHeader.SourceCode };
                        }
                        else
                        {
                            warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                        }

                        vStockWarehouse vStock = db.vStockWarehouses.Where(m => warehouseCodes.Contains(m.WarehouseCode) && m.MaterialCode.Equals(vMaterialRequestDetail.MaterialCode)).FirstOrDefault();
                        if (vStock == null)
                            throw new Exception("Material not recognized.");

                        decimal availableQty = vStock.TotalQuantity.Value;
                        decimal requestedQty = Convert.ToDecimal(vMaterialRequestDetail.RequestBagQty) * vStock.QtyPerBag;
                        if (availableQty < requestedQty)
                        {
                            ModelState.AddModelError("PurchaseRequest.BagQty", string.Format("Bag Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableQty)));
                        }
                    }
                    // End

                    if (purchaseRequestDetailVM.Qty >= 0)
                    {
                        int? receivedQty = vMaterialRequestDetail.ReceivedBagQty;
                        if (purchaseRequestDetailVM.Qty < receivedQty)
                        {
                            ModelState.AddModelError("PurchaseRequest.BagQty", string.Format("Bag Qty cannot below {0} Bag", receivedQty));
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

                    //update purchase request detail
                    //update receiving
                    requestDetail.Qty = purchaseRequestDetailVM.Qty * requestDetail.QtyPerBag;
                    requestDetail.ETA = Convert.ToDateTime(purchaseRequestDetailVM.ETA);
                    Receiving receiving = db.Receivings.Where(m => m.PurchaseRequestID.Equals(requestDetail.ID)).FirstOrDefault();
                    receiving.Qty = requestDetail.Qty;
                    receiving.ETA = requestDetail.ETA;

                    //otomatis close apabila request dan received qty sudah sama, dan received qty sudah sama dengan putaway qty
                    if (purchaseRequestDetailVM.Qty == vMaterialRequestDetail.ReceivedBagQty)
                    {
                        if (vMaterialRequestDetail.ReceivedBagQty == vMaterialRequestDetail.PutawayBagQty)
                        {
                            receiving.TransactionStatus = "CLOSED";
                        }
                    }
                    else if (purchaseRequestDetailVM.Qty > vMaterialRequestDetail.ReceivedBagQty)
                    {
                        if (purchaseRequestDetailVM.Qty > vMaterialRequestDetail.PutawayBagQty)
                        {
                            receiving.TransactionStatus = "PROGRESS";
                        }
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Update detail succeeded.";
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
    }
}
