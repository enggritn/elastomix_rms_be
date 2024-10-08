using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class ReqBarcode
    {
        public string ID { get; set; }
    }
    public class TestFMG
    {
        public string nama { get; set; }
        public string usia { get; set; }
    }
    public class ReceivingSFGVM
    {
        public string ID { get; set; }
        public string PurchaseRequestID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int OKBag { get; set; }
        public int SisaQty { get; set; }
        public string Barcode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public DateTime ProductionDate { get; set; }
        public decimal QtyPerBag { get; set; }
        public decimal BagQty { get; set; }
        public decimal Qty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal ActualBagQty { get; set; }
        public decimal OKQty { get; set; }
        public decimal OKBagQty { get; set; }
        public decimal NGQty { get; set; }
        public decimal NGBagQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal AvailableReceive { get; set; }
        public decimal xAvailableReceive { get; set; }
        public decimal AvailableBagQty { get; set; }
        public string UoM { get; set; }
        public DateTime ATA { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ReceivedBy { get; set; }
        public DateTime ReceivedOn { get; set; }
        public string NGBinRackID { get; set; }
        public string NGBinRackCode { get; set; }
        public string NGBinRackName { get; set; }
        public string TransactionStatus { get; set; }
        public decimal JudgementQty { get; set; }
        public decimal JudgementBagQty { get; set; }
        public string JudgementMethod { get; set; }
        public string JudgeBy { get; set; }
        public DateTime JudgeOn { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public decimal PutawayQty { get; set; }
        public decimal PutawayBagQty { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public DateTime PutOn { get; set; }
        public decimal QtyTotal { get; set; }
    }

    public class ReceivingSFGList
    {
        public string StockCode { get; set; }
        public string ProductCode { get; set; }
        public string LotNo { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
    }
    public class ReceivingSFGDTO
    {
        public string ID { get; set; }
        public string MaterialRequestID { get; set; }
        public string MaterialRequestCode { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ProductionDate { get; set; }
        public string ExpDate { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string Qty { get; set; }
        public string ActualQty { get; set; }
        public string ActualQtyPerBag { get; set; }
        public string ActualBagQty { get; set; }
        public string OKQty { get; set; }
        public string OKQtyPerBag { get; set; }
        public string OKBagQty { get; set; }
        public string RemainderQty { get; set; }
        public string NGQty { get; set; }
        public string NGQtyPerBag { get; set; }
        public string NGBagQty { get; set; }
        public string AvailableQty { get; set; }
        public string AvailableBagQty { get; set; }
        public string UoM { get; set; }
        public string ATA { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ReceivedBy { get; set; }
        public string ReceivedOn { get; set; }
        public string NGBinRackID { get; set; }
        public string NGBinRackCode { get; set; }
        public string NGBinRackName { get; set; }
        public string TransactionStatus { get; set; }
        public string JudgementQty { get; set; }
        public string JudgementBagQty { get; set; }
        public string JudgementMethod { get; set; }
        public string JudgeBy { get; set; }
        public string JudgeOn { get; set; }
        public string WarehouseID { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string DisposeQty { get; set; }
        public string DisposeBagQty { get; set; }
        public string DisposeMethod { get; set; }
        public string DisposedBy { get; set; }
        public string DisposedOn { get; set; }
    }
    public class vReceivingSFGDTO
    {
        public string ID { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string MaterialRequestCode { get; set; }
        public string PurchaseRequestDetailID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Barcode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ProductionDate { get; set; }
        public string ExpDate { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string Qty { get; set; }
        public string ActualQty { get; set; }
        public string ActualQtyPerBag { get; set; }
        public string ActualBagQty { get; set; }
        public string OKQty { get; set; }
        public string OKQtyPerBag { get; set; }
        public string OKBagQty { get; set; }
        public string RemainderQty { get; set; }
        public string NGQty { get; set; }
        public string NGQtyPerBag { get; set; }
        public string NGBagQty { get; set; }
        public string AvailableQty { get; set; }
        public string AvailableBagQty { get; set; }
        public string UoM { get; set; }
        public string ATA { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ReceivedBy { get; set; }
        public string ReceivedOn { get; set; }
        public string NGBinRackID { get; set; }
        public string NGBinRackCode { get; set; }
        public string NGBinRackName { get; set; }
        public string TransactionStatus { get; set; }
        public string JudgementQty { get; set; }
        public string TotalOrder { get; set; }
        public string AvailableReceive { get; set; }
        public string TotalReceive { get; set; }
        public string JudgementBagQty { get; set; }
        public string JudgementMethod { get; set; }
        public string JudgeBy { get; set; }
        public string JudgeOn { get; set; }
        public string WarehouseID { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string DisposeQty { get; set; }
        public string DisposeBagQty { get; set; }
        public string DisposeMethod { get; set; }
        public string DisposedBy { get; set; }
        public string DisposedOn { get; set; }
        public string DefaultLotNo { get; set; }


        public bool ReceiveAction { get; set; }
        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }
        public bool EditReceiveAction { get; set; }


    }
    public class vListBarcodeSFGDTO
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string QtyPerBag { get; set; }
        public string UoM { get; set; }
    }
    public class ReceivingDetailSFGDTO
    {
        public string ID { get; set; }
        public string Barcode { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PutawayTotalQty { get; set; }
        public string PutawayTotalBagQty { get; set; }
        public string PutawayAvailableQty { get; set; }
        public string PutawayAvailableBagQty { get; set; }
    }

    public class JudgementSFGVM
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public string LotNo { get; set; }
        public string HeaderID { get; set; }
        public decimal Qty { get; set; }
        public string JudgeBy { get; set; }
        public DateTime JudgeOn { get; set; }
        public string JudgementMethod { get; set; }
    }

    public partial class ReceivingSFGDetailDTO
    {
        public string ID { get; set; }
        public string ReceivingID { get; set; }
        public string Barcode { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNo { get; set; }
        public decimal QtyActual { get; set; }
        public decimal QtyBag { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public decimal QtyPerBag { get; set; }
        public int LastSeries { get; set; }

    }

    public class PutawaySFGVM
    {
        public string StockCode { get; set; }
        public string ID { get; set; }
        public int BagQty { get; set; }
        public string AreaList { get; set; }
        public string BinRackID { get; set; }
        public string ProductCode { get; set; }
        public string LotNo { get; set; }
        public decimal PutAwayQty { get; set; }
        public decimal TotalAvailableQty { get; set; }
    }

    public class PutawayMobileSFGVM
    {
        public string Barcode { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
}