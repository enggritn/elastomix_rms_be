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
    //public class PurchaseRequestController : Controller
    //{
    //    private EIN_WMSEntities db = new EIN_WMSEntities();

    //    public ActionResult Export(string id)
    //    {
    //        PurchaseRequestHeader header = db.PurchaseRequestHeaders.Where(s => s.ID.Equals(id)).FirstOrDefault();

    //        if (header.SourceType.ToUpper().Equals("OUTSOURCE"))
    //        {
    //            return ExportOutsource(id, header);
    //        }
    //        else
    //        {
    //            return Export(id, header);
    //        }
    //    }

    //    public ActionResult Export(string id, PurchaseRequestHeader header)
    //    {
    //        string fileName = "PR_" + header.RefNumber + ".xlsx";

    //        ExcelPackage excel = new ExcelPackage();
    //        var workSheet = excel.Workbook.Worksheets.Add("Sheet1");

    //        //Default settings all cells
    //        workSheet.Cells["A1:K500"].Style.Font.Name = "Century";
    //        workSheet.Cells["A1:K500"].Style.Font.Size = 11;
    //        workSheet.Cells["A1:K500"].Style.ShrinkToFit = true;
    //        workSheet.Row(6).Height = 16.50;
    //        workSheet.Row(7).Height = 16.50;
    //        workSheet.Row(8).Height = 16.50;
    //        workSheet.Row(9).Height = 16.50;
    //        workSheet.Row(10).Height = 16.50;
    //        workSheet.Row(11).Height = 16.50;
    //        workSheet.Row(12).Height = 16.50;
    //        workSheet.Row(13).Height = 16.50;
    //        workSheet.Row(14).Height = 16.50;
    //        workSheet.Row(15).Height = 16.50;
    //        workSheet.Row(16).Height = 16.50;

    //        workSheet.Column(1).Width = 0.92;
    //        workSheet.Column(2).Width = 6.29;
    //        workSheet.Column(3).Width = 12;
    //        workSheet.Column(4).Width = 42.71;
    //        workSheet.Column(5).Width = 9.29;
    //        workSheet.Column(6).Width = 3.57;
    //        workSheet.Column(7).Width = 4.29;
    //        workSheet.Column(8).Width = 11;
    //        workSheet.Column(9).Width = 6;
    //        workSheet.Column(10).Width = 12.29;
    //        workSheet.Column(11).Width = 1.57;

    //        //Logo

    //        //Title
    //        workSheet.Cells["A2:H2"].Merge = true;
    //        workSheet.Cells[2, 1].Value = "DELIVERY NOTE";

    //        workSheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[2, 1].Style.Font.Size = 24;
    //        workSheet.Cells[2, 1].Style.Font.Bold = true;
    //        workSheet.Row(2).Height = 21.75;

    //        workSheet.Cells[3, 1].Value = "Date Request";
    //        workSheet.Cells[3, 3].Value = ": " + header.CreatedOn.ToString("M d, yyyy");

    //        workSheet.Cells[4, 1].Value = "Ref Number";
    //        workSheet.Cells[4, 3].Value = header.RefNumber;

    //        //Headers
    //        workSheet.Cells[7, 1].Value = "NO";
    //        workSheet.Cells[7, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 1].Style.Font.Bold = true;
    //        workSheet.Cells[7, 2].Value = "ITEM";
    //        workSheet.Cells[7, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 2].Style.Font.Bold = true;
    //        workSheet.Cells[7, 3].Value = "DESCRIPTION";
    //        workSheet.Cells[7, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 3].Style.Font.Bold = true;
    //        workSheet.Cells[7, 4].Value = "QTY";
    //        workSheet.Cells[7, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 4].Style.Font.Bold = true;
    //        workSheet.Cells[7, 5].Value = "U/M";
    //        workSheet.Cells[7, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 5].Style.Font.Bold = true;
    //        workSheet.Cells[7, 6].Value = "UNIT PRICE";
    //        workSheet.Cells[7, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 6].Style.Font.Bold = true;
    //        workSheet.Cells[7, 7].Value = "REQUEST DATE";
    //        workSheet.Cells[7, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 7].Style.Font.Bold = true;
    //        workSheet.Cells[7, 8].Value = "REMARKS";
    //        workSheet.Cells[7, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 8].Style.Font.Bold = true;

    //        //Details
    //        int recordIndex = 8;
    //        int startRow = 8;
    //        int count = 1;

    //        foreach (PurchaseRequestDetail detail in header.PurchaseRequestDetails)
    //        {
    //            workSheet.Cells[recordIndex, 1].Value = count;
    //            workSheet.Cells[recordIndex, 2].Value = detail.MaterialCode;
    //            workSheet.Cells[recordIndex, 3].Value = detail.MaterialName;
    //            workSheet.Cells[recordIndex, 4].Value = detail.Qty;
    //            workSheet.Cells[recordIndex, 5].Value = detail.UoM;
    //            workSheet.Cells[recordIndex, 7].Value = detail.ETA.ToString("d/MMM/yy");

    //            recordIndex++;
    //        }

    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        //Headers
    //        workSheet.Cells[recordIndex + 1, 2, recordIndex + 3, 2].Merge = true;
    //        workSheet.Cells[recordIndex, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 2].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 2].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 2].Value = "ISSUED";
    //        workSheet.Cells[recordIndex + 1, 3, recordIndex + 3, 3].Merge = true;
    //        workSheet.Cells[recordIndex, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 3].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 3].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 3].Value = "SECTION HEAD";
    //        workSheet.Cells[recordIndex, 4, recordIndex, 5].Merge = true;
    //        workSheet.Cells[recordIndex + 1, 4, recordIndex + 3, 5].Merge = true;
    //        workSheet.Cells[recordIndex, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 4].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 4].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 4].Value = "DEPT. HEAD";
    //        workSheet.Cells[recordIndex, 6, recordIndex, 7].Merge = true;
    //        workSheet.Cells[recordIndex + 1, 6, recordIndex + 3, 7].Merge = true;
    //        workSheet.Cells[recordIndex, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 6].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 6].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 6].Value = "GENERAL MANAGER";
    //        workSheet.Cells[recordIndex, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 8].Value = "RECEIPT";

    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        startRow = recordIndex + 1;
    //        recordIndex += 3;
    //        workSheet.Cells[recordIndex, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 8].Value = "PURCHASE";

    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, startRow, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex, 1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 8, recordIndex, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        var memoryStream = new MemoryStream();

    //        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    //        excel.SaveAs(memoryStream);

    //        memoryStream.Position = 0;

    //        return File(memoryStream, contentType, fileName);
    //    }

    //    public ActionResult ExportOutsource(string id, PurchaseRequestHeader header)
    //    {
    //        string fileName = "PR_" + header.RefNumber + ".xlsx";

    //        ExcelPackage excel = new ExcelPackage();
    //        var workSheet = excel.Workbook.Worksheets.Add("Sheet1");

    //        //Default settings all cells
    //        workSheet.Cells["A1:H500"].Style.Font.Name = "Calibri";
    //        workSheet.Cells["A1:H500"].Style.Font.Size = 11;
    //        workSheet.Cells["A1:H500"].Style.ShrinkToFit = true;
    //        workSheet.Row(1).Height = 41.25;
    //        workSheet.Row(6).Height = 6.75;
    //        workSheet.Column(1).Width = 3.43;
    //        workSheet.Column(2).Width = 14.71;
    //        workSheet.Column(3).Width = 29.86;
    //        workSheet.Column(4).Width = 11;
    //        workSheet.Column(5).Width = 6.43;
    //        workSheet.Column(6).Width = 10.71;
    //        workSheet.Column(7).Width = 13.14;
    //        workSheet.Column(8).Width = 20.86;
    //        workSheet.Column(9).Width = 3.43;

    //        //Logo

    //        //Title
    //        workSheet.Cells["A2:H2"].Merge = true;
    //        if (header.SourceType.ToUpper().Equals("VENDOR") || header.SourceType.ToUpper().Equals("IMPORT"))
    //        {
    //            workSheet.Cells[2, 1].Value = "PURCHASING REQUEST";
    //            workSheet.Cells[5, 1].Value = "Vendor Name";
    //            workSheet.Cells[5, 3].Value = header.SourceName;
    //        }
    //        else if (header.SourceType.ToUpper().Equals("CUSTOMER"))
    //        {
    //            workSheet.Cells[2, 1].Value = "RM Customer Supplied";
    //            workSheet.Cells[5, 1].Value = "Customer Name";
    //            workSheet.Cells[5, 3].Value = header.SourceName;
    //        }
    //        //else if (header.SourceType.ToUpper().Equals("OUTSOURCE"))
    //        //{
    //        //    workSheet.Cells[2, 1].Value = "DELIVERY NOTE";
    //        //}
    //        workSheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[2, 1].Style.Font.Size = 24;
    //        workSheet.Cells[2, 1].Style.Font.Bold = true;
    //        workSheet.Row(2).Height = 21.75;

    //        workSheet.Cells[3, 1].Value = "Date Request";
    //        workSheet.Cells[3, 3].Value = ": " + header.CreatedOn.ToString("M d, yyyy");

    //        workSheet.Cells[4, 1].Value = "Ref Number";
    //        workSheet.Cells[4, 3].Value = header.RefNumber;

    //        //Headers
    //        workSheet.Cells[7, 1].Value = "NO";
    //        workSheet.Cells[7, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 1].Style.Font.Bold = true;
    //        workSheet.Cells[7, 2].Value = "ITEM";
    //        workSheet.Cells[7, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 2].Style.Font.Bold = true;
    //        workSheet.Cells[7, 3].Value = "DESCRIPTION";
    //        workSheet.Cells[7, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 3].Style.Font.Bold = true;
    //        workSheet.Cells[7, 4].Value = "QTY";
    //        workSheet.Cells[7, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 4].Style.Font.Bold = true;
    //        workSheet.Cells[7, 5].Value = "U/M";
    //        workSheet.Cells[7, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 5].Style.Font.Bold = true;
    //        workSheet.Cells[7, 6].Value = "UNIT PRICE";
    //        workSheet.Cells[7, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 6].Style.Font.Bold = true;
    //        workSheet.Cells[7, 7].Value = "REQUEST DATE";
    //        workSheet.Cells[7, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 7].Style.Font.Bold = true;
    //        workSheet.Cells[7, 8].Value = "REMARKS";
    //        workSheet.Cells[7, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[7, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[7, 8].Style.Font.Bold = true;

    //        //Details
    //        int recordIndex = 8;
    //        int startRow = 8;
    //        int count = 1;

    //        foreach (PurchaseRequestDetail detail in header.PurchaseRequestDetails)
    //        {
    //            workSheet.Cells[recordIndex, 1].Value = count;
    //            workSheet.Cells[recordIndex, 2].Value = detail.MaterialCode;
    //            workSheet.Cells[recordIndex, 3].Value = detail.MaterialName;
    //            workSheet.Cells[recordIndex, 4].Value = detail.Qty;
    //            workSheet.Cells[recordIndex, 5].Value = detail.UoM;
    //            workSheet.Cells[recordIndex, 7].Value = detail.ETA.ToString("d/MMM/yy");

    //            recordIndex++;
    //        }

    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex - 1, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        //Headers
    //        workSheet.Cells[recordIndex + 1, 2, recordIndex + 3, 2].Merge = true;
    //        workSheet.Cells[recordIndex, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 2].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 2].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 2].Value = "ISSUED";
    //        workSheet.Cells[recordIndex + 1, 3, recordIndex + 3, 3].Merge = true;
    //        workSheet.Cells[recordIndex, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 3].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 3].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 3].Value = "SECTION HEAD";
    //        workSheet.Cells[recordIndex, 4, recordIndex, 5].Merge = true;
    //        workSheet.Cells[recordIndex + 1, 4, recordIndex + 3, 5].Merge = true;
    //        workSheet.Cells[recordIndex, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 4].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 4].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 4].Value = "DEPT. HEAD";
    //        workSheet.Cells[recordIndex, 6, recordIndex, 7].Merge = true;
    //        workSheet.Cells[recordIndex + 1, 6, recordIndex + 3, 7].Merge = true;
    //        workSheet.Cells[recordIndex, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 6].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 6].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 6].Value = "GENERAL MANAGER";
    //        workSheet.Cells[recordIndex, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 8].Value = "RECEIPT";

    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        startRow = recordIndex + 1;
    //        recordIndex += 3;
    //        workSheet.Cells[recordIndex, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Bold = true;
    //        workSheet.Cells[recordIndex, 8].Style.Font.Size = 14;
    //        workSheet.Cells[recordIndex, 8].Value = "PURCHASE";

    //        workSheet.Cells[recordIndex, 1, recordIndex, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, startRow, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 1, recordIndex, 1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
    //        workSheet.Cells[startRow, 8, recordIndex, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

    //        var memoryStream = new MemoryStream();

    //        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    //        excel.SaveAs(memoryStream);

    //        memoryStream.Position = 0;

    //        return File(memoryStream, contentType, fileName);
    //    }
    //}
}