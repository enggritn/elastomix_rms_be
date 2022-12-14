using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class MenuControlDTO
    {
        public string ID { get; set; }
        public List<MenuDTO> Menus { get; set; }
    }
}