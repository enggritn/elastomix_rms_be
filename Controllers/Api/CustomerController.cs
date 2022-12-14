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
    public class CustomerController : ApiController
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

            IEnumerable<Customer> list = Enumerable.Empty<Customer>();
            IEnumerable<CustomerDTO> pagedData = Enumerable.Empty<CustomerDTO>();

            IQueryable<Customer> query = db.Customers.AsQueryable();

            int recordsTotal = db.Customers.Count();
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

                Dictionary<string, Func<Customer, object>> cols = new Dictionary<string, Func<Customer, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("Abbreviation", x => x.Abbreviation);
                cols.Add("ClassificationName", x => x.ClassificationName);
                cols.Add("Address", x => x.Address);
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
                                select new CustomerDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Abbreviation = x.Abbreviation,
                                    ClassificationName = x.ClassificationName,
                                    Address = x.Address,
                                    Telephone = x.Telephone,
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
            IEnumerable<CustomerDTO> list = Enumerable.Empty<CustomerDTO>();

            try
            {
                IEnumerable<Customer> tempList = await db.Customers.ToListAsync();
                list = from x in tempList
                       select new CustomerDTO
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
            CustomerDTO customerDTO = null;

            try
            {
                Customer customer = await db.Customers.Where(m => m.Code.Equals(id)).FirstOrDefaultAsync();
                customerDTO = new CustomerDTO
                {
                    Code = customer.Code,
                    Name = customer.Name,
                    Abbreviation = customer.Abbreviation,
                    ClassificationName = customer.ClassificationName,
                    Address = customer.Address,
                    Telephone = customer.Telephone,
                    IsActive = customer.IsActive,
                    CreatedBy = customer.CreatedBy,
                    CreatedOn = customer.CreatedOn.ToString(),
                    ModifiedBy = customer.ModifiedBy != null ? customer.ModifiedBy : "",
                    ModifiedOn = customer.ModifiedOn.ToString()
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

            obj.Add("data", customerDTO);
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

                                    string prefix = "CUS";

                                    //string lastNumber = db.Customers.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    //int currentNumber = 0;

                                    //if (!string.IsNullOrEmpty(lastNumber))
                                    //{
                                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                                    //}

                                    //currentNumber++;

                                    List<Customer> updatedList = new List<Customer>();

                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (dt.Rows.IndexOf(row) != 0)
                                        {
                                            Customer customer = new Customer();
                                            customer.Code = row[2].ToString();
                                            customer.Name = row[3].ToString();
                                            customer.Abbreviation = row[4].ToString();
                                            customer.ClassificationName = row[6].ToString();
                                            customer.Address = row[7].ToString();
                                            string devDate = row[8].ToString();
                                            if (!string.IsNullOrEmpty(devDate))
                                            {
                                                try
                                                {
                                                    customer.DevelopmentDate = Convert.ToDateTime(devDate);
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }

                                            customer.Telephone = row[9].ToString();

                                            Customer cus = db.Customers.Where(m => m.Code.Equals(customer.Code)).FirstOrDefault();
                                            if (cus != null)
                                            {
                                                cus.Name = customer.Name;
                                                cus.Abbreviation = customer.Abbreviation;
                                                cus.ClassificationName = customer.ClassificationName;
                                                cus.Address = customer.Address;
                                                cus.DevelopmentDate = customer.DevelopmentDate;
                                                cus.Telephone = customer.Telephone;
                                                cus.ModifiedBy = activeUser;
                                                cus.ModifiedOn = DateTime.Now;

                                                updatedList.Add(cus);
                                            }
                                            else
                                            {
                                                //customer.ID = Helper.CreateGuid(prefix);
                                                //customer.Code = prefix + string.Format("{0:D3}", currentNumber++);
                                                customer.IsActive = true;
                                                customer.CreatedBy = activeUser;
                                                customer.CreatedOn = DateTime.Now;

                                                db.Customers.Add(customer);
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
        public async Task<IHttpActionResult> Create(CustomerVM customerVM)
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

                    if (!string.IsNullOrEmpty(customerVM.Code))
                    {
                        Customer tempCustomer = await db.Customers.Where(s => s.Code.ToLower().Equals(customerVM.Code.ToLower())).FirstOrDefaultAsync();

                        if (tempCustomer != null)
                        {
                            ModelState.AddModelError("Customer.Code", "Customer Code is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Customer.Code", "Customer Code is required.");
                    }


                    if (!string.IsNullOrEmpty(customerVM.Name))
                    {
                        Customer tempCustomer = await db.Customers.Where(s => s.Name.ToLower().Equals(customerVM.Name.ToLower())).FirstOrDefaultAsync();

                        if (tempCustomer != null)
                        {
                            ModelState.AddModelError("Customer.Name", "Customer Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Customer.Name", "Customer Name is required.");
                    }

                   
                    if (!string.IsNullOrEmpty(customerVM.Abbreviation))
                    {
                        Customer tempCustomer = await db.Customers.Where(s => s.Name.ToUpper().Equals(customerVM.Abbreviation.ToUpper())).FirstOrDefaultAsync();

                        if (tempCustomer != null)
                        {
                            ModelState.AddModelError("Customer.Abbreviation", "Customer Abbreviation is already registered.");
                        }
                    }

                    if (string.IsNullOrEmpty(customerVM.Address))
                    {
                        ModelState.AddModelError("Customer.Address", "Address is required.");
                    }

                    if (string.IsNullOrEmpty(customerVM.Telephone))
                    {
                        ModelState.AddModelError("Customer.Telephone", "Telephone is required.");
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

                    string prefix = "CUS";

                    //string lastNumber = db.Customers.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    //}

                    Customer customer = new Customer
                    {
                        //ID = Helper.CreateGuid(prefix),
                        //Code = prefix + string.Format("{0:D3}", currentNumber + 1),
                        Code = Helper.ToUpper(customerVM.Code),
                        Name = Helper.ToUpper(customerVM.Name),
                        Abbreviation = Helper.ToUpper(customerVM.Abbreviation),
                        ClassificationName = customerVM.ClassificationName,
                        Address = customerVM.Address,
                        Telephone = customerVM.Telephone,
                        DevelopmentDate = DateTime.Now,
                        IsActive = true,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };

                    //id = customer.ID;

                    db.Customers.Add(customer);
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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Update(CustomerVM customerVM)
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
                    Customer customer = null;

                    if (!string.IsNullOrEmpty(customerVM.Code))
                    {
                        customer = await db.Customers.Where(x => x.Code.Equals(customerVM.Code)).FirstOrDefaultAsync();

                        if (customer == null)
                        {
                            ModelState.AddModelError("Customer.Code", "Customer Code is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Customer.Code", "Customer ID Code required.");
                    }


                    if (!string.IsNullOrEmpty(customerVM.Name))
                    {
                        Customer tempCustomer = await db.Customers.Where(s => s.Name.ToLower().Equals(customerVM.Name.ToLower()) && !s.Code.Equals(customerVM.Code)).FirstOrDefaultAsync();

                        if (tempCustomer != null)
                        {
                            ModelState.AddModelError("Customer.Name", "Customer Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Customer.Name", "Customer Name is required.");
                    }

                    if (!string.IsNullOrEmpty(customerVM.Abbreviation))
                    {
                        Customer tempCustomer = await db.Customers.Where(s => s.Name.ToUpper().Equals(customerVM.Abbreviation.ToUpper()) && !s.Code.Equals(customerVM.Code)).FirstOrDefaultAsync();

                        if (tempCustomer != null)
                        {
                            ModelState.AddModelError("Customer.Abbreviation", "Customer Abbreviation is already registered.");
                        }
                    }

                    if (string.IsNullOrEmpty(customerVM.Address))
                    {
                        ModelState.AddModelError("Customer.Address", "Address is required.");
                    }

                    if (string.IsNullOrEmpty(customerVM.Telephone))
                    {
                        ModelState.AddModelError("Customer.Telephone", "Telephone is required.");
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

                    customer.Name = Helper.ToUpper(customerVM.Name);
                    customer.Abbreviation = Helper.ToUpper(customerVM.Abbreviation);
                    customer.ClassificationName = customerVM.ClassificationName;
                    customer.Address = customerVM.Address;
                    customer.Telephone = customerVM.Telephone;
                    customer.IsActive = customerVM.IsActive;
                    customer.ModifiedBy = activeUser;
                    customer.ModifiedOn = DateTime.Now;

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
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Delete(string id)
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
                    Customer customer = await db.Customers.Where(x => x.Code.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (customer != null)
                    {
                        customer.IsActive = false;
                        customer.ModifiedBy = activeUser;
                        customer.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Customer is successfully deleted.";

                    }
                    else
                    {
                        message = "Customer is not exist.";
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
