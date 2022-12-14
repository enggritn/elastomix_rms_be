using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class DataTableParameters
    {
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public order[] order { get; set; }
        public column[] columns { get; set; }
    }

    public class order
    {
        public int column { get; set; }
        public string dir { get; set; }
    }

    public class column
    {
        public string data { get; set; }
        public string name { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public search search { get; set; }
    }

    public class search
    {
        public string value { get; set; }
        public bool regex { get; set; }
    }
}