using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WMS_BE.Models
{
    public class InboundHeaderVM
    {
        public string ID { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
    }

    public class InboundHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public List<InboundOrderDTO> Details { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

        public bool SaveAction { get; set; }
        public bool CancelAction { get; set; }
        public bool ConfirmAction { get; set; }
        public bool AddOrderAction { get; set; }
        public bool RemoveOrderAction { get; set; }
    }

    public class InboundOrderVM
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public decimal Qty { get; set; }
    }

    public class InboundOrderDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string ReceiveQty { get; set; }
        public string DiffQty { get; set; }
        public string OutstandingQty { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string FullBagQty { get; set; }
        public string RemainderQty { get; set; }
        public bool ReceiveAction { get; set; }
        public string OutstandingBagQty { get; set; }
        public string OutstandingRemainderQty { get; set; }
    }

    public class InboundMaterialDTO
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string QtyPerBag { get; set; }
    }

    public class InboundReceiveDTO
    {
        public string ID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string InboundOrderID { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string ReceivedBy { get; set; }
        public string ReceivedOn { get; set; }
        public string LastSeries { get; set; }
        public bool BarcodeExist { get; set; }

        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }

        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }

        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class InboundReceiveDTOReport
    {
        public string ID { get; set; }
        public string ReceiptDate { get; set; }
        public string ReceiptNo { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string InboundOrderID { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Uom { get; set; }
        public string QtyL { get; set; }
        public string Qty { get; set; }
        public string Memo { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string ReceivedBy { get; set; }
        public string ReceivedOn { get; set; }
        public string LastSeries { get; set; }
        public bool BarcodeExist { get; set; }

        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }

        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }

        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class InboundPutawayDTO
    {
        public string ID { get; set; }
        public string InboundOrderID { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string BinRackCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public  string PutawayMethod { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string LastSeries { get; set; }
        public bool BarcodeExist { get; set; }

        public string PutawayQty { get; set; }
        public string PutawayBagQty { get; set; }

        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }

        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }
    }

    //public class InboundDetailVM
    //{
    //    public string InboundOrderID { get; set; }
    //    public string DoNo { get; set; }
    //    public string LotNo { get; set; }
    //    public decimal Qty { get; set; }
    //}

    //public class InboundCoaVM
    //{
    //    public string ID { get; set; }
    //    public bool IsChecked { get; set; }
    //}

    //public class InboundInspectionVM
    //{
    //    public string ID { get; set; }
    //    public int OKBagQty { get; set; }
    //    public int DamageQty { get; set; }
    //    public int WetQty { get; set; }
    //    public int ContaminationQty { get; set; }
    //}

    //public class InboundJudgementVM
    //{
    //    public string ID { get; set; }
    //    public int OKBagQty { get; set; }
    //}
}
