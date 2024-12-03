using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class ControlController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpGet]
        public async Task<IHttpActionResult> GetData()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IEnumerable<Control> list = Enumerable.Empty<Control>();

            try
            {
                list = await db.Controls.ToListAsync();
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
            Control control = new Control();

            try
            {
                control = await db.Controls.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
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

            obj.Add("data", control);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> ListPrinter()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            try
            {
                List<PrinterDTO> printers = new List<PrinterDTO>();

                PrinterDTO printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_1_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_1_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_2_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_2_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_3_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_3_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_4_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_4_name"].ToString();

                printers.Add(printer);

                //Dictionary<string, object> printers = new Dictionary<string, object>();
                //printers.Add(ConfigurationManager.AppSettings["printer_1_ip"].ToString(), ConfigurationManager.AppSettings["printer_1_name"].ToString());
                //printers.Add(ConfigurationManager.AppSettings["printer_2_ip"].ToString(), ConfigurationManager.AppSettings["printer_2_name"].ToString());

                obj.Add("printers", printers);
                status = true;
                message = "Printer found.";
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
        public async Task<IHttpActionResult> ListPrinterWeb()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            try
            {
                List<PrinterDTO> printers = new List<PrinterDTO>();

                PrinterDTO printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_1_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_1_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_2_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_2_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_3_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_3_name"].ToString();

                printers.Add(printer);

                printer = new PrinterDTO();
                printer.PrinterIP = ConfigurationManager.AppSettings["printer_4_ip"].ToString();
                printer.PrinterName = ConfigurationManager.AppSettings["printer_4_name"].ToString();

                printers.Add(printer);
                obj.Add("printers", printers);
                status = true;
                message = "Printer found.";
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
