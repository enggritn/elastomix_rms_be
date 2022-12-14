using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class UsageHeaderVM
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal TotalRequestedQty { get; set; }
        public string TransactionStatus { get; set; }
        //public List<IssueSlipDetailVM> Details { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class UsageDetailVM
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string VendorName { get; set; }
        public decimal RequestedQty { get; set; }
        public decimal PickingQty { get; set; }
        public string PickingBinRackID { get; set; }
        public string PickingBinRackCode { get; set; }
        public string PickingBinRackName { get; set; }
        public string PickingWarehouseCode { get; set; }
        public string PickingWarehouseName { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public DateTime PickedOn { get; set; }
        public bool InspectionQRLabel { get; set; }
        public bool InspectionPackage { get; set; }
        public bool InspectionExpDate { get; set; }
        public bool InspectionApproveStamp { get; set; }
        public string InspectionMethod { get; set; }
        public string InspectedBy { get; set; }
        public DateTime InspectedOn { get; set; }
        public bool Judgement { get; set; }
        public string JudgementMethod { get; set; }
        public string JudgeBy { get; set; }
        public DateTime JudgeOn { get; set; }
    }

    public class UsageHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string TotalRequestedQty { get; set; }
        public string TransactionStatus { get; set; }
        //public List<IssueSlipDetailDTO> Details { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class UsageDetailDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string VendorName { get; set; }
        public string RequestedQty { get; set; }
        public decimal PickingQty { get; set; }
        public string PickingBinRackID { get; set; }
        public string PickingBinRackCode { get; set; }
        public string PickingBinRackName { get; set; }
        public string PickingWarehouseCode { get; set; }
        public string PickingWarehouseName { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public DateTime PickedOn { get; set; }
        public bool InspectionQRLabel { get; set; }
        public bool InspectionPackage { get; set; }
        public bool InspectionExpDate { get; set; }
        public bool InspectionApproveStamp { get; set; }
        public string InspectionMethod { get; set; }
        public string InspectedBy { get; set; }
        public DateTime InspectedOn { get; set; }
        public bool Judgement { get; set; }
        public string JudgementMethod { get; set; }
        public string JudgeBy { get; set; }
        public DateTime JudgeOn { get; set; }
    }
}