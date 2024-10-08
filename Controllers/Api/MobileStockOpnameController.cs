using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class MobileStockOpnameController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();



        [HttpGet]
        public async Task<IHttpActionResult> GetListHeader()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<StockOpnameHeaderDTO> list = Enumerable.Empty<StockOpnameHeaderDTO>();



            try
            {
                IQueryable<StockOpnameHeader> query = query = db.StockOpnameHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();

                query = query.OrderByDescending(m => m.CreatedOn);

                IEnumerable<StockOpnameHeader> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new StockOpnameHeaderDTO
                       {
                           ID = data.ID,
                           Code = data.Code,
                           WarehouseCode = data.WarehouseCode,
                           WarehouseName = data.WarehouseName,
                           BinRackAreaID = data.BinRackAreaID,
                           BinRackAreaCode = data.BinRackAreaCode,
                           BinRackAreaName = data.BinRackAreaName,
                           Remarks = data.Remarks,
                           TransactionStatus = data.TransactionStatus,
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
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            StockOpnameHeaderDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null || header.TransactionStatus != "OPEN")
                {
                    throw new Exception("Data tidak ditemukan.");
                }

                dataDTO = new StockOpnameHeaderDTO
                {
                    ID = header.ID,
                    Code = header.Code,
                    WarehouseCode = header.WarehouseCode,
                    WarehouseName = header.WarehouseName,
                    BinRackAreaID = header.BinRackAreaID,
                    BinRackAreaCode = header.BinRackAreaCode,
                    BinRackAreaName = header.BinRackAreaName,
                    Remarks = header.Remarks,
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = header.CreatedOn.ToString(),
                    RemainingTask = "10 / 100" //sisa jumlah material yg sudah discan dan belum discan
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

            obj.Add("data", dataDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        //[HttpPost]
        //public async Task<IHttpActionResult> Scan(StockOpnameScanReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;
        //    string wareHouseName = string.Empty;
        //    string wareHouseCode = string.Empty;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }
        //        //token = "$2a$12$vzhDjE1NRoYG/QVHOrAMFeICRjpchEsWjU5m4al2jsLE2VWotG77y";

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            if (string.IsNullOrEmpty(req.HeaderId))
        //            {
        //                throw new Exception("Id is required.");
        //            }

        //            StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(req.HeaderId)).FirstOrDefaultAsync();

        //            if (header == null || header.TransactionStatus != "OPEN")
        //            {
        //                throw new Exception("Data tidak ditemukan.");
        //            }

        //            if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
        //            {
        //                throw new Exception("Barcode Left & Barcode Right harus diisi.");
        //            }

        //            string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
        //            string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
        //            string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
        //            string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
        //            string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

        //            string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

        //            BinRack binRack = null;
        //            if (string.IsNullOrEmpty(req.BinRackCode))
        //            {
        //                throw new Exception("BinRack harus diisi.");

        //            }
        //            else
        //            {
        //                binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
        //                if (binRack == null)
        //                {
        //                    throw new Exception("BinRack tidak ditemukan.");
        //                }

        //                string WarehouseCode = header.WarehouseCode;
        //                if (!binRack.WarehouseCode.Equals(WarehouseCode))
        //                {
        //                    if (header.WarehouseCode == "ALL" && binRack.Warehouse.Type.Equals("EMIX"))
        //                    {
        //                        goto checkedWarehouse;
        //                    }
        //                    throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse yang dipilih.");
        //                }

        //            }
        //        checkedWarehouse:
        //            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
        //            //vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode)).FirstOrDefault();
        //            //if (stockAll == null)
        //            //{
        //            //    throw new Exception("Stock tidak ditemukan.");
        //            //}

        //            if (req.BagQty <= 0)
        //            {
        //                throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
        //            }

        //            DateTime expiredDate = DateTime.ParseExact(ExpiredDate, "yyMMdd", CultureInfo.InvariantCulture);
        //            DateTime inDate = DateTime.ParseExact(InDate.Substring(1), "yyMMdd", CultureInfo.InvariantCulture);

        //            StockOpnameDetail detail = header.StockOpnameDetails.Where(m => m.MaterialCode.Equals(MaterialCode) && m.LotNo.Equals(LotNumber) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
        //            if (detail == null)
        //            {
        //                detail = new StockOpnameDetail
        //                {
        //                    ID = Helper.CreateGuid("STOd"),
        //                    HeaderID = header.ID,
        //                    MaterialCode = MaterialCode,
        //                    MaterialName = vProductMaster.MaterialName,
        //                    MaterialType = vProductMaster.ProdType,
        //                    LotNo = LotNumber,
        //                    InDate = inDate,
        //                    ExpDate = expiredDate,
        //                    BagQty = req.BagQty,
        //                    QtyPerBag = Convert.ToDecimal(QtyPerBag),
        //                    BinRackCode = binRack.Code,
        //                    BinRackName = binRack.Name,
        //                    ActualBagQty = req.BagQty,
        //                };

        //                db.StockOpnameDetails.Add(detail);
        //            }
        //            else
        //            {
        //                detail.BagQty = req.BagQty;
        //                detail.ActualBagQty = req.BagQty;
        //            }


        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Scan berhasil.";
        //        }
        //        else
        //        {
        //            message = "Token sudah berakhir, silahkan login kembali.";
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

        //    return Ok(obj);
        //}


        [HttpGet]
        public async Task<IHttpActionResult> GetList(string id, string MaterialName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<StockOpnameDetailDTO> list = Enumerable.Empty<StockOpnameDetailDTO>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null || header.TransactionStatus != "OPEN")
                {
                    throw new Exception("Data tidak ditemukan.");
                }

                IQueryable<StockOpnameDetail> query = query = db.StockOpnameDetails.Where(s => s.HeaderID.Equals(header.ID)).AsQueryable();

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                }



                IEnumerable<StockOpnameDetail> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new StockOpnameDetailDTO
                       {
                           ID = data.ID,
                           HeaderID = data.HeaderID,
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName,
                           MaterialType = data.MaterialType,
                           LotNo = data.LotNo,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpDate),
                           //TotalQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                           //ScannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                           //UnscannedQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                           TotalBagQty = Helper.FormatThousand(data.BagQty),
                           ScannedBagQty = Helper.FormatThousand(data.ActualBagQty),
                           UnscannedBagQty = Helper.FormatThousand(data.BagQty - data.ActualBagQty),
                           BinRackCode = data.BinRackCode,
                           BinRackName = data.BinRackName,
                           IsScanned = data.ActualBagQty >= data.BagQty
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

        [HttpPost]
        public async Task<IHttpActionResult> Scan(StockOpnameScanReq req)
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

                //token = "$2a$12$vzhDjE1NRoYG/QVHOrAMFeICRjpchEsWjU5m4al2jsLE2VWotG77y";

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(req.HeaderId))
                    {
                        throw new Exception("Id is required.");
                    }

                    StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(req.HeaderId)).FirstOrDefaultAsync();

                    if (header == null || header.TransactionStatus != "OPEN")
                    {
                        throw new Exception("Data tidak ditemukan.");
                    }

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    //dont trim materialcode
                    string QtyPerBag = "";
                    string LotNumber = "";
                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    RawMaterial cekQtyPerBag = await db.RawMaterials.Where(s => s.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (vProductMaster.ProdType == "SFG")
                    {
                        if (req.BarcodeRight.Length == 29)
                        {
                            QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 8).Trim();
                            LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 16);
                        }
                        else
                        {
                            QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                            LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                        }
                    }
                    else
                    {
                        if (cekQtyPerBag.Qty >= 1000)
                        {
                            QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 8).Trim();
                            LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 16);
                        }
                        else
                        {
                            QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                            LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                        }
                    }
                                        
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string QtyPerBag2 = QtyPerBag.Replace(',', '.');
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

                        if (header.WarehouseCode.Equals("ALL"))
                        {
                            string[] warehouseCodes = { };
                            if (!header.WarehouseCode.Equals("ALL"))
                            {
                                warehouseCodes = new string[1] { header.WarehouseCode};
                            }
                            else
                            {
                                warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
                            }

                            if (!warehouseCodes.Contains(binRack.WarehouseCode))
                            {
                                throw new Exception("Bin Rack tidak dikenali pada warehouse yang dipilih.");
                            }
                        }                  
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }

                    //validasi check stock dan lokasi

                    DateTime inDate = DateTime.ParseExact(InDate.Substring(1), "yyMMdd", CultureInfo.InvariantCulture);
                    DateTime expiredDate = DateTime.ParseExact(ExpiredDate, "yyMMdd", CultureInfo.InvariantCulture);

                    //tidak perlu validasi check stock
                    StockOpnameDetail detail = header.StockOpnameDetails.Where(m => m.StockCode.Equals(StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    if (detail == null)
                    {
                        detail = new StockOpnameDetail
                        {
                            ID = Helper.CreateGuid("STOd"),
                            HeaderID = header.ID,
                            StockCode = StockCode,
                            MaterialCode = MaterialCode,
                            MaterialName = vProductMaster.MaterialName,
                            MaterialType = vProductMaster.ProdType,
                            LotNo = LotNumber,
                            InDate = inDate,
                            ExpDate = expiredDate,
                            BagQty = req.BagQty,
                            QtyPerBag = Convert.ToDecimal(QtyPerBag2),
                            BinRackCode = binRack.Code,
                            BinRackName = binRack.Name,
                            ActualBagQty = req.BagQty,
                        };

                        db.StockOpnameDetails.Add(detail);
                        await db.SaveChangesAsync();

                        StockOpnameDetail detailcek = header.StockOpnameDetails.Where(m => m.StockCode.Equals(StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();

                        StockOpnameItem stockOpnameItem = new StockOpnameItem();
                        stockOpnameItem.ID = Helper.CreateGuid("STOi");
                        stockOpnameItem.DetailID = detailcek.ID;
                        stockOpnameItem.BagQty = req.BagQty;
                        if (req.BagQty == 1 && Convert.ToDecimal(QtyPerBag) > vProductMaster.QtyPerBag)
                        {
                            stockOpnameItem.QtyPerBag = Convert.ToDecimal(QtyPerBag2);
                        }
                        else
                        {
                            stockOpnameItem.QtyPerBag = detailcek.QtyPerBag;
                        }
                        stockOpnameItem.ScannedOn = DateTime.Now;
                        stockOpnameItem.ScannedBy = activeUser;

                        db.StockOpnameItems.Add(stockOpnameItem);
                    }
                    else
                    {
                        StockOpnameItem items = await db.StockOpnameItems.Where(m => m.DetailID.Equals(detail.ID) && m.BagQty.Equals(req.BagQty)).FirstOrDefaultAsync();
                        if (items == null)
                        {
                            if (detail.ActualBagQty > 0)
                            {
                                detail.ActualBagQty = detail.ActualBagQty + req.BagQty;
                            }
                            else
                            {
                                detail.ActualBagQty = req.BagQty;
                            }

                            StockOpnameItem stockOpnameItem = new StockOpnameItem();
                            stockOpnameItem.ID = Helper.CreateGuid("STOi");
                            stockOpnameItem.DetailID = detail.ID;
                            stockOpnameItem.BagQty = req.BagQty;
                            if (req.BagQty == 1 && Convert.ToDecimal(QtyPerBag) > vProductMaster.QtyPerBag)
                            {
                                stockOpnameItem.QtyPerBag = Convert.ToDecimal(QtyPerBag2);
                            }
                            else
                            {
                                stockOpnameItem.QtyPerBag = detail.QtyPerBag;
                            }
                            stockOpnameItem.ScannedOn = DateTime.Now;
                            stockOpnameItem.ScannedBy = activeUser;

                            db.StockOpnameItems.Add(stockOpnameItem);
                        }
                        else
                        {
                            items.ScannedOn = DateTime.Now;
                            items.ScannedBy = activeUser;
                        }
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Scan berhasil.";
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
        public async Task<IHttpActionResult> GetListDetail(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            StockOpnameDetailDTO dataDTO = null;
            IEnumerable<StockOpnameItemDTO> list = Enumerable.Empty<StockOpnameItemDTO>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                StockOpnameDetail header = await db.StockOpnameDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data tidak ditemukan.");
                }

                dataDTO = new StockOpnameDetailDTO()
                {
                    ID = header.ID,
                    HeaderID = header.HeaderID,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    MaterialType = header.MaterialType,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TotalBagQty = Helper.FormatThousand(header.BagQty),
                    ScannedBagQty = Helper.FormatThousand(header.ActualBagQty),
                    UnscannedBagQty = Helper.FormatThousand(header.BagQty - header.ActualBagQty),
                    BinRackCode = header.BinRackCode,
                    BinRackName = header.BinRackName,
                    IsScanned = header.ActualBagQty >= header.BagQty
                };

                IQueryable<StockOpnameItem> query = query = db.StockOpnameItems.Where(s => s.DetailID.Equals(header.ID)).AsQueryable();





                IEnumerable<StockOpnameItem> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new StockOpnameItemDTO
                       {
                           ID = data.ID,
                           DetailID = data.DetailID,
                           BagQty = Helper.FormatThousand(data.BagQty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           TotalQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
                           ScannedBy = data.ScannedBy,
                           ScannedOn = Helper.NullDateTimeToString(data.ScannedOn)
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

            obj.Add("data", dataDTO);
            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        //[HttpGet]
        //public async Task<IHttpActionResult> GetScannedList(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    IEnumerable<StockOpnameDetailDTO> list = Enumerable.Empty<StockOpnameDetailDTO>();

        //    try
        //    {
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        StockOpnameHeader header = await db.StockOpnameHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

        //        if (header == null || header.TransactionStatus != "OPEN")
        //        {
        //            throw new Exception("Data tidak ditemukan.");
        //        }

        //        IQueryable<StockOpnameDetail> query = query = db.StockOpnameDetails.Where(s => s.HeaderID.Equals(header.ID)).AsQueryable();



        //        IEnumerable<StockOpnameDetail> tempList = await query.ToListAsync();

        //        list = from data in tempList
        //               select new StockOpnameDetailDTO
        //               {
        //                   ID = data.ID,
        //                   HeaderID = data.HeaderID,
        //                   StockCode = data.StockCode,
        //                   MaterialCode = data.MaterialCode,
        //                   MaterialName = data.MaterialName,
        //                   MaterialType = data.MaterialType,
        //                   LotNo = data.LotNo,
        //                   InDate = Helper.NullDateToString(data.InDate),
        //                   ExpDate = Helper.NullDateToString(data.ExpDate),
        //                   BagQty = Helper.FormatThousand(data.BagQty),
        //                   QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
        //                   TotalQty = Helper.FormatThousand(data.BagQty * data.QtyPerBag),
        //                   BinRackID = data.BinRackID,
        //                   BinRackCode = data.BinRackCode,
        //                   BinRackName = data.BinRackName,
        //                   ActualBagQty = Helper.FormatThousand(data.ActualBagQty),
        //                   ActualBinRackID = data.ActualBinRackID,
        //                   ActualBinRackCode = data.ActualBinRackCode,
        //                   ActualBinRackName = data.ActualBinRackName,
        //                   ScannedBy = data.ScannedBy,
        //                   ScannedOn = Helper.NullDateTimeToString(data.ScannedOn)
        //               };

        //        if (list.Count() > 0)
        //        {
        //            status = true;
        //            message = "Fetch data succeded.";
        //        }
        //        else
        //        {
        //            message = "Tidak ada data.";
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

        //    obj.Add("list", list);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}





    }
}
