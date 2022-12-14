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
    public class UsageController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        //[HttpPost]
        //public async Task<IHttpActionResult> InspectionManual(InspectionVM inspectionVM)
        //{
        //    inspectionVM.InspectionMethod = "Manual";
        //    return await Inspect(inspectionVM);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> InspectionScan(InspectionVM inspectionVM)
        //{
        //    inspectionVM.InspectionMethod = "Scan";
        //    return await Inspect(inspectionVM);
        //}

        //public async Task<IHttpActionResult> Inspect(InspectionVM inspectionVM)
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
        //            ReceivingHeader receivingHeader = await db.ReceivingHeaders.Where(s => s.ID.Equals(inspectionVM.HeaderID)).FirstOrDefaultAsync();

        //            if (receivingHeader != null)
        //            {
        //                ReceivingDetail detail = receivingHeader.ReceivingDetails.Where(s => s.MaterialCode.Equals(inspectionVM.MaterialCode)).FirstOrDefault();

        //                if (detail != null)
        //                {
        //                    if (detail.InspectedOn == null)
        //                    {
        //                        detail.InspectionMethod = inspectionVM.InspectionMethod;
        //                        detail.InspectedOn = DateTime.Now;
        //                        detail.InspectedBy = activeUser;
        //                        detail.InspectionQty = inspectionVM.Qty;
        //                        detail.ActualQty = inspectionVM.Qty;

        //                        if (detail.InspectionQty > detail.ReceiveQty)
        //                        {
        //                            throw new Exception("Inspection quantity can not exceed the received quantity.");
        //                        }
        //                        else if (detail.InspectionQty < 0)
        //                        {
        //                            throw new Exception("Judgement quantity can not be below zero.");
        //                        }
        //                        else
        //                        {
        //                            await db.SaveChangesAsync();
        //                            status = true;
        //                            message = "Inspection succeeded.";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        message = "This Raw Material is already inspected.";
        //                    }
        //                }
        //                else
        //                {
        //                    message = "Raw Material is not recognized.";
        //                }
        //            }
        //            else
        //            {
        //                message = "Header is not recognized.";
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

        //[HttpPost]
        //public async Task<IHttpActionResult> JudgementManual(JudgementVM judgementVM)
        //{
        //    judgementVM.JudgementMethod = "Manual";
        //    return await Judge(judgementVM);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> JudgementScan(JudgementVM judgementVM)
        //{
        //    judgementVM.JudgementMethod = "Scan";
        //    return await Judge(judgementVM);
        //}

        //public async Task<IHttpActionResult> Judge(JudgementVM judgementVM)
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
        //            ReceivingHeader receivingHeader = await db.ReceivingHeaders.Where(s => s.ID.Equals(judgementVM.HeaderID)).FirstOrDefaultAsync();

        //            if (receivingHeader != null)
        //            {
        //                ReceivingDetail detail = receivingHeader.ReceivingDetails.Where(s => s.MaterialCode.Equals(judgementVM.MaterialCode)).FirstOrDefault();

        //                if (detail != null && detail.InspectedOn != null)
        //                {
        //                    if (detail.JudgeOn == null)
        //                    {
        //                        detail.JudgementMethod = judgementVM.JudgementMethod;
        //                        detail.JudgeOn = DateTime.Now;
        //                        detail.JudgeBy = activeUser;
        //                        detail.JudgementQty = judgementVM.Qty;
        //                        detail.ActualQty += detail.JudgementQty;

        //                        if (detail.JudgementQty > (detail.ReceiveQty - detail.InspectionQty))
        //                        {
        //                            throw new Exception("Judgement quantity can not exceed the NG quantity.");
        //                        }
        //                        else if (detail.JudgementQty < 0)
        //                        {
        //                            throw new Exception("Judgement quantity can not be below zero.");
        //                        }
        //                        else
        //                        {
        //                            await db.SaveChangesAsync();
        //                            status = true;
        //                            message = "Judgement succeeded.";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        message = "This Raw Material is already judged.";
        //                    }
        //                }
        //                else
        //                {
        //                    message = "Raw Material is not recognized or has not been inspected before.";
        //                }
        //            }
        //            else
        //            {
        //                message = "Header is not recognized.";
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

        //[HttpPost]
        //public async Task<IHttpActionResult> PutawayManual(PutawayVM putawayVM)
        //{
        //    putawayVM.PutawayMethod = "Manual";
        //    return await Putaway(putawayVM);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> PutawayScan(PutawayVM putawayVM)
        //{
        //    putawayVM.PutawayMethod = "Scan";
        //    return await Putaway(putawayVM);
        //}

        //public async Task<IHttpActionResult> Putaway(PutawayVM putawayVM)
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
        //            BinRack binRack = await db.BinRacks.Where(s => s.ID.Equals(putawayVM.BinRackID)).FirstOrDefaultAsync();

        //            if (binRack != null)
        //            {
        //                ReceivingHeader receivingHeader = await db.ReceivingHeaders.Where(s => s.ID.Equals(putawayVM.HeaderID)).FirstOrDefaultAsync();

        //                if (receivingHeader != null)
        //                {
        //                    ReceivingDetail detail = receivingHeader.ReceivingDetails.Where(s => s.MaterialCode.Equals(putawayVM.MaterialCode)).FirstOrDefault();

        //                    if (detail != null && detail.InspectedOn != null)
        //                    {
        //                        if (detail.PutOn == null)
        //                        {
        //                            detail.PutawayMethod = putawayVM.PutawayMethod;
        //                            detail.PutOn = DateTime.Now;
        //                            detail.PutBy = activeUser;
        //                            detail.BinRackID = putawayVM.BinRackID;
        //                            detail.BinRackCode = binRack.Code;
        //                            detail.BinRackName = binRack.Name;

        //                            await db.SaveChangesAsync();
        //                            status = true;
        //                            message = "Putaway succeeded.";
        //                        }
        //                        else
        //                        {
        //                            message = "This Raw Material is already put away.";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        message = "Raw Material is not recognized or has not been inspected nor judged before.";
        //                    }
        //                }
        //                else
        //                {
        //                    message = "Header is not recognized.";
        //                }
        //            }
        //            else
        //            {
        //                message = "Bin Rack is not recognized.";
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
