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
    
    public partial class FormulaDetail
    {
        public string ID { get; set; }
        public string RecipeNumber { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Type { get; set; }
        public decimal Qty { get; set; }
        public string UoM { get; set; }
        public int Fullbag { get; set; }
        public decimal RemainderQty { get; set; }
    
        public virtual Formula Formula { get; set; }
    }
}
