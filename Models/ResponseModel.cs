using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class ResponseModel
    {
        public ResponseModel()  // static ctor
        {
            IsSuccess = true;
            Message = "";
        }

        public ResponseModel(bool isSuccess)  // static ctor
        {
            IsSuccess = isSuccess;
            Message = "";

        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public bool Status { get; set; }
        public Object ResponseObject { get; set; }

        public void SetError()
        {
            this.IsSuccess = false;
        }

        public void SetError(string message)
        {
            this.IsSuccess = false;
            this.Message = message;
        }

        public void SetError(string text, params object[] args)
        {
            this.IsSuccess = true;
            this.Message = string.Format(text, args);
        }

        public void SetSuccess()
        {
            SetSuccess(string.Empty);
        }

        public void SetSuccess(string message)
        {
            this.IsSuccess = true;
            this.Message = message;
        }

        public void SetSuccess(string text, params object[] args)
        {
            this.IsSuccess = true;
            this.Message = string.Format(text, args);
        }
    }
}