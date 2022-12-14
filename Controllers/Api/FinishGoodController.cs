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
    public class FinishGoodController : ApiController
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

            IEnumerable<FinishGood> list = Enumerable.Empty<FinishGood>();
            IEnumerable<FinishGoodDTO> pagedData = Enumerable.Empty<FinishGoodDTO>();

            IQueryable<FinishGood> query = db.FinishGoods.AsQueryable();

            int recordsTotal = db.FinishGoods.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        || m.StockCategoryCode.Contains(search)
                        || m.StockCategoryName.Contains(search)
                        //|| m.InputTaxRate.Contains(search)
                        //|| m.OutputTaxRate.Contains(search)
                        //|| m.EnabledDate.Contains(search)
                        || m.ABLIAN.Contains(search)
                        //|| m.Factor.Contains(search)
                        //|| m.WeightPerBag.Contains(search)
                        //|| m.SpecificGravity.Contains(search)
                        //|| m.PerPalletWeight.Contains(search)
                        || m.UoM.Contains(search)
                        || m.Specifications.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<FinishGood, object>> cols = new Dictionary<string, Func<FinishGood, object>>();
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("Specifications", x => x.Specifications);
                cols.Add("StockCode", x => x.StockCode);
                cols.Add("StockCategoryCode", x => x.StockCategoryCode);
                cols.Add("StockCategoryName", x => x.StockCategoryName);
                cols.Add("InputTaxRate", x => x.InputTaxRate);
                cols.Add("OutputTaxRate", x => x.OutputTaxRate);
                cols.Add("EnabledDate", x => x.EnabledDate);
                cols.Add("ABLIAN", x => x.ABLIAN);
                cols.Add("Factor", x => x.Factor);
                cols.Add("WeightPerBag", x => x.WeightPerBag);
                cols.Add("SpecificGravity", x => x.SpecificGravity);
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
                                select new FinishGoodDTO
                                {
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    Specifications = x.Specifications,
                                    StockCode = x.StockCode,
                                    StockCategoryCode = x.StockCategoryCode,
                                    StockCategoryName = x.StockCategoryName,
                                    InputTaxRate = Helper.FormatThousand(x.InputTaxRate),
                                    OutputTaxRate = Helper.FormatThousand(x.OutputTaxRate),
                                    EnabledDate = Helper.NullDateToString2(x.EnabledDate),
                                    ABLIAN = x.ABLIAN,
                                    Factor = Helper.FormatThousand(x.Factor),
                                    WeightPerBag = Helper.FormatThousand(x.WeightPerBag),
                                    SpecificGravity = Helper.FormatThousand(x.SpecificGravity),
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

                                    //string prefix = "FG";

                                    //string lastNumber = db.FinishGoods.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    //int currentNumber = 0;

                                    //if (!string.IsNullOrEmpty(lastNumber))
                                    //{
                                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 5));
                                    //}

                                    //currentNumber++;

                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (dt.Rows.IndexOf(row) != 0)
                                        {
                                            FinishGood finishGood = new FinishGood();
                                            finishGood.MaterialCode = row[2].ToString();
                                            finishGood.MaterialName = row[3].ToString();
                                            finishGood.Specifications = row[4].ToString();
                                            finishGood.StockCode = row[5].ToString();
                                            finishGood.StockCategoryCode = row[6].ToString();
                                            finishGood.StockCategoryName = row[7].ToString();
                                            finishGood.InputTaxRate = string.IsNullOrEmpty(row[8].ToString()) ? 0 : int.Parse(row[8].ToString());
                                            finishGood.OutputTaxRate = string.IsNullOrEmpty(row[9].ToString()) ? 0 : int.Parse(row[9].ToString());
                                            
                                            finishGood.ABLIAN = row[11].ToString();
                                            finishGood.Factor = string.IsNullOrEmpty(row[12].ToString()) ? 0 : decimal.Parse(row[12].ToString());
                                            finishGood.WeightPerBag = string.IsNullOrEmpty(row[13].ToString()) ? 0 : decimal.Parse(row[13].ToString());
                                            finishGood.SpecificGravity = string.IsNullOrEmpty(row[14].ToString()) ? 0 : decimal.Parse(row[14].ToString());
                                            finishGood.PerPalletWeight = string.IsNullOrEmpty(row[15].ToString()) ? 0 : decimal.Parse(row[15].ToString());
                                            finishGood.UoM = row[16].ToString();
                                            string EnabledDate = row[10].ToString();
                                            if (!string.IsNullOrEmpty(EnabledDate))
                                            {
                                                try
                                                {
                                                    finishGood.EnabledDate = Convert.ToDateTime(EnabledDate);
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }

                                            FinishGood fg = db.FinishGoods.Where(m => m.MaterialCode.Equals(finishGood.MaterialCode)).FirstOrDefault();
                                            if (fg != null)
                                            {
                                                fg.MaterialCode = finishGood.MaterialCode;
                                                fg.MaterialName = finishGood.MaterialName;
                                                fg.Specifications = finishGood.Specifications;
                                                fg.StockCode = finishGood.StockCode;
                                                fg.StockCategoryCode = finishGood.StockCategoryCode;
                                                fg.StockCategoryName = finishGood.StockCategoryName;
                                                fg.InputTaxRate = finishGood.InputTaxRate;
                                                fg.OutputTaxRate = finishGood.OutputTaxRate;
                                                fg.EnabledDate = finishGood.EnabledDate;
                                                fg.ABLIAN = finishGood.ABLIAN;
                                                fg.Factor = finishGood.Factor;
                                                fg.WeightPerBag = finishGood.WeightPerBag;
                                                fg.SpecificGravity = finishGood.SpecificGravity;
                                                fg.PerPalletWeight = finishGood.PerPalletWeight;
                                                fg.UoM = finishGood.UoM;
                                                fg.ModifiedBy = activeUser;
                                                fg.ModifiedOn = DateTime.Now;
                                            }
                                            else
                                            {                                                
                                                //finishGood.ID = Helper.CreateGuid(prefix);
                                                //finishGood.Code = prefix + string.Format("{0:D5}", currentNumber++);
                                                finishGood.IsActive = true;
                                                finishGood.CreatedBy = activeUser;
                                                finishGood.CreatedOn = DateTime.Now;

                                                db.FinishGoods.Add(finishGood);
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
        public async Task<IHttpActionResult> Create(FinishGoodVM finishGoodVM)
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
                    if (string.IsNullOrEmpty(finishGoodVM.MaterialCode))
                    {
                        ModelState.AddModelError("FinishGood.ItemNumber", "Item Number is required.");
                    }
                    else
                    {
                        FinishGood temp = await db.FinishGoods.Where(s => s.MaterialCode.ToUpper().Equals(finishGoodVM.MaterialCode.ToUpper())).FirstOrDefaultAsync();
                        if (temp != null)
                        {
                            ModelState.AddModelError("FinishGood.ItemNumber", "Item Number is already registered.");
                        }
                    }

                    if (!string.IsNullOrEmpty(finishGoodVM.MaterialName))
                    {
                        FinishGood temp = await db.FinishGoods.Where(s => s.MaterialName.ToUpper().Equals(finishGoodVM.MaterialName.ToUpper())).FirstOrDefaultAsync();

                        if (temp != null)
                        {
                            ModelState.AddModelError("FinishGood.StockName", "Stock Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("FinishGood.StockName", "Stock Name is required.");
                    }

                    if (string.IsNullOrEmpty(finishGoodVM.StockCode))
                    {
                        ModelState.AddModelError("FinishGood.StockCode", "Stock Code is required.");
                    }

                    if (string.IsNullOrEmpty(finishGoodVM.Specifications))
                    {
                        ModelState.AddModelError("FinishGood.Specifications", "Specifications is required.");
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
                    FinishGood fg = new FinishGood();

                    //string prefix = "FG";

                    //string lastNumber = db.FinishGoods.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 5));
                    //}

                    //fg.ID = Helper.CreateGuid(prefix);
                    //fg.Code = prefix + string.Format("{0:D5}", currentNumber + 1);
                    fg.MaterialCode = finishGoodVM.MaterialCode;
                    fg.MaterialName = finishGoodVM.MaterialName;
                    fg.Specifications = finishGoodVM.Specifications;
                    fg.StockCode = finishGoodVM.StockCode;
                    fg.StockCategoryCode = finishGoodVM.StockCategoryCode;
                    fg.StockCategoryName = finishGoodVM.StockCategoryName;
                    fg.InputTaxRate = finishGoodVM.InputTaxRate;
                    fg.OutputTaxRate = finishGoodVM.OutputTaxRate;
                    fg.EnabledDate = finishGoodVM.EnabledDate;
                    fg.ABLIAN = finishGoodVM.ABLIAN;
                    fg.Factor = finishGoodVM.Factor;
                    fg.WeightPerBag = finishGoodVM.WeightPerBag;
                    fg.SpecificGravity = finishGoodVM.SpecificGravity;
                    fg.PerPalletWeight = finishGoodVM.PerPalletWeight;
                    fg.UoM = finishGoodVM.UoM;
                    fg.IsActive = true;
                    fg.CreatedBy = activeUser;
                    fg.CreatedOn = DateTime.Now;


                    db.FinishGoods.Add(fg);

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
        public async Task<IHttpActionResult> Update(FinishGoodVM finishGoodVM)
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
                    FinishGood fg = null;

                    if (string.IsNullOrEmpty(finishGoodVM.MaterialCode))
                    {
                        throw new Exception("Item Number is required.");
                    }

                    fg = await db.FinishGoods.Where(s => s.MaterialCode.Equals(finishGoodVM.MaterialCode)).FirstOrDefaultAsync();

                    if (fg == null)
                    {
                        ModelState.AddModelError("FinishGood.ItemNumber", "Item Number is not recognized.");
                    }

                    if (!string.IsNullOrEmpty(finishGoodVM.MaterialName))
                    {
                        FinishGood temp = await db.FinishGoods.Where(s => s.MaterialName.ToUpper().Equals(finishGoodVM.MaterialName.ToUpper()) && !s.MaterialCode.Equals(finishGoodVM.MaterialCode)).FirstOrDefaultAsync();

                        if (temp != null)
                        {
                            ModelState.AddModelError("FinishGood.StockName", "Stock Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("FinishGood.StockName", "Stock Name is required.");
                    }

                    if (string.IsNullOrEmpty(finishGoodVM.StockCode))
                    {
                        ModelState.AddModelError("FinishGood.StockCode", "Stock Code is required.");
                    }

                    if (string.IsNullOrEmpty(finishGoodVM.Specifications))
                    {
                        ModelState.AddModelError("FinishGood.Specifications", "Specifications is required.");
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

                    fg.MaterialCode = finishGoodVM.MaterialCode;
                    fg.MaterialName = finishGoodVM.MaterialName;
                    fg.Specifications = finishGoodVM.Specifications;
                    fg.StockCode = finishGoodVM.StockCode;
                    fg.StockCategoryCode = finishGoodVM.StockCategoryCode;
                    fg.StockCategoryName = finishGoodVM.StockCategoryName;
                    fg.InputTaxRate = finishGoodVM.InputTaxRate;
                    fg.OutputTaxRate = finishGoodVM.OutputTaxRate;
                    fg.EnabledDate = finishGoodVM.EnabledDate;
                    fg.ABLIAN = finishGoodVM.ABLIAN;
                    fg.Factor = finishGoodVM.Factor;
                    fg.WeightPerBag = finishGoodVM.WeightPerBag;
                    fg.SpecificGravity = finishGoodVM.SpecificGravity;
                    fg.PerPalletWeight = finishGoodVM.PerPalletWeight;
                    fg.UoM = finishGoodVM.UoM;
                    fg.IsActive = finishGoodVM.IsActive;
                    fg.ModifiedBy = activeUser;
                    fg.ModifiedOn = DateTime.Now;

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
            FinishGoodDTO finishGoodDTO = null;

            try
            {
                FinishGood x = await db.FinishGoods.Where(m => m.MaterialCode.Equals(id)).FirstOrDefaultAsync();
                if(x != null)
                {
                    finishGoodDTO = new FinishGoodDTO
                    {
                        MaterialCode = x.MaterialCode,
                        MaterialName = x.MaterialName,
                        Specifications = x.Specifications,
                        StockCode = x.StockCode,
                        StockCategoryCode = x.StockCategoryCode,
                        StockCategoryName = x.StockCategoryName,
                        InputTaxRate = Helper.FormatThousand(x.InputTaxRate),
                        OutputTaxRate = Helper.FormatThousand(x.OutputTaxRate),
                        EnabledDate = Helper.NullDateToString3(x.EnabledDate),
                        ABLIAN = x.ABLIAN,
                        Factor = Helper.FormatThousand(x.Factor),
                        WeightPerBag = Helper.FormatThousand(x.WeightPerBag),
                        SpecificGravity = Helper.FormatThousand(x.SpecificGravity),
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

            obj.Add("data", finishGoodDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> UploadRecipe(string finishGoodID)
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
        //            if (!string.IsNullOrEmpty(finishGoodID))
        //            {
        //                FinishGood fg = await db.FinishGoods.Where(s => s.ID.Equals(finishGoodID)).FirstOrDefaultAsync();

        //                if (fg == null)
        //                {
        //                    throw new Exception("Finish Good does not exist.");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("Finish Good is required.");
        //            }

        //            if (request.Files.Count > 0)
        //            {
        //                for(int i = 0; i < request.Files.Count; i++)
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
        //                                    ItemID = finishGoodID,
        //                                    Type = "FG",
        //                                    RecipeNumber = dt.Rows[3][22].ToString(),
        //                                    IsActive = true,
        //                                    CreatedBy = activeUser,
        //                                    CreatedOn = DateTime.Now
        //                                };

        //                                Formula frm = await db.Formulae.Where(s => s.RecipeNumber.Equals(formula.RecipeNumber)).FirstOrDefaultAsync();
                                        
        //                                if(frm == null)
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

        [HttpPost]
        public async Task<IHttpActionResult> DatatableRecipe(string finishGoodCode)
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

            IQueryable<Formula> query = db.Formulae.AsQueryable().Where(m => m.ItemCode.Equals(finishGoodCode) && m.IsActive == true);

            int recordsTotal = db.Formulae.Where(m => m.ItemCode.Equals(finishGoodCode) && m.IsActive == true).Count();
            int recordsFiltered = 0;

            try
            {

                query = query
                        .Where(m => m.RecipeNumber.Contains(search));

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
                Formula formula = await db.Formulae.Where(m => m.RecipeNumber.Equals(id) && m.IsActive == true).FirstOrDefaultAsync();
                if (formula != null)
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
    }
}