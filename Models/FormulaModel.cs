using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class FormulaDTO
    {
        public string RecipeNumber { get; set; }
        public string ProductName { get; set; }
        public string ItemCode { get; set; }
        public string Type { get; set; }
        public IEnumerable<FormulaDetailDTO> Details { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }


    public class FormulaDetailDTO
    {
        public string ID { get; set; }
        public string UoM { get; set; }
        public string Qty { get; set; }
        public string Fullbag { get; set; }
        public string RemainderQty { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string AvailableQty { get; set; }
        public string OutstandingQty { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string Type { get; set; }
    }
}