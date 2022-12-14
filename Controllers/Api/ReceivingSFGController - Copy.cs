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

namespace WMS_BE.Controllers.Api
{
    //public class ReceivingSFGControllerOld : ApiController
    //{
    //    private EIN_WMSEntities db = new EIN_WMSEntities();

    //    [HttpPost]
    //    public IHttpActionResult GetData()
    //    {
    //        int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
    //        int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
    //        int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
    //        string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
    //        string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
    //        string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
    //        string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];


    //        bool IsJudgement = false;
    //        string is_judgement = HttpContext.Current.Request.QueryString["is_judgement"];
    //        if (!string.IsNullOrEmpty(is_judgement) && is_judgement.Contains("1")) IsJudgement = true;

    //        Dictionary<string, Func<vReceivingSFGDataTable, object>> cols = new Dictionary<string, Func<vReceivingSFGDataTable, object>>();
    //        cols.Add("ProductCode", x => x.ProductCode);
    //        cols.Add("ProductName", x => x.ProductName);
    //        cols.Add("TransactionStatus", x => x.TransactionStatus);



    //        IQueryable<vReceivingSFGDataTable> query = db.vReceivingSFGDataTables.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
    //        if (!string.IsNullOrEmpty(search))
    //        {
    //            query = query
    //                .Where(m => m.ProductCode.Contains(search)
    //                || m.ProductName.Contains(search)
    //                || m.LotNo.Contains(search)
    //                );
    //        }

    //        if (IsJudgement) query = query.Where(x => x.NGQty > 0);
    //        int recordsTotal = query.Count();
    //        int recordsFiltered = 0;

    //        if (sortDirection.Equals("asc"))
    //            query = query.OrderBy(cols[sortName]).AsQueryable();
    //        else
    //            query = query.OrderByDescending(cols[sortName]).AsQueryable();

    //        recordsFiltered = query.Count();
    //        var list = query.Skip(start).Take(length).ToList();

    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        string message = "";
    //        bool status = false;
    //        obj.Add("draw", draw);
    //        obj.Add("recordsTotal", recordsTotal);
    //        obj.Add("recordsFiltered", recordsFiltered);
    //        obj.Add("data", list);
    //        obj.Add("status", status);
    //        obj.Add("message", message);

    //        return Ok(obj);
    //    }

    //    //[HttpGet]
    //    //public async Task<IHttpActionResult> GetDataBarcode(string receivingID)
    //    //{
    //    //    var items = db.sp_item_barcode(receivingID);
    //    //    return Ok(items.ToList());
    //    //}

    //    //[HttpGet]
    //    //public async Task<IHttpActionResult> GetDataItemPutAway(string ReceivingDetailId)
    //    //{
    //    //    var model = new PutAwayItemModel();
    //    //    var detail = db.ReceivingSFGDetails.FirstOrDefault(x => x.ID == ReceivingDetailId);
    //    //    model.ID = detail.ID;
    //    //    model.ReceivingID = detail.ReceivingID;
    //    //    model.Barcode = detail.Barcode;
    //    //    model.QtyActual = detail.QtyActual.HasValue ? detail.QtyActual.Value : 0;
    //    //    model.QtyPerBag = detail.QtyPerBag.HasValue ? detail.QtyPerBag.Value : 0;
    //    //    model.QtyBag = detail.QtyBag.HasValue ? detail.QtyBag.Value : 0;
    //    //    model.AvailableQTYBag = detail.QtyBag.HasValue ? detail.QtyBag.Value : 0;

    //    //    var putaways = db.PutawaySFGs.Where(x => x.ReceivingSFGDetailID == ReceivingDetailId);
    //    //    if(putaways != null && putaways.Count() > 0)
    //    //    {
    //    //        model.AvailableQTYBag = model.AvailableQTYBag - putaways.Sum(x => x.PutawayQty);
    //    //    }

    //    //    return Ok(model);
    //    //}

    //    [HttpGet]
    //    public async Task<IHttpActionResult> GetDataJudgement(ReceivingSFG req)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        string message = "";
    //        bool status = false;
    //        List<ReceivingSFG> listDto = new List<ReceivingSFG>();
    //        listDto = db.Database.SqlQuery<ReceivingSFG>("select * from ReceivingSFG where NGQty is not null or NGQty > 0").ToList();

    //        //IQueryable<ReceivingSFG> query = db.ReceivingSFGs.Where(s => s.ID.Equals(req.ID)).AsQueryable();
    //        //IEnumerable<ReceivingSFG> list = Enumerable.Empty<ReceivingSFG>();
    //        //list = query.ToList();
    //        //IEnumerable<ReceivingSFGDTO> listDto = Enumerable.Empty<ReceivingSFGDTO>();

    //        //listDto = from x in list
    //        //          select new ReceivingSFGDTO
    //        //          {
    //        //              ID = x.ID,
    //        //              ReceivingID = x.ReceivingID,
    //        //              Barcode = x.Barcode,
    //        //              QtyActual = x.QtyActual,
    //        //              QtyBag = x.QtyBag,
    //        //              CreatedBy = x.CreatedBy,
    //        //              CreatedDate = x.CreatedDate,
    //        //              QtyPerBag = x.QtyPerBag
    //        //          };
    //        status = true;
    //        message = "Fetch data succeded.";

    //        obj.Add("list", listDto);
    //        obj.Add("status", status);
    //        obj.Add("message", message);

    //        return Ok(obj);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> Datatable()
    //    {
    //        int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
    //        int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
    //        int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
    //        string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
    //        string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
    //        string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
    //        string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        string message = "";
    //        bool status = false;
    //        HttpRequest request = HttpContext.Current.Request;

