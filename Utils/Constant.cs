using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Utils
{
    public class Constant
    {
        public const string app_token_key = "ein_wms_wip";

        public static List<string> WarehouseTypes()
        {
            List<string> types = new List<string>();
            types.Add("EMIX");
            types.Add("OUTSOURCE");
            return types;
        }

        public static List<string> AreaTypes()
        {
            List<string> types = new List<string>();
            types.Add("PRODUCTION");
            types.Add("LOGISTIC");
            return types;
        }

        public static List<string> SourceTypes()
        {
            List<string> types = new List<string>();
            types.Add("VENDOR");
            types.Add("CUSTOMER");
            types.Add("OUTSOURCE");
            types.Add("IMPORT");
            //types.Add("MANUAL");
            types.Add("OTHER");
            return types;
        }

        public static List<string> ActionTypes()
        {
            List<string> types = new List<string>();
            types.Add("EXTEND");
            types.Add("DISPOSAL");
            return types;
        }

        public static List<double> TruckTypes()
        {
            List<double> types = new List<double>();
            types.Add(2);
            types.Add(2.5);
            types.Add(4);
            types.Add(5);
            types.Add(6);
            types.Add(8);
            types.Add(10);
            types.Add(13);
            types.Add(15);
            return types;
        }

        public static List<string> ProductTypes()
        {
            List<string> types = new List<string>();
            types.Add("FG");
            types.Add("SFG");
            return types;
        }

        public static Dictionary<int, string> LineTypes()
        {
            Dictionary<int, string> types = new Dictionary<int, string>();
            types.Add(1, "Line 1");
            types.Add(2, "Line 2");
            return types;
        }
        public static List<string> StockCategoryNameSFG()
        {
            List<string> types = new List<string>();
            types.Add("A Mixing Product");
            types.Add("B Mixing Product");
            types.Add("R Mixing Product");
            types.Add("M Mixing Product");
            types.Add("S Mixing Product");
            return types;
        }
    }
}