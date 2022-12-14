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
    public class SemiFinishGoodController : ApiController
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

            IEnumerable<SemiFinishGood> list = Enumerable.Empty<SemiFinishGood>();
            IEnumerable<SemiFinishGoodDTO> pagedData = Enumerable.Empty<SemiFinishGoodDTO>();

            IQueryable<SemiFinishGood> query = db.SemiFinishGoods.AsQueryable();

            int recordsTotal = db.SemiFinishGoods.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.StockCategoryName.Contains(search)
                        || m.AB.Contains(search)
                        || m.UoM.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<SemiFinishGood, object>> cols = new Dictionary<string, Func<SemiFinishGood, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("StockCategoryName", x => x.StockCategoryName);
                cols.Add("CustomerProductName", x => x.CustProdName);
                cols.Add("ExpiredDate", x => x.ExpiredDate);
                cols.Add("AB", x => x.AB);
                cols.Add("WeightPerBag", x => x.WeightPerBag);
                cols.Add("PerPalletWeight", x => x.PerPalletWeight);
                cols.Add("UoM", x => x.UoM);
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
                                select new SemiFinishGoodDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    StockCategoryName = x.StockCategoryName,
                                    CustomerProductName = x.CustProdName,
                                    ExpiredDate = x.ExpiredDate,
                                    AB = x.AB,
                                    WeightPerBag = Helper.FormatThousand(x.WeightPerBag),
                                    PerPalletWeight = Helper.FormatThousand(x.PerPalletWeight),
                                    UoM = x.UoM,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
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

                                    //string prefix = "SFG";

                                    //string lastNumber = db.SemiFinishGoods.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    //int currentNumber = 0;

                                    //if (!string.IsNullOrEmpty(lastNumber))
                                    //{
                                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 5));
                                    //}

                                    //currentNumber++;

                                    foreach (DataRow row in dt.AsEnumerable().Skip(1))
                                    {
                                        SemiFinishGood semiFinishGood = new SemiFinishGood();
                                        semiFinishGood.MaterialCode = string.IsNullOrEmpty(row[0].ToString()) ? "" : row[0].ToString();
                                        if (!string.IsNullOrEmpty(semiFinishGood.MaterialCode))
                                        {
                                            semiFinishGood.MaterialName = string.IsNullOrEmpty(row[1].ToString()) ? "" : row[1].ToString();
                                            semiFinishGood.StockCategoryName = string.IsNullOrEmpty(row[5].ToString()) ? "" : row[5].ToString();
                                            semiFinishGood.CustProdName = string.IsNullOrEmpty(row[6].ToString()) ? "" : row[6].ToString();
                                            semiFinishGood.ExpiredDate = string.IsNullOrEmpty(row[9].ToString()) ? "" : row[9].ToString();
                                            semiFinishGood.AB = string.IsNullOrEmpty(row[11].ToString()) ? "" : row[11].ToString();
                                            semiFinishGood.WeightPerBag = string.IsNullOrEmpty(row[19].ToString()) ? 0 : decimal.Parse(row[19].ToString());
                                            semiFinishGood.PerPalletWeight = string.IsNullOrEmpty(row[20].ToString()) ? 0 : decimal.Parse(row[20].ToString());
                                            semiFinishGood.UoM = "KG";
                                            semiFinishGood.IsActive = true;

                                            string mixingDiff = row[24].ToString();

                                            //A, S -> SFG | M, R, B -> FG
                                            if (mixingDiff.Equals("A") || mixingDiff.Equals("S"))
                                            {
                                                SemiFinishGood sfg = db.SemiFinishGoods.Where(m => m.MaterialName.Equals(semiFinishGood.MaterialName)).FirstOrDefault();
                                                if (sfg != null)
                                                {
                                                    sfg.MaterialName = semiFinishGood.MaterialName;
                                                    sfg.StockCategoryName = semiFinishGood.StockCategoryName;
                                                    sfg.CustProdName = semiFinishGood.CustProdName;
                                                    sfg.ExpiredDate = semiFinishGood.ExpiredDate;
                                                    sfg.AB = semiFinishGood.AB;
                                                    sfg.WeightPerBag = semiFinishGood.WeightPerBag;
                                                    sfg.PerPalletWeight = semiFinishGood.PerPalletWeight;
                                                    sfg.UoM = semiFinishGood.UoM;
                                                    sfg.ModifiedBy = activeUser;
                                                    sfg.ModifiedOn = DateTime.Now;
                                                }
                                                else
                                                {
                                                    //semiFinishGood.ID = Helper.CreateGuid(prefix);
                                                    //semiFinishGood.Code = prefix + string.Format("{0:D5}", currentNumber++);
                                                    semiFinishGood.IsActive = true;
                                                    semiFinishGood.CreatedBy = activeUser;
                                                    semiFinishGood.CreatedOn = DateTime.Now;

                                                    db.SemiFinishGoods.Add(semiFinishGood);
                                                }
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
                message = ex.Message;
            }


            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(SemiFinishGoodVM semiFinishGoodVM)
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
                    if (string.IsNullOrEmpty(semiFinishGoodVM.MaterialCode))
                    {
                        ModelState.AddModelError("SemiFinishGood.MaterialCode", "Product Code is required.");
                    }
                    else
                    {
                        SemiFinishGood temp = await db.SemiFinishGoods.Where(s => s.MaterialCode.ToLower().Equals(semiFinishGoodVM.MaterialCode.ToLower())).FirstOrDefaultAsync();

                        if (temp != null)
                        {
                            ModelState.AddModelError("SemiFinishGood.MaterialCode", "Product Code is already registered.");
                        }
                    }

                    if (string.IsNullOrEmpty(semiFinishGoodVM.MaterialName))
                    {
                        ModelState.AddModelError("SemiFinishGood.MaterialName", "Product Name is required.");
                    }
                    //else
                    //{
                    //    SemiFinishGood temp = await db.SemiFinishGoods.Where(s => s.MaterialName.ToUpper().Equals(semiFinishGoodVM.MaterialName.ToUpper()) && !s.MaterialCode.Equals(semiFinishGoodVM.MaterialCode)).FirstOrDefaultAsync();

                    //    if (temp != null)
                    //    {
                    //        ModelState.AddModelError("SemiFinishGood.MaterialName", "Stock Name is already registered.");
                    //    }
                    //}


                    if (semiFinishGoodVM.WeightPerBag <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.WeightPerBag", "Weight/Bag can not below zero.");
                    }

                    if (semiFinishGoodVM.PerPalletWeight <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.PerPalletWeight", "Weight/Pallet can not below zero.");
                    }

                    if (semiFinishGoodVM.ExpiredDate <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.ExpiredDate", "Expired can not below zero.");
                    }

                    if (string.IsNullOrEmpty(semiFinishGoodVM.LifeRange))
                    {
                        ModelState.AddModelError("SemiFinishGood.LifeRange", "Range is required.");
                    }
                    else
                    {
                        if (!semiFinishGoodVM.LifeRange.Equals("m") && !semiFinishGoodVM.LifeRange.Equals("d") && !semiFinishGoodVM.LifeRange.Equals("y"))
                        {
                            ModelState.AddModelError("SemiFinishGood.LifeRange", "Life Range is not recognized.");
                        }
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

                    SemiFinishGood sfg = new SemiFinishGood();

                    //string prefix = "SFG";

                    //string lastNumber = db.SemiFinishGoods.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 5));
                    //}

                    //sfg.ID = Helper.CreateGuid(prefix);
                    //sfg.Code = prefix + string.Format("{0:D5}", currentNumber + 1);
                    sfg.MaterialCode = semiFinishGoodVM.MaterialCode;
                    sfg.MaterialName = semiFinishGoodVM.MaterialName;
                    sfg.StockCategoryName = semiFinishGoodVM.StockCategoryName;
                    sfg.CustProdName = semiFinishGoodVM.CustomerProductName;
                    sfg.AB = semiFinishGoodVM.AB;

                    string LifeRange = "";
                    if (semiFinishGoodVM.LifeRange.Equals("m"))
                    {
                        LifeRange = "Month";

                    }
                    else if (semiFinishGoodVM.LifeRange.Equals("d"))
                    {
                        LifeRange = "Day";
                    }
                    else if (semiFinishGoodVM.LifeRange.Equals("y"))
                    {
                        LifeRange = "Year";
                    }

                    if (semiFinishGoodVM.ExpiredDate > 1)
                    {
                        LifeRange += "s";
                    }

                    sfg.ExpiredDate = string.Format("{0} {1}", semiFinishGoodVM.ExpiredDate, LifeRange);

                    sfg.WeightPerBag = semiFinishGoodVM.WeightPerBag;
                    sfg.PerPalletWeight = semiFinishGoodVM.PerPalletWeight;
                    sfg.UoM = "KG";
                    sfg.IsActive = true;
                    sfg.CreatedBy = activeUser;
                    sfg.CreatedOn = DateTime.Now;

                    //id = sfg.ID;

                    db.SemiFinishGoods.Add(sfg);

                    await db.SaveChangesAsync();
                    message = "Create data succeeded.";
                    status = true;
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
        public async Task<IHttpActionResult> Update(SemiFinishGoodVM semiFinishGoodVM)
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

                    if (string.IsNullOrEmpty(semiFinishGoodVM.MaterialCode))
                    {
                        throw new Exception("Material Code is required.");
                    }


                    SemiFinishGood sfg = await db.SemiFinishGoods.Where(s => s.MaterialCode.Equals(semiFinishGoodVM.MaterialCode)).FirstOrDefaultAsync();

                    if (sfg == null)
                    {
                        throw new Exception("Material Code is not recognized.");
                    }

                    if (string.IsNullOrEmpty(semiFinishGoodVM.MaterialName))
                    {
                        ModelState.AddModelError("SemiFinishGood.MaterialName", "Product Name is required.");
                    }

                    if (semiFinishGoodVM.WeightPerBag <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.WeightPerBag", "Weight/Bag can not below zero.");
                    }

                    if (semiFinishGoodVM.PerPalletWeight <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.PerPalletWeight", "Weight/Pallet can not below zero.");
                    }

                    if (semiFinishGoodVM.ExpiredDate <= 0)
                    {
                        ModelState.AddModelError("SemiFinishGood.ExpiredDate", "Expired can not below zero.");
                    }

                    if (string.IsNullOrEmpty(semiFinishGoodVM.LifeRange))
                    {
                        ModelState.AddModelError("SemiFinishGood.LifeRange", "Range is required.");
                    }
                    else
                    {
                        if (!semiFinishGoodVM.LifeRange.Equals("m") && !semiFinishGoodVM.LifeRange.Equals("d") && !semiFinishGoodVM.LifeRange.Equals("y"))
                        {
                            ModelState.AddModelError("SemiFinishGood.LifeRange", "Life Range is not recognized.");
                        }
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

                    sfg.MaterialCode = semiFinishGoodVM.MaterialCode;
                    sfg.MaterialName = semiFinishGoodVM.MaterialName;
                    sfg.StockCategoryName = semiFinishGoodVM.StockCategoryName;
                    sfg.CustProdName = semiFinishGoodVM.CustomerProductName;
                    sfg.AB = semiFinishGoodVM.AB;

                    string LifeRange = "";
                    if (semiFinishGoodVM.LifeRange.Equals("m"))
                    {
                        LifeRange = "Month";

                    }
                    else if (semiFinishGoodVM.LifeRange.Equals("d"))
                    {
                        LifeRange = "Day";
                    }
                    else if (semiFinishGoodVM.LifeRange.Equals("y"))
                    {
                        LifeRange = "Year";
                    }

                    if (semiFinishGoodVM.ExpiredDate > 1)
                    {
                        LifeRange += "s";
                    }

                    sfg.ExpiredDate = string.Format("{0} {1}", semiFinishGoodVM.ExpiredDate, LifeRange);

                    sfg.WeightPerBag = semiFinishGoodVM.WeightPerBag;
                    sfg.PerPalletWeight = semiFinishGoodVM.PerPalletWeight;
                    sfg.ModifiedBy = activeUser;
                    sfg.ModifiedOn = DateTime.Now;

                    await db.SaveChangesAsync();
                    message = "Update data succeeded.";
                    status = true;
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
            SemiFinishGoodDTO semiFinishGoodDTO = null;

            try
            {
                SemiFinishGood x = await db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(id)).FirstOrDefaultAsync();
                if(x == null)
                {
                    throw new Exception("Data not found.");
                }

                string LifeRange = Regex.Replace(x.ExpiredDate, @"[\d-]", string.Empty).ToString();

                if (LifeRange.ToLower().Contains("day"))
                {
                    LifeRange = "d";
                }
                else if (LifeRange.ToLower().Contains("month"))
                {
                    LifeRange = "m";
                }
                else if (LifeRange.ToLower().Contains("year"))
                {
                    LifeRange = "y";
                }

                var ExpiredDate = Regex.Match(x.ExpiredDate, @"\d+").Value;

                semiFinishGoodDTO = new SemiFinishGoodDTO
                {
                    MaterialCode = x.MaterialCode,
                    MaterialName = x.MaterialName,
                    StockCategoryName = x.StockCategoryName,
                    CustomerProductName = x.CustProdName,
                    AB = x.AB,
                    ExpiredDate = ExpiredDate,
                    LifeRange = LifeRange,
                    WeightPerBag = Helper.FormatThousand(x.WeightPerBag),
                    PerPalletWeight = Helper.FormatThousand(x.PerPalletWeight),
                    UoM = x.UoM,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn)
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

            obj.Add("data", semiFinishGoodDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> UploadRecipe(string semiFinishGoodID)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    HttpRequest request = HttpContext.Current.Request;

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
        //            if (!string.IsNullOrEmpty(semiFinishGoodID))
        //            {
        //                SemiFinishGood sfg = await db.SemiFinishGoods.Where(s => s.ID.Equals(semiFinishGoodID)).FirstOrDefaultAsync();

        //                if (sfg == null)
        //                {
        //                    throw new Exception("Semi Finish Good does not exist.");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("semi Finish Good is required.");
        //            }

        //            if (request.Files.Count > 0)
        //            {
        //                for (int i = 0; i < request.Files.Count; i++)
        //                {
        //                    HttpPostedFile file = request.Files[i];
        //                    if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
        //                    {
        //                        if (file.ContentLength < (10 * 1024 * 1024))
        //                        {
        //                            try
        //                            {
        //                                Stream stream = file.InputStream;
        //                                IExcelDataReader reader = null;
        //                                if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
        //                                {
        //                                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

        //                                }
        //                                else
        //                                {
        //                                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
        //                                }

        //                                DataSet result = reader.AsDataSet();
        //                                reader.Close();

        //                                DataTable dt = result.Tables[0];


        //                                Formula formula = new Formula()
        //                                {
        //                                    ItemID = semiFinishGoodID,
        //                                    Type = "SFG",
        //                                    RecipeNumber = dt.Rows[3][22].ToString(),
        //                                    IsActive = true,
        //                                    CreatedBy = activeUser,
        //                                    CreatedOn = DateTime.Now
        //                                };

        //                                Formula frm = await db.Formulae.Where(s => s.RecipeNumber.Equals(formula.RecipeNumber)).FirstOrDefaultAsync();

        //                                if (frm == null)
        //                                {
        //                                    formula.ID = Helper.CreateGuid("RCP");
        //                                    db.Formulae.Add(formula);
        //                                }
        //                                else
        //                                {
        //                                    formula.ID = frm.ID;
        //                                    frm.IsActive = true;
        //                                    frm.ModifiedBy = activeUser;
        //                                    frm.ModifiedOn = DateTime.Now;
        //                                    db.FormulaDetails.RemoveRange(db.FormulaDetails.Where(m => m.FormulaID.Equals(frm.ID)));
        //                                }

        //                                foreach (DataRow row in dt.AsEnumerable().Skip(6))
        //                                {
        //                                    string MaterialCode = row[7].ToString();
        //                                    if (!string.IsNullOrEmpty(MaterialCode))
        //                                    {
        //                                        string UoM = row[17].ToString();
        //                                        string Qty = row[18].ToString();
        //                                        string FullBag = row[21].ToString();
        //                                        string Remainder = row[24].ToString();

        //                                        RawMaterial rawMaterial = await db.RawMaterials.Where(s => s.Code.Equals(MaterialCode)).FirstOrDefaultAsync();
        //                                        if (rawMaterial != null)
        //                                        {
        //                                            FormulaDetail formulaDetail = new FormulaDetail()
        //                                            {
        //                                                ID = Helper.CreateGuid("RCPD"),
        //                                                FormulaID = formula.ID,
        //                                                MaterialCode = rawMaterial.Code,
        //                                                MaterialName = rawMaterial.Name,
        //                                                Qty = Convert.ToDecimal(Qty),
        //                                                RemainderQty = !string.IsNullOrEmpty(Remainder) ? Convert.ToDecimal(Remainder) : 0
        //                                            };


        //                                            if (!string.IsNullOrEmpty(FullBag))
        //                                            {
        //                                                if (!FullBag.ToLower().Equals("offline"))
        //                                                {
        //                                                    formulaDetail.Fullbag = Convert.ToInt32(FullBag);
        //                                                }
        //                                            }

        //                                            db.FormulaDetails.Add(formulaDetail);
        //                                        }

        //                                    }

        //                                }


        //                            }
        //                            catch (Exception e)
        //                            {
        //                                message = string.Format("Upload item failed. {0}", e.Message);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            message = "Upload failed. Maximum allowed file size : 10MB ";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        message = "Upload item failed. File is invalid.";
        //                    }
        //                    await db.SaveChangesAsync();

        //                }

        //                message = "Upload succeeded.";
        //                status = true;

        //            }
        //            else
        //            {
        //                message = "No file uploaded.";
        //            }
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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


        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> DeleteRecipe(string id)
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

        //            Formula frm = await db.Formulae.Where(s => s.ID.Equals(id)).FirstOrDefaultAsync();

        //            if (frm != null)
        //            {
        //                frm.IsActive = false;
        //                db.FormulaDetails.RemoveRange(db.FormulaDetails.Where(m => m.FormulaID.Equals(frm.ID)));
        //                await db.SaveChangesAsync();
        //                status = true;
        //                message = "Recipe is successfully deleted.";
        //            }
        //            else
        //            {
        //                message = "Recipe is not exist.";
        //            }
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
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
        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        [HttpPost]
        public async Task<IHttpActionResult> DatatableRecipe(string itemCode)
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

            IEnumerable<Formula> list = Enumerable.Empty<Formula>();
            IEnumerable<FormulaDTO> pagedData = Enumerable.Empty<FormulaDTO>();

            IQueryable<Formula> query = db.Formulae.AsQueryable().Where(m => m.ItemCode.Equals(itemCode) && m.IsActive == true);

            int recordsTotal = db.Formulae.Count();
            int recordsFiltered = 0;

            try
            {

                query = query
                        .Where(m => m.RecipeNumber.Contains(search));

                list = await query.ToListAsync();

                recordsFiltered = list.Count();


                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {


                    pagedData = from x in list
                                select new FormulaDTO
                                {
                                    RecipeNumber = x.RecipeNumber,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = x.ModifiedOn.ToString()
                                };
                }

                Dictionary<string, Func<Formula, object>> cols = new Dictionary<string, Func<Formula, object>>();
                cols.Add("RecipeNumber", x => x.RecipeNumber);
                cols.Add("IsActive", x => x.IsActive);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

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

        [HttpGet]
        public async Task<IHttpActionResult> GetRecipeById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            FormulaDTO formulaDTO = null;
            decimal total = 0;
            try
            {
                Formula formula = await db.Formulae.Where(m => m.ItemCode.Equals(id) && m.IsActive == true).FirstOrDefaultAsync();
                if(formula != null)
                {
                    formulaDTO = new FormulaDTO
                    {
                        RecipeNumber = formula.RecipeNumber,
                        Details = from y in formula.FormulaDetails
                                  select new FormulaDetailDTO
                                  {
                                      ID = y.ID,
                                      MaterialCode = y.MaterialCode,
                                      MaterialName = y.MaterialName,
                                      UoM = y.UoM,
                                      Qty = Helper.FormatThousand(y.Qty),
                                      RemainderQty = Helper.FormatThousand(y.RemainderQty),
                                      Fullbag = Helper.FormatThousand(y.Fullbag)
                                  },
                        IsActive = formula.IsActive,
                        CreatedBy = formula.CreatedBy,
                        CreatedOn = formula.CreatedOn.ToString(),
                        ModifiedBy = formula.ModifiedBy != null ? formula.ModifiedBy : "",
                        ModifiedOn = formula.ModifiedOn.ToString()
                    };

                    total = formula.FormulaDetails.Sum(i => i.Qty);
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

            obj.Add("totalQty", total);
            obj.Add("data", formulaDTO);
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
        public IHttpActionResult GetStockCategoryNameType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Source type found.";
            bool status = true;

            obj.Add("source_type", Constant.StockCategoryNameSFG());
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}