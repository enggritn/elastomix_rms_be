using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Routing;
using WMS_BE.Models;
using WMS_BE.Utils;
using ZXing;
using ZXing.QrCode;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace WMS_BE.Controllers.Api
{
    public class MobileStockTransformController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpGet]
        public async Task<IHttpActionResult> GetProductList(string MaterialName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<MaterialInfo> list = Enumerable.Empty<MaterialInfo>();

            try
            {
                
                IQueryable<vStockProduct> query = db.vStockProducts.AsQueryable();

                //if (type.Equals("source"))
                //{
                //    query = query.Where(m => m.TotalQty > 0);
                //}

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                    list = from detail in await query.ToListAsync()
                           select new MaterialInfo
                           {
                               MaterialCode = detail.MaterialCode,
                               MaterialName = detail.MaterialName,
                               TotalQty = Helper.FormatThousand(detail.TotalQty),
                               MaterialType = detail.ProdType
                           };
                }


                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";
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

            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(StockTransformHeaderReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            StockTransformListResp data = new StockTransformListResp();

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
                    if (string.IsNullOrEmpty(req.MaterialCode))
                    {
                        throw new Exception("Source Material Code is required.");
                    }
                    else
                    {
                        product = db.vStockProducts.Where(m => m.MaterialCode.Equals(req.MaterialCode)).FirstOrDefault();
                        if (product == null)
                        {
                            throw new Exception("Source Material Code not found.");
                        }
                    }

                    if (string.IsNullOrEmpty(req.MaterialCodeTarget))
                    {
                        throw new Exception("Target Material Code is required.");
                    }
                    else
                    {
                        productTarget = db.vStockProducts.Where(m => m.MaterialCode.Equals(req.MaterialCodeTarget)).FirstOrDefault();
                        if (productTarget == null)
                        {
                            throw new Exception("Target Material Code not found.");
                        }
                    }

                    if (req.TotalQty <= 0)
                    {
                        throw new Exception("Transform Qty is required.");
                    }
                    else
                    {
                        //validation available qty
                        if (req.TotalQty > product.TotalQty)
                        {
                            throw new Exception(string.Format("Transform Qty exceeded. Available Qty : {0}", product.TotalQty));
                        }
                    }


                    DateTime now = DateTime.Now;
                    var CreatedAt = now;
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
                        TotalQty = req.TotalQty,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                    };

                    db.Transforms.Add(header);

                    await db.SaveChangesAsync();

                    data = new StockTransformListResp
                       {
                        ID = header.ID.ToString(),
                           TransformNo = header.Code,
                           MaterialCode = header.MaterialCode,
                           MaterialName = header.MaterialName,
                           MaterialType = header.MaterialType,
                           MaterialCodeTarget = header.MaterialCodeTarget,
                           MaterialNameTarget = header.MaterialNameTarget,
                           MaterialTypeTarget = header.MaterialType,
                           TransformQty = Helper.FormatThousand(header.TotalQty),
                           OutstandingQty = Helper.FormatThousand(header.TotalQty - (header.TransformDetails.Where(m => m.MaterialCode.Equals(header.MaterialCode)).Sum(m => m.Qty))),
                           CreatedBy = header.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(header.CreatedOn)
                       };

                    status = true;
                    message = "Data berhasil dibuat.";

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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> GetList()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<StockTransformListResp> list = Enumerable.Empty<StockTransformListResp>();


            try
            {
                IQueryable<Transform> query = query = db.Transforms.Where(s => s.TransactionStatus.Equals("OPEN")).AsQueryable();


                IEnumerable<Transform> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new StockTransformListResp
                       {
                           ID = data.ID.ToString(),
                           TransformNo = data.Code,
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName,
                           MaterialType = data.MaterialType,
                           MaterialCodeTarget = data.MaterialCodeTarget,
                           MaterialNameTarget = data.MaterialNameTarget,
                           MaterialTypeTarget = data.MaterialType,
                           TransformQty = Helper.FormatThousand(data.TotalQty),
                           OutstandingQty = Helper.FormatThousand(data.TotalQty - (data.TransformDetails.Where(m => m.MaterialCode.Equals(data.MaterialCode)).Sum(m => m.Qty))),
                           CreatedBy = data.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(data.CreatedOn)
                       };

                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";
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

            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetTransformList(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<StockTransformDetailResp> list = Enumerable.Empty<StockTransformDetailResp>();

            StockTransformListResp headerDTO = new StockTransformListResp();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                Transform header = db.Transforms.Where(m => m.ID.Equals(id)).FirstOrDefault();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");

                }

                headerDTO = new StockTransformListResp
                {
                    ID = header.ID.ToString(),
                    TransformNo = header.Code,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    MaterialType = header.MaterialType,
                    MaterialCodeTarget = header.MaterialCodeTarget,
                    MaterialNameTarget = header.MaterialNameTarget,
                    MaterialTypeTarget = header.MaterialType,
                    TransformQty = Helper.FormatThousand(header.TotalQty),
                    OutstandingQty = Helper.FormatThousand(header.TotalQty - (header.TransformDetails.Where(m => m.MaterialCode.Equals(header.MaterialCode)).Sum(m => m.Qty))),
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn)
                };

                IQueryable<TransformDetail> query = query = db.TransformDetails.Where(m => m.TransformID.Equals(id)).AsQueryable();


                IEnumerable<TransformDetail> tempList = await query.OrderBy(m => m.MaterialCode).ToListAsync();

                list = from data in tempList
                       select new StockTransformDetailResp
                       {
                           ID = data.ID,
                           TransformID = data.TransformID,
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName,
                           StockCode = data.StockCode,
                           LotNo = data.LotNo,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpDate),
                           Qty = Helper.FormatThousand(data.Qty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           LastSeries = data.LastSeries.ToString(),
                           PrintedAt = Helper.NullDateTimeToString(data.PrintedAt),
                           PrintedBy = data.PrintedBy,
                           PrintBarcodeAction = string.IsNullOrEmpty(data.PrintedBy) && data.LastSeries > 0
                       };

                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";
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

            obj.Add("data", headerDTO);
            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetPickingList(string HeaderId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(HeaderId))
            {
                throw new Exception("Header Id is required.");
            }

            Transform trf = db.Transforms.Where(m => m.ID.ToString().Equals(HeaderId)).FirstOrDefault();

            if (trf == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }
            //string warehouseCode = request["warehouseCode"].ToString();
            //string areaCode = request["areaCode"].ToString();

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(trf.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            query = query.Where(m => warehouses.Contains(m.WarehouseCode));
            int totalRow = query.Count();


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

                            if (MaterialType.Equals("RM"))
                            {
                                dat.BarcodeRight = vProductMaster.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(stock.QtyPerBag).PadLeft(6) + " " + stock.LotNumber;
                                dat.BarcodeLeft = vProductMaster.MaterialCode.PadRight(7) + stock.InDate.Value.ToString("yyyyMMdd").Substring(1) + stock.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2);
                            }
                            else
                            {
                                dat.Barcode = "";
                            }

                            if (dat.IsExpired)
                            {
                                dat.QCAction = true;
                            }

                            data.Add(dat);
                        }
                        else
                        {
                            break;
                        }

                    }

                    message = "Fetch data succeeded.";
                }

                if (data.Count() < 1)
                {
                    message = "Tidak ada stock tersedia.";
                }

                status = true;


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
            obj.Add("list", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Picking(StockTransformPickingReq req)
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


                    if (string.IsNullOrEmpty(req.HeaderId))
                    {
                        throw new Exception("Header Id is required.");
                    }

                    Transform trf = db.Transforms.Where(m => m.ID.ToString().Equals(req.HeaderId)).FirstOrDefault();

                    if (trf == null)
                    {
                        throw new Exception("Data tidak ditemukan.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    vProductMaster vProductMasterTarget = await db.vProductMasters.Where(m => m.MaterialCode.Equals(trf.MaterialCodeTarget)).FirstOrDefaultAsync();
                    if (vProductMasterTarget == null)
                    {
                        throw new Exception("Material Target tidak dikenali.");
                    }


                    string StockCode = "";

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);


                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        throw new Exception("BinRack harus diisi.");

                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack tidak ditemukan.");
                        }

                    }


                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }


                    //restriction 1 : AREA TYPE

                    User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
                    string userAreaType = userData.AreaType;

                    string materialAreaType = stockAll.BinRackAreaType;

                    if (!userAreaType.Equals(materialAreaType))
                    {
                        throw new Exception(string.Format("FIFO Restriction, tidak dapat mengambil material di area {0}", materialAreaType));
                    }



                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(trf.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType))
                       .OrderByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                       .ThenBy(s => s.InDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();
                    //.ThenBy(s => s.Quantity).FirstOrDefault();

                    if (stkAll == null)
                    {
                        throw new Exception("Stock tidak tersedia.");
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
                        throw new Exception("Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        decimal Qty = stockAll.Quantity;

                        if (req.Qty > Qty)
                        {
                            throw new Exception(string.Format("Qty melewati jumlah yang dibutuhkan. Qty tersedia : {0}", Helper.FormatThousand(Qty)));
                        }
                        else
                        {
                            decimal requestedQty = trf.TotalQty;
                            decimal pickedQty = trf.TransformDetails.Where(m => m.MaterialCode.Equals(vProductMasterTarget.MaterialCode)).Sum(i => i.Qty);
                            decimal availableQty = requestedQty - pickedQty;

                            if (req.Qty > availableQty)
                            {
                                throw new Exception(string.Format("Qty melewati jumlah tersedia. Qty tersedia : {0}", Helper.FormatThousand(availableQty)));
                            }
                        }
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

                        if(remainderQty > 0)
                        {
                            string stkCode = string.Format("{0}{1}{2}{3}{4}", vProductMaster.MaterialCode, Helper.FormatThousand(remainderQty), stockAll.LotNumber, stockAll.InDate.Value.ToString("yyyyMMdd").Substring(1), stockAll.ExpiredDate.Value.ToString("yyyyMMdd").Substring(2));
                            StockRM stk = db.StockRMs.Where(m => m.Code.Equals(stkCode) && m.BinRackCode.Equals(stockAll.BinRackCode)).FirstOrDefault();
                            if(stk != null)
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
                            
                            //new material, print barcode

                            int startSeries = 0;
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
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

                        if(curBagQtyTarget > 0)
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
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                            if (lastSeries == 0)
                            {
                                startSeries = 1;
                            }
                            else
                            {
                                startSeries = lastSeries + curBagQtyTarget;
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
                        
                        if(remainderQtyTarget > 0)
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
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
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
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
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
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
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
                            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stkCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
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

                    int lastSeries = detail.LastSeries;
                    int startSeries = 0;


                    //get last series
                    seq = Convert.ToInt32(startSeries);


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
                                if (printerDTO.PrinterIP.Equals(req.Printer))
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
    }
}
