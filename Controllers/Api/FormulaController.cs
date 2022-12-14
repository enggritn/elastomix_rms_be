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
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class FormulaController : ApiController
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

            IEnumerable<Formula> list = Enumerable.Empty<Formula>();
            IEnumerable<FormulaDTO> pagedData = Enumerable.Empty<FormulaDTO>();

            IQueryable<Formula> query = db.Formulae.AsQueryable();

            int recordsTotal = db.Formulae.Count();
            int recordsFiltered = 0;

            try
            {

                query = query
                        .Where(m => m.ItemCode.Contains(search)
                        || m.RecipeNumber.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        //|| m.Type.Contains(search)
                        );

                Dictionary<string, Func<Formula, object>> cols = new Dictionary<string, Func<Formula, object>>();
                cols.Add("ItemCode", x => x.ItemCode);
                cols.Add("RecipeNumber", x => x.RecipeNumber);
                cols.Add("Type", x => x.Type);
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
                                    Type = x.Type,
                                    ItemCode = x.ItemCode,
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
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<FormulaDTO> list = Enumerable.Empty<FormulaDTO>();

            try
            {
                IEnumerable<Formula> tempList = await db.Formulae.ToListAsync();
                list = from x in tempList
                       select new FormulaDTO
                       {
                           RecipeNumber = x.RecipeNumber,
                           ItemCode = x.ItemCode,
                           //Code = x.Code,
                           //ProductNumber = x.ProductNumber,
                           Details = from y in x.FormulaDetails
                                     select new FormulaDetailDTO
                                     {
                                         ID = y.ID,
                                         //Code = y.Code,
                                         Qty = Helper.FormatThousand(y.Qty),
                                         RemainderQty = Helper.FormatThousand(y.RemainderQty),
                                         Fullbag = Helper.FormatThousand(y.Fullbag),
                                     },
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

            obj.Add("list", list);
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
            FormulaDTO formulaDTO = null;
            decimal total = 0;
            try
            {
                Formula formula = await db.Formulae.Where(m => m.RecipeNumber.Equals(id)).FirstOrDefaultAsync();
                if (formula != null)
                {
                    formulaDTO = new FormulaDTO
                    {
                        RecipeNumber = formula.RecipeNumber,
                        Details = from y in formula.FormulaDetails.OrderBy(i => i.MaterialCode)
                                  select new FormulaDetailDTO
                                  {
                                      ID = y.ID,
                                      MaterialCode = y.MaterialCode,
                                      MaterialName = y.MaterialName,
                                      UoM = y.UoM,
                                      Qty = Helper.FormatThousand(y.Qty),
                                      RemainderQty = Helper.FormatThousand(y.RemainderQty),
                                      Fullbag = Helper.FormatThousand(y.Fullbag),
                                      Type = y.Type
                                  },
                        IsActive = formula.IsActive,
                        CreatedBy = formula.CreatedBy,
                        CreatedOn = formula.CreatedOn.ToString(),
                        ModifiedBy = formula.ModifiedBy != null ? formula.ModifiedBy : "",
                        ModifiedOn = formula.ModifiedOn.ToString()
                    };

                    string ProductName = "";

                    if (formula.Type.Equals("FG"))
                    {
                        FinishGood item = await db.FinishGoods.Where(m => m.MaterialCode.Equals(formula.ItemCode)).FirstOrDefaultAsync();
                        if(item == null)
                        {
                            throw new Exception("Item not found.");
                        }
                        ProductName = item.MaterialName;
                    }
                    else if(formula.Type.Equals("SFG"))
                    {
                        SemiFinishGood item = await db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(formula.ItemCode)).FirstOrDefaultAsync();
                        if (item == null)
                        {
                            throw new Exception("Item not found.");
                        }
                        ProductName = item.MaterialName;
                    }

                    formulaDTO.ProductName = ProductName;

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

        //[HttpPost]
        //public async Task<IHttpActionResult> Create(FormulaVM formulaVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;
        //    string id = null;

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
        //            //if (!string.IsNullOrEmpty(formulaVM.Code))
        //            //{
        //            //    Formula tempFormula = await db.Formulae.Where(s => s.Code.ToUpper().Equals(formulaVM.Code.ToUpper())).FirstOrDefaultAsync();

        //            //    if (tempFormula != null)
        //            //    {
        //            //        ModelState.AddModelError("Formula.Code", "Code is already registered.");
        //            //    }
        //            //}

        //            if (formulaVM.Details != null)
        //            {
        //                bool isExist = true;
        //                foreach (string rawMaterialID in formulaVM.Details.Select(s => s.RawMaterialID))
        //                {
        //                    RawMaterial tempRawMaterial = await db.RawMaterials.Where(s => s.ID.Equals(rawMaterialID)).FirstOrDefaultAsync();

        //                    if (tempRawMaterial == null)
        //                    {
        //                        isExist = false;
        //                        break;
        //                    }
        //                }

        //                if (!isExist)
        //                {
        //                    ModelState.AddModelError("FormulaDetail.RawMaterialId", "One or more of the Raw Materials does not exist.");
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

        //                throw new Exception("Input is not valid");
        //            }

        //            var CreatedAt = DateTime.Now;

        //            Formula formula = new Formula
        //            {
        //                ID = Helper.CreateGuid("F"),
        //                //Code = Helper.ToUpper(formulaVM.Code),
        //                //ProductNumber = formulaVM.ProductNumber,
        //                IsActive = true,
        //                CreatedBy = activeUser,
        //                CreatedOn = CreatedAt
        //            };

        //            IEnumerable<FormulaDetail> details = Enumerable.Empty<FormulaDetail>();

        //            if (formulaVM.Details != null && formulaVM.Details.Count > 0)
        //            {
        //                string prefix = "FD";
        //                int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
        //                int month = CreatedAt.Month;
        //                string romanMonth = Helper.ConvertMonthToRoman(month);

        //                // get last number, and do increment.
        //                //string lastNumber = db.Formulae.AsQueryable().OrderByDescending(x => x.Code)
        //                //    .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
        //                //    .AsEnumerable().Select(x => x.Code).FirstOrDefault();
        //                //int currentNumber = 0;

        //                //if (!string.IsNullOrEmpty(lastNumber))
        //                //{
        //                //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
        //                //}

        //                foreach (FormulaDetailVM y in formulaVM.Details)
        //                {
        //                    RawMaterial rawMaterial = await db.RawMaterials.Where(s => s.ID.Equals(y.RawMaterialID)).FirstOrDefaultAsync();

        //                    //currentNumber++;
        //                    //string runningNumber = string.Format("{0:D3}", currentNumber);

        //                    FormulaDetail detail = new FormulaDetail()
        //                    {
        //                        ID = Helper.CreateGuid("FD"),
        //                        //Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber),
        //                        FormulaID = formula.ID,
        //                        Qty = y.Qty,
        //                        RemainderQty = y.Qty % rawMaterial.Qty,
        //                        Fullbag = Convert.ToInt32(Math.Floor(y.Qty / rawMaterial.Qty))
        //                    };

        //                    formula.FormulaDetails.Add(detail);
        //                }
        //            }

        //            id = formula.ID;

        //            db.Formulae.Add(formula);
        //            await db.SaveChangesAsync();
        //            status = true;
        //            message = "Create data succeeded.";
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

        //    obj.Add("id", id);
        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Update(FormulaVM formulaVM)
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
        //            Formula formula = await db.Formulae.Where(x => x.ID.Equals(formulaVM.ID)).FirstOrDefaultAsync();

        //            if (formula != null)
        //            {
        //                //if (!string.IsNullOrEmpty(formulaVM.Code))
        //                //{
        //                //    Formula tempFormula = await db.Formulae.Where(s => s.Code.ToUpper().Equals(formulaVM.Code.ToUpper()) && !s.ID.Equals(formulaVM.ID)).FirstOrDefaultAsync();

        //                //    if (tempFormula != null)
        //                //    {
        //                //        ModelState.AddModelError("Formula.Code", "Code is already registered.");
        //                //    }
        //                //}

        //                if (!ModelState.IsValid)
        //                {
        //                    foreach (var state in ModelState)
        //                    {
        //                        string field = state.Key.Split('.')[1];
        //                        string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                        customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                    }

        //                    throw new Exception("Input is not valid");
        //                }

        //                //formula.Code = Helper.ToUpper(formulaVM.Code);
        //                formula.IsActive = formulaVM.IsActive;
        //                formula.ModifiedBy = activeUser;
        //                formula.ModifiedOn = DateTime.Now;

        //                await db.SaveChangesAsync();
        //                status = true;
        //                message = "Update data succeeded.";
        //            }
        //            else
        //            {
        //                message = "Formula is no longer exist.";
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

        //[HttpPost]
        //public async Task<IHttpActionResult> DeleteHeader(string id)
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
        //            Formula formula = await db.Formulae.Where(x => x.ID.Equals(id) && x.IsActive).FirstOrDefaultAsync();

        //            if (formula != null)
        //            {
        //                formula.IsActive = false;
        //                formula.ModifiedBy = activeUser;
        //                formula.ModifiedOn = DateTime.Now;

        //                await db.SaveChangesAsync();
        //                status = true;
        //                message = "Formula is successfully deleted.";

        //            }
        //            else
        //            {
        //                message = "Formula is not exist.";
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

        //[HttpGet]
        //public async Task<IHttpActionResult> GetDataDetail()
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    IEnumerable<FormulaDetailDTO> list = Enumerable.Empty<FormulaDetailDTO>();

        //    try
        //    {
        //        IEnumerable<FormulaDetail> tempList = await db.FormulaDetails.ToListAsync();
        //        list = from x in tempList
        //               select new FormulaDetailDTO
        //               {
        //                   ID = x.ID,
        //                   Code = x.Code,
        //                   Qty = x.Qty,
        //                   Fullbag = x.Fullbag,
        //                   RemainderQty = x.RemainderQty,
        //                   RawMaterialID = x.RawMaterialID,
        //                   MaterialCode = x.RawMaterial.Code,
        //                   MaterialName = x.RawMaterial.Name,
        //                   CreatedBy = x.CreatedBy,
        //                   CreatedOn = x.CreatedOn.ToString(),
        //                   ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
        //                   ModifiedOn = x.ModifiedOn.ToString()
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
        //public async Task<IHttpActionResult> GetDataDetailById(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    FormulaDetailDTO formulaDetailDTO = null;

        //    try
        //    {
        //        FormulaDetail formulaDetail = await db.FormulaDetails.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
        //        formulaDetailDTO = new FormulaDetailDTO
        //        {
        //            ID = formulaDetail.ID,
        //            Code = formulaDetail.Code,
        //            Qty = formulaDetail.Qty,
        //            Fullbag = formulaDetail.Fullbag,
        //            RemainderQty = formulaDetail.RemainderQty,
        //            RawMaterialID = formulaDetail.RawMaterialID,
        //            MaterialCode = formulaDetail.RawMaterial.Code,
        //            MaterialName = formulaDetail.RawMaterial.Name,
        //            CreatedBy = formulaDetail.CreatedBy,
        //            CreatedOn = formulaDetail.CreatedOn.ToString(),
        //            ModifiedBy = formulaDetail.ModifiedBy != null ? formulaDetail.ModifiedBy : "",
        //            ModifiedOn = formulaDetail.ModifiedOn.ToString()
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

        //    obj.Add("data", formulaDetailDTO);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> UpdateDetail(FormulaDetailVM formulaDetailVM)
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
        //            FormulaDetail formulaDetail = await db.FormulaDetails.Where(x => x.ID.Equals(formulaDetailVM.ID)).FirstOrDefaultAsync();

        //            if (formulaDetail != null)
        //            {
        //                if (!string.IsNullOrEmpty(formulaDetailVM.Code))
        //                {
        //                    FormulaDetail tempFormulaDetail = await db.FormulaDetails.Where(s => s.Code.ToUpper().Equals(formulaDetailVM.Code.ToUpper()) && !s.ID.Equals(formulaDetailVM.ID)).FirstOrDefaultAsync();

        //                    if (tempFormulaDetail != null)
        //                    {
        //                        ModelState.AddModelError("FormulaDetail.Code", "Code is already registered.");
        //                    }
        //                }

        //                if (!string.IsNullOrEmpty(formulaDetailVM.RawMaterialID))
        //                {
        //                    RawMaterial tempRawMaterial = await db.RawMaterials.Where(s => s.ID.Equals(formulaDetailVM.RawMaterialID)).FirstOrDefaultAsync();

        //                    if (tempRawMaterial == null)
        //                    {
        //                        ModelState.AddModelError("FormulaDetail.RawMaterialId", "Raw Material does not exist.");
        //                    }
        //                }

        //                if (!ModelState.IsValid)
        //                {
        //                    foreach (var state in ModelState)
        //                    {
        //                        string field = state.Key.Split('.')[1];
        //                        string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                        customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                    }

        //                    throw new Exception("Input is not valid");
        //                }

        //                formulaDetail.Code = Helper.ToUpper(formulaDetailVM.Code);
        //                formulaDetail.Qty = formulaDetailVM.Qty;
        //                formulaDetail.Fullbag = formulaDetailVM.Fullbag;
        //                formulaDetail.RemainderQty = formulaDetailVM.RemainderQty;
        //                formulaDetail.RawMaterialID = formulaDetailVM.RawMaterialID;
        //                formulaDetail.ModifiedBy = activeUser;
        //                formulaDetail.ModifiedOn = DateTime.Now;

        //                await db.SaveChangesAsync();
        //                status = true;
        //                message = "Update data succeeded.";
        //            }
        //            else
        //            {
        //                message = "FormulaDetail is no longer exist.";
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
        public async Task<IHttpActionResult> UploadRecipe()
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
                        for (int i = 0; i < request.Files.Count; i++)
                        {
                            HttpPostedFile file = request.Files[i];
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

                                        string fileName = file.FileName.ToLower().Replace(Path.GetExtension(file.FileName), "");

                                        string MaterialName = fileName.ToUpper();

                                        //check stockName
                                        vProduct product = await db.vProducts.Where(s => s.MaterialName.Equals(MaterialName)).FirstOrDefaultAsync();


                                        if(product != null)
                                        {

                                            //logic update recipe update h-2 dari tanggal production


                                            Formula prevRecipe = await db.Formulae.Where(s => s.ItemCode.Equals(product.MaterialCode) && s.IsActive.Equals(true)).OrderBy(s => s.CreatedOn).FirstOrDefaultAsync();
                                            if(prevRecipe != null)
                                            {
                                                prevRecipe.IsActive = false;
                                            }

                                            Formula formula = new Formula()
                                            {
                                                ItemCode = product.MaterialCode,
                                                Type = product.ProdType,
                                                RecipeNumber = dt.Rows[3][22].ToString(),
                                                IsActive = true,
                                                CreatedBy = activeUser,
                                                CreatedOn = DateTime.Now
                                            };

                                            Formula frm = await db.Formulae.Where(s => s.RecipeNumber.Equals(formula.RecipeNumber) && s.IsActive == true).FirstOrDefaultAsync();

                                            if (frm == null)
                                            {
                                                //formula.ID = Helper.CreateGuid("RCP");
                                                db.Formulae.Add(formula);

                                                foreach (DataRow row in dt.AsEnumerable().Skip(6))
                                                {
                                                    string MaterialCode = row[7].ToString();
                                                    if (!string.IsNullOrEmpty(MaterialCode))
                                                    {
                                                        string UoM = row[17].ToString();
                                                        string Qty = row[18].ToString();
                                                        string FullBag = row[21].ToString();
                                                        string Remainder = row[24].ToString();

                                                        vProductMaster material = await db.vProductMasters.Where(s => s.MaterialCode.Equals(MaterialCode)).FirstOrDefaultAsync();
                                                        if (material != null)
                                                        {
                                                            FormulaDetail formulaDetail = new FormulaDetail()
                                                            {
                                                                ID = Helper.CreateGuid("RCPD"),
                                                                RecipeNumber = formula.RecipeNumber,
                                                                MaterialCode = material.MaterialCode,
                                                                MaterialName = material.MaterialName,
                                                                Type = material.ProdType,
                                                                Qty = Convert.ToDecimal(Qty),
                                                                UoM = "KG",
                                                                RemainderQty = !string.IsNullOrEmpty(Remainder) ? Convert.ToDecimal(Remainder) : 0
                                                            };


                                                            if (!string.IsNullOrEmpty(FullBag))
                                                            {
                                                                if (!FullBag.ToLower().Equals("offline"))
                                                                {
                                                                    formulaDetail.Fullbag = Convert.ToInt32(FullBag);
                                                                }
                                                            }

                                                            db.FormulaDetails.Add(formulaDetail);
                                                        }
                                                    }
                                                }
                                            }
                                        }                                    


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
                            await db.SaveChangesAsync();

                        }

                        message = "Upload succeeded.";
                        status = true;

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

        [HttpGet]
        public async Task<IHttpActionResult> GetDataByProduct(string itemCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            FormulaDTO formulaDTO = null;
            decimal total = 0;
            int batch = 0;
            try
            {
                Formula formula = await db.Formulae.Where(m => m.ItemCode.Equals(itemCode) && m.IsActive == true).FirstOrDefaultAsync();
                if (formula != null)
                {
                    formulaDTO = new FormulaDTO
                    {
                        RecipeNumber = formula.RecipeNumber,
                        Details = from y in formula.FormulaDetails.OrderBy(m => m.MaterialCode)
                                  select new FormulaDetailDTO
                                  {
                                      ID = y.ID,
                                      MaterialCode = y.MaterialCode,
                                      MaterialName = y.MaterialName,
                                      UoM = y.UoM,
                                      Qty = Helper.FormatThousand(y.Qty),
                                      RemainderQty = Helper.FormatThousand(y.RemainderQty),
                                      Fullbag = Helper.FormatThousand(y.Fullbag),
                                      Type = y.Type
                                  },
                        IsActive = formula.IsActive,
                        CreatedBy = formula.CreatedBy,
                        CreatedOn = formula.CreatedOn.ToString(),
                        ModifiedBy = formula.ModifiedBy != null ? formula.ModifiedBy : "",
                        ModifiedOn = formula.ModifiedOn.ToString()
                    };

                    string ProductName = "";

                    if (formula.Type.Equals("FG"))
                    {
                        FinishGood item = await db.FinishGoods.Where(m => m.MaterialCode.Equals(formula.ItemCode)).FirstOrDefaultAsync();
                        if (item == null)
                        {
                            throw new Exception("Item not found.");
                        }
                        ProductName = item.MaterialName;
                    }
                    else if (formula.Type.Equals("SFG"))
                    {
                        SemiFinishGood item = await db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(formula.ItemCode)).FirstOrDefaultAsync();
                        if (item == null)
                        {
                            throw new Exception("Item not found.");
                        }
                        ProductName = item.MaterialName;
                    }

                    formulaDTO.ProductName = ProductName;

                    total = formula.FormulaDetails.Sum(i => i.Qty);

                    batch = Convert.ToInt32(total / 1000);
                    if(batch == 0)
                    {
                        batch += 1;
                    }

                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "No recipe found.";
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

            obj.Add("totalQty", total);
            obj.Add("batch", batch);
            obj.Add("data", formulaDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetSFGCalculationByProduct(int batchQty, string itemCode, string stockDate)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<StockSFGDTO> stocks = null;
            try
            {
                Formula formula = await db.Formulae.Where(m => m.ItemCode.Equals(itemCode) && m.IsActive == true).FirstOrDefaultAsync();
                if (formula != null)
                {
                    decimal totalRecipe = formula.FormulaDetails.Sum(i => i.Qty);
                    //calculate here
                    DateTime StockDate = Convert.ToDateTime(stockDate);
                    var query = db.vSFGRecipeCalculations
                     .Where(m => m.ItemCode.Equals(itemCode) && DbFunctions.TruncateTime(m.StockDate) <= DbFunctions.TruncateTime(StockDate))
                     .GroupBy(g => new { MaterialCode = g.MaterialCode, MaterialName = g.MaterialName, Qty = g.Qty })
                     .Select(s => new { MaterialCode = s.Key.MaterialCode, MaterialName = s.Key.MaterialName, Qty = s.Key.Qty, TotalQty = s.Sum(y => y.TotalQty ?? 0) }).ToList();

                    if(query != null && query.Count() > 0)
                    {
                        stocks = new List<StockSFGDTO>();
                        foreach (var item in query)
                        {
                            StockSFGDTO stock = new StockSFGDTO
                            {
                                MaterialCode = item.MaterialCode,
                                MaterialName = item.MaterialName,
                                TotalQty = Helper.FormatThousand(item.TotalQty),
                                RecipeQty = Helper.FormatThousand(item.Qty),
                                ProductionQty = Helper.FormatThousand((batchQty * totalRecipe) * (item.Qty / totalRecipe)),
                                OutstandingQty = ((batchQty * totalRecipe) * (item.Qty / totalRecipe)) - item.TotalQty > 0 ? Helper.FormatThousand(((batchQty * totalRecipe) * (item.Qty / totalRecipe)) - item.TotalQty) : "-"
                            };

                            stocks.Add(stock);
                        }
                    }
 
                    status = true;
                    message = "Fetch data succeded.";
                }
                else
                {
                    message = "No recipe found.";
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

            obj.Add("data", stocks);
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
