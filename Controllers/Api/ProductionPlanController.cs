using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.UI;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers
{
    public class ProductionPlanController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> DatatableHeader()
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

            int year = Convert.ToInt32(request["year"].ToString());
            int month = Convert.ToInt32(request["month"].ToString());

            IEnumerable<ProductionPlanHeader> list = Enumerable.Empty<ProductionPlanHeader>();
            IEnumerable<ProductionPlanHeaderDTO> pagedData = Enumerable.Empty<ProductionPlanHeaderDTO>();

            IQueryable<ProductionPlanHeader> query = db.ProductionPlanHeaders.Where(m => DbFunctions.TruncateTime(m.ScheduleDate).Value.Year == year
               && DbFunctions.TruncateTime(m.ScheduleDate).Value.Month == month).AsQueryable();

            int recordsTotal = db.ProductionPlanHeaders.Where(m => DbFunctions.TruncateTime(m.ScheduleDate).Value.Year == year
               && DbFunctions.TruncateTime(m.ScheduleDate).Value.Month == month).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                      .Where(m => m.Code.Contains(search) ||
                         m.ItemCode.Contains(search) || m.ItemName.Contains(search));

                Dictionary<string, Func<ProductionPlanHeader, object>> cols = new Dictionary<string, Func<ProductionPlanHeader, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("Code", x => x.Code);
                cols.Add("OrderNumber", x => x.OrderNumber);
                cols.Add("ItemCode", x => x.ItemCode);
                cols.Add("ItemName", x => x.ItemName);
                cols.Add("BatchQty", x => x.BatchQty);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("ScheduleDate", x => x.ScheduleDate);
                cols.Add("ETA", x => x.ETA);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);
                cols.Add("LineNumber", x => x.LineNumber);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                list = list.Skip(start).Take(length).ToList();

                recordsFiltered = list.Count();


                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new ProductionPlanHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    OrderNumber = x.OrderNumber,
                                    ItemCode = x.ItemCode,
                                    ItemName = x.ItemName,
                                    RecipeNumber = x.Formula.RecipeNumber,
                                    BatchQty = Helper.FormatThousand(x.BatchQty),
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    ScheduleDate = Helper.NullDateToString(x.ScheduleDate),
                                    ETA = Helper.NullDateToString(x.ETA),
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn),
                                    LineNumber = x.LineNumber.ToString()
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

            ProductionPlanHeaderDTO productionPlanHeaderDTO = null;
            decimal TotalTon = 0;
            decimal Ton = 0;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                ProductionPlanHeader productionPlanHeader = await db.ProductionPlanHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                if (productionPlanHeader == null || productionPlanHeader.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }


                productionPlanHeaderDTO = new ProductionPlanHeaderDTO()
                {
                    ID = productionPlanHeader.ID,
                    Code = productionPlanHeader.Code,
                    OrderNumber = productionPlanHeader.OrderNumber,
                    RecipeNumber = productionPlanHeader.Formula.RecipeNumber,
                    ItemCode = productionPlanHeader.ItemCode,
                    ItemName = productionPlanHeader.ItemName,
                    BatchQty = Helper.FormatThousand(productionPlanHeader.BatchQty),
                    TotalQty = Helper.FormatThousand(productionPlanHeader.TotalQty),
                    ScheduleDate = productionPlanHeader.ScheduleDate.ToString("dd/MM/yyyy"),
                    StartTime = string.Format("{0:hh\\:mm\\:ss}", productionPlanHeader.StartTime),
                    FinishTime = string.Format("{0:hh\\:mm\\:ss}", productionPlanHeader.FinishTime),
                    ETA = productionPlanHeader.ETA.ToString("dd/MM/yyyy"),
                    Remarks = productionPlanHeader.Remarks,
                    TransactionStatus = productionPlanHeader.TransactionStatus,
                    CreatedBy = productionPlanHeader.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(productionPlanHeader.CreatedOn),
                    ModifiedBy = productionPlanHeader.ModifiedBy != null ? productionPlanHeader.ModifiedBy : "",
                    ModifiedOn = Helper.NullDateTimeToString(productionPlanHeader.ModifiedOn),
                    LineNumber = productionPlanHeader.LineNumber.ToString(),
                    OrderDetails = from frm in productionPlanHeader.ProductionPlanOrders
                                                                  select new ProductionPlanOrderDTO
                                                                  {
                                                                      ID = frm.ID,
                                                                      OrderNumber = frm.OrderNumber,
                                                                      OrderQty = Helper.FormatThousand(frm.OrderQty),
                                                                      BatchQty = Helper.FormatThousand(frm.BatchQty),
                                                                      TotalQty = Helper.FormatThousand(frm.TotalQty),
                                                                      CreatedOn = Helper.NullDateTimeToString(frm.CreatedOn),
                                                                      CreatedBy = frm.CreatedBy,
                                                                      ModifiedOn = Helper.NullDateTimeToString(frm.CreatedOn),
                                                                      ModifiedBy = frm.ModifiedBy,
                                                                  },
                    FormulaDetails = from frm in productionPlanHeader.Formula.FormulaDetails.OrderBy(m => m.MaterialCode)
                              select new FormulaDetailDTO
                              {
                                  ID = frm.ID,
                                  MaterialCode = frm.MaterialCode,
                                  MaterialName = frm.MaterialName,
                                  UoM = frm.UoM,
                                  Qty = Helper.FormatThousand(frm.Qty),
                                  RemainderQty = Helper.FormatThousand(frm.RemainderQty),
                                  Fullbag = Helper.FormatThousand(frm.Fullbag),
                                  Type = frm.Type
                              },
                    Details = from frm in productionPlanHeader.ProductionPlanDetails.OrderBy(m => m.MaterialCode)
                              select new ProductionPlanDetailDTO
                                                           {
                                                               ID = frm.ID,
                                                               MaterialCode = frm.MaterialCode,
                                                               MaterialName = frm.MaterialName,
                                                               Qty = Helper.FormatThousand(frm.Qty),
                                                               QtyPerBag = Helper.FormatThousand(frm.QtyPerBag),
                                                               BagQty = Helper.FormatThousand(frm.BagQty),
                                                               RemainderQty = Helper.FormatThousand(frm.RemainderQty),
                                                               TotalQty = Helper.FormatThousand(frm.TotalQty)
                                                           },
                };

                //TimeSpan breakTime = productionPlanHeader.FinishTime.Add(new TimeSpan(0, 0, productionPlanHeader.BreakMinute, 0, 0));
                //productionPlanHeaderDTO.BreakTime = string.Format("{0:hh\\:mm\\:ss}", breakTime);

                Ton = productionPlanHeader.Formula.FormulaDetails.Sum(i => i.Qty) / 1000;
                TotalTon = productionPlanHeader.TotalQty / 1000;


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

            obj.Add("ton", Helper.FormatThousand(Ton));
            obj.Add("totalTon", Helper.FormatThousand(TotalTon));
            obj.Add("data", productionPlanHeaderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        public string ScheduleColor(ProductionPlanHeader prod, string itemName)
        {
            string color = "";

            switch (prod.TransactionStatus)
            {
                case "OPEN":
                    color = "#ffea05";
                    break;
                case "POSTPONE":
                    color = "#28a745";
                    break;
                case "ADVANCE":
                    color = "#fb8f41";
                    break;
                case "OUTSTANDING":
                    color = "#ef2d2a";
                    break;
            }

            if (!string.IsNullOrEmpty(itemName) && itemName.Length > 4)
            {
                itemName = itemName.ToLower();
                vProductionPlanProduct product = db.vProductionPlanProducts.Where(m => m.MaterialName.ToLower().Equals(itemName)).FirstOrDefault();
                if(product != null)
                {
                    if (!prod.ItemName.ToLower().Equals(itemName))
                    {
                        color = "#fff";
                    }
                }
                
            }
          


            return color;
        }

        public string GenerateRemarks(ProductionPlanHeader prod)
        {
            string remarks = "";

            switch (prod.TransactionStatus)
            {
                case "OUTSTANDING":
                    remarks = "<b style='color : #000'>" + prod.Remarks + "</b>";
                    break;
                default:
                    remarks = "<b style='color : #ef2d2a'>" + prod.Remarks + "</b>";
                    break;
            }

            return remarks;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetSchedule(int year, int month, int line, string itemName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<ProductionPlanHeader> list = Enumerable.Empty<ProductionPlanHeader>();
            IEnumerable<ProductionPlanBreak> list_break = Enumerable.Empty<ProductionPlanBreak>();
            List<ProductionPlanScheduleDTO> data = new List<ProductionPlanScheduleDTO>();
            try
            {


                IQueryable<ProductionPlanHeader> query = db.ProductionPlanHeaders.Where(m => m.LineNumber.Equals(line) && DbFunctions.TruncateTime(m.ScheduleDate).Value.Year == year 
                && DbFunctions.TruncateTime(m.ScheduleDate).Value.Month == month).AsQueryable();

                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    IEnumerable<ProductionPlanScheduleDTO> dataSchedule = Enumerable.Empty<ProductionPlanScheduleDTO>();
                    dataSchedule = from prod in list
                                select new ProductionPlanScheduleDTO
                                {
                                    id = prod.ID,
                                    title = string.Format("{0} ({1}) <br> 1/1 {2} t <br> {3}) <br><br> {4}", prod.ItemName, prod.BatchQty, Helper.FormatThousand(prod.TotalQty / 1000), prod.OrderNumber, GenerateRemarks(prod)),
                                    description = string.Format("{0} ({1}) <br> 1/1 {2} t <br> {3}) <br><br> {4}", prod.ItemName, prod.BatchQty, Helper.FormatThousand(prod.TotalQty / 1000), prod.OrderNumber, GenerateRemarks(prod)),
                                    start = string.Format("{0}T{1}", prod.ScheduleDate.ToString("yyyy-MM-dd"), string.Format("{0:hh\\:mm\\:ss}", prod.StartTime)),
                                    end = string.Format("{0}T{1}", prod.ScheduleDate.ToString("yyyy-MM-dd"), string.Format("{0:hh\\:mm\\:ss}", prod.FinishTime)),
                                    backgroundColor = ScheduleColor(prod, itemName),
                                    textColor = "#000"
                                };

                    data.AddRange(dataSchedule.ToList());
                }

                IQueryable<ProductionPlanBreak> query_break = db.ProductionPlanBreaks.Where(m => m.LineNumber.Equals(line) && m.BreakMinute > 0 && DbFunctions.TruncateTime(m.ScheduleDate).Value.Year == year
                && DbFunctions.TruncateTime(m.ScheduleDate).Value.Month == month).AsQueryable();


                list_break = query_break.ToList();

                if (list_break != null && list_break.Count() > 0)
                {
                    IEnumerable<ProductionPlanScheduleDTO> dataBreak = Enumerable.Empty<ProductionPlanScheduleDTO>();
                    dataBreak = from prod in list_break
                                select new ProductionPlanScheduleDTO
                                    {
                                        id = prod.ID,
                                        title = "BREAK",
                                        description = "BREAK",
                                        start = string.Format("{0}T{1}", prod.ScheduleDate.ToString("yyyy-MM-dd"), string.Format("{0:hh\\:mm\\:ss}", prod.StartTime)),
                                        end = string.Format("{0}T{1}", prod.ScheduleDate.ToString("yyyy-MM-dd"), string.Format("{0:hh\\:mm\\:ss}", prod.FinishTime)),
                                        backgroundColor = "#4f4f4f",
                                        textColor = "#fff"
                                    };

                    data.AddRange(dataBreak.ToList());


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

            return Ok(data);
        }

        //[HttpGet]
        //public async Task<IHttpActionResult> GetDataById(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    ProductionPlanHeaderDTO productionPlanHeaderDTO = null;
        //    decimal TotalTon = 0;
        //    decimal Ton = 0;

        //    try
        //    {
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        ProductionPlanHeader productionPlanHeader = await db.ProductionPlanHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

        //        if (productionPlanHeader == null || productionPlanHeader.TransactionStatus == "CANCELLED")
        //        {
        //            throw new Exception("Data not found.");
        //        }


        //        productionPlanHeaderDTO = new ProductionPlanHeaderDTO()
        //        {
        //            ID = productionPlanHeader.ID,
        //            Code = productionPlanHeader.Code,
        //            OrderNumber = productionPlanHeader.OrderNumber,
        //            FormulaID = productionPlanHeader.FormulaID,
        //            RecipeNumber = productionPlanHeader.Formula.RecipeNumber,
        //            ItemCode = productionPlanHeader.ItemCode,
        //            ItemName = productionPlanHeader.ItemName,
        //            BatchQty = Helper.FormatThousand(productionPlanHeader.BatchQty),
        //            TotalQty = Helper.FormatThousand(productionPlanHeader.TotalQty),
        //            StartDate = Helper.NullDateTimeToString(productionPlanHeader.StartDate),
        //            FinishDate = Helper.NullDateTimeToString(productionPlanHeader.FinishDate),
        //            Details = new List<ProductionPlanDetailDTO>(),
        //            TransactionStatus = productionPlanHeader.TransactionStatus,
        //            CreatedBy = productionPlanHeader.CreatedBy,
        //            CreatedOn = Helper.NullDateTimeToString(productionPlanHeader.CreatedOn),
        //            ModifiedBy = productionPlanHeader.ModifiedBy != null ? productionPlanHeader.ModifiedBy : "",
        //            ModifiedOn = Helper.NullDateTimeToString(productionPlanHeader.ModifiedOn)
        //        };

        //        decimal TotalQuantity = Convert.ToDecimal(db.FormulaDetails.Where(m => m.FormulaID == productionPlanHeader.FormulaID).Select(i => i.Qty).Sum().ToString());
        //        Ton = TotalQuantity;
        //        // Edited by Kenzi 2020-08-17 -- Ganti jadi dibuletin ke bawah
        //        int totalBatch = (int)(Convert.ToDecimal(productionPlanHeader.TotalQty) / TotalQuantity);
        //        TotalTon = Ton * totalBatch;

        //        foreach (ProductionPlanDetail detail in productionPlanHeader.ProductionPlanDetails)
        //        {
        //            ProductionPlanDetailDTO detailDTO = new ProductionPlanDetailDTO();
        //            detailDTO.ID = detail.ID;
        //            detailDTO.MaterialCode = detail.MaterialCode;
        //            detailDTO.MaterialName = detail.MaterialName;
        //            detailDTO.RequestedQty = Helper.FormatThousand(detail.RequestedQty);
        //            detailDTO.FullBag = Helper.FormatThousand(detail.FullBag);
        //            //detailDTO.FullBagQty = Helper.FormatThousand(detail.RawMaterial.Qty);
        //            detailDTO.RemainderQty = Helper.FormatThousand(detail.RemainderQty);
        //            detailDTO.ProductionQty = Helper.FormatThousand(totalBatch);
        //            detailDTO.TotalQty = Helper.FormatThousand(detail.TotalQty);
        //            //detailDTO.TotalFullBag = Helper.FormatThousand(Convert.ToInt32(Math.Floor(detail.TotalQty / detail.RawMaterial.Qty)));
        //            decimal sisa = detail.RequestedQty - detail.RemainderQty;
        //            detailDTO.TotalRemainderQty = sisa > 0 ? Helper.FormatThousand(detail.TotalQty % sisa) : "0";
        //            //detailDTO.AvailableQty = Helper.FormatThousand(detail.TotalQty - detail.LackingQty);
        //            detailDTO.UoM = "KG";

        //            productionPlanHeaderDTO.Details.Add(detailDTO);

        //            //Ton += detail.TotalQty;
        //            //TotalTon += detail.RequestedQty;
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

        //    obj.Add("ton", Helper.FormatThousand(Ton / 1000));
        //    obj.Add("totalTon", Helper.FormatThousand(TotalTon / 1000));
        //    obj.Add("data", productionPlanHeaderDTO);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        public IHttpActionResult GetProductType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Product type found.";
            bool status = true;

            obj.Add("product_type", Constant.ProductTypes());
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        public IHttpActionResult GetLineType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Line type found.";
            bool status = true;

            obj.Add("line_type", Constant.LineTypes());
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

            string ProductType = HttpContext.Current.Request.Form.GetValues("ProductType")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<vProductionPlanProduct> list = Enumerable.Empty<vProductionPlanProduct>();
            IEnumerable<ProductDTO> pagedData = Enumerable.Empty<ProductDTO>();

            IQueryable<vProductionPlanProduct> query = db.vProductionPlanProducts.Where(s => s.ProdType.Equals(ProductType)).AsQueryable();

            int recordsTotal = db.vProductionPlanProducts.Where(s => s.ProdType.Equals(ProductType)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query.Where(m => m.MaterialCode.Contains(search) || m.MaterialName.Contains(search));

                Dictionary<string, Func<vProductionPlanProduct, object>> cols = new Dictionary<string, Func<vProductionPlanProduct, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("ProdType", x => x.ProdType);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {


                    pagedData = from x in list
                                select new ProductDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    ProdType = x.ProdType
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
        public async Task<IHttpActionResult> DatatableOrder()
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

            string MaterialCode = request["MaterialCode"].ToString();

            IEnumerable<vProductionPlanOrder> list = Enumerable.Empty<vProductionPlanOrder>();
            IEnumerable<ProductionRequestDetailDTO> pagedData = Enumerable.Empty<ProductionRequestDetailDTO>();

            IQueryable<vProductionPlanOrder> query = db.vProductionPlanOrders.Where(s => s.MaterialCode.Equals(MaterialCode) && s.AvailableOrderQty > 0).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query.Where(m => m.MaterialCode.Contains(search) || m.MaterialCode.Contains(search) || m.OrderNumber.Contains(search));

                Dictionary<string, Func<vProductionPlanOrder, object>> cols = new Dictionary<string, Func<vProductionPlanOrder, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("OrderNumber", x => x.OrderNumber);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("CustomerCode", x => x.CustomerCode);
                cols.Add("CustomerName", x => x.CustomerName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("ETA", x => x.ETA);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("UsedQty", x => x.UsedOrderQty);
                cols.Add("AvailableQty", x => x.AvailableOrderQty);


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {


                    pagedData = from x in list
                                select new ProductionRequestDetailDTO
                                {
                                    ID = x.ID,
                                    OrderNumber = x.OrderNumber,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    CustomerCode = x.CustomerCode,
                                    CustomerName = x.CustomerName,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    ETA = Helper.NullDateToString(x.ETA),
                                    Remarks = x.Remarks,
                                    AvailableQty = Helper.FormatThousand(x.AvailableOrderQty),
                                    UsedQty = Helper.FormatThousand(x.UsedOrderQty)
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

        public async Task<IHttpActionResult> Create(ProductionPlanHeaderVM productionPlanHeaderVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
            Dictionary<int, object> error_details = new Dictionary<int, object>();
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
                    if (productionPlanHeaderVM.LineNumber > 0)
                    {
                        if (!Constant.LineTypes().ContainsKey(productionPlanHeaderVM.LineNumber))
                        {
                            ModelState.AddModelError("ProductionPlanHeader.LineNumber", "Line Number is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ProductionPlanHeader.LineNumber", "Line Number is required.");
                    }

                    if (!string.IsNullOrEmpty(productionPlanHeaderVM.ItemId))
                    {
                        vProduct prod = await db.vProducts.Where(s => s.MaterialCode.Equals(productionPlanHeaderVM.ItemId)).FirstOrDefaultAsync();

                        if (prod == null)
                        {
                            ModelState.AddModelError("ProductionPlanHeader.ItemID", "Product not found.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ProductionPlanHeader.ItemID", "Product is required.");
                    }

                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ScheduleDate))
                    {
                        ModelState.AddModelError("PurchaseRequest.ScheduleDate", "Production Date is required.");
                    }
                    else
                    {
                        try
                        {
                            DateTime prodDate = Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate);

                            if (prodDate.Date < DateTime.Now.Date)
                            {
                                ModelState.AddModelError("ProductionPlanHeader.ScheduleDate", "Production Date can not back date.");
                            }
                        }
                        catch (FormatException e)
                        {
                            ModelState.AddModelError("PurchaseRequest.ScheduleDate", "Bad format Production Date.");
                        }
                    }

                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ETA))
                    {
                        ModelState.AddModelError("PurchaseRequest.DeliveryDate", "Delivery Date is required.");
                    }
                    else
                    {
                        try
                        {
                            DateTime etaDate = Convert.ToDateTime(productionPlanHeaderVM.ETA);
                        }
                        catch (FormatException e)
                        {
                            ModelState.AddModelError("PurchaseRequest.DeliveryDate", "Bad format Delivery Date.");
                        }
                    }

                   


                    if ((productionPlanHeaderVM.OrderIds == null || productionPlanHeaderVM.OrderIds.Count() == 0) && productionPlanHeaderVM.Qty == 0)
                    {
                        ModelState.AddModelError("ProductionPlanHeader.Qty", "Quantity is required.");
                    }

                    if (productionPlanHeaderVM.OrderIds != null && productionPlanHeaderVM.OrderIds.Count() > 0)
                    {
                        List<string> rmList = new List<string>();
                        int idx = 0;

                        foreach (string OrderId in productionPlanHeaderVM.OrderIds)
                        {
                            List<CustomValidationMessage> validationDetails = new List<CustomValidationMessage>();
                            vProductionPlanOrder order = await db.vProductionPlanOrders.Where(s => s.ID.Equals(OrderId)).FirstOrDefaultAsync();
                            if (order == null)
                            {
                                validationDetails.Add(new CustomValidationMessage("OrderNumber", "Order Number not found."));
                            }
                            else
                            {
                                if(order.AvailableOrderQty < 0)
                                {
                                    validationDetails.Add(new CustomValidationMessage("OrderNumber", "Order already complete."));
                                }
                            }

                            if (validationDetails.Count() > 0)
                            {
                                error_details.Add(idx, validationDetails);
                                ModelState.AddModelError("ProductionPlanHeader.Details", "Order not validated.");
                            }

                            idx++;

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

                        obj.Add("error_validation_details", error_details);

                        throw new Exception("Input is not valid");
                    }

                    var CreatedAt = DateTime.Now;
                    var TransactionId = Helper.CreateGuid("PP");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.ProductionPlanHeaders.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);


                    ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader
                    {
                        ID = TransactionId,
                        Code = Code,
                        ScheduleDate = Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate),
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        LineNumber = productionPlanHeaderVM.LineNumber
                    };

                    
                    vProduct product = await db.vProducts.Where(s => s.MaterialCode.Equals(productionPlanHeaderVM.ItemId)).FirstOrDefaultAsync();
                    productionPlanHeader.ItemCode = product.MaterialCode;
                    productionPlanHeader.ItemName = product.MaterialName;

                    Formula formula = await db.Formulae.Where(s => s.ItemCode.Equals(product.MaterialCode) && s.IsActive == true).FirstOrDefaultAsync();
                    if (formula == null)
                    {
                        throw new Exception("No Recipe available for current item. Please add recipe first.");
                    }

                    decimal totalRecipe = formula.FormulaDetails.Sum(i => i.Qty);
                    productionPlanHeader.RecipeNumber = formula.RecipeNumber;


                    //check if not based on Order, create dummy order number
                    //NR150, NR160, NR170, NR370,EP-CL,NR-CL,NBR-CL,CHEM-CL -> product without PRF
                    //I-(Current Year)-(Current Month)-MaterialCode(RunningNo) ex: I-19-07-00NR1701
                    //runningNo, counting based on current month

                    if (productionPlanHeaderVM.OrderIds == null || productionPlanHeaderVM.OrderIds.Count() == 0)
                    {
                        // get last number, and do increment.
                        int record = db.ProductionPlanHeaders.AsQueryable().Where(x => x.ItemCode.Equals(productionPlanHeaderVM.ItemId) && x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                            .AsEnumerable().Select(x => x.ItemCode).ToList().Count();

                        string runningNo = string.Format("{0:D2}", record + 1);

                        productionPlanHeader.OrderNumber = string.Format("I-{0}-{1}-{2}{3}", DateTime.Now.ToString("yy"), DateTime.Now.ToString("MM"), product.MaterialCode, runningNo);
                        //check if not based on Order, input quantity manually
                        //productionPlanHeader.BatchQty = productionPlanHeaderVM.Qty;
                        //productionPlanHeader.TotalQty = productionPlanHeaderVM.Qty * totalRecipe;
                        productionPlanHeader.ETA = Convert.ToDateTime(productionPlanHeaderVM.ETA);
                    }
                    else
                    {
                        string OrderNumber = "";
                        decimal TotalQty = 0;
                        decimal RequestedQty = productionPlanHeaderVM.Qty * totalRecipe;
                        decimal RemainderQty = 0;
                        int index = 0;

                        //start from order ascending
                        List<vProductionPlanOrder> productionRequestDetails = await db.vProductionPlanOrders.Where(s => productionPlanHeaderVM.OrderIds.Contains(s.ID)).OrderBy(s => s.OrderNumber).ToListAsync();
                        foreach (vProductionPlanOrder productionRequestDetail in productionRequestDetails)
                        {
                            if(RemainderQty == 0)
                            {
                                RemainderQty = RequestedQty - productionRequestDetail.AvailableOrderQty.Value;
                            }
                            else
                            {
                                RemainderQty -= Math.Abs(productionRequestDetail.AvailableOrderQty.Value);
                            }
                            
                            decimal OrderQty = 0;
                            //check order qty, if still available use it
                            if((RemainderQty) > 0)
                            {
                                OrderQty = productionRequestDetail.AvailableOrderQty.Value;
                            }
                            else
                            {
                                OrderQty = Math.Abs(productionRequestDetail.AvailableOrderQty.Value + RemainderQty);
                            }

                            int batch = Convert.ToInt32(OrderQty / totalRecipe);
                            if (batch == 0)
                            {
                                batch += 1;
                            }

                            productionPlanHeader.ProductionPlanOrders.Add(new ProductionPlanOrder
                            {
                                ID = Helper.CreateGuid("PPO"),
                                HeaderID = productionPlanHeader.ID,
                                PRFID = productionRequestDetail.HeaderID,
                                ETA = productionRequestDetail.ETA,
                                OrderNumber = productionRequestDetail.OrderNumber,
                                OrderQty = OrderQty,
                                BatchQty = batch,
                                TotalQty = OrderQty * batch,
                                CreatedBy = activeUser,
                                CreatedOn = CreatedAt
                            });

                            // partial order
                            int record = db.ProductionPlanOrders.AsQueryable().Where(x => x.OrderNumber.Equals(productionRequestDetail.OrderNumber))
                                .AsEnumerable().Select(x => x.OrderNumber).ToList().Count();

                            string runningNo = string.Format("{0:D2}", record + 1);

                            ProductionPlanOrder productionPlanOrder = await db.ProductionPlanOrders.AsQueryable().Where(m => m.OrderNumber.Equals(productionRequestDetail.OrderNumber)).FirstOrDefaultAsync();
                            if (productionPlanOrder != null)
                            {
                                if (index == 0)
                                {
                                    productionPlanHeader.OrderNumber = string.Format("{0}_{1}", productionRequestDetail.OrderNumber, runningNo);
                                }
                                else
                                {
                                    productionPlanHeader.OrderNumber += ")" + string.Format("{0}_{1}", productionRequestDetail.OrderNumber.Substring(8), runningNo);
                                }

                            }
                            else
                            {
                                if ((RemainderQty) < 0)
                                {
                                    if (index == 0)
                                    {
                                        productionPlanHeader.OrderNumber = string.Format("{0}_{1}", productionRequestDetail.OrderNumber, runningNo);
                                    }
                                    else
                                    {
                                        productionPlanHeader.OrderNumber += ")" + string.Format("{0}_{1}", productionRequestDetail.OrderNumber.Substring(8), runningNo);
                                    }
                                }
                                else
                                {
                                    if (index == 0)
                                    {
                                        productionPlanHeader.OrderNumber = productionRequestDetail.OrderNumber;
                                    }
                                    else
                                    {
                                        productionPlanHeader.OrderNumber += ")" + productionRequestDetail.OrderNumber.Substring(8);
                                    }
                                }
                            }                         
                
                            index++;

                            //use one order if requested quantity already fulfilled
                            if ((RemainderQty) < 0)
                            {
                                break;
                            }

                        }
                        //foreach (string OrderId in productionPlanHeaderVM.OrderIds)
                        //{
                        //    //check order already used or not
                        //    ProductionRequestDetail productionRequestDetail = await db.ProductionRequestDetails.Where(s => s.ID.Equals(OrderId)).FirstOrDefaultAsync();
                        //    if (productionRequestDetail != null)
                        //    {
                        //        int batch = Convert.ToInt32(productionRequestDetail.Qty / totalRecipe);
                        //        if (batch == 0)
                        //        {
                        //            batch += 1;
                        //        }

                        //        productionPlanHeader.ProductionPlanOrders.Add(new ProductionPlanOrder
                        //        {
                        //            ID = Helper.CreateGuid("PPO"),
                        //            HeaderID = productionPlanHeader.ID,
                        //            PRFID = productionRequestDetail.HeaderID,
                        //            ETA = productionRequestDetail.ETA,
                        //            OrderNumber = productionRequestDetail.OrderNumber,
                        //            OrderQty = productionRequestDetail.Qty,
                        //            BatchQty = batch,
                        //            TotalQty = productionRequestDetail.Qty * batch,
                        //            CreatedBy = activeUser,
                        //            CreatedOn = CreatedAt
                        //        });

                        //        if (index == 0)
                        //        {
                        //            OrderNumber = productionRequestDetail.OrderNumber;
                        //        }
                        //        else
                        //        {
                        //            OrderNumber += ")" + productionRequestDetail.OrderNumber.Substring(8);
                        //        }

                        //        TotalQty += productionRequestDetail.Qty;

                        //        index++;
                        //    }

                        //}

                        //get ETA from early ETA Order
                        ProductionPlanOrder prfDetail = productionPlanHeader.ProductionPlanOrders.Where(s => s.HeaderID.Equals(productionPlanHeader.ID)).OrderBy(m => m.ETA).FirstOrDefault();
                        productionPlanHeader.ETA = prfDetail.ETA;

                      
                        //int totalBatch = Convert.ToInt32(TotalQty / totalRecipe);
                        //if (totalBatch == 0)
                        //{
                        //    totalBatch += 1;
                        //}
                        //productionPlanHeader.BatchQty = totalBatch;
                        //productionPlanHeader.TotalQty *= productionPlanHeader.BatchQty;


                    }

                    productionPlanHeader.BatchQty = productionPlanHeaderVM.Qty;
                    productionPlanHeader.TotalQty = productionPlanHeaderVM.Qty * totalRecipe;

                    int totalMinutes = 0;
                    //finish time
                    //line 1 = * total Batch * 5 Minute
                    //line 2 = * total Batch * 10 Minute
                    switch (productionPlanHeader.LineNumber)
                    {
                        case 1:
                            totalMinutes = productionPlanHeader.BatchQty * 5;
                            break;
                        case 2:
                            totalMinutes = productionPlanHeader.BatchQty * 10;
                            break;
                    }

                    //check latest finish time current date
                    //check break time

                    vSchedule schedule = await db.vSchedules.Where(m => m.ScheduleDate.Equals(productionPlanHeader.ScheduleDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber)).OrderByDescending(m => m.FinishTime).FirstOrDefaultAsync();
                    TimeSpan startTime = new TimeSpan(0, 7, 15, 0, 0);
                    //ProductionPlanHeader prevProd = await db.ProductionPlanHeaders.Where(m => m.ScheduleDate.Equals(productionPlanHeader.ScheduleDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber)).OrderByDescending(m => m.FinishTime).FirstOrDefaultAsync();
                    //if (prevProd == null)
                    //{
                    //    productionPlanHeader.StartTime = startTime;
                    //}
                    //else
                    //{
                    //    productionPlanHeader.StartTime = prevProd.FinishTime;
                    //}
                    if (schedule == null)
                    {
                        productionPlanHeader.StartTime = startTime;
                    }
                    else
                    {
                        productionPlanHeader.StartTime = schedule.FinishTime;
                    }


                    productionPlanHeader.FinishTime = productionPlanHeader.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));


                    foreach (FormulaDetail formulaDetail in formula.FormulaDetails)
                    {
                        vProductMaster material = await db.vProductMasters.Where(s => s.MaterialCode.Equals(formulaDetail.MaterialCode)).FirstOrDefaultAsync();
                        ProductionPlanDetail productionPlanDetail = new ProductionPlanDetail()
                        {
                            ID = Helper.CreateGuid("PPD"),
                            HeaderID = productionPlanHeader.ID,
                            MaterialCode = formulaDetail.MaterialCode,
                            MaterialName = formulaDetail.MaterialName,
                            Qty = formulaDetail.Qty,
                            QtyPerBag = material.QtyPerBag
                        };

                        //A = total batch * total recipe
                        //B = qty / total recipe
                        //TotalQty = A * B;

                        decimal A = productionPlanHeader.BatchQty * totalRecipe;
                        decimal B = productionPlanDetail.Qty / totalRecipe;
                        productionPlanDetail.TotalQty = A * B;
                        productionPlanDetail.BagQty = Convert.ToInt32(Math.Floor(productionPlanDetail.TotalQty / productionPlanDetail.QtyPerBag));
                        productionPlanDetail.RemainderQty = productionPlanDetail.TotalQty % productionPlanDetail.QtyPerBag;

                        productionPlanHeader.ProductionPlanDetails.Add(productionPlanDetail);
                    }

                    db.ProductionPlanHeaders.Add(productionPlanHeader);
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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateSchedule(ProductionPlanHeaderVM productionPlanHeaderVM)
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
                    ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader();
                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ID))
                    {
                        throw new Exception("Production Plan ID is required.");
                    }
                    else
                    {
                        productionPlanHeader = await db.ProductionPlanHeaders.Where(s => s.ID.Equals(productionPlanHeaderVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanHeader == null)
                        {
                            throw new Exception("Production Plan is not recognized.");
                        }

                        if (productionPlanHeader.TransactionStatus.Equals("CLOSED"))
                        {
                            throw new Exception("Production Plan is already closed.");
                        }

                    }

                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ScheduleDate))
                    {
                        ModelState.AddModelError("PurchaseRequest.ScheduleDate", "Production Date is required.");
                    }
                    else
                    {
                        try
                        {
                            DateTime prodDate = Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate);

                            if (prodDate.Date < DateTime.Now.Date)
                            {
                                ModelState.AddModelError("ProductionPlanHeader.ScheduleDate", "Production Date can not back date.");
                            }

                            //check if schedule date still the same
                            if(productionPlanHeader.ScheduleDate.Date == prodDate.Date)
                            {
                                ModelState.AddModelError("ProductionPlanHeader.ScheduleDate", "Production Date still same.");
                            }
                        }
                        catch (FormatException e)
                        {
                            ModelState.AddModelError("PurchaseRequest.ScheduleDate", "Bad format Production Date.");
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


                    productionPlanHeader.ModifiedBy = activeUser;
                    productionPlanHeader.ModifiedOn = DateTime.Now;


                    if (DateTime.Now.Date >= productionPlanHeader.CreatedOn.Date)
                    {
                        //check status
                        if (Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate).Date > productionPlanHeader.ScheduleDate.Date)
                        {
                            productionPlanHeader.TransactionStatus = "POSTPONE";
                        }
                        else if (Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate).Date < productionPlanHeader.ScheduleDate.Date)
                        {
                            productionPlanHeader.TransactionStatus = "ADVANCE";
                        }
                    }

                    //if (!string.IsNullOrEmpty(productionPlanHeader.Remarks))
                    //{
                    //    productionPlanHeader.TransactionStatus = "OUTSTANDING";
                    //}

                    DateTime prevDate = productionPlanHeader.ScheduleDate;
                    TimeSpan prevStartTime = productionPlanHeader.StartTime;
                    TimeSpan prevFinishTime = productionPlanHeader.FinishTime;

                    productionPlanHeader.ScheduleDate = Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate);

                    int totalMinutes = 0;

                    switch (productionPlanHeader.LineNumber)
                    {
                        case 1:
                            totalMinutes = productionPlanHeader.BatchQty * 5;
                            break;
                        case 2:
                            totalMinutes = productionPlanHeader.BatchQty * 10;
                            break;
                    }

                    //check latest finish time current date
                    TimeSpan startTime = new TimeSpan(0, 7, 15, 0, 0);
                    //ProductionPlanHeader prevProd = await db.ProductionPlanHeaders.Where(m => m.ScheduleDate.Equals(productionPlanHeader.ScheduleDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber)).OrderByDescending(m => m.FinishTime).FirstOrDefaultAsync();
                    //if (prevProd == null)
                    //{
                    //    productionPlanHeader.StartTime = startTime;
                    //}
                    //else
                    //{
                    //    productionPlanHeader.StartTime = prevProd.FinishTime;
                    //}

                    vSchedule schedule = await db.vSchedules.Where(m => m.ScheduleDate.Equals(productionPlanHeader.ScheduleDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber)).OrderByDescending(m => m.FinishTime).FirstOrDefaultAsync();
                    if (schedule == null)
                    {
                        productionPlanHeader.StartTime = startTime;
                    }
                    else
                    {
                        productionPlanHeader.StartTime = schedule.FinishTime;
                    }


                    productionPlanHeader.FinishTime = productionPlanHeader.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));

                    //update all schedule, auto adjustment
                    List<vSchedule> schedules = await db.vSchedules.Where(m => !m.ID.Equals(productionPlanHeader.ID) && m.ScheduleDate.Equals(prevDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber) && m.StartTime >= prevFinishTime).OrderBy(m => m.StartTime).ToListAsync();


                    TimeSpan start = prevStartTime;

                    foreach (vSchedule sched in schedules)
                    {
                        //update each
                        if (sched.ScheduleType.Equals("PROD"))
                        {
                            //update production plan
                            ProductionPlanHeader prodPlan = await db.ProductionPlanHeaders.Where(m => m.ID.Equals(sched.ID)).FirstOrDefaultAsync();
                            prodPlan.StartTime = start;

                            totalMinutes = 0;

                            switch (prodPlan.LineNumber)
                            {
                                case 1:
                                    totalMinutes = prodPlan.BatchQty * 5;
                                    break;
                                case 2:
                                    totalMinutes = prodPlan.BatchQty * 10;
                                    break;
                            }

                            prodPlan.FinishTime = prodPlan.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));
                            start = prodPlan.FinishTime;
                        }
                        else
                        {
                            //update break time
                            ProductionPlanBreak prodBreak = await db.ProductionPlanBreaks.Where(m => m.ID.Equals(sched.ID)).FirstOrDefaultAsync();
                            prodBreak.StartTime = start;
                            prodBreak.FinishTime = prodBreak.StartTime.Add(new TimeSpan(0, 0, prodBreak.BreakMinute, 0, 0));
                            start = prodBreak.FinishTime;
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
        public async Task<IHttpActionResult> UpdateRemarks(ProductionPlanHeaderVM productionPlanHeaderVM)
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
                    ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader();
                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ID))
                    {
                        throw new Exception("Production Plan ID is required.");
                    }
                    else
                    {
                        productionPlanHeader = await db.ProductionPlanHeaders.Where(s => s.ID.Equals(productionPlanHeaderVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanHeader == null)
                        {
                            throw new Exception("Production Plan is not recognized.");
                        }

                        if (productionPlanHeader.TransactionStatus.Equals("CLOSED"))
                        {
                            throw new Exception("Production Plan is already closed.");
                        }

                    }

                    if (string.IsNullOrEmpty(productionPlanHeaderVM.Remarks))
                    {
                        ModelState.AddModelError("ProductionPlan.Remarks", "Remarks is required.");
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


                    productionPlanHeader.ModifiedBy = activeUser;
                    productionPlanHeader.ModifiedOn = DateTime.Now;
                    productionPlanHeader.Remarks = productionPlanHeaderVM.Remarks;

                    //check status
                    //if (Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate).Date > productionPlanHeader.ScheduleDate.Date)
                    //{
                    //    productionPlanHeader.TransactionStatus = "POSTPONE";
                    //}
                    //else if (Convert.ToDateTime(productionPlanHeaderVM.ScheduleDate).Date < productionPlanHeader.ScheduleDate.Date)
                    //{
                    //    productionPlanHeader.TransactionStatus = "ADVANCE";
                    //}

                    //if previous status == NEW, then transaction status

                    if (productionPlanHeader.TransactionStatus.Equals("OPEN"))
                    {
                        if (!string.IsNullOrEmpty(productionPlanHeader.Remarks))
                        {
                            productionPlanHeader.TransactionStatus = "OUTSTANDING";
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
        public async Task<IHttpActionResult> RemoveSchedule(ProductionPlanHeaderVM productionPlanHeaderVM)
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
                    ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader();
                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ID))
                    {
                        throw new Exception("Production Plan ID is required.");
                    }
                    else
                    {
                        productionPlanHeader = await db.ProductionPlanHeaders.Where(s => s.ID.Equals(productionPlanHeaderVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanHeader == null)
                        {
                            throw new Exception("Production Plan is not recognized.");
                        }

                        if (productionPlanHeader.TransactionStatus.Equals("CLOSED"))
                        {
                            throw new Exception("Production Plan is already closed.");
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


                    DateTime prevDate = productionPlanHeader.ScheduleDate;
                    TimeSpan prevStartTime = productionPlanHeader.StartTime;
                    TimeSpan prevFinishTime = productionPlanHeader.FinishTime;

     
                    //update all schedule, auto adjustment
                    List<vSchedule> schedules = await db.vSchedules.Where(m => !m.ID.Equals(productionPlanHeader.ID) && m.ScheduleDate.Equals(prevDate) && m.LineNumber.Equals(productionPlanHeader.LineNumber) && m.StartTime >= prevFinishTime).OrderBy(m => m.StartTime).ToListAsync();


                    TimeSpan start = prevStartTime;

                    foreach (vSchedule sched in schedules)
                    {
                        //update each
                        if (sched.ScheduleType.Equals("PROD"))
                        {
                            //update production plan
                            ProductionPlanHeader prodPlan = await db.ProductionPlanHeaders.Where(m => m.ID.Equals(sched.ID)).FirstOrDefaultAsync();
                            prodPlan.StartTime = start;

                            int totalMinutes = 0;

                            switch (prodPlan.LineNumber)
                            {
                                case 1:
                                    totalMinutes = prodPlan.BatchQty * 5;
                                    break;
                                case 2:
                                    totalMinutes = prodPlan.BatchQty * 10;
                                    break;
                            }

                            prodPlan.FinishTime = prodPlan.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));
                            start = prodPlan.FinishTime;
                        }
                        else
                        {
                            //update break time
                            ProductionPlanBreak prodBreak = await db.ProductionPlanBreaks.Where(m => m.ID.Equals(sched.ID)).FirstOrDefaultAsync();
                            prodBreak.StartTime = start;
                            prodBreak.FinishTime = prodBreak.StartTime.Add(new TimeSpan(0, 0, prodBreak.BreakMinute, 0, 0));
                            start = prodBreak.FinishTime;
                        }
                    }


                    db.ProductionPlanOrders.RemoveRange(productionPlanHeader.ProductionPlanOrders);
                    db.ProductionPlanDetails.RemoveRange(productionPlanHeader.ProductionPlanDetails);
                    db.ProductionPlanHeaders.Remove(productionPlanHeader);


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
        public async Task<IHttpActionResult> Close(ProductionPlanHeaderVM productionPlanHeaderVM)
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
                    ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader();
                    if (string.IsNullOrEmpty(productionPlanHeaderVM.ID))
                    {
                        throw new Exception("Production Plan ID is required.");
                    }
                    else
                    {
                        productionPlanHeader = await db.ProductionPlanHeaders.Where(s => s.ID.Equals(productionPlanHeaderVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanHeader == null)
                        {
                            throw new Exception("Production Plan is not recognized.");
                        }

                        if (productionPlanHeader.TransactionStatus.Equals("CLOSED"))
                        {
                            throw new Exception("Production Plan is already closed.");
                        }

                        if(productionPlanHeader.ScheduleDate.Date.AddDays(1) != DateTime.Now.Date)
                        {
                            throw new Exception("Production Plan can be closed on " + Helper.NullDateToString(productionPlanHeader.ScheduleDate.Date.AddDays(1)) + ".");
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


                    productionPlanHeader.FinishedBy = activeUser;
                    productionPlanHeader.FinishedOn = DateTime.Now;


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Close data succeeded.";

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
        //public async Task<IHttpActionResult> Create(ProductionPlanHeaderVM productionPlanHeaderVM)
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

        //            if (!string.IsNullOrEmpty(productionPlanHeaderVM.ItemId))
        //            {
        //                vProduct prod = await db.vProducts.Where(s => s.ID.Equals(productionPlanHeaderVM.ItemId)).FirstOrDefaultAsync();

        //                if (prod == null)
        //                {
        //                    ModelState.AddModelError("ProductionPlanHeader.ItemID", "Product not found.");
        //                }
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("ProductionPlanHeader.ItemID", "Product is required.");
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
        //            var TransactionId = Helper.CreateGuid("PP");

        //            string prefix = TransactionId.Substring(0, 2);
        //            int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
        //            int month = CreatedAt.Month;
        //            string romanMonth = Helper.ConvertMonthToRoman(month);

        //            // get last number, and do increment.
        //            string lastNumber = db.ProductionPlanHeaders.AsQueryable().OrderByDescending(x => x.Code)
        //                .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
        //                .AsEnumerable().Select(x => x.Code).FirstOrDefault();
        //            int currentNumber = 0;

        //            if (!string.IsNullOrEmpty(lastNumber))
        //            {
        //                currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //            }

        //            string runningNumber = string.Format("{0:D3}", currentNumber + 1);

        //            var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);


        //            ProductionPlanHeader productionPlanHeader = new ProductionPlanHeader
        //            {
        //                ID = TransactionId,
        //                Code = Code,
        //                StartDate = productionPlanHeaderVM.StartDate,
        //                FinishDate = productionPlanHeaderVM.FinishDate,
        //                TransactionStatus = "OPEN",
        //                CreatedBy = activeUser,
        //                CreatedOn = CreatedAt
        //            };

        //            vProduct product = await db.vProducts.Where(s => s.ID.Equals(productionPlanHeaderVM.ItemId)).FirstOrDefaultAsync();
        //            productionPlanHeader.ItemId = product.ID;
        //            productionPlanHeader.ItemCode = product.MaterialCode;
        //            productionPlanHeader.ItemName = product.MaterialName;

        //            Formula formula = await db.Formulae.Where(s => s.ItemID.Equals(product.ID) && s.IsActive == true).FirstOrDefaultAsync();
        //            if (formula == null)
        //            {
        //                throw new Exception("No Recipe available for current item. Please add recipe first.");
        //            }

        //            productionPlanHeader.FormulaID = formula.ID;


        //            //check if not based on Order, create dummy order number
        //            //NR150, NR160, NR170, NR370,EP-CL,NR-CL,NBR-CL,CHEM-CL -> product without PRF
        //            //I-(Current Year)-(Current Month)-MaterialCode(RunningNo) ex: I-19-07-00NR1701
        //            //runningNo, counting based on current month


        //            db.ProductionPlanHeaders.Add(productionPlanHeader);
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


        [HttpPost]
        public async Task<IHttpActionResult> CreateBreakTime(ScheduleBreakVM scheduleBreakVM)
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

                    if (string.IsNullOrEmpty(scheduleBreakVM.ScheduleDate))
                    {
                        ModelState.AddModelError("Schedule.ScheduleDate", "Schedule Date is required.");
                    }
                    else
                    {
                        try
                        {
                            DateTime prodDate = Convert.ToDateTime(scheduleBreakVM.ScheduleDate);

                            if (prodDate.Date < DateTime.Now.Date)
                            {
                                ModelState.AddModelError("Schedule.ScheduleDate", "Schedule Date can not back date.");
                            }

                        }
                        catch (FormatException e)
                        {
                            ModelState.AddModelError("Schedule.ScheduleDate", "Bad format Production Date.");
                        }
                    }

                    if(scheduleBreakVM.BreakMinute < 1)
                    {
                        ModelState.AddModelError("Schedule.BreakMinute", "Break Minute is required.");
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

                    //check latest finish time current date
                    //check break time

                    ProductionPlanBreak productionPlanBreak = new ProductionPlanBreak();
                    productionPlanBreak.ScheduleDate = Convert.ToDateTime(scheduleBreakVM.ScheduleDate);
                    productionPlanBreak.ID = Helper.CreateGuid("PPb");
                    productionPlanBreak.LineNumber = scheduleBreakVM.LineNumber;
                    productionPlanBreak.BreakMinute = scheduleBreakVM.BreakMinute;

                    vSchedule schedule = await db.vSchedules.Where(m => m.ScheduleDate.Equals(productionPlanBreak.ScheduleDate) && m.LineNumber.Equals(scheduleBreakVM.LineNumber)).OrderByDescending(m => m.FinishTime).FirstOrDefaultAsync();
                    TimeSpan startTime = new TimeSpan(0, 7, 15, 0, 0);
                    if (schedule == null)
                    {
                        productionPlanBreak.StartTime = startTime;
                    }
                    else
                    {
                        productionPlanBreak.StartTime = schedule.FinishTime;
                    }

                    productionPlanBreak.FinishTime = productionPlanBreak.StartTime.Add(new TimeSpan(0, 0, scheduleBreakVM.BreakMinute, 0, 0));


                    db.ProductionPlanBreaks.Add(productionPlanBreak);

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Create break time succeeded.";

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
        public async Task<IHttpActionResult> GetBreakScheduleById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            ScheduleBreakDTO scheduleBreakDTO = null;
            
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                ProductionPlanBreak productionPlanBreak = await db.ProductionPlanBreaks.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                if (productionPlanBreak == null)
                {
                    throw new Exception("Data not found.");
                }


                scheduleBreakDTO = new ScheduleBreakDTO()
                {
                    ID = productionPlanBreak.ID,
                    ScheduleDate = productionPlanBreak.ScheduleDate.ToString("dd/MM/yyyy"),
                    LineNumber = productionPlanBreak.LineNumber.ToString(),
                    BreakMinute = productionPlanBreak.BreakMinute.ToString()
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

            obj.Add("data", scheduleBreakDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UpdateBreakTime(ScheduleBreakVM scheduleBreakVM)
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
                    ProductionPlanBreak productionPlanBreak = new ProductionPlanBreak();
                    if (string.IsNullOrEmpty(scheduleBreakVM.ID))
                    {
                        throw new Exception("Production Plan Break ID is required.");
                    }
                    else
                    {
                        productionPlanBreak = await db.ProductionPlanBreaks.Where(s => s.ID.Equals(scheduleBreakVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanBreak == null)
                        {
                            throw new Exception("Production Plan Break is not recognized.");
                        }
                    }

                  
                    if (scheduleBreakVM.BreakMinute < 1)
                    {
                        ModelState.AddModelError("Schedule.BreakMinute", "Break Minute is required.");
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

                    productionPlanBreak.BreakMinute = scheduleBreakVM.BreakMinute;
                   

                    //auto adjustment function
                    //get all schedule, and loop update each

                    List<vSchedule> schedules = await db.vSchedules.Where(m => !m.ID.Equals(productionPlanBreak.ID) && m.ScheduleDate.Equals(productionPlanBreak.ScheduleDate) && m.LineNumber.Equals(productionPlanBreak.LineNumber) && m.StartTime >= productionPlanBreak.FinishTime).OrderBy(m => m.StartTime).ToListAsync();

                    //update finish time
                    productionPlanBreak.FinishTime = productionPlanBreak.StartTime.Add(new TimeSpan(0, 0, scheduleBreakVM.BreakMinute, 0, 0));


                    TimeSpan startTime = productionPlanBreak.FinishTime;

                    foreach (vSchedule schedule in schedules)
                    {
                        //update each
                        if (schedule.ScheduleType.Equals("PROD"))
                        {
                            //update production plan
                            ProductionPlanHeader prodPlan = await db.ProductionPlanHeaders.Where(m => m.ID.Equals(schedule.ID)).FirstOrDefaultAsync();
                            prodPlan.StartTime = startTime;

                            int totalMinutes = 0;

                            switch (prodPlan.LineNumber)
                            {
                                case 1:
                                    totalMinutes = prodPlan.BatchQty * 5;
                                    break;
                                case 2:
                                    totalMinutes = prodPlan.BatchQty * 10;
                                    break;
                            }

                            prodPlan.FinishTime = prodPlan.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));
                            startTime = prodPlan.FinishTime;
                        }
                        else
                        {
                            //update break time
                            ProductionPlanBreak prodBreak = await db.ProductionPlanBreaks.Where(m => m.ID.Equals(schedule.ID)).FirstOrDefaultAsync();
                            prodBreak.StartTime = startTime;
                            prodBreak.FinishTime = prodBreak.StartTime.Add(new TimeSpan(0, 0, prodBreak.BreakMinute, 0, 0));
                            startTime = prodBreak.FinishTime;
                        }
                    }

                    //adjust all schedule find by line number, schedule date, and start time < edited productionPlanBreak.FinishTime


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Create break time succeeded.";

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
        public async Task<IHttpActionResult> RemoveBreakTime(ScheduleBreakVM scheduleBreakVM)
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
                    ProductionPlanBreak productionPlanBreak = new ProductionPlanBreak();
                    if (string.IsNullOrEmpty(scheduleBreakVM.ID))
                    {
                        throw new Exception("Production Plan Break ID is required.");
                    }
                    else
                    {
                        productionPlanBreak = await db.ProductionPlanBreaks.Where(s => s.ID.Equals(scheduleBreakVM.ID)).FirstOrDefaultAsync();

                        if (productionPlanBreak == null)
                        {
                            throw new Exception("Production Plan Break is not recognized.");
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



                    //auto adjustment function
                    //get all schedule, and loop update each

                    List<vSchedule> schedules = await db.vSchedules.Where(m => !m.ID.Equals(productionPlanBreak.ID) && m.ScheduleDate.Equals(productionPlanBreak.ScheduleDate) && m.LineNumber.Equals(productionPlanBreak.LineNumber) && m.StartTime >= productionPlanBreak.FinishTime).OrderBy(m => m.StartTime).ToListAsync();


                    TimeSpan startTime = productionPlanBreak.StartTime;

                    foreach (vSchedule schedule in schedules)
                    {
                        //update each
                        if (schedule.ScheduleType.Equals("PROD"))
                        {
                            //update production plan
                            ProductionPlanHeader prodPlan = await db.ProductionPlanHeaders.Where(m => m.ID.Equals(schedule.ID)).FirstOrDefaultAsync();
                            prodPlan.StartTime = startTime;

                            int totalMinutes = 0;

                            switch (prodPlan.LineNumber)
                            {
                                case 1:
                                    totalMinutes = prodPlan.BatchQty * 5;
                                    break;
                                case 2:
                                    totalMinutes = prodPlan.BatchQty * 10;
                                    break;
                            }

                            prodPlan.FinishTime = prodPlan.StartTime.Add(new TimeSpan(0, 0, totalMinutes, 0, 0));
                            startTime = prodPlan.FinishTime;
                        }
                        else
                        {
                            //update break time
                            ProductionPlanBreak prodBreak = await db.ProductionPlanBreaks.Where(m => m.ID.Equals(schedule.ID)).FirstOrDefaultAsync();
                            prodBreak.StartTime = startTime;
                            prodBreak.FinishTime = prodBreak.StartTime.Add(new TimeSpan(0, 0, prodBreak.BreakMinute, 0, 0));
                            startTime = prodBreak.FinishTime;
                        }
                    }

                    db.ProductionPlanBreaks.Remove(productionPlanBreak);

                    //adjust all schedule find by line number, schedule date, and start time < edited productionPlanBreak.FinishTime


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Create break time succeeded.";

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

    }
}
