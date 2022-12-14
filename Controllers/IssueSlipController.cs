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

namespace WMS_BE.Controllers
{
    public class IssueSlipController : Controller
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //public ActionResult Export(string id)
        //{

        //    IssueSlipHeader header = db.IssueSlipHeaders.Where(s => s.ID.Equals(id)).FirstOrDefault();

        //    string date = DateTime.Now.ToString("ddMMyy");
        //    string fileName = "IssueSlip_" + header.Name.Replace(' ', '_') + "_" + date + ".xlsx";

        //    ExcelPackage excel = new ExcelPackage();
        //    var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
        //    decimal totalQty = 0;

        //    //Default settings all cells
        //    workSheet.Cells["A1:Q500"].Style.Font.Name = "Times New Roman";
        //    workSheet.Cells["A1:Q500"].Style.Font.Size = 11;
        //    workSheet.Cells["A1:Q500"].Style.ShrinkToFit = true;
        //    workSheet.Row(9).Height = 0;
        //    workSheet.Column(1).Width = 3.63;
        //    workSheet.Column(2).Width = 8.63;
        //    workSheet.Column(3).Width = 30.75;
        //    workSheet.Column(4).Width = 24.38;
        //    workSheet.Column(5).Width = 0;
        //    workSheet.Column(6).Width = 12.13;
        //    workSheet.Column(7).Width = 9.38;
        //    workSheet.Column(8).Width = 9.38;
        //    workSheet.Column(9).Width = 8.25;
        //    workSheet.Column(10).Width = 8.25;
        //    workSheet.Column(11).Width = 8.25;
        //    workSheet.Column(12).Width = 8.25;
        //    workSheet.Column(13).Width = 8.88;
        //    workSheet.Column(14).Width = 7.38;
        //    workSheet.Column(15).Width = 8.25;
        //    workSheet.Column(16).Width = 9.25;
        //    workSheet.Column(17).Width = 2;
        //    workSheet.Column(18).Width = 2;
        //    workSheet.Column(19).Width = 2;
        //    workSheet.Column(20).Width = 2;
        //    workSheet.Column(21).Width = 2;
        //    workSheet.Column(22).Width = 2;
        //    workSheet.Column(23).Width = 2;
        //    workSheet.Column(24).Width = 2;
        //    workSheet.Column(25).Width = 2;

        //    //Title
        //    workSheet.Cells["A2:P2"].Merge = true;
        //    workSheet.Cells[2, 1].Value = header.Name;
        //    workSheet.Cells[2, 1].Style.Font.Size = 20;
        //    workSheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(2).Height = 27.00;

        //    //Title Desc
        //    workSheet.Cells[5, 4].Value = "Total Weight:";
        //    workSheet.Cells[5, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        //    //workSheet.Cells[5, 6].Value = header.TotalRequestedQty;
        //    workSheet.Cells[5, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

        //    workSheet.Cells["M4:N4"].Merge = true;
        //    workSheet.Cells[4, 13].Value = "Created By:";
        //    workSheet.Cells[4, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        //    workSheet.Cells["O4:P4"].Merge = true;
        //    workSheet.Cells[4, 15].Value = header.ExcelCreatedBy;
        //    workSheet.Cells[4, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

        //    // Header
        //    workSheet.Cells["A6:A8"].Merge = true;
        //    workSheet.Cells[6, 1].Value = "No.";
        //    workSheet.Cells["B6:B8"].Merge = true;
        //    workSheet.Cells[6, 2].Value = "R/M Code";
        //    workSheet.Cells["C6:C8"].Merge = true;
        //    workSheet.Cells[6, 3].Value = "R/M Name";
        //    workSheet.Cells["D6:D8"].Merge = true;
        //    workSheet.Cells[6, 4].Value = "Raw Material Vendor Name";
        //    workSheet.Cells["E6:E8"].Merge = true;
        //    workSheet.Cells[6, 5].Value = "";
        //    workSheet.Cells["F6:F8"].Merge = true;
        //    workSheet.Cells[6, 6].Value = "Wt_Requested";
        //    workSheet.Cells["G6:L6"].Merge = true;
        //    workSheet.Cells[6, 7].Value = "Raw Material Supply";
        //    workSheet.Cells["G7:G8"].Merge = true;
        //    workSheet.Cells[7, 7].Value = "Supply Qty";
        //    workSheet.Cells["H7:H8"].Merge = true;
        //    workSheet.Cells[7, 8].Value = "From Rack No";
        //    workSheet.Cells["I7:L7"].Merge = true;
        //    workSheet.Cells[7, 9].Value = "Condition Check";
        //    workSheet.Cells[8, 9].Value = "QR Label";
        //    workSheet.Cells[8, 10].Value = "Package";
        //    workSheet.Cells[8, 11].Value = "Exp Date";
        //    workSheet.Cells[8, 12].Value = "Approve Stamp";
        //    workSheet.Cells["M6:P6"].Merge = true;
        //    workSheet.Cells[6, 13].Value = "Return Raw Material";
        //    workSheet.Cells["M7:M8"].Merge = true;
        //    workSheet.Cells[7, 13].Value = "Actual Return Qty";
        //    workSheet.Cells["N7:P7"].Merge = true;
        //    workSheet.Cells[7, 14].Value = "Condition Check";
        //    workSheet.Cells[8, 14].Value = "Package";
        //    workSheet.Cells[8, 15].Value = "QR Label";
        //    workSheet.Cells[8, 16].Value = "Go To Rack No";

