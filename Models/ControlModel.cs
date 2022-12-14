using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class ControlDTO
    {
        public string ID { get; set; }
        public string MenuControlID { get; set; }
        public string Name { get; set; }
        public string Parent { get; set; }
        public bool IsChecked { get; set; }
    }

    public class PrinterDTO
    {
        public string PrinterIP { get; set; }
        public string PrinterName { get; set; }
    }
}