    //        IEnumerable<vReceivingSFG> list = Enumerable.Empty<vReceivingSFG>();
    //        IEnumerable<ReceivingSFGDTO> pagedData = Enumerable.Empty<ReceivingSFGDTO>();

    //        string warehouseCode = "3001";

    //        IQueryable<vReceivingSFG> query = db.vReceivingSFGs.Where(s => s.WarehouseCode.Equals(warehouseCode) && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))).AsQueryable();

    //        int recordsTotal = db.vReceivingSFGs.Where(s => s.WarehouseCode.Equals(warehouseCode) && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))
    //            ).ToList().Count();
    //        int recordsFiltered = 0;

    //        try
    //        {
    //            query = query
    //                    .Where(m => m.ProductCode.Contains(search)
    //                    || m.ProductName.Contains(search)
    //                    || m.LotNo.Contains(search)
    //                    //|| m.ActualQty.Contains(search)
    //                    //|| m.ReceiveQty.Contains(search)
    //                    || m.Barcode.Contains(search)
    //                    //|| m.InDate.Contains(search)
    //                    //|| m.ExpDate.Contains(search)
    //                    //|| m.ATA.Contains(search)
    //                    );

    //            Dictionary<string, Func<vReceivingSFG, object>> cols = new Dictionary<string, Func<vReceivingSFG, object>>();
    //            cols.Add("ID", x => x.ID);
    //            cols.Add("LotNo", x => x.LotNo);
    //            cols.Add("Barcode", x => x.Barcode);
    //            cols.Add("InDate", x => Helper.NullDateToString2(x.InDate));
    //            cols.Add("ExpDate", x => Helper.NullDateToString2(x.ExpDate));
    //            cols.Add("QtyPerBag", x => Helper.FormatThousand(x.QtyPerBag));
    //            cols.Add("BagQty", x => Helper.FormatThousand(x.Qty / x.QtyPerBag));
    //            cols.Add("Qty", x => Helper.FormatThousand(x.Qty));
    //            cols.Add("ActualQty", x => "16");
    //            cols.Add("ActualQtyPerBag", x => Helper.FormatThousand(x.QtyPerBag));
    //            cols.Add("ActualBagQty", x => "0");
    //            cols.Add("OKQty", x => "0");
    //            cols.Add("OKQtyPerBag", x => Helper.FormatThousand(x.QtyPerBag));
    //            cols.Add("OKBagQty", x => "90");
    //            cols.Add("NGQty", x => Helper.FormatThousand(x.NGQty));
    //            cols.Add("NGBagQty", x => Helper.FormatThousand(x.NGQty / x.QtyPerBag));
    //            cols.Add("NGQtyPerBag", x => Helper.FormatThousand(x.QtyPerBag));
    //            cols.Add("AvailableQty", x => "0");
    //            cols.Add("AvailableBagQty", x => "0");
    //            cols.Add("RequestUoM", x => x.UoM);
    //            cols.Add("UoM", x => x.UoM);
    //            cols.Add("ATA", x => x.ATA);
    //            cols.Add("JudgementQty", x => Helper.FormatThousand(x.JudgementQty));
    //            cols.Add("JudgementBagQty", x => Helper.FormatThousand(x.JudgementQty / x.QtyPerBag));
    //            cols.Add("JudgementMethod", x => x.JudgementMethod);
    //            cols.Add("JudgeBy", x => x.JudgeBy);
    //            cols.Add("JudgeOn", x => Helper.NullDateTimeToString(x.JudgeOn));
    //            cols.Add("PutawayQty", x => Helper.FormatThousand(x.PutawayQty));
    //            cols.Add("PutawayBagQty", x => Helper.FormatThousand(x.PutawayQty / x.QtyPerBag));
    //            cols.Add("PutawayMethod", x => x.PutawayMethod);
    //            cols.Add("PutBy", x => x.PutBy);
    //            cols.Add("PutOn", x => Helper.NullDateTimeToString(x.PutOn));
    //            cols.Add("WarehouseID", x => x.WarehouseID);
    //            cols.Add("WarehouseCode", x => x.WarehouseCode);
    //            cols.Add("WarehouseName", x => x.WarehouseName);
    //            cols.Add("BinRackID", x => x.BinRackID);
    //            cols.Add("BinRackCode", x => x.BinRackCode);
    //            cols.Add("BinRackName", x => x.BinRackName);
    //            cols.Add("TransactionStatus", x => x.TransactionStatus);
    //            cols.Add("ReceivedBy", x => x.ReceivedBy);
    //            cols.Add("ReceivedOn", x => Helper.NullDateTimeToString(x.ReceivedOn));
    //            cols.Add("DisposeQty", x => Helper.FormatThousand(x.DisposeQty));
    //            cols.Add("DisposeBagQty", x => Helper.FormatThousand(x.DisposeQty / x.QtyPerBag));
    //            cols.Add("DisposeMethod", x => x.DisposeMethod);
    //            cols.Add("DisposedBy", x => x.DisposedBy);
    //            cols.Add("DisposedOn", x => Helper.NullDateTimeToString(x.DisposedOn));

    //            if (sortDirection.Equals("asc"))
    //                list = query.OrderBy(cols[sortName]);
    //            else
    //                list = query.OrderByDescending(cols[sortName]);

    //            recordsFiltered = list.Count();

    //            list = list.Skip(start).Take(length).ToList();

    //            if (list != null && list.Count() > 0)
    //            {

