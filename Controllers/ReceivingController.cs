using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS_BE.Models;
using WMS_BE.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;
using System.Data.Entity;

namespace WMS_BE.Controllers
{
    public class ReceivingController : Controller
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //public ActionResult Export(string date, string warehouseId)
        //{
        //    DateTime filterDate = Convert.ToDateTime(date);
        //    List<vReceiving> receivingList = db.vReceivings.Where(s => DbFunctions.TruncateTime(s.ETA) == DbFunctions.TruncateTime(filterDate) && s.WarehouseID.Equals(warehouseId)).ToList();

        //    string dateformat = filterDate.ToString("ddMMyy");
        //    string fileName = "Receiving_" + dateformat + ".xlsx";

        //    ExcelPackage excel = new ExcelPackage();
        //    var workSheet = excel.Workbook.Worksheets.Add("Sheet1");

        //    //Default settings all cells
        //    workSheet.Cells["A1:R500"].Style.Font.Name = "Calibri";
        //    workSheet.Cells["A1:R500"].Style.Font.Size = 11;
        //    workSheet.Cells["A1:R500"].Style.ShrinkToFit = true;

        //    workSheet.Row(5).Height = 26.25;
        //    workSheet.Column(1).Width = 5.43;
        //    workSheet.Column(2).Width = 35.14;
        //    workSheet.Column(3).Width = 10.29;
        //    workSheet.Column(4).Width = 10;
        //    workSheet.Column(5).Width = 28.71;
        //    workSheet.Column(6).Width = 7.86;
        //    workSheet.Column(7).Width = 7;
        //    workSheet.Column(8).Width = 7;
        //    workSheet.Column(9).Width = 7.86;
        //    workSheet.Column(10).Width = 7;
        //    workSheet.Column(11).Width = 7;
        //    workSheet.Column(12).Width = 7;
        //    workSheet.Column(13).Width = 8.29;
        //    workSheet.Column(14).Width = 8.29;
        //    workSheet.Column(15).Width = 8.29;
        //    workSheet.Column(16).Width = 5.71;
        //    workSheet.Column(17).Width = 8.14;
        //    workSheet.Column(18).Width = 17.43;

        //    workSheet.Cells["A5:R5"].Merge = true;
        //    workSheet.Cells[5, 1].Value = "Raw Material Daily Receiving Check";
        //    workSheet.Cells[5, 1].Style.Font.Size = 20;
        //    workSheet.Cells[5, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Cells[6, 1].Value = "Date :";
        //    workSheet.Cells[6, 2].Value = filterDate.ToString("EEE, M d, yyyy");

        //    // Header
        //    workSheet.Row(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(7).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        //    workSheet.Row(7).Style.Font.Bold = true;
        //    workSheet.Row(7).Style.WrapText = true;
        //    workSheet.Row(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(8).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        //    workSheet.Row(8).Style.WrapText = true;

        //    workSheet.Cells["A7:A8"].Merge = true;
        //    workSheet.Cells[7, 1].Value = "No";
        //    workSheet.Cells["B7:B8"].Merge = true;
        //    workSheet.Cells[7, 2].Value = "Supplier";
        //    workSheet.Cells["C7:C8"].Merge = true;
        //    workSheet.Cells[7, 3].Value = "Ref No.";
        //    workSheet.Cells["D7:D8"].Merge = true;
        //    workSheet.Cells[7, 4].Value = "Material Code";
        //    workSheet.Cells["E7:E8"].Merge = true;
        //    workSheet.Cells[7, 5].Value = "Material Name";
        //    workSheet.Cells["F7:H7"].Merge = true;
        //    workSheet.Cells[7, 6].Value = "PO Qty";
        //    workSheet.Cells[8, 6].Value = "Qty/bag";
        //    workSheet.Cells[8, 7].Value = "Qty bag";
        //    workSheet.Cells[8, 8].Value = "Total";
        //    workSheet.Cells["I7:K7"].Merge = true;
        //    workSheet.Cells[7, 9].Value = "Actual Qty";
        //    workSheet.Cells[8, 10].Value = "Qty/bag";
        //    workSheet.Cells[8, 11].Value = "Qty bag";
        //    workSheet.Cells[8, 12].Value = "Total";
        //    workSheet.Cells["L7:L8"].Merge = true;
        //    workSheet.Cells[7, 12].Value = "+/-";
        //    workSheet.Cells["M7:M8"].Merge = true;
        //    workSheet.Cells[7, 13].Value = "DO. No";
        //    workSheet.Cells["N7:N8"].Merge = true;
        //    workSheet.Cells[7, 14].Value = "Lot. No";
        //    workSheet.Cells["O7:O8"].Merge = true;
        //    workSheet.Cells[7, 15].Value = "Packing";
        //    workSheet.Cells["P7:P8"].Merge = true;
        //    workSheet.Cells[7, 16].Value = "COA";
        //    workSheet.Cells["Q7:Q8"].Merge = true;
        //    workSheet.Cells[7, 17].Value = "Rack No";
        //    workSheet.Cells["R7:R8"].Merge = true;
        //    workSheet.Cells[7, 18].Value = "Remarks";

