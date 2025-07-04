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
    
    public partial class QCPicking
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public QCPicking()
        {
            this.QCPutaways = new HashSet<QCPutaway>();
        }
    
        public string ID { get; set; }
        public string QCInspectionID { get; set; }
        public string StockCode { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public decimal Qty { get; set; }
        public decimal QtyPerBag { get; set; }
        public string PickedMethod { get; set; }
        public Nullable<System.DateTime> PickedOn { get; set; }
        public string PickedBy { get; set; }
    
        public virtual QCInspection QCInspection { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<QCPutaway> QCPutaways { get; set; }
    }
}
