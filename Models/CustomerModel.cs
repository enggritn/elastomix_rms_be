using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class CustomerVM
    {
        public string Code { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [MinLength(5, ErrorMessage = "Customer Name can not less than 5 characters.")]
        [MaxLength(50, ErrorMessage = "Customer Name can not more than 50 characters.")]
        public string Name { get; set; }

        public string Abbreviation { get; set; }
        public string ClassificationName { get; set; }
        public string Address { get; set; }
        public string DevelopmentDate { get; set; }
        public string Telephone { get; set; }
        public string Contact { get; set; }
        public string CurrencyID { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class CustomerDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string ClassificationName { get; set; }
        public string Address { get; set; }
        public string DevelopmentDate { get; set; }
        public string Telephone { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }
}
