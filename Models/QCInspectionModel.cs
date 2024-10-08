using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class QCInspectionVM
    {
        public string StockID { get; set; }
        public string MaterialCode { get; set; }
        public string LotNumber { get; set; }
        public string InDate { get; set; }
        public string ExpiredDate { get; set; }
    }


    public class QCPickingVM
    {
        public string PickingID { get; set; }
    }

    public class QCPutawayVM
    {
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string PrevBinRackID { get; set; }
        public string BinRackID { get; set; }
        public int BagQty { get; set; }
    }

    public class QCJudgementVM
    {
        public string InspectionID { get; set; }
        public int ExtendQty { get; set; }
        public string ExtendRange { get; set; }
    }

    public class QCDisposeVM
    {
        public string InspectionID { get; set; }
    }

    public class QCPickingDisposeVM
    {
        public string PutawayID { get; set; }
    }


    public class QCRevertVM
    {
        public string InspectionID { get; set; }
        public string DisposeID { get; set; }
    }

    public class QCReturnVM
    {
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string PrevBinRackID { get; set; }
        public string BinRackID { get; set; }
        public int BagQty { get; set; }
        public int ReturnQty { get; set; }
        public string ReturnType { get; set; }
    }

    public class QCInspectionDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string MaterialType { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string NewExpDate { get; set; }
        public string ExpirationDay { get; set; }
        public string CreatedBy { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedOn { get; set; }
        //public string TotalJudgementQty { get; set; }
        //public string TotalDisposalQty { get; set; }

        public string InspectionStatus { get; set; }
        public string InspectedBy { get; set; }
        public string InspectedOn { get; set; }

        public bool Priority { get; set; }

        public bool JudgementAction { get; set; }
        public bool DisposeAction { get; set; }
        public bool PickingAction { get; set; }
        public bool PutawayExtendAction { get; set; }
        public bool PrintPutawayExtendAction { get; set; }
        public bool PickingDisposeAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class QCPickingDTO
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }
        public string PutawayBagQty { get; set; }
        public string OutstandingPutawayBagQty { get; set; }

        public bool PickingAction { get; set; }
        public bool PutawayAction { get; set; }
    }

    public class QCPutawayDTO
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string PutawayQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }
        public string PutawayBagQty { get; set; }
        public string OutstandingPutawayBagQty { get; set; }

        public bool PickingAction { get; set; }
        public bool PutawayAction { get; set; }
    }


    public class ListDataReturnDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BinRackCode { get; set; }
        public string QtyPerBag { get; set; }
        public int BagQty { get; set; }
        public string Quantity { get; set; }
    }
    //summary from QCPutaway
    public class QCMaterialDTO
    {
        public string StockCode { get; set; }
        public string TotalQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }

        public bool DisposalAction { get; set; }
    }

    public class QCDisposeDTO
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string DisposeQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string DisposedOn { get; set; }
        public string DisposedBy { get; set; }
    }

    public class QCJudgementDTO
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string TotalDays { get; set; }
        public string JudgedQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string JudgedBy { get; set; }
        public string JudgedOn { get; set; }

        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string NewExpDate { get; set; }

        public bool ReturnAction { get; set; }
    }

    public class QCReturnDTO
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string StockCode { get; set; }
        public string NewStockCode { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string ReturnQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string ReturnMethod { get; set; }
        public string ReturnBy { get; set; }
        public string ReturnOn { get; set; }

        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string NewExpDate { get; set; }
    }

    public class ExpiredListDTO
    {
        //public string MaterialCode { get; set; }
        //public string MaterialName { get; set; }
        //public string MaterialType { get; set; }
        //public string LotNumber { get; set; }
        //public string InDate { get; set; }
        //public string ExpiredDate { get; set; }
        //public string TotalQty { get; set; }
        //public string QtyPerBag { get; set; }
        //public string BagQty { get; set; }
        //public string BinRackCode { get; set; }
        //public string BinRackName { get; set; }
        //public string BinRackAreaCode { get; set; }
        //public string BinRackAreaName { get; set; }
        //public string WarehouseCode { get; set; }
        //public string WarehouseName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string LotNumber { get; set; }
        public string InDate { get; set; }
        public string ExpiredDate { get; set; }
        public string TotalQty { get; set; }
        public string ExpirationDay { get; set; }
    }
}