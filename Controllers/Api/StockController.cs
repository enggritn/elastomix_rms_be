using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;

namespace WMS_BE.Controllers.Api
{
    public class StockController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> UploadOpeningBalance()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string MaterialCode = "";

            try
            {
                if (request.Files.Count > 0)
                {
                    HttpPostedFile file = request.Files[0];

                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                DataTable dt = result.Tables[0];


                                //string area1 = "AR003";
                                //string area2 = "AR001";

                                DateTime curTime = DateTime.Now;


                                foreach (DataRow row in dt.AsEnumerable().Skip(8))
                                {
                                    if(string.IsNullOrEmpty(row[1].ToString()) || string.IsNullOrEmpty(row[2].ToString()))
                                    {
                                        continue;
                                    }
                                    
                                    //check to master Raw Material
                                    string AreaCode = row[1].ToString();
                                    string RackCode = row[2].ToString();

                                    BinRackArea area = db.BinRackAreas.Where(m => m.Code.Equals(AreaCode)).FirstOrDefault();

                                    string BinRackCode = string.Format("{0}{1}", AreaCode, RackCode);
                                    //create binrack
                                    BinRack binRack = db.BinRacks.Where(m => m.BinRackAreaCode.Equals(AreaCode) && m.Name.Equals(RackCode)).FirstOrDefault();
                                    if(binRack == null)
                                    {
                                        binRack = new BinRack
                                        {
                                            ID = Helper.CreateGuid("BIN"),
                                            Code = BinRackCode,
                                            Name = Helper.ToUpper(RackCode),
                                            WarehouseCode = area.WarehouseCode,
                                            WarehouseName = area.WarehouseName,
                                            BinRackAreaID = area.ID,
                                            BinRackAreaCode = area.Code,
                                            BinRackAreaName = area.Name,
                                            IsActive = true,
                                            CreatedBy = "Back-end",
                                            CreatedOn = DateTime.Now
                                        };

                                        db.BinRacks.Add(binRack);
                                        await db.SaveChangesAsync();
                                    }

                                    if (string.IsNullOrEmpty(row[3].ToString()))
                                    {
                                        continue;
                                    }

                                    string RMCode = row[3].ToString();

                                    MaterialCode = RMCode;
                                    RawMaterial rawMaterial = db.RawMaterials.Where(m => m.MaterialCode.Equals(RMCode)).FirstOrDefault();
                                    if (rawMaterial != null)
                                    {
                                        
                                        string inDate = row[5].ToString();
                                        string expDate = row[6].ToString();
                                        string lotNo = row[7].ToString();

                                        decimal FullBag = string.IsNullOrEmpty(row[10].ToString()) ? 0 : decimal.Parse(row[10].ToString());
                                        decimal Remainder = string.IsNullOrEmpty(row[12].ToString()) ? 0 : decimal.Parse(row[12].ToString());
                                      
                                        StockRM stockRM = new StockRM();
                                        stockRM.ID = Helper.CreateGuid("S");

                                        MaterialCode += string.Format(" {0}-{1}-{2}-{3}-{4}", inDate, expDate, lotNo, FullBag, Remainder);

                                        decimal QtyPerBag = 0;

                                        if (rawMaterial.MaterialCode.Equals("DCHINA"))
                                        {
                                            QtyPerBag = 25;
                                        }
                                        else
                                        {
                                            QtyPerBag = rawMaterial.Qty;
                                        }                                        

                                        if (!string.IsNullOrEmpty(inDate))
                                        {
                                            try
                                            {
                                                stockRM.InDate = Convert.ToDateTime(inDate);

                                                if (string.IsNullOrEmpty(lotNo) || lotNo.Equals("-"))
                                                {
                                                    lotNo = stockRM.InDate.Value.ToString("yyyMMdd").Substring(1);
                                                }
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }

                                        if (!string.IsNullOrEmpty(expDate))
                                        {
                                            try
                                            {
                                                stockRM.ExpiredDate = Convert.ToDateTime(expDate);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }

                                        if (FullBag > 0)
                                        {
                                            //create barcode
                                            string StockCode = Helper.StockCode(rawMaterial.MaterialCode, rawMaterial.Qty, lotNo, stockRM.InDate.Value, stockRM.ExpiredDate.Value);

                                            stockRM.MaterialCode = rawMaterial.MaterialCode;
                                            stockRM.MaterialName = rawMaterial.MaterialName;
                                            stockRM.Code = StockCode;
                                            stockRM.LotNumber = lotNo;
                                            stockRM.Quantity = FullBag * QtyPerBag;
                                            stockRM.QtyPerBag = QtyPerBag;
                                            stockRM.BinRackID = binRack.ID;
                                            stockRM.BinRackCode = binRack.Code;
                                            stockRM.BinRackName = binRack.Name;
                                            stockRM.ReceivedAt = curTime;

                                            db.StockRMs.Add(stockRM);

                                            //add to Log Print RM
                                            LogPrintRM logPrintRM = new LogPrintRM();
                                            logPrintRM.ID = Helper.CreateGuid("LOG");
                                            logPrintRM.Remarks = "Opening Balance";
                                            logPrintRM.StockCode = StockCode;
                                            logPrintRM.MaterialCode = stockRM.MaterialCode;
                                            logPrintRM.MaterialName = stockRM.MaterialName;
                                            logPrintRM.LotNumber = stockRM.LotNumber;
                                            logPrintRM.InDate = stockRM.InDate.Value;
                                            logPrintRM.ExpiredDate = stockRM.ExpiredDate.Value;
                                            logPrintRM.StartSeries = 1001;
                                            logPrintRM.LastSeries = 1001 + Convert.ToInt32(FullBag);
                                            logPrintRM.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRM);
                                        }

                                        if (Remainder > 0)
                                        {
                                            StockRM stockRemain = new StockRM();
                                            stockRemain.ID = Helper.CreateGuid("S");
                                            string StockCode = Helper.StockCode(rawMaterial.MaterialCode, Remainder, lotNo, stockRM.InDate.Value, stockRM.ExpiredDate.Value);
                                            stockRemain.MaterialCode = rawMaterial.MaterialCode;
                                            stockRemain.MaterialName = rawMaterial.MaterialName;
                                            stockRemain.LotNumber = lotNo;
                                            stockRemain.InDate = stockRM.InDate;
                                            stockRemain.ExpiredDate = stockRM.ExpiredDate;
                                            stockRemain.QtyPerBag = Remainder;
                                            stockRemain.Quantity = Remainder;
                                            stockRemain.Code = StockCode;
                                            stockRemain.BinRackID = binRack.ID;
                                            stockRemain.BinRackCode = binRack.Code;
                                            stockRemain.BinRackName = binRack.Name;
                                            stockRemain.ReceivedAt = curTime;
                                            db.StockRMs.Add(stockRemain);

                                            LogPrintRM logPrintRemainder = new LogPrintRM();
                                            logPrintRemainder.ID = Helper.CreateGuid("LOG");
                                            logPrintRemainder.Remarks = "Opening Balance";
                                            logPrintRemainder.StockCode = StockCode;
                                            logPrintRemainder.MaterialCode = rawMaterial.MaterialCode;
                                            logPrintRemainder.MaterialName = rawMaterial.MaterialName;
                                            logPrintRemainder.LotNumber = lotNo;
                                            logPrintRemainder.InDate = stockRM.InDate.Value;
                                            logPrintRemainder.ExpiredDate = stockRM.ExpiredDate.Value;
                                            logPrintRemainder.StartSeries = 1001;
                                            logPrintRemainder.LastSeries = 1001;
                                            logPrintRemainder.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRemainder);
                                        }                 
                                    }

                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;

                            }
                            catch (Exception e)
                            {
                                message = string.Format("Upload item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else if (file != null && Path.GetExtension(file.FileName).ToLower() == ".csv")
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                //logic insertion here
                                StreamReader sr = new StreamReader(file.InputStream, Encoding.Default);
                                string results = sr.ReadToEnd();
                                sr.Close();

                                string[] row = results.Split('\n');

                                message = "Upload succeeded.";
                                status = true;

                            }
                            catch (Exception)
                            {
                                message = "Upload item failed";
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else
                    {
                        message = "Upload item failed. File is invalid.";
                    }
                }
                else
                {
                    message = "No file uploaded.";
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

            obj.Add("MaterialCode", MaterialCode);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UploadOpeningBalanceSFG()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string MaterialCode = "";
            try
            {
                if (request.Files.Count > 0)
                {
                    HttpPostedFile file = request.Files[0];

                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                DataTable dt = result.Tables[0];

                                //string area1 = "AR003";
                                //string area2 = "AR001";

                                DateTime curTime = DateTime.Now;

                                foreach (DataRow row in dt.AsEnumerable().Skip(8))
                                {
                                    if (string.IsNullOrEmpty(row[1].ToString()) || string.IsNullOrEmpty(row[2].ToString()))
                                    {
                                        continue;
                                    }
                                    //check to master Raw Material
                                    string AreaCode = row[1].ToString();
                                    string RackCode = row[2].ToString();

                                    BinRackArea area = db.BinRackAreas.Where(m => m.Code.Equals(AreaCode)).FirstOrDefault();

                                    string BinRackCode = string.Format("{0}{1}", AreaCode, RackCode);
                                    //create binrack
                                    BinRack binRack = db.BinRacks.Where(m => m.BinRackAreaCode.Equals(AreaCode) && m.Name.Equals(RackCode)).FirstOrDefault();
                                    if (binRack == null)
                                    {
                                        binRack = new BinRack
                                        {
                                            ID = Helper.CreateGuid("BIN"),
                                            Code = BinRackCode,
                                            Name = Helper.ToUpper(RackCode),
                                            WarehouseCode = area.WarehouseCode,
                                            WarehouseName = area.WarehouseName,
                                            BinRackAreaID = area.ID,
                                            BinRackAreaCode = area.Code,
                                            BinRackAreaName = area.Name,
                                            IsActive = true,
                                            CreatedBy = "Back-end",
                                            CreatedOn = DateTime.Now
                                        };

                                        db.BinRacks.Add(binRack);
                                        await db.SaveChangesAsync();
                                    }

                                    if (string.IsNullOrEmpty(row[5].ToString()))
                                    {
                                        continue;
                                    }

                                    MaterialCode = row[5].ToString();

                                    SemiFinishGood sfg = db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefault();
                                    if (sfg != null)
                                    {
                                        if (sfg.AB.Equals("B"))
                                        {
                                            continue;
                                        }

                                        string lotNo = row[6].ToString();
                                        DateTime InDate = DateTime.ParseExact(lotNo, "yyMMdd", CultureInfo.InvariantCulture);

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

                                        DateTime ExpDate = InDate.AddDays(days);


                                        decimal TotalQty = string.IsNullOrEmpty(row[9].ToString()) ? 0 : decimal.Parse(row[9].ToString());
                                        decimal remainderQty = 0;
                                        int fullBagQty = 0;

                                        if (sfg.WeightPerBag > 0)
                                        {
                                            //count full bag and remainder
                                            remainderQty = TotalQty % sfg.WeightPerBag;
                                            fullBagQty = Convert.ToInt32(Math.Floor(TotalQty / sfg.WeightPerBag));
                                        }
                                        else
                                        {
                                            remainderQty = TotalQty;
                                        }                                      

                                        StockSFG stock = new StockSFG();
                                        stock.ID = Helper.CreateGuid("S");

                                        stock.QtyPerBag = sfg.WeightPerBag;
                                        stock.InDate = InDate;
                                        stock.ExpiredDate = ExpDate;

                                        //create barcode
                                        string StockCode = Helper.StockCode(sfg.MaterialCode, sfg.WeightPerBag, lotNo, stock.InDate.Value, stock.ExpiredDate.Value);

                                        stock.MaterialCode = sfg.MaterialCode;
                                        stock.MaterialName = sfg.MaterialName;
                                        stock.Code = StockCode;
                                        stock.LotNumber = lotNo;
                                        stock.Quantity = fullBagQty * sfg.WeightPerBag;
                                        stock.QtyPerBag = sfg.WeightPerBag;
                                        stock.BinRackID = binRack.ID;
                                        stock.BinRackCode = binRack.Code;
                                        stock.BinRackName = binRack.Name;
                                        stock.ReceivedAt = curTime;

                                        if(fullBagQty > 0)
                                        {
                                            db.StockSFGs.Add(stock);
                                            //add to Log Print RM
                                            LogPrintRM logPrintRM = new LogPrintRM();
                                            logPrintRM.ID = Helper.CreateGuid("LOG");
                                            logPrintRM.Remarks = "Opening Balance";
                                            logPrintRM.StockCode = StockCode;
                                            logPrintRM.MaterialCode = stock.MaterialCode;
                                            logPrintRM.MaterialName = stock.MaterialName;
                                            logPrintRM.LotNumber = stock.LotNumber;
                                            logPrintRM.InDate = stock.InDate.Value;
                                            logPrintRM.ExpiredDate = stock.ExpiredDate.Value;
                                            logPrintRM.StartSeries = 1001;
                                            logPrintRM.LastSeries = 1001 + Convert.ToInt32(fullBagQty);
                                            logPrintRM.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRM);
                                        }


                                        if (remainderQty > 0)
                                        {
                                            StockSFG stockRemain = new StockSFG();
                                            stockRemain.ID = Helper.CreateGuid("S");
                                            StockCode = Helper.StockCode(sfg.MaterialCode, remainderQty, lotNo, stock.InDate.Value, stock.ExpiredDate.Value);
                                            stockRemain.MaterialCode = stock.MaterialCode;
                                            stockRemain.MaterialName = stock.MaterialName;
                                            stockRemain.LotNumber = stock.LotNumber;
                                            stockRemain.InDate = stock.InDate;
                                            stockRemain.ExpiredDate = stock.ExpiredDate;
                                            stockRemain.QtyPerBag = remainderQty;
                                            stockRemain.Quantity = remainderQty;
                                            stockRemain.Code = StockCode;
                                            stockRemain.BinRackID = binRack.ID;
                                            stockRemain.BinRackCode = binRack.Code;
                                            stockRemain.BinRackName = binRack.Name;
                                            stockRemain.ReceivedAt = curTime;
                                            db.StockSFGs.Add(stockRemain);

                                            LogPrintRM logPrintRemainder = new LogPrintRM();
                                            logPrintRemainder.ID = Helper.CreateGuid("LOG");
                                            logPrintRemainder.Remarks = "Opening Balance";
                                            logPrintRemainder.StockCode = StockCode;
                                            logPrintRemainder.MaterialCode = stock.MaterialCode;
                                            logPrintRemainder.MaterialName = stock.MaterialName;
                                            logPrintRemainder.LotNumber = stock.LotNumber;
                                            logPrintRemainder.InDate = stock.InDate.Value;
                                            logPrintRemainder.ExpiredDate = stock.ExpiredDate.Value;
                                            logPrintRemainder.StartSeries = 1001;
                                            logPrintRemainder.LastSeries = 1001;
                                            logPrintRemainder.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRemainder);
                                        }
                                    }
                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception e)
                            {
                                message = string.Format("Upload item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else if (file != null && Path.GetExtension(file.FileName).ToLower() == ".csv")
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                //logic insertion here
                                StreamReader sr = new StreamReader(file.InputStream, Encoding.Default);
                                string results = sr.ReadToEnd();
                                sr.Close();

                                string[] row = results.Split('\n');

                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception)
                            {
                                message = "Upload item failed";
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else
                    {
                        message = "Upload item failed. File is invalid.";
                    }
                }
                else
                {
                    message = "No file uploaded.";
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

            obj.Add("MaterialCode", MaterialCode);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetListBarcode(string MaterialCode = "", string WarehouseCode = "")
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<ActualStockDTO> list = Enumerable.Empty<ActualStockDTO>();

            try
            {
                IQueryable<vStockAll> query = db.vStockAlls.AsQueryable();

                if (!string.IsNullOrEmpty(MaterialCode))
                {
                    query = query.Where(m => m.MaterialCode.Equals(MaterialCode));
                }

                if (!string.IsNullOrEmpty(WarehouseCode))
                {
                    query = query.Where(m => m.WarehouseCode.Equals(WarehouseCode));
                }

                list = from x in query.ToList()
                            select new ActualStockDTO
                            {
                                Barcode = x.Code,
                                LotNo = x.LotNumber,
                                BinRackCode = x.BinRackCode,
                                BinRackName = x.BinRackName,
                                BinRackAreaCode = x.BinRackAreaCode,
                                BinRackAreaName = x.BinRackAreaName,
                                WarehouseCode = x.WarehouseCode,
                                WarehouseName = x.WarehouseName,
                                MaterialCode = x.MaterialCode,
                                MaterialName = x.MaterialName,
                                InDate = Helper.NullDateToString(x.InDate),
                                ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                BagQty = Helper.FormatThousand(x.BagQty),
                                QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                IsExpired = x.ExpiredDate.HasValue ? DateTime.Now.Date >= x.ExpiredDate.Value.Date : false,
                                BarcodeLeft = Helper.BarcodeLeft(x.Type.Equals("RM") ? x.MaterialCode.PadRight(7) : x.MaterialCode.PadRight(7), x.InDate, x.ExpiredDate),
                                BarcodeRight = Helper.BarcodeRight(x.Type.Equals("RM") ? x.MaterialCode.PadRight(7) : x.MaterialCode.PadRight(7), string.Format("{0:D5}", 1), Helper.FormatThousand(x.QtyPerBag).PadLeft(6), x.LotNumber)
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
        public async Task<IHttpActionResult> UploadStockOutsource()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            try
            {
                if (request.Files.Count > 0)
                {
                    HttpPostedFile file = request.Files[0];

                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                DataTable dt = result.Tables[0];

                                DateTime curTime = DateTime.Now;

                                foreach (DataRow row in dt.AsEnumerable().Skip(1))
                                {
                                    if (string.IsNullOrEmpty(row[1].ToString()) || string.IsNullOrEmpty(row[2].ToString()))
                                    {
                                        continue;
                                    }
                                    //check to master Raw Material
                                    string WarehouseCode = row[0].ToString();

                                    BinRack binRack = db.BinRacks.Where(m => m.WarehouseCode.Equals(WarehouseCode)).FirstOrDefault();
                                    if (binRack == null)
                                    {
                                        break;
                                    }

                                    string MaterialCode = row[1].ToString();                                  

                                    vProductMaster rawMaterial = db.vProductMasters.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefault();
                                    if (rawMaterial != null)
                                    {

                                        decimal TotalQty = string.IsNullOrEmpty(row[3].ToString()) ? 0 : decimal.Parse(row[3].ToString());

                                        StockRM stockRM = new StockRM();
                                        stockRM.ID = Helper.CreateGuid("S");

                                        decimal QtyPerBag = rawMaterial.QtyPerBag;

                                        stockRM.InDate = null;
                                        stockRM.ExpiredDate = null;

                                        int totalFullBag = Convert.ToInt32(Math.Floor(TotalQty / rawMaterial.QtyPerBag));
                                        decimal RemainderQty = TotalQty % rawMaterial.QtyPerBag;

                                        //create barcode
                                        string StockCode = Helper.StockCode(rawMaterial.MaterialCode, rawMaterial.QtyPerBag, null, stockRM.InDate, stockRM.ExpiredDate);

                                        stockRM.MaterialCode = rawMaterial.MaterialCode;
                                        stockRM.MaterialName = rawMaterial.MaterialName;
                                        stockRM.Code = StockCode;
                                        stockRM.LotNumber = "";
                                        stockRM.Quantity = totalFullBag;
                                        stockRM.QtyPerBag = QtyPerBag;
                                        stockRM.BinRackID = binRack.ID;
                                        stockRM.BinRackCode = binRack.Code;
                                        stockRM.BinRackName = binRack.Name;
                                        stockRM.ReceivedAt = curTime;

                                        db.StockRMs.Add(stockRM);

                                        if (RemainderQty > 0)
                                        {
                                            StockRM stockRemain = new StockRM();
                                            stockRemain.ID = Helper.CreateGuid("S");
                                            StockCode = Helper.StockCode(rawMaterial.MaterialCode, RemainderQty, "", stockRM.InDate, stockRM.ExpiredDate);
                                            stockRemain.MaterialCode = stockRM.MaterialCode;
                                            stockRemain.MaterialName = stockRM.MaterialName;
                                            stockRemain.LotNumber = stockRM.LotNumber;
                                            stockRemain.InDate = stockRM.InDate;
                                            stockRemain.ExpiredDate = stockRM.ExpiredDate;
                                            stockRemain.QtyPerBag = RemainderQty;
                                            stockRemain.Quantity = RemainderQty;
                                            stockRemain.Code = StockCode;
                                            stockRemain.BinRackID = binRack.ID;
                                            stockRemain.BinRackCode = binRack.Code;
                                            stockRemain.BinRackName = binRack.Name;
                                            stockRemain.ReceivedAt = curTime;
                                            db.StockRMs.Add(stockRemain);
                                        }
                                    }
                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception e)
                            {
                                message = string.Format("Upload item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else if (file != null && Path.GetExtension(file.FileName).ToLower() == ".csv")
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                //logic insertion here
                                StreamReader sr = new StreamReader(file.InputStream, Encoding.Default);
                                string results = sr.ReadToEnd();
                                sr.Close();

                                string[] row = results.Split('\n');

                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception)
                            {
                                message = "Upload item failed";
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else
                    {
                        message = "Upload item failed. File is invalid.";
                    }
                }
                else
                {
                    message = "No file uploaded.";
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

        public async Task<IHttpActionResult> UploadOpeningBalanceNew()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string MaterialCode = "";

            try
            {
                if (request.Files.Count > 0)
                {
                    HttpPostedFile file = request.Files[0];

                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                DataTable dt = result.Tables[0];


                                //string area1 = "AR003";
                                //string area2 = "AR001";

                                DateTime curTime = DateTime.Now;
                                int days = 1;
                                DateTime awalbulan = new DateTime(DateTime.Now.Year, DateTime.Now.Month, days);

                                foreach (DataRow row in dt.AsEnumerable().Skip(8))
                                {
                                    if (string.IsNullOrEmpty(row[1].ToString()) || string.IsNullOrEmpty(row[2].ToString()))
                                    {
                                        continue;
                                    }

                                    //check to master Raw Material
                                    string AreaCode = row[1].ToString();
                                    string RackCode = row[2].ToString();

                                    BinRackArea area = db.BinRackAreas.Where(m => m.Code.Equals(AreaCode)).FirstOrDefault();

                                    string BinRackCode = string.Format("{0}{1}", AreaCode, RackCode);
                                    //create binrack
                                    BinRack binRack = db.BinRacks.Where(m => m.BinRackAreaCode.Equals(AreaCode) && m.Name.Equals(RackCode)).FirstOrDefault();
                                    if (binRack == null)
                                    {
                                        binRack = new BinRack
                                        {
                                            ID = Helper.CreateGuid("BIN"),
                                            Code = BinRackCode,
                                            Name = Helper.ToUpper(RackCode),
                                            WarehouseCode = area.WarehouseCode,
                                            WarehouseName = area.WarehouseName,
                                            BinRackAreaID = area.ID,
                                            BinRackAreaCode = area.Code,
                                            BinRackAreaName = area.Name,
                                            IsActive = true,
                                            CreatedBy = "Back-end",
                                            CreatedOn = DateTime.Now
                                        };

                                        db.BinRacks.Add(binRack);
                                        await db.SaveChangesAsync();
                                    }



                                    if (string.IsNullOrEmpty(row[3].ToString()))
                                    {
                                        continue;
                                    }

                                    string RMCode = row[3].ToString();

                                    MaterialCode = RMCode;
                                    RawMaterial rawMaterial = db.RawMaterials.Where(m => m.MaterialCode.Equals(RMCode)).FirstOrDefault();
                                    if (rawMaterial != null)
                                    {

                                        string inDate = row[5].ToString();
                                        string expDate = row[6].ToString();
                                        string lotNo = row[7].ToString();

                                        decimal FullBag = string.IsNullOrEmpty(row[10].ToString()) ? 0 : decimal.Parse(row[10].ToString());
                                        decimal Remainder = string.IsNullOrEmpty(row[12].ToString()) ? 0 : decimal.Parse(row[12].ToString());

                                        StockRM stockRM = new StockRM();
                                        stockRM.ID = Helper.CreateGuid("S");

                                        MaterialCode += string.Format(" {0}-{1}-{2}-{3}-{4}", inDate, expDate, lotNo, FullBag, Remainder);

                                        decimal QtyPerBag = 0;

                                        if (rawMaterial.MaterialCode.Equals("DCHINA"))
                                        {
                                            QtyPerBag = 25;
                                        }
                                        else
                                        {
                                            QtyPerBag = rawMaterial.Qty;
                                        }

                                        if (!string.IsNullOrEmpty(inDate))
                                        {
                                            try
                                            {
                                                stockRM.InDate = Convert.ToDateTime(inDate);

                                                if (string.IsNullOrEmpty(lotNo) || lotNo.Equals("-"))
                                                {
                                                    lotNo = stockRM.InDate.Value.ToString("yyyMMdd").Substring(1);
                                                }
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }

                                        if (!string.IsNullOrEmpty(expDate))
                                        {
                                            try
                                            {
                                                stockRM.ExpiredDate = Convert.ToDateTime(expDate);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }

                                        if (FullBag > 0)
                                        {
                                            //create barcode
                                            string StockCode = Helper.StockCode(rawMaterial.MaterialCode, rawMaterial.Qty, lotNo, stockRM.InDate.Value, stockRM.ExpiredDate.Value);

                                            // Cek Issue Slip Bulan berjalan
                                            // Cek history picking bulan berjalan s/d upload opening balance, berdasar material code, lotnumber, indate, expdate, binrackcode, qty data opening melebihi qty picking
                                            // Adjust stock actual = Data opening balance -  history picking                                                                                      

                                            IQueryable<IssueSlipPicking> query = query = db.IssueSlipPickings.Where(m => m.StockCode.Equals(StockCode) && m.QtyPerBag.Equals(rawMaterial.Qty) && m.BinRackCode.Equals(binRack.Code) && m.PickedOn <= DateTime.Now && m.IssueSlipOrder.IssueSlipHeader.ProductionDate >= awalbulan && m.IssueSlipOrder.IssueSlipHeader.ProductionDate <= DateTime.Now).AsQueryable();
                                            IEnumerable<IssueSlipPicking> tempList = await query.ToListAsync();
                                            
                                            decimal sisa = 0;
                                            foreach (IssueSlipPicking isPicking in tempList)
                                            {
                                                if (sisa > 0)
                                                {
                                                    sisa = sisa - isPicking.BagQty;
                                                }
                                                else
                                                {
                                                    sisa = FullBag - isPicking.BagQty;
                                                }
                                            }

                                            if (sisa == 0)
                                            {
                                                sisa = FullBag;
                                            }

                                            stockRM.MaterialCode = rawMaterial.MaterialCode;
                                            stockRM.MaterialName = rawMaterial.MaterialName;
                                            stockRM.Code = StockCode;
                                            stockRM.LotNumber = lotNo;
                                            stockRM.Quantity = sisa * QtyPerBag;
                                            stockRM.QtyPerBag = QtyPerBag;
                                            stockRM.BinRackID = binRack.ID;
                                            stockRM.BinRackCode = binRack.Code;
                                            stockRM.BinRackName = binRack.Name;
                                            stockRM.ReceivedAt = curTime;

                                            db.StockRMs.Add(stockRM);

                                            //add to Log Print RM
                                            LogPrintRM logPrintRM = new LogPrintRM();
                                            logPrintRM.ID = Helper.CreateGuid("LOG");
                                            logPrintRM.Remarks = "Opening Balance";
                                            logPrintRM.StockCode = StockCode;
                                            logPrintRM.MaterialCode = stockRM.MaterialCode;
                                            logPrintRM.MaterialName = stockRM.MaterialName;
                                            logPrintRM.LotNumber = stockRM.LotNumber;
                                            logPrintRM.InDate = stockRM.InDate.Value;
                                            logPrintRM.ExpiredDate = stockRM.ExpiredDate.Value;
                                            logPrintRM.StartSeries = 1001;
                                            logPrintRM.LastSeries = 1001 + Convert.ToInt32(sisa);
                                            logPrintRM.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRM);
                                        }

                                        if (Remainder > 0)
                                        {
                                            StockRM stockRemain = new StockRM();
                                            stockRemain.ID = Helper.CreateGuid("S");
                                            string StockCode = Helper.StockCode(rawMaterial.MaterialCode, Remainder, lotNo, stockRM.InDate.Value, stockRM.ExpiredDate.Value);
                                            stockRemain.MaterialCode = rawMaterial.MaterialCode;
                                            stockRemain.MaterialName = rawMaterial.MaterialName;
                                            stockRemain.LotNumber = lotNo;
                                            stockRemain.InDate = stockRM.InDate.Value;
                                            stockRemain.ExpiredDate = stockRM.ExpiredDate.Value;
                                            stockRemain.QtyPerBag = Remainder;
                                            stockRemain.Quantity = Remainder;
                                            stockRemain.Code = StockCode;
                                            stockRemain.BinRackID = binRack.ID;
                                            stockRemain.BinRackCode = binRack.Code;
                                            stockRemain.BinRackName = binRack.Name;
                                            stockRemain.ReceivedAt = curTime;
                                            db.StockRMs.Add(stockRemain);

                                            LogPrintRM logPrintRemainder = new LogPrintRM();
                                            logPrintRemainder.ID = Helper.CreateGuid("LOG");
                                            logPrintRemainder.Remarks = "Opening Balance";
                                            logPrintRemainder.StockCode = StockCode;
                                            logPrintRemainder.MaterialCode = rawMaterial.MaterialCode;
                                            logPrintRemainder.MaterialName = rawMaterial.MaterialName;
                                            logPrintRemainder.LotNumber = lotNo;
                                            logPrintRemainder.InDate = stockRM.InDate.Value;
                                            logPrintRemainder.ExpiredDate = stockRM.ExpiredDate.Value;
                                            logPrintRemainder.StartSeries = 1001;
                                            logPrintRemainder.LastSeries = 1001;
                                            logPrintRemainder.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRemainder);
                                        }
                                    }

                                    // Adjust stock rm lama, quantity => 0 sebelum bulan berjalan atau sebelum tanggal 1 tiap bulannya.
                                    // IQueryable<StockRM> query1 = query1 = db.StockRMs.Where(m => m.ReceivedAt < awalbulan && m.Quantity > 0).AsQueryable();
                                    IQueryable<StockRM> query1 = query1 = db.StockRMs.Where(m => m.ReceivedAt < awalbulan && m.Quantity > 0 && (m.MaterialCode.Equals(rawMaterial.MaterialCode))).AsQueryable();
                                    IEnumerable<StockRM> tempList1 = await query1.ToListAsync();

                                    foreach (StockRM stockrm in tempList1)
                                    {
                                        stockrm.Quantity = 0;
                                    }

                                    // Closed oustanding QC Inspection, berdasar material code, lotnumber, indate, expdate, binrackcode, qty
                                    // IQueryable<QCInspection> query2 = query2 = db.QCInspections.Where(m => m.CreatedOn < awalbulan && (m.TransactionStatus.Equals("OPEN") || m.TransactionStatus.Equals("PROGRESS"))).AsQueryable();
                                    IQueryable<QCInspection> query2 = query2 = db.QCInspections.Where(m => m.CreatedOn < awalbulan && m.MaterialCode.Equals(rawMaterial.MaterialCode) && (m.TransactionStatus.Equals("OPEN") || m.TransactionStatus.Equals("PROGRESS"))).AsQueryable();
                                    IEnumerable<QCInspection> tempList2 = await query2.ToListAsync();

                                    foreach (QCInspection qcinspect in tempList2)
                                    {
                                        qcinspect.TransactionStatus = "CLOSED";
                                        qcinspect.InspectionStatus = "WAITING";
                                    }
                                }
                                

                                // Cek Issue Slip Bulan berjalan
                                // Cek history return yang sudah putaway bulan berjalan s/d tanggal 1 tiap bulannya
                                // Tambah stock RM dari history return yang sudah putaway bulan berjalan s/d tanggal 1 tiap bulannya
                                IQueryable<IssueSlipPutaway> query3 = query3 = db.IssueSlipPutaways.Where(m => m.PutOn < awalbulan && m.IssueSlipOrder.IssueSlipHeader.ProductionDate >= awalbulan && m.IssueSlipOrder.IssueSlipHeader.ProductionDate <= DateTime.Now).AsQueryable();
                                IEnumerable<IssueSlipPutaway> tempList3 = await query3.ToListAsync();

                                foreach (IssueSlipPutaway isputaway in tempList3)
                                {
                                    StockRM stockRM1 = new StockRM();
                                    stockRM1.ID = Helper.CreateGuid("S");
                                    stockRM1.MaterialCode = isputaway.IssueSlipOrder.MaterialCode;                                   
                                    stockRM1.MaterialName = isputaway.IssueSlipOrder.MaterialName;
                                    stockRM1.Code = isputaway.StockCode;
                                    stockRM1.LotNumber = isputaway.LotNo; 
                                    stockRM1.InDate = isputaway.InDate;
                                    stockRM1.ExpiredDate = isputaway.ExpDate;
                                    stockRM1.Quantity = isputaway.PutawayQty;
                                    stockRM1.QtyPerBag = isputaway.QtyPerBag;
                                    stockRM1.BinRackID = isputaway.BinRackID;
                                    stockRM1.BinRackCode = isputaway.BinRackCode;
                                    stockRM1.BinRackName = isputaway.BinRackName;
                                    stockRM1.ReceivedAt = curTime;

                                    db.StockRMs.Add(stockRM1);

                                    //add to Log Print RM
                                    LogPrintRM logPrintRM1 = new LogPrintRM();
                                    logPrintRM1.ID = Helper.CreateGuid("LOG");
                                    logPrintRM1.Remarks = "Opening Balance";
                                    logPrintRM1.StockCode = isputaway.StockCode;
                                    logPrintRM1.MaterialCode = stockRM1.MaterialCode;
                                    logPrintRM1.MaterialName = stockRM1.MaterialName;
                                    logPrintRM1.LotNumber = isputaway.LotNo;
                                    logPrintRM1.InDate = isputaway.InDate;
                                    logPrintRM1.ExpiredDate = isputaway.ExpDate;
                                    logPrintRM1.StartSeries = 1001;
                                    logPrintRM1.LastSeries = 1001 + Convert.ToInt32(isputaway.PutawayQty / isputaway.QtyPerBag);
                                    logPrintRM1.PrintDate = DateTime.Now;

                                    db.LogPrintRMs.Add(logPrintRM1);
                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception e)
                            {
                                message = string.Format("Upload item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else if (file != null && Path.GetExtension(file.FileName).ToLower() == ".csv")
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                //logic insertion here
                                StreamReader sr = new StreamReader(file.InputStream, Encoding.Default);
                                string results = sr.ReadToEnd();
                                sr.Close();

                                string[] row = results.Split('\n');

                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception)
                            {
                                message = "Upload item failed";
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else
                    {
                        message = "Upload item failed. File is invalid.";
                    }
                }
                else
                {
                    message = "No file uploaded.";
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

            obj.Add("MaterialCode", MaterialCode);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UploadOpeningBalanceSFGNew()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string MaterialCode = "";
            try
            {
                if (request.Files.Count > 0)
                {
                    HttpPostedFile file = request.Files[0];

                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                Stream stream = file.InputStream;
                                IExcelDataReader reader = null;
                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                {
                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                }
                                else
                                {
                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                }

                                DataSet result = reader.AsDataSet();
                                reader.Close();

                                DataTable dt = result.Tables[0];

                                //string area1 = "AR003";
                                //string area2 = "AR001";

                                DateTime curTime = DateTime.Now;

                                foreach (DataRow row in dt.AsEnumerable().Skip(8))
                                {
                                    if (string.IsNullOrEmpty(row[1].ToString()) || string.IsNullOrEmpty(row[2].ToString()))
                                    {
                                        continue;
                                    }
                                    //check to master Raw Material
                                    string AreaCode = row[1].ToString();
                                    string RackCode = row[2].ToString();

                                    BinRackArea area = db.BinRackAreas.Where(m => m.Code.Equals(AreaCode)).FirstOrDefault();

                                    string BinRackCode = string.Format("{0}{1}", AreaCode, RackCode);
                                    //create binrack
                                    BinRack binRack = db.BinRacks.Where(m => m.BinRackAreaCode.Equals(AreaCode) && m.Name.Equals(RackCode)).FirstOrDefault();
                                    if (binRack == null)
                                    {
                                        binRack = new BinRack
                                        {
                                            ID = Helper.CreateGuid("BIN"),
                                            Code = BinRackCode,
                                            Name = Helper.ToUpper(RackCode),
                                            WarehouseCode = area.WarehouseCode,
                                            WarehouseName = area.WarehouseName,
                                            BinRackAreaID = area.ID,
                                            BinRackAreaCode = area.Code,
                                            BinRackAreaName = area.Name,
                                            IsActive = true,
                                            CreatedBy = "Back-end",
                                            CreatedOn = DateTime.Now
                                        };

                                        db.BinRacks.Add(binRack);
                                        await db.SaveChangesAsync();
                                    }

                                    if (string.IsNullOrEmpty(row[5].ToString()))
                                    {
                                        continue;
                                    }

                                    MaterialCode = row[5].ToString();

                                    SemiFinishGood sfg = db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(MaterialCode)).FirstOrDefault();
                                    if (sfg != null)
                                    {
                                        if (sfg.AB.Equals("B"))
                                        {
                                            continue;
                                        }

                                        string lotNo = row[6].ToString();
                                        DateTime InDate = DateTime.ParseExact(lotNo, "yyMMdd", CultureInfo.InvariantCulture);

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

                                        DateTime ExpDate = InDate.AddDays(days);


                                        decimal TotalQty = string.IsNullOrEmpty(row[9].ToString()) ? 0 : decimal.Parse(row[9].ToString());
                                        decimal remainderQty = 0;
                                        int fullBagQty = 0;

                                        if (sfg.WeightPerBag > 0)
                                        {
                                            //count full bag and remainder
                                            remainderQty = TotalQty % sfg.WeightPerBag;
                                            fullBagQty = Convert.ToInt32(Math.Floor(TotalQty / sfg.WeightPerBag));
                                        }
                                        else
                                        {
                                            remainderQty = TotalQty;
                                        }

                                        StockSFG stock = new StockSFG();
                                        stock.ID = Helper.CreateGuid("S");

                                        stock.QtyPerBag = sfg.WeightPerBag;
                                        stock.InDate = InDate;
                                        stock.ExpiredDate = ExpDate;

                                        //create barcode
                                        string StockCode = Helper.StockCode(sfg.MaterialCode, sfg.WeightPerBag, lotNo, stock.InDate.Value, stock.ExpiredDate.Value);

                                        stock.MaterialCode = sfg.MaterialCode;
                                        stock.MaterialName = sfg.MaterialName;
                                        stock.Code = StockCode;
                                        stock.LotNumber = lotNo;
                                        stock.Quantity = fullBagQty * sfg.WeightPerBag;
                                        stock.QtyPerBag = sfg.WeightPerBag;
                                        stock.BinRackID = binRack.ID;
                                        stock.BinRackCode = binRack.Code;
                                        stock.BinRackName = binRack.Name;
                                        stock.ReceivedAt = curTime;

                                        if (fullBagQty > 0)
                                        {
                                            db.StockSFGs.Add(stock);
                                            //add to Log Print RM
                                            LogPrintRM logPrintRM = new LogPrintRM();
                                            logPrintRM.ID = Helper.CreateGuid("LOG");
                                            logPrintRM.Remarks = "Opening Balance";
                                            logPrintRM.StockCode = StockCode;
                                            logPrintRM.MaterialCode = stock.MaterialCode;
                                            logPrintRM.MaterialName = stock.MaterialName;
                                            logPrintRM.LotNumber = stock.LotNumber;
                                            logPrintRM.InDate = stock.InDate.Value;
                                            logPrintRM.ExpiredDate = stock.ExpiredDate.Value;
                                            logPrintRM.StartSeries = 1001;
                                            logPrintRM.LastSeries = 1001 + Convert.ToInt32(fullBagQty);
                                            logPrintRM.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRM);
                                        }


                                        if (remainderQty > 0)
                                        {
                                            StockSFG stockRemain = new StockSFG();
                                            stockRemain.ID = Helper.CreateGuid("S");
                                            StockCode = Helper.StockCode(sfg.MaterialCode, remainderQty, lotNo, stock.InDate.Value, stock.ExpiredDate.Value);
                                            stockRemain.MaterialCode = stock.MaterialCode;
                                            stockRemain.MaterialName = stock.MaterialName;
                                            stockRemain.LotNumber = stock.LotNumber;
                                            stockRemain.InDate = stock.InDate;
                                            stockRemain.ExpiredDate = stock.ExpiredDate;
                                            stockRemain.QtyPerBag = remainderQty;
                                            stockRemain.Quantity = remainderQty;
                                            stockRemain.Code = StockCode;
                                            stockRemain.BinRackID = binRack.ID;
                                            stockRemain.BinRackCode = binRack.Code;
                                            stockRemain.BinRackName = binRack.Name;
                                            stockRemain.ReceivedAt = curTime;
                                            db.StockSFGs.Add(stockRemain);

                                            LogPrintRM logPrintRemainder = new LogPrintRM();
                                            logPrintRemainder.ID = Helper.CreateGuid("LOG");
                                            logPrintRemainder.Remarks = "Opening Balance";
                                            logPrintRemainder.StockCode = StockCode;
                                            logPrintRemainder.MaterialCode = stock.MaterialCode;
                                            logPrintRemainder.MaterialName = stock.MaterialName;
                                            logPrintRemainder.LotNumber = stock.LotNumber;
                                            logPrintRemainder.InDate = stock.InDate.Value;
                                            logPrintRemainder.ExpiredDate = stock.ExpiredDate.Value;
                                            logPrintRemainder.StartSeries = 1001;
                                            logPrintRemainder.LastSeries = 1001;
                                            logPrintRemainder.PrintDate = DateTime.Now;

                                            db.LogPrintRMs.Add(logPrintRemainder);
                                        }
                                    }
                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception e)
                            {
                                message = string.Format("Upload item failed. {0}", e.Message);
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else if (file != null && Path.GetExtension(file.FileName).ToLower() == ".csv")
                    {
                        if (file.ContentLength < (10 * 1024 * 1024))
                        {
                            try
                            {
                                //logic insertion here
                                StreamReader sr = new StreamReader(file.InputStream, Encoding.Default);
                                string results = sr.ReadToEnd();
                                sr.Close();

                                string[] row = results.Split('\n');

                                message = "Upload succeeded.";
                                status = true;
                            }
                            catch (Exception)
                            {
                                message = "Upload item failed";
                            }
                        }
                        else
                        {
                            message = "Upload failed. Maximum allowed file size : 10MB ";
                        }
                    }
                    else
                    {
                        message = "Upload item failed. File is invalid.";
                    }
                }
                else
                {
                    message = "No file uploaded.";
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

            obj.Add("MaterialCode", MaterialCode);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
