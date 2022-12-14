using ExcelDataReader;
using System;
using System.Collections.Generic;
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

namespace WMS_BE.Controllers.Api
{
    public class InboundManualController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpGet]
        public async Task<IHttpActionResult> Datatable()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<ManualStock> list = Enumerable.Empty<ManualStock>();
            IEnumerable<InboundManualVM> pagedData = Enumerable.Empty<InboundManualVM>();

            IQueryable<ManualStock> query = db.ManualStocks.AsQueryable();
          
            try
            {
                list = await query.ToListAsync();
                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new InboundManualVM
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    Name = x.Name,
                                    InDate = Helper.NullDateToString2(x.InDate),
                                    ExpiredDate = Helper.NullDateToString2(x.ExpiredDate),
                                    Lot = x.Lot,
                                    BagQty = x.BagQty.ToString(),
                                    FullBagQty = x.FullBagQty.ToString(),
                                    FullQty = x.FullQty.ToString(),
                                    RemainQty = x.RemainQty.ToString(),
                                    TotalQty = x.TotalQty.ToString(),
                                    UoM = x.UoM
                                };
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

            obj.Add("list", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Upload()
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


                                foreach (DataRow row in dt.Rows)
                                {
                                    if (dt.Rows.IndexOf(row) != 0)
                                    {
                                        //check to master Raw Material
                                        string RMCode = row[0].ToString();
                                        RawMaterial rawMaterial = db.RawMaterials.Where(m => m.MaterialCode.Equals(RMCode)).FirstOrDefault();
                                        if(rawMaterial != null)
                                        {
                                            ManualStock manualStock = new ManualStock();
                                            manualStock.ID = Helper.CreateGuid("IM");
                                            manualStock.Code = rawMaterial.MaterialCode;
                                            manualStock.Name = rawMaterial.MaterialName;

                                            manualStock.Lot = row[2].ToString();


                                            string inDate = row[1].ToString();
                                            if (!string.IsNullOrEmpty(inDate))
                                            {
                                                try
                                                {
                                                    manualStock.InDate = Convert.ToDateTime(inDate);
                                                    int days = (Convert.ToInt32(rawMaterial.ShelfLife[0].ToString()) * 30) - 1;
                                                    manualStock.ExpiredDate = manualStock.InDate.Value.AddDays(days);

                                                    if (string.IsNullOrEmpty(manualStock.Lot) || manualStock.Lot.Equals("-"))
                                                    {
                                                        manualStock.Lot = manualStock.InDate.Value.ToString("yyyMMdd").Substring(1);
                                                    }
                                                    
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }

                                            
                                            manualStock.BagQty = rawMaterial.Qty;
                                            manualStock.FullBagQty = string.IsNullOrEmpty(row[3].ToString()) ? 0 : decimal.Parse(row[3].ToString());
                                            manualStock.FullQty = string.IsNullOrEmpty(row[4].ToString()) ? 0 : decimal.Parse(row[4].ToString());
                                            manualStock.RemainQty = string.IsNullOrEmpty(row[5].ToString()) ? 0 : decimal.Parse(row[5].ToString());
                                            manualStock.TotalQty = (manualStock.BagQty * manualStock.FullBagQty) + manualStock.FullQty + manualStock.RemainQty;
                                            manualStock.UoM = "KG";
                                            


                                            IQueryable<ManualStock> query = db.ManualStocks.Where(m => m.Code.Equals(manualStock.Code) && DbFunctions.TruncateTime(m.InDate.Value) == DbFunctions.TruncateTime(manualStock.InDate.Value) && m.Lot.Equals(manualStock.Lot));
                                            ManualStock sm = await query.FirstOrDefaultAsync();
                                            if (sm != null)
                                            {
                                                sm.Name = manualStock.Name;
                                                sm.FullBagQty = manualStock.FullBagQty;
                                                sm.FullQty = manualStock.FullQty;
                                                sm.RemainQty = manualStock.RemainQty;
                                                sm.TotalQty = manualStock.TotalQty;
                                                sm.UoM = manualStock.UoM;
                                            }
                                            else
                                            {
                                                manualStock.Seq = 0;
                                                db.ManualStocks.Add(manualStock);
                                            }
                                        }


                                        


                                        //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [ManualStock]");
                                        //db.ManualStocks.Add(manualStock);

                                        //IQueryable<ManualStock> query = db.ManualStocks.Where(m => m.Code.Equals(manualStock.Code));

                                        //if (manualStock.InDate.HasValue)
                                        //{
                                        //    query = query.Where(m => m.InDate.Value.Equals(manualStock.InDate.Value));
                                        //}

                                        //if (manualStock.ExpiredDate.HasValue)
                                        //{
                                        //    query = query.Where(m => m.ExpiredDate.Value.Equals(manualStock.ExpiredDate.Value));
                                        //}

                                        //ManualStock sm = await query.FirstOrDefaultAsync();
                                        //if (sm != null)
                                        //{
                                        //    sm.Name = manualStock.Name;
                                        //    sm.InDate = manualStock.InDate;
                                        //    sm.ExpiredDate = manualStock.ExpiredDate;
                                        //    sm.Lot = manualStock.Lot;
                                        //    sm.BagQty = manualStock.BagQty;
                                        //    sm.FullBagQty = manualStock.FullBagQty;
                                        //    sm.FullQty = manualStock.FullQty;
                                        //    sm.RemainQty = manualStock.RemainQty;
                                        //    sm.TotalQty = manualStock.TotalQty;
                                        //    sm.UoM = manualStock.UoM;
                                        //}
                                        //else
                                        //{
                                        //    db.ManualStocks.Add(manualStock);
                                        //}

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

        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            InboundManualVM view = new InboundManualVM();
           
            try
            {
                ManualStock manualStock = await db.ManualStocks.FindAsync(id);
                if(manualStock != null)
                {
                    RawMaterial rawMaterial = await db.RawMaterials.Where(m => m.MaterialCode.Equals(manualStock.Code)).FirstOrDefaultAsync();
                    view.ID = manualStock.ID;
                    view.Code = manualStock.Code;
                    view.Name = manualStock.Name;
                    view.InDate = Helper.NullDateToString2(manualStock.InDate);
                    view.ExpiredDate = Helper.NullDateToString2(manualStock.ExpiredDate);
                    view.Lot = manualStock.Lot;
                    view.BagQty = manualStock.BagQty.ToString();
                    view.FullBagQty = manualStock.FullBagQty.ToString();
                    view.FullQty = manualStock.FullQty.ToString();
                    view.RemainQty = manualStock.RemainQty.ToString();
                    view.TotalQty = manualStock.TotalQty.ToString();
                    view.UoM = manualStock.UoM;
                    view.MakerName = rawMaterial != null ? rawMaterial.Maker : "NO DATA";
                    view.PrintDate = manualStock.PrintDate.ToString();
                    view.Sequence = manualStock.Seq;

                    view.ShelfLife = !string.IsNullOrEmpty(rawMaterial.ShelfLife) ? Convert.ToInt32(rawMaterial.ShelfLife[0].ToString()) : 0;


                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Fetch data failed.";
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

            obj.Add("data", view);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> GetSeqNoById(InboundManualVM data, int fullBag)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            InboundManualVM view = new InboundManualVM();

            try
            {
                DateTime in_date = DateTime.ParseExact(data.InDate, "dd/MM/yyyy", null);
                ManualStock manualStock = await db.ManualStocks.FindAsync(data.ID);
                if (manualStock != null)
                {
                    RawMaterial rawMaterial = await db.RawMaterials.Where(m => m.MaterialCode.Equals(manualStock.Code)).FirstOrDefaultAsync();
                    view.ID = manualStock.ID;
                    view.Code = manualStock.Code;
                    view.Name = manualStock.Name;
                    view.InDate = Helper.NullDateToString2(manualStock.InDate);
                    view.ExpiredDate = Helper.NullDateToString2(manualStock.ExpiredDate);
                    view.Lot = manualStock.Lot;
                    view.BagQty = manualStock.BagQty.ToString();
                    view.FullBagQty = manualStock.FullBagQty.ToString();
                    view.FullQty = manualStock.FullQty.ToString();
                    view.RemainQty = manualStock.RemainQty.ToString();
                    view.TotalQty = manualStock.TotalQty.ToString();
                    view.UoM = manualStock.UoM;
                    view.MakerName = rawMaterial != null ? rawMaterial.Maker : "NO DATA";
                    manualStock.PrintDate = DateTime.Now;

                    view.PrintDate = manualStock.PrintDate.ToString();


                    ManualStock inboundManual = await db.ManualStocks.Where(m => m.Code.Equals(manualStock.Code) && DbFunctions.TruncateTime(m.InDate.Value) == DbFunctions.TruncateTime(in_date) && m.Lot.Equals(data.Lot)).FirstOrDefaultAsync();

                    //reset if indate & lotnumber difference
                    if (inboundManual != null)
                    {
                        manualStock.Seq += fullBag;
                        view.Sequence = manualStock.Seq;
                    }
                    else
                    {
                        //insert new row
                        //manualStock.Seq = fullBag;
                        ManualStock ms = new ManualStock();
                        ms.ID = Helper.CreateGuid("IM");
                        ms.Code = rawMaterial.MaterialCode;
                        ms.Name = rawMaterial.MaterialName;
                        ms.InDate = in_date;
                        int days = (Convert.ToInt32(rawMaterial.ShelfLife[0].ToString()) * 30) - 1;
                        ms.ExpiredDate = ms.InDate.Value.AddDays(days);

                        ms.Lot = data.Lot;
                        ms.BagQty = rawMaterial.Qty;
                        ms.FullBagQty = string.IsNullOrEmpty(data.FullBagQty) ? 0 : decimal.Parse(data.FullBagQty);
                        ms.FullQty = string.IsNullOrEmpty(data.FullQty) ? 0 : decimal.Parse(data.FullQty);
                        ms.RemainQty = string.IsNullOrEmpty(data.RemainQty) ? 0 : decimal.Parse(data.RemainQty);
                        ms.TotalQty = (ms.BagQty * ms.FullBagQty) + ms.FullQty + ms.RemainQty;
                        ms.UoM = "KG";
                        ms.Seq = fullBag;

                        db.ManualStocks.Add(ms);

                        view.Sequence = ms.Seq;

                    }

                    //if (manualStock.PrintDate != null)
                    //{
                    //    if(manualStock.PrintDate.Value.Date != DateTime.Now.Date)
                    //    {
                    //        manualStock.Seq = fullBag;
                    //    }
                    //}

                   

                    //IEnumerable<ManualStock> manualStocks = await db.ManualStocks.Where(m => m.Code.Equals(manualStock.Code)).ToListAsync();
                    //foreach(ManualStock mat in manualStocks)
                    //{
                    //    mat.Seq = manualStock.Seq;
                    //    mat.PrintDate = manualStock.PrintDate;
                    //}

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "Fetch data failed.";
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
                //message = ex.Message;
                message = data.InDate;
            }

            obj.Add("data", view);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}