using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class RawMaterialVM
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public decimal Qty { get; set; }
        public string UoM { get; set; }
        public int ShelfLife { get; set; }
        public string LifeRange { get; set; }
        public decimal MinPurchaseQty { get; set; }
        public string Maker { get; set; }
        public string Vendor { get; set; }
        public decimal PoRate { get; set; }
        public string ManfCd { get; set; }
        public string VendorCode { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class RawMaterialDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }
        public string ShelfLife { get; set; }
        public string LifeRange { get; set; }
        public string MinPurchaseQty { get; set; }
        public string Maker { get; set; }
        public string Vendor { get; set; }
        public string PoRate { get; set; }
        public string ManfCd { get; set; }
        public string VendorCode { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public bool IsConvertible { get; set; }
        public string MinPurchaseQtyLitre { get; set; }
        public string ActualQty { get; set; }
    }
}