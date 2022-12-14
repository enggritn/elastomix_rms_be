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
    public class MobileReceivingSFGController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

     

        [HttpPost]
        public async Task<IHttpActionResult> GetList(string MaterialName = "")
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            List<vReceivingSFGDTO> list = new List<vReceivingSFGDTO>();

            try
            {
                IQueryable<vReceivingSFG2> query = db.vReceivingSFG2.AsQueryable();
                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(m => m.ProductName.Contains(MaterialName));
                }

                IEnumerable<vReceivingSFG2> tempList = query.ToList();

                foreach (vReceivingSFG2 rec in tempList)
                {
                    vReceivingSFGDTO data = new vReceivingSFGDTO();
                    data.MaterialCode = rec.ProductCode;
                    data.MaterialName = rec.ProductName;
                    data.LotNo = rec.LotNo;
                    data.InDate = Helper.NullDateToString(rec.InDate);
                    data.ExpDate = Helper.NullDateToString(rec.ExpDate);
                    data.QtyPerBag = Helper.FormatThousand(rec.QtyPerBag);
                    data.TotalOrder = Helper.FormatThousand(rec.TotalOrder);
                    data.AvailableReceive = Helper.FormatThousand(rec.AvailableReceive);
                    data.TotalReceive = Helper.FormatThousand(rec.TotalReceive);
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(rec.TotalOrder / rec.QtyPerBag));

                    list.Add(data);
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
        public async Task<IHttpActionResult> GetReceivingList(ReceivingSFGVM receivingVM)
        {
            HttpRequest request = HttpContext.Current.Request;
            string inDate = receivingVM.InDate;
            string expDate = receivingVM.ExpDate;
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            IEnumerable<ReceivingSFGDetailResp> list = Enumerable.Empty<ReceivingSFGDetailResp>();
            vReceivingSFGDTO receivingDTO = new vReceivingSFGDTO();

            try
            {
                if (DateTime.TryParse(inDate, out temp))
                {
                    xInDate = Convert.ToDateTime(inDate);
                }
                if (DateTime.TryParse(expDate, out temp1))
                {
                    xExpDate = Convert.ToDateTime(expDate);
                }

                vReceivingSFG receiving = db.vReceivingSFGs.Where(s => s.ProductCode.Equals(receivingVM.ProductCode) && s.LotNo.Equals(receivingVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefault();

                receivingDTO = new vReceivingSFGDTO
                {
                    MaterialCode = receiving.ProductCode,
                    MaterialName = receiving.ProductName,
                    LotNo = receiving.LotNo,
                    InDate = Helper.NullDateToString(receiving.InDate),
                    ExpDate = Helper.NullDateToString(receiving.ExpDate),
                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                    TotalOrder = Helper.FormatThousand(receiving.TotalOrder),
                    AvailableReceive = Helper.FormatThousand(receiving.AvailableReceive),
                    TotalReceive = Helper.FormatThousand(receiving.TotalReceive),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.TotalOrder / receiving.QtyPerBag)),
                    ReceiveAction = !string.IsNullOrEmpty(receiving.LotNo) && receiving.InDate.HasValue && receiving.TotalOrder != receiving.TotalReceive,
                    PrintBarcodeAction = true,
                    PutawayAction = true,
                };

                IQueryable<ReceivingSFGDetail> query = query = db.ReceivingSFGDetails.Where(s => s.ProductCode.Equals(receivingVM.ProductCode) && s.LotNo.Equals(receivingVM.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).AsQueryable();
                IEnumerable<ReceivingSFGDetail> tempList = await query.OrderByDescending(m => m.ReceivedOn).ToListAsync();

                int len = 7;
                if(receiving.ProductCode.Length > 7)
                {
                    len = receiving.ProductCode.Length;
                }

                list = from data in tempList
                       select new ReceivingSFGDetailResp
                       {
                            ReceiveId = data.ID,
                            MaterialCode = data.ProductCode,
                            MaterialName = receiving.ProductName,
                            LotNo = data.LotNo,
                            InDate = Helper.NullDateToString(data.InDate),
                            ExpDate = Helper.NullDateToString(data.ExpDate),
                            BagQty = Helper.FormatThousand(Convert.ToInt32(data.Qty / data.QtyPerBag)),
                            QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                            PutawayBagQty = Helper.FormatThousand(data.PutawaySFGs.Sum(m => m.PutawayQty / data.QtyPerBag)),
                            OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(data.Qty / data.QtyPerBag) - data.PutawaySFGs.Sum(m => m.PutawayQty) / data.QtyPerBag),
                            PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.PutawaySFGs.Sum(m => m.PutawayQty),
                            PutawayAction = data.Qty > data.PutawaySFGs.Sum(m => m.PutawayQty),
                            BarcodeRight = data.ProductCode.PadRight(len) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + data.LotNo,
                            BarcodeLeft = data.ProductCode.PadRight(len) + data.InDate.ToString("yyyyMMdd").Substring(1) + data.ExpDate.Value.ToString("yyyyMMdd").Substring(2)
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

            obj.Add("data", receivingDTO);
            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Receive(ReceivingSFGVM req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            string inDate = req.InDate;
            string expDate = req.ExpDate;
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

            if (DateTime.TryParse(inDate, out temp))
            {
                xInDate = Convert.ToDateTime(inDate);
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                xExpDate = Convert.ToDateTime(expDate);
            }

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
                    vReceivingSFG header = await db.vReceivingSFGs.Where(s => s.ProductCode.Equals(req.ProductCode) && s.LotNo.Equals(req.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(xExpDate)).FirstOrDefaultAsync();

                    SemiFinishGood sfg = null;

                    if (header == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }
                    else
                    {
                        sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(header.ProductCode)).FirstOrDefaultAsync();
                    }

                    if (req.Qty <= 0)
                    {
                        throw new Exception("Data tidak kosong.");
                    }
                    else
                    {
                        int availableQty = Convert.ToInt32(header.TotalOrder) - Convert.ToInt32(header.TotalReceive);
                        if (req.Qty > availableQty)
                        {
                            throw new Exception("Data melebihi sisa.");
                        }
                    }

                    decimal TotalQty = req.Qty;

                    decimal RemainderQty = TotalQty / req.QtyPerBag;
                    RemainderQty = TotalQty - (Math.Floor(RemainderQty) * req.QtyPerBag);
                    int BagQty = Convert.ToInt32((TotalQty - RemainderQty) / req.QtyPerBag);

                    Guid guid1 = Guid.NewGuid();

                    int lastSeries = 0;
                    int startSeries = 0;

                    DateTime TransactionDate = DateTime.Now;

                    if (BagQty > 0)
                    {
                        string StockCode = string.Format("{0}{1}{2}{3}{4}", header.ProductCode.PadRight(7), Helper.FormatThousand(header.QtyPerBag).PadLeft(6), header.LotNo, header.InDate.Value.ToString("yyyyMMdd").Substring(1), header.ExpDate.Value.ToString("yyyyMMdd").Substring(1));

                        lastSeries = await db.ReceivingSFGDetails.Where(m => m.StockCode.Equals(StockCode.Replace(" ", ""))).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + BagQty - 1;


                        ReceivingSFGDetail rec = new ReceivingSFGDetail();
                        rec.ID = guid1.ToString();
                        rec.ProductCode = header.ProductCode;
                        rec.StockCode = StockCode.Replace(" ", "");
                        rec.LotNo = header.LotNo;
                        rec.InDate = header.InDate.Value;
                        rec.ExpDate = header.ExpDate.Value;
                        rec.Qty = TotalQty - RemainderQty;
                        rec.QtyPerBag = header.QtyPerBag;
                        rec.ReceivedBy = activeUser;
                        rec.ReceivedOn = TransactionDate;
                        rec.LastSeries = lastSeries;

                        db.ReceivingSFGDetails.Add(rec);

                    }

                    if(RemainderQty > 0)
                    {
                        int lastSeries1 = 0;
                        int startSeries1 = 0;

                        Guid guid = Guid.NewGuid();
                        string barcodeReceh = string.Format("{0}{1}{2}{3}{4}", header.ProductCode.PadRight(7), Helper.FormatThousand(RemainderQty).PadLeft(6), header.LotNo, header.InDate.Value.ToString("yyyyMMdd").Substring(1), header.ExpDate.Value.ToString("yyyyMMdd").Substring(1));
                        string strID_receh = guid.ToString();

                        lastSeries1 = await db.ReceivingSFGDetails.Where(m => m.StockCode.Equals(barcodeReceh.Replace(" ", ""))).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries1 == 0)
                        {
                            startSeries1 = 1;
                        }
                        else
                        {
                            startSeries1 = lastSeries1 + 1;
                        }
                        lastSeries1 = startSeries1;

                        ReceivingSFGDetail rec = new ReceivingSFGDetail();
                        rec.ID = strID_receh;
                        rec.StockCode = barcodeReceh.Replace(" ", "");
                        rec.ProductCode = header.ProductCode;
                        rec.LotNo = header.LotNo;
                        rec.InDate = header.InDate.Value;
                        rec.ExpDate = header.ExpDate.Value;
                        rec.Qty = RemainderQty;
                        rec.QtyPerBag = RemainderQty;
                        rec.ReceivedBy = activeUser;
                        rec.ReceivedOn = TransactionDate;
                        rec.LastSeries = lastSeries1;

                        db.ReceivingSFGDetails.Add(rec);
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Receive berhasil.";
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
        public async Task<IHttpActionResult> Putaway(ReceivingSFGPutawayReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            int receiveBagQty = 0;
            int putBagQty = 0;
            int availableBagQty = 0;

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
                    if (string.IsNullOrEmpty(req.ReceiveId))
                    {
                        throw new Exception("Receive Id is required.");
                    }

                    ReceivingSFGDetail receive = await db.ReceivingSFGDetails.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    ReceivingSFG receiving = db.ReceivingSFGs.Where(s => s.ProductCode.Equals(receive.ProductCode) && s.LotNo.Equals(receive.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(receive.InDate) && DbFunctions.TruncateTime(s.ExpDate) == DbFunctions.TruncateTime(receive.ExpDate)).FirstOrDefault();
                    if (receiving.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Putaway tidak dapat dilakukan, transaksi sudah selesai.");
                    }
                    
                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.ProductCode)).FirstOrDefaultAsync();  
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
                    string QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                    string LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        receiveBagQty = Convert.ToInt32(receive.Qty / receive.QtyPerBag);
                        putBagQty = Convert.ToInt32(receive.PutawaySFGs.Sum(s => s.PutawayQty / receive.QtyPerBag));
                        availableBagQty = receiveBagQty - putBagQty;

                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
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

                        if (!binRack.WarehouseCode.Equals("3001"))
                        {
                            throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse tujuan.");
                        }
                    }

                    PutawaySFG putaway = new PutawaySFG();
                    putaway.ID = Helper.CreateGuid("Rcp");
                    putaway.ReceivingSFGDetailID = receive.ID;
                    putaway.PutawayMethod = "SCAN";
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * receive.QtyPerBag;

                    db.PutawaySFGs.Add(putaway);

                    StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(receive.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                    if (stockSFG != null)
                    {
                        stockSFG.Quantity += putaway.PutawayQty;
                    }
                    else
                    {
                        stockSFG = new StockSFG();
                        stockSFG.ID = Helper.CreateGuid("S");
                        stockSFG.MaterialCode = vProductMaster.MaterialCode;
                        stockSFG.MaterialName = vProductMaster.MaterialName;
                        stockSFG.Code = receive.StockCode;
                        stockSFG.LotNumber = receive.LotNo;
                        stockSFG.InDate = receive.InDate;
                        stockSFG.ExpiredDate = receive.ExpDate;
                        stockSFG.Quantity = putaway.PutawayQty;
                        stockSFG.QtyPerBag = receive.QtyPerBag;
                        stockSFG.BinRackID = putaway.BinRackID;
                        stockSFG.BinRackCode = putaway.BinRackCode;
                        stockSFG.BinRackName = putaway.BinRackName;
                        stockSFG.ReceivedAt = putaway.PutOn;

                        db.StockSFGs.Add(stockSFG);
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
        public async Task<IHttpActionResult> Print(ReceiveSFGPrintReq req)
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
                    ReceivingSFGDetail receive = await db.ReceivingSFGDetails.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.ProductCode)).FirstOrDefault();
                    if (material == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    string Maker = "";

                    //create pdf file to specific printer folder for middleware printing

                    decimal totalQty = 0;
                    decimal qtyPerBag = 0;

                    int len = 7;

                    if(material.MaterialCode.Length > 7)
                    {
                        len = material.MaterialCode.Length;
                    }

                    int seq = 0;


                    int fullBag = Convert.ToInt32(receive.Qty / receive.QtyPerBag);

                    int lastSeries = receive.LastSeries;


                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();

                    int series = req.UseSeries ? 1 : 0;


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    fullBag = req.UseSeries ? fullBag : req.PrintQty;

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        if (series == 1)
                        {
                            runningNumber = string.Format("{0:D5}", seq++);
                        }
                        else
                        {
                            runningNumber = string.Format("{0:D5}", 1);
                        }

                        LabelDTO dto = new LabelDTO();
                        string qr1 = material.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(receive.QtyPerBag).PadLeft(6) + " " + receive.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);

                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = receive.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = receive.ExpDate.Value;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = material.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = receive.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = material.MaterialName;
                        dto.Field9 = Helper.FormatThousand(receive.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = material.MaterialCode;
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
        public async Task<IHttpActionResult> GetListMaterialSFG()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<SemiFinishGoodDTO> list = Enumerable.Empty<SemiFinishGoodDTO>();

            try
            {
                IQueryable<SemiFinishGood> query = query = db.SemiFinishGoods.OrderBy(s => s.MaterialCode).AsQueryable();
                IEnumerable<SemiFinishGood> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new SemiFinishGoodDTO
                       {
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName
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
        public async Task<IHttpActionResult> Create(ReceivingSFGHeaderResp dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            string warehouseID = "3001";
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
                    if (!string.IsNullOrEmpty(warehouseID))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(warehouseID)).FirstOrDefaultAsync();

                        if (wh != null)
                        {
                            try
                            {                                            
                                string prefix = "RSFG";
                                string sfgCode = "";
                                ReceivingSFG receiving = new ReceivingSFG();

                                string x = dataVM.MaterialCode;
                                sfgCode = x;
                                vProduct product = await db.vProducts.Where(s => s.MaterialCode.Equals(x)).FirstOrDefaultAsync();

                                if (product != null)
                                {
                                    if (product.ProdType.Equals("SFG"))
                                    {
                                        SemiFinishGood sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(x)).FirstOrDefaultAsync();
                                        receiving.ProductCode = product.MaterialCode;
                                        receiving.ProductName = product.MaterialName;
                                        string qty = dataVM.Qty;
                                        int index = qty.LastIndexOf(".");
                                        if (index > 0)
                                        {
                                            qty = qty.Substring(0, index);
                                        }
                                        receiving.Qty = Decimal.Parse(qty);
                                        receiving.QtyPerBag = sfg.WeightPerBag;
                                        receiving.UoM = sfg.UoM;
                                        receiving.LotNo = dataVM.LotNo;

                                        int ShelfLife = Convert.ToInt32(Regex.Match(sfg.ExpiredDate, @"\d+").Value);
                                        int days = 0;

                                        string LifeRange = Regex.Replace(sfg.ExpiredDate, @"[\d-]", string.Empty).ToString();

                                        if (LifeRange.ToLower().Contains("year"))
                                        {
                                            days = (ShelfLife * (Convert.ToInt32(12 * 30))) - 1;
                                        }
                                        else if (LifeRange.ToLower().Contains("month"))
                                        {
                                            days = (Convert.ToInt32(ShelfLife * 30)) - 1;
                                        }
                                        else
                                        {
                                            days = ShelfLife - 1;
                                        }
                                        if (!string.IsNullOrEmpty(dataVM.InDate))
                                        {
                                            try
                                            {
                                                receiving.InDate = DateTime.ParseExact(dataVM.InDate, "yyyy-MM-dd", null);
                                                receiving.ExpDate = DateTime.ParseExact(Convert.ToDateTime(receiving.InDate).AddDays(days).ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else 
                                        { 
                                            receiving.InDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
                                            receiving.ExpDate = DateTime.ParseExact(Convert.ToDateTime(receiving.InDate).AddDays(days).ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
                                        }
                                        if (!string.IsNullOrEmpty(dataVM.ExpDate))
                                        {
                                            try
                                            {
                                                receiving.ExpDate = DateTime.ParseExact(dataVM.ExpDate, "yyyy-MM-dd", null);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        if (!string.IsNullOrEmpty(dataVM.ProdDate))
                                        {
                                            try
                                            {
                                                receiving.ProductionDate = DateTime.ParseExact(dataVM.ProdDate, "yyyy-MM-dd", null);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                        else { receiving.ProductionDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null); }

                                        receiving.ID = Helper.CreateGuid(prefix);
                                        receiving.TransactionStatus = "OPEN";
                                        receiving.WarehouseCode = wh.Code;
                                        receiving.WarehouseName = wh.Name;
                                        receiving.CreatedBy = activeUser;
                                        receiving.CreatedOn = DateTime.Now;

                                        db.ReceivingSFGs.Add(receiving);
                                    }
                                }
                                else
                                {
                                    throw new Exception(string.Format("Item Number {0} not recognized, please check WIP Master Data.", sfgCode));
                                }

                                await db.SaveChangesAsync();
                                message = "Data berhasil dibuat.";
                                status = true;
                            }
                            catch (Exception e)
                            {
                                message = string.Format("Create item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Warehouse tidak ditemukan.";
                        }
                    }
                    else
                    {
                        message = "Warehouse harus diisi.";
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

    }
}
