using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class MainMenuDTO
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public IEnumerable<MenuDTO> ChildMenus { get; set; }
    }
}