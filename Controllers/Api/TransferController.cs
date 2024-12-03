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
    public class SelectItemModel
    {
        public bool Selected { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }

    [Route("api/transfer")]
    public class TransferController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        /// <summary>
        /// Get All Product master
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/transfer/products")]
        public IHttpActionResult Products()
        {
            var list = new List<SelectItemModel>();
            var products = db.vProductMasters.AsQueryable();
            string productType = "RM";
            string searchName = string.Empty;
            string excludeName = string.Empty;

            var paramType = HttpContext.Current.Request.Params.GetValues("type");
            if (paramType != null && paramType.Length > 0 && !string.IsNullOrEmpty(paramType[0].ToString()))
            {
                productType = paramType[0].ToString();
            }

            var paramSearch = HttpContext.Current.Request.Params.GetValues("search");
            if (paramSearch != null && paramSearch.Length > 0 && !string.IsNullOrEmpty(paramSearch[0].ToString()))
            {
                searchName = paramSearch[0].ToString();
            }

            var paramExSearch = HttpContext.Current.Request.Params.GetValues("exclude");
            if (paramExSearch != null && paramExSearch.Length > 0 && !string.IsNullOrEmpty(paramExSearch[0].ToString()))
            {
                excludeName = paramExSearch[0].ToString();
            }

            products = products.Where(x => x.ProdType == productType);
            if (!string.IsNullOrEmpty(searchName))
            {
                products = products.Where(x => x.MaterialCode.Contains(searchName) || x.MaterialName.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(excludeName))
            {
                products = products.Where(x => x.MaterialCode != excludeName);
            }

            list = products.Take(5)
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
        [Route("api/transfer/stocks")]
        public IHttpActionResult Stocks()
        {
            var list = new List<TableStock>();
            var stocks = db.vStockAlls.AsQueryable();
            string productType = "RM";
            string searchName = string.Empty;

            var paramType = HttpContext.Current.Request.Params.GetValues("type");
            if (paramType != null && paramType.Length > 0 && !string.IsNullOrEmpty(paramType[0].ToString()))
            {
                productType = paramType[0].ToString();
            }

            var paramSearch = HttpContext.Current.Request.Params.GetValues("material-code");
            if (paramSearch != null && paramSearch.Length > 0 && !string.IsNullOrEmpty(paramSearch[0].ToString()))
            {
                searchName = paramSearch[0].ToString();
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
                            StockCode = x.Code,
                            LotNumber = x.LotNumber,
                            WarehouseName = x.WarehouseName,
                            BinRackCode = x.BinRackCode,
                            BinRackName = x.BinRackName,
                            BinRackAreaCode = x.BinRackAreaCode,
                            BinRackAreaName = x.BinRackAreaName,
                            QtyPerBag = x.QtyPerBag,
                            Qty = x.Quantity,
                            QtyPerBagStr = Helper.FormatThousand(x.QtyPerBag),
                            QtyStr = Helper.FormatThousand(x.Quantity),
                            QtyTransfer = 0
                        })
                    .ToList();
            }

            return Ok(list);
        }

        /// <summary>
        /// Get All Product master
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/transfer/stocks-fifo")]
        public IHttpActionResult StocksFIFO()
        {
            Guid transferID = Guid.Empty;
            IQueryable<vStockAll> query = db.vStockAlls.AsQueryable();
            List<FifoStockDTO> data = new List<FifoStockDTO>();
            var transferId = HttpContext.Current.Request.Params.GetValues("transfer-id");
            if (transferId != null && transferId.Length > 0 && !string.IsNullOrEmpty(transferId[0].ToString()))
            {
                transferID = Guid.Parse(transferId[0].ToString());
            }

            data = GetFIFOData(transferID);

            return Ok(data);
        }
        public List<FifoStockDTO> GetFIFOData(Guid ID)
        {
            decimal QtyStock = 0;
            decimal QtyRemain = 0;

            IQueryable<vStockAll> query = db.vStockAlls.AsQueryable();

            List<FifoStockDTO> data = new List<FifoStockDTO>();
            string message = "";

            var transform = db.Transfers.FirstOrDefault(x => x.ID == ID);
            QtyRemain = transform.QtyTransfer;
            if (transform != null && transform.TransferDetails != null && transform.TransferDetails.Count() > 0)
            {
                QtyRemain -= (transform.TransferDetails.Sum(x => x.QtyTransfer) + transform.TransferDetails.Sum(x => x.QtyTransferRetail));
            }

            query = query.Where(x => x.Type == transform.ProductType && x.MaterialCode.Equals(transform.ProductID) && x.Quantity > 0 && !x.OnInspect);
            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();

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
                    foreach (vStockAll stock in list)
                    {
                        if (QtyStock < QtyRemain)
                        {
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
                                BagQty = Helper.FormatThousand(stock.BagQty),
                                QtyPerBag = Helper.FormatThousand(stock.QtyPerBag),
                                TotalQty = Helper.FormatThousand(stock.Quantity),
                                Quantity = stock.Quantity,
                                IsExpired = DateTime.Now.Date >= stock.ExpiredDate.Value.Date
                            };

                            data.Add(dat);
                            QtyStock += stock.Quantity;
                        }
                    }
                }

                message = "Fetch data succeeded.";
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

            return data;
        }

        [HttpGet]
        [Route("api/transfer/transform")]
        public IHttpActionResult Transform(Guid ID)
        {
            var transform = db.Transfers.FirstOrDefault(x => x.ID == ID);
            var materialSource = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == transform.ProductID);
            var materialTarget = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == transform.ProductIDTarget);
            var model = new TransferModel()
            {
                ID = transform.ID,
                TransferNo = transform.TransferNo,
                ItemSourceMaterialCode = string.Format("{0} {1}", transform.ProductID, materialSource != null ? " - " + materialSource.MaterialName : string.Empty),
                ItemSourceType = transform.ProductType,
                
                ItemTargetType = transform.ProductTypeTarget,
                ItemTargetMaterialCode = string.Format("{0} {1}", transform.ProductIDTarget, materialTarget != null ? " - " + materialTarget.MaterialName : string.Empty),
                TotalTransfer = transform.QtyTransfer,
                TotalTransferOutstanding = transform.QtyTransfer,
                CreatedBy = transform.CreatedBy,
                CreatedOn = transform.CreatedOn,
                CreatedOnStr = Helper.NullDateTimeToString(transform.CreatedOn)
            };

            model.Details = new List<TransferDetailModel>();
            if(transform.TransferDetails != null && transform.TransferDetails.Count() > 0)
            {
                var dateNow = DateTime.Now.Date;

                foreach (var transfer in transform.TransferDetails)
                {
                    var stockSource = db.vStockAlls.FirstOrDefault(x => x.ID == transfer.StockIDSource);
                    var transformSource = new TransformSource()
                    {
                        CreatedBy = string.Empty,
                        CreatedOn = string.Empty,
                        Barcode = stockSource.Code,
                        LotNo = stockSource.LotNumber,
                        BinRackCode = stockSource.BinRackCode,
                        BinRackName = stockSource.BinRackName,
                        BinRackAreaCode = stockSource.BinRackAreaCode,
                        BinRackAreaName = stockSource.BinRackAreaName,
                        WarehouseCode = stockSource.WarehouseCode,
                        WarehouseName = stockSource.WarehouseName,
                        MaterialCode = stockSource.MaterialCode,
                        MaterialName = stockSource.MaterialName,
                        InDate = Helper.NullDateToString(stockSource.InDate),
                        ExpDate = Helper.NullDateToString(stockSource.ExpiredDate),
                        BagQty = Helper.FormatThousand(stockSource.BagQty),
                        QtyPerBag = Helper.FormatThousand(stockSource.QtyPerBag),
                        TotalQty = Helper.FormatThousand(stockSource.BagQty * stockSource.QtyPerBag),
                        IsExpired = DateTime.Now.Date >= stockSource.ExpiredDate.Value.Date,
                        QtyBefore = Helper.FormatThousand(transfer.QtyTransfer),
                        QtyToBe = Helper.FormatThousand(transfer.Qty),
                    };
                    model.TransformSources.Add(transformSource);

                    if (!string.IsNullOrEmpty(transfer.StockIDSourceRetail))
                    {
                        var stockSourceRetail = db.vStockAlls.FirstOrDefault(x => x.ID == transfer.StockIDSourceRetail);
                        var transformSourceRetail = new TransformSource()
                        {
                            CreatedBy = string.Empty,
                            CreatedOn = string.Empty,
                            Barcode = stockSourceRetail.Code,
                            LotNo = stockSourceRetail.LotNumber,
                            BinRackCode = stockSourceRetail.BinRackCode,
                            BinRackName = stockSourceRetail.BinRackName,
                            BinRackAreaCode = stockSourceRetail.BinRackAreaCode,
                            BinRackAreaName = stockSourceRetail.BinRackAreaName,
                            WarehouseCode = stockSourceRetail.WarehouseCode,
                            WarehouseName = stockSourceRetail.WarehouseName,
                            MaterialCode = stockSourceRetail.MaterialCode,
                            MaterialName = stockSourceRetail.MaterialName,
                            InDate = Helper.NullDateToString(stockSourceRetail.InDate),
                            ExpDate = Helper.NullDateToString(stockSourceRetail.ExpiredDate),
                            BagQty = Helper.FormatThousand(stockSourceRetail.BagQty),
                            QtyPerBag = Helper.FormatThousand(stockSourceRetail.QtyPerBag),
                            TotalQty = Helper.FormatThousand(transfer.QtyRetail),
                            IsExpired = DateTime.Now.Date >= stockSourceRetail.ExpiredDate.Value.Date,
                            QtyBefore = Helper.FormatThousand(transfer.Qty),
                            QtyToBe = Helper.FormatThousand(transfer.QtyRetail),
                        };
                        model.TransformSources.Add(transformSourceRetail);
                    }


                    var stockTarget = db.vStockAlls.FirstOrDefault(x => x.ID == transfer.StockIDTarget);
                    var transformTarget = new TransformSource()
                    {
                        CreatedBy = string.Empty,
                        CreatedOn = string.Empty,
                        Barcode = stockTarget.Code,
                        LotNo = stockTarget.LotNumber,
                        BinRackCode = stockTarget.BinRackCode,
                        BinRackName = stockTarget.BinRackName,
                        BinRackAreaCode = stockTarget.BinRackAreaCode,
                        BinRackAreaName = stockTarget.BinRackAreaName,
                        WarehouseCode = stockTarget.WarehouseCode,
                        WarehouseName = stockTarget.WarehouseName,
                        MaterialCode = stockTarget.MaterialCode,
                        MaterialName = stockTarget.MaterialName,
                        InDate = Helper.NullDateToString(stockTarget.InDate),
                        ExpDate = Helper.NullDateToString(stockTarget.ExpiredDate),
                        BagQty = Helper.FormatThousand(stockTarget.BagQty),
                        QtyPerBag = Helper.FormatThousand(stockTarget.QtyPerBag),
                        TotalQty = Helper.FormatThousand(stockTarget.BagQty * stockTarget.QtyPerBag),
                        IsExpired = DateTime.Now.Date >= stockTarget.ExpiredDate.Value.Date,
                        QtyBefore = Helper.FormatThousand(transfer.Qty),
                        QtyToBe = Helper.FormatThousand(transfer.QtyTransfer),
                    };

                    model.TransformTarget.Add(transformTarget);

                    if (!string.IsNullOrEmpty(transfer.StockIDTargetRetail))
                    {
                        var stockTargetRetail = db.vStockAlls.FirstOrDefault(x => x.ID == transfer.StockIDTargetRetail);
                        var transformSourceRetail = new TransformSource()
                        {
                            CreatedBy = string.Empty,
                            CreatedOn = string.Empty,
                            Barcode = stockTargetRetail.Code,
                            LotNo = stockTargetRetail.LotNumber,
                            BinRackCode = stockTargetRetail.BinRackCode,
                            BinRackName = stockTargetRetail.BinRackName,
                            BinRackAreaCode = stockTargetRetail.BinRackAreaCode,
                            BinRackAreaName = stockTargetRetail.BinRackAreaName,
                            WarehouseCode = stockTargetRetail.WarehouseCode,
                            WarehouseName = stockTargetRetail.WarehouseName,
                            MaterialCode = stockTargetRetail.MaterialCode,
                            MaterialName = stockTargetRetail.MaterialName,
                            InDate = Helper.NullDateToString(stockTargetRetail.InDate),
                            ExpDate = Helper.NullDateToString(stockTargetRetail.ExpiredDate),
                            BagQty = Helper.FormatThousand(stockTargetRetail.BagQty),
                            QtyPerBag = Helper.FormatThousand(stockTargetRetail.QtyPerBag),
                            TotalQty = Helper.FormatThousand(transfer.QtyTransferRetail),
                            IsExpired = DateTime.Now.Date >= stockTargetRetail.ExpiredDate.Value.Date,
                            QtyBefore = Helper.FormatThousand(transfer.Qty),
                            QtyToBe = Helper.FormatThousand(transfer.QtyTransferRetail),
                        };
                        model.TransformTarget.Add(transformSourceRetail);
                    }

                }

                model.TotalTransferOutstanding -= transform.TransferDetails.Sum(x => x.QtyTransfer) + transform.TransferDetails.Sum(x => x.QtyTransferRetail);
            }
            
            

            return Ok(model);
        }

        [HttpPost]
        [Route("api/transfer/save")]
        public async Task<IHttpActionResult> Save(TransferModel model)
        {
            ResponseModel response = new ResponseModel();
            HttpRequest request = HttpContext.Current.Request;
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
                    sbError.AppendLine("User token failed");
                }

                if (string.IsNullOrEmpty(model.ItemSourceMaterialCode))
                {
                    sbError.AppendLine("Item Source must be select");
                }

                if (string.IsNullOrEmpty(model.ItemTargetMaterialCode))
                {
                    sbError.AppendLine("Item Target must be select");
                }

                var stocks = model.Stocks.Where(x => x.Qty > 0).ToList();
                if (!(stocks != null && stocks.Count > 0))
                {
                    sbError.AppendLine("Item Source must have stock");
                }

                if (!(model.TotalTransfer > 0))
                {
                    sbError.AppendLine("Qty Transfer must have");
                } else
                {
                    if(model.TotalTransfer > stocks.Sum(x => x.Qty))
                    {
                        sbError.AppendLine("Qty Transfer cannot over from Qty Stock");
                    }
                }


                if (sbError.Length < 1)
                {
                    vProductMaster itemSource = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == model.ItemSourceMaterialCode);
                    var stocksTotal = db.vStockAlls.Where(x => x.MaterialCode == model.ItemSourceMaterialCode);
                    var transfer = new Transfer();
                    transfer.ID = Guid.NewGuid();
                    transfer.TransferNo = "TRF/" + DateTime.Now.Year.ToString() + "/" + DateTime.Now.Month.ToString("00") + "/" + Guid.NewGuid().ToString().Split('-')[0].Substring(1, 5).ToUpper();
                    transfer.ProductID = model.ItemSourceMaterialCode;
                    transfer.ProductType = model.ItemSourceType;
                    transfer.ProductIDTarget = model.ItemTargetMaterialCode;
                    transfer.ProductTypeTarget = model.ItemTargetType;
                    transfer.QtyTotal = stocksTotal.Sum(x => x.Quantity);
                    transfer.QtyTransfer = model.TotalTransfer;
                    transfer.CreatedOn = DateTime.Now;
                    transfer.CreatedBy = activeUser;


                    transfer.TransferDetails = new List<TransferDetail>();

                    transfer.QtyTotal = 0;
                    db.Transfers.Add(transfer);
                    db.SaveChanges();
                    response.SetSuccess("Material code {0} to {1} ready to transform", model.ItemSourceMaterialCode, model.ItemTargetMaterialCode);
                    response.ResponseObject = transfer.ID;
                } else
                {
                    response.SetError(sbError.ToString());
                }

            } catch(Exception ex) {
                response.SetError(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost]
        [Route("api/transfer/save-detail")]
        public async Task<IHttpActionResult> SaveDetail(TransferDetailModel model)
        {
            ResponseModel response = new ResponseModel();
            HttpRequest request = HttpContext.Current.Request;
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
                    sbError.AppendLine("User token failed");
                }

                var transform = new Transfer();
                decimal QtyOutstanding = 0;

                if (model.TransferID == Guid.Empty)
                {
                    sbError.AppendLine("Transform ID cannot be null");
                } else
                {
                    transform = db.Transfers.FirstOrDefault(x => x.ID == model.TransferID);
                    if(transform != null)
                    {
                        QtyOutstanding = transform.QtyTransfer;
                        if(transform.TransferDetails != null && transform.TransferDetails.Count() > 0)
                        {
                            QtyOutstanding -= (transform.TransferDetails.Sum(x => x.QtyTransfer) + transform.TransferDetails.Sum(x => x.QtyTransferRetail));
                        }


                        var dataFIFO = GetFIFOData(transform.ID);
                        if (dataFIFO != null && dataFIFO.Count > 0)
                        {
                            var dataFIFOFirst = dataFIFO.FirstOrDefault();
                            var dataFIFOFirstID = dataFIFO.FirstOrDefault().ID;
                            var stockSource = db.vStockAlls.FirstOrDefault(x => x.ID == dataFIFOFirstID);

                            if (model.StockIDSource != stockSource.ID)
                            {
                                sbError.AppendLine(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & Qty/Bag : {1}", stockSource.LotNumber, dataFIFOFirst.TotalQty));
                            } else
                            {
                                if (model.QtyTransfer > stockSource.Quantity)
                                {
                                    sbError.AppendLine(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & Qty/Bag : {1}", stockSource.LotNumber, dataFIFOFirst.TotalQty));
                                }
                                //else
                                //{
                                //    if (DateTime.Now.Date >= stockSource.ExpiredDate.Date)
                                //    {
                                //        sbError.AppendLine(string.Format("FIFO Restriction, must execute QC Inspection for material with following detail = LotNo : {0} & Qty/Bag : {1}", stockSource.LotNumber, Helper.FormatThousand(stockSource.Quantity)));
                                //    }
                                //}
                            }                         
                        } else
                        {
                            sbError.AppendLine("Stock not found");
                        }

                    } else
                    {
                        sbError.AppendLine("Transform not found");
                    }
                }

                if (string.IsNullOrEmpty(model.ProductIDTarget))
                {
                    sbError.AppendLine("Item Target must be select");
                }

                if (string.IsNullOrEmpty(model.ProductTypeTarget))
                {
                    sbError.AppendLine("Item Target Type must be select");
                }

                if (model.QtyTransfer > 0)
                {
                    if(model.QtyTransfer > QtyOutstanding)
                    {
                        sbError.AppendLine("Qty cannot over from outstanding");
                    }
                }
                else
                {
                    sbError.AppendLine("Item Source must have stock");
                }


                if (sbError.Length < 1)
                {
                    string StockIDSourceRetail = string.Empty;
                    string StockIDTargetRetail = string.Empty;
                    decimal QtyStockMain = 0;
                    decimal QtyRetail = 0;

                    decimal QtyTransfer = 0;
                    decimal QtyTransferRetail = 0;

                    vStockAll stockParam = new vStockAll();
                    var itemStockTarget = db.vStockAlls.FirstOrDefault(x => x.ID == model.StockIDSource);
                    if (itemStockTarget != null)
                    {
                        stockParam.BinRackCode = itemStockTarget.BinRackCode;
                        stockParam.LotNumber = itemStockTarget.LotNumber;
                        stockParam.ExpiredDate = itemStockTarget.ExpiredDate;
                        stockParam.QtyPerBag = itemStockTarget.QtyPerBag;
                        stockParam.InDate = itemStockTarget.InDate;

                        string ItemSourceType = itemStockTarget.Type;
                        if (ItemSourceType == "RM")
                        {
                            var itemStock = db.StockRMs.FirstOrDefault(x => x.ID == itemStockTarget.ID);
                            if(itemStock != null)
                            {
                                //itemStock.Quantity -= stock.QtyTransfer;
                                var QtyPerBag = itemStock.QtyPerBag;
                                var QuantityNew = itemStock.Quantity - model.QtyTransfer;
                                if (QuantityNew > itemStock.QtyPerBag)
                                {
                                    var QtyRemainder = QuantityNew % itemStock.QtyPerBag;
                                    if (QtyRemainder > 0)
                                    {
                                        var stockRM = new StockRM();
                                        stockRM.ID = Helper.CreateGuid("S");
                                        stockRM.MaterialCode = itemStock.MaterialCode;
                                        stockRM.MaterialName = itemStock.MaterialName;
                                        stockRM.Code = string.Format("{0}{1}{2}{3}{4}", itemStock.MaterialCode, Helper.FormatThousand(QtyRemainder), itemStock.LotNumber, itemStock.InDate.Value.ToString("yyyyMMdd").Substring(1), itemStock.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                        stockRM.LotNumber = itemStock.LotNumber;
                                        stockRM.InDate = itemStock.InDate;
                                        stockRM.ExpiredDate = itemStock.ExpiredDate;
                                        stockRM.Quantity = QtyRemainder;
                                        stockRM.QtyPerBag = QtyRemainder;
                                        stockRM.BinRackID = itemStock.BinRackID;
                                        stockRM.BinRackCode = itemStock.BinRackCode;
                                        stockRM.BinRackName = itemStock.BinRackName;
                                        stockRM.ReceivedAt = DateTime.Now;
                                        
                                        db.StockRMs.Add(stockRM);

                                        StockIDSourceRetail = stockRM.ID;
                                        QtyRetail = QtyRemainder;
                                    }

                                    itemStock.Quantity = QuantityNew - QtyRemainder;
                                }
                                else
                                {
                                    itemStock.Code = string.Format("{0}{1}{2}{3}", itemStock.MaterialCode, Helper.FormatThousand(QuantityNew), itemStock.LotNumber, itemStock.InDate.Value.ToString("yyyyMMdd").Substring(1));
                                    itemStock.Quantity = QuantityNew;
                                    itemStock.QtyPerBag = QuantityNew;
                                }

                                QtyStockMain = itemStock.Quantity;
                            }

                        }

                        if (ItemSourceType == "SFG")
                        {
                            var itemStock = db.StockSFGs.FirstOrDefault(x => x.ID == itemStockTarget.ID);
                            if (itemStock != null)
                            {
                                var QtyPerBag = itemStock.QtyPerBag;
                                var QuantityNew = itemStock.Quantity - model.QtyTransfer;
                                if (QuantityNew > itemStock.QtyPerBag)
                                {
                                    var QtyRemainder = QuantityNew % itemStock.QtyPerBag;
                                    if (QtyRemainder > 0)
                                    {
                                        var stockSFG = new StockSFG();
                                        stockSFG.ID = Helper.CreateGuid("S");
                                        stockSFG.MaterialCode = itemStock.MaterialCode;
                                        stockSFG.MaterialName = itemStock.MaterialName;
                                        stockSFG.Code = string.Format("{0}{1}{2}{3}{4}", itemStock.MaterialCode, Helper.FormatThousand(QtyRemainder), itemStock.LotNumber, itemStock.InDate.Value.ToString("yyyyMMdd").Substring(1), itemStock.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                        stockSFG.LotNumber = itemStock.LotNumber;
                                        stockSFG.InDate = itemStock.InDate;
                                        stockSFG.ExpiredDate = itemStock.ExpiredDate;
                                        stockSFG.Quantity = QtyRemainder;
                                        stockSFG.QtyPerBag = QtyRemainder;
                                        stockSFG.BinRackID = itemStock.BinRackID;
                                        stockSFG.BinRackCode = itemStock.BinRackCode;
                                        stockSFG.BinRackName = itemStock.BinRackName;
                                        stockSFG.ReceivedAt = DateTime.Now;

                                        db.StockSFGs.Add(stockSFG);

                                        StockIDSourceRetail = stockSFG.ID;
                                        QtyRetail = QtyRemainder;
                                    }

                                    itemStock.Quantity = QuantityNew - QtyRemainder;
                                }
                                else
                                {
                                    itemStock.Code = string.Format("{0}{1}{2}{3}{4}", itemStock.MaterialCode, Helper.FormatThousand(QuantityNew), itemStock.LotNumber, itemStock.InDate.Value.ToString("yyyyMMdd").Substring(1), itemStock.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    itemStock.Quantity = QuantityNew;
                                    itemStock.QtyPerBag = QuantityNew;
                                }

                                QtyStockMain = itemStock.Quantity;
                            }
                        }


                        if (model.ProductTypeTarget == "RM")
                        {
                            StockRM stockRM = db.StockRMs.FirstOrDefault(x => x.BinRackCode == stockParam.BinRackCode && x.MaterialCode == model.ProductIDTarget && x.QtyPerBag == stockParam.QtyPerBag && x.LotNumber == stockParam.LotNumber && x.InDate == stockParam.InDate);
                            if (stockRM != null)
                            {
                                var QuantityNew = stockRM.Quantity + model.QtyTransfer;
                                if (QuantityNew > stockRM.QtyPerBag)
                                {
                                    var QtyRemainder = QuantityNew % stockParam.QtyPerBag;
                                    if (QtyRemainder > 0)
                                    {
                                        var newStockRM = new StockRM();
                                        newStockRM.ID = Helper.CreateGuid("S");
                                        newStockRM.MaterialCode = stockRM.MaterialCode;
                                        newStockRM.MaterialName = stockRM.MaterialName;
                                        newStockRM.Code = string.Format("{0}{1}{2}{3}{4}", stockRM.MaterialCode, Helper.FormatThousand(QtyRemainder), stockRM.LotNumber, stockRM.InDate.Value.ToString("yyyyMMdd").Substring(1), stockRM.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                        newStockRM.LotNumber = stockParam.LotNumber;
                                        newStockRM.InDate = stockRM.InDate;
                                        newStockRM.ExpiredDate = stockRM.ExpiredDate;
                                        newStockRM.Quantity = QtyRemainder;
                                        newStockRM.QtyPerBag = QtyRemainder;
                                        newStockRM.BinRackID = stockRM.BinRackID;
                                        newStockRM.BinRackCode = stockRM.BinRackCode;
                                        newStockRM.BinRackName = stockRM.BinRackName;
                                        newStockRM.ReceivedAt = DateTime.Now;

                                        db.StockRMs.Add(newStockRM);

                                        StockIDTargetRetail = newStockRM.ID;
                                        QtyTransferRetail = QtyRemainder;
                                    }
                                    stockRM.Quantity = QuantityNew - QtyRemainder;
                                }
                                else
                                {
                                    stockRM.Code = string.Format("{0}{1}{2}{3}{4}", stockRM.MaterialCode, Helper.FormatThousand(QuantityNew), stockRM.LotNumber, stockRM.InDate.Value.ToString("yyyyMMdd").Substring(1), stockRM.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockRM.Quantity = QuantityNew;
                                    stockRM.QtyPerBag = QuantityNew;
                                }

                                QtyTransfer = stockRM.Quantity;
                            }
                            else
                            {
                                var itemTarget = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == model.ProductIDTarget);
                                var binRack = db.BinRacks.FirstOrDefault(x => x.Code == stockParam.BinRackCode);

                                var QtyRemainder = model.QtyTransfer % stockParam.QtyPerBag;
                                if (QtyRemainder > 0)
                                {
                                    stockRM = new StockRM();
                                    stockRM.ID = Helper.CreateGuid("S");
                                    stockRM.MaterialCode = itemTarget.MaterialCode;
                                    stockRM.MaterialName = itemTarget.MaterialName;
                                    stockRM.Code = string.Format("{0}{1}{2}{3}{4}", itemTarget.MaterialCode, Helper.FormatThousand(QtyRemainder), stockParam.LotNumber, stockParam.InDate.Value.ToString("yyyyMMdd").Substring(1), stockParam.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockRM.LotNumber = stockParam.LotNumber;
                                    stockRM.InDate = stockParam.InDate;
                                    stockRM.ExpiredDate = stockParam.ExpiredDate;
                                    stockRM.Quantity = QtyRemainder;
                                    stockRM.QtyPerBag = QtyRemainder;
                                    stockRM.BinRackID = binRack.ID;
                                    stockRM.BinRackCode = binRack.Code;
                                    stockRM.BinRackName = binRack.Name;
                                    stockRM.ReceivedAt = DateTime.Now;

                                    db.StockRMs.Add(stockRM);

                                    StockIDTargetRetail = stockRM.ID;
                                    QtyTransferRetail = QtyRemainder;
                                }

                                var QtyNew = model.QtyTransfer - QtyRemainder;
                                if (QtyNew > 0)
                                {
                                    stockRM = new StockRM();
                                    stockRM.ID = Helper.CreateGuid("S");
                                    stockRM.MaterialCode = itemTarget.MaterialCode;
                                    stockRM.MaterialName = itemTarget.MaterialName;
                                    stockRM.Code = string.Format("{0}{1}{2}{3}{4}", itemTarget.MaterialCode, Helper.FormatThousand(stockParam.QtyPerBag), stockParam.LotNumber, stockParam.InDate.Value.ToString("yyyyMMdd").Substring(1), stockParam.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockRM.LotNumber = stockParam.LotNumber;
                                    stockRM.InDate = stockParam.InDate;
                                    stockRM.ExpiredDate = stockParam.ExpiredDate;
                                    stockRM.Quantity = QtyNew;
                                    stockRM.QtyPerBag = stockParam.QtyPerBag;
                                    stockRM.BinRackID = binRack.ID;
                                    stockRM.BinRackCode = binRack.Code;
                                    stockRM.BinRackName = binRack.Name;
                                    stockRM.ReceivedAt = DateTime.Now;

                                    db.StockRMs.Add(stockRM);
                                }

                                QtyTransfer = stockRM.Quantity;
                            }

                            TransferDetail _detail = new TransferDetail();
                            _detail.ID = Guid.NewGuid();
                            _detail.TransferID = transform.ID;
                            _detail.ProductIDSource = itemStockTarget.MaterialCode;
                            _detail.StockIDSource = itemStockTarget.ID;
                            _detail.Qty = QtyStockMain;

                            _detail.ProductIDTarget = stockRM.MaterialCode;
                            _detail.StockIDTarget = stockRM.ID;
                            _detail.QtyTransfer = QtyTransfer;
                            _detail.StockIDSourceRetail = StockIDSourceRetail;
                            _detail.StockIDTargetRetail = StockIDTargetRetail;

                            if (!string.IsNullOrEmpty(StockIDSourceRetail))
                            {
                                _detail.StockIDSourceRetail = StockIDSourceRetail;
                                _detail.QtyRetail = QtyRetail;
                            }

                            if (!string.IsNullOrEmpty(StockIDTargetRetail))
                            {
                                _detail.StockIDTargetRetail = StockIDTargetRetail;
                                _detail.QtyTransferRetail = QtyTransferRetail;
                            }

                            transform.TransferDetails.Add(_detail);
                        }


                        if (model.ProductTypeTarget == "SFG")
                        {
                            StockSFG stockSFG = db.StockSFGs.FirstOrDefault(x => x.BinRackCode == stockParam.BinRackCode && x.MaterialCode == model.ProductIDTarget && x.QtyPerBag == stockParam.QtyPerBag && x.LotNumber == stockParam.LotNumber && x.InDate == stockParam.InDate);
                            if (stockSFG != null)
                            {
                                var QuantityNew = stockSFG.Quantity + model.QtyTransfer;
                                if (QuantityNew > stockSFG.QtyPerBag)
                                {
                                    var QtyRemainder = QuantityNew % stockParam.QtyPerBag;
                                    if (QtyRemainder > 0)
                                    {
                                        var newStockSFG = new StockSFG();
                                        newStockSFG.ID = Helper.CreateGuid("S");
                                        newStockSFG.MaterialCode = stockSFG.MaterialCode;
                                        newStockSFG.MaterialName = stockSFG.MaterialName;
                                        newStockSFG.Code = string.Format("{0}{1}{2}{3}{4}", stockSFG.MaterialCode, Helper.FormatThousand(QtyRemainder), stockSFG.LotNumber, stockSFG.InDate.Value.ToString("yyyyMMdd").Substring(1), stockSFG.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                        newStockSFG.LotNumber = stockParam.LotNumber;
                                        newStockSFG.InDate = stockSFG.InDate;
                                        newStockSFG.ExpiredDate = stockSFG.ExpiredDate;
                                        newStockSFG.Quantity = QtyRemainder;
                                        newStockSFG.QtyPerBag = QtyRemainder;
                                        newStockSFG.BinRackID = stockSFG.BinRackID;
                                        newStockSFG.BinRackCode = stockSFG.BinRackCode;
                                        newStockSFG.BinRackName = stockSFG.BinRackName;
                                        newStockSFG.ReceivedAt = DateTime.Now;

                                        db.StockSFGs.Add(newStockSFG);
                                        StockIDTargetRetail = newStockSFG.ID;
                                        QtyTransferRetail = QtyRemainder;
                                    }
                                    stockSFG.Quantity = QuantityNew - QtyRemainder;
                                }
                                else
                                {
                                    stockSFG.Code = string.Format("{0}{1}{2}{3}{4}", stockSFG.MaterialCode, Helper.FormatThousand(QuantityNew), stockSFG.LotNumber, stockSFG.InDate.Value.ToString("yyyyMMdd").Substring(1), stockSFG.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockSFG.Quantity = QuantityNew;
                                    stockSFG.QtyPerBag = QuantityNew;
                                }
                                QtyTransfer = stockSFG.Quantity;
                            }
                            else
                            {
                                var itemTarget = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == model.ProductIDTarget);
                                var binRack = db.BinRacks.FirstOrDefault(x => x.Code == stockParam.BinRackCode);

                                var QtyRemainder = model.QtyTransfer % stockParam.QtyPerBag;
                                if (QtyRemainder > 0)
                                {
                                    stockSFG = new StockSFG();
                                    stockSFG.ID = Helper.CreateGuid("S");
                                    stockSFG.MaterialCode = itemTarget.MaterialCode;
                                    stockSFG.MaterialName = itemTarget.MaterialName;
                                    stockSFG.Code = string.Format("{0}{1}{2}{3}{4}", itemTarget.MaterialCode, Helper.FormatThousand(QtyRemainder), stockParam.LotNumber, stockParam.InDate.Value.ToString("yyyyMMdd").Substring(1), stockParam.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockSFG.LotNumber = stockParam.LotNumber;
                                    stockSFG.InDate = stockParam.InDate;
                                    stockSFG.ExpiredDate = stockParam.ExpiredDate;
                                    stockSFG.Quantity = QtyRemainder;
                                    stockSFG.QtyPerBag = QtyRemainder;
                                    stockSFG.BinRackID = binRack.ID;
                                    stockSFG.BinRackCode = binRack.Code;
                                    stockSFG.BinRackName = binRack.Name;
                                    stockSFG.ReceivedAt = DateTime.Now;

                                    db.StockSFGs.Add(stockSFG);

                                    StockIDTargetRetail = stockSFG.ID;
                                    QtyTransferRetail = QtyRemainder;
                                }

                                var QtyNew = model.QtyTransfer - QtyRemainder;
                                if (QtyNew < 0)
                                {
                                    stockSFG = new StockSFG();
                                    stockSFG.ID = Helper.CreateGuid("S");
                                    stockSFG.MaterialCode = itemTarget.MaterialCode;
                                    stockSFG.MaterialName = itemTarget.MaterialName;
                                    stockSFG.Code = string.Format("{0}{1}{2}{3}{4}", itemTarget.MaterialCode, Helper.FormatThousand(stockParam.QtyPerBag), stockParam.LotNumber, stockParam.InDate.Value.ToString("yyyyMMdd").Substring(1), stockParam.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                                    stockSFG.LotNumber = stockParam.LotNumber;
                                    stockSFG.InDate = stockParam.InDate;
                                    stockSFG.ExpiredDate = stockParam.ExpiredDate;
                                    stockSFG.Quantity = QtyNew;
                                    stockSFG.QtyPerBag = stockParam.QtyPerBag;
                                    stockSFG.BinRackID = binRack.ID;
                                    stockSFG.BinRackCode = binRack.Code;
                                    stockSFG.BinRackName = binRack.Name;
                                    stockSFG.ReceivedAt = DateTime.Now;

                                    db.StockSFGs.Add(stockSFG);
                                }

                                QtyTransfer = stockSFG.Quantity;

                            }

                            TransferDetail _detail = new TransferDetail();
                            _detail.ID = Guid.NewGuid();
                            _detail.TransferID = transform.ID;
                            _detail.ProductIDSource = itemStockTarget.MaterialCode;
                            _detail.StockIDSource = itemStockTarget.ID;
                            _detail.Qty = itemStockTarget.Quantity;

                            _detail.ProductIDTarget = stockSFG.MaterialCode;
                            _detail.StockIDTarget = stockSFG.ID;
                            _detail.QtyTransfer = model.QtyTransfer;
                            _detail.StockIDSourceRetail = StockIDSourceRetail;
                            _detail.StockIDTargetRetail = StockIDTargetRetail;

                            if (!string.IsNullOrEmpty(StockIDSourceRetail))
                            {
                                _detail.StockIDSourceRetail = StockIDSourceRetail;
                                _detail.QtyRetail = QtyRetail;
                            }

                            if (!string.IsNullOrEmpty(StockIDTargetRetail))
                            {
                                _detail.StockIDTargetRetail = StockIDTargetRetail;
                                _detail.QtyTransferRetail = QtyTransferRetail;
                            }

                            transform.TransferDetails.Add(_detail);
                        }

                        db.SaveChanges();
                        response.SetSuccess("Material code {0} to {1} success to transfer", model.ProductIDSource, model.ProductIDTarget);

                    }                    
                }
                else
                {
                    response.SetError(sbError.ToString());
                }

            }
            catch (Exception ex)
            {
                response.SetError(ex.Message);
            }

            return Ok(response);
        }


        /// <summary>
        /// List PR Box Approval
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/transfer/data-table")]
        public IHttpActionResult GetData()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, Func<Transfer, object>> cols = new Dictionary<string, Func<Transfer, object>>();
            cols.Add("TransferNo", x => x.TransferNo);
            cols.Add("CreatedOn", x => x.CreatedOn);
            cols.Add("CreatedBy", x => x.CreatedBy);

            IQueryable<Transfer> query = db.Transfers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query
                    .Where(m => m.TransferNo.Contains(search) || m.ProductID.Contains(search) || m.ProductIDTarget.Contains(search));
            }


            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            if (sortDirection.Equals("asc"))
                query = query.OrderBy(cols[sortName]).AsQueryable();
            else
                query = query.OrderByDescending(cols[sortName]).AsQueryable();

            recordsFiltered = query.Count();
            var queryList = query.Skip(start).Take(length).ToList();
            List<TransferTableModel> list = new List<TransferTableModel>();
            foreach (var item in queryList)
            {
                vProductMaster productSource = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == item.ProductID);
                vProductMaster productTarget = db.vProductMasters.FirstOrDefault(x => x.MaterialCode == item.ProductIDTarget);
                var details = db.TransferDetails.Where(x => x.TransferID == item.ID).ToList();

                var _itemNew = new TransferTableModel()
                {
                    ID = item.ID,
                    TransferNo = item.TransferNo,
                    CreatedBy = item.CreatedBy,
                    CreatedOn = item.CreatedOn,
                    CreatedOnStr = Helper.NullDateTimeToString(item.CreatedOn),
                    MaterialSource = String.Format("{0}-{1}", productSource.MaterialCode, productSource.MaterialName),
                    MaterialSourceType = item.ProductType,
                    MaterialTarget = String.Format("{0}-{1}", productTarget.MaterialCode, productTarget.MaterialName),
                    MaterialTargetType = item.ProductTypeTarget,
                    QtyTransfer = item.QtyTransfer,
                    QtyRemain = item.QtyTransfer - (details != null && details.Count > 0 ? details.Sum(x => x.QtyTransfer) + details.Sum(x => x.QtyTransferRetail): 0),
                    Status = "NEW"
                };

                if(_itemNew.QtyRemain == 0)
                {
                    _itemNew.Status = "COMPLETED";
                } else
                {
                    if (_itemNew.QtyRemain > 0 && _itemNew.QtyRemain < _itemNew.QtyTransfer)
                    {
                        _itemNew.Status = "IN PROGRESS";
                    }
                }

                list.Add(_itemNew);
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

        [HttpPost]
        [Route("api/transfer/items")]
        public async Task<IHttpActionResult> ItemTransfers(SearchItemModel model)
        {
            IQueryable<ItemLookupModel> list = null;

            if(model.Source == "SFG")
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
            } else
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

            if(model.ExcludeItems != null && model.ExcludeItems.Count > 0)
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
        [Route("api/transfer/items-target")]
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
    }
}
