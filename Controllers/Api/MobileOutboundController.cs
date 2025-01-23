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
    public class MobileOutboundController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [HttpPost]
        public async Task<IHttpActionResult> Create(OutboundHeaderReq dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            OutboundHeaderDTO data = new OutboundHeaderDTO();

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
                    if (string.IsNullOrEmpty(dataVM.WarehouseCode))
                    {
                        throw new Exception("Warehouse harus diisi.");
                    }
                    else
                    {
                        var temp = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();

                        if (temp == null)
                        {
                            throw new Exception("Warehouse tidak ditemukan.");
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.Remarks))
                    {
                        throw new Exception("Remarks harus diisi.");
                    }

                    

                    var CreatedAt = DateTime.Now;
                    var TransactionId = Helper.CreateGuid("OUT");

                    string prefix = TransactionId.Substring(0, 3);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.OutboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
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

                    OutboundHeader header = new OutboundHeader
                    {
                        ID = TransactionId,
                        Code = Code,
                        Remarks = dataVM.Remarks,
                        TransactionStatus = "OPEN",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                    };

                    Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(dataVM.WarehouseCode)).FirstOrDefaultAsync();
                    header.WarehouseCode = wh.Code;
                    header.WarehouseName = wh.Name;

                    db.OutboundHeaders.Add(header);

                    await db.SaveChangesAsync();

                    data = new OutboundHeaderDTO
                    {
                        ID = header.ID,
                        Code = header.Code,
                        Remarks = header.Remarks,
                        WarehouseCode = header.WarehouseCode,
                        WarehouseName = header.WarehouseName,
                        TransactionStatus = header.TransactionStatus,
                        CreatedBy = header.CreatedBy,
                        CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                        SaveAction = header.TransactionStatus.Equals("OPEN"),
                        CancelAction = header.TransactionStatus.Equals("OPEN"),
                        ConfirmAction = header.TransactionStatus.Equals("OPEN"),
                        AddOrderAction = header.TransactionStatus.Equals("OPEN"),
                        RemoveOrderAction = header.TransactionStatus.Equals("OPEN")
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

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Update(OutboundHeaderReq dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            OutboundHeaderDTO data = new OutboundHeaderDTO();

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
                    if (string.IsNullOrEmpty(dataVM.ID))
                    {
                        throw new Exception("Outbound ID is required.");
                    }

                    OutboundHeader header = await db.OutboundHeaders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
                    if (header == null)
                    {
                        throw new Exception("Data tidak ditemukan.");
                    }

                    if (!header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Ubah data tidak dapat dilakukan.");
                    }

                    if (string.IsNullOrEmpty(dataVM.Remarks))
                    {
                        throw new Exception("Remarks harus diisi.");
                    }


                    header.ModifiedBy = activeUser;
                    header.ModifiedOn = DateTime.Now;
                    header.Remarks = dataVM.Remarks;

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Ubah data berhasil.";

                    data = new OutboundHeaderDTO
                    {
                        ID = header.ID,
                        Code = header.Code,
                        Remarks = header.Remarks,
                        WarehouseCode = header.WarehouseCode,
                        WarehouseName = header.WarehouseName,
                        TransactionStatus = header.TransactionStatus,
                        CreatedBy = header.CreatedBy,
                        CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                        SaveAction = header.TransactionStatus.Equals("OPEN"),
                        CancelAction = header.TransactionStatus.Equals("OPEN"),
                        ConfirmAction = header.TransactionStatus.Equals("OPEN"),
                        AddOrderAction = header.TransactionStatus.Equals("OPEN"),
                        RemoveOrderAction = header.TransactionStatus.Equals("OPEN")
                    };

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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> GetList()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<OutboundHeaderDTO> list = Enumerable.Empty<OutboundHeaderDTO>();


            try
            {
                IQueryable<OutboundHeader> query = query = db.OutboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED") && !s.TransactionStatus.Equals("CLOSED")).AsQueryable();
                query = query.OrderByDescending(m => m.CreatedOn);

                IEnumerable<OutboundHeader> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new OutboundHeaderDTO
                       {
                           ID = data.ID,
                           Code = data.Code,
                           Remarks = data.Remarks,
                           WarehouseCode = data.WarehouseCode,
                           WarehouseName = data.WarehouseName,
                           CreatedBy = data.CreatedBy,
                           TransactionStatus = data.TransactionStatus,
                           CreatedOn = Helper.NullDateTimeToString(data.CreatedOn),
                           ModifiedBy = data.ModifiedBy ?? "",
                           ModifiedOn = Helper.NullDateTimeToString(data.ModifiedOn),
                           SaveAction = data.TransactionStatus.Equals("OPEN"),
                           CancelAction = data.TransactionStatus.Equals("OPEN"),
                           ConfirmAction = data.TransactionStatus.Equals("OPEN"),
                           AddOrderAction = data.TransactionStatus.Equals("OPEN"),
                           RemoveOrderAction = data.TransactionStatus.Equals("OPEN")
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
        public async Task<IHttpActionResult> GetProductList(string id, string MaterialName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<OutboundOrderResp> list = Enumerable.Empty<OutboundOrderResp>();

            try
            {
                OutboundHeader header = await db.OutboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data tidak dikenali.");
                }

                IQueryable<vStockProduct> query = db.vStockProducts.Where(m => m.WarehouseCode.Equals(header.WarehouseCode) && m.TotalQty > 0).AsQueryable();

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                    list = from detail in await query.ToListAsync()
                           select new OutboundOrderResp
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

        [HttpGet]
        public async Task<IHttpActionResult> GetOrderById(string id, string MaterialName)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<OutboundOrderDTO2> list = Enumerable.Empty<OutboundOrderDTO2>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                OutboundHeader header = await db.OutboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                IQueryable<OutboundOrder> query = db.OutboundOrders.Where(s => s.OutboundID.Equals(header.ID)).AsQueryable();

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                }

                //query = query.Where(s => s.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag) - s.TotalQty <= 0);
               
                list = from detail in await query.OrderBy(m => m.CreatedOn).ToListAsync()
                       select new OutboundOrderDTO2
                       {
                           ID = detail.ID,
                           MaterialCode = detail.MaterialCode,
                           MaterialName = detail.MaterialName,
                           MaterialType = detail.MaterialType,
                           TotalQty = Helper.FormatThousand(detail.TotalQty),
                           PickedQty = Helper.FormatThousand(detail.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag)),
                           DiffQty = Helper.FormatThousand(detail.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - detail.TotalQty),
                           OutstandingQty = Helper.FormatThousand(detail.TotalQty - (detail.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag))),
                           OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((detail.TotalQty - (detail.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / detail.QtyPerBag))),
                           CreatedBy = detail.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(detail.CreatedOn),
                           PickingAction = header.TransactionStatus.Equals("CONFIRMED") && detail.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag) - detail.TotalQty < 0,
                           ReturnAction = header.TransactionStatus.Equals("CONFIRMED") && detail.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag) - detail.TotalQty > 0
                       };


                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Tidak ada data.";

                    //if (header.TransactionStatus.Equals("CONFIRMED"))
                    //{
                    //    header.TransactionStatus = "CLOSED";
                    //    await db.SaveChangesAsync();
                    //}
                    
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
        public async Task<IHttpActionResult> CreateOrder(OutboundOrderReq dataVM)
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

                    OutboundHeader header = null;
                    vStockProduct stockProduct = null;
                    if (string.IsNullOrEmpty(dataVM.HeaderID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        header = await db.OutboundHeaders.Where(s => s.ID.Equals(dataVM.HeaderID)).FirstOrDefaultAsync();

                        if (header == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }
                        else
                        {
                            if (!header.TransactionStatus.Equals("OPEN"))
                            {
                                throw new Exception("Tidak dapat menambahkan order.");
                            }
                        }
                    }



                    if (string.IsNullOrEmpty(dataVM.MaterialCode))
                    {
                        //ModelState.AddModelError("Outbound.MaterialCode", "Material Code is required.");
                        throw new Exception("Material Code wajib diisi.");
                    }
                    else
                    {
                        stockProduct = await db.vStockProducts.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode) && m.WarehouseCode.Equals(header.WarehouseCode)).FirstOrDefaultAsync();
                        if (stockProduct == null)
                        {
                            //ModelState.AddModelError("Outbound.MaterialCode", "Material is not recognized.");
                            throw new Exception("Material tidak dikenali.");
                        }
                        else
                        {
                            OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.OutboundID.Equals(dataVM.HeaderID) && m.MaterialCode.Equals(dataVM.MaterialCode)).FirstOrDefaultAsync();
                            if (outboundOrder != null)
                            {
                                throw new Exception("Material sudah ada di dalam order.");
                            }
                        }

                    }

                    if (dataVM.RequestQty <= 0)
                    {
                        throw new Exception("Request Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        //stock validation, check available qty
                        decimal? availableStock = 0;

                        availableStock = stockProduct.TotalQty;

                        if (dataVM.RequestQty > availableStock)
                        {
                            throw new Exception(string.Format("Request Qty melewati jumlah tersedia. Quantity tersedia : {0}", Helper.FormatThousand(availableStock)));
                        }
                    }

                    vProductMaster productMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(stockProduct.MaterialCode)).FirstOrDefaultAsync();

                   
                    OutboundOrder order = new OutboundOrder()
                    {
                        ID = Helper.CreateGuid("Oo"),
                        OutboundID = dataVM.HeaderID,
                        MaterialCode = stockProduct.MaterialCode,
                        MaterialName = stockProduct.MaterialName,
                        MaterialType = stockProduct.ProdType,
                        TotalQty = dataVM.RequestQty,
                        QtyPerBag = productMaster.QtyPerBag,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };


                    header.OutboundOrders.Add(order);



                    await db.SaveChangesAsync();

                    status = true;
                    message = "Add order berhasil.";
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
        public async Task<IHttpActionResult> RemoveOrder(string OrderId)
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
                    if (string.IsNullOrEmpty(OrderId))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefaultAsync();
                        if (outboundOrder == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }

                        if (!outboundOrder.OutboundHeader.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Edit data is not allowed.");
                        }

                        db.OutboundOrders.Remove(outboundOrder);

                        await db.SaveChangesAsync();

                        status = true;
                        message = "Remove order berhasil.";
                    }

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


        public async Task<IHttpActionResult> UpdateStatus(string id, string transactionStatus)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            OutboundHeaderDTO data = new OutboundHeaderDTO();

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
                    OutboundHeader header = await db.OutboundHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                    if (header.TransactionStatus.Equals("CANCELLED"))
                    {
                        throw new Exception("Transaksi sudah di-cancel.");
                    }

                    if (transactionStatus.Equals("CANCELLED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaksi tidak dapat di-cancel.");
                    }

                    if (transactionStatus.Equals("CONFIRMED") && !header.TransactionStatus.Equals("OPEN"))
                    {
                        throw new Exception("Transaksi tidak dapat di-confirm.");
                    }

                    if (transactionStatus.Equals("CLOSED") && !header.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Transaksi tidak dapat di-close.");
                    }

                    header.TransactionStatus = transactionStatus;
                    header.ModifiedBy = activeUser;
                    header.ModifiedOn = DateTime.Now;

                    if (transactionStatus.Equals("CANCELLED"))
                    {
                        db.OutboundOrders.RemoveRange(header.OutboundOrders);

                        message = "Cancel data berhasil.";
                    }

                    if (transactionStatus.Equals("CONFIRMED"))
                    {
                        //check detail
                        if (header.OutboundOrders.Count() < 1)
                        {
                            throw new Exception("Order tidak boleh kosong.");
                        }


                        //automated logic check
                        //warehouse type = outsource will auto generate picking -> auto picked

                        message = "Confirm data berhasil.";
                    }

                    if (transactionStatus.Equals("CLOSED"))
                    {
                        message = "Close data berhasil.";
                    }

                    await db.SaveChangesAsync();
                    status = true;

                    data = new OutboundHeaderDTO
                    {
                        ID = header.ID,
                        Code = header.Code,
                        Remarks = header.Remarks,
                        WarehouseCode = header.WarehouseCode,
                        WarehouseName = header.WarehouseName,
                        TransactionStatus = header.TransactionStatus,
                        CreatedBy = header.CreatedBy,
                        CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                        SaveAction = header.TransactionStatus.Equals("OPEN"),
                        CancelAction = header.TransactionStatus.Equals("OPEN"),
                        ConfirmAction = header.TransactionStatus.Equals("OPEN"),
                        AddOrderAction = header.TransactionStatus.Equals("OPEN"),
                        RemoveOrderAction = header.TransactionStatus.Equals("OPEN")
                    };
                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);
            return Ok(obj);
        }

        public async Task<IHttpActionResult> Cancel(string id)
        {
            return await UpdateStatus(id, "CANCELLED");
        }

        public async Task<IHttpActionResult> Confirm(string id)
        {
            return await UpdateStatus(id, "CONFIRMED");
        }

        public async Task<IHttpActionResult> Close(string id)
        {
            return await UpdateStatus(id, "CLOSED");
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetPickingList(string OrderId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            OutboundOrder order = db.OutboundOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();

            if (order == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }
            //string warehouseCode = request["warehouseCode"].ToString();
            //string areaCode = request["areaCode"].ToString();

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.WarehouseCode.Equals(order.OutboundHeader.WarehouseCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();

            int totalRow = query.Count();


            decimal requestedQty = order.TotalQty;
            decimal pickedQty = order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag);
            decimal availableQty = requestedQty - pickedQty;

            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;

            string OutstandingQty = "0";
            string PickingBagQty = "0";

            try
            {
                //check outstanding, if outstanding already finished dont get fifo
                decimal osQty = order.OutboundPickings.Sum(m => m.BagQty * m.QtyPerBag) - order.TotalQty;


                if(osQty < 0)
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
                                    IsExpired = DateTime.Now.Date >= stock.ExpiredDate.Value.Date.Date,
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
                }


                if (data.Count() < 1)
                {
                    message = "Tidak ada stock tersedia.";
                }

                status = true;

                order = await db.OutboundOrders.Where(s => s.ID.Equals(OrderId)).FirstOrDefaultAsync();

                OutstandingQty = Helper.FormatThousand(order.TotalQty - (order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.TotalQty - (order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag)));

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

            obj.Add("outstanding_qty", OutstandingQty);
            obj.Add("picking_bag_qty", PickingBagQty);
            obj.Add("material_type", MaterialType);
            obj.Add("list", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetPickingDetail(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            OutboundPickingDTO dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                vOutboundPickingSummary outboundPicking = await db.vOutboundPickingSummaries.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (outboundPicking == null)
                {
                    throw new Exception("Data not found.");
                }

                dataDTO = new OutboundPickingDTO
                {
                    OrderID = outboundPicking.ID,
                    StockCode = outboundPicking.StockCode,
                    MaterialCode = outboundPicking.MaterialCode,
                    MaterialName = outboundPicking.MaterialName,
                    QtyPerBag = Helper.FormatThousand(outboundPicking.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(outboundPicking.TotalQty / outboundPicking.QtyPerBag)),
                    LotNo = outboundPicking.LotNo,
                    InDate = Helper.NullDateToString(outboundPicking.InDate),
                    ExpDate = Helper.NullDateToString(outboundPicking.ExpDate),
                    TotalQty = Helper.FormatThousand(outboundPicking.TotalQty),
                    ReturnedTotalQty = Helper.FormatThousand(outboundPicking.ReturnQty),
                    AvailableReturnQty = Helper.FormatThousand(outboundPicking.TotalQty - outboundPicking.ReturnQty)
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

        [HttpGet]
        public async Task<IHttpActionResult> GetReturnList(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            OutboundReturnList dataDTO = null;
            IEnumerable<OutboundReturnResp> list = Enumerable.Empty<OutboundReturnResp>();

            string OutstandingQty = "0";
            string PickingBagQty = "0";
            string ReturnQty = "0";

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                vOutboundReturnSummary outboundReturn = await db.vOutboundReturnSummaries.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                //if (outboundReturn == null)
                //{
                //    throw new Exception("Data not found.");
                //}

                OutboundOrder outboundOrder = await db.OutboundOrders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                OutboundHeader outboundHeader = await db.OutboundHeaders.Where(m => m.ID.Equals(outboundOrder.OutboundID)).FirstOrDefaultAsync();

                vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(outboundOrder.MaterialCode)).FirstOrDefaultAsync();

                string MaterialType = vProductMaster.ProdType;

                dataDTO = new OutboundReturnList
                {
                    ID = outboundOrder.OutboundID,
                    OrderID = outboundOrder.ID,
                    MaterialCode = outboundOrder.MaterialCode,
                    MaterialName = outboundOrder.MaterialName,
                    MaterialType = MaterialType,
                    WarehouseCode = outboundOrder.OutboundHeader.WarehouseCode,
                    WarehouseName = outboundOrder.OutboundHeader.WarehouseName,
                    QtyPerBag = Helper.FormatThousand(outboundOrder.QtyPerBag),
                    TotalQty = Helper.FormatThousand(outboundOrder.TotalQty),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(outboundOrder.TotalQty / outboundOrder.QtyPerBag))
            };

                IQueryable<OutboundReturn> query = query = db.OutboundReturns.Where(m => m.OutboundOrderID.Equals(id)).AsQueryable();
                IEnumerable<OutboundReturn> tempList = await query.OrderByDescending(m => m.ReturnedOn).ToListAsync();

                list = from data in tempList
                       select new OutboundReturnResp
                       {
                           ID = data.ID,
                           OutboundOrderID = data.OutboundOrderID,
                           OrderId = outboundOrder.ID,
                           StockCode = data.StockCode,
                           MaterialCode = outboundOrder.MaterialCode,
                           MaterialName = outboundOrder.MaterialName,
                           LotNo = data.LotNo,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpDate),
                           BagQty = Helper.FormatThousand(Convert.ToInt32(data.ReturnQty / data.QtyPerBag)),
                           ReturnQty = Helper.FormatThousand(data.ReturnQty),
                           Remarks = data.Remarks,
                           LastSeries = data.LastSeries,
                           PrintBarcodeAction = outboundHeader.TransactionStatus.Equals("CONFIRMED") && string.IsNullOrEmpty(data.Remarks),
                           PutawayAction = outboundHeader.TransactionStatus.Equals("CONFIRMED") && string.IsNullOrEmpty(data.Remarks)
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

                ReturnQty = Helper.FormatThousand(outboundOrder.OutboundPutaways.Sum(i => i.PutawayQty));
                OutstandingQty = Helper.FormatThousand(outboundOrder.TotalQty - (outboundOrder.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((outboundOrder.TotalQty - (outboundOrder.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / outboundOrder.QtyPerBag)));

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

            obj.Add("outstanding_qty", OutstandingQty);
            obj.Add("picking_bag_qty", PickingBagQty);
            obj.Add("return_qty", ReturnQty);
            obj.Add("data", dataDTO);
            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Return(OutboundReturnRes dataVM)
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
                    vOutboundPickingSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    string LotNumber = "";
                    string QtyPerBag = "";
                    string MaterialCode = dataVM.BarcodeLeft.Substring(0, dataVM.BarcodeLeft.Length - 13);
                    RawMaterial cekQtyPerBag = await db.RawMaterials.Where(s => s.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (vProductMaster.ProdType == "SFG")
                    {
                        if (dataVM.BarcodeRight.Length == 29)
                        {
                            QtyPerBag = dataVM.BarcodeRight.Substring(MaterialCode.Length + 7, 8).Trim();
                            LotNumber = dataVM.BarcodeRight.Substring(MaterialCode.Length + 16);
                        }
                        else
                        {
                            QtyPerBag = dataVM.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                            LotNumber = dataVM.BarcodeRight.Substring(MaterialCode.Length + 14);
                        }
                    }
                    else
                    {
                        if (cekQtyPerBag.Qty >= 1000)
                        {
                            QtyPerBag = dataVM.BarcodeRight.Substring(MaterialCode.Length + 7, 8).Trim();
                            LotNumber = dataVM.BarcodeRight.Substring(MaterialCode.Length + 16);
                        }
                        else
                        {
                            QtyPerBag = dataVM.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                            LotNumber = dataVM.BarcodeRight.Substring(MaterialCode.Length + 14);
                        }
                    }
                    string InDate = dataVM.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpDate = dataVM.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpDate);

                    summary = await db.vOutboundPickingSummaries.Where(s => s.ID.Equals(dataVM.OrderID) && s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }                                   

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();
                    
                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (!order.OutboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Return sudah tidak dapat dilakukan lagi karena transaksi sudah ditutup.");
                    }

                    if (dataVM.Qty <= 0)
                    {
                        throw new Exception("Total Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.ReturnQty.Value;

                        if (dataVM.Qty > availableQty)
                        {
                             throw new Exception(string.Format("Total Qty melewati jumlah tersedia. Quantity tersedia : {0}", Helper.FormatThousand(availableQty)));
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        throw new Exception("Input is not valid");
                    }

                    //fullbag
                    decimal RemainderQty = dataVM.Qty / summary.QtyPerBag;
                    RemainderQty = dataVM.Qty - (Math.Floor(RemainderQty) * summary.QtyPerBag);
                    int BagQty = Convert.ToInt32((dataVM.Qty - RemainderQty) / summary.QtyPerBag);
                    decimal totalQty = BagQty * summary.QtyPerBag;

                    int lastSeries = 0;
                    int startSeries = 0;

                    OutboundReturn ret = new OutboundReturn();

                    if (BagQty > 0)
                    {
                        lastSeries = await db.OutboundReturns.Where(m => m.StockCode.Equals(StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + BagQty - 1;

                        ret.ID = Helper.CreateGuid("R");
                        ret.OutboundOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = totalQty;
                        ret.StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), Helper.FormatThousand(totalQty), summary.LotNo, summary.InDate.ToString("yyyyMMdd").Substring(1), summary.ExpDate.ToString("yyyyMMdd").Substring(2));
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.QtyPerBag = summary.QtyPerBag;
                        ret.PrevStockCode = StockCode;
                        ret.LastSeries = lastSeries;
                        db.OutboundReturns.Add(ret);
                    }

                    //remainder
                    if (RemainderQty > 0)
                    {
                        int lastSeries1 = 0;
                        int startSeries1 = 0;

                        lastSeries1 = await db.OutboundReturns.Where(m => m.StockCode.Equals(StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries1 == 0)
                        {
                            startSeries1 = 1;
                        }
                        else
                        {
                            startSeries1 = lastSeries1 + 1;
                        }
                        lastSeries1 = startSeries1;

                        ret = new OutboundReturn();
                        ret.ID = Helper.CreateGuid("R");
                        ret.OutboundOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = RemainderQty;
                        ret.StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), Helper.FormatThousand(RemainderQty), summary.LotNo, summary.InDate.ToString("yyyyMMdd").Substring(1), summary.ExpDate.ToString("yyyyMMdd").Substring(2));
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.QtyPerBag = RemainderQty;
                        ret.PrevStockCode = StockCode;
                        ret.LastSeries = lastSeries1;

                        db.OutboundReturns.Add(ret);
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Return berhasil.";
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
        public async Task<IHttpActionResult> Picking(OutboundPickingReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            string OutstandingQty = "0";
            string PickingBagQty = "0";
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


                    if (string.IsNullOrEmpty(req.OrderId))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (order.OutboundHeader.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi karena transaksi sudah ditutup.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    //dont trim materialcode
                    string LotNumber = "";
                    string QtyPerBag = "";
                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    RawMaterial cekQtyPerBag = await db.RawMaterials.Where(s => s.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();

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

                        string WarehouseCode = order.OutboundHeader.WarehouseCode;
                        if (!binRack.WarehouseCode.Equals(WarehouseCode))
                        {
                            throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse yang dipilih.");
                        }
                    }


                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.Quantity > 0 && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
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



                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType))
                       .OrderByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                       .ThenBy(s => s.InDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();
                    //.ThenBy(s => s.Quantity).FirstOrDefault();

                    if (stkAll == null)
                    {
                        throw new Exception("Stock tidak tersedia.");
                    }

                    //restriction 2 : REMAINDER QTY

                    //if (stockAll.QtyPerBag > stkAll.QtyPerBag)
                    //{
                    //    throw new Exception(string.Format("FIFO Restriction, harus mengambil material dengan keterangan = LotNo : {0} & Qty/Bag : {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag), stkAll.BinRackCode));
                    //}

                    ////restriction 3 : IN DATE

                    //if (stockAll.InDate.Date > stkAll.InDate.Date)
                    //{
                    //    throw new Exception(string.Format("FIFO Restriction, harus mengambil material dengan keterangan = LotNo : {0} & In Date: {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.NullDateToString(stkAll.InDate), stkAll.BinRackCode));
                    //}

                    //restriction 4 : EXPIRED DATE

                    if (DateTime.Now.Date >= stkAll.ExpiredDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, harus melakukan QC Inspection untuk material dengan keterangan = LotNo : {0} & Qty/Bag : {1} pada Bin Rack {2} terlebih dahulu.", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag), stkAll.BinRackCode));
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
                            decimal requestedQty = order.TotalQty;
                            decimal pickedQty = order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag);
                            decimal availableQty = requestedQty - pickedQty;
                            int availableBagQty = Convert.ToInt32(Math.Ceiling(availableQty / order.QtyPerBag));

                            if (req.BagQty > availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                            }
                        }
                    }

                    OutboundPicking picking = new OutboundPicking();
                    picking.ID = Helper.CreateGuid("P");
                    picking.OutboundOrderID = order.ID;
                    picking.PickingMethod = "SCAN";
                    picking.PickedOn = DateTime.Now;
                    picking.PickedBy = activeUser;
                    picking.BinRackID = binRack.ID;
                    picking.BinRackCode = stockAll.BinRackCode;
                    picking.BinRackName = stockAll.BinRackName;
                    picking.BagQty = req.BagQty;
                    picking.QtyPerBag = stockAll.QtyPerBag;
                    picking.StockCode = stockAll.Code;
                    picking.LotNo = stockAll.LotNumber;
                    picking.InDate = stockAll.InDate.Value;
                    picking.ExpDate = stockAll.ExpiredDate.Value;

                    db.OutboundPickings.Add(picking);

                    //reduce stock

                    if (stockAll.Type.Equals("RM"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }

              

                    await db.SaveChangesAsync();



                    status = true;
                    message = "Picking berhasil.";

                    order = await db.OutboundOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();


                    OutstandingQty = Helper.FormatThousand(order.TotalQty - (order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                    PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.TotalQty - (order.OutboundPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag)));

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

            obj.Add("outstanding_qty", OutstandingQty);
            obj.Add("picking_bag_qty", PickingBagQty);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Print(OutboundExcessPrintReq req)
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

                    OutboundReturn outboundReturn = await db.OutboundReturns.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (outboundReturn == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(outboundReturn.OutboundOrder.MaterialCode)).FirstOrDefault();
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

                    int fullBag = Convert.ToInt32(outboundReturn.ReturnQty / outboundReturn.QtyPerBag);

                    int lastSeries = outboundReturn.LastSeries;


                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = outboundReturn.OutboundOrder.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(outboundReturn.QtyPerBag).PadLeft(6) + " " + outboundReturn.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);

                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = outboundReturn.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = outboundReturn.ExpDate;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = outboundReturn.OutboundOrder.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = outboundReturn.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = outboundReturn.OutboundOrder.MaterialName;
                        dto.Field9 = Helper.FormatThousand(outboundReturn.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = outboundReturn.OutboundOrder.MaterialCode;
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

                            // Ambil jumlah printer dari AppSettings
                            int printerCount = int.Parse(ConfigurationManager.AppSettings["printer_count"] ?? "0");

                            for (int i = 1; i <= printerCount; i++)
                            {
                                string printerIpKey = $"printer_{i}_ip";
                                string printerNameKey = $"printer_{i}_name";

                                // Periksa apakah kunci untuk printer tersedia di AppSettings
                                if (ConfigurationManager.AppSettings[printerIpKey] != null && ConfigurationManager.AppSettings[printerNameKey] != null)
                                {
                                    PrinterDTO printer = new PrinterDTO
                                    {
                                        PrinterIP = ConfigurationManager.AppSettings[printerIpKey],
                                        PrinterName = ConfigurationManager.AppSettings[printerNameKey]
                                    };

                                    printers.Add(printer);
                                }
                            }

                            // Mencari folder_name berdasarkan PrinterIP yang dipilih
                            string folder_name = printers.FirstOrDefault(printerDTO => printerDTO.PrinterIP.Equals(req.Printer))?.PrinterName ?? string.Empty;

                            string file_name = string.Format("{0}.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"));

                            using (Stream fileStream = new FileStream(string.Format(@"C:\RMI_PRINTER_SERVICE\{0}\{1}", folder_name, file_name), FileMode.CreateNew))
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

        [HttpPost]
        public async Task<IHttpActionResult> Putaway(OutboundPutawayReturnRes req)
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
                    vOutboundReturnSummary summary = null; //dont trim materialcode
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
                    string ExpDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpDate);

                    if (string.IsNullOrEmpty(req.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    summary = await db.vOutboundReturnSummaries.Where(s => s.ID.Equals(req.OrderID) && s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Item is not recognized.");
                    }

                    OutboundOrder order = await db.OutboundOrders.Where(s => s.ID.Equals(req.OrderID)).FirstOrDefaultAsync();
                    OutboundReturn outboundreturn = await db.OutboundReturns.Where(s => s.OutboundOrderID.Equals(req.OrderID) && s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!order.OutboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Return not allowed.");
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.PutawayQty.Value;
                        int availableBagQty = Convert.ToInt32(availableQty / summary.QtyPerBag);

                        if (req.BagQty > availableBagQty)
                        {
                             throw new Exception(string.Format("Bag Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableBagQty)));
                        }
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

                    OutboundPutaway putaway = new OutboundPutaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.OutboundOrderID = order.ID;
                    putaway.PutawayMethod = "MANUAL";
                    putaway.LotNo = summary.LotNo;
                    putaway.InDate = summary.InDate;
                    putaway.ExpDate = summary.ExpDate;
                    putaway.QtyPerBag = summary.QtyPerBag;
                    putaway.StockCode = summary.StockCode;
                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * summary.QtyPerBag;

                    db.OutboundPutaways.Add(putaway);

                    outboundreturn.Remarks = Convert.ToString(outboundreturn.LastSeries);

                    if (order.MaterialType.Equals("RM"))
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = order.MaterialCode;
                            stockRM.MaterialName = order.MaterialName;
                            stockRM.Code = summary.StockCode;
                            stockRM.LotNumber = summary.LotNo;
                            stockRM.InDate = summary.InDate;
                            stockRM.ExpiredDate = summary.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = summary.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = order.MaterialCode;
                            stockSFG.MaterialName = order.MaterialName;
                            stockSFG.Code = summary.StockCode;
                            stockSFG.LotNumber = summary.LotNo;
                            stockSFG.InDate = summary.InDate;
                            stockSFG.ExpiredDate = summary.ExpDate;
                            stockSFG.Quantity = putaway.PutawayQty;
                            stockSFG.QtyPerBag = summary.QtyPerBag;
                            stockSFG.BinRackID = putaway.BinRackID;
                            stockSFG.BinRackCode = putaway.BinRackCode;
                            stockSFG.BinRackName = putaway.BinRackName;
                            stockSFG.ReceivedAt = putaway.PutOn;

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
    }
}
