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
    public class SupplierController : ApiController
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

            IEnumerable<Supplier> list = Enumerable.Empty<Supplier>();
            IEnumerable<SupplierDTO> pagedData = Enumerable.Empty<SupplierDTO>();

            IQueryable<Supplier> query = db.Suppliers.AsQueryable();

            int recordsTotal = db.Suppliers.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.Name.Contains(search)
                        || m.Abbreviation.Contains(search)
                        || m.ClassificationName.Contains(search)
                        || m.Address.Contains(search)
                        || m.Telephone.Contains(search)
                        || m.Contact.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<Supplier, object>> cols = new Dictionary<string, Func<Supplier, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("Abbreviation", x => x.Abbreviation);
                cols.Add("ClassificationName", x => x.ClassificationName);
                cols.Add("Address", x => x.Address);
                cols.Add("DevelopmentDate", x => x.DevelopmentDate);
                cols.Add("Telephone", x => x.Telephone);
                cols.Add("Contact", x => x.Contact);
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
                                select new SupplierDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Abbreviation = x.Abbreviation,
                                    ClassificationName = x.ClassificationName,
                                    Address = x.Address,
                                    DevelopmentDate = Helper.NullDateToString2(x.DevelopmentDate),
                                    Telephone = x.Telephone,
                                    Contact = x.Contact,
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
        public async Task<IHttpActionResult> DatatableSupplierCustomer()
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

            IEnumerable<vCustomerSupplier> list = Enumerable.Empty<vCustomerSupplier>();
            IEnumerable<SupplierDTO> pagedData = Enumerable.Empty<SupplierDTO>();

            IQueryable<vCustomerSupplier> query = db.vCustomerSuppliers.AsQueryable();
            int recordsTotal = db.vCustomerSuppliers.Count();
            int recordsFiltered = 0;
            try
            {
          
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.Name.Contains(search)
                        || m.Abbreviation.Contains(search)
                        || m.ClassificationName.Contains(search)
                        || m.Address.Contains(search)
                        || m.Telephone.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<vCustomerSupplier, object>> cols = new Dictionary<string, Func<vCustomerSupplier, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("Abbreviation", x => x.Abbreviation);
                cols.Add("ClassificationName", x => x.ClassificationName);
                cols.Add("Address", x => x.Address);
                cols.Add("DevelopmentDate", x => x.DevelopmentDate);
                cols.Add("Telephone", x => x.Telephone);
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
                                select new SupplierDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Abbreviation = x.Abbreviation,
                                    ClassificationName = x.ClassificationName,
                                    Address = x.Address,
                                    DevelopmentDate = Helper.NullDateToString2(x.DevelopmentDate),
                                    Telephone = x.Telephone,
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

        [HttpGet]
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<SupplierDTO> list = Enumerable.Empty<SupplierDTO>();

            try
            {
                IEnumerable<Supplier> tempList = await db.Suppliers.ToListAsync();
                list = from x in tempList
                       select new SupplierDTO
                       {
                           Code = x.Code,
                           Name = x.Name,
                           Abbreviation = x.Abbreviation,
                           ClassificationName = x.ClassificationName,
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
            SupplierDTO supplierDTO = null;

            try
            {
                Supplier supplier = await db.Suppliers.Where(m => m.Code.Equals(id)).FirstOrDefaultAsync();
                supplierDTO = new SupplierDTO
                {
                    Code = supplier.Code,
                    Name = supplier.Name,
                    Abbreviation = supplier.Abbreviation,
                    ClassificationName = supplier.ClassificationName,
                    Address = supplier.Address,
                    DevelopmentDate = Helper.NullDateToString2(supplier.DevelopmentDate),
                    Telephone = supplier.Telephone,
                    Contact = supplier.Contact,
                    IsActive = supplier.IsActive,
                    CreatedBy = supplier.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(supplier.CreatedOn),
                    ModifiedBy = supplier.ModifiedBy != null ? supplier.ModifiedBy : "",
                    ModifiedOn = Helper.NullDateTimeToString(supplier.ModifiedOn)
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

            obj.Add("data", supplierDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

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

                                    //string prefix = "SUP";

                                    //string lastNumber = db.Suppliers.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    //int currentNumber = 0;

                                    //if (!string.IsNullOrEmpty(lastNumber))
                                    //{
                                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                                    //}

                                    //currentNumber++;

                                    List<Supplier> updatedList = new List<Supplier>();

                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (dt.Rows.IndexOf(row) != 0)
                                        {
                                            Supplier supplier = new Supplier();
                                            supplier.Code = row[2].ToString();
                                            supplier.Name = row[3].ToString();
                                            supplier.Abbreviation = row[4].ToString();
                                            supplier.ClassificationName = row[5].ToString();
                                            supplier.Address = row[6].ToString();
                                            string devDate = row[7].ToString();
                                            if (!string.IsNullOrEmpty(devDate))
                                            {
                                                try
                                                {
                                                    supplier.DevelopmentDate = Convert.ToDateTime(devDate);
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }
                                            supplier.Telephone = row[8].ToString();
                                            supplier.Contact = row[9].ToString();

                                            Supplier sup = db.Suppliers.Where(m => m.Code.Equals(supplier.Code)).FirstOrDefault();
                                            if (sup != null)
                                            {
                                                sup.Name = supplier.Name;
                                                sup.Abbreviation = supplier.Abbreviation;
                                                sup.ClassificationName = supplier.ClassificationName;
                                                sup.Address = supplier.Address;
                                                sup.DevelopmentDate = supplier.DevelopmentDate;
                                                sup.Telephone = supplier.Telephone;
                                                sup.Contact = supplier.Contact;
                                                sup.ModifiedBy = activeUser;
                                                sup.ModifiedOn = DateTime.Now;

                                                updatedList.Add(sup);
                                            }
                                            else
                                            {
                                                //supplier.ID = Helper.CreateGuid(prefix);
                                                //supplier.Code = prefix + string.Format("{0:D3}", currentNumber++);
                                                supplier.IsActive = true;
                                                supplier.CreatedBy = activeUser;
                                                supplier.CreatedOn = DateTime.Now;

                                                db.Suppliers.Add(supplier);
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
        public async Task<IHttpActionResult> Create(SupplierVM supplierVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> suptomValidationMessages = new List<CustomValidationMessage>();

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

                    if (!string.IsNullOrEmpty(supplierVM.Code))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Code.ToLower().Equals(supplierVM.Code.ToLower())).FirstOrDefaultAsync();

                        if (tempSupplier != null)
                        {
                            ModelState.AddModelError("Supplier.Code", "Supplier Code is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Supplier.Code", "Supplier Code is required.");
                    }

                    if (!string.IsNullOrEmpty(supplierVM.Name))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Name.ToLower().Equals(supplierVM.Name.ToLower())).FirstOrDefaultAsync();

                        if (tempSupplier != null)
                        {
                            ModelState.AddModelError("Supplier.Name", "Supplier Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Supplier.Name", "Supplier Name is required.");
                    }
                    

                    if (!string.IsNullOrEmpty(supplierVM.Abbreviation))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Name.ToUpper().Equals(supplierVM.Abbreviation.ToUpper())).FirstOrDefaultAsync();

                        if (tempSupplier != null)
                        {
                            ModelState.AddModelError("Supplier.Abbreviation", "Supplier Abbreviation is already registered.");
                        }
                    }

                    if (string.IsNullOrEmpty(supplierVM.Address))
                    {
                        ModelState.AddModelError("Supplier.Address", "Address is required.");
                    }

                    if (string.IsNullOrEmpty(supplierVM.Telephone))
                    {
                        ModelState.AddModelError("Supplier.Telephone", "Telephone is required.");
                    }

                    if (string.IsNullOrEmpty(supplierVM.Contact))
                    {
                        ModelState.AddModelError("Supplier.Contact", "Contact is required.");
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            suptomValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    string prefix = "SUP";

                    //string lastNumber = db.Suppliers.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    //}

                    Supplier supplier = new Supplier
                    {
                        //ID = Helper.CreateGuid(prefix),
                        //Code = prefix + string.Format("{0:D3}", currentNumber + 1),
                        Code = Helper.ToUpper(supplierVM.Code),
                        Name = Helper.ToUpper(supplierVM.Name),
                        Abbreviation = Helper.ToUpper(supplierVM.Abbreviation),
                        ClassificationName = supplierVM.ClassificationName,
                        Address = supplierVM.Address,
                        Telephone = supplierVM.Telephone,
                        Contact = supplierVM.Contact,
                        DevelopmentDate = DateTime.Now,
                        IsActive = true,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };

                    //id = supplier.ID;

                    db.Suppliers.Add(supplier);
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

            //obj.Add("id", id);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", suptomValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Update(SupplierVM supplierVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> suptomValidationMessages = new List<CustomValidationMessage>();

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
                    Supplier supplier = null;

                    if (!string.IsNullOrEmpty(supplierVM.Code))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Code.ToLower().Equals(supplierVM.Code.ToLower())).FirstOrDefaultAsync();

                        if (tempSupplier == null)
                        {
                            ModelState.AddModelError("Supplier.Code", "Supplier Code not found.");
                        }

                        supplier = tempSupplier;
                    }
                    else
                    {
                        ModelState.AddModelError("Supplier.Code", "Supplier Code is required.");
                    }

                    if (!string.IsNullOrEmpty(supplierVM.Name))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Name.ToLower().Equals(supplierVM.Name.ToLower()) && !s.Code.Equals(supplierVM.Code)).FirstOrDefaultAsync();

                        if (tempSupplier != null)
                        {
                            ModelState.AddModelError("Supplier.Name", "Supplier Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Supplier.Name", "Supplier Name is required.");
                    }


                    if (!string.IsNullOrEmpty(supplierVM.Abbreviation))
                    {
                        Supplier tempSupplier = await db.Suppliers.Where(s => s.Name.ToUpper().Equals(supplierVM.Abbreviation.ToUpper()) && !s.Code.Equals(supplierVM.Code)).FirstOrDefaultAsync();

                        if (tempSupplier != null)
                        {
                            ModelState.AddModelError("Supplier.Abbreviation", "Supplier Abbreviation is already registered.");
                        }
                    }

                    if (string.IsNullOrEmpty(supplierVM.Address))
                    {
                        ModelState.AddModelError("Supplier.Address", "Address is required.");
                    }

                    if (string.IsNullOrEmpty(supplierVM.Telephone))
                    {
                        ModelState.AddModelError("Supplier.Telephone", "Telephone is required.");
                    }

                    if (string.IsNullOrEmpty(supplierVM.Contact))
                    {
                        ModelState.AddModelError("Supplier.Contact", "Contact is required.");
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            suptomValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    supplier.Name = Helper.ToUpper(supplierVM.Name);
                    supplier.Abbreviation = Helper.ToUpper(supplierVM.Abbreviation);
                    supplier.ClassificationName = supplierVM.ClassificationName;
                    supplier.Address = supplierVM.Address;
                    supplier.Telephone = supplierVM.Telephone;
                    supplier.Contact = supplierVM.Contact;
                    //supplier.DevelopmentDate = DateTime.Now;
                    supplier.IsActive = supplierVM.IsActive;
                    supplier.ModifiedBy = activeUser;
                    supplier.ModifiedOn = DateTime.Now;

                    await db.SaveChangesAsync();
                    status = true;
                    message = "Update data succeeded.";
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
            obj.Add("error_validation", suptomValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Delete(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> suptomValidationMessages = new List<CustomValidationMessage>();

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
                    if (string.IsNullOrEmpty(id))
                    {
                        throw new Exception("Supplier Code is required.");
                    }

                    Supplier supplier = await db.Suppliers.Where(x => x.Code.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (supplier != null)
                    {
                        supplier.IsActive = false;
                        supplier.ModifiedBy = activeUser;
                        supplier.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Supplier is successfully deleted.";

                    }
                    else
                    {
                        message = "Supplier is not exist.";
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
            obj.Add("error_validation", suptomValidationMessages);

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