    //                pagedData = from receiving in list
    //                            select new ReceivingSFGDTO
    //                            {
    //                                ID = receiving.ID,
    //                                Barcode = receiving.Barcode != null ? receiving.Barcode : "",
    //                                ProductCode = receiving.ProductCode != null ? receiving.ProductCode : "",
    //                                ProductName = receiving.ProductName != null ? receiving.ProductName : "",
    //                                LotNo = receiving.LotNo != null ? receiving.LotNo : "",
    //                                InDate = Helper.NullDateToString2(receiving.InDate),
    //                                ExpDate = Helper.NullDateToString2(receiving.ExpDate),
    //                                QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
    //                                BagQty = Helper.FormatThousand(receiving.Qty / receiving.QtyPerBag),
    //                                Qty = Helper.FormatThousand(receiving.Qty),
    //                                ActualQty = "12",
    //                                ActualBagQty = "4",
    //                                OKQty = string.Format("{0:F2}", (receiving.Qty - receiving.NGQty)),
    //                                OKBagQty = "33",
    //                                NGQty = Helper.FormatThousand(receiving.NGQty.HasValue ? receiving.NGQty.Value : 0),
    //                                NGBagQty = Helper.FormatThousand((receiving.NGQty.HasValue ? receiving.NGQty.Value : 0) / receiving.QtyPerBag),
    //                                UoM = "KG",
    //                                JudgementQty = Helper.FormatThousand(receiving.JudgementQty),
    //                                JudgementBagQty = Helper.FormatThousand(receiving.JudgementQty / receiving.QtyPerBag),
    //                                JudgementMethod = receiving.JudgementMethod != null ? receiving.JudgementMethod : "",
    //                                JudgeBy = receiving.JudgeBy != null ? receiving.JudgeBy : "",
    //                                JudgeOn = Helper.NullDateTimeToString(receiving.JudgeOn),
    //                                PutawayQty = Helper.FormatThousand(receiving.PutawayQty),
    //                                PutawayBagQty = Helper.FormatThousand(receiving.PutawayQty / receiving.QtyPerBag),
    //                                PutawayMethod = receiving.PutawayMethod != null ? receiving.PutawayMethod : "",
    //                                PutBy = receiving.PutBy != null ? receiving.PutBy : "",
    //                                PutOn = Helper.NullDateTimeToString(receiving.PutOn),
    //                                WarehouseID = receiving.WarehouseID != null ? receiving.WarehouseID : "",
    //                                WarehouseCode = receiving.WarehouseCode != null ? receiving.WarehouseCode : "",
    //                                WarehouseName = receiving.WarehouseName != null ? receiving.WarehouseName : "",
    //                                BinRackID = receiving.BinRackID != null ? receiving.BinRackID : "",
    //                                BinRackCode = receiving.BinRackCode != null ? receiving.BinRackCode : "",
    //                                BinRackName = receiving.BinRackName != null ? receiving.BinRackName : "",
    //                                ATA = Helper.NullDateToString2(receiving.ATA),
    //                                TransactionStatus = receiving.TransactionStatus,
    //                                ReceivedBy = receiving.ReceivedBy != null ? receiving.ReceivedBy : "",
    //                                ReceivedOn = Helper.NullDateTimeToString(receiving.ReceivedOn),
    //                                DisposeQty = Helper.FormatThousand(receiving.DisposeQty),
    //                                DisposeBagQty = Helper.FormatThousand(receiving.DisposeQty / receiving.QtyPerBag),
    //                                DisposeMethod = receiving.PutawayMethod != null ? receiving.DisposeMethod : "",
    //                                DisposedBy = receiving.PutBy != null ? receiving.DisposedBy : "",
    //                                DisposedOn = Helper.NullDateTimeToString(receiving.DisposedOn),
    //                                AvailableQty = receiving.ID,
    //                                AvailableBagQty = receiving.ID
    //                            };
    //            }

    //            status = true;
    //            message = "Fetch data succeeded.";
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //            return BadRequest();
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //            return NotFound();
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("draw", draw);
    //        obj.Add("recordsTotal", recordsTotal);
    //        obj.Add("recordsFiltered", recordsFiltered);
    //        obj.Add("data", pagedData);
    //        obj.Add("status", status);
    //        obj.Add("message", message);

    //        return Ok(obj);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> Upload()
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        HttpRequest request = HttpContext.Current.Request;

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;

    //        string warehouseID = "3001";
    //        try
    //        {
    //            string token = "";
    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

    //            if (activeUser != null)
    //            {
    //                if (!string.IsNullOrEmpty(warehouseID))
    //                {
    //                    Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(warehouseID)).FirstOrDefaultAsync();

    //                    if (wh != null)
    //                    {
    //                        if (request.Files.Count > 0)
    //                        {
    //                            HttpPostedFile file = request.Files[0];

    //                            if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
    //                            {
    //                                if (file.ContentLength < (10 * 1024 * 1024))
    //                                {
    //                                    try
    //                                    {
    //                                        Stream stream = file.InputStream;
    //                                        IExcelDataReader reader = null;
    //                                        if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
    //                                        {
    //                                            reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

    //                                        }
    //                                        else
    //                                        {
    //                                            reader = ExcelReaderFactory.CreateBinaryReader(stream);
    //                                        }

    //                                        DataSet result = reader.AsDataSet();
    //                                        reader.Close();

    //                                        DataTable dt = result.Tables[0];

    //                                        string prefix = "RSFG";

    //                                        List<ReceivingSFG> updatedList = new List<ReceivingSFG>();

    //                                        foreach (DataRow row in dt.Rows)
    //                                        {
    //                                            if (dt.Rows.IndexOf(row) != 0)
    //                                            {
    //                                                ReceivingSFG receiving = new ReceivingSFG();
                                                    

    //                                                string x = row[1].ToString();

    //                                                SemiFinishGood sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(x)).FirstOrDefaultAsync();

    //                                                if (sfg != null)
    //                                                {

