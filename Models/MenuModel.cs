using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class MenuDTO
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public IEnumerable<ControlDTO> Controls { get; set; }
        public string Path { get; set; }
    }


    public class MobileMenuDTO
    {
        public string name { get; set; }
        public int job_count { get; set; }
    }
}