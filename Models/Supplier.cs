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
    
    public partial class Supplier
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string ClassificationName { get; set; }
        public string Address { get; set; }
        public Nullable<System.DateTime> DevelopmentDate { get; set; }
        public string Telephone { get; set; }
        public string Contact { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
    }
}
