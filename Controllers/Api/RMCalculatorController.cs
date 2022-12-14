using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.UI;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class RMCalculatorController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpPost]
        //public async Task<IHttpActionResult> Calculate(RMCalculationVM sample)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    decimal TotalTon = 0;
        //    decimal Ton = 0;


        //    List<OrderNumberDetailDTO> listRM = new List<OrderNumberDetailDTO>();

        //    try
        //    {
        //        Formula formula = await db.Formulae.Where(s => s.ID.Equals(sample.FormulaID)).FirstOrDefaultAsync();

        //        if (formula != null)
        //        {

        //            decimal TotalQuantity = Convert.ToDecimal(db.FormulaDetails.Where(m => m.FormulaID == sample.FormulaID).Select(i => i.Qty).Sum().ToString());
        //            Ton = TotalQuantity;
        //            // Edited by Kenzi 2020-08-17 -- Ganti jadi dibuletin ke bawah
        //            int totalBatch = (int) (Convert.ToDecimal(sample.Qty) / TotalQuantity);
        //            TotalTon = Ton * totalBatch;


        //            foreach (FormulaDetail formulaDetail in formula.FormulaDetails)
        //            {
        //                OrderNumberDetailDTO detail = new OrderNumberDetailDTO()
        //                {
        //                    UoM = "kg",
        //                    RequestedQty = Helper.FormatThousand(formulaDetail.Qty),
        //                    FullBag = Helper.FormatThousand(formulaDetail.Fullbag),
        //                    RemainderQty = Helper.FormatThousand(formulaDetail.RemainderQty),
        //                    ProductionQty = Helper.FormatThousand(totalBatch),
        //                    TotalQty = Helper.FormatThousand(formulaDetail.Qty * totalBatch),
        //                    AvailableQty = "0",
        //                    LackingQty = Helper.FormatThousand(formulaDetail.Qty * totalBatch)
        //                };

        //                Stock stock = db.Stocks.Where(s => s.RawMaterialID.Equals(detail.RawMaterialID)).FirstOrDefault();

        //                if (stock != null)
        //                {
        //                    detail.AvailableQty = Helper.FormatThousand(stock.ActualQty);
        //                    detail.LackingQty = (formulaDetail.Qty * totalBatch) - stock.ActualQty  > 0 ? Helper.FormatThousand((formulaDetail.Qty * totalBatch) - stock.ActualQty) : "0";
        //                }

        //                listRM.Add(detail);

        //                //Ton += formulaDetail.Qty;
        //                //TotalTon += (formulaDetail.Qty * totalBatch);
        //            }


                  
        //            message = "Fetch data succeeded.";
        //            status = true;
        //        }
        //        else
        //        {
        //            message = "Formula does not exist.";
        //        }
        //    }
        //    catch (HttpRequestException reqpEx)
        //    {
        //        message = reqpEx.Message;
        //        return BadRequest();
        //    }
        //    catch (HttpResponseException respEx)
        //    {
        //        message = respEx.Message;
        //        return NotFound();
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    obj.Add("ton", Helper.FormatThousand(Ton / 1000));
        //    obj.Add("totalTon", Helper.FormatThousand(TotalTon / 1000));
        //    obj.Add("data", listRM);
        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    return Ok(obj);
        //}
    }
}
