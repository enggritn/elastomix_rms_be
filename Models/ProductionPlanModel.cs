using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WMS_BE.Models
{
    public class ProductionPlanHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string OrderNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string RecipeNumber { get; set; }
        public string BatchQty { get; set; }
        public string TotalQty { get; set; }
        public string ScheduleDate { get; set; }
        public string ETA { get; set; }
        public IEnumerable<ProductionPlanDetailDTO> Details { get; set; }
        public IEnumerable<ProductionPlanOrderDTO> OrderDetails { get; set; }
        public IEnumerable<FormulaDetailDTO> FormulaDetails { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string LineNumber { get; set; }
        public string FinishedBy { get; set; }
        public string FinishedOn { get; set; }
        public string Remarks { get; set; }
        public string StartTime { get; set; }
        public string FinishTime { get; set; }
        public string BreakTime { get; set; }
        public string BreakMinute { get; set; }

    }

    public class ProductionPlanDetailDTO
    {
        public string ID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string RemainderQty { get; set; }
        public string TotalQty { get; set; }
    }

    public class OrderNumberDetailDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string UoM { get; set; }
        public string RequestedQty { get; set; }
        public string FullBag { get; set; }
        public string FullBagQty { get; set; }
        public string RemainderQty { get; set; }
        public string ProductionQty { get; set; }
        public string TotalQty { get; set; }
        public string TotalFullBag { get; set; }
        public string TotalRemainderQty { get; set; }
        public string AvailableQty { get; set; }
        public string LackingQty { get; set; }
    }

    public class ProductionPlanHeaderVM
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public int LineNumber { get; set; }
        public string ItemId { get; set; }
        public int Qty { get; set; }
        public string ScheduleDate { get; set; }
        public string ETA { get; set; }
        public string TransactionStatus { get; set; }
        public List<string> OrderIds { get; set; }
        public string Remarks { get; set; }
    }

    public class ScheduleBreakVM
    {
        public string ID { get; set; }
        public int LineNumber { get; set; }
        public string ScheduleDate { get; set; }
        public int BreakMinute { get; set; }
    }

    public class ScheduleBreakDTO
    {
        public string ID { get; set; }
        public string LineNumber { get; set; }
        public string ScheduleDate { get; set; }
        public string BreakMinute { get; set; }
    }

    public class RMCalculationVM
    {
        public string FormulaID { get; set; }
        public decimal Qty { get; set; }
    }

    public class ProductDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string ProdType { get; set; }
    }

    public class ProductionPlanOrderDTO
    {
        public string ID { get; set; }
        public string OrderNumber { get; set; }
        public string OrderQty { get; set; }
        public string BatchQty { get; set; }
        public string TotalQty { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class ProductionPlanScheduleDTO
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public bool allDay { get; set; }
        public string backgroundColor { get; set; }
        public string textColor { get; set; }
    }
}