        //    workSheet.Row(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(6).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        //    workSheet.Row(6).Style.WrapText = true;
        //    workSheet.Row(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(7).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        //    workSheet.Row(7).Style.WrapText = true;
        //    workSheet.Row(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //    workSheet.Row(8).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        //    workSheet.Row(8).Style.WrapText = true;
        //    workSheet.Row(8).Height = 35.25;

        //    // Details
        //    int detailRecordIndex = 10;
        //    int recordIndex = 10;
        //    int count = 1;
        //    foreach (IssueSlipDetail detail in header.IssueSlipDetails)
        //    {
        //        workSheet.Cells[recordIndex, 1].Value = count;
        //        workSheet.Cells[recordIndex, 2].Value = detail.MaterialCode;
        //        workSheet.Cells[recordIndex, 3].Value = detail.MaterialName;
        //        workSheet.Cells[recordIndex, 4].Value = detail.VendorName;
        //        workSheet.Cells[recordIndex, 5].Value = detail.RequestedQty;
        //        workSheet.Cells[recordIndex, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        //        workSheet.Cells[recordIndex, 6].Formula = "=IF(" + workSheet.Cells[recordIndex, 5].Address + "=\"\",\"\",IF(" + workSheet.Cells[recordIndex, 5].Address + "=0,0,IF(RIGHT(TEXT(" + workSheet.Cells[recordIndex, 5].Address + ",\"0.###\"),1)=\".\",LEFT(TEXT(" + workSheet.Cells[recordIndex, 5].Address + ",\"0.###\"),LEN(TEXT(" + workSheet.Cells[recordIndex, 5].Address + ",\"0.###\")-1)),TEXT(" + workSheet.Cells[recordIndex, 5].Address + ",\"0.###\"))))";
        //        workSheet.Cells[recordIndex, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

        //        totalQty += detail.RequestedQty;


        //        foreach (IssueSlipList list in detail.IssueSlipLists)
        //        {

        //            workSheet.Cells[recordIndex, 7].Value = Helper.FormatThousand(list.UsageSupplyQty);
        //            workSheet.Cells[recordIndex, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        //            workSheet.Cells[recordIndex, 8].Value = list.UsageBinRackName;

        //            if (list.UsageQRLabel.HasValue ? list.UsageQRLabel.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 9].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            if (list.UsagePackage.HasValue ? list.UsagePackage.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 10].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            if (list.UsageExpDate.HasValue ? list.UsageExpDate.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 11].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            if (list.UsageApproveStamp.HasValue ? list.UsageApproveStamp.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 12].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            workSheet.Cells[recordIndex, 13].Value = list.ReturnActualQty;
        //            workSheet.Cells[recordIndex, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

        //            if (list.ReturnPackage.HasValue ? list.ReturnPackage.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 14].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            if (list.ReturnQRLabel.HasValue ? list.ReturnQRLabel.Value : false)
        //            {
        //                workSheet.Cells[recordIndex, 15].Value = ((char)0x221A).ToString();
        //                workSheet.Cells[recordIndex, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            }

        //            workSheet.Cells[recordIndex, 16].Value = list.ReturnBinRackName;

        //            recordIndex++;
        //        }

        //        if (detailRecordIndex == recordIndex)
        //        {
        //            recordIndex++;
        //        }

        //        detailRecordIndex = recordIndex;

        //        count++;
        //    }

        //    workSheet.Cells[5, 6].Value = totalQty;

        //    string range = "A6:P" + (recordIndex - 1).ToString();

        //    workSheet.Cells[range].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[range].Style.Border.Top.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[range].Style.Border.Left.Style = ExcelBorderStyle.Thin;
        //    workSheet.Cells[range].Style.Border.Right.Style = ExcelBorderStyle.Thin;

        //    var memoryStream = new MemoryStream();

        //    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //    excel.SaveAs(memoryStream);

        //    memoryStream.Position = 0;

        //    return File(memoryStream, contentType, fileName);

        //}
    }
}