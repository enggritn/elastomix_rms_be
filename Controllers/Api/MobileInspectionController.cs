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
    public class MobileInspectionController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Create(QCInspectionVM dataVM)
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
                    if (string.IsNullOrEmpty(dataVM.StockID))
                    {
                        throw new Exception("StockID is required.");
                    }

                    vStockAll vStock = await db.vStockAlls.Where(m => m.ID.Equals(dataVM.StockID)).FirstOrDefaultAsync();
                    if (vStock == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    //check expired date
                    if (!(DateTime.Now.Date >= vStock.ExpiredDate.Value.Date))
                    {
                        throw new Exception("Material belum kadaluarsa.");
                    }

                    //check if material on qc progress
                    if (vStock.Quantity <= 0)
                    {
                        throw new Exception("Material tidak tersedia.");
                    }

                    //check qc already created
                    QCInspection prevInspect = db.QCInspections.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNo.Equals(vStock.LotNumber) && m.InDate == vStock.InDate && m.ExpDate == vStock.ExpiredDate).FirstOrDefault();

                    if (prevInspect != null && !prevInspect.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("QC Inspection already on progress.");
                    }

                    var CreatedAt = DateTime.Now;
                    var TransactionId = Helper.CreateGuid("QC");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.QCInspections.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                    QCInspection qCInspection = new QCInspection
                    {
                        ID = TransactionId,
                        Code = Code,
                        MaterialType = vStock.Type,
                        MaterialCode = vStock.MaterialCode,
                        MaterialName = vStock.MaterialName,
                        LotNo = vStock.LotNumber,
                        InDate = vStock.InDate.Value,
                        ExpDate = vStock.ExpiredDate.Value,
                        TransactionStatus = "PROGRESS",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        Priority = true
                    };

                    List<StockDTO> stocks = new List<StockDTO>();
                    List<QCPicking> pickings = new List<QCPicking>();

                    if (vStock.Type.Equals("RM"))
                    {
                        //create picking list
                        IEnumerable<StockRM> stockRMs = await db.StockRMs.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNumber.Equals(vStock.LotNumber) && m.InDate.Value.Equals(vStock.InDate.Value) && m.ExpiredDate.Value.Equals(vStock.ExpiredDate.Value)).ToListAsync();
                        foreach (StockRM stock in stockRMs)
                        {
                            if (stock.Quantity > 0)
                            {
                                QCPicking pick = new QCPicking();
                                pick.ID = Helper.CreateGuid("QCp");
                                pick.QCInspectionID = TransactionId;
                                pick.StockCode = stock.Code;
                                pick.BinRackID = stock.BinRackID;
                                pick.BinRackCode = stock.BinRackCode;
                                pick.BinRackName = stock.BinRackName;
                                pick.Qty = stock.Quantity;
                                pick.QtyPerBag = stock.QtyPerBag;

                                pickings.Add(pick);

                                //update stock to zero
                                //stock.Quantity = 0;
                                stock.OnInspect = true;
                            }
                            
                        }
                    }
                    else
                    {
                        IEnumerable<StockSFG> stockSFGs = await db.StockSFGs.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNumber.Equals(vStock.LotNumber) && m.InDate.Value.Equals(vStock.InDate.Value) && m.ExpiredDate.Value.Equals(vStock.ExpiredDate.Value)).ToListAsync();
                        foreach (StockSFG stock in stockSFGs)
                        {
                            if (stock.Quantity > 0)
                            {
                                QCPicking pick = new QCPicking();
                                pick.ID = Helper.CreateGuid("QCp");
                                pick.QCInspectionID = TransactionId;
                                pick.StockCode = stock.Code;
                                pick.BinRackID = stock.BinRackID;
                                pick.BinRackCode = stock.BinRackCode;
                                pick.BinRackName = stock.BinRackName;
                                pick.Qty = stock.Quantity;
                                pick.QtyPerBag = stock.QtyPerBag;

                                pickings.Add(pick);

                                //update stock to zero
                                //stock.Quantity = 0;
                                stock.OnInspect = true;
                            }
                           
                        }
                    }

                    qCInspection.QCPickings = pickings;

                    db.QCInspections.Add(qCInspection);

                    await db.SaveChangesAsync();

                    obj.Add("id", qCInspection.ID);

                    status = true;
                    message = "QC Inspection berhasil dibuat.";

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

        [HttpGet]
        public async Task<IHttpActionResult> GetList(string InspectionStatus, string MaterialName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<InspectionListResp> list = Enumerable.Empty<InspectionListResp>();

            //add new property

            List<InspectionCountResp> list_count = new List<InspectionCountResp>();

            int totalWaiting = db.QCInspections.Where(m => string.IsNullOrEmpty(m.InspectionStatus) && !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
            int totalExtend = db.QCInspections.Where(m => m.InspectionStatus.Equals("EXTEND") && !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
            int totalDispose = db.QCInspections.Where(m => m.InspectionStatus.Equals("DISPOSE") && !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
            int totalReturn = db.QCInspections.Where(m => m.InspectionStatus.Equals("RETURN") && !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();

            list_count.Add(new InspectionCountResp
            {
                StatusName = "Waiting",
                TotalRow = totalWaiting
            });

            list_count.Add(new InspectionCountResp
            {
                StatusName = "Extend",
                TotalRow = totalExtend
            });

            list_count.Add(new InspectionCountResp
            {
                StatusName = "Dispose",
                TotalRow = totalDispose
            });

            list_count.Add(new InspectionCountResp
            {
                StatusName = "Return",
                TotalRow = totalReturn
            });
            try
            {
                IQueryable<QCInspection> query = query = db.QCInspections.OrderByDescending(m => m.Priority).AsQueryable();
                if (!string.IsNullOrEmpty(InspectionStatus))
                {
                    query = query.Where(m => m.InspectionStatus.Equals(InspectionStatus) && !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED"));
                }
                else
                {
                    query = query.Where(s => string.IsNullOrEmpty(s.InspectionStatus));
                }

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(m => m.MaterialName.Contains(MaterialName));                   
                }
                              

                IEnumerable<QCInspection> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InspectionListResp
                       {
                           ID = data.ID,
                           Code = data.Code,
                           MaterialType = data.MaterialType,
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName,
                           LotNo = data.LotNo,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpDate),
                           TransactionStatus = data.TransactionStatus,
                           CreatedBy = data.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(data.CreatedOn),
                           Priority = data.Priority,
                           InspectionStatus = data.InspectionStatus
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

            obj.Add("list", list);
            obj.Add("list_count", list_count);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetListWaiting(string InspectionId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<InspectionWaitingListResp> list = Enumerable.Empty<InspectionWaitingListResp>();

            InspectionListResp headerDTO = new InspectionListResp();

            try
            {
                if (string.IsNullOrEmpty(InspectionId))
                {
                    throw new Exception("Id is required.");
                }

                QCInspection header = db.QCInspections.Where(m => m.ID.Equals(InspectionId) && string.IsNullOrEmpty(m.InspectionStatus)).FirstOrDefault();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");

                }

                headerDTO = new InspectionListResp
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialType = header.MaterialType,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    Priority = header.Priority
                };

                IQueryable<QCPicking> query = query = db.QCPickings.Where(m => m.QCInspectionID.Equals(InspectionId)).AsQueryable();


                IEnumerable<QCPicking> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InspectionWaitingListResp
                       {
                           ID = data.ID,
                           InspectionID = header.ID,
                           BinRackID = data.BinRackID,
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           Qty = Helper.FormatThousand(data.Qty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
                           PickingMethod = string.IsNullOrEmpty(data.PickedMethod) ? "-" : data.PickedMethod,
                           PickedBy = string.IsNullOrEmpty(data.PickedBy) ? "-" : data.PickedBy,
                           PickedOn = Helper.NullDateTimeToString(data.PickedOn),
                           PutawayBagQty = Helper.FormatThousand(data.QCPutaways.Sum(i => i.PutawayQty / i.QtyPerBag)),
                           PutawayAction = data.PickedOn != null && data.Qty != data.QCPutaways.Sum(i => i.PutawayQty),
                           BarcodeRight = header.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + header.LotNo,
                           BarcodeLeft = header.MaterialCode.PadRight(7) + header.InDate.ToString("yyyyMMdd").Substring(1) + header.ExpDate.ToString("yyyyMMdd").Substring(2),
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


        [HttpPost]
        public async Task<IHttpActionResult> PickingWaiting(PickingWaitingReq req)
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
                    if (string.IsNullOrEmpty(req.InspectionId))
                    {
                        throw new Exception("Inspection Id is required.");
                    }

                    QCInspection header = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (header == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (!string.IsNullOrEmpty(header.InspectionStatus))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(header.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);


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

                    //validasi material code yg discan, apabila material code berbeda munculkan notifikasi material code tidak sesuai, harusnya material : {material code}

                    QCPicking picking = db.QCPickings.Where(m => m.QCInspectionID.Equals(req.InspectionId) && m.BinRackCode.Equals(binRack.Code) && m.StockCode.Equals(StockCode)).FirstOrDefault();
                    if (picking == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(picking.PickedMethod))
                        {
                            throw new Exception("Stock sudah diambil.");
                        }
                    }

                    picking.PickedMethod = "SCAN";
                    picking.PickedOn = DateTime.Now;
                    picking.PickedBy = activeUser;

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
        public async Task<IHttpActionResult> PutawayWaiting(PutawayWaitingReq req)
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
                    if (string.IsNullOrEmpty(req.PickingId))
                    {
                        throw new Exception("Picking Id is required.");
                    }

                    QCPicking picking = await db.QCPickings.Where(s => s.ID.Equals(req.PickingId)).FirstOrDefaultAsync();

                    if (picking == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (!string.IsNullOrEmpty(picking.QCInspection.InspectionStatus))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(picking.QCInspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

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

                    if(!picking.BinRackCode.Equals(binRack.Code) && !picking.StockCode.Equals(StockCode))
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    if (string.IsNullOrEmpty(picking.PickedMethod))
                    {
                        throw new Exception("Stock belum diambil.");
                    }

                    //do stock movement in here
                    //insert to qc putaway
                    //update stock location

                    if (picking.BinRackCode.Equals(req.BinRackCode))
                    {
                        throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int pickedBagQty = Convert.ToInt32(picking.Qty / picking.QtyPerBag);
                        int putBagQty = Convert.ToInt32(picking.QCPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
                        int availableBagQty = pickedBagQty - putBagQty;
                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                        }
                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(picking.BinRackCode)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        //update stock
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(picking.StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = vProductMaster.MaterialCode;
                            stockRM.MaterialName = vProductMaster.MaterialName;
                            stockRM.Code = stockAll.Code;
                            stockRM.LotNumber = stockAll.LotNumber;
                            stockRM.InDate = stockAll.InDate;
                            stockRM.ExpiredDate = stockAll.ExpiredDate;
                            stockRM.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockRM.QtyPerBag = stockAll.QtyPerBag;
                            stockRM.BinRackID = binRack.ID;
                            stockRM.BinRackCode = binRack.Code;
                            stockRM.BinRackName = binRack.Name;
                            stockRM.ReceivedAt = DateTime.Now;
                            stockRM.OnInspect = true;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(picking.StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = vProductMaster.MaterialCode;
                            stockSFG.MaterialName = vProductMaster.MaterialName;
                            stockSFG.Code = stockAll.Code;
                            stockSFG.LotNumber = stockAll.LotNumber;
                            stockSFG.InDate = stockAll.InDate;
                            stockSFG.ExpiredDate = stockAll.ExpiredDate;
                            stockSFG.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockSFG.QtyPerBag = stockAll.QtyPerBag;
                            stockSFG.BinRackID = binRack.ID;
                            stockSFG.BinRackCode = binRack.Code;
                            stockSFG.BinRackName = binRack.Name;
                            stockSFG.ReceivedAt = DateTime.Now;
                            stockSFG.OnInspect = true;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }

                    QCPutaway putaway = new QCPutaway();
                    putaway.ID = Helper.CreateGuid("QCp");
                    putaway.QCPickingID = picking.ID;
                    putaway.StockCode = stockAll.Code;
                    putaway.LotNo = stockAll.LotNumber;
                    putaway.InDate = stockAll.InDate.Value;
                    putaway.ExpDate = stockAll.ExpiredDate.Value;
                    putaway.PrevBinRackID = picking.BinRackID;
                    putaway.PrevBinRackCode = picking.BinRackCode;
                    putaway.PrevBinRackName = picking.BinRackName;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * stockAll.QtyPerBag;
                    putaway.QtyPerBag = stockAll.QtyPerBag;
                    putaway.PutawayMethod = "SCAN";
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;

                    db.QCPutaways.Add(putaway);

                    await db.SaveChangesAsync();


                    status = true;
                    message = "Putaway berhasil.";

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


        [HttpGet]
        public async Task<IHttpActionResult> GetListDispose(string InspectionId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<InspectionDisposeListResp> list = Enumerable.Empty<InspectionDisposeListResp>();

            InspectionListResp headerDTO = new InspectionListResp();

            try
            {
                if (string.IsNullOrEmpty(InspectionId))
                {
                    throw new Exception("Id is required.");
                }

                QCInspection header = db.QCInspections.Where(m => m.ID.Equals(InspectionId) && m.InspectionStatus.Equals("DISPOSE")).FirstOrDefault();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");

                }

                headerDTO = new InspectionListResp
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialType = header.MaterialType,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    Priority = header.Priority
                };

                IQueryable<vStockAll> query = query = db.vStockAlls.Where(m => m.MaterialCode.Equals(header.MaterialCode) && m.LotNumber.Equals(header.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(header.InDate) && DbFunctions.TruncateTime(m.ExpiredDate) == DbFunctions.TruncateTime(header.ExpDate) && m.Quantity > 0).AsQueryable();

                IEnumerable<vStockAll> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InspectionDisposeListResp
                       {
                           ID = data.ID,
                           InspectionID = header.ID,
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           Qty = Helper.FormatThousand(data.Quantity),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.Quantity / data.QtyPerBag),
                           BarcodeRight = header.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + header.LotNo,
                           BarcodeLeft = header.MaterialCode.PadRight(7) + header.InDate.ToString("yyyyMMdd").Substring(1) + header.ExpDate.ToString("yyyyMMdd").Substring(2),
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
        public async Task<IHttpActionResult> GetListReturn(string InspectionId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<InspectionReturnListResp> list = Enumerable.Empty<InspectionReturnListResp>();
            InspectionListRespReturn headerDTO = new InspectionListRespReturn();

            try
            {
                if (string.IsNullOrEmpty(InspectionId))
                {
                    throw new Exception("Id is required.");
                }

                QCInspection header = db.QCInspections.Where(m => m.ID.Equals(InspectionId) && m.InspectionStatus.Equals("RETURN")).FirstOrDefault();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");
                }

                QCPicking picking = await db.QCPickings.Where(m => m.QCInspectionID.Equals(header.ID) && m.PickedMethod.Equals("SCAN")).FirstOrDefaultAsync();
                QCPutaway putaway = await db.QCPutaways.Where(m => m.QCPickingID.Equals(picking.ID) && m.PutawayMethod.Equals("SCAN")).FirstOrDefaultAsync();

                decimal totQty = 0;
                decimal totReturnQty = 0;
                try
                {
                    totQty = db.QCReturns.Where(m => m.QCPutawayID.Equals(putaway.ID)).Sum(i => i.PutawayQty);
                }
                catch
                {
                }

                try
                {
                    totReturnQty = db.QCReturns.Where(m => m.QCPutawayID.Equals(putaway.ID) && !m.PutawayMethod.Equals("INSPECT")).Sum(i => i.PutawayQty);
                }
                catch
                {
                }
                
                headerDTO = new InspectionListRespReturn
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialType = header.MaterialType,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    Priority = header.Priority,
                    NewExpDate = Helper.NullDateToString(header.NewExpDate),
                    TotalQty = totQty,
                    TotalReturnQty = totReturnQty,
                };

                IQueryable<QCReturn> query = query = db.QCReturns.Where(m => m.QCPutawayID.Equals(putaway.ID) && m.PutawayMethod.Equals("INSPECT")).AsQueryable();
                IEnumerable<QCReturn> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InspectionReturnListResp
                       {
                           ID = data.ID,
                           InspectionID = header.ID,                           
                           BinRackID = data.BinRackID,
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           Qty = Helper.FormatThousand(data.PutawayQty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.PutawayQty / data.QtyPerBag),
                           PutawayMethod = string.IsNullOrEmpty(data.PutawayMethod) ? "-" : data.PutawayMethod,
                           PutBy = string.IsNullOrEmpty(data.PutBy) ? "-" : data.PutBy,
                           PutOn = Helper.NullDateTimeToString(data.PutOn),
                           PutawayAction = data.PutawayMethod.Equals("INSPECT"),
                           BarcodeRight = header.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + data.LotNo,
                           BarcodeLeft = header.MaterialCode.PadRight(7) + data.InDate.ToString("yyyyMMdd").Substring(1) + data.NewExpDate.ToString("yyyyMMdd").Substring(2),
                       };

                //check if all transaction is done, empty the list and update transaction status
                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";

                    header.TransactionStatus = "CLOSED";
                    await db.SaveChangesAsync();
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


        [HttpPost]
        public async Task<IHttpActionResult> PickingDispose(PickingDisposeReq req)
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


                    if (string.IsNullOrEmpty(req.InspectionId))
                    {
                        throw new Exception("Inspection Id is required.");
                    }

                    QCInspection header = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (header == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (header.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(header.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }


                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

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

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int bagQty = Convert.ToInt32(stockAll.Quantity / stockAll.QtyPerBag);

                        if (req.BagQty > bagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah yang dibutuhkan. Bag Qty tersedia : {0}", bagQty));
                        }
                        else
                        {
                            int availableBagQty = Convert.ToInt32(Math.Ceiling(stockAll.Quantity / stockAll.QtyPerBag));

                            if (req.BagQty > availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                            }
                        }
                    }

                    if (stockAll.Type.Equals("RM"))
                    {
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                        stock.OnInspect = false;
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                        stock.OnInspect = false;
                    }

                    await db.SaveChangesAsync();

                    IQueryable<vStockAll> query = query = db.vStockAlls.Where(m => m.MaterialCode.Equals(header.MaterialCode) && m.LotNumber.Equals(header.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(header.InDate) && DbFunctions.TruncateTime(m.ExpiredDate) == DbFunctions.TruncateTime(header.ExpDate) && m.Quantity > 0).AsQueryable();

                    IEnumerable<vStockAll> tempList = await query.ToListAsync();

                    if(tempList.Count() < 1)
                    {
                        header.TransactionStatus = "CLOSED";
                        await db.SaveChangesAsync();
                    }

                    status = true;
                    message = "Picking dispose berhasil.";
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


        [HttpGet]
        public async Task<IHttpActionResult> GetListExtend(string InspectionId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<InspectionExtendListResp> list = Enumerable.Empty<InspectionExtendListResp>();

            InspectionListResp headerDTO = new InspectionListResp();

            try
            {
                if (string.IsNullOrEmpty(InspectionId))
                {
                    throw new Exception("Id is required.");
                }

                QCInspection header = db.QCInspections.Where(m => m.ID.Equals(InspectionId) && m.InspectionStatus.Equals("EXTEND")).FirstOrDefault();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");
                }

                headerDTO = new InspectionListResp
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialType = header.MaterialType,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    Priority = header.Priority,
                    NewExpDate = Helper.NullDateToString(header.NewExpDate)
                };

                IQueryable<QCPutaway> query = query = db.QCPutaways.Where(m => m.QCPicking.QCInspectionID.Equals(InspectionId) && !m.PutawayQty.Equals(m.QCReturns.Sum(i => i.PutawayQty))).AsQueryable();
                IEnumerable<QCPutaway> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InspectionExtendListResp
                       {
                           ID = data.ID,
                           InspectionID = header.ID,
                           BinRackID = data.BinRackID,
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           Qty = Helper.FormatThousand(data.PutawayQty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.PutawayQty / data.QtyPerBag),
                           PickingMethod = string.IsNullOrEmpty(data.PickedMethod) ? "-" : data.PickedMethod,
                           PickedBy = string.IsNullOrEmpty(data.PickedBy) ? "-" : data.PickedBy,
                           PickedOn = Helper.NullDateTimeToString(data.PickedOn),
                           PutawayBagQty = Helper.FormatThousand(data.QCReturns.Sum(i => i.PutawayQty / i.QtyPerBag)),
                           PutawayAction = data.PutawayQty != data.QCReturns.Sum(i => i.PutawayQty),
                           BarcodeRight = header.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + header.LotNo,
                           BarcodeLeft = header.MaterialCode.PadRight(7) + header.InDate.ToString("yyyyMMdd").Substring(1) + header.NewExpDate.Value.ToString("yyyyMMdd").Substring(2),
                           Sample = !data.PutawayMethod.Equals("INSPECT")
                       };

                //check if all transaction is done, empty the list and update transaction status

                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";

                    header.TransactionStatus = "CLOSED";
                    await db.SaveChangesAsync();
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


        [HttpPost]
        public async Task<IHttpActionResult> PutawayExtend(PutawayExtendReq req)
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
                    if (string.IsNullOrEmpty(req.PutawayId))
                    {
                        throw new Exception("Putaway Id is required.");
                    }

                    QCPutaway putaway = await db.QCPutaways.Where(s => s.ID.Equals(req.PutawayId)).FirstOrDefaultAsync();

                    if (putaway == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (putaway.QCPicking.QCInspection.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(putaway.QCPicking.QCInspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

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

                    if (!putaway.NewStockCode.Equals(StockCode))
                    {
                        throw new Exception("Stock tidak ditemukan. Mohon cek kembali barcode yang discan.");
                    }

                    //do stock movement in here
                    //update to qc putaway
                    //update to qc inspection
                    //insert to qc return
                    //insert to stock RM / stock SFG

                    //check if old stock, dont allow stock movement
                    if (putaway.PutawayMethod.Equals("INSPECT"))
                    {
                        //same location with previous
                        if (!putaway.BinRackCode.Equals(req.BinRackCode))
                        {
                            throw new Exception("Bin/Rack harus diisi sesuai dengan Bin/Rack sebelumnya.");
                        }
                    }
                    else
                    {
                        //new location, cannot same with old location
                        if (putaway.BinRackCode.Equals(req.BinRackCode))
                        {
                            throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                        }

                        if (req.BagQty <= 0)
                        {
                            throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                        }
                        else
                        {
                            int putBagQty = Convert.ToInt32(putaway.PutawayQty / putaway.QtyPerBag);
                            int returnBagQty = Convert.ToInt32(putaway.QCReturns.Sum(s => s.PutawayQty / s.QtyPerBag));
                            int availableBagQty = putBagQty - returnBagQty;
                            if (req.BagQty > availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                            }
                        }
                    }

                    if (putaway.PutawayMethod.Equals("INSPECT"))
                    {
                        req.BagQty = Convert.ToInt32(putaway.PutawayQty / putaway.QtyPerBag);
                    }

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        StockRM stockRM = new StockRM();
                        stockRM = new StockRM();
                        stockRM.ID = Helper.CreateGuid("S");
                        stockRM.MaterialCode = vProductMaster.MaterialCode;
                        stockRM.MaterialName = vProductMaster.MaterialName;
                        stockRM.Code = putaway.NewStockCode;
                        stockRM.LotNumber = putaway.LotNo;
                        stockRM.InDate = putaway.InDate;
                        stockRM.ExpiredDate = putaway.NewExpDate.Value;
                        stockRM.Quantity = req.BagQty * putaway.QtyPerBag;
                        stockRM.QtyPerBag = putaway.QtyPerBag;
                        stockRM.BinRackID = binRack.ID;
                        stockRM.BinRackCode = binRack.Code;
                        stockRM.BinRackName = binRack.Name;
                        stockRM.ReceivedAt = DateTime.Now;

                        db.StockRMs.Add(stockRM);

                        StockRM StockRM = await db.StockRMs.Where(m => m.Code.Equals(putaway.StockCode) && m.Quantity.Equals(req.BagQty * putaway.QtyPerBag) && m.OnInspect).FirstOrDefaultAsync();
                        if (StockRM != null)
                        {
                            StockRM.Quantity = 0;
                        }
                    }
                    else
                    {
                        StockSFG stockSFG = new StockSFG();
                        stockSFG.ID = Helper.CreateGuid("S");
                        stockSFG.MaterialCode = vProductMaster.MaterialCode;
                        stockSFG.MaterialName = vProductMaster.MaterialName;
                        stockSFG.Code = putaway.NewStockCode;
                        stockSFG.LotNumber = putaway.LotNo;
                        stockSFG.InDate = putaway.InDate;
                        stockSFG.ExpiredDate = putaway.NewExpDate.Value;
                        stockSFG.Quantity = req.BagQty * putaway.QtyPerBag;
                        stockSFG.QtyPerBag = putaway.QtyPerBag;
                        stockSFG.BinRackID = binRack.ID;
                        stockSFG.BinRackCode = binRack.Code;
                        stockSFG.BinRackName = binRack.Name;
                        stockSFG.ReceivedAt = DateTime.Now;

                        db.StockSFGs.Add(stockSFG);

                        StockSFG StockSFG = await db.StockSFGs.Where(m => m.Code.Equals(putaway.StockCode) && m.Quantity.Equals(req.BagQty * putaway.QtyPerBag) && m.OnInspect).FirstOrDefaultAsync();
                        if (StockSFG != null)
                        {
                            StockSFG.Quantity = 0;
                        }
                    }

                    QCReturn _return = new QCReturn();
                    _return.ID = Helper.CreateGuid("QCr");
                    _return.QCPutawayID = putaway.ID;
                    _return.StockCode = putaway.StockCode;
                    _return.NewStockCode = putaway.NewStockCode;
                    _return.LotNo = putaway.LotNo;
                    _return.InDate = putaway.InDate;
                    _return.ExpDate = putaway.ExpDate;
                    _return.NewExpDate = putaway.NewExpDate.Value;
                    _return.PrevBinRackID = putaway.BinRackID;
                    _return.PrevBinRackCode = putaway.BinRackCode;
                    _return.PrevBinRackName = putaway.BinRackName;
                    _return.BinRackID = binRack.ID;
                    _return.BinRackCode = binRack.Code;
                    _return.BinRackName = binRack.Name;
                    _return.PutawayQty = req.BagQty * putaway.QtyPerBag;
                    _return.QtyPerBag = putaway.QtyPerBag;
                    _return.PutawayMethod = "SCAN";
                    _return.PutOn = DateTime.Now;
                    _return.PutBy = activeUser;

                    db.QCReturns.Add(_return);

                    putaway.PutawayMethod = "SCAN";
                    putaway.PutBy = activeUser;
                    putaway.PutOn = DateTime.Now;

                    QCExtend extend = await db.QCExtends.Where(s => s.StockCode.Equals(putaway.NewStockCode)).FirstOrDefaultAsync();
                    QCPutaway ptaway = await db.QCPutaways.Where(s => s.QCPicking.QCInspectionID.Equals(putaway.QCPicking.QCInspection.ID) && !s.PutawayMethod.Equals("SCAN")).FirstOrDefaultAsync();
                    if (ptaway == null)
                    {
                        extend.QCInspection.TransactionStatus = "CLOSED";
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway berhasil.";
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
        public async Task<IHttpActionResult> PutawayReturn(PutawayReturnReq req)
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
                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    QCReturn putaway = await db.QCReturns.Where(s => s.NewStockCode.Equals(StockCode)).FirstOrDefaultAsync();
                    if (putaway == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (putaway.PutawayMethod.Equals("SCAN"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

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

                    if (!putaway.NewStockCode.Equals(StockCode))
                    {
                        throw new Exception("Stock tidak ditemukan. Mohon cek kembali barcode yang discan.");
                    }

                    //check if old stock, dont allow stock movement
                    if (putaway.PutawayMethod.Equals("INSPECT"))
                    {                       
                        //new location, cannot same with old location
                        if (putaway.BinRackCode.Equals(req.BinRackCode))
                        {
                            throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                        }

                        if (req.ReturnBagQty <= 0)
                        {
                            throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                        }
                        else
                        {                            
                            int availableBagQty = Convert.ToInt32(Math.Floor(putaway.PutawayQty / putaway.QtyPerBag));
                            if (req.ReturnBagQty != availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty tidak sesuai, Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                            }
                        }
                    }

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        StockRM stockRM = new StockRM();
                        stockRM.ID = Helper.CreateGuid("S");
                        stockRM.MaterialCode = vProductMaster.MaterialCode;
                        stockRM.MaterialName = vProductMaster.MaterialName;
                        stockRM.Code = putaway.NewStockCode;
                        stockRM.LotNumber = putaway.LotNo;
                        stockRM.InDate = putaway.InDate;
                        stockRM.ExpiredDate = putaway.NewExpDate;
                        stockRM.Quantity = putaway.PutawayQty;
                        stockRM.QtyPerBag = putaway.QtyPerBag;
                        stockRM.BinRackID = binRack.ID;
                        stockRM.BinRackCode = binRack.Code;
                        stockRM.BinRackName = binRack.Name;
                        stockRM.ReceivedAt = DateTime.Now;

                        db.StockRMs.Add(stockRM);

                        //StockRM StockRM = await db.StockRMs.Where(m => m.Code.Equals(putaway.StockCode) && m.Quantity.Equals(req.BagQty * putaway.QtyPerBag) && m.OnInspect).FirstOrDefaultAsync();
                        //if (StockRM != null)
                        //{
                        //    StockRM.Quantity = 0;
                        //}
                    }
                    else
                    {
                        StockSFG stockSFG = new StockSFG();
                        stockSFG.ID = Helper.CreateGuid("S");
                        stockSFG.MaterialCode = vProductMaster.MaterialCode;
                        stockSFG.MaterialName = vProductMaster.MaterialName;
                        stockSFG.Code = putaway.NewStockCode;
                        stockSFG.LotNumber = putaway.LotNo;
                        stockSFG.InDate = putaway.InDate;
                        stockSFG.ExpiredDate = putaway.NewExpDate;
                        stockSFG.Quantity = putaway.PutawayQty;
                        stockSFG.QtyPerBag = putaway.QtyPerBag;
                        stockSFG.BinRackID = binRack.ID;
                        stockSFG.BinRackCode = binRack.Code;
                        stockSFG.BinRackName = binRack.Name;
                        stockSFG.ReceivedAt = DateTime.Now;

                        db.StockSFGs.Add(stockSFG);

                        //StockSFG StockSFG = await db.StockSFGs.Where(m => m.Code.Equals(putaway.StockCode) && m.Quantity.Equals(req.BagQty * putaway.QtyPerBag) && m.OnInspect).FirstOrDefaultAsync();
                        //if (StockSFG != null)
                        //{
                        //    StockSFG.Quantity = 0;
                        //}
                    }

                    putaway.PutawayMethod = "SCAN";
                    putaway.PutBy = activeUser;
                    putaway.PutOn = DateTime.Now;
                    putaway.QCPutaway.QCPicking.QCInspection.TransactionStatus = "CLOSED";

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway berhasil.";
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
        public async Task<IHttpActionResult> Print(PrintExtendPrintReq req)
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

                    //if (req.PrintQty <= 0)
                    //{
                    //    throw new Exception("Print Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    //}

                    QCInspection inspection = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(inspection.MaterialCode)).FirstOrDefault();
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

                    IQueryable<QCExtend> query = query = db.QCExtends.Where(m => m.QCInspectionID.Equals(inspection.ID)).AsQueryable();

                    IEnumerable<QCExtend> tempList = await query.ToListAsync();

                    List<string> bodies = new List<string>();

                    int len = 7;

                    if (material.MaterialCode.Length > 7)
                    {
                        len = material.MaterialCode.Length;
                    }

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    foreach (QCExtend qCExtend in tempList)
                    {
                        int startSeries = qCExtend.LastSeries;

                        //get last series
                        int seq = Convert.ToInt32(startSeries);
                        int fullBag = Convert.ToInt32(qCExtend.Qty / qCExtend.QtyPerBag);
                        int lastSeries = qCExtend.LastSeries + fullBag;
                        for (int i = 0; i < fullBag; i++)
                        {
                            string runningNumber = "";
                            runningNumber = string.Format("{0:D5}", seq++);

                            LabelDTO dto = new LabelDTO();
                            string qr1 = inspection.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(qCExtend.QtyPerBag).PadLeft(6) + " " + qCExtend.LotNo;
                            dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                            string inDate = "";
                            string inDate2 = "";
                            string inDate3 = "";
                            string expiredDate = "";
                            string expiredDate2 = "";

                            DateTime dt = qCExtend.InDate;
                            dto.Field4 = dt.ToString("MMMM").ToUpper();
                            inDate = dt.ToString("yyyyMMdd").Substring(1);
                            inDate2 = dt.ToString("yyyMMdd");
                            inDate2 = inDate2.Substring(1);
                            inDate3 = dt.ToString("yyyy-MM-dd");

                            DateTime dt2 = qCExtend.ExpDate;
                            expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                            expiredDate2 = dt2.ToString("yyyy-MM-dd");

                            string qr2 = inspection.MaterialCode.PadRight(len) + inDate + expiredDate;
                            dto.Field5 = qCExtend.LotNo;
                            dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                            dto.Field7 = Maker;
                            dto.Field8 = inspection.MaterialName;
                            dto.Field9 = Helper.FormatThousand(qCExtend.QtyPerBag);
                            dto.Field10 = "KG".ToUpper();
                            dto.Field11 = inDate2;
                            dto.Field12 = inspection.MaterialCode;
                            dto.Field13 = inDate3;
                            dto.Field14 = expiredDate2;
                            String body = RenderViewToString("Values", "~/Views/Receiving/Label.cshtml", dto);
                            bodies.Add(body);
                        }

                        //update log print rm here
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Print";
                        logPrintRM.StockCode = qCExtend.StockCode;
                        logPrintRM.MaterialCode = inspection.MaterialCode;
                        logPrintRM.MaterialName = inspection.MaterialName;
                        logPrintRM.LotNumber = qCExtend.LotNo;
                        logPrintRM.InDate = qCExtend.InDate;
                        logPrintRM.ExpiredDate = qCExtend.ExpDate;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);

                        LogReprint reprint = new LogReprint();
                        reprint.ID = Helper.CreateGuid("LOG");
                        reprint.StockCode = qCExtend.StockCode;
                        reprint.MaterialCode = inspection.MaterialCode;
                        reprint.MaterialName = inspection.MaterialName;
                        reprint.LotNumber = qCExtend.LotNo;
                        reprint.InDate = qCExtend.InDate;
                        reprint.ExpiredDate = qCExtend.ExpDate;
                        reprint.StartSeries = startSeries;
                        reprint.LastSeries = lastSeries;
                        reprint.PrintDate = DateTime.Now;
                        reprint.PrintedBy = activeUser;
                        reprint.PrintQty = req.PrintQty;

                        db.LogReprints.Add(reprint);

                        qCExtend.LastSeries = lastSeries;

                        await db.SaveChangesAsync();
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


        [HttpPost]
        public async Task<IHttpActionResult> PrintReturn(PrintReturnPrintReq req)
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

                    QCInspection inspection = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(inspection.MaterialCode)).FirstOrDefault();
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

                    IQueryable<QCReturn> query = query = db.QCReturns.Where(m => m.NewStockCode.Contains(inspection.MaterialCode) && m.LotNo.Equals(inspection.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(m.ExpDate) == DbFunctions.TruncateTime(inspection.ExpDate) && m.PutawayMethod.Equals("INSPECT")).AsQueryable();
                    IEnumerable<QCReturn> tempList = await query.ToListAsync();

                    List<string> bodies = new List<string>();

                    int len = 7;

                    if (material.MaterialCode.Length > 7)
                    {
                        len = material.MaterialCode.Length;
                    }

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    foreach (QCReturn qCReturn in tempList)
                    {
                        int startSeries = 0;
                        int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(qCReturn.NewStockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries;

                        //get last series
                        int seq = Convert.ToInt32(lastSeries);
                        int fullBag = Convert.ToInt32(qCReturn.PutawayQty / qCReturn.QtyPerBag);
                        for (int i = 0; i < fullBag; i++)
                        {
                            string runningNumber = "";
                            runningNumber = string.Format("{0:D5}", seq++);

                            LabelDTO dto = new LabelDTO();
                            string qr1 = inspection.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(qCReturn.QtyPerBag).PadLeft(6) + " " + qCReturn.LotNo;
                            dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                            string inDate = "";
                            string inDate2 = "";
                            string inDate3 = "";
                            string expiredDate = "";
                            string expiredDate2 = "";

                            DateTime dt = qCReturn.InDate;
                            dto.Field4 = dt.ToString("MMMM").ToUpper();
                            inDate = dt.ToString("yyyyMMdd").Substring(1);
                            inDate2 = dt.ToString("yyyMMdd");
                            inDate2 = inDate2.Substring(1);
                            inDate3 = dt.ToString("yyyy-MM-dd");

                            DateTime dt2 = qCReturn.ExpDate;
                            expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                            expiredDate2 = dt2.ToString("yyyy-MM-dd");

                            string qr2 = inspection.MaterialCode.PadRight(len) + inDate + expiredDate;
                            dto.Field5 = qCReturn.LotNo;
                            dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                            dto.Field7 = Maker;
                            dto.Field8 = inspection.MaterialName;
                            dto.Field9 = Helper.FormatThousand(qCReturn.QtyPerBag);
                            dto.Field10 = "KG".ToUpper();
                            dto.Field11 = inDate2;
                            dto.Field12 = inspection.MaterialCode;
                            dto.Field13 = inDate3;
                            dto.Field14 = expiredDate2;
                            String body = RenderViewToString("Values", "~/Views/Receiving/Label.cshtml", dto);
                            bodies.Add(body);
                        }


                        //update log print rm here
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Print";
                        logPrintRM.StockCode = qCReturn.NewStockCode;
                        logPrintRM.MaterialCode = inspection.MaterialCode;
                        logPrintRM.MaterialName = inspection.MaterialName;
                        logPrintRM.LotNumber = qCReturn.LotNo;
                        logPrintRM.InDate = qCReturn.InDate;
                        logPrintRM.ExpiredDate = qCReturn.ExpDate;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);

                        LogReprint reprint = new LogReprint();
                        reprint.ID = Helper.CreateGuid("LOG");
                        reprint.StockCode = qCReturn.NewStockCode;
                        reprint.MaterialCode = inspection.MaterialCode;
                        reprint.MaterialName = inspection.MaterialName;
                        reprint.LotNumber = qCReturn.LotNo;
                        reprint.InDate = qCReturn.InDate;
                        reprint.ExpiredDate = qCReturn.ExpDate;
                        reprint.StartSeries = startSeries;
                        reprint.LastSeries = lastSeries;
                        reprint.PrintDate = DateTime.Now;
                        reprint.PrintedBy = activeUser;
                        reprint.PrintQty = fullBag;

                        db.LogReprints.Add(reprint);
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
