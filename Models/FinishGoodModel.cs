using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{ 
    public partial class FinishGoodVM
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Specifications { get; set; }
        public string StockCode { get; set; }
        public string StockCategoryCode { get; set; }
        public string StockCategoryName { get; set; }
        public int InputTaxRate { get; set; }
        public int OutputTaxRate { get; set; }
        public DateTime? EnabledDate { get; set; }
        public string ABLIAN { get; set; }
        public decimal Factor { get; set; }
        public decimal WeightPerBag { get; set; }
        public decimal SpecificGravity { get; set; }
        public decimal PerPalletWeight { get; set; }
        public string UoM { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public partial class FinishGoodDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Specifications { get; set; }
        public string StockCode { get; set; }
        public string StockCategoryCode { get; set; }
        public string StockCategoryName { get; set; }
        public string InputTaxRate { get; set; }
        public string OutputTaxRate { get; set; }
        public string EnabledDate { get; set; }
        public string ABLIAN { get; set; }
        public string Factor { get; set; }
        public string WeightPerBag { get; set; }
        public string SpecificGravity { get; set; }
        public string PerPalletWeight { get; set; }
        public string UoM { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }
}
