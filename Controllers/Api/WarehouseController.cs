using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
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
    public class WarehouseController : ApiController
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

            IEnumerable<Warehouse> list = Enumerable.Empty<Warehouse>();
            IEnumerable<WarehouseDTO> pagedData = Enumerable.Empty<WarehouseDTO>();

            IQueryable<Warehouse> query = db.Warehouses.AsQueryable();

            int recordsTotal = db.Warehouses.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                     .Where(m => m.Code.Contains(search)
                     || m.Name.Contains(search)
                     || m.CreatedBy.Contains(search)
                     || m.ModifiedBy.Contains(search)
                     );

                Dictionary<string, Func<Warehouse, object>> cols = new Dictionary<string, Func<Warehouse, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
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
                                select new WarehouseDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Type = x.Type,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy,
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

        public async Task<IHttpActionResult> DatatableFilterType(string type)
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

            IEnumerable<Warehouse> list = Enumerable.Empty<Warehouse>();
            IEnumerable<WarehouseDTO> pagedData = Enumerable.Empty<WarehouseDTO>();

            IQueryable<Warehouse> query = db.Warehouses.Where(s => s.Type.Equals(type)).AsQueryable();

            int recordsTotal = db.Warehouses.Where(s => s.Type.Equals(type)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                     .Where(m => m.Name.Contains(search));

                Dictionary<string, Func<Warehouse, object>> cols = new Dictionary<string, Func<Warehouse, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name != null ? x.Name : null);
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
                                select new WarehouseDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    Type = x.Type,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy,
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
            IEnumerable<WarehouseDTO> list = Enumerable.Empty<WarehouseDTO>();

            try
            {
                IEnumerable<Warehouse> tempList = await db.Warehouses.Where(m => m.IsActive == true).OrderBy(m => m.Code).ToListAsync();
                list = from x in tempList
                       select new WarehouseDTO
                       {
                           Code = x.Code,
                           Name = x.Name,
                           Type = x.Type,
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
        public async Task<IHttpActionResult> GetDataEmix()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<WarehouseDTO> list = Enumerable.Empty<WarehouseDTO>();

            try
            {
                IEnumerable<Warehouse> tempList = await db.Warehouses.Where(m => m.IsActive == true && m.Type == "EMIX").OrderBy(m => m.Code).ToListAsync();
                list = from x in tempList
                       select new WarehouseDTO
                       {
                           Code = x.Code,
                           Name = x.Name,
                           Type = x.Type,
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
        public async Task<IHttpActionResult> GetDataByType(string type)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<WarehouseDTO> list = Enumerable.Empty<WarehouseDTO>();

            try
            {
                IEnumerable<Warehouse> tempList = await db.Warehouses.Where(s => s.Type.Equals(type)).ToListAsync();
                list = from x in tempList
                       select new WarehouseDTO
                       {
                           Code = x.Code,
                           Name = x.Name,
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
            WarehouseDTO warehouseDTO = null;

            try
            {
                Warehouse warehouse = await db.Warehouses.Where(m => m.Code.Equals(id)).FirstOrDefaultAsync();
                warehouseDTO = new WarehouseDTO
                {
                    Code = warehouse.Code,
                    Name = warehouse.Name,
                    Type = warehouse.Type,
                    IsActive = warehouse.IsActive,
                    CreatedBy = warehouse.CreatedBy,
                    CreatedOn = warehouse.CreatedOn.ToString(),
                    ModifiedBy = warehouse.ModifiedBy != null ? warehouse.ModifiedBy : "",
                    ModifiedOn = warehouse.ModifiedOn.ToString()
                };

                status = true;
                message = "Fetch data succeded.";

                obj.Add("warehouse_type", Constant.WarehouseTypes());
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

            obj.Add("data", warehouseDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public IHttpActionResult GetWarehouseType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Warehouse type found.";
            bool status = true;

            obj.Add("warehouse_type", Constant.WarehouseTypes());
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> Create(WarehouseVM warehouseVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            //string warehouseID = null;

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
                    if (!string.IsNullOrEmpty(warehouseVM.Code))
                    {
                        Warehouse tempWarehouse = await db.Warehouses.Where(s => s.Code.ToLower().Equals(warehouseVM.Code.ToLower())).FirstOrDefaultAsync();

                        if (tempWarehouse != null)
                        {
                            ModelState.AddModelError("Warehouse.Code", "Warehouse Code is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Code", "Warehouse Code is required.");
                    }


                    if (!string.IsNullOrEmpty(warehouseVM.Name))
                    {
                        Warehouse tempWarehouse = await db.Warehouses.Where(s => s.Name.ToLower().Equals(warehouseVM.Name.ToLower())).FirstOrDefaultAsync();

                        if (tempWarehouse != null)
                        {
                            ModelState.AddModelError("Warehouse.Name", "Warehouse Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Name", "Warehouse Name is required.");
                    }

                   
                    //check warehouse type
                    if (!string.IsNullOrEmpty(warehouseVM.Type))
                    {
                        bool exist = Constant.WarehouseTypes().Contains(warehouseVM.Type);

                        if (!exist)
                        {
                            ModelState.AddModelError("Warehouse.Type", "warehouse Type is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Type", "Warehouse Type is required.");
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

                    //string prefix = "WH";
                    //warehouseID = Helper.CreateGuid(prefix);

                    //string lastNumber = db.Warehouses.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    //}


                    Warehouse warehouse = new Warehouse
                    {
                        //Code = prefix + string.Format("{0:D3}", currentNumber + 1),
                        Code = Helper.ToUpper(warehouseVM.Code),
                        Name = Helper.ToUpper(warehouseVM.Name),
                        Type = Helper.ToUpper(warehouseVM.Type),
                        IsActive = true,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };

                    db.Warehouses.Add(warehouse);
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
        public async Task<IHttpActionResult> Update(WarehouseVM warehouseVM)
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
                    Warehouse warehouse = null;

                    if (!string.IsNullOrEmpty(warehouseVM.Code))
                    {
                        warehouse = await db.Warehouses.Where(x => x.Code.Equals(warehouseVM.Code)).FirstOrDefaultAsync();

                        if (warehouse == null)
                        {
                            ModelState.AddModelError("Warehouse.Code", "Warehouse Code is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Code", "Warehouse Code is required.");
                    }

                    if (!string.IsNullOrEmpty(warehouseVM.Name))
                    {
                        Warehouse tempWarehouse = await db.Warehouses.Where(s => s.Name.ToLower().Equals(warehouseVM.Name.ToLower()) && !s.Code.Equals(warehouseVM.Code)).FirstOrDefaultAsync();

                        if (tempWarehouse != null)
                        {
                            ModelState.AddModelError("Warehouse.Name", "Warehouse Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Name", "Warehouse Name is required.");
                    }

                    //check warehouse type
                    if (!string.IsNullOrEmpty(warehouseVM.Type))
                    {
                        bool exist = Constant.WarehouseTypes().Contains(warehouseVM.Type);

                        if (!exist)
                        {
                            ModelState.AddModelError("Warehouse.Type", "warehouse Type is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Warehouse.Type", "Warehouse Type is required.");
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

                    warehouse.Name = Helper.ToUpper(warehouseVM.Name);
                    warehouse.Type = warehouseVM.Type.ToUpper();
                    warehouse.IsActive = warehouseVM.IsActive;
                    warehouse.ModifiedBy = activeUser;
                    warehouse.ModifiedOn = DateTime.Now;

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
        public async Task<IHttpActionResult> GetWarehouseAreas()
        {
            return await GetFilteredWarehouse("Warehouse Area");
        }

        [HttpPost]
        public async Task<IHttpActionResult> GetProductionAreas()
        {
            return await GetFilteredWarehouse("Production Area");
        }

        public async Task<IHttpActionResult> GetFilteredWarehouse(string type)
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

            IEnumerable<Warehouse> list = Enumerable.Empty<Warehouse>();
            IEnumerable<WarehouseDTO> pagedData = Enumerable.Empty<WarehouseDTO>();

            IQueryable<Warehouse> query = db.Warehouses.Where(s => s.Type.Equals(type)).AsQueryable();

            int recordsTotal = db.Warehouses.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                     .Where(m => m.Name.Contains(search));

                list = await query.ToListAsync();

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {


                    pagedData = from x in list
                                select new WarehouseDTO
                                {
                                    Code = x.Code,
                                    Name = x.Name,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy,
                                    ModifiedOn = x.ModifiedOn.ToString()
                                };
                }

                Dictionary<string, Func<Warehouse, object>> cols = new Dictionary<string, Func<Warehouse, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name != null ? x.Name : null);
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

        /**
         * Deprecated
         */
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
                    Warehouse warehouse = await db.Warehouses.Where(x => x.Code.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (warehouse != null)
                    {
                        warehouse.IsActive = false;
                        warehouse.ModifiedBy = activeUser;
                        warehouse.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Warehouse is successfully deleted.";

                    }
                    else
                    {
                        message = "Warehouse is not exist.";
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
        public async Task<IHttpActionResult> GetEmixWarehouse()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<WarehouseDTO> list = Enumerable.Empty<WarehouseDTO>();

            try
            {
                IEnumerable<Warehouse> tempList = await db.Warehouses.Where(m => m.IsActive == true && m.Type.Equals("EMIX")).OrderBy(m => m.Code).ToListAsync();
                list = from x in tempList
                       select new WarehouseDTO
                       {
                           Code = x.Code,
                           Name = x.Name,
                           Type = x.Type,
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
