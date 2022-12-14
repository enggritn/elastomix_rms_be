using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    [Route("api/pr-box-approval")]
    public class PRBoxApprovalController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        /// <summary>
        /// List PR Box Approval
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IHttpActionResult> Index()
        {
            var prBoxApprovals = db.PRBoxApprovals.ToList();
            var list = prBoxApprovals.GroupBy(x => x.PRBoxGroupCode).Select(x => new PRBoxApprovalModel() { PRBoxGroupCode = x.FirstOrDefault().PRBoxGroupCode }).ToList();
            return Ok(list);
        }

        [HttpGet]
        [Route("api/pr-box-approval/{id}")]
        public async Task<IHttpActionResult> GetDetail(string id)
        {
            var prBoxApprovals = db.PRBoxApprovals.Where(x => x.PRBoxGroupCode == id).ToList();
            var model = new PRBoxApprovalModel();
            if(prBoxApprovals != null && prBoxApprovals.Count > 0)
            {
                model.PRBoxGroupCode = prBoxApprovals.FirstOrDefault().PRBoxGroupCode;
                model.PRBoxGroupDescription = !string.IsNullOrEmpty(prBoxApprovals.FirstOrDefault().PRBoxGroupDescription) ? prBoxApprovals.FirstOrDefault().PRBoxGroupDescription : string.Empty;
                model.Details = new List<PRBoxApprovalDetailModel>();
                foreach (var prBoxApproval in prBoxApprovals.OrderBy(x => x.SequenceNo))
                {
                    model.Details.Add(new PRBoxApprovalDetailModel()
                    {
                        SequenceNo = prBoxApproval.SequenceNo,
                        PRBoxName = prBoxApproval.PRBoxName,
                        PRBoxTitle = prBoxApproval.PRBoxTitle
                    });
                }
            }


            return Ok(model);
        }
        
        /// <summary>
        /// Create PR Box Approval
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IHttpActionResult> Save(PRBoxApprovalModel model)
        {
            HttpRequest request = HttpContext.Current.Request;
            var re = Request;
            var headers = re.Headers;

            StringBuilder sbError = new StringBuilder();

            string token = "";
            if (headers.Contains("token"))
            {
                token = headers.GetValues("token").First();
            }

            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

            if (activeUser == null)
            {
                sbError.AppendLine("User token failed");
            }
            
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    sbError.AppendLine(state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0]);
                }
            } else
            {
                if (!(model.Details != null && model.Details.Count > 0))
                {
                    sbError.AppendLine("must have approval");
                }
            }

            if(sbError.Length > 0)
            {
                return BadRequest(sbError.ToString());
            } else
            {
                db.Database.ExecuteSqlCommand(string.Format("DELETE FROM PRBoxApproval WHERE PRBoxGroupCode = '{0}'", model.PRBoxGroupCode));
                foreach (var item in model.Details)
                {
                    var _item = new PRBoxApproval();
                    _item.ID = Guid.NewGuid();
                    _item.PRBoxGroupCode = model.PRBoxGroupCode;
                    _item.PRBoxGroupDescription = !string.IsNullOrEmpty(model.PRBoxGroupDescription) ? model.PRBoxGroupDescription : string.Empty;
                    _item.SequenceNo = item.SequenceNo;
                    _item.PRBoxName = item.PRBoxName;
                    _item.PRBoxTitle = item.PRBoxTitle;
                    _item.CreatedBy = activeUser;
                    _item.CreatedOn = DateTime.Now;
                    _item.ModifiedBy = activeUser;
                    _item.ModifiedOn = DateTime.Now;
                    db.PRBoxApprovals.Add(_item);
                }

                db.SaveChangesAsync();
                return Ok(string.Format("PR Box Approval : {0} success save", model.PRBoxGroupCode));
            }
        }

        [HttpDelete]
        public async Task<IHttpActionResult> Delete(string PRBoxGroupCode)
        {
            var prboxapproval = db.PRBoxApprovals.FirstOrDefault(x => x.PRBoxGroupCode == PRBoxGroupCode);
            if(prboxapproval != null && !string.IsNullOrEmpty(prboxapproval.PRBoxGroupCode))
            {
                db.Database.ExecuteSqlCommand(string.Format("DELETE FROM PRBoxApproval WHERE PRBoxGroupCode = '{0}'", PRBoxGroupCode));
            } else
            {
                return BadRequest("item not found");
            }

            return Ok(string.Format("Success to delete ", PRBoxGroupCode));
        }
    }
}
