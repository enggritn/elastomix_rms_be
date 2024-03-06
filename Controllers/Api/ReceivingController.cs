using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
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
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace WMS_BE.Controllers.Api
{
    public class ReceivingController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> Datatable()
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

            IEnumerable<Receiving> list = Enumerable.Empty<Receiving>();
            IEnumerable<ReceivingDTO> pagedData = Enumerable.Empty<ReceivingDTO>();

            string date = request["date"].ToString();
            string warehouseCode = request["warehouseCode"].ToString();
            string sourceType = request["sourceType"].ToString();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<Receiving> query;



            if (!string.IsNullOrEmpty(sourceType))
            {
                query = db.Receivings.Where(s => DbFunctions.TruncateTime(s.ETA) <= DbFunctions.TruncateTime(filterDate)
                && s.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code.Equals(warehouseCode)
                && s.PurchaseRequestDetail.PurchaseRequestHeader.SourceType.Equals(sourceType)
                && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")));
            }
            else
            {
                query = db.Receivings.Where(s => DbFunctions.TruncateTime(s.ETA) <= DbFunctions.TruncateTime(filterDate)
                && s.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code.Equals(warehouseCode)
                && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")));
            }

            int recordsTotal = query.Count();

            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.RefNumber.Contains(search)
                        || m.PurchaseRequestDetail.PurchaseRequestHeader.Code.Contains(search)
                        );

                Dictionary<string, Func<Receiving, object>> cols = new Dictionary<string, Func<Receiving, object>>();
                cols.Add("DocumentNo", x => x.PurchaseRequestDetail.PurchaseRequestHeader.Code);
                cols.Add("RefNumber", x => x.RefNumber);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("OutstandingQty", x => Convert.ToInt32((x.Qty / x.QtyPerBag)) - Convert.ToInt32(x.ReceivingDetails.Sum(i => i.Qty / i.QtyPerBag)));
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("SourceType", x => x.PurchaseRequestDetail.PurchaseRequestHeader.SourceType);
                cols.Add("SourceCode", x => x.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode);
                cols.Add("SourceName", x => x.PurchaseRequestDetail.PurchaseRequestHeader.SourceName);
                cols.Add("ETA", x => x.ETA);

                query = query.OrderBy(m => m.PurchaseRequestDetail.PurchaseRequestHeader.Code);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from receiving in list
                                select new ReceivingDTO
                                {
                                    ID = receiving.ID,
                                    DocumentNo = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                                    RefNumber = receiving.RefNumber,
                                    SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
                                    SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
                                    SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                                    ETA = Helper.NullDateToString(receiving.ETA),
                                    WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
                                    WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
                                    MaterialCode = receiving.MaterialCode,
                                    MaterialName = receiving.MaterialName,
                                    Qty = Helper.FormatThousand(receiving.Qty),
                                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                                    UoM = receiving.UoM,
                                    OutstandingQty = Helper.FormatThousand(Convert.ToInt32((receiving.Qty / receiving.QtyPerBag)) - Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty / i.QtyPerBag)))
                                    //QtyPerBag = Helper.FormatThousand((receiving.UoM.Equals("KG") ? receiving.QtyPerBag : receiving.QtyPerBag * receiving.PurchaseRequestDetail.PoRate)),
                                    //BagQty = Helper.FormatThousand((receiving.UoM.Equals("KG") ? receiving.Qty : (receiving.Qty * receiving.PurchaseRequestDetail.PoRate)) / (receiving.UoM.Equals("KG") ? receiving.QtyPerBag : receiving.QtyPerBag * receiving.PurchaseRequestDetail.PoRate)),
                                    //Qty = Helper.FormatThousand(receiving.UoM.Equals("KG") ? receiving.Qty : receiving.Qty * receiving.PurchaseRequestDetail.PoRate),
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
        public async Task<IHttpActionResult> DatatableDetailReceiving()
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
            string sourceType = request["sourceType"].ToString();

            IEnumerable<ReceivingDetail> list = Enumerable.Empty<ReceivingDetail>();
            IEnumerable<ReceivingDetailDTO> pagedData = Enumerable.Empty<ReceivingDetailDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<ReceivingDetail> query;

            if (!string.IsNullOrEmpty(sourceType) && !string.IsNullOrEmpty(warehouseCode))
            {
                query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate)
                        && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode.Equals(warehouseCode)
                        && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType.Equals(sourceType));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourceType))
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate)
                            && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType.Equals(sourceType));
                }
                else if (!string.IsNullOrEmpty(warehouseCode))
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate)
                            && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode.Equals(warehouseCode));
                }
                else
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ReceivedOn) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();

            //IQueryable<ReceivingDetail> query = db.ReceivingDetails.Where(s => s.HeaderID.Equals("")).AsQueryable();

            //int recordsTotal = db.ReceivingDetails.Where(s => s.HeaderID.Equals("")).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.DoNo.Contains(search)
                        || m.LotNo.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<ReceivingDetail, object>> cols = new Dictionary<string, Func<ReceivingDetail, object>>();
                cols.Add("SourceName", x => x.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName); 
                cols.Add("DocumentNo", x => x.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code);
                cols.Add("MaterialCode", x => x.Receiving.MaterialCode);
                cols.Add("MaterialName", x => x.Receiving.MaterialName);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ATA", x => x.ATA);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("COA", x => x.COA);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from detail in list
                                select new ReceivingDetailDTO
                                {
                                    ID = detail.ID,
                                    SourceName = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                                    DocumentNo = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                                    MaterialCode = detail.Receiving.MaterialCode,
                                    MaterialName = detail.Receiving.MaterialName,
                                    DoNo = detail.DoNo != null ? detail.DoNo : "",
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    UoM = detail.UoM,
                                    Remarks = detail.Remarks,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(detail.ReceivedOn),
                                    COA = detail.COA,
                                    CoaAction = !detail.COA ? true : false,
                                    InspectionAction = detail.Inspections.Count() > 0 || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004" ? false : true,
                                    JudgementAction = detail.NGQty > 0 ? true : false,
                                    PutawayAction = detail.Qty == detail.Putaways.Sum(i => i.PutawayQty) ? false : true,
                                    OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
                                    OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
                                    NGQty = Helper.FormatThousand(detail.NGQty),
                                    NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
                                    PutawayTotalQty = Helper.FormatThousand(detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(detail.Putaways.Sum(i => i.PutawayQty) / detail.QtyPerBag)),
                                    PutawayAvailableQty = Helper.FormatThousand((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag)),
                                    DestinationCode = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode,
                                    EditAction = detail.Putaways.Sum(i => i.PutawayQty) == 0
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
        public async Task<IHttpActionResult> DatatableDetailReceiving2()
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
            string warehouseCode = request["warehouseCode"].ToString();
            string sourceType = request["sourceType"].ToString();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;


            IEnumerable<vReceivingReport2> list = Enumerable.Empty<vReceivingReport2>();
            IEnumerable<ReceivingDetailDTOReport> pagedData = Enumerable.Empty<ReceivingDetailDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<vReceivingReport2> query;

            if (!string.IsNullOrEmpty(sourceType) && !string.IsNullOrEmpty(warehouseCode))
            {
                query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                        && s.SourceCode.Equals(warehouseCode)
                        && s.SourceType.Equals(sourceType));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourceType))
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceType.Equals(sourceType));
                }
                else if (!string.IsNullOrEmpty(warehouseCode))
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceCode.Equals(warehouseCode));
                }
                else
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vReceivingReport2, object>> cols = new Dictionary<string, Func<vReceivingReport2, object>>();
                cols.Add("DestinationName", x => x.DestinationName);
                cols.Add("RefNumber", x => x.RefNumber);
                cols.Add("SourceName", x => x.SourceName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("PerBag", x => x.PerBag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("ATA", x => x.ATA);
                cols.Add("TransactionStatus", x => x.TransactionStatus);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ReceivingDetailDTOReport
                                {
                                    DestinationName = detail.DestinationName,
                                    RefNumber = detail.RefNumber,
                                    SourceCode = detail.SourceCode,
                                    SourceType = detail.SourceType,
                                    SourceName = detail.SourceName,
                                    MaterialCode = detail.MaterialCode,
                                    MaterialName = detail.MaterialName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    PerBag = Helper.FormatThousand(detail.PerBag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(Convert.ToInt32(detail.Total)),
                                    Area = detail.Area != null ? detail.Area : "",
                                    RackNo = detail.RackNo != null ? detail.RackNo : "",
                                    DoNo = detail.DoNo,
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    TransactionStatus = detail.TransactionStatus,
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
        public async Task<IHttpActionResult> DatatableDetailReceiving3()
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
            string warehouseCode = request["warehouseCode"].ToString();
            string sourceType = request["sourceType"].ToString();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;


            IEnumerable<vReceivingReport3> list = Enumerable.Empty<vReceivingReport3>();
            IEnumerable<ReceivingDetailDTOReport3> pagedData = Enumerable.Empty<ReceivingDetailDTOReport3>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<vReceivingReport3> query;

            if (!string.IsNullOrEmpty(sourceType) && !string.IsNullOrEmpty(warehouseCode))
            {
                query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                        && s.SourceCode.Equals(warehouseCode)
                        && s.SourceType.Equals(sourceType));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourceType))
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceType.Equals(sourceType));
                }
                else if (!string.IsNullOrEmpty(warehouseCode))
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceCode.Equals(warehouseCode));
                }
                else
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        );

                Dictionary<string, Func<vReceivingReport3, object>> cols = new Dictionary<string, Func<vReceivingReport3, object>>();
                cols.Add("RefNumber", x => x.RefNumber);
                cols.Add("SourceName", x => x.SourceName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Schedule", x => x.Schedule);
                cols.Add("TotalQtyPo", x => x.TotalQtyPo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("QtyBag", x => x.QtyBag);
                cols.Add("Total", x => x.Total);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("Ok", x => x.Ok);
                cols.Add("NgDamage", x => x.NgDamage);
                cols.Add("COA", x => x.COA);
                cols.Add("StatusPo", x => x.StatusPo);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);
                cols.Add("QtyPutaway", x => x.QtyPutaway);
                cols.Add("Area", x => x.Area);
                cols.Add("RackNo", x => x.RackNo);
                cols.Add("Status", x => x.Status);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ReceivingDetailDTOReport3
                                {
                                    DestinationName = detail.DestinationName,
                                    RefNumber = detail.RefNumber,
                                    SourceCode = detail.SourceCode,
                                    SourceType = detail.SourceType,
                                    SourceName = detail.SourceName,
                                    MaterialCode = detail.MaterialCode,
                                    MaterialName = detail.MaterialName,
                                    Schedule = Helper.NullDateToString2(detail.Schedule),
                                    TotalQtyPo = Helper.FormatThousand(detail.TotalQtyPo),
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    QtyBag = Helper.FormatThousand(detail.QtyBag),
                                    Total = Helper.FormatThousand(Convert.ToInt32(detail.Total)),
                                    DoNo = detail.DoNo,
                                    Ok = Helper.FormatThousand(detail.Ok),
                                    NgDamage = Helper.FormatThousand(detail.NgDamage),
                                    COA = detail.COA,
                                    StatusPo = detail.StatusPo,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = detail.ReceivedOn,
                                    QtyPutaway = Helper.FormatThousand(detail.QtyPutaway),
                                    Area = detail.Area != null ? detail.Area : "",
                                    RackNo = detail.RackNo != null ? detail.RackNo : "",
                                    Status = detail.Status,
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
        public async Task<IHttpActionResult> GetDataReportReceiving(string date, string warehouse, string sourcetype)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(warehouse) && string.IsNullOrEmpty(sourcetype))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<ReceivingDetail> list = Enumerable.Empty<ReceivingDetail>();
            IEnumerable<ReceivingDetailDTO> pagedData = Enumerable.Empty<ReceivingDetailDTO>(); 
            
            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<ReceivingDetail> query;

            if (!string.IsNullOrEmpty(sourcetype) && !string.IsNullOrEmpty(warehouse))
            {
                query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                        && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode.Equals(warehouse)
                        && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType.Equals(sourcetype));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourcetype))
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                            && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType.Equals(sourcetype));
                }
                else if (!string.IsNullOrEmpty(warehouse))
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)                    
                            && s.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode.Equals(warehouse));
                }
                else
                {
                    query = db.ReceivingDetails.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<ReceivingDetail, object>> cols = new Dictionary<string, Func<ReceivingDetail, object>>();
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ATA", x => x.ATA);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("COA", x => x.COA);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ReceivingDetailDTO
                                {
                                    ID = detail.ID,
                                    SourceName = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                                    DocumentNo = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                                    MaterialCode = detail.Receiving.MaterialCode,
                                    MaterialName = detail.Receiving.MaterialName,
                                    DoNo = detail.DoNo != null ? detail.DoNo : "",
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    UoM = detail.UoM,
                                    Remarks = detail.Remarks,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(detail.ReceivedOn),
                                    COA = detail.COA,
                                    CoaAction = !detail.COA ? true : false,
                                    InspectionAction = detail.Inspections.Count() > 0 || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004" ? false : true,
                                    JudgementAction = detail.NGQty > 0 ? true : false,
                                    PutawayAction = detail.Qty == detail.Putaways.Sum(i => i.PutawayQty) ? false : true,
                                    OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
                                    OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
                                    NGQty = Helper.FormatThousand(detail.NGQty),
                                    NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
                                    PutawayTotalQty = Helper.FormatThousand(detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(detail.Putaways.Sum(i => i.PutawayQty) / detail.QtyPerBag)),
                                    PutawayAvailableQty = Helper.FormatThousand((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag)),
                                    DestinationCode = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode,
                                    EditAction = detail.Putaways.Sum(i => i.PutawayQty) == 0
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

        [HttpGet]
        public async Task<IHttpActionResult> GetDataReportReceiving2(string date, string warehouse, string sourcetype)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(warehouse) && string.IsNullOrEmpty(sourcetype))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vReceivingReport2> list = Enumerable.Empty<vReceivingReport2>();
            IEnumerable<ReceivingDetailDTOReport> pagedData = Enumerable.Empty<ReceivingDetailDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<vReceivingReport2> query;

            if (!string.IsNullOrEmpty(sourcetype) && !string.IsNullOrEmpty(warehouse))
            {
                query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                        && s.SourceCode.Equals(warehouse)
                        && s.SourceType.Equals(sourcetype));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourcetype))
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceType.Equals(sourcetype));
                }
                else if (!string.IsNullOrEmpty(warehouse))
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceCode.Equals(warehouse));
                }
                else
                {
                    query = db.vReceivingReport2.Where(s => DbFunctions.TruncateTime(s.ATA) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vReceivingReport2, object>> cols = new Dictionary<string, Func<vReceivingReport2, object>>();
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("PerBag", x => x.PerBag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("ATA", x => x.ATA);
                cols.Add("TransactionStatus", x => x.TransactionStatus);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ReceivingDetailDTOReport
                                {
                                    DestinationName = detail.DestinationName,
                                    RefNumber = detail.RefNumber != null ? detail.RackNo : "",
                                    SourceCode = detail.SourceCode,
                                    SourceType = detail.SourceType,
                                    SourceName = detail.SourceName,
                                    MaterialCode = detail.MaterialCode,
                                    MaterialName = detail.MaterialName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    PerBag = Helper.FormatThousand(detail.PerBag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(Convert.ToInt32(detail.Total)),
                                    Area = detail.Area != null ? detail.Area : "",
                                    RackNo = detail.RackNo != null ? detail.RackNo : "",
                                    DoNo = detail.DoNo != null ? detail.RackNo : "",
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    TransactionStatus = detail.TransactionStatus,
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

            obj.Add("list2", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetDataReportReceiving3(string date, string warehouse, string sourcetype)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(warehouse) && string.IsNullOrEmpty(sourcetype))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vReceivingReport3> list = Enumerable.Empty<vReceivingReport3>();
            IEnumerable<ReceivingDetailDTOReport3> pagedData = Enumerable.Empty<ReceivingDetailDTOReport3>();

            DateTime filterDate = Convert.ToDateTime(date);
            IQueryable<vReceivingReport3> query;

            if (!string.IsNullOrEmpty(sourcetype) && !string.IsNullOrEmpty(warehouse))
            {
                query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                        && s.SourceCode.Equals(warehouse)
                        && s.SourceType.Equals(sourcetype));
            }
            else
            {
                if (!string.IsNullOrEmpty(sourcetype))
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceType.Equals(sourcetype));
                }
                else if (!string.IsNullOrEmpty(warehouse))
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate)
                            && s.SourceCode.Equals(warehouse));
                }
                else
                {
                    query = db.vReceivingReport3.Where(s => DbFunctions.TruncateTime(s.Schedule) == DbFunctions.TruncateTime(filterDate));
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vReceivingReport3, object>> cols = new Dictionary<string, Func<vReceivingReport3, object>>();
                cols.Add("RefNumber", x => x.RefNumber);
                cols.Add("SourceName", x => x.SourceName);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Schedule", x => x.Schedule);
                cols.Add("TotalQtyPo", x => x.TotalQtyPo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("QtyBag", x => x.QtyBag);
                cols.Add("Total", x => x.Total);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("Ok", x => x.Ok);
                cols.Add("NgDamage", x => x.NgDamage);
                cols.Add("COA", x => x.COA);
                cols.Add("StatusPo", x => x.StatusPo);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);
                cols.Add("QtyPutaway", x => x.QtyPutaway);
                cols.Add("Area", x => x.Area);
                cols.Add("RackNo", x => x.RackNo);
                cols.Add("Status", x => x.Status);
                cols.Add("Remarks", x => x.Remarks);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ReceivingDetailDTOReport3
                                {
                                    DestinationName = detail.DestinationName,
                                    RefNumber = detail.RefNumber,
                                    SourceCode = detail.SourceCode,
                                    SourceType = detail.SourceType,
                                    SourceName = detail.SourceName,
                                    MaterialCode = detail.MaterialCode,
                                    MaterialName = detail.MaterialName,
                                    Schedule = Helper.NullDateToString2(detail.Schedule),
                                    TotalQtyPo = Helper.FormatThousand(detail.TotalQtyPo),
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    QtyBag = Helper.FormatThousand(detail.QtyBag),
                                    Total = Helper.FormatThousand(Convert.ToInt32(detail.Total)),
                                    DoNo = detail.DoNo,
                                    Ok = Helper.FormatThousand(detail.Ok),
                                    NgDamage = Helper.FormatThousand(detail.NgDamage),
                                    COA = detail.COA,
                                    StatusPo = detail.StatusPo,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = detail.ReceivedOn,
                                    QtyPutaway = Helper.FormatThousand(detail.QtyPutaway),
                                    Area = detail.Area != null ? detail.Area : "",
                                    RackNo = detail.RackNo != null ? detail.RackNo : "",
                                    Status = detail.Status,
                                    Remarks = detail.Remarks,
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

            obj.Add("list3", pagedData);
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
            ReceivingDTO receivingDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                Receiving receiving = await db.Receivings.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (receiving == null)
                {
                    throw new Exception("Data not found.");
                }


                receivingDTO = new ReceivingDTO
                {
                    ID = receiving.ID,
                    RefNumber = receiving.RefNumber,
                    SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
                    SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
                    SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                    WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
                    WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
                    WarehouseType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Type,
                    MaterialCode = receiving.MaterialCode,
                    MaterialName = receiving.MaterialName,
                    Qty = Helper.FormatThousand(receiving.Qty),
                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                    UoM = receiving.UoM,
                    TransactionStatus = receiving.TransactionStatus,
                    ETA = Helper.NullDateToString(receiving.ETA),
                    DestinationCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode
                };

                RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

                receivingDTO.UoM2 = "KG";

                decimal Qty2 = 0;
                decimal ReceivedQty = receiving.ReceivingDetails.Sum(i => i.Qty);
                int ReceivedBagQty = 0;
                decimal AvailableQty = 0;
                int AvailableBagQty = 0;
                decimal QtyPerBag = 0;

                if (receiving.UoM.ToUpper().Equals("L"))
                {
                    QtyPerBag = receiving.QtyPerBag * rm.PoRate;
                    Qty2 = Convert.ToInt32(receiving.Qty / receiving.QtyPerBag) * QtyPerBag;
                    //Qty2 = receiving.Qty;
                }
                else
                {
                    QtyPerBag = receiving.QtyPerBag;
                    //Qty2 = receiving.Qty * receiving.QtyPerBag;
                    Qty2 = receiving.Qty;
                }

                AvailableQty = Qty2 - receiving.ReceivingDetails.Sum(i => i.Qty);

                receivingDTO.Qty2 = Helper.FormatThousand(Qty2);
                receivingDTO.QtyPerBag2 = Helper.FormatThousand(QtyPerBag);
                ReceivedBagQty = Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty) / QtyPerBag);

                AvailableBagQty = Convert.ToInt32(AvailableQty / QtyPerBag);


                receivingDTO.ReceivedQty = Helper.FormatThousand(ReceivedQty);
                receivingDTO.ReceivedBagQty = Helper.FormatThousand(ReceivedBagQty);



                receivingDTO.AvailableQty = Helper.FormatThousand(AvailableQty);
                receivingDTO.AvailableBagQty = Helper.FormatThousand(AvailableBagQty);


                receivingDTO.DefaultLot = DateTime.Now.ToString("yyyMMdd").Substring(1);


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

            obj.Add("data", receivingDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDetailById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ReceivingDetailDTO receivingDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                ReceivingDetail receiving = await db.ReceivingDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (receiving == null)
                {
                    throw new Exception("Data not found.");
                }

                receivingDTO = new ReceivingDetailDTO
                {
                    ID = receiving.ID,
                    StockCode = receiving.StockCode,
                    DoNo = receiving.DoNo,
                    LotNo = receiving.LotNo,
                    InDate = Helper.NullDateToString(receiving.InDate),
                    ExpDate = Helper.NullDateToString(receiving.ExpDate),
                    Qty = Helper.FormatThousand(receiving.Qty),
                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                    UoM = receiving.UoM,
                    ATA = Helper.NullDateToString(receiving.ATA),
                    ReceivedBy = receiving.ReceivedBy,
                    ReceivedOn = Helper.NullDateToString(receiving.ReceivedOn),
                    COA = receiving.COA,
                    LastSeries = receiving.LastSeries.ToString(),
                    FotoCOA = receiving.FotoCOA,
                    LastFotoCOA = receiving.LastFotoCOA.ToString()
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

            obj.Add("datadetail", receivingDTO);
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

            IEnumerable<ReceivingDetail> list = Enumerable.Empty<ReceivingDetail>();
            IEnumerable<ReceivingDetailDTO> pagedData = Enumerable.Empty<ReceivingDetailDTO>();

            IQueryable<ReceivingDetail> query = db.ReceivingDetails.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = db.ReceivingDetails.Where(s => s.HeaderID.Equals(HeaderID)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.DoNo.Contains(search)
                        || m.LotNo.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<ReceivingDetail, object>> cols = new Dictionary<string, Func<ReceivingDetail, object>>();
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ATA", x => x.ATA);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("COA", x => x.COA);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from detail in list
                                select new ReceivingDetailDTO
                                {
                                    ID = detail.ID,
                                    MaterialCode = detail.Receiving.MaterialCode,
                                    DoNo = detail.DoNo != null ? detail.DoNo : "",
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    UoM = detail.UoM,
                                    Remarks = detail.Remarks,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(detail.ReceivedOn),
                                    COA = detail.COA,
                                    CoaAction = !detail.COA ? true : false,
                                    InspectionAction = detail.Inspections.Count() > 0 || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004" ? false : true,
                                    JudgementAction = detail.NGQty > 0 ? true : false,
                                    PutawayAction = detail.Qty == detail.Putaways.Sum(i => i.PutawayQty) ? false : true,
                                    OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
                                    OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
                                    NGQty = Helper.FormatThousand(detail.NGQty),
                                    NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
                                    PutawayTotalQty = Helper.FormatThousand(detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(detail.Putaways.Sum(i => i.PutawayQty) / detail.QtyPerBag)),
                                    PutawayAvailableQty = Helper.FormatThousand((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)),
                                    PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag)),
                                    DestinationCode = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode,
                                    EditAction = detail.Putaways.Sum(i => i.PutawayQty) == 0
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
        public async Task<IHttpActionResult> Receive(ReceivingDetailVM receivingDetailVM)
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
                    Receiving receiving = await db.Receivings.Where(s => s.ID.Equals(receivingDetailVM.ReceivingID)).FirstOrDefaultAsync();

                    if (receiving == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

                    if (rm == null)
                    {
                        throw new Exception("Raw Material is not recognized.");
                    }

                    if (string.IsNullOrEmpty(receivingDetailVM.DoNo))
                    {
                        ModelState.AddModelError("Receiving.ReceiveDoNo", "Do Number can not be empty.");
                    }

                    if (string.IsNullOrEmpty(receivingDetailVM.LotNo))
                    {
                        ModelState.AddModelError("Receiving.ReceiveLotNo", "Lot Number can not be empty.");
                    }

                    if (receivingDetailVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.ReceiveActualQty", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32((receiving.Qty - receiving.ReceivingDetails.Sum(i => i.Qty)) / receiving.QtyPerBag);
                        if (receivingDetailVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.ReceiveActualQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
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

                    ReceivingDetail receivingDetail = new ReceivingDetail();
                    receivingDetail.ID = Helper.CreateGuid("RCd");
                    receivingDetail.HeaderID = receiving.ID;
                    receivingDetail.UoM = "KG";

                    if (receiving.UoM.ToUpper().Equals("L"))
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag * rm.PoRate;
                    }
                    else
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag;
                    }

                    receivingDetail.Qty = receivingDetailVM.BagQty * receivingDetail.QtyPerBag;

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    receivingDetail.ATA = transactionDate;
                    receivingDetail.InDate = receivingDetail.ATA;
                    int ShelfLife = Convert.ToInt32(Regex.Match(rm.ShelfLife, @"\d+").Value);
                    int days = 0;

                    string LifeRange = Regex.Replace(rm.ShelfLife, @"[\d-]", string.Empty).ToString();

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

                    if (!receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationType.Equals("OUTSOURCE"))
                    {
                        receivingDetail.ExpDate = receivingDetail.InDate.AddDays(days);
                        receivingDetail.LotNo = receivingDetailVM.LotNo.Trim().ToString();
                        receivingDetail.StockCode = string.Format("{0}{1}{2}{3}{4}", receiving.MaterialCode, Helper.FormatThousand(receivingDetail.QtyPerBag), receivingDetail.LotNo, receivingDetail.InDate.ToString("yyyyMMdd").Substring(1), receivingDetail.ExpDate.Value.ToString("yyyyMMdd").Substring(2));
                    }
                    else
                    {
                        receivingDetail.StockCode = string.Format("{0}{1}{2}", receiving.MaterialCode, Helper.FormatThousand(receivingDetail.QtyPerBag), receivingDetail.InDate.ToString("yyyyMMdd").Substring(1));
                    }

                    receivingDetail.DoNo = receivingDetailVM.DoNo;

                    receivingDetail.ReceivedBy = activeUser;
                    receivingDetail.ReceivedOn = transactionDate;
                    receivingDetail.COA = false;
                    receivingDetail.NGQty = 0;

                    int BagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);

                    //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
                    int startSeries = 0;
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(receivingDetail.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                        lastSeries = await db.ReceivingDetails.Where(m => m.Receiving.MaterialCode.Equals(receiving.MaterialCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.LotNo.Equals(receivingDetail.LotNo)).OrderByDescending(m => m.ReceivedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }

                    lastSeries = startSeries + BagQty - 1;


                    receivingDetail.LastSeries = lastSeries;

                    db.ReceivingDetails.Add(receivingDetail);

                    //add to Log Print RM
                    LogPrintRM logPrintRM = new LogPrintRM();

                    if (!receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationType.Equals("OUTSOURCE"))
                    {
                        logPrintRM.LotNumber = receivingDetail.LotNo;
                        logPrintRM.ExpiredDate = receivingDetail.ExpDate.Value;
                    }
                    logPrintRM.ID = Helper.CreateGuid("LOG");
                    logPrintRM.Remarks = "Receiving RM";
                    logPrintRM.StockCode = receivingDetail.StockCode;
                    logPrintRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                    logPrintRM.MaterialName = receivingDetail.Receiving.MaterialName;
                    logPrintRM.InDate = receivingDetail.InDate;
                    logPrintRM.StartSeries = startSeries;
                    logPrintRM.LastSeries = lastSeries;
                    logPrintRM.PrintDate = DateTime.Now;

                    db.LogPrintRMs.Add(logPrintRM);



                    receiving.TransactionStatus = "PROGRESS";
                    if (receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004")
                    {
                        receivingDetail.COA = true;
                        BinRack binRack = null;
                        binRack = await db.BinRacks.Where(m => m.WarehouseCode.Equals(receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Receiving.BinRackID", "BinRack is not recognized.");
                        }
                        Putaway putaway = new Putaway();
                        putaway.ID = Helper.CreateGuid("P");
                        putaway.ReceivingDetailID = receivingDetail.ID;
                        putaway.PutawayMethod = "MANUAL";

                        //TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                        //DateTime now = DateTime.Now;
                        //DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                        putaway.PutOn = transactionDate;
                        putaway.PutBy = activeUser;
                        putaway.BinRackID = binRack.ID;
                        putaway.BinRackCode = binRack.Code;
                        putaway.BinRackName = binRack.Name;
                        putaway.PutawayQty = receivingDetailVM.BagQty * receivingDetail.QtyPerBag;

                        db.Putaways.Add(putaway);

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same
                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                            stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
                            stockRM.Code = receivingDetail.StockCode;
                            stockRM.InDate = receivingDetail.InDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = receivingDetail.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);

                        }

                        #region pemotongan stock sourcenya dipindahin ke materq langsung
                        //var source = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode;
                        //BinRack binRack1 = db.BinRacks.Where(x => x.WarehouseCode == source).FirstOrDefault();
                        ////vProductMaster vStocks = await db.vProductMasters.Where(m => m.MaterialCode.Equals(prd.MaterialCode)).FirstOrDefaultAsync();
                        //vStockAll stockAll = db.vStockAlls.Where(x => x.BinRackAreaCode == binRack1.BinRackAreaCode && x.MaterialCode == stockRM.MaterialCode).SingleOrDefault();
                        //if (stockAll == null)
                        //{
                        //    throw new Exception("Stock is not found.");
                        //}
                        //if (stockAll.Type.Equals("RM"))
                        //{
                        //    decimal pickQty = putaway.PutawayQty;
                        //    StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        //    stock.Quantity -= pickQty;
                        //}
                        //else if (stockAll.Type.Equals("SFG"))
                        //{
                        //    decimal pickQty = putaway.PutawayQty;
                        //    StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        //    stock.Quantity -= pickQty;
                        //}

                        //update receiving plan status if all quantity have been received and putaway
                        #endregion
                        receiving.TransactionStatus = "CLOSED";
                    }

                    if (receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType == "OUTSOURCE")
                    {
                        var source = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode;
                        BinRack binRack1 = db.BinRacks.Where(x => x.WarehouseCode == source).FirstOrDefault();

                        List<vStockAll> stocks = db.vStockAlls.Where(m => m.Quantity > 0 && m.BinRackAreaCode == binRack1.BinRackAreaCode && m.MaterialCode == receiving.MaterialCode).OrderBy(m => m.ReceivedAt).ToList();

                        decimal countqty = receivingDetail.Qty;
                        decimal pickQty = receivingDetail.Qty;
                        foreach (vStockAll stock in stocks)
                        {
                            if (countqty > 0)
                            {
                                if (stock.Type.Equals("RM"))
                                {
                                    StockRM stockrm = db.StockRMs.Where(m => m.ID.Equals(stock.ID)).FirstOrDefault();
                                    if (pickQty >= countqty)
                                    {
                                        decimal qty = stockrm.Quantity;
                                        decimal stockrmqty;
                                        if (countqty < pickQty)
                                        {
                                            stockrmqty = stockrm.Quantity - countqty;
                                        }
                                        else
                                        {
                                            stockrmqty = stockrm.Quantity - pickQty;
                                        }

                                        if (stockrmqty > 0)
                                        {
                                            stockrm.Quantity = stockrmqty;
                                            countqty = countqty - stockrmqty;
                                        }
                                        else 
                                        { 
                                            if (stockrm.Quantity > countqty)
                                            {
                                                stockrm.Quantity = stockrm.Quantity - countqty;
                                                countqty = countqty - stockrm.Quantity;
                                            }
                                            else
                                            {
                                                if (stockrm.Quantity > 0)
                                                {
                                                    stockrm.Quantity = 0;
                                                    countqty = countqty - (pickQty - qty);
                                                }
                                                else
                                                {
                                                    stockrm.Quantity = 0;
                                                    countqty = countqty - (pickQty - stockrm.Quantity);
                                                }
                                            }
                                        }                                        
                                    }                                    
                                }
                                else if (stock.Type.Equals("SFG"))
                                {
                                    StockSFG stockfg = db.StockSFGs.Where(m => m.ID.Equals(stock.ID)).FirstOrDefault();
                                    if (pickQty >= countqty)
                                    {
                                        decimal qty = stockfg.Quantity;
                                        decimal stockfgqty;
                                        if (countqty < pickQty)
                                        {
                                            stockfgqty = stockfg.Quantity - countqty;
                                        }
                                        else
                                        {
                                            stockfgqty = stockfg.Quantity - pickQty;
                                        }

                                        if (stockfgqty > 0)
                                        {
                                            stockfg.Quantity = stockfgqty;
                                            countqty = countqty - stockfgqty;
                                        }
                                        else
                                        {
                                            if (stockfg.Quantity > countqty)
                                            {
                                                stockfg.Quantity = stockfg.Quantity - countqty;
                                                countqty = countqty - stockfg.Quantity;
                                            }
                                            else
                                            {
                                                if (stockfg.Quantity > 0)
                                                {
                                                    stockfg.Quantity = 0;
                                                    countqty = countqty - (pickQty - qty);
                                                }
                                                else
                                                {
                                                    stockfg.Quantity = 0;
                                                    countqty = countqty - (pickQty - stockfg.Quantity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }                               
                        }
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
        public async Task<IHttpActionResult> EditReceive(ReceivingDetailVM receivingDetailVM)
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
                    Receiving receiving = await db.Receivings.Where(s => s.ID.Equals(receivingDetailVM.ReceivingID)).FirstOrDefaultAsync();

                    if (receiving == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    ReceivingDetail receivingDetail = db.ReceivingDetails.Where(s => s.ID.Equals(receivingDetailVM.ID)).FirstOrDefault();
                    if(receivingDetail == null)
                    {
                        throw new Exception("Receiving Detail is not recognized.");
                    }


                    decimal totalPutaway = receivingDetail.Putaways.Sum(i => i.PutawayQty);

                    if(totalPutaway > 0)
                    {
                        throw new Exception("Can not edit receiving, putaway already processed.");
                    }

                    RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

                    if (rm == null)
                    {
                        throw new Exception("Raw Material is not recognized.");
                    }

                    if (string.IsNullOrEmpty(receivingDetailVM.DoNo))
                    {
                        ModelState.AddModelError("Receiving.ReceiveDoNo", "Do Number can not be empty.");
                    }

                    if (string.IsNullOrEmpty(receivingDetailVM.LotNo))
                    {
                        ModelState.AddModelError("Receiving.ReceiveLotNo", "Lot Number can not be empty.");
                    }

                    if (receivingDetailVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.ReceiveActualQty", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32((receiving.Qty - receiving.ReceivingDetails.Sum(i => i.Qty)) / receiving.QtyPerBag) + Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
                        if (receivingDetailVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.ReceiveActualQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
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


                    if (receiving.UoM.ToUpper().Equals("L"))
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag * rm.PoRate;
                    }
                    else
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag;
                    }

                    receivingDetail.Qty = receivingDetailVM.BagQty * receivingDetail.QtyPerBag;

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    receivingDetail.ATA = transactionDate;

                    if (!receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationType.Equals("OUTSOURCE"))
                    {
                        receivingDetail.LotNo = receivingDetailVM.LotNo.Trim().ToString();
                        receivingDetail.StockCode = string.Format("{0}{1}{2}{3}{4}", receiving.MaterialCode, Helper.FormatThousand(receivingDetail.QtyPerBag), receivingDetail.LotNo, receivingDetail.InDate.ToString("yyyyMMdd").Substring(1), receivingDetail.ExpDate.Value.ToString("yyyyMMdd").Substring(2));
                    }
                    else
                    {
                        receivingDetail.StockCode = string.Format("{0}{1}{2}", receiving.MaterialCode, Helper.FormatThousand(receivingDetail.QtyPerBag), receivingDetail.InDate.ToString("yyyyMMdd").Substring(1));
                    }

                    receivingDetail.DoNo = receivingDetailVM.DoNo;

                    receivingDetail.ReceivedBy = activeUser;
                    receivingDetail.ReceivedOn = transactionDate;
                    receivingDetail.NGQty = 0;
                    receivingDetail.Remarks = "";


                    //reset inspection
                    foreach (Inspection inspection in receivingDetail.Inspections.ToList())
                        db.Inspections.Remove(inspection);

                    //reset judgement
                    foreach (Judgement judgement in receivingDetail.Judgements.ToList())
                        db.Judgements.Remove(judgement);


                    int BagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);

                    //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
                    int startSeries = 0;
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(receivingDetail.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                        lastSeries = await db.ReceivingDetails.Where(m => m.Receiving.MaterialCode.Equals(receiving.MaterialCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.LotNo.Equals(receivingDetail.LotNo)).OrderByDescending(m => m.ReceivedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }

                    lastSeries = startSeries + BagQty - 1;


                    receivingDetail.LastSeries = lastSeries;


                    //add to Log Print RM
                    LogPrintRM logPrintRM = new LogPrintRM();

                    if (!receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationType.Equals("OUTSOURCE"))
                    {
                        logPrintRM.LotNumber = receivingDetail.LotNo;
                        logPrintRM.ExpiredDate = receivingDetail.ExpDate.Value;
                    }
                    logPrintRM.ID = Helper.CreateGuid("LOG");
                    logPrintRM.Remarks = "Receiving RM";
                    logPrintRM.StockCode = receivingDetail.StockCode;
                    logPrintRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                    logPrintRM.MaterialName = receivingDetail.Receiving.MaterialName;
                    logPrintRM.InDate = receivingDetail.InDate;
                    logPrintRM.StartSeries = startSeries;
                    logPrintRM.LastSeries = lastSeries;
                    logPrintRM.PrintDate = DateTime.Now;

                    db.LogPrintRMs.Add(logPrintRM);



                    receiving.TransactionStatus = "PROGRESS";
                    if (receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004")
                    {
                        receivingDetail.COA = true;
                        BinRack binRack = null;
                        binRack = await db.BinRacks.Where(m => m.WarehouseCode.Equals(receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Receiving.BinRackID", "BinRack is not recognized.");
                        }
                        Putaway putaway = new Putaway();
                        putaway.ID = Helper.CreateGuid("P");
                        putaway.ReceivingDetailID = receivingDetail.ID;
                        putaway.PutawayMethod = "MANUAL";

                        //TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                        //DateTime now = DateTime.Now;
                        //DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                        putaway.PutOn = transactionDate;
                        putaway.PutBy = activeUser;
                        putaway.BinRackID = binRack.ID;
                        putaway.BinRackCode = binRack.Code;
                        putaway.BinRackName = binRack.Name;
                        putaway.PutawayQty = receivingDetailVM.BagQty * receivingDetail.QtyPerBag;

                        db.Putaways.Add(putaway);

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        //StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                            stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
                            stockRM.Code = receivingDetail.StockCode;
                            //stockRM.LotNumber = receivingDetail.LotNo;
                            stockRM.InDate = receivingDetail.InDate;
                            //stockRM.ExpiredDate = receivingDetail.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = receivingDetail.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);

                        }
                        #region pemotongan stock sourcenya dipindahin ke materq langsung
                        //var source = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode;
                        //BinRack binRack1 = db.BinRacks.Where(x => x.WarehouseCode == source).FirstOrDefault();
                        ////vProductMaster vStocks = await db.vProductMasters.Where(m => m.MaterialCode.Equals(prd.MaterialCode)).FirstOrDefaultAsync();
                        //vStockAll stockAll = db.vStockAlls.Where(x => x.BinRackAreaCode == binRack1.BinRackAreaCode && x.MaterialCode == stockRM.MaterialCode).SingleOrDefault();
                        //if (stockAll == null)
                        //{
                        //    throw new Exception("Stock is not found.");
                        //}
                        //if (stockAll.Type.Equals("RM"))
                        //{
                        //    decimal pickQty = putaway.PutawayQty;
                        //    StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        //    stock.Quantity -= pickQty;
                        //}
                        //else if (stockAll.Type.Equals("SFG"))
                        //{
                        //    decimal pickQty = putaway.PutawayQty;
                        //    StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        //    stock.Quantity -= pickQty;
                        //}

                        //update receiving plan status if all quantity have been received and putaway
                        #endregion
                        receiving.TransactionStatus = "CLOSED";
                    }


                    await db.SaveChangesAsync();


                    status = true;
                    message = "Edit Receiving succeeded.";

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
        public async Task<IHttpActionResult> UpdateCOA(CoaVM coaVM)
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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(coaVM.ID)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
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

                    receivingDetail.COA = coaVM.IsChecked;

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Update COA succeeded.";
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
        public async Task<IHttpActionResult> Inspection(InspectionVM inspectionVM)
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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(inspectionVM.ID)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        ModelState.AddModelError("Receiving.ID", "Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    if (receivingDetail.Inspections.Count() > 0)
                    {
                        throw new Exception("Already inspected, please proceed to next step.");
                    }

                    int NGBagQty = 0;
                    int remarkNGQty = 0;

                    if (inspectionVM.OKBagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.InspectionQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
                        if (inspectionVM.OKBagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.InspectionQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                        }

                        NGBagQty = availableBagQty - inspectionVM.OKBagQty;

                        if (NGBagQty > 0)
                        {
                            remarkNGQty = inspectionVM.DamageQty + inspectionVM.WetQty + inspectionVM.ContaminationQty;
                            if (remarkNGQty <= 0)
                            {
                                ModelState.AddModelError("Receiving.NGBagQty", "Please input NG Detail below.");
                            }
                            else
                            {
                                if (remarkNGQty != NGBagQty)
                                {
                                    ModelState.AddModelError("Receiving.NGBagQty", "NG Detail Bag Qty must be completed.");
                                }
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

                    NGBagQty = (Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag)) - inspectionVM.OKBagQty;

                    remarkNGQty = inspectionVM.DamageQty + inspectionVM.WetQty + inspectionVM.ContaminationQty;

                    string remarks = "";

                    if (inspectionVM.DamageQty > 0)
                    {
                        remarks += "Damage : " + inspectionVM.DamageQty.ToString();
                    }

                    if (inspectionVM.WetQty > 0)
                    {
                        remarks += ", Wet : " + inspectionVM.WetQty.ToString();
                    }

                    if (inspectionVM.ContaminationQty > 0)
                    {
                        remarks += ", Foreign Contamination : " + inspectionVM.ContaminationQty.ToString();
                    }

                    if (remarkNGQty == NGBagQty)
                    {
                        receivingDetail.Remarks = remarks;
                    }

                    receivingDetail.NGQty = NGBagQty * receivingDetail.QtyPerBag;



                    int startSeries = receivingDetail.LastSeries - Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
                    //insert to inspection log

                    int OKBagQty = Convert.ToInt32(inspectionVM.OKBagQty);


                    Inspection inspection = new Inspection();
                    inspection.ID = Helper.CreateGuid("I");
                    inspection.ReceivingDetailID = receivingDetail.ID;
                    inspection.InspectionMethod = "MANUAL";

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    inspection.InspectedOn = transactionDate;
                    inspection.InspectedBy = activeUser;
                    inspection.LastSeries = startSeries + OKBagQty;
                    inspection.InspectionQty = OKBagQty * receivingDetail.QtyPerBag;

                    db.Inspections.Add(inspection);

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Inspection succeeded.";

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
        public async Task<IHttpActionResult> Judgement(JudgementVM judgementVM)
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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(judgementVM.ID)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
                    }


                    if (judgementVM.OKBagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.JudgementQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32(receivingDetail.NGQty / receivingDetail.QtyPerBag);
                        if (judgementVM.OKBagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.JudgementQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
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


                    int lastSeries = await db.Judgements.Where(m => m.ReceivingDetailID.Equals(receivingDetail.ID)).OrderByDescending(m => m.JudgeOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    if (lastSeries == 0)
                    {
                        lastSeries = await db.Inspections.Where(m => m.ReceivingDetailID.Equals(receivingDetail.ID)).OrderByDescending(m => m.InspectedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    }



                    lastSeries += judgementVM.OKBagQty;

                    //insert log judgement

                    Judgement judgement = new Judgement();
                    judgement.ID = Helper.CreateGuid("J");
                    judgement.ReceivingDetailID = receivingDetail.ID;
                    judgement.JudgementMethod = "MANUAL";

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    judgement.JudgeOn = transactionDate;
                    judgement.JudgeBy = activeUser;
                    judgement.LastSeries = lastSeries;
                    judgement.JudgementQty = judgementVM.OKBagQty * receivingDetail.QtyPerBag;

                    db.Judgements.Add(judgement);


                    receivingDetail.NGQty -= judgement.JudgementQty;

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Judgement succeeded.";

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
        public async Task<IHttpActionResult> Putaway(PutawayVM putawayVM)
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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(putawayVM.ID)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }

                    if (putawayVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
                        int availableBagQty = Convert.ToInt32(availableQty / receivingDetail.QtyPerBag);
                        if (putawayVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.PutawayQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                        }
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

                    Putaway putaway = new Putaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.ReceivingDetailID = receivingDetail.ID;
                    putaway.PutawayMethod = "MANUAL";

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    putaway.PutOn = transactionDate;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = putawayVM.BagQty * receivingDetail.QtyPerBag;

                    db.Putaways.Add(putaway);

                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                     StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                    if (stockRM != null)
                    {
                        stockRM.Quantity += putaway.PutawayQty;
                    }
                    else
                    {
                        stockRM = new StockRM();
                        stockRM.ID = Helper.CreateGuid("S");
                        stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                        stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
                        stockRM.Code = receivingDetail.StockCode;
                        stockRM.LotNumber = receivingDetail.LotNo;
                        stockRM.InDate = receivingDetail.InDate;
                        stockRM.ExpiredDate = receivingDetail.ExpDate;
                        stockRM.Quantity = putaway.PutawayQty;
                        stockRM.QtyPerBag = receivingDetail.QtyPerBag;
                        stockRM.BinRackID = putaway.BinRackID;
                        stockRM.BinRackCode = putaway.BinRackCode;
                        stockRM.BinRackName = putaway.BinRackName;
                        stockRM.ReceivedAt = putaway.PutOn;

                        db.StockRMs.Add(stockRM);
                    }

                    //update receiving plan status if all quantity have been received and putaway
                    //pindahin pemotongan stock pindah di matreq
                    Receiving rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();
                    //if (rec.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode == "2003" || rec.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode == "2004")
                    //{
                    //    var source = rec.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode;
                    //    BinRack binRack1 = db.BinRacks.Where(x => x.WarehouseCode == source).SingleOrDefault();
                    //    //vStockAll stockAll = db.vStockAlls.Where(x => x.BinRackCode == binRack1.Code).SingleOrDefault();
                    //    vStockAll stockAll = db.vStockAlls.Where(x => x.BinRackCode == binRack1.Code && x.Code == stockRM.Code).SingleOrDefault();
                    //    if (stockAll.Type.Equals("RM"))
                    //    {
                    //        decimal pickQty = putaway.PutawayQty;
                    //        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                    //        stock.Quantity -= pickQty;
                    //    }
                    //    else if (stockAll.Type.Equals("SFG"))
                    //    {
                    //        decimal pickQty = putaway.PutawayQty;
                    //        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                    //        stock.Quantity -= pickQty;
                    //    }

                    //}

                    await db.SaveChangesAsync();

                    rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();

                    decimal totalReceive = rec.Qty;
                    decimal totalPutaway = 0;

                    foreach(ReceivingDetail recDetail in rec.ReceivingDetails)
                    {
                        totalPutaway += recDetail.Putaways.Sum(i => i.PutawayQty);
                    }

                    RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(rec.MaterialCode)).FirstOrDefaultAsync();

                    int OutstandingQty = Convert.ToInt32(rec.Qty / rec.QtyPerBag) - Convert.ToInt32(totalPutaway / rec.Qty);

                    if (totalReceive == totalPutaway)
                    {
                        rec.TransactionStatus = "CLOSED";
                    }
                    else if (OutstandingQty < 1)
                    {
                        rec.TransactionStatus = "CLOSED";
                    }

                    await db.SaveChangesAsync();

                    decimal availQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
                    int availBagQty = Convert.ToInt32(availQty / receivingDetail.QtyPerBag);

                    obj.Add("availableTotalQty", availQty);
                    obj.Add("availableBagQty", availBagQty);

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

        [HttpGet]
        public async Task<IHttpActionResult> GetListStockCode(string receivingDetailID)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<ReceivingDetailBarcodeDTO> list = new List<ReceivingDetailBarcodeDTO>();
            try
            {
                if (string.IsNullOrEmpty(receivingDetailID))
                {
                    throw new Exception("Id is required.");
                }

                ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(receivingDetailID)).FirstOrDefaultAsync();

                if (receivingDetail == null)
                {
                    throw new Exception("Receiving not recognized.");
                }

                if (receivingDetail.Inspections != null && receivingDetail.Inspections.Count() > 0)
                {
                    IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
                    data = from dat in receivingDetail.Inspections.OrderBy(m => m.InspectedOn)
                           select new ReceivingDetailBarcodeDTO
                           {
                               ID = dat.ID,
                               Type = "Inspection",
                               BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag)),
                               QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
                               TotalQty = Helper.FormatThousand(dat.InspectionQty),
                               Date = Helper.NullDateTimeToString(dat.InspectedOn),
                               Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
                           };

                    list.AddRange(data.ToList());
                }

                if (receivingDetail.Judgements != null && receivingDetail.Judgements.Count() > 0)
                {
                    IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
                    data = from dat in receivingDetail.Judgements.OrderBy(m => m.JudgeOn)
                           select new ReceivingDetailBarcodeDTO
                           {
                               ID = dat.ID,
                               Type = "Judgement",
                               BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag)),
                               QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
                               TotalQty = Helper.FormatThousand(dat.JudgementQty),
                               Date = Helper.NullDateTimeToString(dat.JudgeOn),
                               Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
                           };

                    list.AddRange(data.ToList());
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

            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Print(ReceivingPrintVM receivingPrintVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            ReceivingBarcodeDTO data = new ReceivingBarcodeDTO();

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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(receivingPrintVM.ReceivingDetailID)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Receiving is not recognized.");
                    }
                    else
                    {
                        //check status already closed
                    }


                    if (receivingPrintVM.PrintQty <= 0)
                    {
                        ModelState.AddModelError("Receiving.PrintQty", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        //check to list barcode available qty for printing
                        int availableBagQty = 0;
                        if (receivingPrintVM.Type.Equals("Inspection"))
                        {
                            Inspection inspection = receivingDetail.Inspections.Where(m => m.ID.Equals(receivingPrintVM.ID)).FirstOrDefault();
                            availableBagQty = Convert.ToInt32(inspection.InspectionQty / receivingDetail.QtyPerBag);
                        }
                        else if (receivingPrintVM.Type.Equals("Judgement"))
                        {
                            Judgement judgement = receivingDetail.Judgements.Where(m => m.ID.Equals(receivingPrintVM.ID)).FirstOrDefault();
                            availableBagQty = Convert.ToInt32(judgement.JudgementQty / receivingDetail.QtyPerBag);
                        }
                        else
                        {
                            throw new Exception("Type not recognized.");
                        }

                        if (receivingPrintVM.PrintQty > availableBagQty)
                        {
                            ModelState.AddModelError("Receiving.PrintQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                        }

                        string[] listPrinter = { ConfigurationManager.AppSettings["printer_1_ip"].ToString(), ConfigurationManager.AppSettings["printer_2_ip"].ToString() };
                        if (!listPrinter.Contains(receivingPrintVM.Printer))
                        {
                            ModelState.AddModelError("Receiving.ListPrinter", "Printer not found.");
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

                    //create pdf file to specific printer folder for middleware printing
                    decimal totalQty = 0;
                    decimal qtyPerBag = 0;

                    if (receivingPrintVM.Type.Equals("Inspection"))
                    {
                        Inspection dat = db.Inspections.Where(m => m.ID.Equals(receivingPrintVM.ID)).FirstOrDefault();
                        RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                        data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                        data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                        data.RawMaterialMaker = rm.Maker;
                        data.StockCode = dat.ReceivingDetail.StockCode;
                        data.LotNo = dat.ReceivingDetail.LotNo;
                        data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                        data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                        data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                        data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag));
                        data.Qty = Helper.FormatThousand(dat.InspectionQty);
                        data.UoM = dat.ReceivingDetail.UoM;
                        data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1);

                        totalQty = dat.InspectionQty;
                        qtyPerBag = dat.ReceivingDetail.QtyPerBag;
                    }
                    else if (receivingPrintVM.Type.Equals("Judgement"))
                    {
                        Judgement dat = db.Judgements.Where(m => m.ID.Equals(receivingPrintVM.ID)).FirstOrDefault();
                        RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                        data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                        data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                        data.RawMaterialMaker = rm.Maker;
                        data.StockCode = dat.ReceivingDetail.StockCode;
                        data.LotNo = dat.ReceivingDetail.LotNo;
                        data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                        data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                        data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                        data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag));
                        data.Qty = Helper.FormatThousand(dat.JudgementQty);
                        data.UoM = dat.ReceivingDetail.UoM;
                        data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1);

                        totalQty = dat.JudgementQty;
                        qtyPerBag = dat.ReceivingDetail.QtyPerBag;
                    }
                    else
                    {
                        throw new Exception("Type not recognized.");
                    }

                    int seq = 0;

                    int fullBag = receivingPrintVM.PrintQty;
                    seq = Convert.ToInt32(data.StartSeries);

                    List<string> bodies = new List<string>();

                    int series = receivingPrintVM.UseSeries ? 1 : 0;

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        if (series == 1)
                        {
                            runningNumber = string.Format("{0:D5}", seq++);
                        }
                        else
                        {
                            runningNumber = string.Format("{0:D5}", 1);
                        }

                        LabelDTO dto = new LabelDTO();
                        string qr1 = data.MaterialCode.PadRight(7) + " " + runningNumber + " " + data.QtyPerBag.PadLeft(6) + " " + data.LotNo;
                        dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";
                        if (!string.IsNullOrEmpty(data.InDate))
                        {
                            try
                            {
                                DateTime dt = DateTime.ParseExact(data.InDate, "dd/MM/yyyy", null);
                                dto.Field4 = dt.ToString("MMMM").ToUpper();
                                inDate = dt.ToString("yyyyMMdd").Substring(1);
                                inDate2 = dt.ToString("yyyMMdd");
                                inDate2 = inDate2.Substring(1);
                                inDate3 = dt.ToString("yyyy-MM-dd");
                            }
                            catch (Exception e)
                            {

                            }
                        }

                        if (!string.IsNullOrEmpty(data.ExpDate))
                        {
                            try
                            {
                                DateTime dt = DateTime.ParseExact(data.ExpDate, "dd/MM/yyyy", null);
                                expiredDate = dt.ToString("yyyyMMdd").Substring(2);
                                expiredDate2 = dt.ToString("yyyy-MM-dd");
                            }
                            catch (Exception e)
                            {

                            }
                        }
                        string qr2 = data.MaterialCode.PadRight(7) + inDate + expiredDate;
                        dto.Field5 = data.LotNo;
                        dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                        dto.Field7 = data.RawMaterialMaker;
                        dto.Field8 = data.MaterialName;
                        dto.Field9 = data.QtyPerBag.ToString();
                        dto.Field10 = data.UoM.ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = data.MaterialCode;
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
                                if (printerDTO.PrinterIP.Equals(receivingPrintVM.Printer))
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
                    message = "Print succeeded. Please wait.";
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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetBarcodeById(string type, string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ReceivingBarcodeDTO data = new ReceivingBarcodeDTO();
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    throw new Exception("Type is required.");
                }

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                if (type.Equals("Inspection"))
                {
                    Inspection dat = db.Inspections.Where(m => m.ID.Equals(id)).FirstOrDefault();
                    RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                    data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                    data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                    data.RawMaterialMaker = rm.Maker;
                    data.StockCode = dat.ReceivingDetail.StockCode;
                    data.LotNo = dat.ReceivingDetail.LotNo;
                    data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                    data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                    data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag));
                    data.Qty = Helper.FormatThousand(dat.InspectionQty);
                    data.UoM = dat.ReceivingDetail.UoM;
                    data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1);
                }
                else if (type.Equals("Judgement"))
                {
                    Judgement dat = db.Judgements.Where(m => m.ID.Equals(id)).FirstOrDefault();
                    RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                    data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                    data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                    data.RawMaterialMaker = rm.Maker;
                    data.StockCode = dat.ReceivingDetail.StockCode;
                    data.LotNo = dat.ReceivingDetail.LotNo;
                    data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                    data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                    data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag));
                    data.Qty = Helper.FormatThousand(dat.JudgementQty);
                    data.UoM = dat.ReceivingDetail.UoM;
                    data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1);
                }
                else
                {
                    throw new Exception("Type not recognized.");
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

            obj.Add("data", data);
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
    }


    public class FakeController : System.Web.Mvc.ControllerBase { protected override void ExecuteCore() { } }

}
