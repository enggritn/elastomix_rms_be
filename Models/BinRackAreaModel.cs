using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class BinRackAreaVM
    {
        public string ID { get; set; }

        public string WarehouseCode { get; set; }

        [Required(ErrorMessage = "Area Name is required.")]
        [MinLength(5, ErrorMessage = "Area Name can not less than 5 characters.")]
        [MaxLength(50, ErrorMessage = "Area Name can not more than 50 characters.")]
        public string AreaName { get; set; }
        //public string Code { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class BinRackAreaDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }
}