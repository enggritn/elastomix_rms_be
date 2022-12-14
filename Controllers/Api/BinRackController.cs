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
    public class BinRackController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Datatable(string binRackAreaId)
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

            IEnumerable<BinRack> list = Enumerable.Empty<BinRack>();
            IEnumerable<BinRackDTO> pagedData = Enumerable.Empty<BinRackDTO>();

            IQueryable<BinRack> query = db.BinRacks.AsQueryable().Where(m => m.BinRackAreaID.Equals(binRackAreaId));

            int recordsTotal = db.BinRacks.Where(m => m.BinRackAreaID.Equals(binRackAreaId)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.WarehouseName.Contains(search)
                        || m.BinRackAreaName.Contains(search)
                        || m.Name.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<BinRack, object>> cols = new Dictionary<string, Func<BinRack, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("Code", x => x.Code);
                cols.Add("WarehouseName", x => x.Warehouse.Name);
                cols.Add("BinRackAreaName", x => x.BinRackArea.Name);
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
                                select new BinRackDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    WarehouseCode = x.Warehouse.Code,
                                    WarehouseName = x.Warehouse.Name,
                                    BinRackAreaID = x.BinRackAreaID,
                                    BinRackAreaName = x.BinRackArea.Name,
                                    Name = x.Name,
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
        public IHttpActionResult AreaRack(string id)
        {
            IQueryable<BinRack> query = db.BinRacks.AsQueryable().Where(m => m.BinRackAreaID.Equals(id)).OrderBy(m => m.Code);
            var list = query.Select(x => new BinRackDTO
            {
                ID = x.ID,
                Code = x.Code,
                WarehouseCode = x.Warehouse.Code,
                WarehouseName = x.Warehouse.Name,
                BinRackAreaID = x.BinRackAreaID,
                BinRackAreaName = x.BinRackArea.Name,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });

            return Ok(list);
        }


        [HttpPost]
        public async Task<IHttpActionResult> DatatableByWarehouse(string warehouseId)
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

            IEnumerable<BinRack> list = Enumerable.Empty<BinRack>();
            IEnumerable<BinRackDTO> pagedData = Enumerable.Empty<BinRackDTO>();

            IQueryable<BinRack> query = db.BinRacks.AsQueryable().Where(m => m.WarehouseCode.Equals(warehouseId));

            int recordsTotal = db.BinRacks.Where(m => m.WarehouseCode.Equals(warehouseId)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search));

                list = await query.ToListAsync();

                recordsFiltered = list.Count();


                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new BinRackDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    WarehouseCode = x.Warehouse.Code,
                                    WarehouseName = x.Warehouse.Name,
                                    BinRackAreaID = x.BinRackAreaID,
                                    BinRackAreaName = x.BinRackArea.Name,
                                    Name = x.Name,
                                    IsActive = x.IsActive,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = x.ModifiedOn.ToString()
                                };
                }

                Dictionary<string, Func<BinRack, object>> cols = new Dictionary<string, Func<BinRack, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("WarehouseCode", x => x.Warehouse.Code);
                cols.Add("WarehouseName", x => x.Warehouse.Name);
                cols.Add("BinRackAreaCode", x => x.BinRackArea.Code);
                cols.Add("BinRackAreaName", x => x.BinRackArea.Name);
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
            IEnumerable<BinRackDTO> list = Enumerable.Empty<BinRackDTO>();

            try
            {
                IEnumerable<BinRack> tempList = await db.BinRacks.ToListAsync();
                list = from x in tempList
                       select new BinRackDTO
                       {
                           ID = x.ID,
                           Code = x.Code,
                           WarehouseCode = x.Warehouse.Code,
                           WarehouseName = x.Warehouse.Name,
                           BinRackAreaID = x.BinRackAreaID,
                           BinRackAreaName = x.BinRackArea.Name,
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
            BinRackDTO binRackDTO = null;

            try
            {
                BinRack binRack = await db.BinRacks.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                binRackDTO = new BinRackDTO
                {
                    ID = binRack.ID,
                    Code = binRack.Code,
                    WarehouseCode = binRack.Warehouse.Code,
                    WarehouseName = binRack.Warehouse.Name,
                    BinRackAreaID = binRack.BinRackAreaID,
                    BinRackAreaName = binRack.BinRackArea.Name,
                    Name = binRack.Name,
                    IsActive = binRack.IsActive,
                    CreatedBy = binRack.CreatedBy,
                    CreatedOn = binRack.CreatedOn.ToString(),
                    ModifiedBy = binRack.ModifiedBy != null ? binRack.ModifiedBy : "",
                    ModifiedOn = binRack.ModifiedOn.ToString()
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

            obj.Add("data", binRackDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(BinRackVM binRackVM)
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
                    if (!string.IsNullOrEmpty(binRackVM.Name))
                    {
                        BinRack tempBinRack = await db.BinRacks.Where(s => s.BinRackAreaID.Equals(binRackVM.BinRackAreaID) && s.Name.ToLower().Equals(binRackVM.Name.ToLower())).FirstOrDefaultAsync();

                        if (tempBinRack != null)
                        {
                            ModelState.AddModelError("BinRack.RackName", "Bin/Rack Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRack.RackName", "BinRack Name is required.");
                    }

                    //if (!string.IsNullOrEmpty(binRackVM.Code))
                    //{
                    //    BinRack tempBinRack = await db.BinRacks.Where(s => s.Name.ToLower().Equals(binRackVM.Code.ToLower())).FirstOrDefaultAsync();

                    //    if (tempBinRack != null)
                    //    {
                    //        ModelState.AddModelError("BinRack.Code", "Bin/Rack Code is already registered.");
                    //    }
                    //}
                    //else
                    //{
                    //    ModelState.AddModelError("BinRack.Code", "BinRack Code is required.");
                    //}

                    if (!string.IsNullOrEmpty(binRackVM.BinRackAreaID))
                    {
                        BinRackArea area = await db.BinRackAreas.Where(s => s.ID.Equals(binRackVM.BinRackAreaID)).FirstOrDefaultAsync();

                        if (area == null)
                        {
                            throw new Exception("Area does not exist.");
                        }
                        else
                        {
                            if(area.IsActive == false)
                            {
                                throw new Exception("Bin Rack Area already inactive.");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Bin Rack Area is required.");
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

                    string prefix = "BIN";

                    //string lastNumber = db.BinRacks.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    //int currentNumber = 0;

                    //if (!string.IsNullOrEmpty(lastNumber))
                    //{
                    //    currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    //}

                    BinRackArea binRackArea = await db.BinRackAreas.Where(s => s.ID.Equals(binRackVM.BinRackAreaID) && s.IsActive == true).FirstOrDefaultAsync();

                    Warehouse warehouse = await db.Warehouses.Where(s => s.Code.Equals(binRackArea.WarehouseCode) && s.IsActive == true).FirstOrDefaultAsync();

                    BinRack binRack = new BinRack
                    {
                        ID = Helper.CreateGuid(prefix),
                        //Code = prefix + string.Format("{0:D3}", currentNumber + 1),
                        //Code = Helper.ToUpper(binRackVM.Code),
                        Name = Helper.ToUpper(binRackVM.Name),
                        WarehouseCode = warehouse.Code,
                        WarehouseName = warehouse.Name,
                        BinRackAreaID = binRackArea.ID,
                        BinRackAreaCode = binRackArea.Code,
                        BinRackAreaName = binRackArea.Name,
                        IsActive = true,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };

                    binRack.Code = string.Format("{0}{1}", binRack.BinRackAreaCode, binRack.Name);
                    id = binRack.ID;

                    db.BinRacks.Add(binRack);
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

            obj.Add("id", id);
            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Update(BinRackVM binRackVM)
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
                    BinRack binRack = await db.BinRacks.Where(x => x.ID.Equals(binRackVM.ID)).FirstOrDefaultAsync();
                    if (!string.IsNullOrEmpty(binRackVM.ID))
                    {
                        binRack = await db.BinRacks.Where(x => x.ID.Equals(binRackVM.ID)).FirstOrDefaultAsync();

                        if (binRack == null)
                        {
                            ModelState.AddModelError("BinRack.RackID", "BinRack is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRack.RackID", "BinRack ID is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackVM.Name))
                    {
                        BinRack tempBinRack = await db.BinRacks.Where(s => s.BinRackAreaID.Equals(binRack.BinRackAreaID) && s.Name.ToLower().Equals(binRackVM.Name.ToLower()) && !s.ID.Equals(binRackVM.ID)).FirstOrDefaultAsync();

                        if (tempBinRack != null)
                        {
                            ModelState.AddModelError("BinRack.RackName", "Bin/Rack Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRack.RackName", "BinRack Name is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackVM.BinRackAreaID))
                    {
                        BinRackArea area = await db.BinRackAreas.Where(s => s.ID.Equals(binRackVM.BinRackAreaID) && s.IsActive == true).FirstOrDefaultAsync();

                        if (area == null)
                        {
                            ModelState.AddModelError("BinRack.BinRackAreaID", "Area does not exist.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRack.BinRackAreaID", "Area is required.");
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


                    binRack.Name = binRackVM.Name.ToUpper();
                    binRack.IsActive = binRackVM.IsActive;
                    binRack.ModifiedBy = activeUser;
                    binRack.ModifiedOn = DateTime.Now;
                    binRack.Code = string.Format("{0}{1}", binRack.BinRackAreaCode, binRack.Name);

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
                    BinRack binRack = await db.BinRacks.Where(x => x.ID.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (binRack != null)
                    {
                        binRack.IsActive = false;
                        binRack.ModifiedBy = activeUser;
                        binRack.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Rack is successfully deleted.";

                    }
                    else
                    {
                        message = "Rack is not exist.";
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
        public async Task<IHttpActionResult> GetDataByCodeAtWarehouse(string BinRackCode, string WarehouseCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            BinRackDTO binRackDTO = null;

            try
            {
                BinRack binRack = await db.BinRacks.Where(m => m.Code.Equals(BinRackCode) && m.WarehouseCode.Equals(WarehouseCode)).FirstOrDefaultAsync();
                if(binRack == null)
                {
                    throw new Exception("Bin Rack not found.");
                }
                binRackDTO = new BinRackDTO
                {
                    ID = binRack.ID,
                    Code = binRack.Code,
                    WarehouseCode = binRack.Warehouse.Code,
                    WarehouseName = binRack.Warehouse.Name,
                    BinRackAreaID = binRack.BinRackAreaID,
                    BinRackAreaName = binRack.BinRackArea.Name,
                    Name = binRack.Name,
                    IsActive = binRack.IsActive,
                    CreatedBy = binRack.CreatedBy,
                    CreatedOn = binRack.CreatedOn.ToString(),
                    ModifiedBy = binRack.ModifiedBy != null ? binRack.ModifiedBy : "",
                    ModifiedOn = binRack.ModifiedOn.ToString()
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

            obj.Add("data", binRackDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataByCode(string BinRackCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            BinRackDTO binRackDTO = null;

            try
            {
                BinRack binRack = await db.BinRacks.Where(m => m.Code.Equals(BinRackCode)).FirstOrDefaultAsync();
                if (binRack == null)
                {
                    throw new Exception("Bin Rack not found.");
                }
                binRackDTO = new BinRackDTO
                {
                    ID = binRack.ID,
                    Code = binRack.Code,
                    WarehouseCode = binRack.Warehouse.Code,
                    WarehouseName = binRack.Warehouse.Name,
                    BinRackAreaID = binRack.BinRackAreaID,
                    BinRackAreaName = binRack.BinRackArea.Name,
                    Name = binRack.Name,
                    IsActive = binRack.IsActive,
                    CreatedBy = binRack.CreatedBy,
                    CreatedOn = binRack.CreatedOn.ToString(),
                    ModifiedBy = binRack.ModifiedBy != null ? binRack.ModifiedBy : "",
                    ModifiedOn = binRack.ModifiedOn.ToString()
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

            obj.Add("data", binRackDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataByWarehouse(string WarehouseCode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<BinRackDTO> list = Enumerable.Empty<BinRackDTO>();

            try
            {
                IEnumerable<BinRack> tempList = await db.BinRacks.Where(m => m.WarehouseCode.Equals(WarehouseCode)).ToListAsync();
                list = from x in tempList
                       select new BinRackDTO
                       {
                           ID = x.ID,
                           Code = x.Code,
                           WarehouseCode = x.Warehouse.Code,
                           WarehouseName = x.Warehouse.Name,
                           BinRackAreaID = x.BinRackAreaID,
                           BinRackAreaName = x.BinRackArea.Name,
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

        [HttpPost]
        public async Task<IHttpActionResult> GetRackItems(AreaCodes areaCodes)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<BinRackItemDTO> data = Enumerable.Empty<BinRackItemDTO>(); ;

            try
            {

                if (areaCodes.AreaCode.Count() <= 0)
                {
                    throw new Exception("AreaCode is required.");
                }

                IEnumerable<vBinRackItem> list = Enumerable.Empty<vBinRackItem>();
                IQueryable<vBinRackItem> query = db.vBinRackItems.AsQueryable().Where(m => areaCodes.AreaCode.Contains(m.BinRackAreaCode));
                list = query.ToList();

                data = from item in list
                            select new BinRackItemDTO
                            {
                                BinRackAreaCode = item.BinRackAreaCode,
                                BinRackCode = item.BinRackCode,
                                BinRackName = item.BinRackName,
                                TotalQty = Convert.ToInt32(item.Total)
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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UploadBinRack()
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


                                //string area1 = "AR003";
                                //string area2 = "AR001";

                                DateTime curTime = DateTime.Now;

                                foreach (DataRow row in dt.AsEnumerable())
                                {
                                    //check to master Raw Material
                                    string AreaCode = row[0].ToString();
                                    string RackCode = row[1].ToString();

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