    //                                                    string InDate = row[0].ToString();
    //                                                    if (!string.IsNullOrEmpty(InDate))
    //                                                    {
    //                                                        try
    //                                                        {
    //                                                            receiving.InDate = DateTime.ParseExact(InDate, "yyyy-MM-dd", null);
    //                                                        }
    //                                                        catch (Exception)
    //                                                        {

    //                                                        }
    //                                                    }
    //                                                    receiving.SemiFinishGoodID = sfg.ID;
    //                                                    receiving.ProductCode = row[1].ToString();
    //                                                    receiving.ProductName = row[2].ToString();
    //                                                    string qty = row[3].ToString();
    //                                                    int index = qty.LastIndexOf(".");
    //                                                    if (index > 0)
    //                                                    {
    //                                                        qty = qty.Substring(0, index);
    //                                                    }
    //                                                    receiving.Qty = Decimal.Parse(qty);
    //                                                    //receiving.Qty = Math.Floor(Convert.ToDecimal(qty));
    //                                                    receiving.QtyPerBag = sfg.WeightPerBag;
    //                                                    receiving.UoM = row[4].ToString();
    //                                                    receiving.LotNo = row[5].ToString();
    //                                                    string ProductionDate = row[6].ToString();
    //                                                    if (!string.IsNullOrEmpty(ProductionDate))
    //                                                    {
    //                                                        try
    //                                                        {
    //                                                            receiving.ProductionDate = DateTime.ParseExact(ProductionDate, "yyyy-MM-dd", null);
    //                                                        }
    //                                                        catch (Exception)
    //                                                        {

    //                                                        }
    //                                                    }

    //                                                    string ExpDate = row[7].ToString();
    //                                                    if (!string.IsNullOrEmpty(ExpDate))
    //                                                    {
    //                                                        try
    //                                                        {
    //                                                            receiving.ExpDate = DateTime.ParseExact(ExpDate, "yyyy-MM-dd", null);
    //                                                        }
    //                                                        catch (Exception)
    //                                                        {

    //                                                        }
    //                                                    }

    //                                                    receiving.ID = Helper.CreateGuid(prefix);
    //                                                    receiving.TransactionStatus = "OPEN";
    //                                                    receiving.WarehouseID = wh.ID;
    //                                                    receiving.WarehouseCode = wh.Code;
    //                                                    receiving.WarehouseName = wh.Name;
    //                                                    receiving.CreatedBy = activeUser;
    //                                                    receiving.CreatedOn = DateTime.Now;

    //                                                    //ReceivingSFG checker = await db.ReceivingSFGs.Where(s => s.Barcode.Equals(receiving.Barcode) && s.LotNo.Equals(receiving.LotNo)).FirstOrDefaultAsync();

    //                                                    //if (checker == null)
    //                                                        db.ReceivingSFGs.Add(receiving);
    //                                                }
    //                                                //else
    //                                                //{
    //                                                //    throw new Exception("One or more WIP FG is not recognized.");
    //                                                //}

    //                                            }
    //                                        }

    //                                        await db.SaveChangesAsync();
    //                                        message = "Upload succeeded.";
    //                                        status = true;


