using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class PRBoxApprovalModel
    {
        public PRBoxApprovalModel()
        {
            Details = new List<PRBoxApprovalDetailModel>();
        }

        [Required]
        public string PRBoxGroupCode { get; set; }
        public string PRBoxGroupDescription { get; set; }

        public List<PRBoxApprovalDetailModel> Details { get; set; }
    }

    public class PRBoxApprovalDetailModel
    {
        public int SequenceNo { get; set; }
        [Required]
        public string PRBoxName { get; set; }
        [Required]
        public string PRBoxTitle { get; set; }
    }

}