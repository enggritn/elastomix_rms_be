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
using ExcelDataReader;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace WMS_BE.Controllers.Api
{
    [Route("api/movement")]
    public class MovementController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [HttpGet]
        [Route("api/movement/warehouse")]
        public IHttpActionResult Warehouse()
        {
            var list = new List<SelectItemModel>();
            var warehouses = db.Warehouses.AsQueryable();

            list = warehouses
                .ToList()
                .Select(x => new SelectItemModel() { id = x.Code, text = string.Format("{0}-{1} {2}", x.Code, x.Name, x.Type) })
                .ToList();

            return Ok(list);
        }

        [HttpGet]
        [Route("api/movement/bin-area")]
        public IHttpActionResult BinArea()
        {
            var list = new List<SelectItemModel>();
            var binRackAreas = db.BinRackAreas.AsQueryable();
            string productType = "RM";
            string searchName = string.Empty;
            string excludeName = string.Empty;

            var paramType = HttpContext.Current.Request.Params.GetValues("warehouse-code");
            if (paramType != null && paramType.Length > 0 && !string.IsNullOrEmpty(paramType[0].ToString()))
            {
                productType = paramType[0].ToString();
            }

            binRackAreas = binRackAreas.Where(x => x.WarehouseCode == productType);
            list = binRackAreas
                .ToList()
                .Select(x => new SelectItemModel() { id = x.Code, text = x.Name })
                .OrderBy(x => x.text)
                .ToList();

            return Ok(list);
        }

        [HttpGet]
        [Route("api/movement/bin-rack")]
        public IHttpActionResult BinRack()
        {
            var list = new List<SelectItemModel>();
            var binRacks = db.BinRacks.AsQueryable();
            string productType = "RM";
            string searchName = string.Empty;
            string excludeName = string.Empty;

            var paramType = HttpContext.Current.Request.Params.GetValues("bin-area");
            if (paramType != null && paramType.Length > 0 && !string.IsNullOrEmpty(paramType[0].ToString()))
            {
                productType = paramType[0].ToString();
            }

            binRacks = binRacks.Where(x => x.BinRackAreaCode == productType);
            list = binRacks
                .ToList()
                .Select(x => new SelectItemModel() { id = x.Code, text = x.Name })
                .OrderBy(x => x.text)
                .ToList();

            return Ok(list);
        }

        /// <summary>
        /// Get All Product master
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/movement/products")]
        public IHttpActionResult Products()
        {
            var list = new List<SelectItemModel>();
            var products = db.vStockAlls.AsQueryable();
            string warehouseCode = string.Empty;

            var paramWarehouse = HttpContext.Current.Request.Params.GetValues("warehouse");
            if (paramWarehouse != null && paramWarehouse.Length > 0 && !string.IsNullOrEmpty(paramWarehouse[0].ToString()))
            {
                warehouseCode = paramWarehouse[0].ToString();
                products = products.Where(x => x.WarehouseCode == warehouseCode);
            }

            var paramSearch = HttpContext.Current.Request.Params.GetValues("search");
            if (paramSearch != null && paramSearch.Length > 0 && !string.IsNullOrEmpty(paramSearch[0].ToString()))
            {
                var searchName = paramSearch[0].ToString();
                products = products.Where(x => x.MaterialCode.Contains(searchName) || x.MaterialName.Contains(searchName));
            }



            list = products
                .GroupBy(x => x.MaterialCode)
                .Select(x => x.FirstOrDefault())
                .Take(5)
                .ToList()
                .Select(x => new SelectItemModel() { id = x.MaterialCode, text = string.Format("[{0}] {1}", x.MaterialCode.PadRight(15, ' '), x.MaterialName) })
                .ToList();

            return Ok(list);
        }

        /// <summary>
        /// Get All Product master
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/movement/stocks")]
        public IHttpActionResult Stocks()
        {
            var list = new List<TableStock>();
            var stocks = db.vStockAlls.AsQueryable();
            string productType = "RM";
            string searchName = string.Empty;
            string message = "";
            bool status = false;

            var paramType = HttpContext.Current.Request.Params.GetValues("type");
            if (paramType != null && paramType.Length > 0 && !string.IsNullOrEmpty(paramType[0].ToString()))
            {
                productType = paramType[0].ToString();
            }


            var paramSearch = HttpContext.Current.Request.Params.GetValues("material-code");
            if (paramSearch != null && paramSearch.Length > 0 && !string.IsNullOrEmpty(paramSearch[0].ToString()))
            {
                searchName = paramSearch[0].ToString();
                //stocks = stocks.Where(x => x.MaterialCode == searchName && x.Quantity > 0 && x.ExpiredDate >= DateTime.Now);

                //expired material can be moved
                stocks = stocks.Where(x => x.MaterialCode == searchName && x.Quantity > 0);

                if (!string.IsNullOrEmpty(searchName))
                {
                    stocks = stocks.Where(x => x.MaterialCode == searchName);
                }

                list = stocks.ToList()
                        .Select(x => new TableStock()
                        {
                            Selected = false,
                            ID = x.ID,
                            WarehouseCode = x.WarehouseCode,
                            WarehouseName = x.WarehouseName,
                            LotNumber = x.LotNumber,
                            StockCode = x.Code,
                            InDate = Helper.NullDateTimeToString(x.InDate),
                            ExpDate = Helper.NullDateTimeToString(x.ExpiredDate),
                            BinRackCode = x.BinRackCode,
                            BinRackName = x.BinRackName,
                            BinRackAreaCode = x.BinRackAreaCode,
                            BinRackAreaName = x.BinRackAreaName,
                            MaterialCode = x.MaterialCode,
                            MaterialName = x.MaterialName,
                            QtyPerBag = x.QtyPerBag,
                            Qty = x.Quantity / x.QtyPerBag,
                            QtyTransfer = 0
                        })
                    .ToList();
            }

            return Ok(list);
        }

        [HttpPost]
        [Route("api/movement/save")]
        public async Task<IHttpActionResult> Save(MovementModel model)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            StringBuilder sbError = new StringBuilder();

            try
            {
                string token = "";
                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser == null)
                {
                    throw new Exception("User token failed.");
                }

                var details = model.Details.Where(x => x.QtyTransfer > 0).ToList();
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details) 
                    {

                        if (string.IsNullOrEmpty(detail.NewArea))
                        {
                            ModelState.AddModelError("Movement.AreaList", string.Format("Warehouse must be select."));
                        }

                        if (string.IsNullOrEmpty(detail.NewBinRackCode))
                        {
                            ModelState.AddModelError("Movement.BinRackCode", string.Format("Target Bin Rack must be select."));
                        } else
                        {
                            var CekbinRack = db.BinRacks.FirstOrDefault(x => x.Code == detail.NewBinRackCode);
                            if (string.IsNullOrEmpty(CekbinRack.Name))
                            {
                                ModelState.AddModelError("Movement.BinRackCode", string.Format("New area must be select."));
                            }
                            else if (detail.PrevBinRackName == CekbinRack.Name)
                            {
                                ModelState.AddModelError("Movement.BinRackCode", string.Format("New Bin Rack can not be same with old Bin Rack."));
                            }

                            if (detail.PrevBinRackName == CekbinRack.Name)
                            {
                                ModelState.AddModelError("Movement.BinRackCode", string.Format("New Bin Rack can not be same with old Bin Rack."));
                            }
                        }
                                                                    

                        if (detail.QtyTransfer > detail.QtyAvailable)
                        {
                            ModelState.AddModelError("Movement.putawayTxt", string.Format("Bag Qty Movement. Available : {0}", detail.QtyAvailable));
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("Movement.putawayTxt", string.Format("Bag Qty can not be empty or below zero."));
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

                if (sbError.Length < 1)
                {
                    foreach (var detail in details)
                    {
                        var stockItem = db.vStockAlls.FirstOrDefault(x => x.ID == detail.ID);
                        var binRack = db.BinRacks.FirstOrDefault(x => x.Code == detail.NewBinRackCode);

                        decimal qtyPerBag = 0;

                        if (stockItem != null)
                        {
                            Movement mv = new Movement();
                            mv.ID = Helper.CreateGuid("M");

                            if (stockItem.Type == "RM")
                            {
                                var itemRM = db.StockRMs.FirstOrDefault(x => x.ID == stockItem.ID);
                                itemRM.Quantity -= (detail.QtyTransfer * itemRM.QtyPerBag);
                                qtyPerBag = itemRM.Quantity;

                                var targetRM = db.StockRMs.FirstOrDefault(x => x.MaterialCode == itemRM.MaterialCode && x.BinRackCode == detail.NewBinRackCode && x.QtyPerBag == itemRM.QtyPerBag);
                                if (targetRM != null)
                                {
                                    targetRM.Quantity += detail.QtyTransfer * targetRM.QtyPerBag;
                                }
                                else
                                {
                                    StockRM stockRM = new StockRM();
                                    stockRM.ID = Helper.CreateGuid("S");
                                    stockRM.MaterialCode = itemRM.MaterialCode;
                                    stockRM.MaterialName = itemRM.MaterialName;
                                    stockRM.Code = itemRM.Code;
                                    stockRM.LotNumber = itemRM.LotNumber;
                                    stockRM.InDate = itemRM.InDate;
                                    stockRM.ExpiredDate = itemRM.ExpiredDate;
                                    stockRM.Quantity = detail.QtyTransfer * itemRM.QtyPerBag;
                                    stockRM.QtyPerBag = itemRM.QtyPerBag;
                                    stockRM.BinRackID = binRack.ID;
                                    stockRM.BinRackCode = binRack.Code;
                                    stockRM.BinRackName = binRack.Name;
                                    stockRM.ReceivedAt = DateTime.Now;

                                    db.StockRMs.Add(stockRM);
                                }

                                mv.StockCode = itemRM.Code;
                                mv.Code = itemRM.Code;
                                mv.LotNo = itemRM.LotNumber;
                                mv.InDate = itemRM.InDate.Value;
                                mv.ExpDate = itemRM.ExpiredDate.Value;
                                mv.MaterialCode = itemRM.MaterialCode;
                                mv.MaterialName = itemRM.MaterialName;
                                mv.PrevBinRackID = itemRM.BinRackID;
                                mv.PrevBinRackCode = itemRM.BinRackCode;
                                mv.PrevBinRackName = itemRM.BinRackName;
                            }

                            if (stockItem.Type == "SFG")
                            {
                                var itemSFG = db.StockSFGs.FirstOrDefault(x => x.ID == stockItem.ID);
                                itemSFG.Quantity -= (detail.QtyTransfer * itemSFG.QtyPerBag);
                                qtyPerBag = itemSFG.QtyPerBag;

                                var targetSFG = db.StockSFGs.FirstOrDefault(x => x.MaterialCode == itemSFG.MaterialCode && x.BinRackCode == itemSFG.BinRackCode && x.QtyPerBag == itemSFG.QtyPerBag);
                                if (targetSFG != null)
                                {
                                    targetSFG.Quantity += detail.QtyTransfer * targetSFG.QtyPerBag;
                                }
                                else
                                {
                                    StockSFG stockSFG = new StockSFG();
                                    stockSFG.ID = Helper.CreateGuid("S");
                                    stockSFG.MaterialCode = itemSFG.MaterialCode;
                                    stockSFG.MaterialName = itemSFG.MaterialName;
                                    stockSFG.Code = itemSFG.Code;
                                    stockSFG.LotNumber = itemSFG.LotNumber;
                                    stockSFG.InDate = itemSFG.InDate;
                                    stockSFG.ExpiredDate = itemSFG.ExpiredDate;
                                    stockSFG.Quantity = detail.QtyTransfer * itemSFG.QtyPerBag;
                                    stockSFG.QtyPerBag = itemSFG.QtyPerBag;
                                    stockSFG.BinRackID = binRack.ID;
                                    stockSFG.BinRackCode = binRack.Code;
                                    stockSFG.BinRackName = binRack.Name;
                                    stockSFG.ReceivedAt = DateTime.Now;

                                    db.StockSFGs.Add(stockSFG);
                                }

                                mv.StockCode = itemSFG.Code;
                                mv.Code = itemSFG.Code;
                                mv.LotNo = itemSFG.LotNumber;
                                mv.InDate = itemSFG.InDate.Value;
                                mv.ExpDate = itemSFG.ExpiredDate.Value;
                                mv.MaterialCode = itemSFG.MaterialCode;
                                mv.MaterialName = itemSFG.MaterialName;
                                mv.PrevBinRackID = itemSFG.BinRackID;
                                mv.PrevBinRackCode = itemSFG.BinRackCode;
                                mv.PrevBinRackName = itemSFG.BinRackName;
                            }

                            mv.Qty = detail.QtyTransfer;
                            mv.QtyPerBag = detail.QtyPerBag;
                            mv.NewBinRackID = binRack.ID;
                            mv.NewBinRackCode = binRack.Code;
                            mv.NewBinRackName = binRack.Name;
                            mv.TransactionStatus = string.Empty;
                            mv.CreatedBy = activeUser;
                            mv.CreatedOn = DateTime.Now;
                            mv.ModifiedBy = activeUser;
                            db.Movements.Add(mv);
                        }

                    }

                    db.SaveChanges();

                    status = true;
                    message = "Putaway succeeded.";

                    //response.Status = true;
                    //response.SetSuccess("Material code {0} success to move", model.MaterialCode);
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                    //response.SetError(sbError.ToString());
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
                //response.SetError(ex.Message);
            }
            //return Ok(response);

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }


        /// <summary>
        /// List PR Box Approval
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/movement/data-table")]
        public IHttpActionResult GetData()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];


            Dictionary<string, Func<Movement, object>> cols = new Dictionary<string, Func<Movement, object>>();
            cols.Add("ID", x => x.ID);
            cols.Add("Code", x => x.Code);
            cols.Add("MaterialCode", x => x.MaterialCode);
            cols.Add("MaterialName", x => x.MaterialName);
            cols.Add("InDate", x => x.InDate);
            cols.Add("CreatedOn", x => x.CreatedOn);
            cols.Add("CreatedBy", x => x.CreatedBy);
            cols.Add("LotNo", x => x.LotNo);
            cols.Add("PrevBinRackCode", x => x.PrevBinRackCode);
            cols.Add("PrevBinRackName", x => x.PrevBinRackName);
            cols.Add("Qty", x => x.Qty);
            cols.Add("QtyPerBag", x => x.QtyPerBag);
            cols.Add("NewBinRackID", x => x.NewBinRackID);
            cols.Add("NewBinRackCode", x => x.NewBinRackCode);
            cols.Add("NewBinRackName", x => x.NewBinRackName);

            IQueryable<Movement> movements = db.Movements.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                movements = movements
                    .Where(m => m.MaterialCode.Contains(search) || m.MaterialName.Contains(search) || m.NewBinRackCode.Contains(search) || m.NewBinRackName.Contains(search) || m.PrevBinRackCode.Contains(search) || m.PrevBinRackName.Contains(search));
            }


            int recordsTotal = movements.Count();
            int recordsFiltered = 0;

            if (sortDirection.Equals("asc"))
                movements = movements.OrderBy(cols[sortName]).AsQueryable();
            else
                movements = movements.OrderByDescending(cols[sortName]).AsQueryable();

            recordsFiltered = movements.Count();

            List<MovementVM> list = new List<MovementVM>();
            var movementsList = movements.Skip(start).Take(length).ToList();
            if (movementsList != null && movementsList.Count > 0)
            {
                foreach (var item in movementsList)
                {
                    var itemList = new MovementVM()
                    {
                        ID = item.ID,
                        Code = item.Code,
                        LotNo = item.LotNo,
                        InDate = Helper.NullDateToString(item.InDate),
                        ExpDate = Helper.NullDateToString(item.ExpDate),
                        MaterialCode = item.MaterialCode,
                        MaterialName = item.MaterialName,
                        PrevBinRackID = item.PrevBinRackID,
                        PrevBinRackCode = item.PrevBinRackCode,
                        PrevBinRackName = item.PrevBinRackName,
                        Qty = item.Qty.HasValue ? item.Qty.Value : 0,
                        QtyPerBag = item.QtyPerBag.HasValue ? item.QtyPerBag.Value : 0,
                        NewBinRackID = item.ID,
                        NewBinRackCode = item.NewBinRackCode,
                        NewBinRackName = item.NewBinRackName,
                        CreatedBy = item.CreatedBy,
                        CreatedOn = item.CreatedOn
                    };

                    var binPrev = db.BinRacks.FirstOrDefault(x => x.ID == item.PrevBinRackID);
                    itemList.PrevArea = binPrev.BinRackAreaName;

                    var binNew = db.BinRacks.FirstOrDefault(x => x.ID == item.NewBinRackID);
                    itemList.NewArea = binNew.BinRackAreaName;

                    list.Add(itemList);
                }
            }

            //var list = movements.Skip(start).Take(length).ToList()
            //    .Select(x => new MovementVM() { 
            //    ID = x.ID,
            //    Code = x.Code,
            //    LotNo = x.LotNo,
            //    InDate = x.InDate.ToString(),
            //    ExpDate = x.ExpDate.ToString(),
            //    MaterialCode = x.MaterialCode,
            //    MaterialName = x.MaterialName,
            //    PrevBinRackID = x.PrevBinRackID,
            //    PrevBinRackCode = x.PrevBinRackCode,
            //    PrevBinRackName = x.PrevBinRackName,
            //    Qty = x.Qty.HasValue ? x.Qty.Value : 0,
            //    QtyPerBag = x.QtyPerBag.HasValue ? x.QtyPerBag.Value : 0,
            //    NewBinRackID = x.ID,
            //    NewBinRackCode = x.NewBinRackCode,
            //    NewBinRackName = x.NewBinRackName,
            //    CreatedBy = x.CreatedBy,
            //    CreatedOn = x.CreatedOn

            //    }).ToList();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        [Route("api/movement/items")]
        public async Task<IHttpActionResult> ItemTransfers(SearchItemModel model)
        {
            IQueryable<ItemLookupModel> list = null;

            if (model.Source == "SFG")
            {
                var transferSourceSFGs = db.vTransferSourceSFGs.AsQueryable();
                list = transferSourceSFGs.Select(x => new ItemLookupModel()
                {
                    ID = x.ID,
                    DataSource = x.DataSource,
                    Barcode = x.Barcode,
                    MaterialName = x.MaterialName,
                    MaterialCode = x.MaterialCode,
                    LotNumber = x.LotNumber,
                    BinRackAreaID = x.BinRackAreaID,
                    BinRackArea = x.BinRackArea,
                    BinRackID = x.BinRackID,
                    BinRack = x.BinRack,
                    Quantity = x.Quantity,
                    IsExclude = false
                });
            }
            else
            {
                var transferSourceRMs = db.vTransferSourceRMs.AsQueryable();
                list = transferSourceRMs.Select(x => new ItemLookupModel()
                {
                    ID = x.ID,
                    DataSource = x.DataSource,
                    Barcode = x.Barcode,
                    MaterialName = x.MaterialName,
                    MaterialCode = x.MaterialCode,
                    LotNumber = x.LotNumber,
                    BinRackAreaID = x.BinRackAreaID,
                    BinRackArea = x.BinRackArea,
                    BinRackID = x.BinRackID,
                    BinRack = x.BinRack,
                    Quantity = x.Quantity,
                    IsExclude = false
                });
            }


            if (!string.IsNullOrEmpty(model.BinRackAreaID))
            {
                list = list.Where(x => x.BinRackAreaID == model.BinRackAreaID);
            }

            if (!string.IsNullOrEmpty(model.BinRackID))
            {
                list = list.Where(x => x.BinRackID == model.BinRackID);
            }

            if (!string.IsNullOrEmpty(model.Name))
            {
                list = list.Where(x => x.MaterialName.Contains(model.Name));
            }

            if (model.ExcludeItems != null && model.ExcludeItems.Count > 0)
            {
                foreach (var itemID in model.ExcludeItems)
                {
                    await list.ForEachAsync(x =>
                    {
                        if (x.ID == itemID)
                        {
                            x.IsExclude = true;
                        }
                    });
                }
            }



            return Ok(list.ToList());
        }

        [HttpPost]
        [Route("api/movement/items-target")]
        public async Task<IHttpActionResult> ItemTarget(SearchItemModel model)
        {
            IQueryable<ItemLookupModel> list = null;
            var _vProductMasters = db.vProductMasters.AsQueryable();

            if (model.Source == "SFG")
            {

                list = _vProductMasters.Where(x => x.ProdType == "SFG").Select(x => new ItemLookupModel()
                {
                    DataSource = "SFG",
                    MaterialName = x.MaterialName,
                    MaterialCode = x.MaterialCode,
                    Quantity = x.QtyPerBag,
                    IsExclude = false
                });
            }
            else
            {
                list = _vProductMasters.Where(x => x.ProdType == "RM").Select(x => new ItemLookupModel()
                {
                    DataSource = "SFG",
                    MaterialName = x.MaterialName,
                    MaterialCode = x.MaterialCode,
                    Quantity = x.QtyPerBag,
                    IsExclude = false
                });
            }


            if (!string.IsNullOrEmpty(model.Name))
            {
                list = list.Where(x => x.MaterialCode.Contains(model.Name) || x.MaterialName.Contains(model.Name));
            }

            list = list.Take(100);
            if (model.ExcludeItems != null && model.ExcludeItems.Count > 0)
            {
                foreach (var itemID in model.ExcludeItems)
                {
                    await list.ForEachAsync(x =>
                    {
                        if (x.ID == itemID)
                        {
                            x.IsExclude = true;
                        }
                    });
                }
            }



            return Ok(list.ToList());
        }

        [HttpPost]
        [Route("api/movement/data-stock")]
        public IHttpActionResult GetDataStock()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            #region set search filter
            HttpRequest request = HttpContext.Current.Request;
            string inDate = request["inDate"].ToString();
            string expDate = request["expDate"].ToString();
            string lotNo = request["lotNo"].ToString();
            string materialName = request["materialName"].ToString();
            string materialCode = request["materialCode"].ToString();
            DateTime filterInDate = new DateTime();
            DateTime filterExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

           
            #endregion

            Dictionary<string, Func<vStockAll, object>> cols = new Dictionary<string, Func<vStockAll, object>>();
            cols.Add("ID", x => x.ID);
            cols.Add("Code", x => x.Code);
            cols.Add("LotNumber", x => x.LotNumber);
            cols.Add("InDate", x => x.InDate);
            cols.Add("ExpiredDate", x => x.ExpiredDate);
            cols.Add("MaterialCode", x => x.MaterialCode);
            cols.Add("MaterialName", x => x.MaterialName);
            cols.Add("Type", x => x.Type);
            cols.Add("Quantity", x => x.Quantity);
            cols.Add("QtyPerBag", x => x.QtyPerBag);
            cols.Add("BagQty", x => x.BagQty);
            cols.Add("BinRackAreaName", x => x.BinRackAreaName);
            cols.Add("BinRackName", x => x.BinRackName);
            cols.Add("BinRackAreaType", x => x.BinRackAreaType);
            cols.Add("WarehouseName", x => x.WarehouseName);
            cols.Add("ReceivedAt", x => x.ReceivedAt);
                       
            IQueryable<vStockAll> vStocks = db.vStockAlls.Where(x => x.Quantity > 0).AsQueryable();
            if (!string.IsNullOrEmpty(materialCode))
            {
                vStocks = db.vStockAlls.Where(x => x.MaterialCode.Contains(materialCode) && x.Quantity > 0);
            }
            if (!string.IsNullOrEmpty(materialName))
            {
                vStocks = db.vStockAlls.Where(x => x.MaterialName.Contains(materialName) && x.Quantity > 0);
            }
            if (!string.IsNullOrEmpty(lotNo))
            {
                vStocks = db.vStockAlls.Where(x => x.LotNumber.Contains(lotNo) && x.Quantity > 0);
            }
            if (DateTime.TryParse(inDate, out temp))
            {
                filterInDate = Convert.ToDateTime(inDate);
                vStocks = db.vStockAlls.Where(x => DbFunctions.TruncateTime(x.InDate) == DbFunctions.TruncateTime(filterInDate) && x.Quantity > 0);
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                filterExpDate = Convert.ToDateTime(expDate);
                vStocks = db.vStockAlls.Where(x => DbFunctions.TruncateTime(x.ExpiredDate) == DbFunctions.TruncateTime(filterExpDate) && x.Quantity > 0);
            }

            string[] warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();

            vStocks = vStocks.Where(m => warehouseCodes.Contains(m.WarehouseCode));

            if (!string.IsNullOrEmpty(search))
            {
                vStocks = vStocks
                    .Where(m => m.MaterialCode.Contains(search) || m.MaterialName.Contains(search) || m.BinRackAreaName.Contains(search));
            }

            


            int recordsTotal = vStocks.Count();
            int recordsFiltered = 0;

            if (sortDirection.Equals("asc"))
                vStocks = vStocks.OrderBy(cols[sortName]).AsQueryable();
            else
                vStocks = vStocks.OrderByDescending(cols[sortName]).AsQueryable();

            recordsFiltered = vStocks.Count();

            List<StockDTO> list = new List<StockDTO>();
            var vStocksList = vStocks.Skip(start).Take(length).ToList();
            if (vStocksList != null && vStocksList.Count > 0)
            {
                foreach (var item in vStocksList)
                {
                    var itemList = new StockDTO()
                    {
                        ID = item.ID,
                        Code = item.Code,
                        LotNumber = item.LotNumber,
                        InDate = Helper.NullDateToString(item.InDate),
                        ExpiredDate = Helper.NullDateToString(item.ExpiredDate),
                        MaterialCode = item.MaterialCode,
                        MaterialName = item.MaterialName,
                        Type = item.Type,
                        Quantity = item.Quantity,
                        QtyPerBag = item.QtyPerBag,
                        BagQty = item.BagQty.HasValue ? item.BagQty.Value : 0,
                        BinRackAreaName = item.BinRackAreaName,
                        BinRackName = item.BinRackName,
                        BinRackAreaType = item.BinRackAreaType,
                        WarehouseName = item.WarehouseName,
                        ReceivedAt = Helper.NullDateToString(item.ReceivedAt),
                        WarehouseCode = item.WarehouseCode,
                        IsExpired = Convert.ToBoolean(item.IsExpired)
                    };

                    list.Add(itemList);
                }
            }

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


    }
}
