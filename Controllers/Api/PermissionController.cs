using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class PermissionController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpGet]
        //public async Task<IHttpActionResult> GetData()
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    IEnumerable<Permission> list = Enumerable.Empty<Permission>();

        //    try
        //    {
        //        list = await db.Permissions.ToListAsync();
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

        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string PermissionID)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            Permission permission = new Permission();

            try
            {
                permission = await db.Permissions.Where(m => m.ID.Equals(PermissionID)).FirstOrDefaultAsync();
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

            obj.Add("data", permission);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> CheckPermission(string MenuName, string ControlName)
        {
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

                User activeUser = await db.Users.Where(x => x.Token.Equals(token)).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    string menuID = await db.Menus.Where(s => s.Path.ToLower().Equals(MenuName.ToLower())).Select(s => s.ID).FirstOrDefaultAsync();
                    string controlID = await db.Controls.Where(s => s.Name.ToLower().Equals(ControlName.ToLower())).Select(s => s.ID).FirstOrDefaultAsync();

                    string menuControlID = await db.MenuControls.Where(s => s.MenuID.Equals(menuID) && s.Control.Equals(controlID))
                        .Select(s => s.ID).FirstOrDefaultAsync();
                    Permission permission = activeUser.Role.Permissions.Where(s => s.MenuControlID.Equals(menuControlID)).FirstOrDefault();

                    if (permission != null)
                    {
                        message = "Permission granted.";
                        status = true;
                    }
                    else
                    {
                        message = "No permission.";
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



    }
}