        //    workSheet.Cells["A7:R8"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        //    workSheet.Cells["A7:R8"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 192, 0));

        //    int recordIndex = 9;
        //    int runningNumber = 1;

        //    foreach (vReceiving r in receivingList)
        //    {
        //        workSheet.Cells[recordIndex, 1].Value = runningNumber;
        //        workSheet.Cells[recordIndex, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //        workSheet.Cells[recordIndex, 2].Value = r.SourceName;
        //        workSheet.Cells[recordIndex, 3].Value = r.RefNumber;
        //        workSheet.Cells[recordIndex, 4].Value = r.MaterialCode;
        //        workSheet.Cells[recordIndex, 5].Value = r.MaterialName;
        //        workSheet.Cells[recordIndex, 6].Value = Helper.FormatThousand(r.QtyPerBag);
        //        workSheet.Cells[recordIndex, 7].Value = Helper.FormatThousand(r.Qty / r.QtyPerBag);
        //        workSheet.Cells[recordIndex, 8].Value = Helper.FormatThousand(r.Qty);
        //        workSheet.Cells[recordIndex, 9].Value = Helper.FormatThousand(r.QtyPerBag);
        //        workSheet.Cells[recordIndex, 10].Value = Helper.FormatThousand((r.ActualQty.HasValue ? r.ActualQty.Value : 0) / r.QtyPerBag);
        //        workSheet.Cells[recordIndex, 11].Value = Helper.FormatThousand(r.ActualQty.HasValue ? r.ActualQty.Value : 0);
        //        workSheet.Cells[recordIndex, 12].Value = Helper.FormatThousand(Math.Abs(r.Qty - (r.ActualQty.HasValue ? r.ActualQty.Value : 0)));
        //        workSheet.Cells[recordIndex, 13].Value = r.DONo;
        //        workSheet.Cells[recordIndex, 14].Value = r.LotNo;
        //        workSheet.Cells[recordIndex, 15].Value = "OK/NG";
        //        workSheet.Cells[recordIndex, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        //        if (r.COA.HasValue ? r.COA.Value : false) {
        //            workSheet.Cells[recordIndex, 16].Value = ((char)0x221A).ToString();
        //            workSheet.Cells[recordIndex, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //        }

        //        workSheet.Cells[recordIndex, 17].Value = r.BinRackName;

        //        runningNumber++;
        //        recordIndex++;
        //    }
            

        //    // Border
        //    workSheet.Cells[7, 1, recordIndex - 1, 18].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[7, 1, recordIndex - 1, 18].Style.Border.Top.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[7, 1, recordIndex - 1, 18].Style.Border.Left.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[7, 1, recordIndex - 1, 18].Style.Border.Right.Style = ExcelBorderStyle.Thin;

        //    var memoryStream = new MemoryStream();

        //    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //    excel.SaveAs(memoryStream);

        //    memoryStream.Position = 0;

        //    return File(memoryStream, contentType, fileName);

        //}
    }
}