using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    //public class ProductionRequestHeaderVM
    //{
    //    public string ID { get; set; }
    //    public string Code { get; set; }
    //    public string IssuedNumber { get; set; }
    //    public DateTime IssuedDate { get; set; }
    //    public List<ProductionRequestDetailVM> Details { get; set; }
    //    public string TransactionStatus { get; set; }
    //    public string CreatedBy { get; set; }
    //    public string CreatedOn { get; set; }
    //    public string ModifiedBy { get; set; }
    //    public string ModifiedOn { get; set; }
    //}

    //public class ProductionRequestDetailVM
    //{
    //    public string ID { get; set; }
    //    public string HeaderID { get; set; }
    //    public string OrderNumber { get; set; }
    //    public string FGID { get; set; }
    //    public string FGCode { get; set; }
    //    public string FGMaterialCode { get; set; }
    //    public string FGMaterialName { get; set; }
    //    public string CustomerID { get; set; }
    //    public string CustomerCode { get; set; }
    //    public string CustomerName { get; set; }
    //    public decimal Qty { get; set; }
    //    public DateTime ETA { get; set; }
    //    public DateTime ATA { get; set; }
    //    public string Remarks { get; set; }
    //    public bool IsActive { get; set; }
    //    public string CreatedBy { get; set; }
    //    public string CreatedOn { get; set; }
    //    public string ModifiedBy { get; set; }
    //    public string ModifiedOn { get; set; }
    //}

    public class ProductionRequestHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string IssuedNumber { get; set; }
        public string IssuedDate { get; set; }
        public List<ProductionRequestDetailDTO> Details { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class ProductionRequestDetailDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string OrderNumber { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }
        public string ETA { get; set; }
        public string ATA { get; set; }
        public string Remarks { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string UsedQty { get; set; }
        public string AvailableQty { get; set; }
    }
}