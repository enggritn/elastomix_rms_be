using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity;
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
    public class MobileMovementController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [HttpPost]
        public async Task<IHttpActionResult> Picking(PickingMovementReq req)
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
                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    //dont trim materialcode
                    string LotNumber = "";
                    string QtyPerBag = "";
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

                    Movement movement = db.Movements.Where(m => m.StockCode.Equals(StockCode) && m.PrevBinRackCode.Equals(binRack.Code) && string.IsNullOrEmpty(m.NewBinRackCode) && m.CreatedBy.Equals(activeUser)).FirstOrDefault();
                    if (movement != null)
                    {
                        throw new Exception("Stock sudah diambil.");
                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BagQty > 0 && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    Movement mv = new Movement();
                    mv.ID = Helper.CreateGuid("M");
                    mv.StockCode = stockAll.Code;
                    mv.Code = stockAll.Code;
                    mv.LotNo = stockAll.LotNumber;
                    mv.InDate = stockAll.InDate.Value;
                    mv.ExpDate = stockAll.ExpiredDate.Value;
                    mv.MaterialCode = stockAll.MaterialCode;
                    mv.MaterialName = stockAll.MaterialName;
                    mv.PrevBinRackID = binRack.ID;
                    mv.PrevBinRackCode = binRack.Code;
                    mv.PrevBinRackName = binRack.Name;
                    mv.CreatedBy = activeUser;
                    mv.CreatedOn = DateTime.Now;
                    mv.TransactionStatus = string.Empty;

                    db.Movements.Add(mv);

                   
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
        public async Task<IHttpActionResult> Putaway(PutawayMovementReq req)
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

                    if (string.IsNullOrEmpty(req.MovementId))
                    {
                        throw new Exception("Id is required.");
                    }

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    //dont trim materialcode
                    string LotNumber = "";
                    string QtyPerBag = "";
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

                    Movement movement = db.Movements.Where(m => m.ID.Equals(req.MovementId)).FirstOrDefault();
                    if (movement == null)
                    {
                        throw new Exception("Data tidak ditemukan.");
                    }

                    if (movement.PrevBinRackCode.Equals(req.BinRackCode))
                    {
                        throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                    }

                    if (!string.IsNullOrEmpty(movement.NewBinRackCode))
                    {
                        throw new Exception("Pindah stok sudah selesai dilakukan.");
                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(movement.PrevBinRackCode)).FirstOrDefault();
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
                        int availableBagQty = Convert.ToInt32(stockAll.BagQty);
                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                        }
                    }

                    movement.Qty = req.BagQty;
                    movement.QtyPerBag = stockAll.QtyPerBag;
                    movement.NewBinRackID = binRack.ID;
                    movement.NewBinRackCode = binRack.Code;
                    movement.NewBinRackName = binRack.Name;
                    movement.ModifiedBy = activeUser;
                    movement.ModifiedOn = DateTime.Now;

                    if (stockAll.Type.Equals("RM"))
                    {
                        //update stock
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(movement.StockCode) && m.BinRackCode.Equals(movement.NewBinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = movement.MaterialCode;
                            stockRM.MaterialName = movement.MaterialName;
                            stockRM.Code = movement.StockCode;
                            stockRM.LotNumber = movement.LotNo;
                            stockRM.InDate = movement.InDate;
                            stockRM.ExpiredDate = movement.ExpDate;
                            stockRM.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockRM.QtyPerBag = stockAll.QtyPerBag;
                            stockRM.BinRackID = movement.NewBinRackID;
                            stockRM.BinRackCode = movement.NewBinRackCode;
                            stockRM.BinRackName = movement.NewBinRackName;
                            stockRM.ReceivedAt = DateTime.Now;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(movement.StockCode) && m.BinRackCode.Equals(movement.NewBinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = movement.MaterialCode;
                            stockSFG.MaterialName = movement.MaterialName;
                            stockSFG.Code = movement.StockCode;
                            stockSFG.LotNumber = movement.LotNo;
                            stockSFG.InDate = movement.InDate;
                            stockSFG.ExpiredDate = movement.ExpDate;
                            stockSFG.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockSFG.QtyPerBag = stockAll.QtyPerBag;
                            stockSFG.BinRackID = movement.NewBinRackID;
                            stockSFG.BinRackCode = movement.NewBinRackCode;
                            stockSFG.BinRackName = movement.NewBinRackName;
                            stockSFG.ReceivedAt = DateTime.Now;

                            db.StockSFGs.Add(stockSFG);
                        }
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
        public async Task<IHttpActionResult> GetList()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            IEnumerable<MovementListResp> list = Enumerable.Empty<MovementListResp>();

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
                    IQueryable<Movement> query = query = db.Movements.Where(s => string.IsNullOrEmpty(s.NewBinRackCode) && s.CreatedBy.Equals(activeUser)).AsQueryable();


                    IEnumerable<Movement> tempList = await query.ToListAsync();

                    list = from data in tempList
                           select new MovementListResp
                           {
                               MovementId = data.ID,
                               MaterialCode = data.MaterialCode,
                               MaterialName = data.MaterialName,
                               LotNo = data.LotNo,
                               InDate = Helper.NullDateToString(data.InDate),
                               ExpDate = Helper.NullDateToString(data.ExpDate),
                               BagQty = Helper.FormatThousand(Convert.ToInt32(data.Qty / data.QtyPerBag)),
                               TotalQty = Helper.FormatThousand(data.Qty),
                               QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                               BinRackCode = data.PrevBinRackCode,
                               BinRackName = data.PrevBinRackName,
                               PutawayAction = true
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

                    status = true;
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

            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }



    }
}
