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
    
    public partial class vDataInOutSummary
    {
        public string ItemCode { get; set; }
        public System.DateTime Date { get; set; }
        public string UserHanheld { get; set; }
        public string Type { get; set; }
        public Nullable<decimal> ReceiveQty { get; set; }
        public Nullable<decimal> IssueSlipQty { get; set; }
        public Nullable<decimal> BalanceQty { get; set; }
    }
}
