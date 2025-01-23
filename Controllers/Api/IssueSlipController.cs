using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class IssueSlipController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Datatable(string transactionStatus)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<IssueSlipHeader> list = Enumerable.Empty<IssueSlipHeader>();
            IEnumerable<IssueSlipHeaderDTO> pagedData = Enumerable.Empty<IssueSlipHeaderDTO>();
            IQueryable<IssueSlipHeader> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.IssueSlipHeaders.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();
            }
            else if (transactionStatus.Equals("OPEN/PROGRESS"))
            {
                query = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
            }
            else
            {
                query = db.IssueSlipHeaders.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                         || m.Name.Contains(search)
                         //|| m.TotalRequestedQty.Contains(search)
                         || m.CreatedBy.Contains(search)
                         || m.ModifiedBy.Contains(search)
                        );

                Dictionary<string, Func<IssueSlipHeader, object>> cols = new Dictionary<string, Func<IssueSlipHeader, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("Code", x => x.Code);
                cols.Add("Name", x => x.Name);
                cols.Add("ProductionDate", x => x.ProductionDate);
                cols.Add("TotalRequestedQty", x => x.IssueSlipOrders.Sum(i => i.Qty));
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("ModifiedBy", x => x.ModifiedBy);
                cols.Add("ModifiedOn", x => x.ModifiedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipHeaderDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    Name = x.Name,
                                    ProductionDate = Helper.NullDateToString(x.ProductionDate),
                                    TotalRequestedQty = Helper.FormatThousand(x.IssueSlipOrders.Sum(i => i.Qty)),
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    ModifiedBy = x.ModifiedBy != null ? x.ModifiedBy : "",
                                    ModifiedOn = Helper.NullDateTimeToString(x.ModifiedOn),
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            IssueSlipHeaderDTO issueSlipHeaderDTO = null;

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }

                IssueSlipHeader header = await db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();
                if (header == null || header.TransactionStatus == "CANCELLED")
                {
                    throw new Exception("Data not found.");
                }

                issueSlipHeaderDTO = new IssueSlipHeaderDTO
                {
                    ID = header.ID,
                    Code = header.Code,
                    Name = header.Name,
                    TotalRequestedQty = Helper.FormatThousand(header.IssueSlipOrders.Sum(i => i.Qty)),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    ModifiedBy = header.ModifiedBy != null ? header.ModifiedBy : "",
                    ModifiedOn = Helper.NullDateTimeToString(header.ModifiedOn)
                };


                status = true;
                message = "Fetch data succeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("data", issueSlipHeaderDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableOrder(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<IssueSlipOrder> list = Enumerable.Empty<IssueSlipOrder>();
            IEnumerable<IssueSlipOrderDTO> pagedData = Enumerable.Empty<IssueSlipOrderDTO>();

            IQueryable<IssueSlipOrder> query = db.IssueSlipOrders.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.VendorName.Contains(search)
                        );

                Dictionary<string, Func<IssueSlipOrder, object>> cols = new Dictionary<string, Func<IssueSlipOrder, object>>();
                cols.Add("No", x => x.No);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("VendorName", x => x.VendorName);
                cols.Add("RequestedQty", x => x.Qty);
                cols.Add("PickedQty", x => x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag));
                cols.Add("DiffQty", x => x.Qty - x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag));

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipOrderDTO
                                {
                                    ID = x.ID,
                                    Number = x.No.ToString(),
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    VendorName = x.VendorName,
                                    RequestedQty = Helper.FormatThousand(x.Qty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    PickedQty = Helper.FormatThousand(x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)),
                                    OutstandingQty = Helper.FormatThousand(x.Qty - (x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))),
                                    PickingBagQty = x.QtyPerBag == 0 ? "0" : Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((x.Qty - (x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / x.QtyPerBag))),
                                    DiffQty = Helper.FormatThousand(x.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - x.Qty)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Upload()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string TransactionId = "";
            try
            {
                string token = "";
                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();
                if (activeUser != null)
                {
                    if (request.Files.Count > 0)
                    {
                        HttpPostedFile file = request.Files[0];

                        if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                        {
                            if (file.ContentLength < (10 * 1024 * 1024))
                            {
                                try
                                {
                                    Stream stream = file.InputStream;
                                    IExcelDataReader reader = null;
                                    if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                    {
                                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                    }
                                    else
                                    {
                                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                    }

                                    DataSet result = reader.AsDataSet();
                                    reader.Close();

                                    DataTable dt = result.Tables[0];

                                    string fileName = file.FileName.Replace(Path.GetExtension(file.FileName), "");

                                    //issue slip date
                                    //string title = dt.Rows[2]["A"].ToString();

                                    DataRow r1 = dt.Rows[1];

                                    string title = r1[0].ToString();

                                    String[] inputText = title.Split(' ');

                                    DateTime dateTime = new DateTime();
                                    foreach (String text in inputText)
                                    {
                                        //Use the Parse() method
                                        try
                                        {
                                            dateTime = DateTime.Parse(text);
                                            break;//no need to execute/loop further if you have your date
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                    }

                                    IssueSlipHeader temp = await db.IssueSlipHeaders.Where(s => s.Name.Equals(fileName) && !s.TransactionStatus.Equals("CANCELLED")).FirstOrDefaultAsync();
                                    if (temp != null)
                                    {
                                        throw new Exception(string.Format("Upload failed, Filename {0} already exist.", fileName));
                                    }

                                    IssueSlipHeader temp2 = await db.IssueSlipHeaders.Where(s => s.ProductionDate.Equals(dateTime.Date) && !s.TransactionStatus.Equals("CANCELLED")).FirstOrDefaultAsync();
                                    if (temp2 == null)
                                    {
                                        TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                                        DateTime now = DateTime.Now;
                                        DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                                        var CreatedAt = transactionDate;

                                        TransactionId = Helper.CreateGuid("IS");

                                        string prefix = TransactionId.Substring(0, 2);
                                        int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                                        int month = CreatedAt.Month;
                                        string romanMonth = Helper.ConvertMonthToRoman(month);

                                        // get last number, and do increment.
                                        string lastNumber = db.IssueSlipHeaders.AsQueryable().OrderByDescending(x => x.Code)
                                            .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                                            .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                        int currentNumber = 0;

                                        if (!string.IsNullOrEmpty(lastNumber))
                                        {
                                            currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                                        }

                                        string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                                        var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);


                                        IssueSlipHeader header = new IssueSlipHeader
                                        {
                                            ID = TransactionId,
                                            Code = Code,
                                            TransactionStatus = "OPEN",
                                            CreatedBy = activeUser,
                                            CreatedOn = CreatedAt,
                                            Name = fileName,
                                            ProductionDate = dateTime.Date
                                        };


                                        int No = 1;
                                        foreach (DataRow row in dt.AsEnumerable().Skip(9))
                                        {
                                            string itemCode = row[1].ToString();
                                            decimal Qty = string.IsNullOrEmpty(row[4].ToString()) ? 0 : decimal.Parse(row[4].ToString());

                                            if (!string.IsNullOrEmpty(itemCode))
                                            {
                                                //loop check if material doesnt exist, throw exception
                                                vProductMaster productMaster = db.vProductMasters.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                                if (productMaster == null)
                                                {
                                                    throw new Exception(string.Format("Upload failed, Material Code {0} does not exist.", row[1].ToString()));
                                                }

                                                IssueSlipOrder order = new IssueSlipOrder()
                                                {
                                                    ID = Helper.CreateGuid("ISO"),
                                                    No = No,
                                                    HeaderID = header.ID,
                                                    MaterialCode = productMaster.MaterialCode,
                                                    MaterialName = productMaster.MaterialName,
                                                    QtyPerBag = productMaster.QtyPerBag,
                                                    VendorName = row[3].ToString(),
                                                    UoM = "KG",
                                                    Qty = Qty
                                                };

                                                header.IssueSlipOrders.Add(order);
                                                No++;
                                            }
                                        }

                                        db.IssueSlipHeaders.Add(header);
                                    }
                                    else
                                    {
                                        //if closed, open it automatically
                                        if (temp2.TransactionStatus.Equals("CLOSED"))
                                        {
                                            temp2.TransactionStatus = "OPEN";
                                        }

                                        TransactionId = temp2.ID;
                                        int No = temp2.IssueSlipOrders.OrderByDescending(m => m.No).Select(m => m.No).FirstOrDefault();
                                        foreach (DataRow row in dt.AsEnumerable().Skip(9))
                                        {
                                            string itemCode = row[1].ToString();
                                            decimal Qty = string.IsNullOrEmpty(row[4].ToString()) ? 0 : decimal.Parse(row[4].ToString());
                                            if (!string.IsNullOrEmpty(itemCode))
                                            {
                                                //loop check if material doesnt exist, throw exception
                                                vProductMaster productMaster = db.vProductMasters.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                                if (productMaster == null)
                                                {
                                                    throw new Exception(string.Format("Upload failed, Material Code {0} does not exist.", row[1].ToString()));
                                                }
                                                //loop check if material doesnt exist, throw exception
                                                IssueSlipOrder order = temp2.IssueSlipOrders.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                                if (order == null)
                                                {
                                                    order = new IssueSlipOrder()
                                                    {
                                                        ID = Helper.CreateGuid("ISO"),
                                                        No = No,
                                                        HeaderID = TransactionId,
                                                        MaterialCode = productMaster.MaterialCode,
                                                        MaterialName = productMaster.MaterialName,
                                                        QtyPerBag = productMaster.QtyPerBag,
                                                        VendorName = row[3].ToString(),
                                                        UoM = "KG",
                                                        Qty = Qty
                                                    };
                                                    db.IssueSlipOrders.Add(order);
                                                    No++;
                                                }
                                                else
                                                {
                                                    order.Qty += Qty;
                                                }
                                            }

                                        }
                                    }

                                    //temp = await db.IssueSlipHeaders.Where(s => DbFunctions.TruncateTime(s.CreatedOn) == DbFunctions.TruncateTime(DateTime.Now) && (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS"))).FirstOrDefaultAsync();
                                    //if (temp == null)
                                    //{
                                    //    TransactionId = Helper.CreateGuid("IS");

                                    //    string prefix = TransactionId.Substring(0, 2);
                                    //    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                                    //    int month = CreatedAt.Month;
                                    //    string romanMonth = Helper.ConvertMonthToRoman(month);

                                    //    // get last number, and do increment.
                                    //    string lastNumber = db.IssueSlipHeaders.AsQueryable().OrderByDescending(x => x.Code)
                                    //        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                                    //        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                                    //    int currentNumber = 0;

                                    //    if (!string.IsNullOrEmpty(lastNumber))
                                    //    {
                                    //        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                                    //    }

                                    //    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                                    //    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                                    //    header = new IssueSlipHeader
                                    //    {
                                    //        ID = TransactionId,
                                    //        Code = Code,
                                    //        TransactionStatus = "OPEN",
                                    //        CreatedBy = activeUser,
                                    //        CreatedOn = CreatedAt,
                                    //        Name = fileName
                                    //    };

                                    //    int No = 1;
                                    //    foreach (DataRow row in dt.AsEnumerable().Skip(9))
                                    //    {
                                    //        string itemCode = row[1].ToString();
                                    //        if (!string.IsNullOrEmpty(itemCode))
                                    //        {
                                    //            //loop check if material doesnt exist, throw exception
                                    //            vProductMaster productMaster = db.vProductMasters.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                    //            if (productMaster == null)
                                    //            {
                                    //                throw new Exception(string.Format("Upload failed, Material Code {0} does not exist.", row[1].ToString()));
                                    //            }

                                    //            decimal Qty = string.IsNullOrEmpty(row[5].ToString()) ? 0 : decimal.Parse(row[5].ToString());
                                    //            IssueSlipOrder order = new IssueSlipOrder()
                                    //            {
                                    //                ID = Helper.CreateGuid("ISO"),
                                    //                No = No,
                                    //                HeaderID = header.ID,
                                    //                MaterialCode = productMaster.MaterialCode,
                                    //                MaterialName = productMaster.MaterialName,
                                    //                QtyPerBag = productMaster.QtyPerBag,
                                    //                VendorName = row[3].ToString(),
                                    //                UoM = "KG",
                                    //                Qty = Qty
                                    //            };

                                    //            header.IssueSlipOrders.Add(order);
                                    //            No++;
                                    //        }
                                    //    }

                                    //    db.IssueSlipHeaders.Add(header);
                                    //}
                                    //else
                                    //{
                                    //    foreach (DataRow row in dt.AsEnumerable().Skip(9))
                                    //    {
                                    //        string itemCode = row[1].ToString();
                                    //        if (!string.IsNullOrEmpty(itemCode))
                                    //        {
                                    //            //loop check if material doesnt exist, throw exception
                                    //            IssueSlipOrder order = temp.IssueSlipOrders.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                    //            if (order == null)
                                    //            {
                                    //                throw new Exception(string.Format("Upload failed, Material Code {0} does not exist on previous issue slip.", row[1].ToString()));
                                    //            }

                                    //            decimal Qty = string.IsNullOrEmpty(row[5].ToString()) ? 0 : decimal.Parse(row[5].ToString());
                                    //            order.Qty += Qty;
                                    //        }

                                    //    }
                                    //}

                                    await db.SaveChangesAsync();
                                    message = "Upload succeeded.";
                                    status = true;
                                }
                                catch (Exception e)
                                {
                                    message = string.Format("Upload item failed. {0}", e.Message);
                                }
                            }
                            else
                            {
                                message = "Upload failed. Maximum allowed file size : 10MB ";
                            }
                        }
                        else
                        {
                            message = "Upload item failed. File is invalid.";
                        }
                    }
                    else
                    {
                        message = "No file uploaded.";
                    }
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("ID", TransactionId);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> UploadRev()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            HttpRequest request = HttpContext.Current.Request;

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;
            string TransactionId = "";
            string id = request.Params.Get("id");
            try
            {
                string token = "";
                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (request.Files.Count > 0)
                    {
                        HttpPostedFile file = request.Files[0];

                        if (file != null && file.ContentLength > 0 && (Path.GetExtension(file.FileName).ToLower() == ".xlsx" || Path.GetExtension(file.FileName).ToLower() == ".xls"))
                        {
                            if (file.ContentLength < (10 * 1024 * 1024))
                            {
                                try
                                {
                                    Stream stream = file.InputStream;
                                    IExcelDataReader reader = null;
                                    if ((Path.GetExtension(file.FileName).ToLower() == ".xlsx"))
                                    {
                                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                                    }
                                    else
                                    {
                                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                                    }

                                    DataSet result = reader.AsDataSet();
                                    reader.Close();

                                    DataTable dt = result.Tables[0];

                                    //issue slip date
                                    //string title = dt.Rows[2]["A"].ToString();

                                    DataRow r1 = dt.Rows[1];

                                    string title = r1[0].ToString();

                                    String[] inputText = title.Split(' ');

                                    DateTime dateTime = new DateTime();
                                    foreach (String text in inputText)
                                    {
                                        //Use the Parse() method
                                        try
                                        {
                                            dateTime = DateTime.Parse(text);
                                            break;//no need to execute/loop further if you have your date
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                    }

                                    IssueSlipHeader issueSlipHeader = db.IssueSlipHeaders.Where(m => m.ID.Equals(id)).FirstOrDefault();
                                    if (issueSlipHeader == null)
                                    {
                                        throw new Exception("Data not found.");
                                    }

                                    if (!issueSlipHeader.ProductionDate.Equals(dateTime))
                                    {
                                        throw new Exception("Production Date not match.");
                                    }

                                    //if closed, open it automatically
                                    if (issueSlipHeader.TransactionStatus.Equals("CLOSED"))
                                    {
                                        issueSlipHeader.TransactionStatus = "OPEN";
                                    }

                                    TransactionId = issueSlipHeader.ID;
                                    int No = issueSlipHeader.IssueSlipOrders.OrderByDescending(m => m.No).Select(m => m.No).FirstOrDefault();
                                    foreach (DataRow row in dt.AsEnumerable().Skip(9))
                                    {
                                        string itemCode = row[1].ToString();
                                        decimal Qty = string.IsNullOrEmpty(row[5].ToString()) ? 0 : decimal.Parse(row[5].ToString());
                                        if (!string.IsNullOrEmpty(itemCode))
                                        {
                                            //loop check if material doesnt exist, throw exception
                                            vProductMaster productMaster = db.vProductMasters.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                            if (productMaster == null)
                                            {
                                                throw new Exception(string.Format("Upload failed, Material Code {0} does not exist.", row[1].ToString()));
                                            }
                                            //loop check if material doesnt exist, throw exception
                                            IssueSlipOrder order = issueSlipHeader.IssueSlipOrders.Where(m => m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                            if (order == null)
                                            {
                                                order = new IssueSlipOrder()
                                                {
                                                    ID = Helper.CreateGuid("ISO"),
                                                    No = No,
                                                    HeaderID = TransactionId,
                                                    MaterialCode = productMaster.MaterialCode,
                                                    MaterialName = productMaster.MaterialName,
                                                    QtyPerBag = productMaster.QtyPerBag,
                                                    VendorName = row[3].ToString(),
                                                    UoM = "KG",
                                                    Qty = Qty
                                                };
                                                db.IssueSlipOrders.Add(order);
                                                No++;
                                            }
                                            else
                                            {
                                                //check quantity still available or not
                                                vIssueSlipPicked picked = db.vIssueSlipPickeds.Where(m => m.HeaderID.Equals(TransactionId) && m.MaterialCode.Equals(itemCode)).FirstOrDefault();
                                                if (picked == null)
                                                {
                                                    order.Qty = Qty;
                                                }
                                                else
                                                {
                                                    if (Qty > picked.TotalQty)
                                                    {
                                                        order.Qty = Qty;
                                                    }
                                                    else
                                                    {
                                                        //add error message
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    await db.SaveChangesAsync();
                                    message = "Upload succeeded.";
                                    status = true;
                                }
                                catch (Exception e)
                                {
                                    message = string.Format("Upload item failed. {0}", e.Message);
                                }
                            }
                            else
                            {
                                message = "Upload failed. Maximum allowed file size : 10MB ";
                            }
                        }
                        else
                        {
                            message = "Upload item failed. File is invalid.";
                        }
                    }
                    else
                    {
                        message = "No file uploaded.";
                    }
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("ID", TransactionId);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        public async Task<IHttpActionResult> UpdateStatus(string id, string transactionStatus)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    IssueSlipHeader header = await db.IssueSlipHeaders.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync();

                    if (transactionStatus.Equals("CLOSED"))
                    {
                        if (!header.TransactionStatus.Equals("PROGRESS"))
                        {
                            throw new Exception("Transaction can not be closed.");
                        }

                        message = "Data closing succeeded.";
                    }

                    if (transactionStatus.Equals("CANCELLED"))
                    {
                        if (!header.TransactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Transaction can not be cancelled.");
                        }

                        db.IssueSlipOrders.RemoveRange(header.IssueSlipOrders);

                        message = "Data cancellation succeeded.";
                    }

                    if (header.TransactionStatus.Equals("CLOSED"))
                    {
                        if (!transactionStatus.Equals("OPEN"))
                        {
                            throw new Exception("Transaction can not be opened.");
                        }
                    }

                    header.TransactionStatus = transactionStatus;
                    header.ModifiedBy = activeUser;
                    header.ModifiedOn = DateTime.Now;

                    await db.SaveChangesAsync();
                    status = true;
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("status", status);
            obj.Add("message", message);
            return Ok(obj);
        }

        public async Task<IHttpActionResult> Cancel(string id)
        {
            return await UpdateStatus(id, "CANCELLED");
        }

        //public async Task<IHttpActionResult> Confirm(string id)
        //{
        //    return await UpdateStatus(id, "CONFIRMED");
        //}

        public async Task<IHttpActionResult> Close(string id)
        {
            return await UpdateStatus(id, "CLOSED");
        }

        public async Task<IHttpActionResult> Open(string id)
        {
            return await UpdateStatus(id, "OPEN");
        }


        [HttpGet]
        public async Task<IHttpActionResult> GetFifo()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string OrderId = request["orderId"].ToString();

            if (string.IsNullOrEmpty(OrderId))
            {
                throw new Exception("Order Id is required.");
            }

            IssueSlipOrder order = db.IssueSlipOrders.Where(m => m.ID.Equals(OrderId)).FirstOrDefault();
            //string warehouseCode = request["warehouseCode"].ToString();
            //string areaCode = request["areaCode"].ToString();

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<FifoStockDTO> data = new List<FifoStockDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(order.MaterialCode) && s.Quantity > 0 && !s.OnInspect).AsQueryable();
            List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
            query = query.Where(a => warehouses.Contains(a.WarehouseCode));

            int totalRow = query.Count();

            decimal requestedQty = order.Qty;
            decimal pickedQty = order.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
            decimal availableQty = requestedQty - pickedQty;

            try
            {
                query = query.OrderByDescending(s => s.BinRackAreaType)
                .ThenByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                .ThenBy(s => s.InDate)
                .ThenBy(s => s.QtyPerBag)
                .ThenBy(s => s.Quantity);
                list = query.ToList();
                //find outstanding quantity
                //do looping until quantity reach
                if (list != null && list.Count() > 0)
                {
                    decimal searchQty = 0;

                    foreach (vStockAll stock in list)
                    {
                        if (searchQty <= availableQty)
                        {
                            if (DateTime.Now.Date < stock.ExpiredDate.Value.Date)
                            {
                                searchQty += stock.Quantity;
                            }

                            FifoStockDTO dat = new FifoStockDTO
                            {
                                ID = stock.ID,
                                StockCode = stock.Code,
                                LotNo = stock.LotNumber,
                                BinRackCode = stock.BinRackCode,
                                BinRackName = stock.BinRackName,
                                BinRackAreaCode = stock.BinRackAreaCode,
                                BinRackAreaName = stock.BinRackAreaName,
                                WarehouseCode = stock.WarehouseCode,
                                WarehouseName = stock.WarehouseName,
                                MaterialCode = stock.MaterialCode,
                                MaterialName = stock.MaterialName,
                                InDate = Helper.NullDateToString(stock.InDate),
                                ExpDate = Helper.NullDateToString(stock.ExpiredDate),
                                BagQty = Helper.FormatThousand(Convert.ToInt32(stock.BagQty)),
                                QtyPerBag = Helper.FormatThousand(stock.QtyPerBag),
                                TotalQty = Helper.FormatThousand(stock.Quantity),
                                IsExpired = DateTime.Now.Date >= stock.ExpiredDate.Value.Date,
                                QCInspected = stock.OnInspect
                            };

                            data.Add(dat);
                        }
                        else
                        {
                            break;
                        }

                    }
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("totalRow", totalRow);
            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Picking(IssueSlipPickingVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    IssueSlipOrder issueSlipOrder = null;
                    vStockAll stockAll = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (issueSlipOrder == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }


                    if (!issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Issue Slip is not open for edit.");
                    }


                    if (string.IsNullOrEmpty(dataVM.StockID))
                    {
                        throw new Exception("Stock is required.");
                    }

                    //check stock quantity
                    List<string> warehouses = db.Warehouses.Where(x => x.Type.Equals("EMIX")).Select(d => d.Code).ToList();
                    stockAll = db.vStockAlls.Where(m => m.ID.Equals(dataVM.StockID) && warehouses.Contains(m.WarehouseCode)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock is not recognized.");
                    }

                    //restriction 1 : AREA TYPE

                    User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
                    string userAreaType = userData.AreaType;

                    string materialAreaType = stockAll.BinRackAreaType;

                    if (!userAreaType.Equals(materialAreaType))
                    {
                        throw new Exception(string.Format("FIFO Restriction, do not allowed to pick material in area {0}", materialAreaType));
                    }

                    vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(issueSlipOrder.MaterialCode) && s.Quantity > 0 && !s.OnInspect && s.BinRackAreaType.Equals(userAreaType) && warehouses.Contains(s.WarehouseCode))
                       .OrderByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
                       .ThenBy(s => s.InDate)
                       .ThenBy(s => s.QtyPerBag).FirstOrDefault();
                    //.ThenBy(s => s.Quantity).FirstOrDefault();

                    if (stkAll == null)
                    {
                        throw new Exception("Stock is not available.");
                    }

                    //restriction 2 : REMAINDER QTY

                    if (stockAll.QtyPerBag > stkAll.QtyPerBag)
                    {
                        throw new Exception(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
                    }

                    //restriction 3 : IN DATE

                    if (stockAll.InDate.Value.Date > stkAll.InDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, must pick item with following detail = LotNo : {0} & In Date: {1}", stkAll.LotNumber, Helper.NullDateToString(stkAll.InDate)));
                    }

                    //restriction 4 : EXPIRED DATE

                    if (DateTime.Now.Date >= stkAll.ExpiredDate.Value.Date)
                    {
                        throw new Exception(string.Format("FIFO Restriction, must execute QC Inspection for material with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
                    }

                    if (dataVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("IssueSlip.BagQty", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        int bagQty = Convert.ToInt32(stockAll.Quantity / stockAll.QtyPerBag);

                        if (dataVM.BagQty > bagQty)
                        {
                            ModelState.AddModelError("Movement.BagQty", string.Format("Bag Qty exceeded. Bag Qty : {0}", bagQty));
                        }
                        else
                        {
                            decimal requestedQty = issueSlipOrder.Qty;
                            decimal pickedQty = issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
                            decimal availableQty = requestedQty - pickedQty;
                            int availableBagQty = Convert.ToInt32(Math.Ceiling(availableQty / issueSlipOrder.QtyPerBag));

                            if (dataVM.BagQty > availableBagQty)
                            {
                                ModelState.AddModelError("IssueSlip.BagQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
                            }
                        }
                    }


                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    BinRack binRack = db.BinRacks.Where(m => m.Code.Equals(stockAll.BinRackCode)).FirstOrDefault();

                    IssueSlipPicking picking = new IssueSlipPicking();
                    picking.ID = Helper.CreateGuid("P");
                    picking.IssueSlipOrderID = issueSlipOrder.ID;
                    picking.PickingMethod = "MANUAL";
                    picking.PickedOn = DateTime.Now;
                    picking.PickedBy = activeUser;
                    picking.BinRackID = binRack.ID;
                    picking.BinRackCode = stockAll.BinRackCode;
                    picking.BinRackName = stockAll.BinRackName;
                    picking.BagQty = dataVM.BagQty;
                    picking.QtyPerBag = stockAll.QtyPerBag;
                    picking.StockCode = stockAll.Code;
                    picking.LotNo = stockAll.LotNumber;
                    picking.InDate = stockAll.InDate.Value;
                    picking.ExpDate = stockAll.ExpiredDate.Value;
                    picking.UoM = "KG";

                    db.IssueSlipPickings.Add(picking);

                    //reduce stock

                    if (stockAll.Type.Equals("RM"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        decimal pickQty = picking.BagQty * picking.QtyPerBag;
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= pickQty;
                    }

                    IssueSlipHeader header = db.IssueSlipHeaders.Where(m => m.ID.Equals(issueSlipOrder.HeaderID)).FirstOrDefault();
                    header.TransactionStatus = "PROGRESS";


                    await db.SaveChangesAsync();

                    issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    IssueSlipOrderDTO order = new IssueSlipOrderDTO
                    {
                        ID = issueSlipOrder.ID,
                        Number = issueSlipOrder.No.ToString(),
                        MaterialCode = issueSlipOrder.MaterialCode,
                        MaterialName = issueSlipOrder.MaterialName,
                        VendorName = issueSlipOrder.VendorName,
                        RequestedQty = Helper.FormatThousand(issueSlipOrder.Qty),
                        QtyPerBag = Helper.FormatThousand(issueSlipOrder.QtyPerBag),
                        PickedQty = Helper.FormatThousand(issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag)),
                        ReturnedQty = Helper.FormatThousand(issueSlipOrder.IssueSlipReturns.Sum(i => i.ReturnQty * i.QtyPerBag)),
                        OutstandingQty = Helper.FormatThousand(issueSlipOrder.Qty - (issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))),
                        PickingBagQty = Helper.FormatThousand(Convert.ToInt32(Math.Ceiling((issueSlipOrder.Qty - (issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag))) / issueSlipOrder.QtyPerBag))),
                        DiffQty = Helper.FormatThousand(issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag) - issueSlipOrder.Qty)
                    };

                    obj.Add("data", order);

                    status = true;
                    message = "Picking succeeded.";
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePickingSummary(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<vIssueSlipPickingSummary> list = Enumerable.Empty<vIssueSlipPickingSummary>();
            IEnumerable<IssueSlipPickingDTO> pagedData = Enumerable.Empty<IssueSlipPickingDTO>();

            IQueryable<vIssueSlipPickingSummary> query = db.vIssueSlipPickingSummaries.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            IssueSlipPicking picking = db.IssueSlipPickings.Where(s => s.ID.Equals(HeaderID)).FirstOrDefault();

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.LotNo.Contains(search)
                        );

                Dictionary<string, Func<vIssueSlipPickingSummary, object>> cols = new Dictionary<string, Func<vIssueSlipPickingSummary, object>>();
                cols.Add("RowNum", x => x.RowNum);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => Convert.ToInt32(x.TotalQty / x.QtyPerBag));
                cols.Add("ReturnQty", x => x.ReturnQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipPickingDTO
                                {
                                    RowNum = x.RowNum.ToString(),
                                    OrderID = x.ID,
                                    StockCode = x.StockCode,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.TotalQty / x.QtyPerBag)),
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    ReturnedTotalQty = Helper.FormatThousand(x.ReturnQty),
                                    AvailableReturnQty = Helper.FormatThousand(x.TotalQty - x.ReturnQty)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePicking(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<IssueSlipPicking> list = Enumerable.Empty<IssueSlipPicking>();
            IEnumerable<IssueSlipPickingDTO> pagedData = Enumerable.Empty<IssueSlipPickingDTO>();

            IQueryable<IssueSlipPicking> query = db.IssueSlipPickings.Where(s => s.IssueSlipOrder.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.IssueSlipOrder.MaterialCode.Contains(search)
                        || m.IssueSlipOrder.MaterialName.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<IssueSlipPicking, object>> cols = new Dictionary<string, Func<IssueSlipPicking, object>>();
                cols.Add("MaterialCode", x => x.IssueSlipOrder.MaterialCode);
                cols.Add("MaterialName", x => x.IssueSlipOrder.MaterialName);
                cols.Add("StockCode", x => x.StockCode);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("BagQty", x => x.BagQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("PickingMethod", x => x.PickingMethod);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("PickedOn", x => x.PickedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipPickingDTO
                                {
                                    ID = x.ID,
                                    MaterialCode = x.IssueSlipOrder.MaterialCode,
                                    MaterialName = x.IssueSlipOrder.MaterialName,
                                    StockCode = x.StockCode,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    BagQty = Helper.FormatThousand(x.BagQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.BagQty * x.QtyPerBag),
                                    UoM = x.UoM,
                                    PickingMethod = x.PickingMethod,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    PickedBy = x.PickedBy,
                                    PickedOn = Helper.NullDateTimeToString(x.PickedOn)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        //[HttpPost]
        //public async Task<IHttpActionResult> PickingMobile(IssueSlipPickingVM dataVM)
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
        //            IssueSlipOrder issueSlipOrder = null;
        //            vStockAll stockAll = null;

        //            if (string.IsNullOrEmpty(dataVM.OrderID))
        //            {
        //                throw new Exception("Order Id is required.");
        //            }

        //            issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

        //            if (issueSlipOrder == null)
        //            {
        //                throw new Exception("Order is not recognized.");
        //            }


        //            if (!issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
        //            {
        //                throw new Exception("Issue Slip is not open for edit.");
        //            }


        //            if (string.IsNullOrEmpty(dataVM.StockID))
        //            {
        //                throw new Exception("Stock is required.");
        //            }

        //            //check stock quantity
        //            stockAll = db.vStockAlls.Where(m => m.ID.Equals(dataVM.StockID)).FirstOrDefault();
        //            if (stockAll == null)
        //            {
        //                throw new Exception("Stock is not recognized.");
        //            }


        //            vStockAll stkAll = db.vStockAlls.Where(s => s.MaterialCode.Equals(issueSlipOrder.MaterialCode) && s.Quantity > 0)
        //                .OrderByDescending(s => s.BinRackAreaType)
        //                .ThenByDescending(s => DbFunctions.TruncateTime(DateTime.Now) >= DbFunctions.TruncateTime(s.ExpiredDate))
        //                .ThenBy(s => s.InDate)
        //                .ThenBy(s => s.QtyPerBag)
        //                .ThenBy(s => s.Quantity).FirstOrDefault();
        //            if (stkAll == null)
        //            {
        //                throw new Exception("Stock is not available.");
        //            }

        //            //restriction 1 : check qty remainder

        //            //if (stockAll.QtyPerBag > stkAll.QtyPerBag)
        //            //{
        //            //    throw new Exception(string.Format("FIFO Restriction, you must pick item with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
        //            //}

        //            //restriction 2 : check expired

        //            if (DateTime.Now.Date >= stkAll.ExpiredDate.Date)
        //            {
        //                throw new Exception(string.Format("FIFO Restriction, you must execute QC Inspection for material with following detail = LotNo : {0} & Qty/Bag : {1}", stkAll.LotNumber, Helper.FormatThousand(stkAll.QtyPerBag)));
        //            }

        //            if (dataVM.BagQty <= 0)
        //            {
        //                ModelState.AddModelError("IssueSlip.BagQty", "Bag Qty can not be empty or below zero.");
        //            }
        //            else
        //            {
        //                int bagQty = Convert.ToInt32(stockAll.Quantity / stockAll.QtyPerBag);

        //                if (dataVM.BagQty > bagQty)
        //                {
        //                    ModelState.AddModelError("IssueSlip.BagQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", bagQty));
        //                }
        //                else
        //                {
        //                    decimal requestedQty = issueSlipOrder.Qty;
        //                    decimal pickedQty = issueSlipOrder.IssueSlipPickings.Sum(i => i.BagQty * i.QtyPerBag);
        //                    decimal availableQty = requestedQty - pickedQty;
        //                    int availableBagQty = Convert.ToInt32(Math.Ceiling(availableQty / issueSlipOrder.QtyPerBag));

        //                    if (dataVM.BagQty > availableBagQty)
        //                    {
        //                        ModelState.AddModelError("IssueSlip.BagQty", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
        //                    }
        //                }



        //            }


        //            if (!ModelState.IsValid)
        //            {
        //                foreach (var state in ModelState)
        //                {
        //                    string field = state.Key.Split('.')[1];
        //                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        //                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        //                }

        //                throw new Exception("Input is not valid");
        //            }

        //            BinRack binRack = db.BinRacks.Where(m => m.Code.Equals(stockAll.BinRackCode)).FirstOrDefault();

        //            IssueSlipPicking picking = new IssueSlipPicking();
        //            picking.ID = Helper.CreateGuid("P");
        //            picking.IssueSlipOrderID = issueSlipOrder.ID;
        //            picking.PickingMethod = "MANUAL";
        //            picking.PickedOn = DateTime.Now;
        //            picking.PickedBy = activeUser;
        //            picking.BinRackID = binRack.ID;
        //            picking.BinRackCode = stockAll.BinRackCode;
        //            picking.BinRackName = stockAll.BinRackName;
        //            picking.BagQty = dataVM.BagQty;
        //            picking.QtyPerBag = stockAll.QtyPerBag;
        //            picking.StockCode = stockAll.Code;
        //            picking.LotNo = stockAll.LotNumber;
        //            picking.InDate = stockAll.InDate;
        //            picking.UoM = "KG";

        //            db.IssueSlipPickings.Add(picking);

        //            //reduce stock

        //            if (stockAll.Type.Equals("RM"))
        //            {
        //                decimal pickQty = picking.BagQty * picking.QtyPerBag;
        //                StockRM stock = db.StockRMs.Where(m => m.Code.Equals(stockAll.Code)).FirstOrDefault();
        //                stock.Quantity -= pickQty;
        //            }
        //            else if (stockAll.Type.Equals("SFG"))
        //            {
        //                decimal pickQty = picking.BagQty * picking.QtyPerBag;
        //                StockSFG stock = db.StockSFGs.Where(m => m.Code.Equals(stockAll.Code)).FirstOrDefault();
        //                stock.Quantity -= pickQty;
        //            }

        //            IssueSlipHeader header = db.IssueSlipHeaders.Where(m => m.ID.Equals(issueSlipOrder.HeaderID)).FirstOrDefault();
        //            header.TransactionStatus = "PROGRESS";


        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Picking succeeded.";

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

        [HttpPost]
        public async Task<IHttpActionResult> DatatableReturnSummary(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<vIssueSlipReturnSummary> list = Enumerable.Empty<vIssueSlipReturnSummary>();
            IEnumerable<IssueSlipReturnDTO> pagedData = Enumerable.Empty<IssueSlipReturnDTO>();

            IQueryable<vIssueSlipReturnSummary> query = db.vIssueSlipReturnSummaries.Where(s => s.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.MaterialCode.Contains(search)
                        || m.MaterialName.Contains(search)
                        || m.LotNo.Contains(search)
                        );

                Dictionary<string, Func<vIssueSlipReturnSummary, object>> cols = new Dictionary<string, Func<vIssueSlipReturnSummary, object>>();
                cols.Add("RowNum", x => x.RowNum);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("TotalQty", x => x.TotalQty);
                cols.Add("PutawayQty", x => x.PutawayQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.TotalQty / x.QtyPerBag);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipReturnDTO
                                {
                                    RowNum = x.RowNum.ToString(),
                                    OrderID = x.ID,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName,
                                    LotNo = x.LotNo,
                                    StockCode = x.StockCode,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    TotalQty = Helper.FormatThousand(x.TotalQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.TotalQty / x.QtyPerBag)),
                                    TotalPutawayQty = Helper.FormatThousand(x.PutawayQty),
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> DatatableReturn(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<IssueSlipReturn> list = Enumerable.Empty<IssueSlipReturn>();
            IEnumerable<IssueSlipReturnDTO> pagedData = Enumerable.Empty<IssueSlipReturnDTO>();

            IQueryable<IssueSlipReturn> query = db.IssueSlipReturns.Where(s => s.IssueSlipOrder.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.IssueSlipOrder.MaterialCode.Contains(search)
                        || m.IssueSlipOrder.MaterialName.Contains(search)
                        || m.LotNo.Contains(search)
                        );

                Dictionary<string, Func<IssueSlipReturn, object>> cols = new Dictionary<string, Func<IssueSlipReturn, object>>();
                cols.Add("MaterialCode", x => x.IssueSlipOrder.MaterialCode);
                cols.Add("MaterialName", x => x.IssueSlipOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.ReturnQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.ReturnQty / x.QtyPerBag);
                cols.Add("ReturnMethod", x => x.ReturnMethod);
                cols.Add("ReturnedBy", x => x.ReturnedBy);
                cols.Add("ReturnedOn", x => x.ReturnedOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipReturnDTO
                                {
                                    MaterialCode = x.IssueSlipOrder.MaterialCode,
                                    MaterialName = x.IssueSlipOrder.MaterialName,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.ReturnQty / x.QtyPerBag)),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.ReturnQty),
                                    ReturnMethod = x.ReturnMethod,
                                    ReturnedBy = x.ReturnedBy,
                                    ReturnedOn = Helper.NullDateTimeToString(x.ReturnedOn)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Return(IssueSlipReturnVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vIssueSlipPickingSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    summary = await db.vIssueSlipPickingSummaries.Where(s => s.ID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Item is not recognized.");
                    }

                    IssueSlipOrder issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (issueSlipOrder == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Return not allowed.");
                    }


                    if (dataVM.Qty <= 0)
                    {
                        ModelState.AddModelError("IssueSlip.ReturnQty", "Return Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.ReturnQty.Value;

                        if (dataVM.Qty > availableQty)
                        {
                            ModelState.AddModelError("IssueSlip.ReturnQty", string.Format("Return Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableQty)));
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    //fullbag
                    int totalFullBag = Convert.ToInt32(Math.Floor(dataVM.Qty / summary.QtyPerBag));
                    decimal totalQty = totalFullBag * summary.QtyPerBag;


                    IssueSlipReturn ret = new IssueSlipReturn();

                    if (totalFullBag > 0)
                    {
                        ret.ID = Helper.CreateGuid("R");
                        ret.IssueSlipOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = totalQty;
                        ret.StockCode = summary.StockCode;
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.QtyPerBag = summary.QtyPerBag;
                        ret.PrevStockCode = summary.StockCode;
                        db.IssueSlipReturns.Add(ret);
                    }




                    //remainder
                    decimal remainderQty = dataVM.Qty - totalQty;
                    if (remainderQty > 0)
                    {
                        ret = new IssueSlipReturn();
                        ret.ID = Helper.CreateGuid("R");
                        ret.IssueSlipOrderID = summary.ID;
                        ret.ReturnMethod = "MANUAL";
                        ret.ReturnedOn = DateTime.Now;
                        ret.ReturnedBy = activeUser;
                        ret.ReturnQty = remainderQty;
                        ret.QtyPerBag = remainderQty;
                        //create new stock code
                        ret.StockCode = string.Format("{0}{1}{2}{3}{4}", summary.MaterialCode, Helper.FormatThousand(remainderQty), summary.LotNo, summary.InDate.ToString("yyyyMMdd").Substring(1), summary.ExpDate.ToString("yyyyMMdd").Substring(2));
                        ret.LotNo = summary.LotNo;
                        ret.InDate = summary.InDate;
                        ret.ExpDate = summary.ExpDate;
                        ret.PrevStockCode = summary.StockCode;

                        //log print RM
                        //check lastSeries in Log/intRM based on Mat  //check lastSeries in LogPrintRM based on StockCode/ MaterialCode, LotNo, InDate, ExpDate
                        int startSeries = 0;
                        int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(ret.StockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries == 0)
                        {
                            startSeries = 1;
                        }
                        else
                        {
                            startSeries = lastSeries + 1;
                        }

                        lastSeries = startSeries + (Convert.ToInt32(remainderQty / remainderQty));

                        ret.LastSeries = lastSeries;

                        db.IssueSlipReturns.Add(ret);
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Return succeeded.";
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> EditReturn(IssueSlipReturnVM2 dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vIssueSlipPickingSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    IssueSlipOrder issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (issueSlipOrder == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Return not allowed.");
                    }

                    if (dataVM.Qty <= 0)
                    {
                        ModelState.AddModelError("IssueSlip.NewReturnQty", "Return Qty can not be empty or below zero.");
                    }

                    int totalFullBag = Convert.ToInt32(Math.Floor(dataVM.Qty / issueSlipOrder.QtyPerBag));
                    if (totalFullBag > 0)
                    {
                        ModelState.AddModelError("IssueSlip.NewReturnQty", string.Format("Edit return qty must be below : {0}", Helper.FormatThousand(issueSlipOrder.QtyPerBag))); 
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    // Hitung panjang total string
                    int totalLength = dataVM.StockCode.Length;

                    // Indeks untuk karakter ke-7 dari belakang
                    int startIndex = totalLength - 12; // Karakter ke-7 dari belakang = 12 karakter dari belakang

                    // Ambil substring dari indeks ke-7 hingga ke-12 dari belakang
                    string indate = dataVM.StockCode.Substring(startIndex, 6);
                    string expdate = dataVM.StockCode.Substring(dataVM.StockCode.Length - 6);

                    IssueSlipReturn returncek = new IssueSlipReturn();
                    returncek = await db.IssueSlipReturns.Where(s => s.IssueSlipOrderID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (returncek != null)
                    {
                        returncek.StockCode = string.Format("{0}{1}{2}{3}{4}", issueSlipOrder.MaterialCode, Helper.FormatThousand(dataVM.Qty), dataVM.LotNo, indate, expdate);
                        returncek.QtyPerBag = dataVM.Qty;
                        returncek.ReturnQty = dataVM.Qty;
                    }
                    await db.SaveChangesAsync();

                    status = true;
                    message = "Edit return succeeded.";
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePutaway(string HeaderID)
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<IssueSlipPutaway> list = Enumerable.Empty<IssueSlipPutaway>();
            IEnumerable<IssueSlipPutawayDTO> pagedData = Enumerable.Empty<IssueSlipPutawayDTO>();

            IQueryable<IssueSlipPutaway> query = db.IssueSlipPutaways.Where(s => s.IssueSlipOrder.HeaderID.Equals(HeaderID)).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.IssueSlipOrder.MaterialCode.Contains(search)
                        || m.IssueSlipOrder.MaterialName.Contains(search)
                        || m.LotNo.Contains(search)
                        );

                Dictionary<string, Func<IssueSlipPutaway, object>> cols = new Dictionary<string, Func<IssueSlipPutaway, object>>();
                cols.Add("MaterialCode", x => x.IssueSlipOrder.MaterialCode);
                cols.Add("MaterialName", x => x.IssueSlipOrder.MaterialName);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("PutawayQty", x => x.PutawayQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.PutawayQty / x.QtyPerBag);
                cols.Add("PutawayMethod", x => x.PutawayMethod);
                cols.Add("PutBy", x => x.PutBy);
                cols.Add("PutOn", x => x.PutOn);
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new IssueSlipPutawayDTO
                                {
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    MaterialCode = x.IssueSlipOrder.MaterialCode,
                                    MaterialName = x.IssueSlipOrder.MaterialName,
                                    LotNo = x.LotNo,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpDate),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.PutawayQty / x.QtyPerBag)),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    TotalQty = Helper.FormatThousand(x.PutawayQty),
                                    PutawayMethod = x.PutawayMethod,
                                    PutBy = x.PutBy,
                                    PutOn = Helper.NullDateTimeToString(x.PutOn)
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Putaway(PutawayReturnVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

            string message = "";
            bool status = false;
            var re = Request;
            var headers = re.Headers;

            try
            {
                string token = "";

                if (headers.Contains("token"))
                {
                    token = headers.GetValues("token").First();
                }

                string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    vIssueSlipReturnSummary summary = null;

                    if (string.IsNullOrEmpty(dataVM.OrderID))
                    {
                        throw new Exception("Order Id is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock Code is required.");
                    }

                    summary = await db.vIssueSlipReturnSummaries.Where(s => s.ID.Equals(dataVM.OrderID) && s.StockCode.Equals(dataVM.StockCode)).FirstOrDefaultAsync();

                    if (summary == null)
                    {
                        throw new Exception("Item is not recognized.");
                    }

                    IssueSlipOrder issueSlipOrder = await db.IssueSlipOrders.Where(s => s.ID.Equals(dataVM.OrderID)).FirstOrDefaultAsync();

                    if (issueSlipOrder == null)
                    {
                        throw new Exception("Order is not recognized.");
                    }

                    if (!issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("OPEN") && !issueSlipOrder.IssueSlipHeader.TransactionStatus.Equals("PROGRESS"))
                    {
                        throw new Exception("Return not allowed.");
                    }


                    if (dataVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("IssueSlip.PutawayQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal availableQty = summary.TotalQty.Value - summary.PutawayQty.Value;
                        int availableBagQty = Convert.ToInt32(availableQty / summary.QtyPerBag);

                        if (dataVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("IssueSlip.PutawayQTY", string.Format("Bag Qty exceeded. Available Qty : {0}", Helper.FormatThousand(availableBagQty)));
                        }
                    }

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(dataVM.BinRackID))
                    {
                        ModelState.AddModelError("IssueSlip.BinRackID", "BinRack is required.");
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.ID.Equals(dataVM.BinRackID)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("IssueSlip.BinRackID", "BinRack is not recognized.");
                        }

                        vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(summary.MaterialCode)).FirstOrDefaultAsync();
                        if (vProductMaster == null)
                        {
                            throw new Exception("Material tidak dikenali.");
                        }

                        BinRackArea binRackArea = await db.BinRackAreas.Where(m => m.ID.Equals(binRack.BinRackAreaID)).FirstOrDefaultAsync();
                        if (binRackArea == null)
                        {
                            ModelState.AddModelError("IssueSlip.BinRackID", "BinRackArea tidak ditemukan.");
                        }
                        if (binRackArea.Type == "PRODUCTION" && summary.PutawayQty >= vProductMaster.QtyPerBag)
                        {
                            ModelState.AddModelError("IssueSlip.BinRackID", "Quantity full bag harus dikembalikan ke warehouse.");
                        }

                        vStockAll cekmaterial = await db.vStockAlls.Where(m => m.MaterialCode.Equals(summary.MaterialCode) && m.Quantity > 0 && !m.OnInspect && m.QtyPerBag < vProductMaster.QtyPerBag && m.BinRackAreaType == "PRODUCTION").FirstOrDefaultAsync();
                        if (cekmaterial != null)
                        {
                            if (binRackArea.Type == "LOGISTIC" && summary.PutawayQty < vProductMaster.QtyPerBag)
                            {
                                ModelState.AddModelError("IssueSlip.BinRackID", "Quantity remaining harus dikembalikan ke production.");
                            }
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        foreach (var state in ModelState)
                        {
                            string field = state.Key.Split('.')[1];
                            string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
                            customValidationMessages.Add(new CustomValidationMessage(field, value));
                        }

                        throw new Exception("Input is not valid");
                    }

                    IssueSlipPutaway putaway = new IssueSlipPutaway();
                    putaway.ID = Helper.CreateGuid("P");
                    putaway.IssueSlipOrderID = issueSlipOrder.ID;
                    putaway.PutawayMethod = "MANUAL";
                    putaway.LotNo = summary.LotNo;
                    putaway.InDate = summary.InDate;
                    putaway.ExpDate = summary.ExpDate;
                    putaway.QtyPerBag = summary.QtyPerBag;
                    putaway.StockCode = summary.StockCode;
                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    putaway.PutOn = transactionDate;
                    putaway.PutBy = activeUser;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = dataVM.BagQty * summary.QtyPerBag;

                    db.IssueSlipPutaways.Add(putaway);


                    vProductMaster productMaster = db.vProductMasters.Where(m => m.MaterialCode.Equals(issueSlipOrder.MaterialCode)).FirstOrDefault();
                    if (productMaster.ProdType.Equals("RM"))
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = issueSlipOrder.MaterialCode;
                            stockRM.MaterialName = issueSlipOrder.MaterialName;
                            stockRM.Code = summary.StockCode;
                            stockRM.LotNumber = summary.LotNo;
                            stockRM.InDate = summary.InDate;
                            stockRM.ExpiredDate = summary.ExpDate;
                            stockRM.Quantity = putaway.PutawayQty;
                            stockRM.QtyPerBag = summary.QtyPerBag;
                            stockRM.BinRackID = putaway.BinRackID;
                            stockRM.BinRackCode = putaway.BinRackCode;
                            stockRM.BinRackName = putaway.BinRackName;
                            stockRM.ReceivedAt = putaway.PutOn;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(summary.StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += putaway.PutawayQty;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = issueSlipOrder.MaterialCode;
                            stockSFG.MaterialName = issueSlipOrder.MaterialName;
                            stockSFG.Code = summary.StockCode;
                            stockSFG.LotNumber = summary.LotNo;
                            stockSFG.InDate = summary.InDate;
                            stockSFG.ExpiredDate = summary.ExpDate;
                            stockSFG.Quantity = putaway.PutawayQty;
                            stockSFG.QtyPerBag = summary.QtyPerBag;
                            stockSFG.BinRackID = putaway.BinRackID;
                            stockSFG.BinRackCode = putaway.BinRackCode;
                            stockSFG.BinRackName = putaway.BinRackName;
                            stockSFG.ReceivedAt = putaway.PutOn;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway succeeded.";
                }
                else
                {
                    message = "Token is no longer valid. Please re-login.";
                }
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("status", status);
            obj.Add("message", message);
            obj.Add("error_validation", customValidationMessages);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableIssueSlip()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string date = request["date"].ToString();
            string enddate = request["enddate"].ToString();

            IEnumerable<vIssueSlipReport> list = Enumerable.Empty<vIssueSlipReport>();
            IEnumerable<IssueSlipDTOReport> pagedData = Enumerable.Empty<IssueSlipDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vIssueSlipReport> query;

            query = db.vIssueSlipReports.Where(s => DbFunctions.TruncateTime(s.Header_ProductionDate) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.Header_ProductionDate) <= DbFunctions.TruncateTime(endfilterDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.RM_Code.Contains(search)
                        || m.RM_Name.Contains(search)
                        || m.RM_VendorName.Contains(search)
                        || m.FromBinRackCode.Contains(search)
                        || m.PickedBy.Contains(search)
                        //|| m.ToBinRackCode.Contains(search)
                        //|| m.PutBy.Contains(search)
                        );

                Dictionary<string, Func<vIssueSlipReport, object>> cols = new Dictionary<string, Func<vIssueSlipReport, object>>();
                cols.Add("RM_Code", x => x.RM_Code);
                cols.Add("RM_Name", x => x.RM_Name);
                cols.Add("RM_VendorName", x => x.RM_VendorName);
                cols.Add("Wt_Request", x => x.Wt_Request);
                cols.Add("SupplyQty", x => x.SupplyQty);
                cols.Add("FromBinRackCode", x => x.FromBinRackCode);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("ReturnQty", x => x.ReturnQty);
                cols.Add("ToBinRackCode", x => x.ToBinRackCode);
                cols.Add("PutBy", x => x.PutBy);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new IssueSlipDTOReport
                                {
                                    ID = detail.ID,
                                    ID_Order = detail.ID_Order,
                                    ID_Header = detail.ID_Header,
                                    Header_Code = detail.Header_Code,
                                    Header_Name = detail.Header_Name,
                                    Header_ProductionDate = Helper.NullDateToString2(detail.Header_ProductionDate),
                                    RM_Code = detail.RM_Code,
                                    RM_Name = detail.RM_Name,
                                    RM_VendorName = detail.RM_VendorName,
                                    Wt_Request = Helper.FormatThousand(detail.Wt_Request),
                                    SupplyQty = Helper.FormatThousand(detail.SupplyQty),
                                    FromBinRackCode = detail.FromBinRackCode != null ? detail.FromBinRackCode : "",
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    PickedBy = detail.PickedBy != null ? detail.PickedBy : "",
                                    ReturnQty = Helper.FormatThousand(detail.ReturnQty),
                                    ToBinRackCode = detail.ToBinRackCode != null ? detail.ToBinRackCode : "",
                                    PutBy = detail.PutBy != null ? detail.PutBy : "",
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableHistoryIssueSlip()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string id = request["id"].ToString();

            IEnumerable<vIssueSlipReport> list = Enumerable.Empty<vIssueSlipReport>();
            IEnumerable<IssueSlipDTOReport> pagedData = Enumerable.Empty<IssueSlipDTOReport>();

            IQueryable<vIssueSlipReport> query;

            query = db.vIssueSlipReports.Where(s => s.ID.Equals(id));

            try
            {
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new IssueSlipDTOReport
                                {
                                    ID = detail.ID,
                                    ID_Order = detail.ID_Order,
                                    ID_Header = detail.ID_Header,
                                    Header_Code = detail.Header_Code,
                                    Header_Name = detail.Header_Name,
                                    Header_ProductionDate = Helper.NullDateToString2(detail.Header_ProductionDate),
                                    RM_Code = detail.RM_Code,
                                    RM_Name = detail.RM_Name,
                                    RM_VendorName = detail.RM_VendorName,
                                    Wt_Request = Helper.FormatThousand(detail.Wt_Request),
                                    SupplyQty = Helper.FormatThousand(detail.SupplyQty),
                                    FromBinRackCode = detail.FromBinRackCode != null ? detail.FromBinRackCode : "",
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    PickedBy = detail.PickedBy != null ? detail.PickedBy : "",
                                    ReturnQty = Helper.FormatThousand(detail.ReturnQty),
                                    ToBinRackCode = detail.ToBinRackCode != null ? detail.ToBinRackCode : "",
                                    PutBy = detail.PutBy != null ? detail.PutBy : "",
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
                        
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableDataInOutSummary()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string materialcode = request["materialcode"].ToString();
            string StartDate = request["filterStartDate"].ToString();
            string EndDate = request["filterEndDate"].ToString();

            if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate) && string.IsNullOrEmpty(materialcode))
            {
                throw new Exception("Parameter is required.");
            }                      

            IEnumerable<vDataInOutSummary> list = Enumerable.Empty<vDataInOutSummary>();
            IEnumerable<DataInOutDTOReport> pagedData = Enumerable.Empty<DataInOutDTOReport>();

            decimal previousTransferInventoryQty = 0;
            decimal previousInventoryQty = 0;  // Variabel untuk menyimpan nilai InventoryQty sebelumnya

            DateTime filterStartDate = Convert.ToDateTime(StartDate);
            DateTime filterEndDate = Convert.ToDateTime(EndDate);
            IQueryable<vDataInOutSummary> query;

            query = db.vDataInOutSummaries.Where(s => s.ItemCode.Equals(materialcode) 
                        && DbFunctions.TruncateTime(s.Date) >= DbFunctions.TruncateTime(filterStartDate) 
                        && DbFunctions.TruncateTime(s.Date) <= DbFunctions.TruncateTime(filterEndDate));

            decimal transferstockqty = 0;
            decimal totalInQty = 0;
            decimal totalOutQty = 0;
            if (!string.IsNullOrEmpty(materialcode))
            {
                transferstockqty = db.vStockAlls
                    .Where(s => s.Quantity > 0 && DbFunctions.TruncateTime(s.InDate) < DbFunctions.TruncateTime(filterStartDate) && s.MaterialCode.Equals(materialcode))
                    .Sum(s => (decimal?)s.Quantity) ?? 0; // Menggunakan nullable decimal untuk menghindari null

                totalInQty = db.vDataInOutSummaries
                   .Where(s => DbFunctions.TruncateTime(s.Date) >= DbFunctions.TruncateTime(filterStartDate) && DbFunctions.TruncateTime(s.Date) <= DbFunctions.TruncateTime(filterEndDate) && s.ItemCode.Equals(materialcode))
                   .Sum(s => (decimal?)s.ReceiveQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null

                totalOutQty = db.vDataInOutSummaries
                   .Where(s => DbFunctions.TruncateTime(s.Date) >= DbFunctions.TruncateTime(filterStartDate) && DbFunctions.TruncateTime(s.Date) <= DbFunctions.TruncateTime(filterEndDate) && s.ItemCode.Equals(materialcode))
                   .Sum(s => (decimal?)s.IssueSlipQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.ItemCode.Contains(search)
                        );

                Dictionary<string, Func<vDataInOutSummary, object>> cols = new Dictionary<string, Func<vDataInOutSummary, object>>();
                cols.Add("ItemCode", x => x.ItemCode);
                cols.Add("Date", x => x.Date);
                cols.Add("UserHanheld", x => x.UserHanheld);
                cols.Add("Type", x => x.Type);
                cols.Add("ReceiveQty", x => x.ReceiveQty);
                cols.Add("IssueSlipQty", x => x.IssueSlipQty);
                cols.Add("BalanceQty", x => x.BalanceQty);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (!string.IsNullOrEmpty(materialcode))
                {
                    if (transferstockqty > 0)
                    {
                        previousTransferInventoryQty = previousTransferInventoryQty + transferstockqty;
                    }
                    previousInventoryQty = previousTransferInventoryQty;

                    // List untuk menampung hasil yang akan dipaging
                    var reportData = new List<DataInOutDTOReport>();

                    // Perhitungan InventoryQty per baris dan simpan semua data untuk halaman ini
                    foreach (var detail in list)
                    {
                        // Update previousInventoryQty untuk setiap baris
                        previousInventoryQty = CalculateInventoryQty(ref previousInventoryQty, Convert.ToDecimal(detail.ReceiveQty), Convert.ToDecimal(detail.IssueSlipQty));

                        reportData.Add(new DataInOutDTOReport
                        {
                            ItemCode = detail.ItemCode,
                            Date = Helper.NullDateToString2(detail.Date),
                            UserHanheld = detail.UserHanheld,
                            Type = detail.Type,
                            ReceiveQty = Helper.FormatThousand(detail.ReceiveQty),
                            IssueSlipQty = Helper.FormatThousand(detail.IssueSlipQty),
                            BalanceQty = previousInventoryQty.ToString("#,0.00"),  
                        });
                    }

                    // Cek apakah ini adalah halaman terakhir
                    bool isLastPage = (start + length) >= recordsTotal;

                    // Jika ini adalah halaman terakhir, tambahkan baris sum
                    if (isLastPage)
                    {
                        // Ambil data dari baris terakhir
                        var lastRow = list.LastOrDefault();

                        // Cek apakah ada data terakhir
                        if (lastRow != null)
                        {
                            var sumRow = new DataInOutDTOReport
                            {
                                ItemCode = materialcode,
                                Date = "",
                                UserHanheld = "",
                                Type = "SUM",
                                ReceiveQty = Helper.FormatThousand(totalInQty),
                                IssueSlipQty = Helper.FormatThousand(totalOutQty),
                                BalanceQty = previousInventoryQty.ToString("#,0.00"),
                            };

                            // Tambahkan row sum ke dalam data
                            reportData.Add(sumRow);
                        }
                        else
                        {
                            var sumRow = new DataInOutDTOReport
                            {
                                ItemCode = materialcode,
                                Date = "",
                                UserHanheld = "",
                                Type = "SUM",
                                ReceiveQty = Helper.FormatThousand(totalInQty),
                                IssueSlipQty = Helper.FormatThousand(totalOutQty),
                                BalanceQty = previousTransferInventoryQty.ToString("#,0.00"),
                            };

                            // Tambahkan row sum ke dalam data
                            reportData.Add(sumRow);
                        }
                    }

                    // Jika ada transaksi sebelumnya, tambahkan ke response
                    if (previousTransferInventoryQty >= 0 && start == 0)
                    {
                        // Tambahkan transaksi sebelumnya ke dalam report
                        var previousTransactionRow = new DataInOutDTOReport
                        {
                            ItemCode = materialcode,
                            Date = "",
                            UserHanheld = "",
                            Type = "TRANSFER",
                            ReceiveQty = 0.ToString("#,0.00"),
                            IssueSlipQty = 0.ToString("#,0.00"),
                            BalanceQty = previousTransferInventoryQty.ToString("#,0.00"),
                        };

                        reportData.Insert(0, previousTransactionRow);  // Insert transaksi sebelumnya pada posisi pertama
                    }

                    pagedData = reportData;
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataReportIssueSlip(string date, string enddate)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vIssueSlipReport> list = Enumerable.Empty<vIssueSlipReport>();
            IEnumerable<IssueSlipDTOReport> pagedData = Enumerable.Empty<IssueSlipDTOReport>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vIssueSlipReport> query;

            query = db.vIssueSlipReports.Where(s => DbFunctions.TruncateTime(s.Header_ProductionDate) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.Header_ProductionDate) <= DbFunctions.TruncateTime(endfilterDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vIssueSlipReport, object>> cols = new Dictionary<string, Func<vIssueSlipReport, object>>();
                cols.Add("RM_Code", x => x.RM_Code);
                cols.Add("RM_Name", x => x.RM_Name);
                cols.Add("RM_VendorName", x => x.RM_VendorName);
                cols.Add("Wt_Request", x => x.Wt_Request);
                cols.Add("SupplyQty", x => x.SupplyQty);
                cols.Add("FromBinRackCode", x => x.FromBinRackCode);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("ReturnQty", x => x.ReturnQty);
                cols.Add("ToBinRackCode", x => x.ToBinRackCode);
                cols.Add("PutBy", x => x.PutBy);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new IssueSlipDTOReport
                                {
                                    ID_Header = detail.ID_Header,
                                    ID_Order = detail.ID_Order,
                                    Header_Code = detail.Header_Code,
                                    Header_Name = detail.Header_Name,
                                    Header_ProductionDate = Helper.NullDateToString2(detail.Header_ProductionDate),
                                    RM_Code = detail.RM_Code,
                                    RM_Name = detail.RM_Name,
                                    RM_VendorName = detail.RM_VendorName,
                                    Wt_Request = Helper.FormatThousand(detail.Wt_Request),
                                    SupplyQty = Helper.FormatThousand(detail.SupplyQty),
                                    FromBinRackCode = detail.FromBinRackCode != null ? detail.FromBinRackCode : "",
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    PickedBy = detail.PickedBy != null ? detail.PickedBy : "",
                                    ReturnQty = Helper.FormatThousand(detail.ReturnQty),
                                    ToBinRackCode = detail.ToBinRackCode != null ? detail.ToBinRackCode : "",
                                    PutBy = detail.PutBy != null ? detail.PutBy : "",
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("list", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataReportDataInOut(string materialcode, string startdate, string enddate)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(materialcode) || string.IsNullOrEmpty(startdate) || string.IsNullOrEmpty(enddate))
            {
                throw new Exception("Parameter is required.");
            }

            SemiFinishGood sfg = db.SemiFinishGoods.Where(m => m.MaterialCode.Equals(materialcode)).FirstOrDefault();
            if (sfg == null)
            {
                throw new Exception("Material not recognized.");
            }

            IEnumerable<vDataInOutSummary> list = Enumerable.Empty<vDataInOutSummary>();
            IEnumerable<DataInOutDTOReport> pagedData = Enumerable.Empty<DataInOutDTOReport>();

            DateTime filterStartDate = Convert.ToDateTime(startdate);
            DateTime filterEndDate = Convert.ToDateTime(enddate);
            IQueryable<vDataInOutSummary> query;

            query = db.vDataInOutSummaries.Where(s => s.ItemCode.Equals(materialcode) && DbFunctions.TruncateTime(s.Date) >= DbFunctions.TruncateTime(filterStartDate) && DbFunctions.TruncateTime(s.Date) <= DbFunctions.TruncateTime(filterEndDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vDataInOutSummary, object>> cols = new Dictionary<string, Func<vDataInOutSummary, object>>();
                cols.Add("ItemCode", x => x.ItemCode);
                cols.Add("Date", x => x.Date);
                cols.Add("UserHanheld", x => x.UserHanheld);
                cols.Add("Type", x => x.Type);
                cols.Add("ReceiveQty", x => x.ReceiveQty);
                cols.Add("IssueSlipQty", x => x.IssueSlipQty);
                cols.Add("BalanceQty", x => x.BalanceQty);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new DataInOutDTOReport
                                {
                                    ItemCode = detail.ItemCode,
                                    Date = Helper.NullDateToString2(detail.Date),
                                    UserHanheld = detail.UserHanheld,
                                    Type = detail.Type,
                                    ReceiveQty = detail.ReceiveQty.ToString(),
                                    IssueSlipQty = detail.IssueSlipQty.ToString(),
                                    BalanceQty = detail.BalanceQty.ToString(),
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("list", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatableDetailListTransaction()
        {
            int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            HttpRequest request = HttpContext.Current.Request;
            string date = request["date"].ToString();
            string enddate = request["enddate"].ToString();
            string materialcode = request["materialcode"].ToString();
            string inouttype = request["inouttype"].ToString();
            string warehousecode = request["warehousecode"].ToString();

            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate) && string.IsNullOrEmpty(materialcode) && string.IsNullOrEmpty(inouttype))
            {
                throw new Exception("Parameter is required.");
            }

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<BinRack> listWHName = Enumerable.Empty<BinRack>();
            IEnumerable<vIssueSlipListTransaction> list = Enumerable.Empty<vIssueSlipListTransaction>();
            IEnumerable<ListTransactionDTOReport> pagedData = Enumerable.Empty<ListTransactionDTOReport>();

            decimal previousTransferInventoryQty = 0;
            decimal previousInventoryQty = 0;  // Variabel untuk menyimpan nilai InventoryQty sebelumnya

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vIssueSlipListTransaction> query;

            // Ambil data dari BinRack untuk mendapatkan WHName sesuai warehousecode
            listWHName = db.BinRacks.Where(br => br.WarehouseCode.Equals(warehousecode)).ToList();

            var warehouseNames = listWHName.Select(br => br.WarehouseName).Distinct().ToList();

            query = db.vIssueSlipListTransactions.AsQueryable(); // Inisialisasi query agar dapat ditambah kondisi

            if (!string.IsNullOrEmpty(warehousecode))
            {
                query = query.Where(s => warehouseNames.Contains(s.WHName));
            }

            if (inouttype != "ALL")
            {
                query = query.Where(s => s.InOutType.Equals(inouttype));
            }

            if (!string.IsNullOrEmpty(materialcode))
            {
                query = query.Where(s => s.RMCode.Equals(materialcode));
            }

            query = query.Where(s => s.RMCode.Equals(materialcode) && DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate));

            decimal transferstockqty = 0;
            decimal totalInQty = 0;
            decimal totalOutQty = 0;
            if (!string.IsNullOrEmpty(materialcode) && !string.IsNullOrEmpty(warehousecode))
            {
                transferstockqty = db.vStockAlls
                    .Where(s => s.Quantity > 0 && DbFunctions.TruncateTime(s.InDate) < DbFunctions.TruncateTime(filterDate) && s.MaterialCode.Equals(materialcode) && warehouseNames.Contains(s.WarehouseName))
                    .Sum(s => (decimal?)s.Quantity) ?? 0; // Menggunakan nullable decimal untuk menghindari null

                if (inouttype != "ALL")
                {
                    totalInQty = db.vIssueSlipListTransactions
                     .Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate) && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                           && warehouseNames.Contains(s.WHName) && s.InOutType.Equals(inouttype) && s.RMCode.Equals(materialcode))
                     .Sum(s => (decimal?)s.InQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null

                    totalOutQty = db.vIssueSlipListTransactions
                       .Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate) && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                            && warehouseNames.Contains(s.WHName) && s.InOutType.Equals(inouttype) && s.RMCode.Equals(materialcode))
                       .Sum(s => (decimal?)s.OutQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null
                }
                else
                {
                    totalInQty = db.vIssueSlipListTransactions
                      .Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate) && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                            && warehouseNames.Contains(s.WHName) && s.RMCode.Equals(materialcode))
                      .Sum(s => (decimal?)s.InQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null

                    totalOutQty = db.vIssueSlipListTransactions
                       .Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate) && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate)
                            && warehouseNames.Contains(s.WHName) && s.RMCode.Equals(materialcode))
                       .Sum(s => (decimal?)s.OutQty) ?? 0; // Menggunakan nullable decimal untuk menghindari null
                }
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.RMCode.Contains(search)
                        || m.RMName.Contains(search)
                        );

                Dictionary<string, Func<vIssueSlipListTransaction, object>> cols = new Dictionary<string, Func<vIssueSlipListTransaction, object>>();
                cols.Add("Id", x => x.Id);
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("WHName", x => x.WHName);
                cols.Add("InOut", x => x.InOut);
                cols.Add("TransactionDate", x => x.TransactionDate);
                cols.Add("InQty", x => x.InQty);
                cols.Add("OutQty", x => x.OutQty);
                cols.Add("InventoryQty", x => x.InventoryQty);
                cols.Add("InOutType", x => x.InOutType);
                cols.Add("CreateBy", x => x.CreateBy);
                cols.Add("CreateOn", x => x.CreateOn);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();
                list = list.Skip(start).Take(length).ToList();

                if (!string.IsNullOrEmpty(materialcode) && !string.IsNullOrEmpty(warehousecode))
                {
                    if (transferstockqty > 0)
                    {
                        previousTransferInventoryQty = previousTransferInventoryQty + transferstockqty;
                    }
                    previousInventoryQty = previousTransferInventoryQty;

                    // List untuk menampung hasil yang akan dipaging
                    var reportData = new List<ListTransactionDTOReport>();

                    // Perhitungan InventoryQty per baris dan simpan semua data untuk halaman ini
                    foreach (var detail in list)
                    {
                        // Update previousInventoryQty untuk setiap baris
                        previousInventoryQty = CalculateInventoryQty(ref previousInventoryQty, Convert.ToDecimal(detail.InQty), Convert.ToDecimal(detail.OutQty));

                        reportData.Add(new ListTransactionDTOReport
                        {
                            Id = detail.Id,
                            RMCode = detail.RMCode,
                            RMName = detail.RMName,
                            WHName = detail.WHName,
                            InOut = detail.InOut,
                            TransactionDate = Helper.NullDateToString2(detail.TransactionDate),
                            InQty = Helper.FormatThousand(detail.InQty),
                            OutQty = Helper.FormatThousand(detail.OutQty),
                            InventoryQty = previousInventoryQty.ToString("#,0.00"),  // Menggunakan previousInventoryQty
                            InOutType = detail.InOutType,
                            CreateBy = detail.CreateBy,
                            CreateOn = detail.CreateOn.ToString("yyyy-MM-dd HH:mm:ss"),
                        });
                    }

                    // Cek apakah ini adalah halaman terakhir
                    bool isLastPage = (start + length) >= recordsTotal;

                    // Jika ini adalah halaman terakhir, tambahkan baris sum
                    if (isLastPage)
                    {
                        // Ambil data dari baris terakhir
                        var lastRow = list.LastOrDefault();

                        // Cek apakah ada data terakhir
                        if (lastRow != null)
                        {
                            var sumRow = new ListTransactionDTOReport
                            {
                                Id = lastRow.Id,
                                RMCode = lastRow.RMCode,
                                RMName = lastRow.RMName,
                                WHName = lastRow.WHName,
                                InOut = "SUM",
                                InQty = Helper.FormatThousand(totalInQty),
                                OutQty = Helper.FormatThousand(totalOutQty),
                                InventoryQty = previousInventoryQty.ToString("#,0.00"),
                                InOutType = "",
                                CreateBy = "",
                                CreateOn = "",
                            };

                            // Tambahkan row sum ke dalam data
                            reportData.Add(sumRow);
                        }
                        else
                        {
                            var sumRow = new ListTransactionDTOReport
                            {
                                Id = "1",
                                RMCode = materialcode,
                                RMName = "",
                                WHName = "",
                                InOut = "SUM",
                                InQty = Helper.FormatThousand(totalInQty),
                                OutQty = Helper.FormatThousand(totalOutQty),
                                InventoryQty = previousTransferInventoryQty.ToString("#,0.00"),
                                InOutType = "",
                                CreateBy = "",
                                CreateOn = "",
                            };

                            // Tambahkan row sum ke dalam data
                            reportData.Add(sumRow);
                        }
                    }

                    // Jika ada transaksi sebelumnya, tambahkan ke response
                    if (previousTransferInventoryQty >= 0 && start == 0)
                    {
                        // Tambahkan transaksi sebelumnya ke dalam report
                        var previousTransactionRow = new ListTransactionDTOReport
                        {
                            Id = "1",
                            RMCode = materialcode,
                            RMName = "",
                            WHName = "",
                            InOut = "TRANSFER",
                            InQty = 0.ToString("#,0.00"),
                            OutQty = 0.ToString("#,0.00"),
                            InventoryQty = previousTransferInventoryQty.ToString("#,0.00"),
                            InOutType = "",
                            CreateBy = "",
                            CreateOn = ""
                        };

                        reportData.Insert(0, previousTransactionRow);  // Insert transaksi sebelumnya pada posisi pertama
                    }

                    pagedData = reportData;
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        private decimal CalculateInventoryQty(ref decimal previousInventoryQty, decimal inQty, decimal outQty)
        {
            decimal inventoryQty = 0;

            // Jika previousInventoryQty masih 0, berarti ini adalah baris pertama
            if (previousInventoryQty == 0)
            {
                inventoryQty = inQty - outQty;  // InventoryQty pertama kali dihitung
            }
            else
            {
                if (inQty == 0)
                {
                    inventoryQty = previousInventoryQty - outQty;  // Kurangi InventoryQty dengan OutQty jika InQty = 0
                }
                else if (outQty == 0)
                {
                    inventoryQty = previousInventoryQty + inQty;  // Tambah InventoryQty dengan InQty jika OutQty = 0
                }
                else
                {
                    inventoryQty = previousInventoryQty + inQty - outQty;  // Default, tambahkan InQty dan kurangi OutQty
                }
            }

            // Update previousInventoryQty untuk digunakan di baris berikutnya
            previousInventoryQty = inventoryQty;

            return inventoryQty;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDataReportListTransaction(string startdate, string enddate, string materialcode, string inouttype, string warehousecode)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(startdate) && string.IsNullOrEmpty(enddate) && string.IsNullOrEmpty(materialcode) && string.IsNullOrEmpty(inouttype))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vIssueSlipListTransaction> list = Enumerable.Empty<vIssueSlipListTransaction>();
            IEnumerable<ListTransactionDTOReport> pagedData = Enumerable.Empty<ListTransactionDTOReport>();

            decimal previousInventoryQty = 0;  // Variabel untuk menyimpan nilai InventoryQty sebelumnya

            DateTime filterDate = Convert.ToDateTime(startdate);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vIssueSlipListTransaction> query;

            IEnumerable<BinRack> listWHName = Enumerable.Empty<BinRack>();

            // Ambil data dari BinRack untuk mendapatkan WHName sesuai warehousecode
            listWHName = db.BinRacks.Where(br => br.WarehouseCode.Equals(warehousecode)).ToList();

            var warehouseNames = listWHName.Select(br => br.WarehouseName).ToList();

            query = db.vIssueSlipListTransactions.AsQueryable(); // Inisialisasi query agar dapat ditambah kondisi

            if (!string.IsNullOrEmpty(warehousecode))
            {
                query = query.Where(s => warehouseNames.Contains(s.WHName));
            }

            if (inouttype != "ALL")
            {
                query = query.Where(s => s.InOutType.Equals(inouttype));
            }

            if (!string.IsNullOrEmpty(materialcode))
            {
                query = query.Where(s => s.RMCode.Equals(materialcode));
            }

            query = query.Where(s => DbFunctions.TruncateTime(s.CreateOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreateOn) <= DbFunctions.TruncateTime(endfilterDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vIssueSlipListTransaction, object>> cols = new Dictionary<string, Func<vIssueSlipListTransaction, object>>();
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("WHName", x => x.WHName);
                cols.Add("InOut", x => x.InOut);
                cols.Add("TransactionDate", x => x.TransactionDate);
                cols.Add("InQty", x => x.InQty);
                cols.Add("OutQty", x => x.OutQty);
                cols.Add("InventoryQty", x => x.InventoryQty);
                cols.Add("InOutType", x => x.InOutType);
                cols.Add("CreateBy", x => x.CreateBy);
                cols.Add("CreateOn", x => x.CreateOn);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ListTransactionDTOReport
                                {
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    WHName = detail.WHName,
                                    InOut = detail.InOut,
                                    TransactionDate = Helper.NullDateToString2(detail.TransactionDate),
                                    InQty = Helper.FormatThousand(detail.InQty),
                                    OutQty = Helper.FormatThousand(detail.OutQty),
                                    //InventoryQty = Helper.FormatThousand(detail.InventoryQty),
                                    InventoryQty = CalculateInventoryQty(ref previousInventoryQty, Convert.ToDecimal(detail.InQty), Convert.ToDecimal(detail.OutQty)).ToString("#,0.00"),  // Panggil fungsi untuk menghitung InventoryQty
                                    InOutType = detail.InOutType,
                                    CreateBy = detail.CreateBy,
                                    CreateOn = detail.CreateOn.ToString("yyyy-MM-dd HH:mm:ss"),
                                };
                }

                status = true;
                message = "Fetch data succeeded.";
            }
            catch (HttpRequestException reqpEx)
            {
                message = reqpEx.Message;
                return BadRequest();
            }
            catch (HttpResponseException respEx)
            {
                message = respEx.Message;
                return NotFound();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            obj.Add("list2", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
    }
}
