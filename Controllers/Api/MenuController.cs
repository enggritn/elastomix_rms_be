using System;
using System.Collections.Generic;
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
    public class MenuController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpGet]
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<Menu> list = Enumerable.Empty<Menu>();

            try
            {
                list = await db.Menus.ToListAsync();
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
            Menu menu = new Menu();

            try
            {
                menu = await db.Menus.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
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

            obj.Add("data", menu);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        public async Task<IHttpActionResult> GetMenuByPermission()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            IEnumerable<MainMenuDTO> listMenu = Enumerable.Empty<MainMenuDTO>();

            try
            {
                string token = "";
                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                if (token != null)
                {

                    IEnumerable<Menu> menus = await db.Menus.Where(x => x.IsActive == true).ToListAsync();
                    IEnumerable<MainMenu> mainMenus = menus.Select(x => x.MainMenu).Distinct().ToList();

                    listMenu = from x in mainMenus.OrderBy(z => z.Index)
                               select new MainMenuDTO
                               {

                                   ID = x.ID,
                                   Name = x.Name,
                                   ChildMenus = from y in menus.Where(z => z.ParentID.Equals(x.ID)).OrderBy(z => z.Index)
                                                select new MenuDTO
                                                {
                                                    ID = y.ID,
                                                    Name = y.Name,
                                                    Path = y.Path
                                                }
                               };
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
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

            obj.Add("data", listMenu);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetMobileMenuByPermission()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            List<MobileMenuDTO> menus = new List<MobileMenuDTO>();

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
                    //get permission by role id
                    List<Permission> permissions = await db.Permissions
                        .Where(m => m.MenuControl.Menu.MainMenu.IsMobile)
                        .Where(m => m.RoleID.Equals(activeUser.RoleID))
                        .Where(m => m.MenuControl.Control.Name.Equals("View"))
                        .OrderBy(m => m.MenuControl.Menu.Index).ToListAsync();
                    foreach(Permission permission in permissions)
                    {
                        MobileMenuDTO menu = new MobileMenuDTO();
                        menu.name = permission.MenuControl.Menu.Path;
                        //count available transaction
                        menu.job_count = TotalTask(menu.name, activeUser.Username);

                        menus.Add(menu);
                    }

                    status = true;
                    message = "Menu loaded.";
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

            obj.Add("menus", menus);
            obj.Add("status", status);
            obj.Add("message", message);
            return Ok(obj);
        }

        private int TotalTask(string menu_name, string username)
        {
            int job_count = 0;


            switch (menu_name)
            {
                case "receiving_rm":
                    job_count = db.Receivings.Where(m => (m.TransactionStatus.Equals("OPEN") || m.TransactionStatus.Equals("PROGRESS"))
                                && !string.IsNullOrEmpty(m.RefNumber)).Count();
                    break;
                case "issue_slip":
                    job_count = db.IssueSlipHeaders.Where(m => (m.TransactionStatus.Equals("OPEN") || m.TransactionStatus.Equals("PROGRESS"))).Count();
                    break;
                case "receiving_sfg":
                    job_count = db.vReceivingSFG2.AsQueryable().Count(); ;
                    break;
                case "qc_inspection":
                    job_count = db.QCInspections.Where(m => !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
                    break;
                case "other_inbound":
                    job_count = db.InboundHeaders.Where(m => !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
                    break;
                case "other_outbound":
                    job_count = db.OutboundHeaders.Where(m => !m.TransactionStatus.Equals("CLOSED") && !m.TransactionStatus.Equals("CANCELLED")).AsQueryable().Count();
                    break;
                case "stock_movement":
                    job_count = db.Movements.Where(s => string.IsNullOrEmpty(s.NewBinRackCode) && s.CreatedBy.Equals(username)).Count();
                    break;
                case "stock_transform":
                    job_count = db.Transforms.AsQueryable().Count();
                    break;
                case "stock_opname":
                    job_count = db.StockOpnameHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).Count();
                    break;
            }

            return job_count;
        }
    }
}
