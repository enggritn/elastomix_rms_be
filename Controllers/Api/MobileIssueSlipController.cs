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
    public class MobileIssueSlipController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpGet]
        //public async Task<IHttpActionResult> GetListStockCode(string receivingDetailId)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    List<ReceivingDetailBarcodeDTO> list = new List<ReceivingDetailBarcodeDTO>();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(receivingDetailId))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(receivingDetailId)).FirstOrDefaultAsync();

        //        if(receivingDetail == null)
        //        {
        //            throw new Exception("Data tidak dikenali.");
        //        }

        //        if(!receivingDetail.COA)
        //        {
        //            throw new Exception("Mohon hubungi bagian QC untuk melakukan pengecekan COA.");
        //        }
        
        //        if (receivingDetail.Inspections != null && receivingDetail.Inspections.Count() > 0)
        //        {
        //            IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
        //            data = from dat in receivingDetail.Inspections.OrderBy(m => m.InspectedOn)
        //                           select new ReceivingDetailBarcodeDTO
        //                           {
        //                            ID = dat.ID,
        //                            Type = "Inspection",
        //                            BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag)),
        //                            QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
        //                            TotalQty = Helper.FormatThousand(dat.InspectionQty),
        //                            Date = Helper.NullDateTimeToString(dat.InspectedOn),
        //                            Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
        //                        };

        //            list.AddRange(data.ToList());
        //        }

        //        if (receivingDetail.Judgements != null && receivingDetail.Judgements.Count() > 0)
        //        {
        //            IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
        //            data = from dat in receivingDetail.Judgements.OrderBy(m => m.JudgeOn)
        //                   select new ReceivingDetailBarcodeDTO
        //                   {
        //                       ID = dat.ID,
        //                       Type = "Judgement",
        //                       BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag)),
        //                       QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
        //                       TotalQty = Helper.FormatThousand(dat.JudgementQty),
        //                       Date = Helper.NullDateTimeToString(dat.JudgeOn),
        //                       Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
        //                   };

        //            list.AddRange(data.ToList());
        //        }


        //        status = true;
        //        message = "Fetch data succeded.";

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

        //    obj.Add("data", list);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        [HttpPost]
        public async Task<IHttpActionResult> Print(IssueSlipReturnPrintReq req)
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
                    IssueSlipOrder order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }
                    else
                    {
                        //check status already closed
                    }

                    vIssueSlipReturnSummary summary = db.vIssueSlipReturnSummaries.Where(s => s.ID.Equals(order.ID) && s.StockCode.Equals(req.StockCode)).FirstOrDefault();
                    if (summary == null)
                    {
                        throw new Exception("Stock tidak dikenali.");
                    }

                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(summary.MaterialCode)).FirstOrDefault();
                    if(material == null)
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

                    IssueSlipReturn retur = db.IssueSlipReturns.Where(s => s.IssueSlipOrderID.Equals(order.ID) && s.StockCode.Equals(req.StockCode)).FirstOrDefault();
                    if (retur == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    int fullBag = Convert.ToInt32(summary.BagQty);

                    //ambil dari table nya langsung
                    int lastSeries = retur.LastSeries;
                    //int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(summary.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();

                    //get last series
                    seq = Convert.ToInt32(lastSeries);

                    List<string> bodies = new List<string>();

                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = summary.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(summary.QtyPerBag).PadLeft(6) + " " + summary.LotNo;
                        dto.Field3 = Domain + "/" + GenerateQRCode(qr1);

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = summary.InDate;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = summary.ExpDate;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");

                        string qr2 = summary.MaterialCode.PadRight(len) + inDate + expiredDate;
                        dto.Field5 = summary.LotNo;
                        dto.Field6 = Domain + "/" + GenerateQRCode(qr2);
                        dto.Field7 = Maker;
                        dto.Field8 = summary.MaterialName;
                        dto.Field9 = Helper.FormatThousand(summary.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = summary.MaterialCode;
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


        [HttpPost]
        public async Task<IHttpActionResult> GetList()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<IssueSlipHeaderDTO> list = Enumerable.Empty<IssueSlipHeaderDTO>();

            try
            {
                IQueryable<IssueSlipHeader> query = query = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();

                query = query.OrderByDescending(m => m.CreatedOn);
              
                IEnumerable<IssueSlipHeader> tempList = await query.ToListAsync(); 

                list = from data in tempList
                       select new IssueSlipHeaderDTO
                       {
                           ID = data.ID,
                           Code = data.Code,
                           Name = data.Name,
                           TotalRequestedQty = Helper.FormatThousand(data.IssueSlipOrders.Sum(i => i.Qty)),
                           TransactionStatus = data.TransactionStatus,
                           CreatedBy = data.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(data.CreatedOn),
                           ModifiedBy = data.ModifiedBy != null ? data.ModifiedBy : "",
                           ModifiedOn = Helper.NullDateTimeToString(data.ModifiedOn),
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
        public async Task<IHttpActionResult> GetOrderById(string id, string MaterialName, string token)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<IssueSlipOrderDTO> list = Enumerable.Empty<IssueSlipOrderDTO>();
            IEnumerable<IssueSlipOrderDTO1> list1 = Enumerable.Empty<IssueSlipOrderDTO1>();

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Token is required.");
                }

                User user = db.Users.Where(m => m.Token.Equals(token)).FirstOrDefault();
                if(user == null)
                {
                    throw new Exception("User is not recognized.");
                }

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                IssueSlipHeader header = await db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                IQueryable<vIssueSlipOrderMobile> query1 = db.vIssueSlipOrderMobiles.Where(s => s.HeaderID.Equals(header.ID)).AsQueryable().OrderBy(m => m.No);
                IQueryable<vIssueSlipOrder> query = db.vIssueSlipOrders.Where(s => s.HeaderID.Equals(header.ID)).AsQueryable().OrderBy(m => m.No);

                if (!string.IsNullOrEmpty(MaterialName) && user.AreaType.Equals("LOGISTIC"))
                {
                    query1 = query1.Where(s => s.MaterialName.Contains(MaterialName));
                }

                if (!string.IsNullOrEmpty(MaterialName) && user.AreaType.Equals("PRODUCTION"))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                }

                string UserType = "";

                if (user.AreaType.Equals("LOGISTIC"))
                {
                    UserType = "Logistic";

                    list1 = from detail in await query1.ToListAsync()
                           select new IssueSlipOrderDTO1
                           {
                               ID = detail.ID,
                               Number = detail.No.ToString(),
                               MaterialCode = detail.MaterialCode,
                               MaterialName = detail.MaterialName,
                               VendorName = detail.VendorName,
                               RequestedQty = Helper.FormatThousand(detail.Qty),
                               QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                           };
                }
                else
                {
                    UserType = "Production";
                    query = query.Where(m => m.UserType.Equals(UserType));

                    list = from detail in await query.ToListAsync()
                           select new IssueSlipOrderDTO
                           {
                               ID = detail.ID,
                               Number = detail.No.ToString(),
                               MaterialCode = detail.MaterialCode,
                               MaterialName = detail.MaterialName,
                               VendorName = detail.VendorName,
                               RequestedQty = Helper.FormatThousand(detail.Qty),
                               QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                               //PickedQty = Helper.FormatThousand(detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)),
                               //OutstandingQty = Helper.FormatThousand(detail.Qty - (detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))),
                               //PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((detail.Qty - (detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / detail.QtyPerBag))),
                               //DiffQty = Helper.FormatThousand(detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - detail.Qty),
                               //PickingAction = detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - detail.Qty <= 0 ? true : false,
                               //ReturnAction = detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) > 0 ? true : false,
                               //ReturnedQty = Helper.FormatThousand(detail.IssueSlipReturns.Sum(i => i.ReturnQty)),
                               //AvailableReturnQty = Helper.FormatThousand(detail.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - detail.IssueSlipReturns.Sum(i => i.ReturnQty))
                           };
                }               


                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                    obj.Add("list", list);
                }
                else if (list1.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";
                    obj.Add("list", list1);
                }
                else
                {
                    if (user.AreaType.Equals("LOGISTIC"))
                    {
                        status = true;
                        message = "Tidak ada data.";
                        obj.Add("list", list1);
                    }
                    else
                    {
                        status = true;
                        message = "Tidak ada data.";
                        obj.Add("list", list);
                    }
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

        [HttpGet]
        public async Task<IHttpActionResult> GetOrderDetailById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IssueSlipOrderDTO orderDTO = new IssueSlipOrderDTO();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                IssueSlipOrder order = await db.IssueSlipOrders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (order == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                orderDTO.ID = order.ID;
                orderDTO.Number = order.No.ToString();
                orderDTO.MaterialCode = order.MaterialCode;
                orderDTO.MaterialName = order.MaterialName;
                orderDTO.VendorName = order.VendorName;
                orderDTO.RequestedQty = Helper.FormatThousand(order.Qty);
                orderDTO.QtyPerBag = Helper.FormatThousand(order.QtyPerBag);
                orderDTO.PickedQty = Helper.FormatThousand(order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag));
                orderDTO.DiffQty = Helper.FormatThousand(order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - order.Qty);
                //orderDTO.ReturnAction = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) > 0 ? true : false;
                orderDTO.ReturnAction = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - order.IssueSlipReturns.Sum(i => i.ReturnQty) > 0 ? true : false;

                if (order.QtyPerBag < 1)
                {
                    orderDTO.ReturnedQty = Helper.FormatThousand(order.IssueSlipReturns.Sum(i => i.ReturnQty));
                    orderDTO.PickingBagQty = Helper.FormatThousand(order.IssueSlipPickings.Sum(i => i.BagQty));
                    orderDTO.OutstandingQty = Helper.FormatThousand(0);
                    orderDTO.AvailableReturnQty = Helper.FormatThousand(order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - (order.Qty + order.IssueSlipReturns.Sum(i => i.ReturnQty)));
                    orderDTO.PickingAction = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - order.Qty <= 0 ? true : false;
                }
                else
                {
                    orderDTO.ReturnedQty = Helper.FormatThousand(order.IssueSlipReturns.Sum(i => i.ReturnQty));
                    orderDTO.PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag)));
                    orderDTO.OutstandingQty = Helper.FormatThousand(order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                    orderDTO.AvailableReturnQty = Helper.FormatThousand(order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - order.IssueSlipReturns.Sum(i => i.ReturnQty));
                    orderDTO.PickingAction = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - (order.Qty + order.IssueSlipReturns.Sum(i => i.ReturnQty)) <= 0 ? true : false;
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

            obj.Add("data", orderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetPickingList(string OrderId, string token)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Token is required.");
            }

            User user = db.Users.Where(m => m.Token.Equals(token)).FirstOrDefault();
            if (user == null)
            {
                throw new Exception("User is not recognized.");
            }

            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            IssueSlipOrder order = db.IssueSlipOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();

            if(order == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }
            //string warehouseCode = request["warehouseCode"].ToString();
            //string areaCode = request["areaCode"].ToString();

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            query = query.Where(a => warehouses.Contains(a.WarehouseCode));

            int totalRow = query.Count();

            decimal requestedQty = order.Qty;
            decimal pickedQty = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
            decimal returnQty = order.IssueSlipReturns.Sum(i => i.ReturnQty);
            decimal availableQty = requestedQty + returnQty - pickedQty;

            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;

            string OutstandingQty = "0";
            string PickingBagQty = "0";

            try
            {
                query = query.Where(m => m.BinRackAreaType.Equals(user.AreaType));

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

                if(data.Count() < 1)
                {
                    message = "Tidak ada stock tersedia.";
                }

                status = true;

                order = await db.IssueSlipOrders.Where(s => s.ID.Equals(OrderId)).FirstOrDefaultAsync();

                OutstandingQty = Helper.FormatThousand(order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag)));

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

        [HttpPost]
        public async Task<IHttpActionResult> Picking(IssueSlipPickingReq req)
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

                    IssueSlipOrder order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (!order.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !order.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi karena transaksi sudah ditutup.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if(vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
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


                    User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
                    string userAreaType = userData.AreaType;

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.Quantity > 0 && m.BinRackCode.Equals(binRack.Code) && m.BinRackAreaType.Equals(userAreaType)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    //restriction 1 : AREA TYPE
                    string materialAreaType = stockAll.BinRackAreaType;

                    if (!userAreaType.Equals(materialAreaType))
                    {
                        throw new Exception(string.Format("FIFO Restriction, tidak dapat mengambil material di area {0}", materialAreaType));
                    }

                    List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType) && warehouses.Contains(s.WarehouseCode))
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
                            if (order.QtyPerBag > 0)
                            {
                                decimal requestedQty = order.Qty;
                                decimal pickedQty = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
                                decimal returnQty = order.IssueSlipReturns.Sum(i => i.ReturnQty);
                                decimal availableQty = requestedQty + returnQty - pickedQty;
                                int availableBagQty = Convert.ToInt32(Math.Ceiling(availableQty / order.QtyPerBag));

                                if (req.BagQty > availableBagQty)
                                {
                                    throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                                }
                            }
                        }
                    }

                    IssueSlipPicking picking = new IssueSlipPicking();
                    picking.ID = Helper.CreateGuid("P");
                    picking.IssueSlipOrderID = order.ID;
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
                    picking.UoM = "KG";

                    db.IssueSlipPickings.Add(picking);

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

                    IssueSlipHeader header = db.IssueSlipHeaders.Where(m => m.ID.Equals(order.HeaderID)).FirstOrDefault();
                    header.TransactionStatus = "PROGRESS";

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Picking berhasil.";

                    order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order.QtyPerBag > 0)
                    {
                        OutstandingQty = Helper.FormatThousand(order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)));
                        PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((order.Qty - (order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / order.QtyPerBag)));
                    }
                    else
                    {
                        OutstandingQty = Helper.FormatThousand(0);
                        PickingBagQty = Helper.FormatThousand(0);
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

            obj.Add("outstanding_qty", OutstandingQty);
            obj.Add("picking_bag_qty", PickingBagQty);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetReturnList(string OrderId)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            IssueSlipOrder order = db.IssueSlipOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();

            if (order == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }

            IEnumerable<vIssueSlipReturnSummary> list = Enumerable.Empty<vIssueSlipReturnSummary>();
            IEnumerable<IssueSlipReturnListResp> data = new List<IssueSlipReturnListResp>();

            IQueryable<vIssueSlipReturnSummary> query = db.vIssueSlipReturnSummaries.Where(s => s.ID.Equals(order.ID)).AsQueryable();

            int totalRow = query.Count();

            decimal pickQty = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
            decimal returnQty = order.IssueSlipReturns.Sum(i => i.ReturnQty);
            decimal availableQty = pickQty - returnQty;

            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;

            IEnumerable<BarcodeResp> barcodeList = new List<BarcodeResp>();
            IQueryable<vIssueSlipPickingSummary> barcodeQuery = db.vIssueSlipPickingSummaries.Where(s => s.ID.Equals(order.ID)).AsQueryable();

            int len = 7;
            if (vProductMaster.MaterialCode.Length > 7)
            {
                len = vProductMaster.MaterialCode.Length;
            }

            barcodeList = from detail in await barcodeQuery.ToListAsync()
                   select new BarcodeResp
                   {
                       BarcodeRight = vProductMaster.MaterialCode.PadRight(len) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(detail.QtyPerBag).PadLeft(6) + " " + detail.LotNo,
                       BarcodeLeft = vProductMaster.MaterialCode.PadRight(len) + detail.InDate.ToString("yyyyMMdd").Substring(1) + detail.ExpDate.ToString("yyyyMMdd").Substring(2)
                   };

            try
            {               
                data = from detail in await query.ToListAsync()
                       select new IssueSlipReturnListResp
                       {
                           OrderId = detail.ID,
                           StockCode = detail.StockCode,
                           MaterialCode = detail.MaterialCode,
                           MaterialName = detail.MaterialName,
                           QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                           LotNo = detail.LotNo,
                           InDate = Helper.NullDateToString(detail.InDate),
                           ExpDate = Helper.NullDateToString(detail.ExpDate),
                           BagQty = Helper.FormatThousand(Convert.ToInt32(detail.BagQty)),
                           TotalQty = Helper.FormatThousand(detail.BagQty * detail.QtyPerBag),
                           PutawayQty = Helper.FormatThousand(detail.PutawayQty),
                           PutawayAction = detail.PutawayQty != detail.TotalQty,
                           //PutawayAction = availableQty > 0,
                           //PrintBarcodeAction = MaterialType.Equals("RM") && vProductMaster.QtyPerBag != detail.QtyPerBag,
                           //BarcodeRight = MaterialType.Equals("RM") ? vProductMaster.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(detail.QtyPerBag).PadLeft(6) + " " + detail.LotNo : "",
                           //BarcodeLeft = MaterialType.Equals("RM") ? vProductMaster.MaterialCode.PadRight(7) + detail.InDate.ToString("yyyyMMdd").Substring(1) + detail.ExpDate.ToString("yyyyMMdd").Substring(2): ""
                           PrintBarcodeAction = detail.PutawayQty != detail.TotalQty && vProductMaster.QtyPerBag != detail.QtyPerBag,
                           BarcodeRight = vProductMaster.MaterialCode.PadRight(len) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(detail.QtyPerBag).PadLeft(6) + " " + detail.LotNo,
                           BarcodeLeft = vProductMaster.MaterialCode.PadRight(len) + detail.InDate.ToString("yyyyMMdd").Substring(1) + detail.ExpDate.ToString("yyyyMMdd").Substring(2)
                       };

                status = true;

                if (data.Count() < 1)
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

            obj.Add("picked_qty", Helper.FormatThousand(pickQty));
            obj.Add("return_qty", Helper.FormatThousand(returnQty));
            obj.Add("available_qty", Helper.FormatThousand(availableQty));
            obj.Add("material_type", MaterialType);
            obj.Add("qty_per_bag", Helper.FormatThousand(vProductMaster.QtyPerBag));
            obj.Add("list", data);
            obj.Add("barcode_list", barcodeList);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Return(IssueSlipReturnReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;


            decimal pickQty = 0;
            decimal returnQty = 0;
            decimal availableQty = 0;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                //string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();
                User user = db.Users.Where(m => m.Token.Equals(token)).FirstOrDefault();
                if (user != null)
                {
                    if(string.IsNullOrEmpty(req.OrderId))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    IssueSlipOrder order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (!order.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !order.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Return sudah tidak dapat dilakukan lagi karena transaksi sudah ditutup.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
                    }

                    //dont trim materialcode
                    string LotNumber = "";
                    string QtyPerBag = "";
                    string MaterialCode = req.BarcodeLeft.Substring(0, req.BarcodeLeft.Length - 13);
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
                        QtyPerBag = req.BarcodeRight.Substring(MaterialCode.Length + 7, 6).Trim();
                        LotNumber = req.BarcodeRight.Substring(MaterialCode.Length + 14);
                    }
                    string InDate = req.BarcodeLeft.Substring(MaterialCode.Length, 7);
                    string ExpiredDate = req.BarcodeLeft.Substring(MaterialCode.Length + 7, 6);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    vIssueSlipPickingSummary summary = await db.vIssueSlipPickingSummaries.Where(s => s.ID.Equals(order.ID) && s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    decimal TotalQty = (req.BagQty * summary.QtyPerBag) + req.RemainderQty;

                    req.BagQty = Convert.ToInt32(Math.Floor(TotalQty / summary.QtyPerBag));
                    req.RemainderQty = TotalQty % summary.QtyPerBag;                  

                    pickQty = summary.TotalQty.Value;
                    returnQty = summary.ReturnQty.Value;
                    availableQty = pickQty - returnQty;

                    if (order.QtyPerBag > 0)
                    {
                        //Return production remaining
                        if (user.AreaType != "LOGISTIC" && TotalQty > vProductMaster.QtyPerBag)
                        {
                            throw new Exception("Quantity full bag harus dikembalikan ke warehouse.");
                        }
                    }

                    if (TotalQty <= 0)
                    {
                        throw new Exception("Total Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        decimal allowedQty = summary.TotalQty.Value - summary.ReturnQty.Value;

                        if (TotalQty > allowedQty)
                        {
                            throw new Exception(string.Format("Total Qty melewati jumlah tersedia. Quantity tersedia : {0}", Helper.FormatThousand(allowedQty)));
                        }
                    }

                    //fullbag
                    int totalFullBag = Convert.ToInt32(req.BagQty);
                    decimal totalQty = totalFullBag * summary.QtyPerBag;

                    IssueSlipReturn ret = new IssueSlipReturn();
                    if (req.BagQty > 0)
                    {
                        ret.ID = Helper.CreateGuid("R");
                        ret.IssueSlipOrderID = summary.ID;
                        ret.ReturnMethod = "SCAN";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = user.Username;
                        ret.ReturnQty = totalQty;
                        ret.StockCode = summary.StockCode;
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.QtyPerBag = summary.QtyPerBag;
                        ret.PrevStockCode = StockCode;
                        db.IssueSlipReturns.Add(ret);
                    }

                    //remainder
                    if (req.RemainderQty > 0)
                    {
                        ret = new IssueSlipReturn();
                        ret.ID = Helper.CreateGuid("R");
                        ret.IssueSlipOrderID = summary.ID;
                        ret.ReturnMethod = "SCAN";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = user.Username;
                        ret.ReturnQty = req.RemainderQty;
                        ret.QtyPerBag = req.RemainderQty;
                        //create new stock code
                        ret.StockCode = string.Format("{0}{1}{2}{3}{4}", summary.MaterialCode, Helper.FormatThousand(req.RemainderQty), summary.LotNo, summary.InDate.ToString("yyyyMMdd").Substring(1), summary.ExpDate.ToString("yyyyMMdd").Substring(2));
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.PrevStockCode = StockCode;

                        //log print RM
                        //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
                        int startSeries = 0;
                        int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(ret.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + (Convert.ToInt32(req.RemainderQty / req.RemainderQty));

                        ret.LastSeries = lastSeries;

                        db.IssueSlipReturns.Add(ret);

                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Issue Slip Return";
                        logPrintRM.StockCode = ret.StockCode;
                        logPrintRM.MaterialCode = order.MaterialCode;
                        logPrintRM.MaterialName = order.MaterialName;
                        logPrintRM.LotNumber = ret.LotNo;
                        logPrintRM.InDate = ret.InDate;
                        logPrintRM.ExpiredDate = ret.ExpDate;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }

                    //submit auto print
                    await db.SaveChangesAsync();

                    status = true;
                    message = "Return berhasil.";

                    order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();
                    pickQty = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
                    returnQty = order.IssueSlipReturns.Sum(i => i.ReturnQty);
                    availableQty = pickQty - returnQty;
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

            obj.Add("picked_qty", Helper.FormatThousand(pickQty));
            obj.Add("return_qty", Helper.FormatThousand(returnQty));
            obj.Add("available_qty", Helper.FormatThousand(availableQty));
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetPutawayList(string OrderId, string StockCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            IssueSlipOrder order = db.IssueSlipOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();

            if (order == null)
            {
                throw new Exception("Data tidak ditemukan.");
            }

            IEnumerable<IssueSlipPutaway> list = Enumerable.Empty<IssueSlipPutaway>();
            IEnumerable<IssueSlipPutawayListResp> data = new List<IssueSlipPutawayListResp>();

            IQueryable<IssueSlipPutaway> query = db.IssueSlipPutaways.Where(s => s.IssueSlipOrderID.Equals(order.ID)).AsQueryable();

            int totalRow = query.Count();

            int returnBagQty = Convert.ToInt32(order.IssueSlipReturns.Where(m => m.StockCode.Equals(StockCode)).Sum(i => i.ReturnQty / i.QtyPerBag));
            int putBagQty = Convert.ToInt32(order.IssueSlipPutaways.Where(m => m.StockCode.Equals(StockCode)).Sum(i => i.PutawayQty / i.QtyPerBag));
            int availableBagQty = returnBagQty - putBagQty;

           
            vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();

            string MaterialType = vProductMaster.ProdType;

            decimal qtyPerBag = vProductMaster.QtyPerBag;
           IEnumerable <BarcodeResp> barcodeList = new List<BarcodeResp>();
            IQueryable<vIssueSlipReturnSummary> barcodeQuery = db.vIssueSlipReturnSummaries.Where(s => s.ID.Equals(order.ID)).AsQueryable();

            barcodeList = from detail in await barcodeQuery.ToListAsync()
                          select new BarcodeResp
                          {
                              BarcodeRight = MaterialType.Equals("RM") ? vProductMaster.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(detail.QtyPerBag).PadLeft(6) + " " + detail.LotNo : "",
                              BarcodeLeft = MaterialType.Equals("RM") ? vProductMaster.MaterialCode.PadRight(7) + detail.InDate.ToString("yyyyMMdd").Substring(1) + detail.ExpDate.ToString("yyyyMMdd").Substring(2) : ""
                          };

            try
            {

                data = from detail in await query.ToListAsync()
                       select new IssueSlipPutawayListResp
                       {
                           OrderId = detail.ID,
                           MaterialCode = detail.IssueSlipOrder.MaterialCode,
                           MaterialName = detail.IssueSlipOrder.MaterialName,
                           QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                           LotNo = detail.LotNo,
                           InDate = Helper.NullDateToString(detail.InDate),
                           ExpDate = Helper.NullDateToString(detail.ExpDate),
                           BagQty = Helper.FormatThousand(Convert.ToInt32(detail.PutawayQty / detail.QtyPerBag)),
                           TotalQty = Helper.FormatThousand(detail.PutawayQty),
                           BinRackCode = detail.BinRackCode,
                           BinRackName = detail.BinRackName
                       };
                status = true;

                if (data.Count() < 1)
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

            obj.Add("return_qty", Helper.FormatThousand(returnBagQty));
            obj.Add("put_qty", Helper.FormatThousand(putBagQty));
            obj.Add("available_qty", Helper.FormatThousand(availableBagQty));
            obj.Add("material_type", MaterialType);
            obj.Add("qty_per_bag", Helper.FormatThousand(qtyPerBag));
            obj.Add("list", data);
            obj.Add("barcode_list", barcodeList);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Putaway(IssueSlipPutawayReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            int returnBagQty = 0;
            int putBagQty = 0;
            int availableBagQty = 0;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                //string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();
                User user = db.Users.Where(m => m.Token.Equals(token)).FirstOrDefault();
                if (user != null)
                {
                    if (string.IsNullOrEmpty(req.OrderId))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    IssueSlipOrder order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (!order.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !order.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi karena transaksi sudah selesai.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    vIssueSlipReturnSummary summary = null;

                    if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
                    {
                        throw new Exception("Barcode Left & Barcode Right harus diisi.");
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

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        summary = await db.vIssueSlipReturnSummaries.Where(s => s.ID.Equals(order.ID) && s.StockCode.Equals(StockCode)).FirstOrDefaultAsync();

                        if(summary == null)
                        {
                            throw new Exception("Material tidak dikenali.");
                        }

                        returnBagQty = Convert.ToInt32(summary.BagQty);
                        putBagQty = (Convert.ToInt32(summary.PutawayQty / summary.QtyPerBag));
                        availableBagQty = returnBagQty - putBagQty;

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
                        BinRackArea binRackArea = await db.BinRackAreas.Where(m => m.ID.Equals(binRack.BinRackAreaID)).FirstOrDefaultAsync();
                        if (binRackArea == null)
                        {
                            throw new Exception("BinRackArea tidak ditemukan.");
                        }

                        if (order.QtyPerBag > 0)
                        {
                            if (binRackArea.Type == "PRODUCTION" && summary.PutawayQty >= vProductMaster.QtyPerBag)
                            {
                                throw new Exception("Quantity full bag harus dikembalikan ke warehouse.");
                            }

                            vStockAll cekmaterial = await db.vStockAlls.Where(m => m.MaterialCode.Equals(MaterialCode) && m.Quantity > 0 && !m.OnInspect && m.QtyPerBag < vProductMaster.QtyPerBag && m.BinRackAreaType == "PRODUCTION").FirstOrDefaultAsync();
                            if (cekmaterial != null)
                            {
                                if (binRackArea.Type == "LOGISTIC" && summary.PutawayQty < vProductMaster.QtyPerBag)
                                {
                                    throw new Exception("Quantity remaining harus dikembalikan ke production.");
                                }
                            }
                        }
                    }

                    IssueSlipPutaway putaway = new IssueSlipPutaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.IssueSlipOrderID = order.ID;
                    putaway.PutawayMethod = "SCAN";
                    putaway.LotNo = summary.LotNo;
                    putaway.InDate = summary.InDate;
                    putaway.ExpDate = summary.ExpDate;
                    putaway.QtyPerBag = summary.QtyPerBag;
                    putaway.StockCode = summary.StockCode;
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = user.Username;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * summary.QtyPerBag;

                    db.IssueSlipPutaways.Add(putaway);

                    if (vProductMaster.ProdType.Equals("RM"))
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

                    order = await db.IssueSlipOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();
                    returnBagQty = Convert.ToInt32(order.IssueSlipReturns.Where(m => m.StockCode.Equals(StockCode)).Sum(i => i.ReturnQty / i.QtyPerBag));
                    putBagQty = Convert.ToInt32(order.IssueSlipPutaways.Where(m => m.StockCode.Equals(StockCode)).Sum(i => i.PutawayQty / i.QtyPerBag));

                    availableBagQty = returnBagQty - putBagQty;
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

            obj.Add("return_qty", Helper.FormatThousand(returnBagQty));
            obj.Add("put_qty", Helper.FormatThousand(putBagQty));
            obj.Add("available_qty", Helper.FormatThousand(availableBagQty));
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
