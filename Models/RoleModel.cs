using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class RoleVM
    {
        public string ID { get; set; }

        [Required(ErrorMessage = "Role Name is required.")]
        [MinLength(5, ErrorMessage = "Role Name can not less than 5 characters.")]
        [MaxLength(50, ErrorMessage = "Role Name can not more than 50 characters.")]
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public List<string> MenuControlIDs { get; set; }
    }

    public class RoleDTO
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, Dictionary<string, string>> Permissions { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }
}