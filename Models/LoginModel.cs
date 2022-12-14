using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool IsRemember { get; set; }

        [Required(ErrorMessage = "Login Date is required.")]
        public string LoginDate { get; set; }
    }

    public class LoginMobileVM
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool IsRemember { get; set; }

    }

    public class LoginReq
    {
        public string Username { get; set; }
    }
}