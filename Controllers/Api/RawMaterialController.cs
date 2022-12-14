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

namespace WMS_BE.Controllers.Api
{
    public class RawMaterialController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Datatable()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<RawMaterial> list = Enumerable.Empty<RawMaterial>();
            IEnumerable<RawMaterialDTO> pagedData = Enumerable.Empty<RawMaterialDTO>();

            IQueryable<RawMaterial> query = db.RawMaterials.AsQueryable();

            int recordsTotal = db.RawMaterials.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        //|| m.Qty.Contains(search)
                        //|| m.UoM.Contains(search)
                        //|| m.ShelfLife.Contains(search)
                        //|| m.OrderedQty.Contains(search)
                        //|| m.TransportingMode.Contains(search)
                        //|| m.MinPurchaseQty.Contains(search)
                        //|| m.Origin.Contains(search)
                        //|| m.Maker.Contains(search)
                        //|| m.Vendor.Contains(search)
                        //|| m.Msds.Contains(search)
                        //|| m.PoRate.Contains(search)
                        //|| m.SpecGravity.Contains(search)
                        //|| m.Factor.Contains(search)
                        //|| m.Hygroscopic.Contains(search)
                        //|| m.Online.Contains(search)
                        //|| m.Kwori.Contains(search)
                        //|| m.Type.Contains(search)
                        //|| m.Aman.Contains(search)
                        //|| m.SafetyStock.Contains(search)
                        //|| m.ArrivalDays.Contains(search)
                        //|| m.Specification.Contains(search)
                        );

                Dictionary<string, Func<RawMaterial, object>> cols = new Dictionary<string, Func<RawMaterial, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ShelfLife", x => x.ShelfLife);
                cols.Add("MinPurchaseQty", x => x.MinPurchaseQty);
                cols.Add("Maker", x => x.Maker);
                cols.Add("Vendor", x => x.Vendor);
                cols.Add("PoRate", x => x.PoRate);
                cols.Add("ManfCode", x => x.ManfCd);
                cols.Add("VendorCode", x => x.VendorCode);
                cols.Add("IsActive", x => x.IsActive);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                   

                    pagedData = from x in list
                                select new RawMaterialDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    UoM = x.UoM,
                                    ShelfLife = x.ShelfLife,
                                    MinPurchaseQty = Helper.FormatThousand(x.MinPurchaseQty),
                                    Maker = x.Maker,
                                    Vendor = x.Vendor,
                                    PoRate = Helper.FormatThousand2(x.PoRate),
                                    ManfCd = x.ManfCd,
                                    VendorCode = x.VendorCode,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = x.ModifiedOn.ToString()
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
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

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Upload()
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
                    if (request.Files.Count > 0)
                    {
                        HttpPostedFile file = request.Files[0];

                        if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                        {
                            if (file.ContentLength < (10 * 1024 * 1024))
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


                                foreach (DataRow row in dt.AsEnumerable().Skip(1))
                                {
                                    RawMaterial rawMaterial = new RawMaterial();
                                    //rawMaterial.ID = Helper.CreateGuid("RM");
                                    rawMaterial.MaterialCode = row[2].ToString();
                                    rawMaterial.MaterialName = row[3].ToString();
                                    rawMaterial.Qty = string.IsNullOrEmpty(row[4].ToString()) ? 0 : decimal.Parse(row[4].ToString());
                                    rawMaterial.UoM = "KG";
                                    rawMaterial.ShelfLife = row[6].ToString();
                                    rawMaterial.MinPurchaseQty = string.IsNullOrEmpty(row[9].ToString()) ? 0 : decimal.Parse(row[9].ToString());
                                    rawMaterial.Maker = string.IsNullOrEmpty(row[13].ToString()) ? "" : row[13].ToString();
                                    rawMaterial.Vendor = string.IsNullOrEmpty(row[14].ToString()) ? "" : row[14].ToString();
                                    rawMaterial.PoRate = string.IsNullOrEmpty(row[24].ToString()) ? 0 : decimal.Parse(row[24].ToString());
                                    rawMaterial.ManfCd = string.IsNullOrEmpty(row[33].ToString()) ? "" : row[33].ToString();
                                    rawMaterial.VendorCode = string.IsNullOrEmpty(row[34].ToString()) ? "" : row[34].ToString();

                                    rawMaterial.IsActive = true;
                                    rawMaterial.CreatedBy = activeUser;
                                    rawMaterial.CreatedOn = DateTime.Now;

                                    RawMaterial raw = db.RawMaterials.Where(m => m.MaterialCode.Equals(rawMaterial.MaterialCode)).FirstOrDefault();
                                    if (raw != null)
                                    {
                                        raw.MaterialName = rawMaterial.MaterialName;
                                        raw.Qty = rawMaterial.Qty;
                                        raw.UoM = rawMaterial.UoM;
                                        raw.ShelfLife = rawMaterial.ShelfLife;
                                        raw.MinPurchaseQty = rawMaterial.MinPurchaseQty;
                                        raw.Maker = rawMaterial.Maker;
                                        raw.Vendor = rawMaterial.Vendor;
                                        raw.PoRate = rawMaterial.PoRate;
                                        raw.ManfCd = rawMaterial.ManfCd;
                                        raw.VendorCode = rawMaterial.VendorCode;
                                        raw.IsActive = rawMaterial.IsActive;
                                        raw.ModifiedBy = activeUser;
                                        raw.ModifiedOn = DateTime.Now;
                                    }
                                    else
                                    {
                                        db.RawMaterials.Add(rawMaterial);
                                    }

                                }

                                await db.SaveChangesAsync();
                                message = "Upload succeeded.";
                                status = true;
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
                message = string.Format("Upload item failed. {0}", ex.Message);
            }


            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(RawMaterialVM rawMaterialVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string id = null;

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
                    if (!string.IsNullOrEmpty(rawMaterialVM.MaterialCode))
                    {
                        RawMaterial temp = await db.RawMaterials.Where(s => s.MaterialCode.ToLower().Equals(rawMaterialVM.MaterialCode.ToLower())).FirstOrDefaultAsync();

                        if (temp != null)
                        {
                            ModelState.AddModelError("RawMaterial.MaterialCode", "Raw Material Code is already registered.");
                        }

                        if(rawMaterialVM.MaterialCode.Length > 7)
                        {
                            ModelState.AddModelError("RawMaterial.MaterialCode", "Raw Material Code cannot more than 7 characters.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("RawMaterial.MaterialCode", "Raw Material Code is required.");
                    }

                    if (string.IsNullOrEmpty(rawMaterialVM.MaterialName))
                    {
                        ModelState.AddModelError("RawMaterial.MaterialName", "Raw Material Name is required.");
                    }

                    if (rawMaterialVM.Qty <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.Qty", "Qty can not below zero.");
                    }

                    if (rawMaterialVM.ShelfLife <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.ShelfLife", "Shelf Life can not below zero.");
                    }

                    if (string.IsNullOrEmpty(rawMaterialVM.LifeRange))
                    {
                        ModelState.AddModelError("RawMaterial.LifeRange", "Range is required.");
                    }
                    else
                    {
                        if (!rawMaterialVM.LifeRange.Equals("y") && !rawMaterialVM.LifeRange.Equals("m") && !rawMaterialVM.LifeRange.Equals("d"))
                        {
                            ModelState.AddModelError("RawMaterial.LifeRange", "Range is not recognized.");
                        }
                    }

                    if (string.IsNullOrEmpty(rawMaterialVM.Maker))
                    {
                        ModelState.AddModelError("RawMaterial.Maker", "Maker Name is required.");
                    }

                    if (rawMaterialVM.MinPurchaseQty <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.MinPurchaseQty", "Minimum Purchase Qty can not below zero.");
                    }

                    if (rawMaterialVM.PoRate <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.PoRate", "PO Rate can not below zero.");
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    RawMaterial rawMaterial = new RawMaterial();
                    rawMaterial.MaterialCode = rawMaterialVM.MaterialCode;
                    rawMaterial.MaterialName = rawMaterialVM.MaterialName;
                    rawMaterial.Qty = rawMaterialVM.Qty;
                    rawMaterial.UoM = "KG";

                    string LifeRange = "";
                    if (rawMaterialVM.LifeRange.Equals("m"))
                    {
                        LifeRange = "Month";

                    }else if (rawMaterialVM.LifeRange.Equals("d"))
                    {
                        LifeRange = "Day";
                    }
                    else if (rawMaterialVM.LifeRange.Equals("y"))
                    {
                        LifeRange = "Year";
                    }

                    if (rawMaterialVM.ShelfLife > 1)
                    {
                        LifeRange += "s";
                    }

                    rawMaterial.ShelfLife = string.Format("{0} {1}", rawMaterialVM.ShelfLife, LifeRange);
                    rawMaterial.MinPurchaseQty = rawMaterialVM.MinPurchaseQty;
                    rawMaterial.Maker = rawMaterialVM.Maker;
                    rawMaterial.Vendor = rawMaterialVM.Vendor;
                    rawMaterial.PoRate = rawMaterialVM.PoRate;
                    rawMaterial.ManfCd = rawMaterialVM.ManfCd;
                    rawMaterial.VendorCode = rawMaterialVM.VendorCode;

                    rawMaterial.IsActive = true;
                    rawMaterial.CreatedBy = activeUser;
                    rawMaterial.CreatedOn = DateTime.Now;


                    db.RawMaterials.Add(rawMaterial);
                    await db.SaveChangesAsync();
                    status = true;
                    message = "Create data succeeded.";
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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Update(RawMaterialVM rawMaterialVM)
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

                    if (string.IsNullOrEmpty(rawMaterialVM.MaterialCode))
                    {
                        throw new Exception("Material Code is required.");
                    }

                    RawMaterial rawMaterial = await db.RawMaterials.Where(s => s.MaterialCode.Equals(rawMaterialVM.MaterialCode)).FirstOrDefaultAsync();

                    if(rawMaterial == null)
                    {
                        throw new Exception("Material Code is not recognized.");
                    }
                    //if (!string.IsNullOrEmpty(rawMaterialVM.MaterialName))
                    //{
                    //    RawMaterial temp = await db.RawMaterials.Where(s => s.MaterialName.ToLower().Equals(rawMaterialVM.MaterialName.ToLower()) && !s.MaterialCode.Equals(rawMaterialVM.MaterialCode)).FirstOrDefaultAsync();

                    //    if (temp != null)
                    //    {
                    //        ModelState.AddModelError("RawMaterial.Name", "Raw Material Name is already registered.");

                    //    }
                    //}

                    if (string.IsNullOrEmpty(rawMaterialVM.MaterialName))
                    {
                        ModelState.AddModelError("RawMaterial.MaterialName", "Raw Material Name is required.");
                    }

                    if (rawMaterialVM.Qty <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.Qty", "Qty can not below zero.");
                    }

                    if (rawMaterialVM.ShelfLife <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.ShelfLife", "Shelf Life can not below zero.");
                    }

                    if (string.IsNullOrEmpty(rawMaterialVM.LifeRange))
                    {
                        ModelState.AddModelError("RawMaterial.LifeRange", "Range is required.");
                    }
                    else
                    {
                        if (!rawMaterialVM.LifeRange.Equals("y") && !rawMaterialVM.LifeRange.Equals("m") && !rawMaterialVM.LifeRange.Equals("d"))
                        {
                            ModelState.AddModelError("RawMaterial.LifeRange", "Range is not recognized.");
                        }
                    }

                    if (string.IsNullOrEmpty(rawMaterialVM.Maker))
                    {
                        ModelState.AddModelError("RawMaterial.Maker", "Maker Name is required.");
                    }

                    if (rawMaterialVM.MinPurchaseQty <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.MinPurchaseQty", "Minimum Purchase Qty can not below zero.");
                    }

                    if (rawMaterialVM.PoRate <= 0)
                    {
                        ModelState.AddModelError("RawMaterial.PoRate", "PO Rate can not below zero.");
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    

                    if (rawMaterial != null)
                    {
                        rawMaterial.MaterialName = rawMaterialVM.MaterialName;
                        rawMaterial.Qty = rawMaterialVM.Qty;
                        rawMaterial.UoM = "KG";
                        rawMaterial.MinPurchaseQty = rawMaterialVM.MinPurchaseQty;
                        rawMaterial.Maker = rawMaterialVM.Maker;
                        rawMaterial.Vendor = rawMaterialVM.Vendor;
                        rawMaterial.PoRate = rawMaterialVM.PoRate;
                        rawMaterial.ManfCd = rawMaterialVM.ManfCd;
                        rawMaterial.VendorCode = rawMaterialVM.VendorCode;
                        //rawMaterial.IsActive = rawMaterialVM.IsActive;

                        rawMaterial.ModifiedBy = activeUser;
                        rawMaterial.ModifiedOn = DateTime.Now;

                        string LifeRange = "";
                        if (rawMaterialVM.LifeRange.Equals("m"))
                        {
                            LifeRange = "Month";

                        }
                        else if (rawMaterialVM.LifeRange.Equals("d"))
                        {
                            LifeRange = "Day";
                        }
                        else if (rawMaterialVM.LifeRange.Equals("y"))
                        {
                            LifeRange = "Year";
                        }

                        if (rawMaterialVM.ShelfLife > 1)
                        {
                            LifeRange += "s";
                        }

                        rawMaterial.ShelfLife = string.Format("{0} {1}", rawMaterialVM.ShelfLife, LifeRange);

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Update data succeeded.";
                    }
                    else
                    {
                        message = "Raw Material does not exist.";
                    }
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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            RawMaterialDTO rawMaterialDTO = null;

            try
            {
                RawMaterial x = await db.RawMaterials.Where(m => m.MaterialCode.Equals(id)).FirstOrDefaultAsync();

                if(x == null)
                {
                    throw new Exception("Data not found.");
                }

                string LifeRange = Regex.Replace(x.ShelfLife, @"[\d-]", string.Empty).ToString();

                if (LifeRange.ToLower().Contains("month")){
                    LifeRange = "m";
                }
                else
                {
                    LifeRange = "d";
                }
                var ShelfLife = Regex.Match(x.ShelfLife, @"\d+").Value;
                rawMaterialDTO = new RawMaterialDTO
                {
                    MaterialCode = x.MaterialCode,
                    MaterialName = x.MaterialName,
                    Qty = Helper.FormatThousand(x.Qty),
                    UoM = x.UoM,
                    ShelfLife = ShelfLife,
                    LifeRange = LifeRange,
                    MinPurchaseQty = Helper.FormatThousand(x.MinPurchaseQty),
                    Maker = x.Maker,
                    Vendor = x.Vendor,
                    PoRate = Helper.FormatThousand2(x.PoRate),
                    ManfCd = x.ManfCd,
                    VendorCode = x.VendorCode,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn.ToString(),
                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                    ModifiedOn = x.ModifiedOn.ToString()
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

            obj.Add("data", rawMaterialDTO);
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