using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class UserController : ApiController
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

            IEnumerable<User> list = Enumerable.Empty<User>();
            IEnumerable<UserDTO> pagedData = Enumerable.Empty<UserDTO>();

            IQueryable<User> query = db.Users.AsQueryable();

            int recordsTotal = db.Users.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Username.Contains(search) ||
                           m.FullName.Contains(search));

                Dictionary<string, Func<User, object>> cols = new Dictionary<string, Func<User, object>>();
                cols.Add("Username", x => x.Username);
                cols.Add("FullName", x => x.FullName);
                cols.Add("Role.Name", x => x.Role != null ? x.Role.Name : "");
                cols.Add("AreaType", x => x.AreaType);
                cols.Add("MobileUser", x => x.MobileUser);
                cols.Add("PhotoURL", x => x.Photo);
                cols.Add("IsActive", x => x.IsActive);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy != null ? x.ModifiedBy : "");
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                list = list.Select(x => new User
                {
                    Username = x.Username,
                    FullName = x.FullName,
                    Role = new Role
                    {
                        ID = x.Role != null ? x.RoleID : "",
                        Name = x.Role != null ? x.Role.Name : ""
                    },
                    AreaType = x.AreaType,
                    MobileUser = x.MobileUser,
                    Photo = x.Photo,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                });

                recordsFiltered = list.Count();


                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from x in list
                                select new UserDTO
                                {
                                    Username = x.Username,
                                    FullName = x.FullName,
                                    AreaType = x.AreaType,
                                    MobileUser = x.MobileUser,
                                    PhotoURL = !string.IsNullOrEmpty(x.Photo) && File.Exists(Path.Combine(HttpContext.Current.Server.MapPath("~/Content/photos"), x.Photo)) ? "Content/photos/" + x.Photo : "Content/photos/default.png",
                                    IsActive = x.IsActive,
                                    RoleName = x.Role != null ? x.Role.Name : "-",
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = x.CreatedOn.ToString(),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "-",
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
            IEnumerable<UserDTO> list = Enumerable.Empty<UserDTO>();

            try
            {
                IEnumerable<User> tempList = await db.Users.Where(m => m.IsActive == true).ToListAsync();
                list = from x in tempList
                       select new UserDTO
                       {
                           Username = x.Username,
                           FullName = x.FullName,
                           RoleID = x.RoleID != null ? x.RoleID : "",
                           RoleName = x.Role != null ? x.Role.Name : "",
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
        public async Task<IHttpActionResult> GetDataById(string Username)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            UserDTO userDTO = new UserDTO();

            try
            {
                User user = await db.Users.Where(m => m.Username.Equals(Username)).FirstOrDefaultAsync();
                userDTO.Username = user.Username;
                userDTO.FullName = user.FullName;
                userDTO.RoleID = user.RoleID != null ? user.RoleID : "";
                userDTO.RoleName = user.Role != null ? user.Role.Name : "";
                userDTO.AreaType = user.AreaType;
                userDTO.MobileUser = user.MobileUser;
                userDTO.IsActive = user.IsActive;
                userDTO.CreatedBy = user.CreatedBy;
                userDTO.CreatedOn = user.CreatedOn.ToString();
                userDTO.ModifiedBy = user.ModifiedBy != null ? user.ModifiedBy : "";
                userDTO.ModifiedOn = user.ModifiedOn.ToString();


                string PhotoURL = "Content/photos/default.png";


                if (!string.IsNullOrEmpty(user.Photo))
                {
                    var prev_path = Path.Combine(
                         HttpContext.Current.Server.MapPath("~/Content/photos"),
                         user.Photo
                     );

                    if (File.Exists(prev_path))
                    {
                        PhotoURL = "Content/photos/" + user.Photo;
                    }
                }

                userDTO.PhotoURL = PhotoURL;

                obj.Add("area_type", Constant.AreaTypes());

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

            obj.Add("data", userDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Create(UserVM userVM)
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
                    if (!string.IsNullOrEmpty(userVM.Username))
                    {
                        User tempUser = await db.Users.Where(s => s.Username.ToLower().Equals(userVM.Username.ToLower())).FirstOrDefaultAsync();

                        if (tempUser != null)
                        {
                            ModelState.AddModelError("User.Username", "Username is already registered.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("User.Username", "Username is required.");
                    }

                    if (!string.IsNullOrEmpty(userVM.FullName))
                    {
                        //User tempUser = await db.Users.Where(s => s.FullName.ToLower().Equals(userVM.FullName.ToLower())).FirstOrDefaultAsync();

                        //if (tempUser != null)
                        //{
                        //    ModelState.AddModelError("User.FullName", "Email is already registered.");
                        //}
                    }
                    else
                    {
                        ModelState.AddModelError("User.FullName", "Full Name is required.");
                    }


                    if (!string.IsNullOrEmpty(userVM.RoleID))
                    {
                        Role tempRole = await db.Roles.Where(s => s.ID.Equals(userVM.RoleID) && s.IsActive == true).FirstOrDefaultAsync();

                        if (tempRole == null)
                        {
                            ModelState.AddModelError("User.RoleID", "Role does not exist.");
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


                    User user = new User();
                    user.Username = Helper.ToLower(userVM.Username);
                    user.Password = Cryptolib.HashPassword(userVM.Password);
                    user.FullName = userVM.FullName;
                    user.RoleID = userVM.RoleID;
                    user.AreaType = userVM.AreaType;
                    user.MobileUser = userVM.MobileUser;
                    user.IsActive = true;
                    user.CreatedBy = activeUser;
                    user.CreatedOn = DateTime.Now;

                    id = user.Username;



                    db.Users.Add(user);
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
        public async Task<IHttpActionResult> Update(UserVM userVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                ModelState["userVM.Password"].Errors.Clear();
                ModelState["userVM.PasswordConfirmation"].Errors.Clear();

                string token = "";
                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    User user = null;

                    if (!string.IsNullOrEmpty(userVM.Username))
                    {
                        user = await db.Users.Where(x => x.Username.ToLower().Equals(userVM.Username.ToLower())).FirstOrDefaultAsync();

                        if (user == null)
                        {
                            ModelState.AddModelError("User.Username", "User is not recognized.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("User.Username", "Username is required.");
                    }

                    if (!string.IsNullOrEmpty(userVM.FullName))
                    {
                        //User tempUser = await db.Users.Where(s => s.FullName.ToLower().Equals(userVM.FullName.ToLower()) && !s.Username.Equals(userVM.Username)).FirstOrDefaultAsync();

                        //if (tempUser != null)
                        //{
                        //    ModelState.AddModelError("User.FullName", "Email is already registered.");
                        //}
                    }
                    else
                    {
                        ModelState.AddModelError("User.FullName", "Full Name is required.");
                    }

                    if (!string.IsNullOrEmpty(userVM.RoleID))
                    {
                        Role tempRole = await db.Roles.Where(s => s.ID.Equals(userVM.RoleID)).FirstOrDefaultAsync();

                        if (tempRole == null)
                        {
                            ModelState.AddModelError("User.RoleID", "Role does not exist.");
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

                    user.FullName = userVM.FullName;
                    user.AreaType = userVM.AreaType;
                    user.MobileUser = userVM.MobileUser;
                    user.RoleID = userVM.RoleID;
                    user.IsActive = userVM.IsActive;
                    user.ModifiedBy = activeUser;
                    user.ModifiedOn = DateTime.Now;

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
        public async Task<IHttpActionResult> UploadPhoto(string username)
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
                    User user = await db.Users.Where(x => x.Username.Equals(username)).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        throw new Exception("Data not found.");
                    }

                    //upload photo
                    if (request.Files.Count > 0)
                    {
                        HttpPostedFile file = request.Files[0];
                        string file_name = string.Format("{0}{1}", user.Username, Path.GetExtension(file.FileName));
                        var fileName = Path.GetFileName(file.FileName);

                        var path = Path.Combine(
                            HttpContext.Current.Server.MapPath("~/Content/photos"),
                            file_name
                        );

                        //check if previous image exist, then delete

                        if (!string.IsNullOrEmpty(user.Photo))
                        {
                            var prev_path = Path.Combine(
                          HttpContext.Current.Server.MapPath("~/Content/photos"),
                          user.Photo
                      );

                            if (File.Exists(prev_path))
                            {
                                File.Delete(prev_path);
                            }
                        }


                        if (Directory.Exists(HttpContext.Current.Server.MapPath("~/Content/photos")))
                        {
                            file.SaveAs(path);

                            user.Photo = file_name;
                        }


                    }


                    await db.SaveChangesAsync();
                    status = true;
                    message = "Upload photo succeeded.";
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
        public async Task<IHttpActionResult> Delete(string username)
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
                    User user = await db.Users.Where(x => x.Username.ToLower().Equals(username.ToLower()) && x.IsActive).FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.IsActive = false;
                        user.Token = null;
                        user.ModifiedBy = activeUser;
                        user.ModifiedOn = DateTime.Now;

                        await db.SaveChangesAsync();
                        status = true;
                        message = "User is successfully deleted.";

                    }
                    else
                    {
                        message = "User is not exist.";
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

        [HttpPost]
        public async Task<IHttpActionResult> ChangePassword(ChangePassVM dataVM)
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
                    User user = await db.Users.Where(x => x.Token.Equals(token)).FirstOrDefaultAsync();

                    if (string.IsNullOrEmpty(dataVM.CurrentPassword))
                    {
                        ModelState.AddModelError("Profile.CurrentPassword", "Current password is required.");
                    }
                    else
                    {
                        
                        if (!Cryptolib.ValidatePassword(dataVM.CurrentPassword, user.Password))
                        {
                            ModelState.AddModelError("Profile.CurrentPassword", "Invalid current password.");
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.NewPassword))
                    {
                        ModelState.AddModelError("Profile.NewPassword", "New password is required.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(dataVM.PasswordConfirmation))
                        {
                            if (!dataVM.NewPassword.Equals(dataVM.PasswordConfirmation))
                            {
                                ModelState.AddModelError("Profile.PasswordConfirmation", " Password confirmation not match.");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(dataVM.PasswordConfirmation))
                    {
                        ModelState.AddModelError("Profile.PasswordConfirmation", "Password confirmation is required.");

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

                    user.Password = Cryptolib.HashPassword(dataVM.NewPassword);

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Change Password succeeded.";

                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
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
        public async Task<IHttpActionResult> GetMobileUsers(string AreaType)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<MobileUserDTO> data = Enumerable.Empty<MobileUserDTO>(); ;

            try
            {

               
                IEnumerable<User> list = Enumerable.Empty<User>();
                IQueryable<User> query = db.Users.AsQueryable().Where(m => m.AreaType.Equals(AreaType) && m.MobileUser).OrderBy(m => m.FullName);
                list = await query.ToListAsync();

                data = from item in list
                       select new MobileUserDTO
                       {
                           Username = item.Username,
                           FullName = item.FullName,
                           Photo = !string.IsNullOrEmpty(item.Photo) && File.Exists(Path.Combine(HttpContext.Current.Server.MapPath("~/Content/photos"), item.Photo)) ? "Content/photos/" + item.Photo : "Content/photos/default.png",
                           LoginStatus = !string.IsNullOrEmpty(item.Token) ? true : false
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
