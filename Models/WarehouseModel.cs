using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class WarehouseVM
    {
        [Required(ErrorMessage = "Warehouse Name is required.")]
        [MinLength(3, ErrorMessage = "Warehouse Name can not less than 5 characters.")]
        [MaxLength(50, ErrorMessage = "Warehouse Name can not more than 50 characters.")]
        public string Code { get; set; }
        public string Name { get; set; }

        [Required(ErrorMessage = "Warehouse Type is required.")]
        public string Type { get; set; }

        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class WarehouseDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

}