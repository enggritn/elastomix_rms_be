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
    public class MobilePrinterController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();


        [HttpPost]
        public async Task<IHttpActionResult> GetList(MobilePrintListReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<MobilePrintResp> list = Enumerable.Empty<MobilePrintResp>();


            try
            {
                IQueryable<vStockAll> query = query = db.vStockAlls.Where(m => m.BagQty > 0).AsQueryable();

                if (!string.IsNullOrEmpty(req.MaterialName))
                {
                    query = query.Where(m => m.MaterialName.Contains(req.MaterialName));

                    if (!string.IsNullOrEmpty(req.LotNo))
                    {
                        query = query.Where(m => m.LotNumber.Contains(req.LotNo));
                    }

                    if (!string.IsNullOrEmpty(req.InDate))
                    {
                        DateTime filterDate = Convert.ToDateTime(req.InDate);
                        query = query.Where(m => DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(filterDate));
                    }

                    if (!string.IsNullOrEmpty(req.ExpDate))
                    {
                        DateTime filterDate = Convert.ToDateTime(req.ExpDate);
                        query = query.Where(m => DbFunctions.TruncateTime(m.ExpiredDate) == DbFunctions.TruncateTime(filterDate));
                    }

                }


                IEnumerable<vStockAll> tempList = await query.OrderBy(m => m.InDate).ToListAsync();

                list = from data in tempList
                       select new MobilePrintResp
                       {
                           StockId = data.ID,
                           MaterialCode = data.MaterialCode,
                           MaterialName = data.MaterialName,
                           LotNo = data.LotNumber,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpiredDate),
                           BagQty = Helper.FormatThousand(data.BagQty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           Qty = Helper.FormatThousand(data.Quantity),
                           Warehouse = data.WarehouseName,
                           Area = data.BinRackAreaName,
                           BinRackCode = data.BinRackCode
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
        public async Task<IHttpActionResult> Print(MobilePrintReq req)
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

                    vStockAll stk1 = db.vStockAlls.Where(m => m.ID.Equals(req.StockId)).FirstOrDefault();
                    if (stk1 == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(stk1.MaterialCode)).FirstOrDefault();
                    if(material == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (material.QtyPerBag > 0)
                    {
                        // update quantity barcode and actual stock - Mei 2022
                        DateTime dtin = Convert.ToDateTime(stk1.InDate.Value);
                        DateTime dtexp = Convert.ToDateTime(stk1.ExpiredDate.Value);
                        //decimal TotalQty = Convert.ToDecimal(req.Qty.Replace('.',','));
                        decimal TotalQty = req.Qty; //(req.Qty.Replace('.', ','));
                        string qtybag = "";
                        if (stk1.QtyPerBag == material.QtyPerBag)
                        {
                            qtybag = "full";
                            if (TotalQty < material.QtyPerBag)
                            {
                                throw new Exception(string.Format("Quantity tidak boleh lebih kecil dari quantity full bag, quantity full bag {0}", Helper.FormatThousand(material.QtyPerBag)));
                            }
                        }
                        else
                        {
                            if (TotalQty >= material.QtyPerBag)
                            {
                                throw new Exception(string.Format("Quantity remainder tidak boleh melebihi quantity full bag, quantity full bag {0}", Helper.FormatThousand(material.QtyPerBag)));
                            }
                        }

                        int BagQty = Convert.ToInt32(TotalQty / material.QtyPerBag);
                        decimal RemainderQty = TotalQty % material.QtyPerBag;
                        if (BagQty < 1)
                        {
                            if (RemainderQty > 0)
                            {

                            }
                            else
                            {
                                throw new Exception("Quantity tidak boleh kosong atau tidak boleh 0.");
                            }
                        }
                        if (qtybag == "full" && BagQty >= 1 && RemainderQty > 0)
                        {
                            throw new Exception(string.Format("Reminder quantity tidak diperbolehkan, total quantity melebihi {0} full bag", Helper.FormatThousand(BagQty)));
                        }

                        if (stk1.Type.Equals("RM"))
                        {
                            StockRM stock = db.StockRMs.Where(m => m.MaterialCode.Equals(stk1.MaterialCode) && m.LotNumber.Equals(stk1.LotNumber) && DbFunctions.TruncateTime(m.InDate.Value) == DbFunctions.TruncateTime(dtin) && DbFunctions.TruncateTime(m.ExpiredDate.Value) == DbFunctions.TruncateTime(dtexp) && m.Quantity.Equals(stk1.Quantity) && m.BinRackCode.Equals(stk1.BinRackCode)).FirstOrDefault();
                            stock.Quantity = TotalQty;
                            if (RemainderQty > 0)
                            {
                                stock.QtyPerBag = TotalQty;
                            }
                        }
                        else if (stk1.Type.Equals("SFG"))
                        {
                            StockSFG stock = db.StockSFGs.Where(m => m.MaterialCode.Equals(stk1.MaterialCode) && m.LotNumber.Equals(stk1.LotNumber) && DbFunctions.TruncateTime(m.InDate.Value) == DbFunctions.TruncateTime(dtin) && DbFunctions.TruncateTime(m.ExpiredDate.Value) == DbFunctions.TruncateTime(dtexp) && m.Quantity.Equals(stk1.Quantity) && m.BinRackCode.Equals(stk1.BinRackCode)).FirstOrDefault();
                            stock.Quantity = TotalQty;
                            if (RemainderQty > 0)
                            {
                                stock.QtyPerBag = TotalQty;
                            }
                        }

                        await db.SaveChangesAsync();
                    }

                    vStockAll stk = db.vStockAlls.Where(m => m.ID.Equals(req.StockId)).FirstOrDefault();
                    string Maker = "";

                    if (material.ProdType.Equals("RM"))
                    {
                        RawMaterial raw = db.RawMaterials.Where(m => m.MaterialCode.Equals(material.MaterialCode)).FirstOrDefault();
                        Maker = raw.Maker;
                    }

                    if (req.PrintQty <= 0)
                    {
                        throw new Exception("Print Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
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

                    int startSeries = 0;
                    //ambil dari table nya langsung
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stk.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();

                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }
                    
                    lastSeries = startSeries + req.PrintQty - 1;

                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < req.PrintQty; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = stk.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(stk.QtyPerBag).PadLeft(6) + " " + stk.LotNumber;
                        dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = stk.InDate.Value;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = stk.ExpiredDate.Value;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = stk.MaterialCode.PadRight(len) + inDate + expiredDate;
                        dto.Field5 = stk.LotNumber;
                        dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                        dto.Field7 = Maker;
                        dto.Field8 = stk.MaterialName;
                        dto.Field9 = Helper.FormatThousand(stk.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = stk.MaterialCode;
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


                    //update log print rm here
                    LogPrintRM logPrintRM = new LogPrintRM();
                    logPrintRM.ID = Helper.CreateGuid("LOG");
                    logPrintRM.Remarks = "Re-print";
                    logPrintRM.StockCode = stk.Code;
                    logPrintRM.MaterialCode = stk.MaterialCode;
                    logPrintRM.MaterialName = stk.MaterialName;
                    logPrintRM.LotNumber = stk.LotNumber;
                    logPrintRM.InDate = stk.InDate.Value;
                    logPrintRM.ExpiredDate = stk.ExpiredDate;
                    logPrintRM.StartSeries = startSeries;
                    logPrintRM.LastSeries = lastSeries;
                    logPrintRM.PrintDate = DateTime.Now;

                    db.LogPrintRMs.Add(logPrintRM);

                    LogReprint reprint = new LogReprint();
                    reprint.ID = Helper.CreateGuid("LOG");
                    reprint.StockCode = stk.Code;
                    reprint.MaterialCode = stk.MaterialCode;
                    reprint.MaterialName = stk.MaterialName;
                    reprint.LotNumber = stk.LotNumber;
                    reprint.InDate = stk.InDate.Value;
                    reprint.ExpiredDate = stk.ExpiredDate;
                    reprint.StartSeries = startSeries;
                    reprint.LastSeries = lastSeries;
                    reprint.PrintDate = DateTime.Now;
                    reprint.PrintedBy = activeUser;
                    reprint.PrintQty = req.PrintQty;

                    db.LogReprints.Add(reprint);

                    await db.SaveChangesAsync();

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
