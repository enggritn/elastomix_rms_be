using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class PutAwayItemModel
    {
        public string ID { get; set; }
        public string ReceivingID { get; set; }
        public string Barcode { get; set; }
        public decimal QtyActual { get; set; }
        public decimal QtyPerBag { get; set; }
        public decimal QtyBag { get; set; }
        public decimal AvailableQTYBag { get; set; }

    }
}