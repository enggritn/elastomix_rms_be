using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class CustomValidationMessage
    {
        public CustomValidationMessage(string FieldName, string ErrorMessage)
        {
            this.FieldName = FieldName;
            this.ErrorMessage = ErrorMessage;
        }
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
    }
}