    //                                    }
    //                                    catch (Exception e)
    //                                    {
    //                                        message = string.Format("Upload item failed. {0}", e.Message);
    //                                    }
    //                                }
    //                                else
    //                                {
    //                                    message = "Upload failed. Maximum allowed file size : 10MB ";
    //                                }
    //                            }
    //                            else
    //                            {
    //                                message = "Upload item failed. File is invalid.";
    //                            }
    //                        }
    //                        else
    //                        {
    //                            message = "No file uploaded.";
    //                        }
    //                    }
    //                    else
    //                    {
    //                        message = "Warehouse is not recognized.";
    //                    }
    //                }
    //                else
    //                {
    //                    message = "Warehouse is required.";
    //                }
    //            }
    //            else
    //            {
    //                message = "Token is no longer valid. Please re-login.";
    //            }
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //            return BadRequest();
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //            return NotFound();
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }


    //        obj.Add("status", status);
    //        obj.Add("message", message);

    //        return Ok(obj);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> Update(ReceivingSFGVM receivingVM)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;

    //        try
    //        {
    //            string token = "";

    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

    //            if (activeUser != null)
    //            {
    //                ReceivingSFG receiving = await db.ReceivingSFGs.Where(s => s.ID.Equals(receivingVM.ID)).FirstOrDefaultAsync();

    //                SemiFinishGood sfg = null;

    //                if (receiving == null)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.ID", "Receiving is not recognized.");
    //                }
    //                else
    //                {
    //                    if (receiving.ReceivedOn != null)
    //                    {
    //                        ModelState.AddModelError("ReceivingSFG.TransactionStatus", "Received item is already inspected.");
    //                    }

    //                    sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(receiving.ProductCode)).FirstOrDefaultAsync();
    //                }

    //                if (receivingVM.OKQty <= 0)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.OKQty", "Receive Qty can not be empty or below zero.");
    //                }
    //                else if (receivingVM.OKQty > receiving.Qty)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.OKQty", "Receive Qty can not exceed the requested Qty.");
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

    //                receiving.NGQty = receiving.Qty - receivingVM.OKQty;
    //                decimal sisa = receivingVM.OKQty / receiving.QtyPerBag;
    //                sisa = receivingVM.OKQty - (Math.Floor(sisa) * receiving.QtyPerBag);
                    
    //                receiving.ATA = DateTime.Now;
    //                //receiving.Barcode = receiving.ProductCode.PadRight(7) + receiving.InDate.ToString("yyyyMMdd").Substring(1) + receiving.ExpDate.ToString("yyyyMMdd").Substring(2)+receivingVM.OKQty.ToString();
    //                receiving.TransactionStatus = "PROGRESS";
    //                receiving.ReceivedBy = activeUser;
    //                receiving.ReceivedOn = DateTime.Now;
    //                string bcd = receiving.ProductCode.PadRight(7) + receiving.InDate.ToString("yyyyMMdd").Substring(1) + receiving.ExpDate.ToString("yyyyMMdd").Substring(2);
    //                bcd = bcd + sisa.ToString();

    //                // tambahan disini
    //                Guid guid1 = Guid.NewGuid();
    //                string barcodeOK = "";
    //                int actBag = Decimal.ToInt32(receivingVM.OKQty / receiving.QtyPerBag);
    //                if (actBag < 1)
    //                {
    //                    actBag = 1;
    //                    sisa = 0;
    //                    //barcodeOK = receiving.ProductCode.PadRight(7) + actBag.ToString("0") + receiving.LotNo;
    //                    barcodeOK = string.Format("{0}{1}{2}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(actBag).PadLeft(6), receiving.LotNo);
    //                    receiving.QtyPerBag = receivingVM.OKQty;
    //                }
    //                else
    //                {
    //                    //barcodeOK = receiving.ProductCode.PadRight(7) + receiving.QtyPerBag.ToString("0") + receiving.LotNo;
    //                    //barcodeOK = string.Format("{0}{1}{2}", receiving.ProductCode.PadRight(7), receiving.QtyPerBag.ToString().PadLeft(6), receiving.LotNo);
    //                    barcodeOK = string.Format("{0}{1}{2}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(receiving.QtyPerBag).PadLeft(6), receiving.LotNo);
    //                }

                    

    //                ReceivingSFGDetail receivingSFGDetail = new ReceivingSFGDetail();
    //                string strID = guid1.ToString();
    //                receivingSFGDetail.ID = strID;
    //                receivingSFGDetail.ReceivingID = receiving.ID;
    //                receivingSFGDetail.Barcode = barcodeOK.Replace(" ","");
    //                receivingSFGDetail.QtyActual = receivingVM.OKQty -sisa;
    //                receivingSFGDetail.CreatedBy = activeUser;
    //                receivingSFGDetail.QtyPerBag = Decimal.ToInt32(receiving.QtyPerBag);
    //                receivingSFGDetail.CreatedDate = DateTime.Now;
    //                db.ReceivingSFGDetails.Add(receivingSFGDetail);

    //                if (sisa > 0)
    //                {
    //                    Guid guid = Guid.NewGuid();
    //                    //string barcodeReceh = receiving.ProductCode.PadRight(7) + sisa.ToString("0") + receiving.LotNo;
    //                    string barcodeReceh = string.Format("{0}{1}{2}", receiving.ProductCode.PadRight(7), Helper.FormatThousand(sisa).PadLeft(6), receiving.LotNo);
    //                    string strID_receh = guid.ToString();
    //                    ReceivingSFGDetail receivingSFGDetail_receh = new ReceivingSFGDetail();
    //                    receivingSFGDetail_receh.ID = strID_receh;
    //                    receivingSFGDetail_receh.ReceivingID = receiving.ID;
    //                    receivingSFGDetail_receh.Barcode = barcodeReceh.Replace(" ", "");
    //                    receivingSFGDetail_receh.QtyActual = sisa;
    //                    receivingSFGDetail_receh.QtyPerBag = sisa;
    //                    receivingSFGDetail_receh.CreatedBy = activeUser;
    //                    receivingSFGDetail_receh.CreatedDate = DateTime.Now;
    //                    db.ReceivingSFGDetails.Add(receivingSFGDetail_receh);
    //                }
    //                //sampai sini 
    //                await db.SaveChangesAsync();

    //                status = true;
    //                message = "Receiving succeeded.";

    //            }
    //            else
    //            {
    //                message = "Token is no longer valid. Please re-login.";
    //            }
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("status", status);
    //        obj.Add("message", message);
    //        obj.Add("error_validation", customValidationMessages);

    //        return Ok(obj);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> JudgementManual(JudgementSFGVM judgementVM)
    //    {
    //        judgementVM.JudgementMethod = "Manual";
    //        return await Judge(judgementVM);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> JudgementScan(JudgementSFGVM judgementVM)
    //    {
    //        judgementVM.JudgementMethod = "Scan";
    //        return await Judge(judgementVM);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> NewJudgment(ReceivingSFGVM receivingVM)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;
    //        try
    //        {
    //            string token = "";

    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();
    //            if (activeUser != null)
    //            {
    //                ReceivingSFG receiving = await db.ReceivingSFGs.Where(s => s.ID.Equals(receivingVM.ID)).FirstOrDefaultAsync();

    //                if (receiving == null)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.ID", "Receiving is not recognized.");
    //                }
                    

    //                if (receivingVM.NGQty <= 0)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.NGQty", "Judgemet Qty can not be empty or below zero.");
    //                }

    //                if (receivingVM.NGQty > receiving.NGQty)
    //                {
    //                    ModelState.AddModelError("ReceivingSFG.NGQty", "Judgemet Qty cmore than NGQty.");
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
    //                //logic start here
    //                Decimal SisaAwal = receiving.NGQty ?? 0;
    //                Decimal qtyjudgement = receivingVM.NGQty;
    //                Decimal SisaAkhir = SisaAwal - qtyjudgement;
    //                receiving.NGQty = SisaAkhir;

    //                string exec = "exec sp_judgement '" + receivingVM.ID + "'," + receivingVM.NGQty.ToString().Replace(",",".") + ",'" + activeUser + "'";
    //                int execSP = db.Database.ExecuteSqlCommand(exec);

    //                JudgementSFG judgement = new JudgementSFG()
    //                {
    //                    ID = Helper.CreateGuid(""),
    //                    ReceivingID = receiving.ID,
    //                    JudgementQty = receivingVM.NGQty,
    //                    JudgementMethod = string.Empty,
    //                    JudgeOn = DateTime.Now,
    //                    JudgeBy = activeUser,
    //                };

    //                db.JudgementSFGs.Add(judgement);
    //                db.SaveChanges();

    //                status = true;
    //                message = "Receiving succeeded.";
    //            }

    //        }


    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("status", status);
    //        obj.Add("message", message);
    //        obj.Add("error_validation", customValidationMessages);

    //        return Ok(obj);

    //    }
        
    //    public async Task<IHttpActionResult> Judge(JudgementSFGVM judgementVM)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;

    //        try
    //        {
    //            string token = "";
    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

    //            if (activeUser != null)
    //            {
    //                ReceivingSFG receiving = null;

    //                if (string.IsNullOrEmpty(judgementVM.Barcode))
    //                {
    //                    ModelState.AddModelError("Receiving.Barcode", "Barcode is not required.");
    //                }

    //                if (string.IsNullOrEmpty(judgementVM.LotNo))
    //                {
    //                    ModelState.AddModelError("Receiving.LotNo", "Lot Number is not required.");
    //                }
    //                else
    //                {
    //                    if (!string.IsNullOrEmpty(judgementVM.Barcode))
    //                    {
    //                        receiving = await db.ReceivingSFGs.Where(s => s.ID == judgementVM.HeaderID && s.LotNo.Equals(judgementVM.LotNo)).FirstOrDefaultAsync();

    //                        if (receiving == null)
    //                        {
    //                            ModelState.AddModelError("Receiving.Barcode", "Receiving is not recognized.");
    //                        }
    //                        else
    //                        {
    //                            if (receiving.TransactionStatus.Equals("CLOSED"))
    //                            {
    //                                ModelState.AddModelError("Receiving.TransactionStatus", "Receiving can no longer accept Judgement. Transaction already closed.");
    //                            }
    //                            else if (receiving.TransactionStatus.Equals("CANCELLED"))
    //                            {
    //                                ModelState.AddModelError("Receiving.TransactionStatus", "Receiving can no longer accept Judgement. Transaction already cancelled.");
    //                            }
    //                        }
    //                    }
    //                }

    //                if (receiving.ReceivedOn == null)
    //                {
    //                    ModelState.AddModelError("Receiving.InspectedOn", "WIP FG has not been Received.");
    //                }

    //                if (receiving.NGQty <= 0)
    //                {
    //                    ModelState.AddModelError("Receiving.NGQty", "Current WIP FG has no more NG.");
    //                }

    //                if (judgementVM.Qty * receiving.QtyPerBag > receiving.NGQty)
    //                {
    //                    ModelState.AddModelError("Judgement.Qty", "Judgement quantity can not exceed the NG quantity.");
    //                }
    //                else if (judgementVM.Qty <= 0)
    //                {
    //                    ModelState.AddModelError("Judgement.Qty", "Judgement quantity can not be zero or below.");
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

    //                JudgementSFG judgement = new JudgementSFG()
    //                {
    //                    ID = Helper.CreateGuid(""),
    //                    //SemiFinishGoodID = receiving.SemiFinishGoodID,
    //                    //Barcode = receiving.Barcode,
    //                    //LotNo = receiving.LotNo,
    //                    ReceivingID = receiving.ID,
    //                    JudgementQty = judgementVM.Qty * receiving.QtyPerBag,
    //                    JudgementMethod = "Manual",
    //                    JudgeOn = DateTime.Now,
    //                    JudgeBy = activeUser,
    //                };


    //                receiving.NGQty -= judgementVM.Qty * receiving.QtyPerBag;

    //                db.JudgementSFGs.Add(judgement);

    //                await db.SaveChangesAsync();
    //                status = true;
    //                message = "Judgement succeeded.";
    //            }
    //            else
    //            {
    //                message = "Token is no longer valid. Please re-login.";
    //            }
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("status", status);
    //        obj.Add("message", message);
    //        obj.Add("error_validation", customValidationMessages);
            
    //        return Ok(obj);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> DisposeManual(JudgementSFGVM judgementVM)
    //    {
    //        judgementVM.JudgementMethod = "Manual";
    //        return await Dispose(judgementVM);
    //    }

    //    [HttpPost]
    //    public async Task<IHttpActionResult> DisposeScan(JudgementSFGVM judgementVM)
    //    {
    //        judgementVM.JudgementMethod = "Scan";
    //        return await Dispose(judgementVM);
    //    }

    //    public async Task<IHttpActionResult> Dispose(JudgementSFGVM judgementVM)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;

    //        try
    //        {
    //            string token = "";
    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

    //            if (activeUser != null)
    //            {
    //                ReceivingSFG receiving = null;
    //                if (string.IsNullOrEmpty(judgementVM.Barcode))
    //                {
    //                    ModelState.AddModelError("Receiving.Barcode", "Barcode is not required.");
    //                }

    //                if (string.IsNullOrEmpty(judgementVM.LotNo))
    //                {
    //                    ModelState.AddModelError("Receiving.LotNo", "Lot Number is not required.");
    //                }
    //                else
    //                {
    //                        receiving = db.ReceivingSFGs.FirstOrDefault(x => x.ID == judgementVM.HeaderID);
    //                        if (receiving == null)
    //                        {
    //                            ModelState.AddModelError("Receiving.Item", "Receiving is not recognized.");
    //                        }
    //                        else
    //                        {
    //                            if (receiving.TransactionStatus.Equals("CLOSED"))
    //                            {
    //                                ModelState.AddModelError("Receiving.TransactionStatus", "Receiving can no longer accept Judgement. Transaction already closed.");
    //                            }
    //                            else if (receiving.TransactionStatus.Equals("CANCELLED"))
    //                            {
    //                                ModelState.AddModelError("Receiving.TransactionStatus", "Receiving can no longer accept Judgement. Transaction already cancelled.");
    //                            }
    //                        }
    //                }

    //                if (receiving.ReceivedOn == null)
    //                {
    //                    ModelState.AddModelError("Receiving.InspectedOn", "WIP FG has not been Received.");
    //                }

    //                if (receiving.NGQty <= 0)
    //                {
    //                    ModelState.AddModelError("Receiving.NGQty", "Current WIP FG has no more NG.");
    //                }

    //                //if (judgementVM.Qty * receiving.QtyPerBag > receiving.NGQty)
    //                if (judgementVM.Qty > receiving.NGQty)
    //                {
    //                    ModelState.AddModelError("Disposal.Qty", "Disposed quantity can not exceed the NG quantity.");
    //                }
    //                else if (judgementVM.Qty <= 0)
    //                {
    //                    ModelState.AddModelError("Disposal.Qty", "Disposed quantity can not be zero or below.");
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

    //                DisposalSFG disposal = new DisposalSFG()
    //                {
    //                    ID = Helper.CreateGuid(""),
    //                    SemiFinishGoodID = receiving.SemiFinishGoodID,
    //                    Barcode = string.Empty,
    //                    LotNo = receiving.LotNo,
    //                    ReceivingID = receiving.ID,
    //                    //DisposeQty = judgementVM.Qty * receiving.QtyPerBag,
    //                    DisposeQty = judgementVM.Qty,
    //                    DisposeMethod = judgementVM.JudgementMethod,
    //                    DisposedOn = DateTime.Now,
    //                    DisposedBy = activeUser,
    //                };

    //                //receiving.NGQty -= judgementVM.Qty * receiving.QtyPerBag;
    //                receiving.NGQty -= judgementVM.Qty;

    //                db.DisposalSFGs.Add(disposal);

    //                //if (receiving.NGQty == 0 && receiving.OKQty == temp.PutawayQty)
    //                //{
    //                //    receiving.TransactionStatus = "CLOSED";
    //                //}

    //                await db.SaveChangesAsync();
    //                status = true;
    //                message = "Disposal succeeded.";
    //            }
    //            else
    //            {
    //                message = "Token is no longer valid. Please re-login.";
    //            }
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("status", status);
    //        obj.Add("message", message);
    //        obj.Add("error_validation", customValidationMessages);

    //        return Ok(obj);
    //    }

    //    //[HttpPost]
    //    //public async Task<IHttpActionResult> PutawayManual(PutawaySFGVM putawayVM)
    //    //{
    //    //    //putawayVM.PutawayMethod = "Manual";
    //    //    return await Putaway(putawayVM);
    //    //}

    //    //[HttpPost]
    //    //public async Task<IHttpActionResult> PutawayScan(PutawaySFGVM putawayVM)
    //    //{
    //    //    //putawayVM.PutawayMethod = "Scan";
    //    //    return await Putaway(putawayVM);
    //    //}

    //    //public async Task<IHttpActionResult> Putaway(PutawaySFGVM putawayVM)
    //    //{
    //    //    Dictionary<string, object> obj = new Dictionary<string, object>();
    //    //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //    //    string message = "";
    //    //    bool status = false;
    //    //    var re = Request;
    //    //    var headers = re.Headers;

    //    //    try
    //    //    {
    //    //        string token = "";
    //    //        if (headers.Contains("token"))
    //    //        {
    //    //            token = headers.GetValues("token").First();
    //    //        }

    //    //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

    //    //        if (activeUser != null)
    //    //        {

    //    //            if (string.IsNullOrEmpty(putawayVM.Barcode))
    //    //            {
    //    //                ModelState.AddModelError("Receiving.Barcode", "Barcode is not required.");
    //    //            }

    //    //            //updated by indra started here.. !!!                    
    //    //            var binRack1 = db.BinRacks.FirstOrDefault(x => x.ID == putawayVM.BinRackID);
    //    //            var ReceivingSFGDetail = db.ReceivingSFGDetails.FirstOrDefault(x => x.ID == putawayVM.ReceiveDetailID);
    //    //            var ReceivingSFG = db.ReceivingSFGs.FirstOrDefault(x => x.ID == ReceivingSFGDetail.ReceivingID);
    //    //            var putaways = db.PutawaySFGs.Where(x => x.ReceivingSFGDetailID == putawayVM.ReceiveDetailID).ToList();



    //    //            if (ReceivingSFGDetail == null)
    //    //            {
    //    //                ModelState.AddModelError("Receiving Detail.ID", "ID is not found.");
    //    //            }
    //    //            //else if (putawayVM.Qty > ReceivingSFGDetail.QtyBag)
    //    //            //{
    //    //            //    ModelState.AddModelError("Receiving.QTy", "Not Allowed");
    //    //            //}
    //    //            //else if (putaways != null && putaways.Count > 0)
    //    //            //{
    //    //            //    if (putawayVM.Qty > (ReceivingSFGDetail.QtyBag - (putaways.Sum(x => x.PutawayQty)/ReceivingSFG.QtyPerBag)))
    //    //            //    {
    //    //            //        ModelState.AddModelError("Receiving.QTy", "Not Allowed");
    //    //            //    }
    //    //            //}
    //    //            //until here

    //    //            if (!ModelState.IsValid)
    //    //            {
    //    //                foreach (var state in ModelState)
    //    //                {
    //    //                    string field = state.Key.Split('.')[1];
    //    //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
    //    //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
    //    //                }

    //    //                throw new Exception("Input is not valid");
    //    //            }
    //    //            else
    //    //            {
    //    //                string LotNomor = putawayVM.LotNo;
    //    //                string SemiFinishGoodID = ReceivingSFG.SemiFinishGoodID;
    //    //                StockSFG stock = await db.StockSFGs.Where(s => s.BinRackID.Equals(binRack1.ID) && s.Barcode.Equals(putawayVM.Barcode)).FirstOrDefaultAsync();

    //    //                if (stock == null)
    //    //                {
    //    //                    stock = new StockSFG()
    //    //                    {
    //    //                        ID = Helper.CreateGuid(""),
    //    //                        SemiFinishGoodID = SemiFinishGoodID,
    //    //                        Barcode = putawayVM.Barcode,
    //    //                        LotNumber = LotNomor,
    //    //                        InDate = ReceivingSFG.InDate,
    //    //                        ExpiredDate = ReceivingSFG.ExpDate,
    //    //                        Quantity = putawayVM.Qty * ReceivingSFGDetail.QtyPerBag,
    //    //                        QtyPerBag = ReceivingSFGDetail.QtyPerBag,
    //    //                        BinRackID = binRack1.ID,
    //    //                        ReceivedAt = DateTime.Now
    //    //                    };

    //    //                    db.StockSFGs.Add(stock);
    //    //                }
    //    //                else
    //    //                {
    //    //                    stock.Quantity += putawayVM.Qty * ReceivingSFGDetail.QtyPerBag;
    //    //                    stock.ReceivedAt = DateTime.Now;
    //    //                }
    //    //            }

    //    //            // log putaway
    //    //            PutawaySFG putaway = new PutawaySFG();
    //    //            putaway.ID = Helper.CreateGuid("");
    //    //            putaway.ReceivingSFGDetailID = ReceivingSFGDetail.ID;
    //    //            putaway.SemiFinishGoodID = ReceivingSFG.SemiFinishGoodID;
    //    //            putaway.Barcode = ReceivingSFGDetail.Barcode;
    //    //            putaway.LotNo = ReceivingSFG.LotNo;
    //    //            putaway.PutawayQty = putawayVM.Qty * ReceivingSFGDetail.QtyPerBag;
    //    //            putaway.BinRackID = putawayVM.BinRackID;
    //    //            putaway.BinRackCode = binRack1.Code;
    //    //            putaway.BinRackName = binRack1.Name;
    //    //            putaway.PutOn = DateTime.Now;
    //    //            putaway.PutBy = activeUser;
    //    //            putaway.PutawayMethod = "MANUAL";
    //    //            db.PutawaySFGs.Add(putaway);

    //    //            await db.SaveChangesAsync();
    //    //            status = true;
    //    //            message = "Putaway succeeded.";
    //    //        }
    //    //        else
    //    //        {
    //    //            message = "Token is no longer valid. Please re-login.";
    //    //        }
    //    //    }
    //    //    catch (HttpRequestException reqpEx)
    //    //    {
    //    //        message = reqpEx.Message;
    //    //    }
    //    //    catch (HttpResponseException respEx)
    //    //    {
    //    //        message = respEx.Message;
    //    //    }
    //    //    catch (Exception ex)
    //    //    {
    //    //        message = ex.Message;
    //    //    }

    //    //    obj.Add("status", status);
    //    //    obj.Add("message", message);
    //    //    obj.Add("error_validation", customValidationMessages);

    //    //    return Ok(obj);
    //    //}

    //    public async Task<IHttpActionResult> UpdateStatus(string id, string transactionStatus)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;

    //        try
    //        {
    //            string token = "";

    //            if (headers.Contains("token"))
    //            {
    //                token = headers.GetValues("token").First();
    //            }

    //            string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

    //            if (activeUser != null)
    //            {
    //                ReceivingSFG receiving = await db.ReceivingSFGs.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

    //                if (transactionStatus.Equals("CLOSED"))
    //                {
    //                    if (!receiving.TransactionStatus.Equals("PROGRESS"))
    //                    {
    //                        throw new Exception("Transaction can not be closed.");
    //                    }

    //                    message = "Data closing succeeded.";
    //                }
    //                else if (transactionStatus.Equals("CANCELLED"))
    //                {
    //                    if (!receiving.TransactionStatus.Equals("OPEN"))
    //                    {
    //                        throw new Exception("Transaction can not be cancelled.");
    //                    }

    //                    message = "Data cancellation succeeded.";
    //                }
    //                else
    //                {
    //                    throw new Exception("Transaction Status is not recognized.");
    //                }

    //                receiving.TransactionStatus = transactionStatus;
    //                await db.SaveChangesAsync();
    //                status = true;
    //            }
    //            else
    //            {
    //                message = "Token is no longer valid. Please re-login.";
    //            }
    //        }
    //        catch (HttpRequestException reqpEx)
    //        {
    //            message = reqpEx.Message;
    //            return BadRequest();
    //        }
    //        catch (HttpResponseException respEx)
    //        {
    //            message = respEx.Message;
    //            return NotFound();
    //        }
    //        catch (Exception ex)
    //        {
    //            message = ex.Message;
    //        }

    //        obj.Add("status", status);
    //        obj.Add("message", message);
    //        return Ok(obj);
    //    }

    //    public async Task<IHttpActionResult> Cancel(string id)
    //    {
    //        return await UpdateStatus(id, "CANCELLED");
    //    }

    //    public async Task<IHttpActionResult> Close(string id)
    //    {
    //        return await UpdateStatus(id, "CLOSED");
    //    }
    //}
}
