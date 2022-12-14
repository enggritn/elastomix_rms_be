using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class PurchaseOrderHeaderVM
    {
        public string ID { get; set; }
        public string PONumber { get; set; }
        public DateTime PODate { get; set; }
        public string PurchaseRequestID { get; set; }
        public string SupplierID { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public DateTime ETA { get; set; }
        public DateTime ATA { get; set; }
        public IEnumerable<PurchaseOrderDetailVM> Details { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class PurchaseOrderDetailVM
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public decimal QtyNeeded { get; set; }
        public decimal OrderQty { get; set; }
    }

    public class PurchaseOrderHeaderDTO
    {
        public string ID { get; set; }
        public string PONumber { get; set; }
        public string PODate { get; set; }
        public string PurchaseRequestID { get; set; }
        public string SupplierID { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string ETA { get; set; }
        public string ATA { get; set; }
        public List<PurchaseOrderDetailDTO> Details { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class PurchaseOrderDetailDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string FullbagQty { get; set; }
        public string RequestedQty { get; set; }
        public string OrderQty { get; set; }
        public string OutstandingQty { get; set; }
        public string Fullbag { get; set; }
        public string RemainderQty { get; set; }
    }

    public class RequestOrderDTO
    {
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string FullbagQty { get; set; }
        public string RequestedQty { get; set; }
        public string OrderedQty { get; set; }
        public string OutstandingQty { get; set; }
        public string Fullbag { get; set; }
        public string RemainderQty { get; set; }
    }

    public class PurchaseOrderStatusVM
    {
        public string PurchaseRequestID { get; set; }
        public string PurchaseOrderID { get; set; }
    }
}