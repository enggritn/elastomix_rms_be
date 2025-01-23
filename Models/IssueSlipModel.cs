using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class IssueSlipHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ProductionDate { get; set; }
        public string TotalRequestedQty { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class IssueSlipOrderDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string Number { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string VendorName { get; set; }
        public string RequestedQty { get; set; }
        public string PickedQty { get; set; }
        public string ReturnedQty { get; set; }
        public string OutstandingQty { get; set; }
        public string PickingBagQty { get; set; }
        public string DiffQty { get; set; }
        public string QtyPerBag { get; set; }

        public string AvailableReturnQty { get; set; }

        public bool PickingAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class IssueSlipOrderDTO1
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string Number { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string VendorName { get; set; }
        public string RequestedQty { get; set; }
        public string PickedQty { get; set; }
        public string ReturnedQty { get; set; }
        public string OutstandingQty { get; set; }
        public string PickingBagQty { get; set; }
        public string DiffQty { get; set; }
        public string QtyPerBag { get; set; }

        public string AvailableReturnQty { get; set; }

        public bool PickingAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class FifoStockDTO
    {
        public string ID { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string ReceivedAt { get; set; }
        public bool IsExpired { get; set; }
        public bool QCInspected { get; set; }
        public decimal Quantity { get; set; }


        public bool QCAction { get; set; }

        public string Barcode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class IssueSlipPickingVM
    {
        public string OrderID { get; set; }
        public string StockID { get; set; }
        public int BagQty { get; set; }
    }

    public class IssueSlipPickingMobileVM
    {
        public string OrderID { get; set; }
        public string Barcode1 { get; set; }
        public string Barcode2 { get; set; }
        public string MaterialType { get; set; }
        public int BagQty { get; set; }
    }

    public class IssueSlipPickingDTO
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string RowNum { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string UoM { get; set; }
        public string PickingMethod { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }


        public string DiffQty { get; set; }
        public string ReturnedTotalQty { get; set; }
        public string AvailableReturnQty { get; set; }
    }

    public class IssueSlipReturnVM
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public decimal Qty { get; set; }
    }

    public class IssueSlipReturnVM2
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public decimal Qty { get; set; }
        public string LotNo { get; set; }
    }
    public class IssueSlipReturnDTO
    {
        public string RowNum { get; set; }
        public string HeaderID { get; set; }
        public string OrderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string UoM { get; set; }
        public string ReturnMethod { get; set; }
        public string ReturnedBy { get; set; }
        public string ReturnedOn { get; set; }
        public string TotalPutawayQty { get; set; }
    }

    public class InspectionReturnVM
    {
        public string IssueSlipReturnId { get; set; }
        public int OkQty { get; set; }
    }

    public class JudgementReturnVM
    {
        public string IssueSlipReturnId { get; set; }
        public int OkQty { get; set; }
    }

    public class PutawayReturnVM
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public int BagQty { get; set; }
        public string BinRackID { get; set; }
    }

    public class IssueSlipPutawayDTO
    {
        public string OrderId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string PutawayMethod { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
    }

    public class IssueSlipDTOReport
    {
        public string ID { get; set; }
        public string ID_Order { get; set; }
        public string ID_Header { get; set; }
        public string Header_Code { get; set; }
        public string Header_Name { get; set; }
        public string Header_ProductionDate { get; set; }
        public string RM_Code { get; set; }
        public string RM_Name { get; set; }
        public string RM_VendorName { get; set; }
        public string Wt_Request { get; set; }
        public string SupplyQty { get; set; }
        public string FromBinRackCode { get; set; }
        public string ExpDate { get; set; }
        public string PickedBy { get; set; }
        public string ReturnQty { get; set; }
        public string ToBinRackCode { get; set; }
        public string PutBy { get; set; }
    }

    public class DataInOutDTOReport
    {
        public string ItemCode { get; set; }
        public string Date { get; set; }
        public string UserHanheld { get; set; }
        public string Type { get; set; }
        public string ReceiveQty { get; set; }
        public string IssueSlipQty { get; set; }
        public string BalanceQty { get; set; }
    }

    public class ListTransactionDTOReport
    {
        public string Id { get; set; }
        public string RMCode { get; set; }
        public string RMName { get; set; }
        public string WHName { get; set; }
        public string InOut { get; set; }
        public string TransactionDate { get; set; }
        public string InQty { get; set; }
        public string OutQty { get; set; }
        public string InventoryQty { get; set; }
        public string InOutType { get; set; }
        public string CreateBy { get; set; }
        public String CreateOn { get; set; }
    }
}