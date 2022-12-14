using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class BinRackVM
    {
        public string ID { get; set; }

        public string BinRackAreaID { get; set; }
      

        [Required(ErrorMessage = "BinRack Name is required.")]
        public string Name { get; set; }
        //public string Code { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class BinRackGet
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }

        public string Name { get; set; }
    }

    public class BinRackDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaName { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class BinRackItemDTO
    {
        public string BinRackAreaCode { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public int TotalQty { get; set; }
    }


    public class AreaCodes
    {
        public string[] AreaCode { get; set; }
    }
}