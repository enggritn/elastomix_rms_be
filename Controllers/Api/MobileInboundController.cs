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
    public class MobileInboundController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpGet]
        //public async Task<IHttpActionResult> GetListStockCode(string inboundDetailId)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    List<ReceivingDetailBarcodeDTO> list = new List<ReceivingDetailBarcodeDTO>();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(inboundDetailId))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(inboundDetailId)).FirstOrDefaultAsync();

        //        if (receivingDetail == null)
        //        {
        //            throw new Exception("Data tidak dikenali.");
        //        }

        //        if (!receivingDetail.COA)
        //        {
        //            throw new Exception("Mohon hubungi bagian QC untuk melakukan pengecekan COA.");
        //        }

        //        if (receivingDetail.Inspections != null && receivingDetail.Inspections.Count() > 0)
        //        {
        //            IEnumerable<ReceivingDetailBarcodeDTO> data = Enumerable.Empty<ReceivingDetailBarcodeDTO>();
        //            data = from dat in receivingDetail.Inspections.OrderBy(m => m.InspectedOn)
        //                   select new ReceivingDetailBarcodeDTO
        //                   {
        //                       ID = dat.ID,
        //                       Type = "Inspection",
        //                       BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag)),
        //                       QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag),
        //                       TotalQty = Helper.FormatThousand(dat.InspectionQty),
        //                       Date = Helper.NullDateTimeToString(dat.InspectedOn),
        //                       Series = string.Format("{0} - {1}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1, dat.LastSeries)
        //                   };

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

        //[HttpPost]
        //public async Task<IHttpActionResult> Print(ReceivingRMPrintReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

        //            if (receivingDetail == null)
        //            {
        //                throw new Exception("Data tidak dikenali.");
        //            }
        //            else
        //            {
        //                //check status already closed
        //            }


        //            if (req.PrintQty <= 0)
        //            {
        //                ModelState.AddModelError("Receiving.PrintQty", "Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
        //            }
        //            else
        //            {
        //                //check to list barcode available qty for printing
        //                int availableBagQty = 0;
        //                if (req.Type.Equals("Inspection"))
        //                {
        //                    Inspection inspection = receivingDetail.Inspections.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
        //                    availableBagQty = Convert.ToInt32(inspection.InspectionQty / receivingDetail.QtyPerBag);
        //                }
        //                else if (req.Type.Equals("Judgement"))
        //                {
        //                    Judgement judgement = receivingDetail.Judgements.Where(m => m.ID.Equals(req.ID)).FirstOrDefault();
        //                    availableBagQty = Convert.ToInt32(judgement.JudgementQty / receivingDetail.QtyPerBag);
        //                }
        //                else
        //                {
        //                    throw new Exception("Type tidak dikenali.");
        //                }

        //                if (req.PrintQty > availableBagQty)
        //                {
        //                    ModelState.AddModelError("Receiving.PrintQty", string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
        //                }

        //                string[] listPrinter = { ConfigurationManager.AppSettings["printer_1_ip"].ToString(), ConfigurationManager.AppSettings["printer_2_ip"].ToString() };
        //                if (!listPrinter.Contains(req.Printer))
        //                {
        //                    ModelState.AddModelError("Receiving.ListPrinter", "Printer tidak ditemukan.");
        //                }
        //            }


        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Kesalahan input");
        //            }


        //            //create pdf file to specific printer folder for middleware printing

        //            status = true;
        //            message = "Print barcode berhasil. Mohon menunggu.";

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
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        //[HttpGet]
        //public async Task<IHttpActionResult> GetBarcodeById(string type, string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    ReceivingBarcodeDTO data = new ReceivingBarcodeDTO();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(type))
        //        {
        //            throw new Exception("Type is required.");
        //        }

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        if (type.Equals("Inspection"))
        //        {
        //            Inspection dat = db.Inspections.Where(m => m.ID.Equals(id)).FirstOrDefault();
        //            RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
        //            data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
        //            data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
        //            data.RawMaterialMaker = rm.Maker;
        //            data.StockCode = dat.ReceivingDetail.StockCode;
        //            data.LotNo = dat.ReceivingDetail.LotNo;
        //            data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
        //            data.ExpDate = dat.ReceivingDetail.ExpDate.ToString("dd/MM/yyyy");
        //            data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
        //            data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag));
        //            data.Qty = Helper.FormatThousand(dat.InspectionQty);
        //            data.UoM = dat.ReceivingDetail.UoM;
        //            data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.InspectionQty / dat.ReceivingDetail.QtyPerBag) + 1);
        //        }
        //        else if (type.Equals("Judgement"))
        //        {
        //            Judgement dat = db.Judgements.Where(m => m.ID.Equals(id)).FirstOrDefault();
        //            RawMaterial rm = db.RawMaterials.Where(m => m.MaterialCode.Equals(dat.ReceivingDetail.Receiving.MaterialCode)).FirstOrDefault();
        //            data.MaterialCode = dat.ReceivingDetail.Receiving.MaterialCode;
        //            data.MaterialName = dat.ReceivingDetail.Receiving.MaterialName;
        //            data.RawMaterialMaker = rm.Maker;
        //            data.StockCode = dat.ReceivingDetail.StockCode;
        //            data.LotNo = dat.ReceivingDetail.LotNo;
        //            data.InDate = dat.ReceivingDetail.InDate.ToString("dd/MM/yyyy");
        //            data.ExpDate = dat.ReceivingDetail.ExpDate.ToString("dd/MM/yyyy");
        //            data.QtyPerBag = Helper.FormatThousand(dat.ReceivingDetail.QtyPerBag);
        //            data.BagQty = Helper.FormatThousand(Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag));
        //            data.Qty = Helper.FormatThousand(dat.JudgementQty);
        //            data.UoM = dat.ReceivingDetail.UoM;
        //            data.StartSeries = string.Format("{0}", dat.LastSeries - Convert.ToInt32(dat.JudgementQty / dat.ReceivingDetail.QtyPerBag) + 1);
        //        }
        //        else
        //        {
        //            throw new Exception("Type not recognized.");
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

        //    obj.Add("data", data);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> GetList(ReceivingRMListReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    IEnumerable<ReceivingDTO> list = Enumerable.Empty<ReceivingDTO>();


        //    DateTime filterDate = Convert.ToDateTime(req.Date);

        //    try
        //    {
        //        IQueryable<Receiving> query = db.Receivings.Where(s => DbFunctions.TruncateTime(s.ETA) <= DbFunctions.TruncateTime(filterDate)
        //                        && s.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code.Equals(req.WarehouseCode)
        //                        && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))
        //                        && !string.IsNullOrEmpty(s.RefNumber)).AsQueryable();


        //        if (!string.IsNullOrEmpty(req.SourceName))
        //        {
        //            query = query.Where(s => s.PurchaseRequestDetail.PurchaseRequestHeader.SourceName.Contains(req.SourceName));
        //        }


        //        if (!string.IsNullOrEmpty(req.MaterialName))
        //        {
        //            query = query.Where(s => s.PurchaseRequestDetail.MaterialName.Contains(req.MaterialName));
        //        }

        //        IEnumerable<Receiving> tempList = await query.ToListAsync();

        //        list = from receiving in tempList
        //               select new ReceivingDTO
        //               {
        //                   ID = receiving.ID,
        //                   DocumentNo = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
        //                   RefNumber = receiving.RefNumber,
        //                   SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
        //                   SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
        //                   SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
        //                   ETA = Helper.NullDateToString(receiving.ETA),
        //                   WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
        //                   WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
        //                   MaterialCode = receiving.MaterialCode,
        //                   MaterialName = receiving.MaterialName,
        //                   Qty = Helper.FormatThousand(receiving.Qty),
        //                   QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
        //                   BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag))
        //               };

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

        //    obj.Add("list", list);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpGet]
        //public async Task<IHttpActionResult> GetHeaderById(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    ReceivingDTO receivingDTO = null;
        //    IEnumerable<ReceivingDataRMResp> list = Enumerable.Empty<ReceivingDataRMResp>();

        //    try
        //    {
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        Receiving receiving = await db.Receivings.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

        //        if (receiving == null)
        //        {
        //            throw new Exception("Data is not recognized.");
        //        }


        //        receivingDTO = new ReceivingDTO
        //        {
        //            ID = receiving.ID,
        //            DocumentNo = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
        //            RefNumber = receiving.RefNumber,
        //            SourceType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
        //            SourceCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
        //            SourceName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
        //            WarehouseCode = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
        //            WarehouseName = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
        //            WarehouseType = receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Type,
        //            MaterialCode = receiving.MaterialCode,
        //            MaterialName = receiving.MaterialName,
        //            Qty = Helper.FormatThousand(receiving.Qty),
        //            QtyPerBag = Helper.FormatThousand(receiving.QtyPerBag),
        //            BagQty = Helper.FormatThousand(Convert.ToInt32(receiving.Qty / receiving.QtyPerBag)),
        //            UoM = receiving.UoM,
        //            TransactionStatus = receiving.TransactionStatus,
        //            ETA = Helper.NullDateToString(receiving.ETA)
        //        };

        //        RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

        //        receivingDTO.UoM2 = "KG";

        //        decimal Qty2 = 0;
        //        decimal ReceivedQty = receiving.ReceivingDetails.Sum(i => i.Qty);
        //        int ReceivedBagQty = 0;
        //        decimal AvailableQty = 0;
        //        int AvailableBagQty = 0;
        //        decimal QtyPerBag = 0;

        //        if (receiving.UoM.ToUpper().Equals("L"))
        //        {
        //            QtyPerBag = receiving.QtyPerBag * rm.PoRate;
        //            Qty2 = Convert.ToInt32(receiving.Qty / receiving.QtyPerBag) * QtyPerBag;
        //            //Qty2 = receiving.Qty;
        //        }
        //        else
        //        {
        //            QtyPerBag = receiving.QtyPerBag;
        //            //Qty2 = receiving.Qty * receiving.QtyPerBag;
        //            Qty2 = receiving.Qty;
        //        }

        //        AvailableQty = Qty2 - receiving.ReceivingDetails.Sum(i => i.Qty);

        //        receivingDTO.Qty2 = Helper.FormatThousand(Qty2);
        //        receivingDTO.QtyPerBag2 = Helper.FormatThousand(QtyPerBag);
        //        ReceivedBagQty = Convert.ToInt32(receiving.ReceivingDetails.Sum(i => i.Qty) / QtyPerBag);

        //        AvailableBagQty = Convert.ToInt32(AvailableQty / QtyPerBag);


        //        receivingDTO.ReceivedQty = Helper.FormatThousand(ReceivedQty);
        //        receivingDTO.ReceivedBagQty = Helper.FormatThousand(ReceivedBagQty);



        //        receivingDTO.AvailableQty = Helper.FormatThousand(AvailableQty);
        //        receivingDTO.AvailableBagQty = Helper.FormatThousand(AvailableBagQty);


        //        receivingDTO.DefaultLot = DateTime.Now.ToString("yyyMMdd").Substring(1);

        //        //get list detail



        //        IEnumerable<ReceivingDetail> tempList = await db.ReceivingDetails.Where(s => s.HeaderID.Equals(id)).OrderBy(m => m.ReceivedOn).ToListAsync();
        //        list = from detail in tempList
        //               select new ReceivingDataRMResp
        //               {
        //                   ID = detail.ID,
        //                   DoNo = detail.DoNo != null ? detail.DoNo : "",
        //                   LotNo = detail.LotNo != null ? detail.LotNo : "",
        //                   InDate = Helper.NullDateToString2(detail.InDate),
        //                   ExpDate = Helper.NullDateToString2(detail.ExpDate),
        //                   Qty = Helper.FormatThousand(detail.Qty),
        //                   QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
        //                   BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
        //                   ATA = Helper.NullDateToString2(detail.ATA),
        //                   UoM = detail.UoM,
        //                   Remarks = detail.Remarks,
        //                   OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
        //                   OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
        //                   NGQty = Helper.FormatThousand(detail.NGQty),
        //                   NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
        //                   PutawayTotalQty = Helper.FormatThousand(detail.Putaways.Sum(i => i.PutawayQty)),
        //                   PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(detail.Putaways.Sum(i => i.PutawayQty) / detail.QtyPerBag)),
        //                   PutawayAvailableQty = Helper.FormatThousand((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)),
        //                   PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((detail.Qty - detail.NGQty) - detail.Putaways.Sum(i => i.PutawayQty)) / detail.QtyPerBag)),
        //                   InspectionAction = detail.Inspections.Count() > 0 ? false : true,
        //                   JudgementAction = detail.NGQty > 0 ? true : false,
        //                   PutawayAction = detail.Inspections.Count() > 0 ? true : false,
        //               };


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

        //    obj.Add("data", receivingDTO);
        //    obj.Add("list", list);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> Receive(ReceivingRMReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            if (string.IsNullOrEmpty(req.ReceivingHeaderId))
        //            {
        //                throw new Exception("ReceivingHeaderId is required.");
        //            }


        //            Receiving receiving = await db.Receivings.Where(s => s.ID.Equals(req.ReceivingHeaderId)).FirstOrDefaultAsync();

        //            if (receiving == null)
        //            {
        //                throw new Exception("Data tidak dikenali.");
        //            }
        //            else
        //            {
        //                //check status already closed
        //            }

        //            RawMaterial rm = await db.RawMaterials.Where(s => s.MaterialCode.Equals(receiving.MaterialCode)).FirstOrDefaultAsync();

        //            if (rm == null)
        //            {
        //                throw new Exception("Material tidak dikenali.");
        //            }

        //            if (string.IsNullOrEmpty(req.DoNo))
        //            {
        //                ModelState.AddModelError("Receiving.DoNo", "Do No. tidak boleh kosong.");
        //            }

        //            if (string.IsNullOrEmpty(req.LotNo))
        //            {
        //                ModelState.AddModelError("Receiving.LotNo", "Lot No. tidak boleh kosong.");
        //            }

        //            if (req.BagQty <= 0)
        //            {
        //                ModelState.AddModelError("Receiving.BagQty", "Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
        //            }
        //            else
        //            {
        //                int availableBagQty = Convert.ToInt32((receiving.Qty - receiving.ReceivingDetails.Sum(i => i.Qty)) / receiving.QtyPerBag);
        //                if (req.BagQty > availableBagQty)
        //                {
        //                    ModelState.AddModelError("Receiving.BagQty", string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
        //                }
        //            }

        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Kesalahan input");
        //            }


        //            ReceivingDetail receivingDetail = new ReceivingDetail();
        //            receivingDetail.ID = Helper.CreateGuid("RCd");
        //            receivingDetail.HeaderID = receiving.ID;
        //            receivingDetail.UoM = "KG";

        //            if (receiving.UoM.ToUpper().Equals("L"))
        //            {
        //                receivingDetail.QtyPerBag = receiving.QtyPerBag * rm.PoRate;
        //            }
        //            else
        //            {
        //                receivingDetail.QtyPerBag = receiving.QtyPerBag;
        //            }

        //            receivingDetail.Qty = req.BagQty * receivingDetail.QtyPerBag;

        //            DateTime now = DateTime.Now;
        //            receivingDetail.ATA = now;
        //            receivingDetail.InDate = receivingDetail.ATA;
        //            int ShelfLife = Convert.ToInt32(Regex.Match(rm.ShelfLife, @"\d+").Value);
        //            int days = 0;

        //            string LifeRange = Regex.Replace(rm.ShelfLife, @"[\d-]", string.Empty).ToString();

        //            if (LifeRange.ToLower().Contains("month"))
        //            {
        //                days = (Convert.ToInt32(ShelfLife * 30)) - 1;
        //            }
        //            else
        //            {
        //                LifeRange = "d";
        //            }


        //            receivingDetail.ExpDate = receivingDetail.InDate.AddDays(days);

        //            receivingDetail.LotNo = req.LotNo.Trim().ToString();

        //            receivingDetail.StockCode = string.Format("{0}{1}{2}{3}{4}", receiving.MaterialCode.PadRight(7), Helper.FormatThousand(receivingDetail.QtyPerBag).PadLeft(6), receivingDetail.LotNo, receivingDetail.InDate.ToString("yyyyMMdd").Substring(1), receivingDetail.ExpDate.ToString("yyyyMMdd").Substring(2));


        //            receivingDetail.DoNo = req.DoNo;

        //            receivingDetail.ReceivedBy = activeUser;
        //            receivingDetail.ReceivedOn = now;
        //            receivingDetail.COA = false;
        //            receivingDetail.NGQty = 0;

        //            int BagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);

        //            //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
        //            int startSeries = 0;
        //            int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(receivingDetail.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
        //            if (lastSeries == 0)
        //            {
        //                startSeries = 1;
        //                lastSeries = await db.ReceivingDetails.Where(m => m.Receiving.MaterialCode.Equals(receiving.MaterialCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.LotNo.Equals(receivingDetail.LotNo)).OrderByDescending(m => m.ReceivedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
        //            }
        //            else
        //            {
        //                startSeries = lastSeries + 1;
        //            }

        //            lastSeries = startSeries + BagQty;

        //            receivingDetail.LastSeries = lastSeries;

        //            db.ReceivingDetails.Add(receivingDetail);

        //            //add to Log Print RM
        //            LogPrintRM logPrintRM = new LogPrintRM();
        //            logPrintRM.ID = Helper.CreateGuid("LOG");
        //            logPrintRM.Remarks = "Receiving RM Mobile";
        //            logPrintRM.StockCode = receivingDetail.StockCode;
        //            logPrintRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
        //            logPrintRM.MaterialName = receivingDetail.Receiving.MaterialName;
        //            logPrintRM.LotNumber = receivingDetail.LotNo;
        //            logPrintRM.InDate = receivingDetail.InDate;
        //            logPrintRM.ExpiredDate = receivingDetail.ExpDate;
        //            logPrintRM.StartSeries = startSeries;
        //            logPrintRM.LastSeries = lastSeries;
        //            logPrintRM.PrintDate = DateTime.Now;

        //            db.LogPrintRMs.Add(logPrintRM);

        //            receiving.TransactionStatus = "PROGRESS";

        //            await db.SaveChangesAsync();


        //            status = true;
        //            message = "Receiving berhasil.";

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
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}


        //[HttpGet]
        //public async Task<IHttpActionResult> GetDetailById(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    ReceivingDataRMResp dataDTO = null;

        //    try
        //    {
        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }

        //        ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

        //        if (receivingDetail == null)
        //        {
        //            throw new Exception("Data is not recognized.");
        //        }


        //        dataDTO = new ReceivingDataRMResp
        //        {
        //            ID = receivingDetail.ID,
        //            DocumentNo = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
        //            RefNumber = receivingDetail.Receiving.RefNumber,
        //            SourceType = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceType,
        //            SourceCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode,
        //            SourceName = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName,
        //            WarehouseCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Code,
        //            WarehouseName = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Warehouse.Name,
        //            MaterialCode = receivingDetail.Receiving.MaterialCode,
        //            MaterialName = receivingDetail.Receiving.MaterialName,
        //            DoNo = receivingDetail.DoNo != null ? receivingDetail.DoNo : "",
        //            LotNo = receivingDetail.LotNo != null ? receivingDetail.LotNo : "",
        //            InDate = Helper.NullDateToString2(receivingDetail.InDate),
        //            ExpDate = Helper.NullDateToString2(receivingDetail.ExpDate),
        //            Qty = Helper.FormatThousand(receivingDetail.Qty),
        //            QtyPerBag = Helper.FormatThousand(receivingDetail.QtyPerBag),
        //            BagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag)),
        //            ATA = Helper.NullDateToString2(receivingDetail.ATA),
        //            UoM = receivingDetail.UoM,
        //            Remarks = receivingDetail.Remarks,
        //            OKQty = Helper.FormatThousand(receivingDetail.Qty - receivingDetail.NGQty),
        //            OKBagQty = Helper.FormatThousand(Convert.ToInt32((receivingDetail.Qty - receivingDetail.NGQty) / receivingDetail.QtyPerBag)),
        //            NGQty = Helper.FormatThousand(receivingDetail.NGQty),
        //            NGBagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.NGQty / receivingDetail.QtyPerBag)),
        //            PutawayTotalQty = Helper.FormatThousand(receivingDetail.Putaways.Sum(i => i.PutawayQty)),
        //            PutawayTotalBagQty = Helper.FormatThousand(Convert.ToInt32(receivingDetail.Putaways.Sum(i => i.PutawayQty) / receivingDetail.QtyPerBag)),
        //            PutawayAvailableQty = Helper.FormatThousand((receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty)),
        //            PutawayAvailableBagQty = Helper.FormatThousand(Convert.ToInt32(((receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty)) / receivingDetail.QtyPerBag)),
        //            BarcodeRight = receivingDetail.Receiving.MaterialCode.PadRight(7) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(receivingDetail.Receiving.QtyPerBag).PadLeft(6) + " " + receivingDetail.LotNo,
        //            BarcodeLeft = receivingDetail.Receiving.MaterialCode.PadRight(7) + receivingDetail.InDate.ToString("yyyyMMdd").Substring(1) + receivingDetail.ExpDate.ToString("yyyyMMdd").Substring(2)
        //        };


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

        //    obj.Add("data", dataDTO);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> Inspection(InspectionRMReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {

        //            if (string.IsNullOrEmpty(req.ReceivingDetailId))
        //            {
        //                throw new Exception("ReceivingDetailId is required.");
        //            }

        //            ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

        //            if (receivingDetail == null)
        //            {
        //                throw new Exception("Data tidak dikenali.");
        //            }

        //            if (receivingDetail.Inspections.Count() > 0)
        //            {
        //                throw new Exception("Inspeksi sudah dilakukan, silahkan melanjutkan proses selanjutnya.");
        //            }

        //            int NGBagQty = 0;
        //            int remarkNGQty = 0;

        //            if (req.OKBagQty <= 0)
        //            {
        //                ModelState.AddModelError("Receiving.InspectionQTY", "Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
        //            }
        //            else
        //            {
        //                int availableBagQty = Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
        //                if (req.OKBagQty > availableBagQty)
        //                {
        //                    ModelState.AddModelError("Receiving.InspectionQTY", string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
        //                }

        //                NGBagQty = availableBagQty - req.OKBagQty;

        //                if (NGBagQty > 0)
        //                {
        //                    remarkNGQty = req.DamageQty + req.WetQty + req.ContaminationQty;
        //                    if (remarkNGQty <= 0)
        //                    {
        //                        ModelState.AddModelError("Receiving.NGBagQty", "Mohon untuk mengisi detail NG Bag Qty.");
        //                    }
        //                    else
        //                    {
        //                        if (remarkNGQty != NGBagQty)
        //                        {
        //                            ModelState.AddModelError("Receiving.NGBagQty", string.Format("Total NG Bag Qty harus sesuai dengan {0}.", NGBagQty));
        //                        }
        //                    }
        //                }
        //            }


        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Kesalahan input");
        //            }

        //            NGBagQty = (Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag)) - req.OKBagQty;

        //            remarkNGQty = req.DamageQty + req.WetQty + req.ContaminationQty;

        //            string remarks = "";

        //            if (req.DamageQty > 0)
        //            {
        //                remarks += "Damage : " + req.DamageQty.ToString();
        //            }

        //            if (req.WetQty > 0)
        //            {
        //                remarks += ", Wet : " + req.WetQty.ToString();
        //            }

        //            if (req.ContaminationQty > 0)
        //            {
        //                remarks += ", Foreign Contamination : " + req.ContaminationQty.ToString();
        //            }

        //            if (remarkNGQty == NGBagQty)
        //            {
        //                receivingDetail.Remarks = remarks;
        //            }

        //            receivingDetail.NGQty = NGBagQty * receivingDetail.QtyPerBag;



        //            int startSeries = receivingDetail.LastSeries - Convert.ToInt32(receivingDetail.Qty / receivingDetail.QtyPerBag);
        //            //insert to inspection log

        //            int OKBagQty = Convert.ToInt32(req.OKBagQty);


        //            Inspection inspection = new Inspection();
        //            inspection.ID = Helper.CreateGuid("I");
        //            inspection.ReceivingDetailID = receivingDetail.ID;
        //            inspection.InspectionMethod = "SCAN";

        //            DateTime now = DateTime.Now;
        //            DateTime transactionDate = now;
        //            inspection.InspectedOn = transactionDate;
        //            inspection.InspectedBy = activeUser;
        //            inspection.LastSeries = startSeries + OKBagQty;
        //            inspection.InspectionQty = OKBagQty * receivingDetail.QtyPerBag;

        //            db.Inspections.Add(inspection);


        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Inspection berhasil.";

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
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}



        //[HttpPost]
        //public async Task<IHttpActionResult> Putaway(PutawayRMReq req)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";

        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {


        //            if (string.IsNullOrEmpty(req.ReceivingDetailId))
        //            {
        //                throw new Exception("ReceivingDetailId is required.");
        //            }


        //            if (string.IsNullOrEmpty(req.BarcodeLeft) || string.IsNullOrEmpty(req.BarcodeRight))
        //            {
        //                throw new Exception("Barcode Left & Barcode Right harus diisi.");
        //            }

        //            string StockCode = "";

        //            try
        //            {
        //                string MaterialCode = req.BarcodeLeft.Substring(0, 7);
        //                string QtyPerBag = req.BarcodeRight.Substring(14, 6);
        //                string LotNumber = req.BarcodeRight.Substring(21);
        //                string InDate = req.BarcodeLeft.Substring(7, 7);
        //                string ExpiredDate = req.BarcodeLeft.Substring(14, 6);
        //                StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode, QtyPerBag, LotNumber, InDate, ExpiredDate);
        //            }
        //            catch
        //            {
        //                throw;
        //            }


        //            ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.StockCode.Equals(StockCode) && s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

        //            if (receivingDetail == null)
        //            {
        //                throw new Exception("Data tidak ditemukan.");
        //            }


        //            if (req.BagQty <= 0)
        //            {
        //                throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
        //            }
        //            else
        //            {
        //                decimal availableQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
        //                int availableBagQty = Convert.ToInt32(availableQty / receivingDetail.QtyPerBag);
        //                if (req.BagQty > availableBagQty)
        //                {
        //                    throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
        //                }
        //            }

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

        //                string DestinationCode = receivingDetail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode;
        //                if (!binRack.WarehouseCode.Equals(DestinationCode))
        //                {
        //                    throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse tujuan.");
        //                }
        //            }


        //            Putaway putaway = new Putaway();
        //            putaway.ID = Helper.CreateGuid("P");
        //            putaway.ReceivingDetailID = receivingDetail.ID;
        //            putaway.PutawayMethod = "SCAN";
        //            putaway.PutOn = DateTime.Now;
        //            putaway.PutBy = activeUser;
        //            putaway.BinRackID = binRack.ID;
        //            putaway.BinRackCode = binRack.Code;
        //            putaway.BinRackName = binRack.Name;
        //            putaway.PutawayQty = req.BagQty * receivingDetail.QtyPerBag;

        //            db.Putaways.Add(putaway);

        //            //insert to Stock if not exist, update quantity if barcode, indate and location is same

        //            //StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.InDate.Equals(receivingDetail.InDate.Date) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
        //            StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receivingDetail.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
        //            if (stockRM != null)
        //            {
        //                stockRM.Quantity += putaway.PutawayQty;
        //            }
        //            else
        //            {
        //                stockRM = new StockRM();
        //                stockRM.ID = Helper.CreateGuid("S");
        //                stockRM.MaterialCode = receivingDetail.Receiving.MaterialCode;
        //                stockRM.MaterialName = receivingDetail.Receiving.MaterialName;
        //                stockRM.Code = receivingDetail.StockCode;
        //                stockRM.LotNumber = receivingDetail.LotNo;
        //                stockRM.InDate = receivingDetail.InDate;
        //                stockRM.ExpiredDate = receivingDetail.ExpDate;
        //                stockRM.Quantity = putaway.PutawayQty;
        //                stockRM.QtyPerBag = receivingDetail.QtyPerBag;
        //                stockRM.BinRackID = putaway.BinRackID;
        //                stockRM.BinRackCode = putaway.BinRackCode;
        //                stockRM.BinRackName = putaway.BinRackName;
        //                stockRM.ReceivedAt = putaway.PutOn;

        //                db.StockRMs.Add(stockRM);
        //            }

        //            //update receiving plan status if all quantity have been received and putaway
        //            Receiving rec = await db.Receivings.Where(s => s.ID.Equals(receivingDetail.HeaderID)).FirstOrDefaultAsync();


        //            decimal totalReceive = rec.Qty;
        //            decimal totalPutaway = receivingDetail.Putaways.Sum(i => i.PutawayQty);

        //            if (totalReceive == totalPutaway)
        //            {
        //                rec.TransactionStatus = "CLOSED";
        //            }


        //            await db.SaveChangesAsync();

        //            decimal availQty = (receivingDetail.Qty - receivingDetail.NGQty) - receivingDetail.Putaways.Sum(i => i.PutawayQty);
        //            int availBagQty = Convert.ToInt32(availQty / receivingDetail.QtyPerBag);

        //            obj.Add("availableTotalQty", availQty);
        //            obj.Add("availableBagQty", availBagQty);

        //            status = true;
        //            message = "Putaway berhasil.";

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


        ////[HttpPost]
        ////public async Task<IHttpActionResult> JudgementMobile(JudgementReq req)
        ////{
        ////    Dictionary<string, object> obj = new Dictionary<string, object>();
        ////    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        ////    string message = "";
        ////    bool status = false;
        ////    var re = Request;
        ////    var headers = re.Headers;

        ////    try
        ////    {
        ////        string token = "";

        ////        if (headers.Contains("token"))
        ////        {
        ////            token = headers.GetValues("token").First();
        ////        }

        ////        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        ////        if (activeUser != null)
        ////        {
        ////            ReceivingDetail receivingDetail = await db.ReceivingDetails.Where(s => s.ID.Equals(req.ReceivingDetailId)).FirstOrDefaultAsync();

        ////            if (receivingDetail == null)
        ////            {
        ////                throw new Exception("Data is not recognized.");
        ////            }
        ////            else
        ////            {
        ////                //check status already closed
        ////            }


        ////            if (req.OKBagQty <= 0)
        ////            {
        ////                ModelState.AddModelError("Receiving.JudgementQTY", "Bag Qty can not be empty or below zero.");
        ////            }
        ////            else
        ////            {
        ////                int availableBagQty = Convert.ToInt32(receivingDetail.NGQty / receivingDetail.QtyPerBag);
        ////                if (req.OKBagQty > availableBagQty)
        ////                {
        ////                    ModelState.AddModelError("Receiving.JudgementQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
        ////                }
        ////            }


        ////            if (!ModelState.IsValid)
        ////            {
        ////                foreach (var state in ModelState)
        ////                {
        ////                    string field = state.Key.Split('.')[1];
        ////                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        ////                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        ////                }

        ////                throw new Exception("Kesalahan input");
        ////            }


        ////            int lastSeries = await db.Judgements.Where(m => m.ReceivingDetailID.Equals(receivingDetail.ID)).OrderByDescending(m => m.JudgeOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
        ////            if (lastSeries == 0)
        ////            {
        ////                lastSeries = await db.Inspections.Where(m => m.ReceivingDetailID.Equals(receivingDetail.ID)).OrderByDescending(m => m.InspectedOn).Select(m => m.LastSeries).FirstOrDefaultAsync();
        ////            }



        ////            lastSeries += req.OKBagQty;

        ////            //insert log judgement

        ////            Judgement judgement = new Judgement();
        ////            judgement.ID = Helper.CreateGuid("J");
        ////            judgement.ReceivingDetailID = receivingDetail.ID;
        ////            judgement.JudgementMethod = "SCAN";

        ////            DateTime now = DateTime.Now;
        ////            DateTime transactionDate = now;
        ////            judgement.JudgeOn = transactionDate;
        ////            judgement.JudgeBy = activeUser;
        ////            judgement.LastSeries = lastSeries;
        ////            judgement.JudgementQty = req.OKBagQty * receivingDetail.QtyPerBag;

        ////            db.Judgements.Add(judgement);


        ////            receivingDetail.NGQty -= judgement.JudgementQty;

        ////            await db.SaveChangesAsync();

        ////            status = true;
        ////            message = "Judgement succeeded.";

        ////        }
        ////        else
        ////        {
        ////            message = "Token sudah berakhir, silahkan login kembali.";
        ////        }
        ////    }
        ////    catch (HttpRequestException reqpEx)
        ////    {
        ////        message = reqpEx.Message;
        ////    }
        ////    catch (HttpResponseException respEx)
        ////    {
        ////        message = respEx.Message;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        message = ex.Message;
        ////    }

        ////    obj.Add("status", status);
        ////    obj.Add("message", message);
        ////    obj.Add("error_validation", customValidationMessages);

        ////    return Ok(obj);
        ////}
        ///

        [HttpPost]
        public async Task<IHttpActionResult> Create(InboundHeaderReq dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            InboundHeaderDTO data = new InboundHeaderDTO();

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
                    var TransactionId = Helper.CreateGuid("IN");

                    string prefix = TransactionId.Substring(0, 3);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.InboundHeaders.AsQueryable().OrderByDescending(x => x.Code)
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

                    InboundHeader header = new InboundHeader
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

                    db.InboundHeaders.Add(header);

                    await db.SaveChangesAsync();

                    data = new InboundHeaderDTO
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
        public async Task<IHttpActionResult> Update(InboundHeaderReq dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            InboundHeaderDTO data = new InboundHeaderDTO();

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
                        throw new Exception("Inbound ID is required.");
                    }

                    InboundHeader header = await db.InboundHeaders.Where(m => m.ID.Equals(dataVM.ID)).FirstOrDefaultAsync();
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

                    data = new InboundHeaderDTO
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
            IEnumerable<InboundHeaderDTO> list = Enumerable.Empty<InboundHeaderDTO>();


            try
            {
                IQueryable<InboundHeader> query = query = db.InboundHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED") && !s.TransactionStatus.Equals("CLOSED")).AsQueryable();
                query = query.OrderByDescending(m => m.CreatedOn);

                IEnumerable<InboundHeader> tempList = await query.ToListAsync();

                list = from data in tempList
                       select new InboundHeaderDTO
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
            IEnumerable<InboundMaterialDTO> list = Enumerable.Empty<InboundMaterialDTO>();

            try
            {
                InboundHeader header = await db.InboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data tidak dikenali.");
                }

                IQueryable<vProductMaster> query = db.vProductMasters.AsQueryable();

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                    list = from detail in await query.OrderBy(m => m.MaterialCode).ToListAsync()
                           select new InboundMaterialDTO
                           {
                               MaterialCode = detail.MaterialCode,
                               MaterialName = detail.MaterialName,
                               QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
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
            IEnumerable<InboundOrderDTO> list = Enumerable.Empty<InboundOrderDTO>();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                InboundHeader header = await db.InboundHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if (header == null)
                {
                    throw new Exception("Data is not recognized.");
                }

                IQueryable<InboundOrder> query = db.InboundOrders.Where(s => s.InboundID.Equals(header.ID)).AsQueryable();

                if (!string.IsNullOrEmpty(MaterialName))
                {
                    query = query.Where(s => s.MaterialName.Contains(MaterialName));
                }


                list = from detail in await query.OrderBy(m => m.CreatedOn).ToListAsync()
                       select new InboundOrderDTO
                       {
                           ID = detail.ID,
                           MaterialCode = detail.MaterialCode,
                           MaterialName = detail.MaterialName,
                           MaterialType = detail.MaterialType,
                           Qty = Helper.FormatThousand(detail.Qty),
                           ReceiveQty = Helper.FormatThousand(detail.InboundReceives.Sum(m => m.Qty)),
                           DiffQty = Helper.FormatThousand(detail.InboundReceives.Sum(m => m.Qty) - detail.Qty),
                           OutstandingQty = Helper.FormatThousand(detail.Qty - (detail.InboundReceives.Sum(m => m.Qty))),
                           CreatedBy = detail.CreatedBy,
                           CreatedOn = Helper.NullDateTimeToString(detail.CreatedOn),
                           ReceiveAction = header.TransactionStatus.Equals("CONFIRMED") && PutawayIsDone(detail)
                       };


                if (list.Count() > 0)
                {
                    status = true;
                    message = "Fetch data succeded.";

                    int totalList = query.Count();
                    int totalDone = 0;
                    foreach (InboundOrder order in query)
                    {
                        if (!PutawayIsDone(order))
                        {
                            totalDone += 1;
                        }
                    }

                    if(totalList == totalDone)
                    {
                        header.TransactionStatus = "CLOSED";
                        await db.SaveChangesAsync();
                    }

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

        private bool PutawayIsDone(InboundOrder order)
        {
            decimal TotalOrder = order.Qty;
            decimal TotalPutaway = 0;

            //check if putaway already done
            foreach (InboundReceive receive in order.InboundReceives)
            {
                TotalPutaway += receive.InboundPutaways.Sum(m => m.PutawayQty);
            }

            return TotalOrder != TotalPutaway;
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateOrder(InboundOrderReq dataVM)
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

                    InboundHeader header = null;
                    vProductMaster vProductMaster = null;
                    if (string.IsNullOrEmpty(dataVM.HeaderID))
                    {
                        throw new Exception("ID is required.");
                    }
                    else
                    {
                        header = await db.InboundHeaders.Where(s => s.ID.Equals(dataVM.HeaderID)).FirstOrDefaultAsync();

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
                        //ModelState.AddModelError("Inbound.MaterialCode", "Material Code is required.");
                        throw new Exception("Material Code wajib diisi.");
                    }
                    else
                    {
                        vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode)).FirstOrDefaultAsync();
                        if (vProductMaster == null)
                        {
                            //ModelState.AddModelError("Inbound.MaterialCode", "Material is not recognized.");
                            throw new Exception("Material tidak dikenali.");
                        }
                    }

                    if (dataVM.InboundQty <= 0)
                    {
                        throw new Exception("Request Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    

                    InboundOrder order = new InboundOrder()
                    {
                        ID = Helper.CreateGuid("Io"),
                        InboundID = dataVM.HeaderID,
                        MaterialCode = vProductMaster.MaterialCode,
                        MaterialName = vProductMaster.MaterialName,
                        MaterialType = vProductMaster.ProdType,
                        Qty = dataVM.InboundQty,
                        QtyPerBag = vProductMaster.QtyPerBag,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };


                    header.InboundOrders.Add(order);



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
                        InboundOrder outboundOrder = await db.InboundOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefaultAsync();
                        if (outboundOrder == null)
                        {
                            throw new Exception("Data is not recognized.");
                        }

                        if (!outboundOrder.InboundHeader.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Edit data is not allowed.");
                        }

                        db.InboundOrders.Remove(outboundOrder);

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

            InboundHeaderDTO data = new InboundHeaderDTO();

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
                    InboundHeader header = await db.InboundHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

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
                        db.InboundOrders.RemoveRange(header.InboundOrders);

                        message = "Cancel data berhasil.";
                    }

                    if (transactionStatus.Equals("CONFIRMED"))
                    {
                        //check detail
                        if (header.InboundOrders.Count() < 1)
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

                    data = new InboundHeaderDTO
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
        public async Task<IHttpActionResult> GetReceivingList(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            IEnumerable<InboundReceiveDTO> list = Enumerable.Empty<InboundReceiveDTO>();

            InboundOrderDTO orderDTO = new InboundOrderDTO();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                InboundOrder order = db.InboundOrders.Where(m => m.ID.Equals(id)).FirstOrDefault();

                if (order == null)
                {
                    throw new Exception("Data tidak ditemukan.");

                }

                orderDTO = new InboundOrderDTO
                {
                    ID = order.ID,
                    MaterialCode = order.MaterialCode,
                    MaterialName = order.MaterialName,
                    MaterialType = order.MaterialType,
                    Qty = Helper.FormatThousand(order.Qty),
                    QtyPerBag = Helper.FormatThousand(order.QtyPerBag),
                    ReceiveQty = Helper.FormatThousand(order.InboundReceives.Sum(m => m.Qty)),
                    DiffQty = Helper.FormatThousand(order.InboundReceives.Sum(m => m.Qty) - order.Qty),
                    OutstandingQty = Helper.FormatThousand(order.Qty - (order.InboundReceives.Sum(m => m.Qty))),
                    CreatedBy = order.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(order.CreatedOn),
                    ReceiveAction = order.InboundHeader.TransactionStatus.Equals("CONFIRMED") && PutawayIsDone(order)
                };

                IQueryable<InboundReceive> query = query = db.InboundReceives.Where(m => m.InboundOrderID.Equals(id)).AsQueryable();

                int len = 7;
                if (order.MaterialCode.Length > 7)
                {
                    len = order.MaterialCode.Length;
                }

                IEnumerable<InboundReceive> tempList = await query.OrderBy(m => m.ReceivedOn).ToListAsync();

                list = from data in tempList
                       select new InboundReceiveDTO
                       {
                           ID = data.ID,
                           InboundOrderID = data.InboundOrderID,
                           StockCode = data.StockCode,
                           LotNo = data.LotNo,
                           InDate = Helper.NullDateToString(data.InDate),
                           ExpDate = Helper.NullDateToString(data.ExpDate),
                           Qty = Helper.FormatThousand(data.Qty),
                           QtyPerBag = Helper.FormatThousand(data.QtyPerBag),
                           BagQty = Helper.FormatThousand(data.Qty / data.QtyPerBag),
                           ReceivedBy = data.ReceivedBy,
                           ReceivedOn = Helper.NullDateTimeToString(data.ReceivedOn),
                           PutawayQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty)),
                           PutawayBagQty = Helper.FormatThousand(data.InboundPutaways.Sum(m => m.PutawayQty / m.QtyPerBag)),
                           OutstandingQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty)),
                           OutstandingBagQty = Helper.FormatThousand(data.Qty - data.InboundPutaways.Sum(m => m.PutawayQty) / data.QtyPerBag),
                           PrintBarcodeAction = data.LastSeries > 0 && data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                           PutawayAction = data.Qty > data.InboundPutaways.Sum(m => m.PutawayQty),
                           BarcodeRight = order.MaterialCode.PadRight(len) + " " + string.Format("{0:D5}", 1) + " " + Helper.FormatThousand(data.QtyPerBag).PadLeft(6) + " " + data.LotNo,
                           BarcodeLeft = order.MaterialCode.PadRight(len) + data.InDate.Value.ToString("yyyyMMdd").Substring(1) + data.ExpDate.Value.ToString("yyyyMMdd").Substring(2)
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

            obj.Add("data", orderDTO);
            obj.Add("list", list);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Receive(InboundReceiveReq req)
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

                    if (string.IsNullOrEmpty(req.OrderId))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    InboundOrder order = await db.InboundOrders.Where(s => s.ID.Equals(req.OrderId)).FirstOrDefaultAsync();

                    if (order == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(order.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }


                    string StockCode = "";

                    vStockAll stock = null;

                    int lastSeries = 0;
                    int startSeries = 0;

                    if (req.ScanBarcode)
                    {
                        if (req.BagQty <= 0)
                        {
                            throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                        }
                        else
                        {
                            decimal TotalQty = req.BagQty * vProductMaster.QtyPerBag;
                            decimal receivedQty = order.InboundReceives.Sum(s => s.Qty);
                            decimal allowedQty = order.Qty - receivedQty;

                            if (allowedQty > vProductMaster.QtyPerBag)
                            {
                                if (TotalQty > allowedQty)
                                {
                                    throw new Exception(string.Format("Total Qty melewati jumlah tersedia. Quantity tersedia : {0}", Helper.FormatThousand(allowedQty)));
                                }
                            }
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

                        StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                        stock = db.vStockAlls.Where(m => m.Code.Equals(StockCode)).FirstOrDefault();
                        if (stock == null)
                        {
                            throw new Exception("Stock tidak ditemukan.");
                        }

                        req.RemainderQty = 0;
                    }
                    else
                    {

                        decimal TotalQty = (req.BagQty * vProductMaster.QtyPerBag) + req.RemainderQty;

                        req.BagQty = Convert.ToInt32(Math.Floor(TotalQty / vProductMaster.QtyPerBag));
                        req.RemainderQty = TotalQty % vProductMaster.QtyPerBag;


                        if (TotalQty <= 0)
                        {
                            throw new Exception("Total Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                        }
                        else
                        {
                            decimal receivedQty = order.InboundReceives.Sum(s => s.Qty);
                            decimal allowedQty = order.Qty - receivedQty;

                            if (TotalQty > allowedQty)
                            {
                                throw new Exception(string.Format("Total Qty melewati jumlah tersedia. Quantity tersedia : {0}", Helper.FormatThousand(allowedQty)));
                            }
                        }

                        //fullbag
                        int totalFullBag = Convert.ToInt32(req.BagQty);
                        decimal totalQty = totalFullBag * vProductMaster.QtyPerBag;

                        stock = db.vStockAlls.Where(s => s.MaterialCode.Equals(vProductMaster.MaterialCode) && s.Quantity > 0)
                       .OrderBy(s => s.InDate)
                       .ThenBy(s => s.ExpiredDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();

                        if (stock == null)
                        {
                            throw new Exception("Stock tidak ditemukan.");
                        }

                        //log print RM
                        //check lastSeries in LogPrintRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate

                        lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(stock.Code)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + totalFullBag - 1;
                    }

                    DateTime TransactionDate = DateTime.Now;

                    InboundReceive rec = new InboundReceive();
                    rec = new InboundReceive();
                    rec.ID = Helper.CreateGuid("Ir");
                    rec.InboundOrderID = order.ID;
                    rec.StockCode = stock.Code;
                    rec.LotNo = stock.LotNumber;
                    rec.InDate = stock.InDate.Value;
                    rec.ExpDate = stock.ExpiredDate;
                    rec.Qty = req.BagQty * vProductMaster.QtyPerBag;
                    rec.QtyPerBag = vProductMaster.QtyPerBag;
                    rec.ReceivedBy = activeUser;
                    rec.ReceivedOn = TransactionDate;
                    rec.LastSeries = lastSeries;

                    if(rec.Qty > 0)
                    {
                        db.InboundReceives.Add(rec);
                    }

                    if (lastSeries > 0)
                    {
                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Other Inbound Receive";
                        logPrintRM.StockCode = rec.StockCode;
                        logPrintRM.MaterialCode = order.MaterialCode;
                        logPrintRM.MaterialName = order.MaterialName;
                        logPrintRM.LotNumber = rec.LotNo;
                        logPrintRM.InDate = rec.InDate.Value;
                        logPrintRM.ExpiredDate = rec.ExpDate.Value;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }

                    if (req.RemainderQty > 0)
                    {
                        StockCode = string.Format("{0}{1}{2}{3}{4}", vProductMaster.MaterialCode, Helper.FormatThousand(req.RemainderQty), rec.LotNo, rec.InDate.Value.ToString("yyyyMMdd").Substring(1), rec.ExpDate.Value.ToString("yyyyMMdd").Substring(2));
                        rec = new InboundReceive();
                        rec.ID = Helper.CreateGuid("Ir");
                        rec.InboundOrderID = order.ID;
                        rec.StockCode = StockCode;
                        rec.LotNo = stock.LotNumber;
                        rec.InDate = stock.InDate.Value;
                        rec.ExpDate = stock.ExpiredDate;
                        rec.Qty = req.RemainderQty;
                        rec.QtyPerBag = req.RemainderQty;
                        rec.ReceivedBy = activeUser;
                        rec.ReceivedOn = TransactionDate;
                        rec.LastSeries = lastSeries;

                        db.InboundReceives.Add(rec);

                        lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries;

                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "Other Inbound Receive";
                        logPrintRM.StockCode = rec.StockCode;
                        logPrintRM.MaterialCode = order.MaterialCode;
                        logPrintRM.MaterialName = order.MaterialName;
                        logPrintRM.LotNumber = rec.LotNo;
                        logPrintRM.InDate = rec.InDate.Value;
                        logPrintRM.ExpiredDate = rec.ExpDate.Value;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }

                    //find stock code
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
        public async Task<IHttpActionResult> Putaway(InboundPutawayReq req)
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

                    InboundReceive receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (!receive.InboundOrder.InboundHeader.TransactionStatus.Equals("CONFIRMED"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi karena transaksi sudah selesai.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.InboundOrder.MaterialCode)).FirstOrDefaultAsync();
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
                        receiveBagQty = Convert.ToInt32(receive.Qty / receive.QtyPerBag);
                        putBagQty = Convert.ToInt32(receive.InboundPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
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

                        string WarehouseCode = receive.InboundOrder.InboundHeader.WarehouseCode;
                        if (!binRack.WarehouseCode.Equals(WarehouseCode))
                        {
                            throw new Exception("Bin Rack Warehouse tidak sesuai dengan Warehouse yang dipilih.");
                        }
                    }


                    InboundPutaway putaway = new InboundPutaway();
                    putaway.ID = Helper.CreateGuid("Ip");
                    putaway.InboundReceiveID = receive.ID;
                    putaway.PutawayMethod = "SCAN";
                    putaway.LotNo = receive.LotNo;
                    putaway.InDate = receive.InDate;
                    putaway.ExpDate = receive.ExpDate;
                    putaway.QtyPerBag = receive.QtyPerBag;
                    putaway.StockCode = receive.StockCode;
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * receive.QtyPerBag;

                    db.InboundPutaways.Add(putaway);

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(receive.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = vProductMaster.MaterialCode;
                            stockRM.MaterialName = vProductMaster.MaterialName;
                            stockRM.Code = receive.StockCode;
                            stockRM.LotNumber = receive.LotNo;
                            stockRM.InDate = receive.InDate;
                            stockRM.ExpiredDate = receive.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = receive.QtyPerBag;
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
                    }


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway berhasil.";


                    receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();
                    receiveBagQty = Convert.ToInt32(receive.Qty / receive.QtyPerBag);
                    putBagQty = Convert.ToInt32(receive.InboundPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
                    availableBagQty = receiveBagQty - putBagQty;

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

            obj.Add("receive_qty", Helper.FormatThousand(receiveBagQty));
            obj.Add("put_qty", Helper.FormatThousand(putBagQty));
            obj.Add("available_qty", Helper.FormatThousand(availableBagQty));
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Print(InboundReceivePrintReq req)
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

                    InboundReceive receive = await db.InboundReceives.Where(s => s.ID.Equals(req.ReceiveId)).FirstOrDefaultAsync();

                    if (receive == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    vProductMaster material = db.vProductMasters.Where(m => m.MaterialCode.Equals(receive.InboundOrder.MaterialCode)).FirstOrDefault();
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

                    int fullBag = Convert.ToInt32(receive.Qty / receive.QtyPerBag);

                    int lastSeries = receive.LastSeries;


                    //get last series
                    seq = Convert.ToInt32(lastSeries);


                    List<string> bodies = new List<string>();


                    string Domain = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                    for (int i = 0; i < fullBag; i++)
                    {
                        string runningNumber = "";
                        runningNumber = string.Format("{0:D5}", seq++);

                        LabelDTO dto = new LabelDTO();
                        string qr1 = receive.InboundOrder.MaterialCode.PadRight(len) + " " + runningNumber + " " + Helper.FormatThousand(receive.QtyPerBag).PadLeft(6) + " " + receive.LotNo;
                        string qrImg1 = GenerateQRCode(qr1);
                        
                        dto.Field3 = Domain + "/" + qrImg1;

                        string inDate = "";
                        string inDate2 = "";
                        string inDate3 = "";
                        string expiredDate = "";
                        string expiredDate2 = "";

                        DateTime dt = receive.InDate.Value;
                        dto.Field4 = dt.ToString("MMMM").ToUpper();
                        inDate = dt.ToString("yyyyMMdd").Substring(1);
                        inDate2 = dt.ToString("yyyMMdd");
                        inDate2 = inDate2.Substring(1);
                        inDate3 = dt.ToString("yyyy-MM-dd");

                        DateTime dt2 = receive.ExpDate.Value;
                        expiredDate = dt2.ToString("yyyyMMdd").Substring(2);
                        expiredDate2 = dt2.ToString("yyyy-MM-dd");


                        string qr2 = receive.InboundOrder.MaterialCode.PadRight(len) + inDate + expiredDate;
                        string qrImg2 = GenerateQRCode(qr2);
                        dto.Field5 = receive.LotNo;
                        dto.Field6 = Domain + "/" + qrImg2;
                        dto.Field7 = Maker;
                        dto.Field8 = receive.InboundOrder.MaterialName;
                        dto.Field9 = Helper.FormatThousand(receive.QtyPerBag);
                        dto.Field10 = "KG".ToUpper();
                        dto.Field11 = inDate2;
                        dto.Field12 = receive.InboundOrder.MaterialCode;
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

    }
}
