using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
    public class RoleController : ApiController
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

            IEnumerable<Role> list = Enumerable.Empty<Role>();
            IEnumerable<RoleDTO> pagedData = Enumerable.Empty<RoleDTO>();

            IQueryable<Role> query = db.Roles.AsQueryable();

            int recordsTotal = db.Roles.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Name.Contains(search));

                Dictionary<string, Func<Role, object>> cols = new Dictionary<string, Func<Role, object>>();
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
                                select new RoleDTO
                                {
                                    ID = x.ID,
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
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<RoleVM> list = Enumerable.Empty<RoleVM>();

            try
            {
                IEnumerable<Role> tempList = await db.Roles.Where(m => m.IsActive == true).ToListAsync();

                list = from x in tempList
                       select new RoleVM
                       {
                           ID = x.ID,
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
            RoleDTO roleDTO = null;

            IEnumerable<MenuDTO> listMenu = Enumerable.Empty<MenuDTO>();

            try
            {
                Role role = await db.Roles.Where(m => m.IsActive == true).Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

                if(role == null)
                {
                    throw new Exception("Data not found.");
                }
                roleDTO = new RoleDTO();
                roleDTO.ID = role.ID;
                roleDTO.Name = role.Name;
                roleDTO.IsActive = role.IsActive;
                roleDTO.CreatedBy = role.CreatedBy;
                roleDTO.CreatedOn = role.CreatedOn.ToString();
                roleDTO.ModifiedBy = role.ModifiedBy != null ? role.ModifiedBy : "";
                roleDTO.ModifiedOn = role.ModifiedOn.ToString();

                IEnumerable<Menu> list = Enumerable.Empty<Menu>();

                list = await db.Menus.Where(x => x.IsActive == true).ToListAsync();
                List<string> MenuControlIDs = role.Permissions.Select(x => x.MenuControlID).ToList();

                listMenu =  from x in list
                            select new MenuDTO
                            {

                                ID = x.ID,
                                Name = x.Name,
                                Controls =  from y in x.MenuControls.OrderBy(z => z.Control.Index)
                                            select new ControlDTO
                                            {
                                                ID = y.ControlID,
                                                Name = y.Control.Name,
                                                Parent = y.Menu.MainMenu.Name,
                                                MenuControlID = y.ID,
                                                IsChecked = MenuControlIDs.Contains(y.ID) ? true : false
                                            }
                            };


                //roleDTO.MenuControls = permissions.Select(m => m.MenuControl).ToList();

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

            obj.Add("data", roleDTO);
            obj.Add("listMenu", listMenu);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(RoleVM roleVM)
        {
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
            Dictionary<string, object> obj = new Dictionary<string, object>();
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
                    if (!string.IsNullOrEmpty(roleVM.Name))
                    {
                        Role tempRole = await db.Roles.Where(s => s.Name.ToLower().Equals(roleVM.Name.ToLower())).FirstOrDefaultAsync();

                        if (tempRole != null)
                        {
                            ModelState.AddModelError("Role.Name", "Name is already exist.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Role.Name", "Name is required.");
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

                    Role role = new Role();
                    role.ID = Helper.CreateGuid("R");
                    role.Name = Helper.UpperFirstCase(roleVM.Name);
                    role.IsActive = true;
                    role.CreatedBy = activeUser;
                    role.CreatedOn = DateTime.Now;
                    role.Permissions = new List<Permission>();

                    if (roleVM.MenuControlIDs != null && roleVM.MenuControlIDs.Count() > 0)
                    {
                        foreach (string x in roleVM.MenuControlIDs)
                        {
                            Permission permission = new Permission();
                            permission.ID = Helper.CreateGuid("P");
                            permission.RoleID = roleVM.ID;
                            permission.MenuControlID = x;

                            role.Permissions.Add(permission);
                        }
                    }

                    id = role.ID;

                    db.Roles.Add(role);
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
        public async Task<IHttpActionResult> Update(RoleVM roleVM)
        {
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();
            Dictionary<string, object> obj = new Dictionary<string, object>();
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
                    Role role = null;

                    if (!string.IsNullOrEmpty(roleVM.Name))
                    {
                        role = await db.Roles.Where(x => x.ID.Equals(roleVM.ID)).FirstOrDefaultAsync();

                        if (role == null)
                        {
                            ModelState.AddModelError("Role.ID", "Role is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Role.ID", "ID is required.");
                    }

                    if (!string.IsNullOrEmpty(roleVM.Name))
                    {
                        Role tempRole = await db.Roles.Where(s => s.Name.ToLower().Equals(roleVM.Name.ToLower()) && !s.ID.Equals(roleVM.ID)).FirstOrDefaultAsync();

                        if (tempRole != null)
                        {
                            ModelState.AddModelError("Role.Name", "Name is already exist.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Role.Name", "Name is required.");
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

                    role.Name = Helper.UpperFirstCase(roleVM.Name);
                    role.IsActive = roleVM.IsActive;

                    string[] prevMenuControlIDs = role.Permissions.ToList().Select(p => p.MenuControlID).ToArray();
                    List<Permission> newPermissions = new List<Permission>();

                    if (roleVM.MenuControlIDs != null && roleVM.MenuControlIDs.Count() > 0)
                    {
                        foreach (string id in roleVM.MenuControlIDs)
                        {
                            Permission permission = new Permission();
                            permission.ID = Helper.CreateGuid("P");

                            if (!prevMenuControlIDs.Contains(id))
                            {
                                permission.RoleID = role.ID;
                                permission.MenuControlID = id;
                            }
                            else
                            {
                                permission = role.Permissions.Where(m => m.MenuControlID.Equals(id)).FirstOrDefault();
                            }

                            newPermissions.Add(permission);
                        }
                    }

                    role.Permissions = newPermissions.ToList();

                    string[] MenuIDs = role.Permissions.Select(m => m.MenuControlID).ToArray();
                    if (MenuIDs.Count() > 0)
                    {
                        db.Permissions.RemoveRange(db.Permissions.Where(m => m.RoleID.Equals(role.ID) && !MenuIDs.Contains(m.MenuControlID)));
                    }
                    else
                    {
                        db.Permissions.RemoveRange(db.Permissions.Where(m => m.RoleID.Equals(role.ID)));
                    }

                    role.ModifiedBy = activeUser;
                    role.ModifiedOn = DateTime.Now;

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
            catch (DbEntityValidationException e)
            {
                message = "";
                foreach (var eve in e.EntityValidationErrors)
                {
                    message += string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:\n",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("- Property: \"{0}\", Error: \"{1}\"\n",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
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
                    Role role = await db.Roles.Where(x => x.ID.Equals(id) && x.IsActive).FirstOrDefaultAsync();

                    if (role != null)
                    {
                        role.IsActive = false;
                        role.ModifiedBy = activeUser;
                        role.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "User is successfully deleted.";

                    }
                    else
                    {
                        message = "User is no longer exist";
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

        [HttpGet]
        public async Task<IHttpActionResult> GetPermissions()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<MenuDTO> listMenu = Enumerable.Empty<MenuDTO>();

            try
            {
                IEnumerable<Menu> list = await db.Menus.Where(x => x.IsActive == true).ToListAsync();

                listMenu =  from x in list
                            select new MenuDTO
                            {

                                ID = x.ID,
                                Name = x.Name,
                                Controls =  from y in x.MenuControls.OrderBy(z => z.Control.Index)
                                            select new ControlDTO
                                            {
                                                ID = y.ControlID,
                                                Name = y.Control.Name,
                                                MenuControlID = y.ID
                                            }
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

            obj.Add("list", listMenu);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetPermissionsByToken(string token)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<MenuDTO> listMenu = Enumerable.Empty<MenuDTO>();

            if (token != null)
            {
                try
                {
                    IEnumerable<Menu> list = await db.Menus.Where(x => x.IsActive == true).ToListAsync();

                    listMenu = from x in list
                                select new MenuDTO
                                {

                                    ID = x.ID,
                                    Name = x.Name,
                                    Controls = from y in x.MenuControls.OrderBy(z => z.Control.Index)
                                                select new ControlDTO
                                                {
                                                    ID = y.ControlID,
                                                    Name = y.Control.Name,
                                                    MenuControlID = y.ID
                                                }
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
            }

            obj.Add("list", listMenu);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
