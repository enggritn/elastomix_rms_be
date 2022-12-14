using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WMS_BE.Utils
{
    public class Helper
    {
        public static string RenderViewToString(ControllerContext context, String viewPath, object model = null)
        {
            context.Controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindView(context, viewPath, null);
                var viewContext = new ViewContext(context, viewResult.View, context.Controller.ViewData, context.Controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(context, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        internal static string EncodeTo64(object p)
        {
            throw new NotImplementedException();
        }

        public static string CreateGuid(string prefix)
        {
            return string.Format("{0}x{1:N}", prefix, Guid.NewGuid());
        }

        public static string EncodeTo64(string value)
        {
            Encoding encodingUTF8 = Encoding.UTF8;
            byte[] encData_byte = new byte[value.Length];
            encData_byte = encodingUTF8.GetBytes(value);
            string returnValue = Convert.ToBase64String(encData_byte);
            return returnValue;
        }

        public static string DecodeFrom64(string value)
        {
            Encoding encodingUTF8 = Encoding.UTF8;
            return encodingUTF8.GetString(Convert.FromBase64String(value));
        }

        public static string UpperFirstCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string val = "";
            string[] words = value.Split(' ');

            foreach (string word in words)
            {
                val += char.ToUpper(word[0]) + word.Substring(1).ToLower() + ' ';
            }

            return val.Trim();
        }

        public static string ToUpper(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.ToUpper().Trim();
        }

        public static string ToLower(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.ToLower().Trim();
        }

        public static string CallOut(string type, string message)
        {
            string callout = string.Format("<div class=\"alert alert-{0}\" role=\"alert\">", type);
            callout += message;
            callout += "</div>";

            return callout;
        }

        public static string NullDateTimeToString(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd MMM yyyy HH:mm:ss") : "-";
        }

        public static string NullDateToString(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd MMM yyyy") : "-";
        }

        public static string NullDateToString2(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "-";
        }

        public static string NullDateToString3(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "";
        }

        public static string ConvertMonthToRoman(int month)
        {
            string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            string roman = "";
            try
            {
                roman = romans[month - 1];
            }
            catch (Exception)
            {

            }
            return roman;
        }

        //public static string FormatDecimalThousand(decimal? value)
        //{
        //    NumberFormatInfo nfi = new NumberFormatInfo();
        //    nfi.NumberDecimalSeparator = ".";
        //    nfi.NumberGroupSeparator = ",";
        //    return string.Format(nfi, "{0:#,0.0000}", value);
        //}

        public static string FormatThousand(int value)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = ",";
            return string.Format(nfi, "{0:#,0}", value);
        }

        public static string FormatThousand(decimal? value)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = ",";

            decimal val = value != null ? Convert.ToDecimal(value.Value.ToString("0.####")) : 0;
            string format = "{0:#,0.00}";

            decimal? res = val % 1;

            if (res > 0)
            {
                string number = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
                int length = number.Substring(number.IndexOf(".") + 1).Length;
                if (length > 2)
                {
                    format = "{0:#,0.000}";
                }
            }

            return string.Format(nfi, format, val);
        }

        public static string FormatThousand2(decimal? value)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = ",";

            decimal val = value != null ? Convert.ToDecimal(value.Value.ToString("0.####")) : 0;
            string format = "{0:#,0.00}";

            decimal? res = val % 1;

            if (res > 0)
            {
                string number = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
                int length = number.Substring(number.IndexOf(".") + 1).Length;
                if (length > 2)
                {
                    format = "{0:#,0.0000}";
                }
            }

            return string.Format(nfi, format, val);
        }

        public static string StockCode(string MaterialCode, decimal QtyPerBag, string LotNo = "", DateTime? InDate = null, DateTime? ExpDate = null)
        {
            if(string.IsNullOrEmpty(LotNo) && !InDate.HasValue && !ExpDate.HasValue)
            {
                return string.Format("{0}{1}", MaterialCode, FormatThousand(QtyPerBag));
            }

            return string.Format("{0}{1}{2}{3}{4}", MaterialCode, FormatThousand(QtyPerBag), LotNo, InDate.Value.ToString("yyyyMMdd").Substring(1), ExpDate.Value.ToString("yyyyMMdd").Substring(2));
        }

        //public static decimal CalcMod(decimal value)
        //{
        //    decimal res = value % 1;
        //    if (res > 0)
        //    {
        //        string number = value.ToString(); // Convert to string
        //        value = Convert.ToDecimal(number.Substring(number.IndexOf(".") + 1));
        //        res = CalcMod(value);
        //    }

        //    return res;
        //}

        public static string IsActiveIcon(bool status)
        {
            string icon = "";
            switch (status)
            {
                case true:
                    icon = "<i class=\"fa fa-check text-primary\"></i>";
                    break;
                case false:
                    icon = "<i class=\"fa fa-times text-danger\"></i>";
                    break;
            }

            return icon;
        }

        public static string BarcodeLeft(string MaterialCode, DateTime? InDate, DateTime? ExpDate)
        {
            string inDate = "";
            string expDate = "";

            if (InDate.HasValue)
            {
                inDate = InDate.Value.ToString("yyyyMMdd").Substring(1);
            }

            if (ExpDate.HasValue)
            {
                expDate = ExpDate.Value.ToString("yyyyMMdd").Substring(2);
            }

            return string.Format("{0}{1}{2}", MaterialCode, inDate, expDate);
        }

        public static string BarcodeRight(string MaterialCode, string RunningNumber, string QtyPerBag, string LotNumber)
        {
            return string.Format("{0} {1} {2} {3}", MaterialCode, RunningNumber, QtyPerBag, LotNumber);
        }

    }
}