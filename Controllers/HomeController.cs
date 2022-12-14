using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WMS_BE.Controllers
{
    public class HomeController : Controller
    {
        public void Index()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("System ready.");

            Response.Write(sb.ToString());
        }

    }
}
