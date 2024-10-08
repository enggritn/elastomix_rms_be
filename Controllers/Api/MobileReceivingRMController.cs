using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using WMS_BE.Models;
using WMS_BE.Utils;
using ZXing;
using ZXing.QrCode;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace WMS_BE.Controllers.Api
{
    public class MobileReceivingRMController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpGet]
        public async Task<IHttpActionResult> GetListStockCode(string receivingDetailId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<ReceivingDetailBarcodeDTO> list = new List<ReceivingDetailBarcodeDTO>();
            try
            {
                if (string.IsNullOrEmpty(receivingDetailId))
                {
                    throw new Exception("Id is required.");
                }

                ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(receivingDetailId)).FirstOrDefaultAsync();

                if(receivingDetail == null)
                {
                    throw new Exception("Data tidak dikenali.");
                }

                if(!receivingDetail.COA)
                {
                    throw new Exception("Mohon hubungi bagian QC untuk melakukan pengecekan COA.");
                }
        
                if (receivingDetail.Inspections != null && receivingDetail.Inspections.Count() > 0)
                {
                    IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
                    data = from dat in receivingDetail.Inspections.OrderBy(m => m.InspectedOn)
                                   select new ReceivingDetailBarcodeDTO
                                   {
                                    ID = dat.ID,
                                    Type = "Inspection",
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag)),
                                    QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(dat.InspectionQty),
                                    Date = Helper.NullDateTimeToString(dat.InspectedOn),
                                    Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
                                };

                    list.AddRange(data.ToList());
                }

                if (receivingDetail.Judgements != null && receivingDetail.Judgements.Count() > 0)
                {
                    IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
                    data = from dat in receivingDetail.Judgements.OrderBy(m => m.JudgeOn)
                           select new ReceivingDetailBarcodeDTO
                           {
                               ID = dat.ID,
                               Type = "Judgement",
                               BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag)),
                               QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
                               TotalQty = Helper.FormatThousand(dat.JudgementQty),
                               Date = Helper.NullDateTimeToString(dat.JudgeOn),
                               Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
                           };

                    list.AddRange(data.ToList());
                }

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

            obj.Add("data", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Print(ReceivingRMPrintReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            ReceivingBarcodeDTO data = new ReceivingBarcodeDTO();

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
                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    if (req.PrintQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        //check to list barcode available qty for printing
                        int availableBagQty = 0;
                        if (req.Type.Equals("Inspection"))
                        {
                            Inspection inspection = receivingDetail.Inspections.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
                            availableBagQty = Convert.ToInt32(inspection.InspectionQty / receivingDetail.QtyPerBag);
                        }
                        else if (req.Type.Equals("Judgement"))
                        {
                            Judgement judgement = receivingDetail.Judgements.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
                            availableBagQty = Convert.ToInt32(judgement.JudgementQty / receivingDetail.QtyPerBag);
                        }
                        else
                        {
                            throw new Exception("Type tidak dikenali.");
                        }

                        if(req.PrintQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                        }

                        string[] listPrinter = { ConfigurationManager.AppSettings["printer_1_ip"].ToString(), ConfigurationManager.AppSettings["printer_2_ip"].ToString() };
                        if (!listPrinter.Contains(req.Printer))
                        {
                            throw new Exception("Printer tidak ditemukan.");
                        }
                    }

                    //create pdf file to specific printer folder for middleware printing
                    decimal totalQty = 0;
                    decimal qtyPerBag = 0;

                    if (req.Type.Equals("Inspection"))
                    {
                        Inspection dat = db.Inspections.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
                        RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                        data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                        data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                        data.RawMaterialMaker = rm.Maker;
                        data.StockCode = dat.ReceivingDetail.StockCode;
                        data.LotNo = dat.ReceivingDetail.LotNo;
                        data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                        data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                        data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                        data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag));
                        data.Qty = Helper.FormatThousand(dat.InspectionQty);
                        data.UoM = dat.ReceivingDetail.UoM;
                        data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1);

                        totalQty = dat.InspectionQty;
                        qtyPerBag = dat.ReceivingDetail.QtyPerBag;
                    }
                    else if (req.Type.Equals("Judgement"))
                    {
                        Judgement dat = db.Judgements.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
                        RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                        data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                        data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                        data.RawMaterialMaker = rm.Maker;
                        data.StockCode = dat.ReceivingDetail.StockCode;
                        data.LotNo = dat.ReceivingDetail.LotNo;
                        data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                        data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                        data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                        data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag));
                        data.Qty = Helper.FormatThousand(dat.JudgementQty);
                        data.UoM = dat.ReceivingDetail.UoM;
                        data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1);

                        totalQty = dat.JudgementQty;
                        qtyPerBag = dat.ReceivingDetail.QtyPerBag;
                    }
                    else
                    {
                        throw new Exception("Type not recognized.");
                    }

                    int seq = 0;

                    int fullBag = req.PrintQty;
                    seq = Convert.ToInt32(data.StartSeries);

                    List<string> bodies = new List<string>();

                    int series = req.UseSeries ? 1 : 0;

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');
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
                        string qr1 = data.MaterialCode.PadRight(7) + " " + runningNumber + " " + data.QtyPerBag.PadLeft(6) + " " + data.LotNo;
                        dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";
                        if (!string.IsNullOrEmpty(data.InDate))
                        {
                            try
                            {
                                DateTime dt = DateTime.ParseExact(data.InDate, "dd/MM/yyyy", null);
                                dto.Field4 = dt.ToString("MMMM").ToUpper();
                                inDate = dt.ToString("yyyyMMdd").Substring(1);
                                inDate2 = dt.ToString("yyyMMdd");
                                inDate2 = inDate2.Substring(1);
                                inDate3 = dt.ToString("yyyy-MM-dd");
                            }
                            catch (Exception e)
                            {
                            }
                        }

                        if (!string.IsNullOrEmpty(data.ExpDate))
                        {
                            try
                            {
                                DateTime dt = DateTime.ParseExact(data.ExpDate, "dd/MM/yyyy", null);
                                expiredDate = dt.ToString("yyyyMMdd").Substring(2);
                                expiredDate2 = dt.ToString("yyyy-MM-dd");
                            }
                            catch (Exception e)
                            {
                            }
                        }
                        string qr2 = data.MaterialCode.PadRight(7) + inDate + expiredDate;
                        dto.Field5 = data.LotNo;
                        dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                        dto.Field7 = data.RawMaterialMaker;
                        dto.Field8 = data.MaterialName;
                        dto.Field9 = data.QtyPerBag.ToString();
                        dto.Field10 = data.UoM.ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = data.MaterialCode;
                        dto.Field13 = inDate3;
                        dto.Field14 = expiredDate2;
                        String body = RenderViewToString("Values", "~/Views/Receiving/Label.cshtml", dto);
                        bodies.Add(body);
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

                            printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_3_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_3_name"].ToString();

                            printers.Add(printer);

                            printer = new PrinterDTO();
                            printer.PrinterIP = ConfigurationManager.AppSettings["printer_4_ip"].ToString();
                            printer.PrinterName = ConfigurationManager.AppSettings["printer_4_name"].ToString();

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

        [HttpGet]
        public async Task<IHttpActionResult> GetBarcodeById(string type, string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ReceivingBarcodeDTO data = new ReceivingBarcodeDTO();
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    throw new Exception("Type is required.");
                }

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                if (type.Equals("Inspection"))
                {
                    Inspection dat = db.Inspections.Where(m => m.ID.Equals(id)).FirstOrDefault();
                    RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                    data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                    data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                    data.RawMaterialMaker = rm.Maker;
                    data.StockCode = dat.ReceivingDetail.StockCode;
                    data.LotNo = dat.ReceivingDetail.LotNo;
                    data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                    data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                    data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag));
                    data.Qty = Helper.FormatThousand(dat.InspectionQty);
                    data.UoM = dat.ReceivingDetail.UoM;
                    data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1);
                }
                else if (type.Equals("Judgement"))
                {
                    Judgement dat = db.Judgements.Where(m => m.ID.Equals(id)).FirstOrDefault();
                    RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
                    data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
                    data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
                    data.RawMaterialMaker = rm.Maker;
                    data.StockCode = dat.ReceivingDetail.StockCode;
                    data.LotNo = dat.ReceivingDetail.LotNo;
                    data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
                    data.ExpDate = dat.ReceivingDetail.ExpDate.Value.ToString("dd/MM/yyyy");
                    data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
                    data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag));
                    data.Qty = Helper.FormatThousand(dat.JudgementQty);
                    data.UoM = dat.ReceivingDetail.UoM;
                    data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1);
                }
                else
                {
                    throw new Exception("Type not recognized.");
                }                             

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

            obj.Add("data", data);
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
        public async Task<IHttpActionResult> GetList(ReceivingRMListReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<ReceivingDTO> list = Enumerable.Empty<ReceivingDTO>();

            DateTime filterDate = Convert.ToDateTime(req.Date);

            try
            {
                IQueryable<Receiving> query = db.Receivings.Where(s => DbFunctions.TruncateTime(s.ETA) <= DbFunctions.TruncateTime(filterDate)
                                && s.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code.Equals(req.WarehouseCode) 
                                && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))
                                && !string.IsNullOrEmpty(s.RefNumber)).AsQueryable();

                if (!string.IsNullOrEmpty(req.SourceName))
                {
                    query = query.Where(s => s.PurchaseRequestDetail.PurchaseRequestHeader.SourceName.Contains(req.SourceName));
                }

                if (!string.IsNullOrEmpty(req.MaterialName))
                {
                    query = query.Where(s => s.PurchaseRequestDetail.MaterialName.Contains(req.MaterialName));
                }

                query = query.OrderBy(s => s.PurchaseRequestDetail.PurchaseRequestHeader.Code);

                IEnumerable<Receiving> tempList = await query.ToListAsync(); 

                list = from receiving in tempList
                       select new ReceivingDTO
                       {
                           ID = receiving.ID,
                           DocumentNo = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                           RefNumber = receiving.RefNumber,
                           SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
                           SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
                           SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                           ETA = Helper.NullDateToString(receiving.ETA),
                           WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
                           WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
                           MaterialCode = receiving.MaterialCode,
                           MaterialName = receiving.MaterialName,
                           Qty = Helper.FormatThousand(receiving.Qty),
                           QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                           BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                           ReceivedBagQty = Helper.FormatThousand(Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty) / receiving.QtyPerBag)),
                           ReceiveBagQty = Helper.FormatThousand(Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty) / receiving.QtyPerBag))
                       };

                if(list.Count() > 0)
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
        public async Task<IHttpActionResult> GetHeaderById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ReceivingDTO receivingDTO = null;
            IEnumerable<ReceivingDataRMResp> list = Enumerable.Empty<ReceivingDataRMResp>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                Receiving receiving = await db.Receivings.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (receiving == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                receivingDTO = new ReceivingDTO
                {
                    ID = receiving.ID,
                    DocumentNo = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                    RefNumber = receiving.RefNumber,
                    SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
                    SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
                    SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                    WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
                    WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
                    WarehouseType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Type,
                    MaterialCode = receiving.MaterialCode,
                    MaterialName = receiving.MaterialName,
                    Qty = Helper.FormatThousand(receiving.Qty),
                    QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
                    UoM = receiving.UoM,
                    TransactionStatus = receiving.TransactionStatus,
                    ETA = Helper.NullDateToString(receiving.ETA),
                    ReceiveAction = receiving.Qty > receiving.ReceivingDetails.Sum(m => m.Qty)
                };

                RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

                receivingDTO.UoM2 = "KG";

                decimal Qty2 = 0;
                decimal ReceivedQty = receiving.ReceivingDetails.Sum(i => i.Qty);
                int ReceivedBagQty = 0;
                decimal AvailableQty = 0;
                int AvailableBagQty = 0;
                decimal QtyPerBag = 0;

                if (receiving.UoM.ToUpper().Equals("L"))
                {
                    QtyPerBag = receiving.QtyPerBag * rm.PoRate;
                    Qty2 = Convert.ToInt32(receiving.Qty / receiving.QtyPerBag) * QtyPerBag;
                }
                else
                {
                    QtyPerBag = receiving.QtyPerBag;
                    Qty2 = receiving.Qty;
                }

                AvailableQty = Qty2 - receiving.ReceivingDetails.Sum(i => i.Qty);

                receivingDTO.Qty2 = Helper.FormatThousand(Qty2);
                receivingDTO.QtyPerBag2 = Helper.FormatThousand(QtyPerBag);
                ReceivedBagQty = Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty) / QtyPerBag);

                AvailableBagQty = Convert.ToInt32(AvailableQty / QtyPerBag);

                receivingDTO.ReceivedQty = Helper.FormatThousand(ReceivedQty);
                receivingDTO.ReceivedBagQty = Helper.FormatThousand(ReceivedBagQty);

                receivingDTO.AvailableQty = Helper.FormatThousand(AvailableQty);
                receivingDTO.AvailableBagQty = Helper.FormatThousand(AvailableBagQty);

                receivingDTO.OutstandingBagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag) - ReceivedBagQty);

                receivingDTO.DefaultLot = DateTime.Now.ToString("yyyMMdd").Substring(1);

                //get list detail            
                IEnumerable<ReceivingDetail> tempList = await db.ReceivingDetails.Where(s => s.HeaderID.Equals(id)).OrderBy(m => m.ReceivedOn).ToListAsync();
                    list = from detail in tempList
                           select new ReceivingDataRMResp
                           {
                               ID = detail.ID,
                               DoNo = detail.DoNo != null ? detail.DoNo : "",
                               LotNo = detail.LotNo != null ? detail.LotNo : "",
                               InDate = Helper.NullDateToString2(detail.InDate),
                               ExpDate = Helper.NullDateToString2(detail.ExpDate),
                               Qty = Helper.FormatThousand(detail.Qty),
                               QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                               BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
                               ATA = Helper.NullDateToString2(detail.ATA),
                               UoM = detail.UoM,
                               Remarks = detail.Remarks,
                               OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
                               OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
                               NGQty = Helper.FormatThousand(detail.NGQty),
                               NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
                               PutawayTotalQty = Helper.FormatThousand(detail.Putaways.Sum(i => i.PutawayQty)),
                               PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(detail.Putaways.Sum(i => i.PutawayQty) / detail.QtyPerBag)),
                               PutawayAvailableQty = Helper.FormatThousand((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)),
                               PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag)),
                               InspectionAction = detail.Inspections.Count() > 0 ? false : true,
                               JudgementAction = detail.NGQty > 0 ? true : false,
                               PutawayAction = Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag) > 0 && detail.Inspections.Count() > 0,
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
        public async Task<IHttpActionResult> Receive(ReceivingRMReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string ReceiveId = "";
            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            ReceivingDetail receivingDetail = null;

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
                    if (string.IsNullOrEmpty(req.ReceivingHeaderId))
                    {
                        throw new Exception("ReceivingHeaderId is required.");
                    }

                    Receiving receiving = await db.Receivings.Where(s => s.ID.Equals(req.ReceivingHeaderId)).FirstOrDefaultAsync();
                    if (receiving == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();
                    if (rm == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (string.IsNullOrEmpty(req.DoNo))
                    {
                        throw new Exception("Do No. tidak boleh kosong.");
                    }

                    if (string.IsNullOrEmpty(req.LotNo))
                    {
                        throw new Exception("Lot No. tidak boleh kosong.");
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32((receiving.Qty - receiving.ReceivingDetails.Sum(i => i.Qty)) / receiving.QtyPerBag);
                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                        }
                    }

                    receivingDetail = new ReceivingDetail();
                    receivingDetail.ID = Helper.CreateGuid("RCd");
                    receivingDetail.HeaderID = receiving.ID;
                    receivingDetail.UoM = "KG";

                    if (receiving.UoM.ToUpper().Equals("L"))
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag * rm.PoRate;
                    }
                    else
                    {
                        receivingDetail.QtyPerBag = receiving.QtyPerBag;
                    }

                    receivingDetail.Qty = req.BagQty * receivingDetail.QtyPerBag;

                    DateTime now = DateTime.Now;
                    receivingDetail.ATA = now;
                    receivingDetail.InDate = receivingDetail.ATA;
                    int ShelfLife = Convert.ToInt32(Regex.Match(rm.ShelfLife, @"\d+").Value);
                    int days = 0;

                    string LifeRange = Regex.Replace(rm.ShelfLife, @"[\d-]", string.Empty).ToString();

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

                    receivingDetail.ExpDate = receivingDetail.InDate.AddDays(days);
                    receivingDetail.LotNo = req.LotNo.Trim().ToString();
                    receivingDetail.StockCode = string.Format("{0}{1}{2}{3}{4}", receiving.MaterialCode, Helper.FormatThousand(receivingDetail.QtyPerBag), receivingDetail.LotNo, receivingDetail.InDate.ToString("yyyyMMdd").Substring(1), receivingDetail.ExpDate.Value.ToString("yyyyMMdd").Substring(2));
                    receivingDetail.DoNo = req.DoNo;
                    receivingDetail.ReceivedBy = activeUser;
                    receivingDetail.ReceivedOn = now;
                    receivingDetail.COA = false;
                    receivingDetail.NGQty = 0;

                    int BagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);

                    //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
                    int startSeries = 0;
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(receivingDetail.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                        lastSeries = await db.ReceivingDetails.Where(m => m.Receiving.MaterialCode.Equals(receiving.MaterialCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.LotNo.Equals(receivingDetail.LotNo)).OrderByDescending(m => m.ReceivedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }

                    lastSeries = startSeries + BagQty - 1;

                    receivingDetail.LastSeries = lastSeries;

                    db.ReceivingDetails.Add(receivingDetail);

                    //add to Log Print RM
                    LogPrintRM logPrintRM = new LogPrintRM();
                    logPrintRM.ID = Helper.CreateGuid("LOG");
                    logPrintRM.Remarks = "Receiving RM Mobile";
                    logPrintRM.StockCode = receivingDetail.StockCode;
                    logPrintRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                    logPrintRM.MaterialName = receivingDetail.Receiving.MaterialName;
                    logPrintRM.LotNumber = receivingDetail.LotNo;
                    logPrintRM.InDate = receivingDetail.InDate;
                    logPrintRM.ExpiredDate = receivingDetail.ExpDate.Value;
                    logPrintRM.StartSeries = startSeries;
                    logPrintRM.LastSeries = lastSeries;
                    logPrintRM.PrintDate = DateTime.Now;

                    db.LogPrintRMs.Add(logPrintRM);

                    receiving.TransactionStatus = "PROGRESS";
                    if (receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2003" || receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode == "2004")
                    {
                        receivingDetail.COA = true;
                        BinRack binRack = null;
                        binRack = await db.BinRacks.Where(m => m.WarehouseCode.Equals(receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("Receiving.BinRackID", "BinRack is not recognized.");
                        }
                        Putaway putaway = new Putaway();
                        putaway.ID = Helper.CreateGuid("P");
                        putaway.ReceivingDetailID = receivingDetail.ID;
                        putaway.PutawayMethod = "MANUAL";
                        putaway.PutOn = now;
                        putaway.PutBy = activeUser;
                        putaway.BinRackID = binRack.ID;
                        putaway.BinRackCode = binRack.Code;
                        putaway.BinRackName = binRack.Name;
                        putaway.PutawayQty = req.BagQty * receivingDetail.QtyPerBag;

                        db.Putaways.Add(putaway);

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                            stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
                            stockRM.Code = receivingDetail.StockCode;
                            stockRM.InDate = receivingDetail.InDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = receivingDetail.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);
                        }

                        receiving.TransactionStatus = "CLOSED";
                    }

                    if (receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType == "OUTSOURCE")
                    {
                        var source = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode;
                        BinRack binRack1 = db.BinRacks.Where(x => x.WarehouseCode == source).FirstOrDefault();

                        List<vStockAll> stocks = db.vStockAlls.Where(m => m.Quantity > 0 && m.BinRackAreaCode == binRack1.BinRackAreaCode && m.MaterialCode == receiving.MaterialCode).OrderBy(m => m.ReceivedAt).ToList();

                        decimal countqty = receivingDetail.Qty;
                        decimal pickQty = receivingDetail.Qty;
                        foreach (vStockAll stock in stocks)
                        {
                            if (countqty > 0)
                            {
                                if (stock.Type.Equals("RM"))
                                {
                                    StockRM stockrm = db.StockRMs.Where(m => m.ID.Equals(stock.ID)).FirstOrDefault();
                                    if (pickQty >= countqty)
                                    {
                                        decimal qty = stockrm.Quantity;
                                        decimal stockrmqty;
                                        if (countqty < pickQty)
                                        {
                                            stockrmqty = stockrm.Quantity - countqty;
                                        }
                                        else
                                        {
                                            stockrmqty = stockrm.Quantity - pickQty;
                                        }

                                        if (stockrmqty > 0)
                                        {
                                            stockrm.Quantity = stockrmqty;
                                            countqty = countqty - stockrmqty;
                                        }
                                        else
                                        {
                                            if (stockrm.Quantity > countqty)
                                            {
                                                stockrm.Quantity = stockrm.Quantity - countqty;
                                                countqty = countqty - stockrm.Quantity;
                                            }
                                            else
                                            {
                                                if (stockrm.Quantity > 0)
                                                {
                                                    stockrm.Quantity = 0;
                                                    countqty = countqty - (pickQty - qty);
                                                }
                                                else
                                                {
                                                    stockrm.Quantity = 0;
                                                    countqty = countqty - (pickQty - stockrm.Quantity);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (stock.Type.Equals("SFG"))
                                {
                                    StockSFG stockfg = db.StockSFGs.Where(m => m.ID.Equals(stock.ID)).FirstOrDefault();
                                    if (pickQty >= countqty)
                                    {
                                        decimal qty = stockfg.Quantity;
                                        decimal stockfgqty;
                                        if (countqty < pickQty)
                                        {
                                            stockfgqty = stockfg.Quantity - countqty;
                                        }
                                        else
                                        {
                                            stockfgqty = stockfg.Quantity - pickQty;
                                        }

                                        if (stockfgqty > 0)
                                        {
                                            stockfg.Quantity = stockfgqty;
                                            countqty = countqty - stockfgqty;
                                        }
                                        else
                                        {
                                            if (stockfg.Quantity > countqty)
                                            {
                                                stockfg.Quantity = stockfg.Quantity - countqty;
                                                countqty = countqty - stockfg.Quantity;
                                            }
                                            else
                                            {
                                                if (stockfg.Quantity > 0)
                                                {
                                                    stockfg.Quantity = 0;
                                                    countqty = countqty - (pickQty - qty);
                                                }
                                                else
                                                {
                                                    stockfg.Quantity = 0;
                                                    countqty = countqty - (pickQty - stockfg.Quantity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await db.SaveChangesAsync();

                    ReceiveId = receivingDetail.ID;
                    status = true;
                    message = "Receiving berhasil.";
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

            obj.Add("ReceiveId", ReceiveId);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDetailById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            ReceivingDataRMResp dataDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (receivingDetail == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                dataDTO = new ReceivingDataRMResp
                {
                    ID = receivingDetail.ID,
                    DocumentNo = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                    RefNumber = receivingDetail.Receiving.RefNumber,
                    SourceType = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
                    SourceCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
                    SourceName = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
                    WarehouseCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
                    WarehouseName = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
                    MaterialCode = receivingDetail.Receiving.MaterialCode,
                    MaterialName = receivingDetail.Receiving.MaterialName,
                    DoNo = receivingDetail.DoNo != null ? receivingDetail.DoNo : "",
                    LotNo = receivingDetail.LotNo != null ? receivingDetail.LotNo : "",
                    InDate = Helper.NullDateToString2(receivingDetail.InDate),
                    ExpDate = Helper.NullDateToString2(receivingDetail.ExpDate),
                    Qty = Helper.FormatThousand(receivingDetail.Qty),
                    QtyPerBag = Helper.FormatThousand(receivingDetail.QtyPerBag),
                    BagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag)),
                    ATA = Helper.NullDateToString2(receivingDetail.ATA),
                    UoM = receivingDetail.UoM,
                    Remarks = receivingDetail.Remarks,
                    OKQty = Helper.FormatThousand(receivingDetail.Qty - receivingDetail.NGQty),
                    OKBagQty = Helper.FormatThousand(Convert.ToInt32((receivingDetail.Qty - receivingDetail.NGQty) / receivingDetail.QtyPerBag)),
                    NGQty = Helper.FormatThousand(receivingDetail.NGQty),
                    NGBagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.NGQty / receivingDetail.QtyPerBag)),
                    PutawayTotalQty = Helper.FormatThousand(receivingDetail.Putaways.Sum(i => i.PutawayQty)),
                    PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.Putaways.Sum(i => i.PutawayQty) / receivingDetail.QtyPerBag)),
                    PutawayAvailableQty = Helper.FormatThousand((receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty)),
                    PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty)) / receivingDetail.QtyPerBag)),
                    BarcodeRight = receivingDetail.Receiving.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(receivingDetail.Receiving.QtyPerBag).PadLeft(6) + " " + receivingDetail.LotNo,
                    BarcodeLeft = receivingDetail.Receiving.MaterialCode.PadRight(7) + receivingDetail.InDate.ToString("yyyyMMdd").Substring(1) + receivingDetail.ExpDate.Value.ToString("yyyyMMdd").Substring(2)
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

        [HttpPost]
        public async Task<IHttpActionResult> Inspection(InspectionRMReq req)
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

                    if (string.IsNullOrEmpty(req.ReceivingDetailId))
                    {
                        throw new Exception("ReceivingDetailId is required.");
                    }

                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if(receivingDetail.Inspections.Count() > 0)
                    {
                        throw new Exception("Inspeksi sudah dilakukan, silahkan melanjutkan proses selanjutnya.");
                    }

                    int NGBagQty = 0;
                    int remarkNGQty = 0;

                    if (req.OKBagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int availableBagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
                        if (req.OKBagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                        }

                        NGBagQty = availableBagQty - req.OKBagQty;

                        if (NGBagQty > 0)
                        {
                            remarkNGQty = req.DamageQty + req.WetQty + req.ContaminationQty;
                            if (remarkNGQty <= 0)
                            {
                                throw new Exception("Mohon untuk mengisi detail NG Bag Qty.");
                            }
                            else
                            {
                                if (remarkNGQty != NGBagQty)
                                {
                                    throw new Exception(string.Format("Total NG Bag Qty harus sesuai dengan {0}.", NGBagQty));
                                }
                            }
                        }
                    }


                    NGBagQty = (Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag)) - req.OKBagQty;
                    remarkNGQty = req.DamageQty + req.WetQty + req.ContaminationQty;

                    string remarks = "";

                    if (req.DamageQty > 0)
                    {
                        remarks += "Damage : " + req.DamageQty.ToString();
                    }

                    if (req.WetQty > 0)
                    {
                        remarks += ", Wet : " + req.WetQty.ToString();
                    }

                    if (req.ContaminationQty > 0)
                    {
                        remarks += ", Foreign Contamination : " + req.ContaminationQty.ToString();
                    }

                    if (remarkNGQty == NGBagQty)
                    {
                        receivingDetail.Remarks = remarks;
                    }

                    receivingDetail.NGQty = NGBagQty * receivingDetail.QtyPerBag;

                    int startSeries = receivingDetail.LastSeries - Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);

                    //insert to inspection log
                    int OKBagQty = Convert.ToInt32(req.OKBagQty);
                    Inspection inspection = new Inspection();
                    inspection.ID = Helper.CreateGuid("I");
                    inspection.ReceivingDetailID = receivingDetail.ID;
                    inspection.InspectionMethod = "SCAN";

                    DateTime now = DateTime.Now;
                    DateTime transactionDate = now;
                    inspection.InspectedOn = transactionDate;
                    inspection.InspectedBy = activeUser;
                    inspection.LastSeries = startSeries + OKBagQty;
                    inspection.InspectionQty = OKBagQty * receivingDetail.QtyPerBag;

                    db.Inspections.Add(inspection);
                    
                    await db.SaveChangesAsync();

                    status = true;
                    message = "Inspection berhasil.";
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
        public async Task<IHttpActionResult> Putaway(PutawayRMReq req)
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
                    if (string.IsNullOrEmpty(req.ReceivingDetailId))
                    {
                        throw new Exception("ReceivingDetailId is required.");
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
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.StockCode.Equals(StockCode) && s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

                    if (receivingDetail == null)
                    {
                        throw new Exception("Data tidak ditemukan.");
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        decimal availableQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
                        int availableBagQty = Convert.ToInt32(availableQty / receivingDetail.QtyPerBag);
                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
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

                        Warehouse wh = db.Warehouses.Where(m => m.Code.Equals(receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode)).FirstOrDefault();
                        if(wh != null && !wh.Type.Equals("EMIX"))
                        {
                            string DestinationCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode;
                            if (!binRack.WarehouseCode.Equals(DestinationCode))
                            {
                                throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse tujuan.");
                            }
                        }
                    }

                    Putaway putaway = new Putaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.ReceivingDetailID = receivingDetail.ID;
                    putaway.PutawayMethod = "SCAN";
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * receivingDetail.QtyPerBag;

                    db.Putaways.Add(putaway);

                    //insert to Stock if not exist, update quantity if barcode, indate and location is same

                    StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                    if (stockRM != null)
                    {
                        stockRM.Quantity += putaway.PutawayQty;
                    }
                    else
                    {
                        stockRM = new StockRM();
                        stockRM.ID = Helper.CreateGuid("S");
                        stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
                        stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
                        stockRM.Code = receivingDetail.StockCode;
                        stockRM.LotNumber = receivingDetail.LotNo;
                        stockRM.InDate = receivingDetail.InDate;
                        stockRM.ExpiredDate = receivingDetail.ExpDate;
                        stockRM.Quantity = putaway.PutawayQty;
                        stockRM.QtyPerBag = receivingDetail.QtyPerBag;
                        stockRM.BinRackID = putaway.BinRackID;
                        stockRM.BinRackCode = putaway.BinRackCode;
                        stockRM.BinRackName = putaway.BinRackName;
                        stockRM.ReceivedAt = putaway.PutOn;

                        db.StockRMs.Add(stockRM);
                    }

                    await db.SaveChangesAsync();

                    //update receiving plan status if all quantity have been received and putaway
                    Receiving rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();

                    decimal totalReceive = rec.Qty;
                    decimal totalPutaway = 0;

                    foreach (ReceivingDetail recDetail in rec.ReceivingDetails)
                    {
                        totalPutaway += recDetail.Putaways.Sum(i => i.PutawayQty);
                    }

                    RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(rec.MaterialCode)).FirstOrDefaultAsync();

                    int OutstandingQty = Convert.ToInt32(rec.Qty / rec.QtyPerBag) - Convert.ToInt32(totalPutaway / rm.Qty);

                    if (totalReceive == totalPutaway)
                    {
                        rec.TransactionStatus = "CLOSED";
                    }
                    else if (OutstandingQty < 1)
                    { 
                        rec.TransactionStatus = "CLOSED"; 
                    }

                    await db.SaveChangesAsync();

                    decimal availQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
                    int availBagQty = Convert.ToInt32(availQty / receivingDetail.QtyPerBag);

                    obj.Add("availableTotalQty", availQty);
                    obj.Add("availableBagQty", availBagQty);

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
        public async Task<IHttpActionResult> UploadPhoto(string ReceiveId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;
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
                    ReceivingDetail stcode = await db.ReceivingDetails.Where(x => x.ID.Equals(ReceiveId)).FirstOrDefaultAsync();
                    if (stcode == null)
                    {
                        throw new Exception("Data not found.");
                    }

                    //upload photo
                    if (request.Files.Count > 0)
                    {
                        if (string.IsNullOrEmpty(stcode.LastFotoCOA.ToString()))
                        {
                            stcode.LastFotoCOA = 1;
                        }
                        else
                        {
                            stcode.LastFotoCOA = stcode.LastFotoCOA + 1;
                        }
                        
                        HttpPostedFile file = request.Files[0];
                        string file_name = string.Format("{0}{1}", stcode.StockCode + stcode.LastSeries.ToString() + stcode.LastFotoCOA.ToString(), ".jpg");
                        string file_save = string.Format("{0}{1}", stcode.StockCode + stcode.LastSeries.ToString(), ".jpg");
                        var fileName = System.IO.Path.GetFileName(file.FileName);

                        var path = System.IO.Path.Combine(
                            HttpContext.Current.Server.MapPath("~/Content/captureCOA"),
                            file_name
                        );
                                                
                        if (!string.IsNullOrEmpty(stcode.FotoCOA))
                        {
                            var prev_path = System.IO.Path.Combine(
                            HttpContext.Current.Server.MapPath("~/Content/captureCOA"),
                            stcode.FotoCOA
                            );
                        }

                        if (Directory.Exists(HttpContext.Current.Server.MapPath("~/Content/captureCOA")))
                        {
                            file.SaveAs(path);

                            stcode.FotoCOA = file_save;
                            stcode.LastFotoCOA = stcode.LastFotoCOA;
                        }
                    }

                    await db.SaveChangesAsync();
                    status = true;
                    message = "Upload photo succeeded.";
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
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
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
