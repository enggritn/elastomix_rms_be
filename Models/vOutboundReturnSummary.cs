//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WMS_BE.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class vOutboundReturnSummary
    {
        public string OutboundID { get; set; }
        public string ID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public decimal QtyPerBag { get; set; }
        public string LotNo { get; set; }
        public System.DateTime InDate { get; set; }
        public System.DateTime ExpDate { get; set; }
        public Nullable<decimal> TotalQty { get; set; }
        public Nullable<decimal> BagQty { get; set; }
        public Nullable<decimal> PutawayQty { get; set; }
    }
}
