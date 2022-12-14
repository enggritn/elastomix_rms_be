using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public partial class SemiFinishGoodVM
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCategoryName { get; set; }
        public string CustomerProductName { get; set; }
        public int ExpiredDate { get; set; }
        public string LifeRange { get; set; }
        public string AB { get; set; }
        public decimal WeightPerBag { get; set; }
        public decimal PerPalletWeight { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public partial class SemiFinishGoodDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCategoryName { get; set; }
        public string CustomerProductName { get; set; }
        public string ExpiredDate { get; set; }
        public string LifeRange { get; set; }
        public string AB { get; set; }
        public string WeightPerBag { get; set; }
        public string PerPalletWeight { get; set; }
        public string UoM { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class StockSFGDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string RecipeQty { get; set; }
        public string ProductionQty { get; set; }
        public string TotalQty { get; set; }
        public string OutstandingQty { get; set; }
    }
}
