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
    public class BinRackAreaController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Datatable(string warehouseId)
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

            IEnumerable<BinRackArea> list = Enumerable.Empty<BinRackArea>();
            IEnumerable<BinRackAreaDTO> pagedData = Enumerable.Empty<BinRackAreaDTO>();

            IQueryable<BinRackArea> query = db.BinRackAreas.AsQueryable().Where(m => m.WarehouseCode.Equals(warehouseId));

            int recordsTotal = db.BinRackAreas.Where(m => m.WarehouseCode.Equals(warehouseId)).Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                        || m.Name.Contains(search)
                        || m.Type.Contains(search)
                        || m.CreatedBy.Contains(search)
                        || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<BinRackArea, object>> cols = new Dictionary<string, Func<BinRackArea, object>>();
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name != null ? x.Name : null);
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
                                select new BinRackAreaDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    Name = x.Name,
                                    Type = x.Type,
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
        public async Task<IHttpActionResult> AreaList()
        {
            List<Dictionary<string, string>> obj = new List<Dictionary<string, string>>();
            IEnumerable<BinRackArea> tempList = await db.BinRackAreas.OrderBy(m => m.Code).ToListAsync();
            var list = tempList.Select( x => new BinRackAreaDTO()
            {
                ID = x.ID,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });

            return Ok(list);
        }

        [HttpGet]
        public IHttpActionResult WarehouseArea(string id)
        {

            Warehouse wh = db.Warehouses.Where(s => s.Code.Equals(id)).FirstOrDefault();
            string[] warehouseCodes = { };
            if (!wh.Type.Equals("EMIX"))
            {
                warehouseCodes = new string[1] { id};
            }
            else
            {
                warehouseCodes = db.Warehouses.Where(m => m.Type.Equals("EMIX")).Select(m => m.Code).ToArray();
            }

            IQueryable<BinRackArea> query = db.BinRackAreas.AsQueryable().Where(m => warehouseCodes.Contains(m.WarehouseCode)).OrderBy(m => m.Code);
            var list = query.Select(x => new BinRackAreaDTO()
            {
                ID = x.ID,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });

            return Ok(list);
        }

        [HttpGet]
        public IHttpActionResult WarehouseAreaByType(string Type)
        {
            IQueryable<BinRackArea> query = db.BinRackAreas.AsQueryable().Where(m => m.Warehouse.Type.Equals(Type)).OrderBy(m => m.Code);
            var list = query.Select(x => new BinRackAreaDTO()
            {
                ID = x.ID,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });

            return Ok(list);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<BinRackAreaDTO> list = Enumerable.Empty<BinRackAreaDTO>();
            string id = null;

            try
            {
                IEnumerable<BinRackArea> tempList = await db.BinRackAreas.ToListAsync();
                list = from x in tempList
                       select new BinRackAreaDTO
                       {
                           ID = x.ID,
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

            obj.Add("id", id);
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
            BinRackAreaDTO binRackAreaDTO = null;

            try
            {
                BinRackArea binRackArea = await db.BinRackAreas.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                
                if(binRackArea == null)
                {
                    throw new Exception("Data not found.");
                }

                binRackAreaDTO = new BinRackAreaDTO
                {
                    ID = binRackArea.ID,
                    Code = binRackArea.Code,
                    Name = binRackArea.Name,
                    Type = binRackArea.Type,
                    WarehouseCode = binRackArea.Warehouse.Code,
                    WarehouseName = binRackArea.Warehouse.Name,
                    IsActive = binRackArea.IsActive,
                    CreatedBy = binRackArea.CreatedBy,
                    CreatedOn = binRackArea.CreatedOn.ToString(),
                    ModifiedBy = binRackArea.ModifiedBy != null ? binRackArea.ModifiedBy : "",
                    ModifiedOn = binRackArea.ModifiedOn.ToString()
                };

                status = true;
                message = "Fetch data succeded.";

                obj.Add("area_type", Constant.AreaTypes());

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

            obj.Add("data", binRackAreaDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(BinRackAreaVM binRackAreaVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string areaID = null;

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
                    if (!string.IsNullOrEmpty(binRackAreaVM.AreaName))
                    {
                        BinRackArea tempBinRackArea = await db.BinRackAreas.Where(s => s.Name.ToLower().Equals(binRackAreaVM.AreaName.ToLower())).FirstOrDefaultAsync();

                        if (tempBinRackArea != null)
                        {
                            ModelState.AddModelError("BinRackArea.AreaName", "Area Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.AreaName", "Area Name is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackAreaVM.Type))
                    {
                        if (!Constant.AreaTypes().Contains(binRackAreaVM.Type.ToUpper()))
                        {
                            ModelState.AddModelError("BinRackArea.AreaType", "Area Type is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.AreaType", "Area Type is required.");
                    }

                    //if (!string.IsNullOrEmpty(binRackAreaVM.Code))
                    //{
                    //    BinRackArea tempBinRackArea = await db.BinRackAreas.Where(s => s.Name.ToLower().Equals(binRackAreaVM.Code.ToLower())).FirstOrDefaultAsync();

                    //    if (tempBinRackArea != null)
                    //    {
                    //        ModelState.AddModelError("BinRackArea.Code", "Area Code is already registered.");
                    //    }
                    //}
                    //else
                    //{
                    //    ModelState.AddModelError("BinRackArea.Code", "Area Code is required.");
                    //}

                    if (!string.IsNullOrEmpty(binRackAreaVM.WarehouseCode))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(binRackAreaVM.WarehouseCode) && s.IsActive == true).FirstOrDefaultAsync();

                        if (wh == null)
                        {
                            ModelState.AddModelError("BinRackArea.WarehouseCode", "Warehouse does not exist.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.WarehouseCode", "Warehouse is required.");
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

                    string prefix = "AR";
                    areaID = Helper.CreateGuid(prefix);

                    string lastNumber = db.BinRackAreas.AsQueryable().OrderByDescending(x => x.Code).AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    Warehouse warehouse = await db.Warehouses.Where(s => s.Code.Equals(binRackAreaVM.WarehouseCode) && s.IsActive == true).FirstOrDefaultAsync();

                    BinRackArea binRackArea = new BinRackArea
                    {
                        ID = areaID,
                        Code = prefix + string.Format("{0:D3}", currentNumber + 1),
                        //Code = Helper.ToUpper(binRackAreaVM.Code),
                        Name = Helper.ToUpper(binRackAreaVM.AreaName),
                        Type = binRackAreaVM.Type,
                        WarehouseCode = warehouse.Code,
                        WarehouseName = warehouse.Name,
                        IsActive = true,
                        CreatedBy = activeUser,
                        CreatedOn = DateTime.Now
                    };

                    db.BinRackAreas.Add(binRackArea);
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
        public async Task<IHttpActionResult> Update(BinRackAreaVM binRackAreaVM)
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
                    BinRackArea binRackArea = await db.BinRackAreas.Where(x => x.ID.Equals(binRackAreaVM.ID)).FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(binRackAreaVM.ID))
                    {
                        binRackArea = await db.BinRackAreas.Where(x => x.ID.Equals(binRackAreaVM.ID)).FirstOrDefaultAsync();

                        if (binRackArea == null)
                        {
                            ModelState.AddModelError("BinRackArea.AreaName", "Area is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.AreaName", "Area ID is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackAreaVM.AreaName))
                    {
                        BinRackArea tempBinRackArea = await db.BinRackAreas.Where(s => s.Name.ToLower().Equals(binRackAreaVM.AreaName.ToLower()) && !s.ID.Equals(binRackAreaVM.ID)).FirstOrDefaultAsync();

                        if (tempBinRackArea != null)
                        {
                            ModelState.AddModelError("BinRackArea.AreaName", "Area Name is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.AreaName", "Area Name is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackAreaVM.Type))
                    {
                        if (!Constant.AreaTypes().Contains(binRackAreaVM.Type.ToUpper()))
                        {
                            ModelState.AddModelError("BinRackArea.AreaType", "Area Type is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.AreaType", "Area Type is required.");
                    }

                    if (!string.IsNullOrEmpty(binRackAreaVM.WarehouseCode))
                    {
                        Warehouse wh = await db.Warehouses.Where(s => s.Code.Equals(binRackAreaVM.WarehouseCode) && s.IsActive == true).FirstOrDefaultAsync();

                        if (wh == null)
                        {
                            ModelState.AddModelError("BinRackArea.WarehouseCode", "Warehouse does not exist.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("BinRackArea.WarehouseCode", "Warehouse is required.");
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

                    binRackArea.Name = Helper.ToUpper(binRackAreaVM.AreaName);
                    binRackArea.Type = binRackAreaVM.Type;
                    binRackArea.IsActive = binRackAreaVM.IsActive;
                    binRackArea.ModifiedBy = activeUser;
                    binRackArea.ModifiedOn = DateTime.Now;

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
                    BinRackArea binRackArea = await db.BinRackAreas.Where(x => x.ID.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (binRackArea != null)
                    {
                        binRackArea.IsActive = false;
                        binRackArea.ModifiedBy = activeUser;
                        binRackArea.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "Area is successfully deleted.";

                    }
                    else
                    {
                        message = "Area is not exist.";
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

        public IHttpActionResult GetAreaType()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Area type found.";
            bool status = true;

            obj.Add("area_type", Constant.AreaTypes());
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
        [HttpGet]
        public IHttpActionResult WarehouseAreabyType(string id)
        {
            var binRacks = db.BinRackAreas.Where(m => m.WarehouseCode.Equals(id)).FirstOrDefault();
            var type = binRacks.Warehouse.Type;
            IQueryable<BinRackArea> query;
            IQueryable<BinRackAreaDTO> list;
            if (type == "EMIX")
            {
                query = db.BinRackAreas.AsQueryable().Where(m => m.Warehouse.Type.Equals(type)).OrderBy(m => m.Code);
            }
            else
            {
                query = db.BinRackAreas.AsQueryable().Where(m => m.WarehouseCode.Equals(id)).OrderBy(m => m.Code);

            }
            list = query.Select(x => new BinRackAreaDTO()
            {
                ID = x.ID,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn.ToString(),
                ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                ModifiedOn = x.ModifiedOn.ToString()
            });
       
            return Ok(list);
        }
    }

}
