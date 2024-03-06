using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    //public class ReceivingHeaderVM
    //{
    //    public string ID { get; set; }
    //    public string Code { get; set; }
    //    public string PurchaseRequestID { get; set; }
    //    public string RefNumber { get; set; }
    //    public string SourceType { get; set; }
    //    public string SourceID { get; set; }
    //    public string SourceCode { get; set; }
    //    public string SourceName { get; set; }
    //    public string SourceAddress { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string WarehouseType { get; set; }
    //    public IEnumerable<ReceivingDetailVM> Details { get; set; }
    //    public string TransactionStatus { get; set; }
    //    public string CreatedBy { get; set; }
    //    public DateTime CreatedOn { get; set; }
    //    public string ModifiedBy { get; set; }
    //    public DateTime ModifiedOn { get; set; }
    //}

    //public class ReceivingDetailVM
    //{
    //    public string ID { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public DateTime InDate { get; set; }
    //    public DateTime ExpDate { get; set; }
    //    public decimal Qty { get; set; }
    //    public decimal ActualQty { get; set; }
    //    public DateTime ETA { get; set; }
    //    public DateTime ATA { get; set; }
    //}

    //public class ReceivingHeaderDTO
    //{
    //    public string ID { get; set; }
    //    public string Code { get; set; }
    //    public string PurchaseRequestID { get; set; }
    //    public string RefNumber { get; set; }
    //    public string SourceType { get; set; }
    //    public string SourceID { get; set; }
    //    public string SourceCode { get; set; }
    //    public string SourceName { get; set; }
    //    public string SourceAddress { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string WarehouseType { get; set; }
    //    public List<ReceivingDetailDTO> Details { get; set; }
    //    public string TransactionStatus { get; set; }
    //    public string CreatedBy { get; set; }
    //    public string CreatedOn { get; set; }
    //    public string ModifiedBy { get; set; }
    //    public string ModifiedOn { get; set; }
    //}

    //public class ReceivingDetailDTO
    //{
    //    public string ID { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public string InDate { get; set; }
    //    public string ExpDate { get; set; }
    //    public string InspectionQty { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string InspectedBy { get; set; }
    //    public string InspectedOn { get; set; }
    //    public string NGQty { get; set; }
    //    public string JudgementQty { get; set; }
    //    public string JudgementMethod { get; set; }
    //    public string JudgementBy { get; set; }
    //    public string JudgementOn { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public string PutBy { get; set; }
    //    public string PutOn { get; set; }
    //    public string Qty { get; set; }
    //    public string ReceiveQty { get; set; }
    //    public string ActualQty { get; set; }
    //    public string ETA { get; set; }
    //    public string ATA { get; set; }
    //}

    //public class InspectionVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public decimal Qty { get; set; }
    //    public string InspectBy { get; set; }
    //    public DateTime InspectOn { get; set; }
    //    public string InspectionMethod { get; set; }
    //}

    //public class InspectionDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public string Qty { get; set; }
    //    public string InspectBy { get; set; }
    //    public string InspectOn { get; set; }
    //    public string InspectionMethod { get; set; }
    //}

    //public class JudgementVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public decimal Qty { get; set; }
    //    public string JudgeBy { get; set; }
    //    public DateTime JudgeOn { get; set; }
    //    public string JudgementMethod { get; set; }
    //}

    //public class JudgementDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public string Qty { get; set; }
    //    public string JudgeBy { get; set; }
    //    public string JudgeOn { get; set; }
    //    public string JudgementMethod { get; set; }
    //}

    //public class PutawayVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutBy { get; set; }
    //    public DateTime PutOn { get; set; }
    //    public string PutawayMethod { get; set; }
    //}

    //public class PutawayDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutBy { get; set; }
    //    public string PutOn { get; set; }
    //    public string PutawayMethod { get; set; }
    //}

    //public class ReceivingVM 
    //{
    //    public string ID { get; set; }
    //    public string PurchaseRequestID { get; set; }
    //    public string RefNumber { get; set; }
    //    public string SourceType { get; set; }
    //    public string SourceID { get; set; }
    //    public string SourceCode { get; set; }
    //    public string SourceName { get; set; }
    //    public string SourceAddress { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseType { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string DONo { get; set; }
    //    public string LotNo { get; set; }
    //    public bool COA { get; set; }
    //    public DateTime InDate { get; set; }
    //    public DateTime ExpDate { get; set; }
    //    public int QtyPerBag { get; set; }
    //    public int BagQty { get; set; }
    //    public int ActualBagQty { get; set; }
    //    public decimal Qty { get; set; }
    //    public decimal ActualQty { get; set; }
    //    public decimal OKQty { get; set; }
    //    public decimal NGQty { get; set; }
    //    public decimal ReturnQty { get; set; }
    //    public string UoM { get; set; }
    //    public decimal DeviationQty { get; set; }
    //    public DateTime ETA { get; set; }
    //    public DateTime ATA { get; set; }
    //    public string ReceivedBy { get; set; }
    //    public DateTime ReceivedOn { get; set; }
    //    public decimal InspectionQty { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string InspectedBy { get; set; }
    //    public DateTime InspectedOn { get; set; }
    //    public string NGBinRackID { get; set; }
    //    public string NGBinRackCode { get; set; }
    //    public string NGBinRackName { get; set; }
    //    public string TransactionStatus { get; set; }
    //    public decimal JudgementQty { get; set; }
    //    public string JudgementMethod { get; set; }
    //    public string JudgeBy { get; set; }
    //    public DateTime JudgeOn { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public decimal PutawayQty { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public string PutBy { get; set; }
    //    public DateTime PutOn { get; set; }
    //}

    public class ReceivingDTO
    {
        public string ID { get; set; }
        public string DocumentNo { get; set; }
        public string RefNumber { get; set; }
        public string SourceType { get; set; }
        public string SourceCode { get; set; }
        public string SourceName { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseType { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Qty { get; set; }
        public string Qty2 { get; set; }
        public string QtyPerBag { get; set; }
        public string QtyPerBag2 { get; set; }
        public string BagQty { get; set; }
        public string ReceivedQty { get; set; }
        public string ReceivedBagQty { get; set; }
        public string ReceiveBagQty { get; set; }
        public string AvailableQty { get; set; }
        public string AvailableBagQty { get; set; }
        public string UoM { get; set; }
        public string UoM2 { get; set; }
        public string ETA { get; set; }
        public string TransactionStatus { get; set; }
        public string DefaultLot { get; set; }
        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }
        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }
        public string DestinationCode { get; set; }

        public bool ReceiveAction { get; set; }
    }

    public class ReceivingDetailDTO
    {
        public string ID { get; set; }
        public string DocNo { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string RefNumber { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string DoNo { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string UoM { get; set; }
        public string ATA { get; set; }
        public string Remarks { get; set; }
        public string ReceivedBy { get; set; }
        public string ReceivedOn { get; set; }
        public string LastSeries { get; set; }
        public string FotoCOA { get; set; }
        public string LastFotoCOA { get; set; }
        public bool COA { get; set; }
        public bool CoaAction { get; set; }
        public bool InspectionAction { get; set; }
        public bool JudgementAction { get; set; }
        public bool PutawayAction { get; set; }

        public string OKQty { get; set; }
        public string OKBagQty { get; set; }
        public string NGQty { get; set; }
        public string NGBagQty { get; set; }

        public string PutawayTotalQty { get; set; }
        public string PutawayTotalBagQty { get; set; }

        public string PutawayAvailableQty { get; set; }
        public string PutawayAvailableBagQty { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public string DestinationCode { get; set; }

        public bool EditAction { get; set; }

        public string SourceType { get; set; }
        public string SourceCode { get; set; }
        public string SourceName { get; set; }
        public string DocumentNo { get; set; }
    }

    public class ReceivingDetailDTOReport
    {       
        public string DestinationName { get; set; }
        public string RefNumber { get; set; }
        public string SourceCode { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string LotNo { get; set; }
        public string PerBag { get; set; }
        public string FullBag { get; set; }
        public string Total { get; set; }
        public string Area { get; set; }
        public string RackNo { get; set; }
        public string DoNo { get; set; }
        public string ATA { get; set; }
        public string TransactionStatus { get; set; }
    }

    public class ReceivingDetailDTOReport3
    {
        public string DestinationName { get; set; }
        public string RefNumber { get; set; }
        public string SourceCode { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Schedule { get; set; }
        public string TotalQtyPo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string LotNo { get; set; }
        public string QtyPerBag { get; set; }
        public string QtyBag { get; set; }
        public string Total { get; set; }
        public string DoNo { get; set; }
        public string Ok { get; set; }
        public string NgDamage { get; set; }
        public string COA { get; set; }
        public string StatusPo { get; set; }
        public string ReceivedBy { get; set; }
        public DateTime ReceivedOn { get; set; }
        public string QtyPutaway { get; set; }
        public string Area { get; set; }
        public string RackNo { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
    }

    public class ReceivingDetailVM
    {
        public string ID { get; set; }
        public string ReceivingID { get; set; }
        public string DoNo { get; set; }
        public string LotNo { get; set; }
        public int BagQty { get; set; }
        public string BinRackID { get; set; }
    }

    public class CoaVM
    {
        public string ID { get; set; }
        public bool IsChecked { get; set; }
    }

    public class InspectionVM
    {
        public string ID { get; set; }
        public int OKBagQty { get; set; }
        public int DamageQty { get; set; }
        public int WetQty { get; set; }
        public int ContaminationQty { get; set; }
    }

    public class JudgementVM
    {
        public string ID { get; set; }
        public int OKBagQty { get; set; }
    }

    public class PutawayVM
    {
        public string ID { get; set; }
        public int BagQty { get; set; }
        public string BinRackID { get; set; }
    }



    public class ReceivingDetailBarcodeDTO
    {
        public string ID { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string Series { get; set; }
    }

    public class ReceivingPrintVM
    {
        public string ReceivingDetailID { get; set; }
        public string Type { get; set; }
        public string ID { get; set; }
        public int PrintQty { get; set; }
        public bool UseSeries { get; set; }
        public string Printer { get; set; }
    }

    public class ReceivingBarcodeDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string RawMaterialMaker { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }
        public string StartSeries { get; set; }
    }

    //public class ReceivingDTO
    //{
    //    public string ID { get; set; }
    //    public string PurchaseRequestDetailID { get; set; }
    //    public string RefNumber { get; set; }
    //    public string SourceType { get; set; }
    //    public string SourceID { get; set; }
    //    public string SourceCode { get; set; }
    //    public string SourceName { get; set; }
    //    public string SourceAddress { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseType { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string RawMaterialMaker { get; set; }
    //    public string Barcode { get; set; }
    //    public string DONo { get; set; }
    //    public string LotNo { get; set; }
    //    public bool COA { get; set; }
    //    public string InDate { get; set; }
    //    public string ExpDate { get; set; }
    //    public string QtyPerBag { get; set; }
    //    public string BagQty { get; set; }
    //    public string ActualBagQty { get; set; }
    //    public string Qty { get; set; }
    //    public string ActualQty { get; set; }
    //    public string OKQty { get; set; }
    //    public string OKBagQty { get; set; }
    //    public string NGQty { get; set; }
    //    public string NGBagQty { get; set; }
    //    public string ReturnQty { get; set; }
    //    public string PRQty { get; set; }
    //    public string PRUoM { get; set; }
    //    public string PRQtyPerBag { get; set; }
    //    public string PRBagQty { get; set; }
    //    public string AvailableQty { get; set; }
    //    public string AvailableBagQty { get; set; }
    //    public string RequestUoM { get; set; }
    //    public string UoM { get; set; }
    //    public string DeviationQty { get; set; }
    //    public string ETA { get; set; }
    //    public string ATA { get; set; }
    //    public string ReceivedBy { get; set; }
    //    public string ReceivedOn { get; set; }
    //    public string InspectionQty { get; set; }
    //    public string InspectionBagQty { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string InspectedBy { get; set; }
    //    public string InspectedOn { get; set; }
    //    public string NGBinRackID { get; set; }
    //    public string NGBinRackCode { get; set; }
    //    public string NGBinRackName { get; set; }
    //    public string TransactionStatus { get; set; }
    //    public string JudgementQty { get; set; }
    //    public string JudgementBagQty { get; set; }
    //    public string JudgementMethod { get; set; }
    //    public string JudgeBy { get; set; }
    //    public string JudgeOn { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutawayQty { get; set; }
    //    public string PutawayBagQty { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public string PutBy { get; set; }
    //    public string PutOn { get; set; }
    //}

    //public class InspectionVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public decimal Qty { get; set; }
    //    public string InspectedBy { get; set; }
    //    public DateTime InspectedOn { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string NGBinRackID { get; set; }
    //    public string NGBinRackCode { get; set; }
    //    public string NGBinRackName { get; set; }
    //}

    //public class InspectionDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string HeaderID { get; set; }
    //    public string Qty { get; set; }
    //    public string InspectedBy { get; set; }
    //    public string InspectedOn { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string NGBinRackID { get; set; }
    //    public string NGBinRackCode { get; set; }
    //    public string NGBinRackName { get; set; }
    //}

    //public class JudgementVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public string HeaderID { get; set; }
    //    public decimal Qty { get; set; }
    //    public string JudgeBy { get; set; }
    //    public DateTime JudgeOn { get; set; }
    //    public string JudgementMethod { get; set; }
    //}

    //public class JudgementDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public string HeaderID { get; set; }
    //    public string Qty { get; set; }
    //    public string JudgeBy { get; set; }
    //    public string JudgeOn { get; set; }
    //    public string JudgementMethod { get; set; }
    //}

    //public class PutawayVM
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public string HeaderID { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutBy { get; set; }
    //    public DateTime PutOn { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public decimal Qty { get; set; }
    //}

    //public class PutawayDTO
    //{
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string Barcode { get; set; }
    //    public string LotNo { get; set; }
    //    public string HeaderID { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutBy { get; set; }
    //    public string PutOn { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public string Qty { get; set; }
    //}
}