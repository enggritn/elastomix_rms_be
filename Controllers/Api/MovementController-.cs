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
    //public class MovementController : ApiController
    //{
    //    private EIN_WMSEntities db = new EIN_WMSEntities();

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

    //        IEnumerable<Movement> list = Enumerable.Empty<Movement>();
    //        IEnumerable<MovementDTO> pagedData = Enumerable.Empty<MovementDTO>();

    //        IQueryable<Movement> query = db.Movements.AsQueryable();

    //        int recordsTotal = db.Movements.Count();
    //        int recordsFiltered = 0;

    //        try
    //        {
    //            query = query
    //                    .Where(m => m.Code.Contains(search)
    //                    || m.Barcode.Contains(search)
    //                    || m.MaterialCode.Contains(search)
    //                    || m.MaterialName.Contains(search)
    //                    || m.PrevBinRackCode.Contains(search)
    //                    || m.PrevBinRackName.Contains(search)
    //                    //|| m.NewBinRackCode.Contains(search)
    //                    || m.NewBinRackName.Contains(search)
    //                    //|| m.TransactionStatus.Contains(search)
    //                    || m.CreatedBy.Contains(search)
    //                    || m.ModifiedBy.Contains(search)
    //                    );

    //            Dictionary<string, Func<Movement, object>> cols = new Dictionary<string, Func<Movement, object>>();
    //            cols.Add("ID", x => x.ID);
    //            cols.Add("Code", x => x.Code);
    //            cols.Add("RawMaterialID", x => x.RawMaterialID);
    //            cols.Add("MaterialCode", x => x.MaterialCode);
    //            cols.Add("MaterialName", x => x.MaterialName);
    //            cols.Add("Barcode", x => x.Barcode);
    //            cols.Add("LotNo", x => x.LotNo);
    //            cols.Add("InDate", x => x.InDate);
    //            cols.Add("ExpDate", x => x.ExpDate);
    //            cols.Add("PrevBinRackID", x => x.PrevBinRackID);
    //            cols.Add("PrevBinRackCode", x => x.PrevBinRackCode);
    //            cols.Add("PrevBinRackName", x => x.PrevBinRackName);
    //            cols.Add("Qty", x => x.Qty);
    //            cols.Add("NewBinRackID", x => x.NewBinRackID);
    //            cols.Add("NewBinRackCode", x => x.NewBinRackCode);
    //            cols.Add("NewBinRackName", x => x.NewBinRackName);
    //            cols.Add("TransactionStatus", x => x.TransactionStatus);
    //            cols.Add("CreatedBy", x => x.CreatedBy);
    //            cols.Add("CreatedOn", x => x.CreatedOn);
    //            cols.Add("ModifiedBy", x => x.ModifiedBy);
    //            cols.Add("ModifiedOn", x => x.ModifiedOn);

    //            if (sortDirection.Equals("asc"))
    //                list = query.OrderBy(cols[sortName]);
    //            else
    //                list = query.OrderByDescending(cols[sortName]);

    //            recordsFiltered = list.Count();

    //            list = list.Skip(start).Take(length).ToList();

    //            if (list != null && list.Count() > 0)
    //            {
    //                pagedData = from x in list
    //                            select new MovementDTO
    //                            {
    //                                ID = x.ID,
    //                                Code = x.Code,
    //                                Barcode = x.Barcode,
    //                                RawMaterialID = x.RawMaterialID,
    //                                MaterialCode = x.MaterialCode,
    //                                MaterialName = x.MaterialName,
    //                                LotNo = x.LotNo,
    //                                InDate = Helper.NullDateToString2(x.InDate),
    //                                ExpDate = Helper.NullDateToString2(x.ExpDate),
    //                                PrevBinRackID = x.PrevBinRackID,
    //                                PrevBinRackCode = x.PrevBinRackCode,
    //                                PrevBinRackName = x.PrevBinRackName,
    //                                Qty = Helper.FormatThousand(x.Qty),
    //                                NewBinRackID = x.NewBinRackID,
    //                                NewBinRackCode = x.NewBinRackCode,
    //                                NewBinRackName = x.NewBinRackName,
    //                                TransactionStatus = x.TransactionStatus,
    //                                CreatedBy = x.CreatedBy,
    //                                CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
    //                                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
    //                                ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn),
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
    //    public async Task<IHttpActionResult> DatatableStock(string binRackID, string binRackAreaID, string warehouseID)
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

    //        IEnumerable<StockRM> list = Enumerable.Empty<StockRM>();
    //        IEnumerable<StockRMDTO> pagedData = Enumerable.Empty<StockRMDTO>();

    //        IQueryable<StockRM> query = null;
    //        int recordsTotal = db.StockRMs.Count();

    //        if (binRackID != null)
    //        {
    //            query = db.StockRMs.Where(s => s.BinRackID.Equals(binRackID)).AsQueryable();
    //            recordsTotal = db.StockRMs.Where(s => s.BinRackID.Equals(binRackID)).Count();
    //        }
    //        else if (binRackAreaID != null)
    //        {
    //            query = db.StockRMs.Where(s => s.BinRack.BinRackAreaID.Equals(binRackAreaID)).AsQueryable();
    //            recordsTotal = db.StockRMs.Where(s => s.BinRack.BinRackAreaID.Equals(binRackAreaID)).Count();
    //        }
    //        else if (warehouseID != null)
    //        {
    //            query = db.StockRMs.Where(s => s.BinRack.WarehouseID.Equals(warehouseID)).AsQueryable();
    //            recordsTotal = db.StockRMs.Where(s => s.BinRack.WarehouseID.Equals(warehouseID)).Count();
    //        }
    //        else
    //        {
    //            query = db.StockRMs.AsQueryable();
    //        }

    //        int recordsFiltered = 0;

    //        try
    //        {
    //            query = query
    //                    .Where(m => m.Barcode.Contains(search)
    //                    || m.RawMaterial.Code.Contains(search)
    //                    || m.RawMaterial.Name.Contains(search)
    //                    || m.LotNumber.Contains(search)
    //                    //|| m.InDate.Contains(search)
    //                    //|| m.ExpiredDate.Contains(search)
    //                    //|| m.Qty.Contains(search)
    //                    || m.BinRack.Code.Contains(search)
    //                    || m.BinRack.Name.Contains(search)
    //                    || m.BinRack.BinRackAreaCode.Contains(search)
    //                    || m.BinRack.BinRackAreaName.Contains(search)
    //                    || m.BinRack.WarehouseCode.Contains(search)
    //                    || m.BinRack.WarehouseName.Contains(search)
    //                    //|| m.ReceivedAt.Contains(search)
    //                    );

    //            Dictionary<string, Func<StockRM, object>> cols = new Dictionary<string, Func<StockRM, object>>();
    //            cols.Add("ID", x => x.ID);
    //            cols.Add("Barcode", x => x.Barcode);
    //            cols.Add("RawMaterialID", x => x.RawMaterialID);
    //            cols.Add("MaterialCode", x => x.RawMaterial.Code);
    //            cols.Add("MaterialName", x => x.RawMaterial.Name);
    //            cols.Add("LotNo", x => x.LotNumber);
    //            cols.Add("InDate", x => x.InDate);
    //            cols.Add("ExpDate", x => x.ExpiredDate);
    //            cols.Add("Qty", x => x.Quantity);
    //            cols.Add("BinRackID", x => x.BinRackID);
    //            cols.Add("BinRackCode", x => x.BinRack.Code);
    //            cols.Add("BinRackName", x => x.BinRack.Name);
    //            cols.Add("BinRackAreaID", x => x.BinRack.BinRackAreaID);
    //            cols.Add("BinRackAreaCode", x => x.BinRack.BinRackAreaCode);
    //            cols.Add("BinRackAreaName", x => x.BinRack.BinRackAreaName);
    //            cols.Add("WarehouseID", x => x.BinRack.WarehouseID);
    //            cols.Add("WarehouseCode", x => x.BinRack.WarehouseCode);
    //            cols.Add("WarehouseName", x => x.BinRack.WarehouseName);
    //            cols.Add("ReceivedAt", x => x.ReceivedAt);

    //            if (sortDirection.Equals("asc"))
    //                list = query.OrderBy(cols[sortName]);
    //            else
    //                list = query.OrderByDescending(cols[sortName]);

    //            recordsFiltered = list.Count();

    //            list = list.Skip(start).Take(length).ToList();

    //            if (list != null && list.Count() > 0)
    //            {
    //                pagedData = from x in list
    //                            select new StockRMDTO
    //                            {
    //                                ID = x.ID,
    //                                Barcode = x.Barcode,
    //                                RawMaterialID = x.RawMaterialID,
    //                                MaterialCode = x.RawMaterial.Code,
    //                                MaterialName = x.RawMaterial.Name,
    //                                Qty = Helper.FormatThousand(x.Quantity),
    //                                LotNo = x.LotNumber,
    //                                InDate = Helper.NullDateToString2(x.InDate),
    //                                ExpDate = Helper.NullDateToString2(x.ExpiredDate),
    //                                BinRackID = x.BinRackID,
    //                                BinRackCode = x.BinRack.Code,
    //                                BinRackName = x.BinRack.Name,
    //                                BinRackAreaID = x.BinRack.BinRackAreaID,
    //                                BinRackAreaCode = x.BinRack.BinRackAreaCode,
    //                                BinRackAreaName = x.BinRack.BinRackAreaName,
    //                                WarehouseID = x.BinRack.WarehouseID,
    //                                WarehouseCode = x.BinRack.WarehouseCode,
    //                                WarehouseName = x.BinRack.WarehouseName,
    //                                ReceivedAt = Helper.NullDateTimeToString(x.ReceivedAt)
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
    //    public async Task<IHttpActionResult> Create(MovementVM movementVM)
    //    {
    //        Dictionary<string, object> obj = new Dictionary<string, object>();
    //        List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

    //        string message = "";
    //        bool status = false;
    //        var re = Request;
    //        var headers = re.Headers;
    //        string movementId = null;

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
    //                BinRack prevBR = null;
    //                StockRM prevStock = null;
    //                BinRack newBR = null;

    //                if (string.IsNullOrEmpty(movementVM.PrevBinRackCode))
    //                {
    //                    ModelState.AddModelError("Movement.PrevBinRackCode", "Bin Rack Code (1) is required.");
    //                }
    //                else
    //                {
    //                    prevBR = await db.BinRacks.Where(s => s.Code.Equals(movementVM.PrevBinRackCode)).FirstOrDefaultAsync();

    //                    if (prevBR == null)
    //                    {
    //                        ModelState.AddModelError("Movement.PrevBinRackCode", "Bin Rack Code (1) is not recognized.");
    //                    }
    //                }

    //                if (string.IsNullOrEmpty(movementVM.Barcode))
    //                {
    //                    ModelState.AddModelError("Movement.Barcode", "Barcode is required.");
    //                }
    //                else
    //                {
    //                    if (prevBR != null)
    //                    {
    //                        prevStock = await db.StockRMs.Where(s => s.Barcode.Equals(movementVM.Barcode) && s.BinRackID.Equals(prevBR.ID)).FirstOrDefaultAsync();

    //                        if (prevStock == null)
    //                        {
    //                            ModelState.AddModelError("Movement.Barcode", "Barcode is not recognized.");
    //                        }
    //                        else
    //                        {
    //                            if (prevStock.Quantity < movementVM.Qty)
    //                            {
    //                                ModelState.AddModelError("Movement.Qty", "Qty can not exceed the scanned Raw Material's current quantity.");
    //                            }
    //                        }
    //                    }
    //                }

    //                if (string.IsNullOrEmpty(movementVM.NewBinRackCode))
    //                {
    //                    ModelState.AddModelError("Movement.NewBinRackCode", "Bin Rack Code (2) is required.");
    //                }
    //                else
    //                {
    //                    newBR = await db.BinRacks.Where(s => s.Code.Equals(movementVM.NewBinRackCode)).FirstOrDefaultAsync();

    //                    if (newBR == null)
    //                    {
    //                        ModelState.AddModelError("Movement.NewBinRackCode", "Bin Rack Code (2) is not recognized.");
    //                    }
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

    //                StockRM newStock = await db.StockRMs.Where(s => s.BinRackID.Equals(newBR.ID) && s.RawMaterialID.Equals(prevStock.RawMaterialID)).FirstOrDefaultAsync();

    //                prevStock.Quantity -= movementVM.Qty;

    //                if (newStock != null)
    //                {
    //                    newStock.Quantity += movementVM.Qty;
    //                }
    //                else
    //                {
    //                    newStock = new StockRM()
    //                    {
    //                        ID = Helper.CreateGuid(""),
    //                        RawMaterialID = prevStock.RawMaterialID,
    //                        Barcode = prevStock.Barcode,
    //                        LotNumber = prevStock.LotNumber,
    //                        InDate = prevStock.InDate,
    //                        ExpiredDate = prevStock.ExpiredDate,
    //                        Quantity = movementVM.Qty,
    //                        BinRackID = newBR.ID,
    //                        ReceivedAt = DateTime.Now
    //                    };

    //                    db.StockRMs.Add(newStock);
    //                }

    //                var CreatedAt = DateTime.Now;
    //                var TransactionId = Helper.CreateGuid("MV");

    //                string prefix = TransactionId.Substring(0, 2);
    //                int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
    //                int month = CreatedAt.Month;
    //                string romanMonth = Helper.ConvertMonthToRoman(month);

    //                // get last number, and do increment.
    //                string lastNumber = db.Movements.AsQueryable().OrderByDescending(x => x.Code)
    //                    .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
    //                    .AsEnumerable().Select(x => x.Code).FirstOrDefault();
    //                int currentNumber = 0;

    //                if (!string.IsNullOrEmpty(lastNumber))
    //                {
    //                    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
    //                }

    //                string runningNumber = string.Format("{0:D3}", currentNumber + 1);

    //                var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

    //                RawMaterial rm = await db.RawMaterials.Where(s => s.Code.Equals(prevStock.RawMaterialID)).FirstOrDefaultAsync(); ;

    //                Movement movement = new Movement()
    //                {
    //                    ID = TransactionId,
    //                    Code = Code,
    //                    Barcode = movementVM.Barcode,
    //                    RawMaterialID = rm.ID,
    //                    MaterialCode = rm.Code,
    //                    MaterialName = rm.Name,
    //                    LotNo = prevStock.LotNumber,
    //                    InDate = prevStock.InDate,
    //                    ExpDate = prevStock.ExpiredDate,
    //                    PrevBinRackID = prevBR.ID,
    //                    PrevBinRackCode = prevBR.Code,
    //                    PrevBinRackName = prevBR.Name,
    //                    Qty = movementVM.Qty,
    //                    NewBinRackID = newBR.ID,
    //                    NewBinRackCode = newBR.Code,
    //                    NewBinRackName = newBR.Name,
    //                    TransactionStatus = "CLOSED",
    //                    CreatedOn = CreatedAt,
    //                    CreatedBy = activeUser,
    //                };

    //                movementId = movement.ID;

    //                db.Movements.Add(movement);

    //                await db.SaveChangesAsync();
    //                status = true;
    //                message = "Movement succeeded.";
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

    //        obj.Add("id", movementId);
    //        obj.Add("status", status);
    //        obj.Add("message", message);

    //        return Ok(obj);
    //    }
    //}
}
