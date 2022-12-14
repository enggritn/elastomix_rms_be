using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class UserVM
    {

        [Required(ErrorMessage = "Username is required.")]
        [MinLength(5, ErrorMessage = "Username can not less than 5 characters.")]
        [MaxLength(30, ErrorMessage = "Username can not more than 30 characters.")]
        [RegularExpression("^[a-zA-Z][a-zA-Z0-9.]*$", ErrorMessage = "Username is not valid.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(5, ErrorMessage = "Password can not less than 5 characters.")]
        [MaxLength(20, ErrorMessage = "Password can not more than 20 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Password Confirmation is required.")]
        [Compare("Password", ErrorMessage = "Password does not match.")]
        [DataType(DataType.Password)]
        public string PasswordConfirmation { get; set; }

        //[Required(ErrorMessage = "Email is required.")]
        //[EmailAddress(ErrorMessage = "Email is not valid.")]
        //[MaxLength(50, ErrorMessage = "Email can not more than 50 characters.")]
        //public string UserEmail { get; set; }
        public string FullName { get; set; }
        public string RoleID { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string AreaType { get; set; }
        public bool MobileUser { get; set; }
    }

    public class UserDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string PasswordConfirmation { get; set; }
        public string FullName { get; set; }
        public string AreaType { get; set; }
        public bool MobileUser { get; set; }
        public string RoleID { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string PhotoURL { get; set; }
    }


    public class ChangePassVM
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string PasswordConfirmation { get; set; }
    }

    public class ForgotPassVM
    {
        public string Username { get; set; }
    }

    public class MobileUserDTO
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Photo { get; set; }
        public bool LoginStatus { get; set; }
    }
}