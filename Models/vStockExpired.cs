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
    
    public partial class vStockExpired
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string LotNumber { get; set; }
        public Nullable<System.DateTime> InDate { get; set; }
        public Nullable<System.DateTime> ExpiredDate { get; set; }
        public Nullable<decimal> TotalQty { get; set; }
        public Nullable<int> ExpirationDay { get; set; }
    }
}
