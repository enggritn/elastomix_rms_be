using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class PutawayController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //public async Task<IHttpActionResult> Putaway(PutawayDetailVM putawayDetailVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        //    string message = "";
        //    bool status = false;
        //    var re = Request;
        //    var headers = re.Headers;

        //    try
        //    {
        //        string token = "";
        //        if (headers.Contains("token"))
        //        {
        //            token = headers.GetValues("token").First();
        //        }

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            PutawayDetail putawayDetail = await db.PutawayDetails.Where(s => s.Barcode.Equals(putawayDetailVM.Barcode)).FirstOrDefaultAsync();

        //            if (putawayDetail != null)
        //            {
        //                BinRack binRack = await db.BinRacks.Where(s => s.ID.Equals(putawayDetailVM.BinRackID)).FirstOrDefaultAsync();

        //                if (binRack != null)
        //                {
        //                    putawayDetail.PutMethod = putawayDetailVM.PutMethod;
        //                    putawayDetail.PutOn = DateTime.Now;
        //                    putawayDetail.PutBy = activeUser;
        //                    putawayDetail.BinRackId = putawayDetailVM.BinRackID;
        //                    putawayDetail.BinRackCode = binRack.Code;
        //                    putawayDetail.Rack = binRack.Name;

        //                    await db.SaveChangesAsync();
        //                    status = true;
        //                    message = "Create data succeeded.";
        //                }
        //                else
        //                {
        //                    message = "BinRack does not exist.";
        //                }
        //            }
        //            else
        //            {
        //                message = "Item does not exist.";
        //            }
        //        }
        //        else
        //        {
        //            message = "Token is no longer valid. Please re-login.";
        //        }
        //    }
        //    catch (HttpRequestException reqpEx)
        //    {
        //        message = reqpEx.Message;
        //    }
        //    catch (HttpResponseException respEx)
        //    {
        //        message = respEx.Message;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    obj.Add("status", status);
        //    obj.Add("message", message);
        //    obj.Add("error_validation", customValidationMessages);

        //    return Ok(obj);
        //}
    }
}
