using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class QCInspectionController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        [HttpPost]
        public async Task<IHttpActionResult> Create(QCInspectionVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.StockID))
                    {
                        throw new Exception("StockID is required.");
                    }

                    vStockAll vStock = await db.vStockAlls.Where(m => m.ID.Equals(dataVM.StockID)).FirstOrDefaultAsync();
                    if (vStock == null)
                    {
                        throw new Exception("Stock is not recognized.");
                    }

                    //check expired date
                    if (!(DateTime.Now.Date >= vStock.ExpiredDate.Value.Date))
                    {
                        throw new Exception("Stock is not expired yet.");
                    }

                    //check if material on qc progress
                    if (vStock.Quantity <= 0)
                    {
                        throw new Exception("Stock is not available.");
                    }

                    //check qc already created
                    QCInspection prevInspect = db.QCInspections.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNo.Equals(vStock.LotNumber) && m.InDate == vStock.InDate && m.ExpDate == vStock.ExpiredDate).FirstOrDefault();

                    if (prevInspect != null && !prevInspect.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("QC Inspection already on progress.");
                    }



                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("QC");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.QCInspections.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                    QCInspection qCInspection = new QCInspection
                    {
                        ID = TransactionId,
                        Code = Code,
                        MaterialType = vStock.Type,
                        MaterialCode = vStock.MaterialCode,
                        MaterialName = vStock.MaterialName,
                        LotNo = vStock.LotNumber,
                        InDate = vStock.InDate.Value,
                        ExpDate = vStock.ExpiredDate.Value,
                        TransactionStatus = "PROGRESS",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        Priority = true
                    };

                    List<StockDTO> stocks = new List<StockDTO>();
                    List<QCPicking> pickings = new List<QCPicking>();

                    if (vStock.Type.Equals("RM"))
                    {
                        //create picking list
                        IEnumerable<StockRM> stockRMs = await db.StockRMs.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNumber.Equals(vStock.LotNumber) && m.InDate.Value.Equals(vStock.InDate.Value) && m.ExpiredDate.Value.Equals(vStock.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockRM stock in stockRMs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }
                    else
                    {
                        IEnumerable<StockSFG> stockSFGs = await db.StockSFGs.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNumber.Equals(vStock.LotNumber) && m.InDate.Value.Equals(vStock.InDate.Value) && m.ExpiredDate.Value.Equals(vStock.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockSFG stock in stockSFGs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }

                    qCInspection.QCPickings = pickings;

                    db.QCInspections.Add(qCInspection);

                    await db.SaveChangesAsync();

                    obj.Add("id", qCInspection.ID);

                    status = true;
                    message = "Create QC Inspection succeeded.";

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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateExpired(QCInspectionVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.MaterialCode))
                    {
                        throw new Exception("Material Code is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.LotNumber))
                    {
                        throw new Exception("Lot Number is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.InDate))
                    {
                        throw new Exception("In Date is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.ExpiredDate))
                    {
                        throw new Exception("Expired Date is required.");
                    }

                    DateTime InDate = Convert.ToDateTime(dataVM.InDate);
                    DateTime ExpDate = Convert.ToDateTime(dataVM.ExpiredDate);
                    //commented by bhov
                    //vStockInpspection vStock = await db.vStockInpspections.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode) && m.LotNumber.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpiredDate == ExpDate && m.TotalQty > 0).FirstOrDefaultAsync();
                    //vStockAll vStockAll = await db.vStockAlls.Where()
                    //if (vStock == null)
                    //{
                    //    throw new Exception("Stock is not recognized.");
                    //}

                    vQCInspectExpired qCInspectExpired = await db.vQCInspectExpireds.Where(m => !m.TransactionStatus.Equals("CLOSED") && m.MaterialCode.Equals(dataVM.MaterialCode) && m.LotNumber.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpiredDate == ExpDate && m.TotalQty > 0).FirstOrDefaultAsync();

                    //check qc already exist or not
                    if (qCInspectExpired != null && !string.IsNullOrEmpty(qCInspectExpired.ID))
                    {
                        throw new Exception("QC Inspection on progress.");
                    }


                    ////check expired date
                    //if (!(DateTime.Now.Date >= vStock.ExpiredDate.Value))
                    //{
                    //    throw new Exception("Stock is not expired yet.");
                    //}

                    //check if material on qc progress
                    //if (vStock.TotalQty <= 0)
                    //{
                    //    throw new Exception("Stock is not available.");
                    //}

                    //check qc already created
                    //QCInspection prevInspect = db.QCInspections.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNo.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpDate == ExpDate).FirstOrDefault();

                    //if (prevInspect != null && !prevInspect.TransactionStatus.Equals("CLOSED"))
                    //{
                    //    throw new Exception("QC Inspection already on progress.");
                    //}

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("QC");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.QCInspections.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                    QCInspection qCInspection = new QCInspection
                    {
                        ID = TransactionId,
                        Code = Code,
                        MaterialType = qCInspectExpired.MaterialType,
                        MaterialCode = qCInspectExpired.MaterialCode,
                        MaterialName = qCInspectExpired.MaterialName.Remove(qCInspectExpired.MaterialName.Length - 5),
                        LotNo = qCInspectExpired.LotNumber,
                        InDate = qCInspectExpired.InDate.Value,
                        ExpDate = qCInspectExpired.ExpiredDate.Value,
                        TransactionStatus = "PROGRESS",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        Priority = false
                    };

                    List<StockDTO> stocks = new List<StockDTO>();
                    List<QCPicking> pickings = new List<QCPicking>();

                    if (qCInspectExpired.MaterialType.Equals("RM"))
                    {

                        //create picking list
                        IEnumerable<StockRM> stockRMs = await db.StockRMs.Where(m => m.MaterialCode.Equals(qCInspectExpired.MaterialCode) && m.LotNumber.Equals(qCInspectExpired.LotNumber) && m.InDate.Value.Equals(qCInspectExpired.InDate.Value) && m.ExpiredDate.Value.Equals(qCInspectExpired.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockRM stock in stockRMs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }
                    else
                    {
                        IEnumerable<StockSFG> stockSFGs = await db.StockSFGs.Where(m => m.MaterialCode.Equals(qCInspectExpired.MaterialCode) && m.LotNumber.Equals(qCInspectExpired.LotNumber) && m.InDate.Value.Equals(qCInspectExpired.InDate.Value) && m.ExpiredDate.Value.Equals(qCInspectExpired.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockSFG stock in stockSFGs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }

                    qCInspection.QCPickings = pickings;

                    db.QCInspections.Add(qCInspection);

                    await db.SaveChangesAsync();

                    obj.Add("id", qCInspection.ID);

                    status = true;
                    message = "Create QC Inspection succeeded.";

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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateNonExpired(QCInspectionVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.MaterialCode))
                    {
                        throw new Exception("Material Code is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.LotNumber))
                    {
                        throw new Exception("Lot Number is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.InDate))
                    {
                        throw new Exception("In Date is required.");
                    }

                    if (string.IsNullOrEmpty(dataVM.ExpiredDate))
                    {
                        throw new Exception("Expired Date is required.");
                    }

                    DateTime InDate = Convert.ToDateTime(dataVM.InDate);
                    DateTime ExpDate = Convert.ToDateTime(dataVM.ExpiredDate);
                    //commented by bhov
                    //vStockInpspection vStock = await db.vStockInpspections.Where(m => m.MaterialCode.Equals(dataVM.MaterialCode) && m.LotNumber.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpiredDate == ExpDate && m.TotalQty > 0).FirstOrDefaultAsync();
                    //vStockAll vStockAll = await db.vStockAlls.Where()
                    //if (vStock == null)
                    //{
                    //    throw new Exception("Stock is not recognized.");
                    //}

                    vQCInspectNonExpired qCInspectNonExpired = await db.vQCInspectNonExpireds.Where(m => !m.TransactionStatus.Equals("CLOSED") && m.MaterialCode.Equals(dataVM.MaterialCode) && m.LotNumber.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpiredDate == ExpDate && m.TotalQty > 0).FirstOrDefaultAsync();

                    //check qc already exist or not
                    if (qCInspectNonExpired != null && !string.IsNullOrEmpty(qCInspectNonExpired.ID))
                    {
                        throw new Exception("QC Inspection on progress.");
                    }


                    ////check expired date
                    //if (!(DateTime.Now.Date >= vStock.ExpiredDate.Value))
                    //{
                    //    throw new Exception("Stock is not expired yet.");
                    //}

                    //check if material on qc progress
                    //if (vStock.TotalQty <= 0)
                    //{
                    //    throw new Exception("Stock is not available.");
                    //}

                    //check qc already created
                    //QCInspection prevInspect = db.QCInspections.Where(m => m.MaterialCode.Equals(vStock.MaterialCode) && m.LotNo.Equals(dataVM.LotNumber) && m.InDate == InDate && m.ExpDate == ExpDate).FirstOrDefault();

                    //if (prevInspect != null && !prevInspect.TransactionStatus.Equals("CLOSED"))
                    //{
                    //    throw new Exception("QC Inspection already on progress.");
                    //}

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);
                    var CreatedAt = transactionDate;
                    var TransactionId = Helper.CreateGuid("QC");

                    string prefix = TransactionId.Substring(0, 2);
                    int year = Convert.ToInt32(CreatedAt.Year.ToString().Substring(2));
                    int month = CreatedAt.Month;
                    string romanMonth = Helper.ConvertMonthToRoman(month);

                    // get last number, and do increment.
                    string lastNumber = db.QCInspections.AsQueryable().OrderByDescending(x => x.Code)
                        .Where(x => x.CreatedOn.Year.Equals(CreatedAt.Year) && x.CreatedOn.Month.Equals(CreatedAt.Month))
                        .AsEnumerable().Select(x => x.Code).FirstOrDefault();
                    int currentNumber = 0;

                    if (!string.IsNullOrEmpty(lastNumber))
                    {
                        currentNumber = Int32.Parse(lastNumber.Substring(lastNumber.Length - 3));
                    }

                    string runningNumber = string.Format("{0:D3}", currentNumber + 1);

                    var Code = string.Format("EIN-RMI/{0}/{1}/{2}/{3}", prefix, year, romanMonth, runningNumber);

                    QCInspection qCInspection = new QCInspection
                    {
                        ID = TransactionId,
                        Code = Code,
                        MaterialType = qCInspectNonExpired.MaterialType,
                        MaterialCode = qCInspectNonExpired.MaterialCode,
                        MaterialName = qCInspectNonExpired.MaterialName.Remove(qCInspectNonExpired.MaterialName.Length - 5),
                        LotNo = qCInspectNonExpired.LotNumber,
                        InDate = qCInspectNonExpired.InDate.Value,
                        ExpDate = qCInspectNonExpired.ExpiredDate.Value,
                        TransactionStatus = "PROGRESS",
                        CreatedBy = activeUser,
                        CreatedOn = CreatedAt,
                        Priority = false
                    };

                    List<StockDTO> stocks = new List<StockDTO>();
                    List<QCPicking> pickings = new List<QCPicking>();

                    if (qCInspectNonExpired.MaterialType.Equals("RM"))
                    {

                        //create picking list
                        IEnumerable<StockRM> stockRMs = await db.StockRMs.Where(m => m.MaterialCode.Equals(qCInspectNonExpired.MaterialCode) && m.LotNumber.Equals(qCInspectNonExpired.LotNumber) && m.InDate.Value.Equals(qCInspectNonExpired.InDate.Value) && m.ExpiredDate.Value.Equals(qCInspectNonExpired.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockRM stock in stockRMs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }
                    else
                    {
                        IEnumerable<StockSFG> stockSFGs = await db.StockSFGs.Where(m => m.MaterialCode.Equals(qCInspectNonExpired.MaterialCode) && m.LotNumber.Equals(qCInspectNonExpired.LotNumber) && m.InDate.Value.Equals(qCInspectNonExpired.InDate.Value) && m.ExpiredDate.Value.Equals(qCInspectNonExpired.ExpiredDate.Value) && m.Quantity > 0).ToListAsync();
                        foreach (StockSFG stock in stockSFGs)
                        {
                            QCPicking pick = new QCPicking();
                            pick.ID = Helper.CreateGuid("QCp");
                            pick.QCInspectionID = TransactionId;
                            pick.StockCode = stock.Code;
                            pick.BinRackID = stock.BinRackID;
                            pick.BinRackCode = stock.BinRackCode;
                            pick.BinRackName = stock.BinRackName;
                            pick.Qty = stock.Quantity;
                            pick.QtyPerBag = stock.QtyPerBag;

                            pickings.Add(pick);

                            //update stock to zero
                            //stock.Quantity = 0;
                            stock.OnInspect = true;
                        }
                    }

                    qCInspection.QCPickings = pickings;

                    db.QCInspections.Add(qCInspection);

                    await db.SaveChangesAsync();

                    obj.Add("id", qCInspection.ID);

                    status = true;
                    message = "Create QC Inspection succeeded.";

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

            return Ok(obj);
        }

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableExpired()
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<vStockExpired> list = Enumerable.Empty<vStockExpired>();
        //    IEnumerable<ExpiredListDTO> pagedData = Enumerable.Empty<ExpiredListDTO>();


        //    int recordsTotal = 0;

        //    IQueryable<vStockExpired> query = db.vStockExpireds.AsQueryable().OrderBy(o => o.ExpiredDate);

        //    recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        query = query
        //                .Where(m => m.MaterialCode.Contains(search)
        //                 || m.MaterialName.Contains(search)
        //                 || m.LotNumber.Contains(search)
        //                );

        //        Dictionary<string, Func<vStockExpired, object>> cols = new Dictionary<string, Func<vStockExpired, object>>();
        //        cols.Add("MaterialCode", x => x.MaterialCode);
        //        cols.Add("MaterialName", x => x.MaterialName);
        //        cols.Add("MaterialType", x => x.MaterialType);
        //        cols.Add("LotNumber", x => x.LotNumber);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpiredDate", x => x.ExpiredDate);
        //        cols.Add("TotalQty", x => x.TotalQty);
        //        cols.Add("ExpirationDay", x => x.ExpirationDay);


        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from x in list
        //                        select new ExpiredListDTO
        //                        {
        //                            MaterialCode = x.MaterialCode,
        //                            MaterialName = x.MaterialName,
        //                            MaterialType = x.MaterialType,
        //                            LotNumber = x.LotNumber,
        //                            InDate = Helper.NullDateToString(x.InDate),
        //                            ExpiredDate = Helper.NullDateToString(x.ExpiredDate),
        //                            TotalQty = Helper.FormatThousand(x.TotalQty)
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        [HttpPost]
        public async Task<IHttpActionResult> DatatableExpired(string transactionStatus, string filter1)
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
            var re = Request;
            var headers = re.Headers;

            string token = "";

            if (headers.Contains("token"))
            {
                token = headers.GetValues("token").First();
            }

            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

            User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
            string userAreaType = userData.AreaType;

            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<vQCInspectExpired2> list = Enumerable.Empty<vQCInspectExpired2>();
            IEnumerable<QCInspectionDTO> pagedData = Enumerable.Empty<QCInspectionDTO>();

            IQueryable<vQCInspectExpired2> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.vQCInspectExpired2.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();
            }
            else if (transactionStatus.Equals("ALL"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")).AsQueryable();
                }
            }
            else if (transactionStatus.Equals("OPEN/PROGRESS"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
                }
            }
            else if (transactionStatus.Equals("CONFIRMED"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "EXTEND" || s.InspectionStatus == "DISPOSE") && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "EXTEND" || s.InspectionStatus == "DISPOSE") && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "EXTEND" || s.InspectionStatus == "DISPOSE")).AsQueryable();
                }
            }
            else
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
                }
            }

            if (userAreaType == "PRODUCTION")
            {
                List<string> materialcode = db.SemiFinishGoods.Where(x => x.IsActive.Equals(true)).Select(d => d.MaterialCode).ToList();
                query = query.Where(a => materialcode.Contains(a.MaterialCode));
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query 
                        .Where(m => m.Code.Contains(search)
                         || m.MaterialCode.Contains(search)
                         || m.MaterialName.Contains(search)
                         || m.LotNumber.Contains(search)
                        );

                Dictionary<string, Func<vQCInspectExpired2, object>> cols = new Dictionary<string, Func<vQCInspectExpired2, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("Code", x => x.Code);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNumber);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpiredDate);
                cols.Add("ExpirationDay", x => x.ExpirationDay);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("Priority", x => x.Priority);
                cols.Add("InspectionStatus", x => x.InspectionStatus);
                cols.Add("InspectedBy", x => x.InspectedBy);
                cols.Add("InspectedOn", x => x.InspectedOn);

                if (!string.IsNullOrEmpty(sortDirection))
                {
                    if (sortDirection.Equals("asc"))
                        list = query.OrderBy(cols[sortName]);
                    else
                        list = query.OrderByDescending(cols[sortName]);
                }
                else
                {
                    list = query.OrderBy(o => o.Priority);
                }

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                //get total


                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new QCInspectionDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    MaterialType = x.MaterialType,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName.Remove(x.MaterialName.Length - 5),
                                    LotNo = x.LotNumber,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                    ExpirationDay = Helper.FormatThousand(x.ExpirationDay),
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    Priority = x.Priority.HasValue && x.Priority.Value,
                                    InspectionStatus = !string.IsNullOrEmpty(x.InspectionStatus) ? x.InspectionStatus : "",
                                    InspectedBy = x.InspectedBy,
                                    InspectedOn = Helper.NullDateTimeToString(x.InspectedOn),
                                    //JudgementAction =   string.IsNullOrEmpty(x.InspectionStatus) && Convert.ToDateTime(x.ExpiredDate) < DateTime.Now,
                                    PickingAction = db.QCPickings.Any(p => p.QCInspectionID == x.ID && p.PickedMethod == null),
                                    JudgementAction = !string.IsNullOrEmpty(x.ID),
                                    DisposeAction = !string.IsNullOrEmpty(x.ID),
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
        public async Task<IHttpActionResult> DatatableNonExpired(string transactionStatus, string filter1)
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
            var re = Request;
            var headers = re.Headers;

            string token = "";

            if (headers.Contains("token"))
            {
                token = headers.GetValues("token").First();
            }

            string activeUser = await db.Users.Where(x => x.Token.Equals(token)).Select(x => x.Username).FirstOrDefaultAsync();

            User userData = await db.Users.Where(x => x.Username.Equals(activeUser)).FirstOrDefaultAsync();
            string userAreaType = userData.AreaType;

            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<vQCInspectNonExpired2> list = Enumerable.Empty<vQCInspectNonExpired2>();
            IEnumerable<QCInspectionDTO> pagedData = Enumerable.Empty<QCInspectionDTO>();

            IQueryable<vQCInspectNonExpired2> query = null;

            int recordsTotal = 0;
            if (string.IsNullOrEmpty(transactionStatus))
            {
                query = db.vQCInspectNonExpired2.Where(s => !s.TransactionStatus.Equals("CANCELLED")).AsQueryable();
            }
            else if (transactionStatus.Equals("ALL"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS") || s.TransactionStatus.Equals("CONFIRMED")).AsQueryable();
                }
            }
            else if (transactionStatus.Equals("OPEN/PROGRESS"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => (s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals("OPEN") || s.TransactionStatus.Equals("PROGRESS")).AsQueryable();
                }
            }
            else if (transactionStatus.Equals("CONFIRMED"))
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "RETURN" || s.InspectionStatus == "DISPOSE") && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "RETURN" || s.InspectionStatus == "DISPOSE") && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals("CONFIRMED") && (s.InspectionStatus == "RETURN" || s.InspectionStatus == "DISPOSE")).AsQueryable();
                }
            }
            else
            {
                if (filter1.Equals("EXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ExpirationDay < 0).AsQueryable();
                }
                else if (filter1.Equals("NONEXPIRED"))
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus) && s.ExpirationDay >= 0).AsQueryable();
                }
                else
                {
                    query = db.vQCInspectNonExpired2.Where(s => s.TransactionStatus.Equals(transactionStatus)).AsQueryable();
                }
            }

            if (userAreaType == "PRODUCTION")
            {
                List<string> materialcode = db.SemiFinishGoods.Where(x => x.IsActive.Equals(true)).Select(d => d.MaterialCode).ToList();
                query = query.Where(a => materialcode.Contains(a.MaterialCode));
            }

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.Code.Contains(search)
                         || m.MaterialCode.Contains(search)
                         || m.MaterialName.Contains(search)
                         || m.LotNumber.Contains(search)
                        );

                Dictionary<string, Func<vQCInspectNonExpired2, object>> cols = new Dictionary<string, Func<vQCInspectNonExpired2, object>>();
                cols.Add("ID", x => x.ID);
                cols.Add("Code", x => x.Code);
                cols.Add("MaterialType", x => x.MaterialType);
                cols.Add("MaterialCode", x => x.MaterialCode);
                cols.Add("MaterialName", x => x.MaterialName);
                cols.Add("LotNo", x => x.LotNumber);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpiredDate);
                cols.Add("ExpirationDay", x => x.ExpirationDay);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("TransactionStatus", x => x.TransactionStatus);
                cols.Add("Priority", x => x.Priority);
                cols.Add("InspectionStatus", x => x.InspectionStatus);
                cols.Add("InspectedBy", x => x.InspectedBy);
                cols.Add("InspectedOn", x => x.InspectedOn);


                if (!string.IsNullOrEmpty(sortDirection))
                {
                    if (sortDirection.Equals("asc"))
                        list = query.OrderBy(cols[sortName]);
                    else
                        list = query.OrderByDescending(cols[sortName]);
                }
                else
                {
                    list = query.OrderBy(o => o.Priority);
                }

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                //get total

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new QCInspectionDTO
                                {
                                    ID = x.ID,
                                    Code = x.Code,
                                    MaterialType = x.MaterialType,
                                    MaterialCode = x.MaterialCode,
                                    MaterialName = x.MaterialName.Remove(x.MaterialName.Length - 5),
                                    LotNo = x.LotNumber,
                                    InDate = Helper.NullDateToString(x.InDate),
                                    ExpDate = Helper.NullDateToString(x.ExpiredDate),
                                    TransactionStatus = x.TransactionStatus,
                                    CreatedBy = x.CreatedBy,
                                    CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
                                    Priority = x.Priority.HasValue && x.Priority.Value,
                                    ExpirationDay = Helper.FormatThousand(x.ExpirationDay),
                                    InspectionStatus = !string.IsNullOrEmpty(x.InspectionStatus) ? x.InspectionStatus : "",
                                    InspectedBy = x.InspectedBy,
                                    InspectedOn = Helper.NullDateTimeToString(x.InspectedOn),
                                    DisposeAction = !string.IsNullOrEmpty(x.ID),
                                    ReturnAction = !string.IsNullOrEmpty(x.ID),
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
        //public async Task<IHttpActionResult> DatatableOperation(string InspectionStatus)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];

        //    string orderCol = "";
        //    string sortName = "";
        //    string sortDirection = "";

        //    if (HttpContext.Current.Request.Form.GetValues("order") != null)
        //    {
        //        orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //        sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //        sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];
        //    }

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<QCInspection> list = Enumerable.Empty<QCInspection>();
        //    IEnumerable<QCInspectionDTO> pagedData = Enumerable.Empty<QCInspectionDTO>();

        //    IQueryable<QCInspection> query = null;

        //    int recordsTotal = 0;
        //    if (InspectionStatus.Equals("ALL"))
        //    {
        //        query = db.QCInspections.Where(s => s.InspectionStatus != null).AsQueryable();
        //    }
        //    else if (InspectionStatus.Equals("EXTEND"))
        //    {
        //        query = db.QCInspections.Where(s => s.InspectionStatus.Equals("EXTEND")).AsQueryable();
        //    }
        //    else if (InspectionStatus.Equals("DISPOSE"))
        //    {
        //        query = db.QCInspections.Where(s => s.InspectionStatus.Equals("DISPOSE")).AsQueryable();
        //    }
        //    else if (InspectionStatus.Equals("RETURN"))
        //    {
        //        query = db.QCInspections.Where(s => s.InspectionStatus.Equals("RETURN")).AsQueryable();
        //    }
        //    else if (InspectionStatus.Equals("WAITING"))
        //    {
        //        query = db.QCInspections.Where(s => s.InspectionStatus == null).AsQueryable();
        //    }

        //    recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        query = query
        //                .Where(m => m.Code.Contains(search)
        //                 || m.MaterialCode.Contains(search)
        //                 || m.MaterialName.Contains(search)
        //                 || m.LotNo.Contains(search)
        //                );

        //        Dictionary<string, Func<QCInspection, object>> cols = new Dictionary<string, Func<QCInspection, object>>();
        //        cols.Add("ID", x => x.ID);
        //        cols.Add("Code", x => x.Code);
        //        cols.Add("MaterialType", x => x.MaterialType);
        //        cols.Add("MaterialCode", x => x.MaterialCode);
        //        cols.Add("MaterialName", x => x.MaterialName);
        //        cols.Add("LotNo", x => x.LotNo);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpDate", x => x.ExpDate);
        //        cols.Add("CreatedBy", x => x.CreatedBy);
        //        cols.Add("CreatedOn", x => x.CreatedOn);
        //        cols.Add("TransactionStatus", x => x.TransactionStatus);
        //        cols.Add("Priority", x => x.Priority);
        //        cols.Add("InspectionStatus", x => x.InspectionStatus);
        //        cols.Add("InspectedBy", x => x.InspectedBy);
        //        cols.Add("InspectedOn", x => x.InspectedOn);


        //        if (!string.IsNullOrEmpty(sortDirection))
        //        {
        //            if (sortDirection.Equals("asc"))
        //                list = query.OrderBy(cols[sortName]);
        //            else
        //                list = query.OrderByDescending(cols[sortName]);
        //        }
        //        else
        //        {
        //            list = query.OrderBy(o => o.Priority);
        //        }

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        //get total

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from x in list
        //                        select new QCInspectionDTO
        //                        {
        //                            ID = x.ID,
        //                            Code = x.Code,
        //                            MaterialType = x.MaterialType,
        //                            MaterialCode = x.MaterialCode,
        //                            MaterialName = x.MaterialName,
        //                            LotNo = x.LotNo,
        //                            InDate = Helper.NullDateToString(x.InDate),
        //                            ExpDate = Helper.NullDateToString(x.ExpDate),
        //                            TransactionStatus = x.TransactionStatus,
        //                            CreatedBy = x.CreatedBy,
        //                            CreatedOn = Helper.NullDateTimeToString(x.CreatedOn),
        //                            Priority = x.Priority,
        //                            InspectionStatus = !string.IsNullOrEmpty(x.InspectionStatus) ? x.InspectionStatus : "",
        //                            InspectedBy = x.InspectedBy,
        //                            InspectedOn = Helper.NullDateTimeToString(x.InspectedOn),
        //                            JudgementAction = string.IsNullOrEmpty(x.InspectionStatus) && Convert.ToDateTime(x.ExpDate) < DateTime.Now,
        //                            DisposeAction = string.IsNullOrEmpty(x.InspectionStatus),
        //                            PutawayExtendAction = x.InspectionStatus == "EXTEND",
        //                            PickingDisposeAction = x.InspectionStatus == "DISPOSE",
        //                            ReturnAction = string.IsNullOrEmpty(x.InspectionStatus) && Convert.ToDateTime(x.ExpDate) > DateTime.Now,
        //                            PrintPutawayExtendAction = x.InspectionStatus == "EXTEND",
        //                            TotalJudgementQty = Helper.FormatThousand(x.QCExtends.Sum(i => i.Qty)),
        //                            TotalDisposalQty = Helper.FormatThousand(x.QCPickings.Sum(i => i.Qty))
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        [HttpGet]
        public async Task<IHttpActionResult> GetDataById(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            QCInspectionDTO headerDTO = null;

            try
            {

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }


                QCInspection header = await db.QCInspections.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();


                if (header == null)
                {
                    throw new Exception("Data not found.");
                }

                headerDTO = new QCInspectionDTO
                {
                    ID = header.ID,
                    Code = header.Code,
                    MaterialType = header.MaterialType,
                    MaterialCode = header.MaterialCode,
                    MaterialName = header.MaterialName,
                    LotNo = header.LotNo,
                    InDate = Helper.NullDateToString(header.InDate),
                    ExpDate = Helper.NullDateToString(header.ExpDate),
                    TransactionStatus = header.TransactionStatus,
                    CreatedBy = header.CreatedBy,
                    CreatedOn = Helper.NullDateTimeToString(header.CreatedOn),
                    InspectionStatus = header.InspectionStatus,
                    //TotalJudgementQty = Helper.FormatThousand(header.QCExtends.Sum(i => i.Qty)),
                    //TotalDisposalQty = Helper.FormatThousand(header.QCPickings.Sum(i => i.DisposedQty))
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

            obj.Add("data", headerDTO);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePicking(string id)
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

            IEnumerable<QCPicking> list = Enumerable.Empty<QCPicking>();
            IEnumerable<QCPickingDTO> pagedData = Enumerable.Empty<QCPickingDTO>();

            IQueryable<QCPicking> query = null;

            int recordsTotal = 0;
            query = db.QCPickings.Where(s => s.QCInspectionID.Equals(id)).AsQueryable();

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {

                Dictionary<string, Func<QCPicking, object>> cols = new Dictionary<string, Func<QCPicking, object>>();
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("PickingMethod", x => x.PickedMethod);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("PickedOn", x => x.PickedOn);
                //cols.Add("PutawayBagQty", x => Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag));
                //cols.Add("OutstandingPutawayBagQty", x => Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag));


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new QCPickingDTO
                                {
                                    ID = x.ID,
                                    StockCode = x.StockCode,
                                    BinRackID = x.BinRackID,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    Qty = Helper.FormatThousand(x.Qty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.Qty / x.QtyPerBag)),
                                    PickingMethod = x.PickedMethod ?? "-",
                                    PickedBy = x.PickedBy ?? "-",
                                    PickedOn = Helper.NullDateTimeToString(x.PickedOn),
                                    PickingAction = string.IsNullOrEmpty(x.PickedMethod) ? true : false,
                                    //PutawayAction = !string.IsNullOrEmpty(x.PickedMethod) && Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag) > 0,
                                    //PutawayBagQty = Helper.FormatThousand(Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag)),
                                    //OutstandingPutawayBagQty = Helper.FormatThousand(Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag)),
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
        public async Task<IHttpActionResult> DatatableExtendPutaway(string InspectionId)
        {
            //int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
            //int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
            //int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
            //string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
            //string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
            //string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
            //string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            IEnumerable<QCPutaway> list = Enumerable.Empty<QCPutaway>();
            IEnumerable<QCPutawayDTO> pagedData = Enumerable.Empty<QCPutawayDTO>();

            int recordsTotal = 0;
            IQueryable<QCPutaway> query = db.QCPutaways.Where(m => m.QCPicking.QCInspectionID.Equals(InspectionId) && !m.PutawayQty.Equals(m.QCReturns.Sum(i => i.PutawayQty))).AsQueryable();

            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {

                Dictionary<string, Func<QCPutaway, object>> cols = new Dictionary<string, Func<QCPutaway, object>>();
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("TotalQty", x => x.PutawayQty);
                cols.Add("BagQty", x => x.PutawayQty / x.QtyPerBag);
                cols.Add("PickingMethod", x => x.PickedMethod);
                cols.Add("PickedBy", x => x.PickedBy);
                cols.Add("PickedOn", x => x.PickedOn);
                cols.Add("PutawayBagQty", x => x.PutawayQty);

                //cols.Add("PutawayBagQty", x => Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag));
                //cols.Add("OutstandingPutawayBagQty", x => Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag));


                //if (sortDirection.Equals("asc"))
                //    list = query.OrderBy(cols[sortName]);
                //else
                //    list = query.OrderByDescending(cols[sortName]);

                //recordsFiltered = list.Count();

                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from x in list
                                select new QCPutawayDTO
                                {
                                    ID = x.ID,
                                    StockCode = x.StockCode,
                                    BinRackID = x.BinRackID,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    PutawayQty = Helper.FormatThousand(x.PutawayQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.PutawayQty / x.QtyPerBag)),
                                    PickingMethod = x.PickedMethod ?? "-",
                                    PickedBy = x.PickedBy ?? "-",
                                    PickedOn = Helper.NullDateTimeToString(x.PickedOn),
                                    PickingAction = string.IsNullOrEmpty(x.PickedMethod) ? true : false,
                                    //PutawayAction = !string.IsNullOrEmpty(x.PickedMethod) && Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag) > 0,
                                    //PutawayBagQty = Helper.FormatThousand(Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag)),
                                    //OutstandingPutawayBagQty = Helper.FormatThousand(Convert.ToInt32(x.Qty / x.QtyPerBag) - Convert.ToInt32(x.QCInspection.QCPutaways.Where(s => s.StockCode.Equals(x.StockCode) && s.PrevBinRackID.Equals(x.BinRackID)).Sum(i => i.PutawayQty) / x.QtyPerBag)),
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

            //obj.Add("draw", draw);
            obj.Add("recordsTotal", recordsTotal);
            obj.Add("recordsFiltered", recordsFiltered);
            obj.Add("data", pagedData);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
        [HttpGet]
        public async Task<IHttpActionResult> GetPickingList(string id)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;
            List<QCPickingDTO> pickingList = null;

            try
            {

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception("Id is required.");
                }


                QCInspection header = await db.QCInspections.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();


                if (header == null)
                {
                    throw new Exception("Data not found.");
                }

                pickingList = new List<QCPickingDTO>();

                foreach (QCPicking picking in header.QCPickings)
                {
                    QCPickingDTO pickDTO = new QCPickingDTO();
                    pickDTO.ID = picking.ID;
                    pickDTO.InspectionID = picking.QCInspectionID;
                    pickDTO.StockCode = picking.StockCode;
                    pickDTO.BinRackCode = picking.BinRackCode;
                    pickDTO.BinRackName = picking.BinRackName;
                    pickDTO.Qty = Helper.FormatThousand(picking.Qty);
                    pickDTO.QtyPerBag = Helper.FormatThousand(picking.QtyPerBag);
                    pickDTO.BagQty = Helper.FormatThousand(Convert.ToInt32(picking.Qty / picking.QtyPerBag));
                    pickDTO.PickingMethod = picking.PickedMethod ?? "-";
                    pickDTO.PickedBy = picking.PickedBy ?? "-";
                    pickDTO.PickedOn = Helper.NullDateTimeToString(picking.PickedOn);
                    pickDTO.PutawayBagQty = Helper.FormatThousand(0);
                    pickDTO.OutstandingPutawayBagQty = Helper.FormatThousand(0);

                    pickingList.Add(pickDTO);
                }



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

            obj.Add("list", pickingList);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }
        [HttpPost]
        public async Task<IHttpActionResult> Picking(QCPickingVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.PickingID))
                    {
                        throw new Exception("PickingID is required.");
                    }

                    QCPicking picking = await db.QCPickings.Where(m => m.ID.Equals(dataVM.PickingID)).FirstOrDefaultAsync();
                    if (picking == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(picking.PickedMethod))
                        {
                            throw new Exception("Material is already picked.");
                        }
                    }

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

                    picking.PickedMethod = "MANUAL";
                    picking.PickedOn = transactionDate;
                    picking.PickedBy = activeUser;

                    if (picking.QCInspection.InspectionStatus == null || picking.QCInspection.InspectionStatus == "RETURN")
                    {
                        QCPutaway putaway = new QCPutaway();
                        putaway.ID = Helper.CreateGuid("QCp");
                        putaway.QCPickingID = picking.ID;
                        putaway.StockCode = picking.StockCode;
                        putaway.LotNo = picking.QCInspection.LotNo;
                        putaway.InDate = picking.QCInspection.InDate;
                        putaway.ExpDate = picking.QCInspection.ExpDate;
                        putaway.PrevBinRackID = picking.BinRackID;
                        putaway.PrevBinRackCode = picking.BinRackCode;
                        putaway.PrevBinRackName = picking.BinRackName;
                        putaway.BinRackID = picking.BinRackID;
                        putaway.BinRackCode = picking.BinRackCode;
                        putaway.BinRackName = picking.BinRackName;
                        putaway.PutawayQty = picking.Qty;
                        putaway.QtyPerBag = picking.QtyPerBag;
                        putaway.PutawayMethod = "MANUAL";
                        putaway.PutOn = DateTime.Now;
                        putaway.PutBy = activeUser;

                        db.QCPutaways.Add(putaway);
                    }

                    await db.SaveChangesAsync();

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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> DatatablePutaway(string id)
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

            IEnumerable<QCPutaway> list = Enumerable.Empty<QCPutaway>();
            IEnumerable<QCPutawayDTO> pagedData = Enumerable.Empty<QCPutawayDTO>();

            IQueryable<QCPutaway> query = null;

            int recordsTotal = 0;
            query = db.QCPutaways.Where(m => m.QCPicking.QCInspectionID.Equals(id) && !m.PutawayQty.Equals(m.QCReturns.Sum(i => i.PutawayQty))).AsQueryable();


            recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {

                Dictionary<string, Func<QCPutaway, object>> cols = new Dictionary<string, Func<QCPutaway, object>>();
                cols.Add("BinRackCode", x => x.BinRackCode);
                cols.Add("BinRackName", x => x.BinRackName);
                cols.Add("PutawayQty", x => x.PutawayQty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.PutawayQty / x.QtyPerBag);
                cols.Add("PutawayMethod", x => x.PutawayMethod);
                cols.Add("PutBy", x => x.PutBy);
                cols.Add("PutOn", x => x.PutOn);
                cols.Add("PickingMethod", x => x.PickedMethod);
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
                                select new QCPutawayDTO
                                {
                                    ID = x.ID,
                                    StockCode = x.StockCode,
                                    BinRackID = x.BinRackID,
                                    BinRackCode = x.BinRackCode,
                                    BinRackName = x.BinRackName,
                                    PutawayQty = Helper.FormatThousand(x.PutawayQty),
                                    QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(x.PutawayQty / x.QtyPerBag)),
                                    PutawayMethod = x.PutawayMethod ?? "-",
                                    PutBy = x.PutBy ?? "-",
                                    PutOn = Helper.NullDateTimeToString(x.PutOn),
                                    PickingMethod = x.PickedMethod ?? "-",
                                    PickedBy = x.PickedBy ?? "-",
                                    PickedOn = Helper.NullDateTimeToString(x.PickedOn),
                                    PutawayAction = x.PutawayQty != x.QCReturns.Sum(i => i.PutawayQty),

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

        //[HttpGet]
        //public async Task<IHttpActionResult> GetPutawayList(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    List<QCPutawayDTO> list = null;

        //    try
        //    {

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }


        //        QCInspection header = await db.QCInspections.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();


        //        if (header == null)
        //        {
        //            throw new Exception("Data not found.");
        //        }

        //        list = new List<QCPutawayDTO>();

        //        foreach (QCPutaway detail in header.QCPutaways)
        //        {
        //            QCPutawayDTO detailDTO = new QCPutawayDTO();
        //            detailDTO.ID = detail.ID;
        //            detailDTO.InspectionID = detail.QCInspectionID;
        //            detailDTO.StockCode = detail.StockCode;
        //            detailDTO.BinRackCode = detail.BinRackCode;
        //            detailDTO.BinRackName = detail.BinRackName;
        //            detailDTO.PutawayQty = Helper.FormatThousand(detail.PutawayQty);
        //            detailDTO.QtyPerBag = Helper.FormatThousand(detail.QtyPerBag);
        //            detailDTO.BagQty = Helper.FormatThousand(Convert.ToInt32(detail.PutawayQty / detail.QtyPerBag));
        //            detailDTO.PutawayMethod = detail.PutawayMethod ?? "-";
        //            detailDTO.PutBy = detail.PutBy ?? "-";
        //            detailDTO.PutOn = Helper.NullDateTimeToString(detail.PutOn);

        //            list.Add(detailDTO);
        //        }



        //        status = true;
        //        message = "Fetch data succeded.";
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

        //    obj.Add("list", list);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> Putaway(QCPutawayVM dataVM)
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

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            if (string.IsNullOrEmpty(dataVM.InspectionID))
        //            {
        //                throw new Exception("Inspection ID is required.");
        //            }

        //            if (string.IsNullOrEmpty(dataVM.StockCode))
        //            {
        //                throw new Exception("Stock is required.");
        //            }

        //            QCPicking picking = await db.QCPickings.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(dataVM.StockCode) && m.BinRackID.Equals(dataVM.PrevBinRackID)).FirstOrDefaultAsync();

        //            if (picking == null)
        //            {
        //                throw new Exception("Data is not recognized.");
        //            }

        //            if (string.IsNullOrEmpty(picking.PickedMethod))
        //            {
        //                throw new Exception("Material must be picked.");
        //            }

        //            BinRack binRack = null;
        //            if (string.IsNullOrEmpty(dataVM.BinRackID))
        //            {
        //                ModelState.AddModelError("QCInspection.BinRackID", "BinRack is required.");
        //            }
        //            else
        //            {
        //                binRack = await db.BinRacks.Where(m => m.ID.Equals(dataVM.BinRackID)).FirstOrDefaultAsync();
        //                if (binRack == null)
        //                {
        //                    ModelState.AddModelError("QCInspection.BinRackID", "BinRack is not recognized.");
        //                }
        //            }

        //            if (dataVM.BagQty <= 0)
        //            {
        //                ModelState.AddModelError("QCInspection.PutawayQTY", "Bag Qty can not be empty or below zero.");
        //            }
        //            else
        //            {
        //                decimal pickedQty = 0;
        //                decimal putQty = 0;
        //                try
        //                {
        //                    pickedQty = db.QCPickings.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID) && !string.IsNullOrEmpty(m.PickedMethod) && m.StockCode.Equals(picking.StockCode) && m.BinRackID.Equals(dataVM.PrevBinRackID)).Sum(m => m.Qty);
        //                }
        //                catch
        //                {

        //                }

        //                try
        //                {
        //                    putQty = db.QCPutaways.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(picking.StockCode) && m.PrevBinRackID.Equals(dataVM.PrevBinRackID)).Sum(m => m.PutawayQty);
        //                }
        //                catch
        //                {

        //                }

        //                decimal availableQty = pickedQty - putQty;
        //                int availableBagQty = Convert.ToInt32(availableQty / picking.QtyPerBag);
        //                if (dataVM.BagQty > availableBagQty)
        //                {
        //                    ModelState.AddModelError("QCInspection.PutawayQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
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

        //            //check quantity picked available

        //            TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
        //            DateTime now = DateTime.Now;
        //            DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

        //            QCPutaway putaway = new QCPutaway();
        //            putaway.ID = Helper.CreateGuid("QCp");
        //            putaway.QCInspectionID = picking.QCInspection.ID;
        //            putaway.StockCode = picking.StockCode;
        //            putaway.PrevBinRackID = picking.BinRackID;
        //            putaway.PrevBinRackCode = picking.BinRackCode;
        //            putaway.PrevBinRackName = picking.BinRackName;
        //            putaway.BinRackID = binRack.ID;
        //            putaway.BinRackCode = binRack.Code;
        //            putaway.BinRackName = binRack.Name;
        //            putaway.PutawayQty = dataVM.BagQty * picking.QtyPerBag;
        //            putaway.QtyPerBag = picking.QtyPerBag;
        //            putaway.PutawayMethod = "MANUAL";
        //            putaway.PutOn = transactionDate;
        //            putaway.PutBy = activeUser;

        //            db.QCPutaways.Add(putaway);


        //            picking.QCInspection.TransactionStatus = "PROGRESS";

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

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableQC(string id)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;


        //    IEnumerable<QCMaterialDTO> pagedData = Enumerable.Empty<QCMaterialDTO>();

        //    QCInspection header = await db.QCInspections.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();

        //    var stockList = header.QCPutaways.GroupBy(u => new { StockCode = u.StockCode, QtyPerBag = u.QtyPerBag }).Select(s => new { StockCode = s.Key.StockCode, QtyPerBag = s.Key.QtyPerBag, TotalQty = s.Sum(x => x.PutawayQty) }).ToList();

        //    int recordsTotal = 0;

        //    recordsTotal = stockList.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {



        //        stockList = stockList.Skip(start).Take(length).ToList();

        //        if (stockList != null && stockList.Count() > 0)
        //        {
        //            pagedData = from x in stockList
        //                        select new QCMaterialDTO
        //                        {
        //                            StockCode = x.StockCode,
        //                            TotalQty = Helper.FormatThousand(x.TotalQty),
        //                            QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
        //                            BagQty = Helper.FormatThousand(Convert.ToInt32(x.TotalQty / x.QtyPerBag)),
        //                            DisposalAction = header.TransactionStatus.Equals("OPEN") ? true : false
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpGet]
        //public async Task<IHttpActionResult> GetQCList(string id)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;
        //    List<QCMaterialDTO> materialList = null;

        //    try
        //    {

        //        if (string.IsNullOrEmpty(id))
        //        {
        //            throw new Exception("Id is required.");
        //        }


        //        QCInspection header = await db.QCInspections.Where(m => m.ID.Equals(id)).FirstOrDefaultAsync();


        //        if (header == null)
        //        {
        //            throw new Exception("Data not found.");
        //        }

        //        materialList = new List<QCMaterialDTO>();

        //        var stockList = header.QCPutaways.GroupBy(u => new { StockCode = u.StockCode, QtyPerBag = u.QtyPerBag }).Select(s => new { StockCode = s.Key.StockCode, QtyPerBag = s.Key.QtyPerBag, TotalQty = s.Sum(x => x.PutawayQty) }).ToList();

        //        foreach (var stkList in stockList)
        //        {
        //            QCMaterialDTO materialDTO = new QCMaterialDTO();
        //            materialDTO.StockCode = stkList.StockCode;
        //            materialDTO.TotalQty = Helper.FormatThousand(stkList.TotalQty);
        //            materialDTO.QtyPerBag = Helper.FormatThousand(stkList.QtyPerBag);
        //            materialDTO.BagQty = Helper.FormatThousand(Convert.ToInt32(stkList.TotalQty / stkList.QtyPerBag));
        //            materialDTO.DisposalAction = header.TransactionStatus.Equals("OPEN") ? true : false;

        //            materialList.Add(materialDTO);
        //        }

        //        status = true;
        //        message = "Fetch data succeded.";
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

        //    obj.Add("list", materialList);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableJudgement(string id)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<QCJudgement> list = Enumerable.Empty<QCJudgement>();
        //    IEnumerable<QCJudgementDTO> pagedData = Enumerable.Empty<QCJudgementDTO>();

        //    IQueryable<QCJudgement> query = null;

        //    int recordsTotal = 0;
        //    query = db.QCJudgements.Where(s => s.QCInspectionID.Equals(id)).AsQueryable();

        //    recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {

        //        Dictionary<string, Func<QCJudgement, object>> cols = new Dictionary<string, Func<QCJudgement, object>>();
        //        cols.Add("JudgedQty", x => x.JudgedQty);
        //        cols.Add("QtyPerBag", x => x.QtyPerBag);
        //        cols.Add("BagQty", x => x.JudgedQty / x.QtyPerBag);
        //        cols.Add("JudgedBy", x => x.JudgedBy);
        //        cols.Add("JudgedOn", x => x.JudgedOn);
        //        cols.Add("LotNo", x => x.LotNo);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpDate", x => x.ExpDate);
        //        cols.Add("NewExpDate", x => x.NewExpDate);

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from x in list
        //                        select new QCJudgementDTO
        //                        {
        //                            ID = x.ID,
        //                            StockCode = x.StockCode,
        //                            JudgedQty = Helper.FormatThousand(x.JudgedQty),
        //                            QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
        //                            LotNo = x.LotNo,
        //                            InDate = Helper.NullDateToString(x.InDate),
        //                            ExpDate = Helper.NullDateToString(x.ExpDate),
        //                            NewExpDate = Helper.NullDateToString(x.NewExpDate),
        //                            BagQty = Helper.FormatThousand(Convert.ToInt32(x.JudgedQty / x.QtyPerBag)),
        //                            JudgedBy = x.JudgedBy ?? "-",
        //                            JudgedOn = Helper.NullDateTimeToString(x.JudgedOn),
        //                            ReturnAction = true
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}



        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableDisposal(string id)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<QCDispose> list = Enumerable.Empty<QCDispose>();
        //    IEnumerable<QCDisposeDTO> pagedData = Enumerable.Empty<QCDisposeDTO>();

        //    IQueryable<QCDispose> query = null;

        //    int recordsTotal = 0;
        //    query = db.QCDisposes.Where(s => s.QCInspectionID.Equals(id)).AsQueryable();

        //    recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {

        //        Dictionary<string, Func<QCDispose, object>> cols = new Dictionary<string, Func<QCDispose, object>>();
        //        cols.Add("DisposeQty", x => x.DisposedQty);
        //        cols.Add("QtyPerBag", x => x.QtyPerBag);
        //        cols.Add("BagQty", x => x.DisposedQty / x.QtyPerBag);
        //        cols.Add("DisposedBy", x => x.DisposedBy);
        //        cols.Add("DisposedOn", x => x.DisposedOn);

        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from x in list
        //                        select new QCDisposeDTO
        //                        {
        //                            ID = x.ID,
        //                            DisposeQty = Helper.FormatThousand(x.DisposedQty),
        //                            QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
        //                            BagQty = Helper.FormatThousand(Convert.ToInt32(x.DisposedQty / x.QtyPerBag)),
        //                            DisposedBy = x.DisposedBy ?? "-",
        //                            DisposedOn = Helper.NullDateTimeToString(x.DisposedOn)
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}


        [HttpPost]
        public async Task<IHttpActionResult> Dispose(QCDisposeVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.InspectionID))
                    {
                        throw new Exception("Inspection ID is required.");
                    }

                    QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }

                    decimal pickingqty = db.QCPickings.Where(m => m.QCInspectionID.Equals(inspection.ID)).Sum(i => i.Qty);
                    decimal putawayqty = 0;

                    IQueryable<QCPutaway> query1 = query1 = db.QCPutaways.Where(m => m.StockCode.Contains(inspection.MaterialCode) && m.LotNo.Equals(inspection.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(m.ExpDate) == DbFunctions.TruncateTime(inspection.ExpDate) && m.PutawayMethod.Equals("SCAN")).AsQueryable();
                    foreach (QCPutaway putaway in query1)
                    {
                        BinRack binrackcode = await db.BinRacks.Where(s => s.Code.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (binrackcode.WarehouseCode.Equals("4001"))
                        {
                            putawayqty = putawayqty + putaway.PutawayQty;
                        }
                    }

                    //if (pickingqty != putawayqty)
                    //{
                    //    throw new Exception("Not allowed, make sure all materials have been picked and putaway to the QC location.");
                    //}

                    if (inspection.TransactionStatus.Equals("CONFIRMED") || inspection.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Not allowed, Inspection finished.");
                    }

                    if (!string.IsNullOrEmpty(inspection.InspectionStatus) && inspection.InspectionStatus.Equals("DISPOSE"))
                    {
                        throw new Exception("Dispose already done.");
                    }

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

                    inspection.TransactionStatus = "CONFIRMED";
                    inspection.InspectionStatus = "DISPOSE";
                    inspection.InspectedBy = activeUser;
                    inspection.InspectedOn = transactionDate;

                    //vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    //if (stockAll == null)
                    //{
                    //    throw new Exception("Stock tidak ditemukan.");
                    //}

                    //if (stockAll.Type.Equals("RM"))
                    //{
                    //    StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                    //    stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                    //}
                    //else if (stockAll.Type.Equals("SFG"))
                    //{
                    //    StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                    //    stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                    //}


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Dispose succeeded.";

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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> Judgement(QCJudgementVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.InspectionID))
                    {
                        throw new Exception("Inspection ID is required.");
                    }

                    QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }

                    decimal pickingqty = db.QCPickings.Where(m => m.QCInspectionID.Equals(inspection.ID)).Sum(i => i.Qty);
                    decimal putawayqty = 0;

                    IQueryable<QCPutaway> query1 = query1 = db.QCPutaways.Where(m => m.StockCode.Contains(inspection.MaterialCode) && m.LotNo.Equals(inspection.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(m.ExpDate) == DbFunctions.TruncateTime(inspection.ExpDate) && m.PutawayMethod.Equals("SCAN")).AsQueryable();
                    foreach (QCPutaway putaway in query1)
                    {
                        BinRack binrackcode = await db.BinRacks.Where(s => s.Code.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (binrackcode.WarehouseCode.Equals("4001"))
                        {
                            putawayqty = putawayqty + putaway.PutawayQty;
                        }
                    }

                    //if (pickingqty != putawayqty)
                    //{
                    //    throw new Exception("Not allowed, make sure all materials have been picked and putaway to the QC location.");
                    //}

                    if (inspection.TransactionStatus.Equals("CONFIRMED") || inspection.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Not allowed, Inspection finished.");
                    }

                    if (!string.IsNullOrEmpty(inspection.InspectionStatus) && inspection.InspectionStatus.Equals("EXTEND"))
                    {
                        throw new Exception("Judgement already done.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(inspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }


                    if (dataVM.ExtendQty <= 0)
                    {
                        ModelState.AddModelError("QCJudgement.ExtendQty", "Extend Qty can not below zero.");
                    }

                    if (string.IsNullOrEmpty(dataVM.ExtendRange))
                    {
                        ModelState.AddModelError("QCJudgement.ExtendQty", "Extend Range is required.");
                    }
                    else
                    {
                        if (!dataVM.ExtendRange.Equals("y") && !dataVM.ExtendRange.Equals("m") && !dataVM.ExtendRange.Equals("d"))
                        {
                            ModelState.AddModelError("QCJudgement.ExtendQty", "Extend Range is not recognized.");
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


                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);


                    int ShelfLife = dataVM.ExtendQty;
                    int days = 0;

                    string LifeRange = Regex.Replace(dataVM.ExtendRange, @"[\d-]", string.Empty).ToString();


                    if (LifeRange.ToLower().Contains("y"))
                    {
                        days = (ShelfLife * (Convert.ToInt32(12 * 30))) - 1;
                    }
                    else if (LifeRange.ToLower().Contains("m"))
                    {
                        days = (Convert.ToInt32(ShelfLife * 30)) - 1;
                    }
                    else
                    {
                        days = ShelfLife - 1;
                    }

                    DateTime newExpiredDate = inspection.ExpDate.AddDays(days);

                    inspection.NewExpDate = newExpiredDate;
                    inspection.TransactionStatus = "CONFIRMED";
                    inspection.InspectionStatus = "EXTEND";
                    inspection.InspectedBy = activeUser;
                    inspection.InspectedOn = DateTime.Now;

                    IEnumerable<QCPicking> pickings = inspection.QCPickings.ToList();
                    foreach (QCPicking picking in pickings)
                    {
                        //loop from qc picking, insert qc putaway
                        int bagQty = Convert.ToInt32(picking.Qty / picking.QtyPerBag);
                        int putBagQty = Convert.ToInt32(picking.QCPutaways.Sum(i => i.PutawayQty / i.QtyPerBag));

                        int availableBagQty = bagQty - putBagQty;


                        if (availableBagQty > 0)
                        {
                            decimal availableQty = availableBagQty * picking.QtyPerBag;
                            QCPutaway putaway = new QCPutaway();
                            putaway.ID = Helper.CreateGuid("QCp");
                            putaway.QCPickingID = picking.ID;
                            putaway.StockCode = Helper.StockCode(inspection.MaterialCode, picking.QtyPerBag, inspection.LotNo, inspection.InDate, inspection.ExpDate);
                            putaway.NewStockCode = Helper.StockCode(inspection.MaterialCode, picking.QtyPerBag, inspection.LotNo, inspection.InDate, newExpiredDate);
                            putaway.LotNo = inspection.LotNo;
                            putaway.InDate = inspection.InDate;
                            putaway.ExpDate = inspection.ExpDate;
                            putaway.NewExpDate = newExpiredDate;
                            putaway.PrevBinRackID = picking.BinRackID;
                            putaway.PrevBinRackCode = picking.BinRackCode;
                            putaway.PrevBinRackName = picking.BinRackName;
                            putaway.BinRackID = picking.BinRackID;
                            putaway.BinRackCode = picking.BinRackCode;
                            putaway.BinRackName = picking.BinRackName;
                            putaway.PutawayQty = availableQty;
                            putaway.QtyPerBag = picking.QtyPerBag;
                            putaway.PutawayMethod = "INSPECT";
                            putaway.PutOn = DateTime.Now;
                            putaway.PutBy = activeUser;

                            db.QCPutaways.Add(putaway);
                        }

                        IQueryable<QCPutaway> putaways = db.QCPutaways.Where(m => m.QCPickingID.Equals(picking.ID)).AsQueryable();
                        foreach (QCPutaway putaway in putaways)
                        {
                            string StockCode = Helper.StockCode(inspection.MaterialCode, putaway.QtyPerBag, inspection.LotNo, inspection.InDate, newExpiredDate);

                            putaway.NewExpDate = newExpiredDate;
                            putaway.NewStockCode = StockCode;
                        }
                    }


                    //update in stock
                    //decrease stock
                    //create new row

                    //if (vProductMaster.ProdType.Equals("RM"))
                    //{
                    //    IQueryable<StockRM> query = query = db.StockRMs.Where(m => m.MaterialCode.Equals(inspection.MaterialCode) && m.LotNumber.Equals(inspection.LotNo) && m.InDate.Value.Equals(inspection.InDate) && m.ExpiredDate.Value.Equals(inspection.ExpDate) && m.Quantity > 0).AsQueryable();
                    //    foreach (StockRM stock in query)
                    //    {
                    //        string StockCode = Helper.StockCode(inspection.MaterialCode, stock.QtyPerBag, inspection.LotNo, inspection.InDate, newExpiredDate);

                    //        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(stock.BinRackCode)).FirstOrDefaultAsync();
                    //        if (stockRM != null)
                    //        {
                    //            stockRM.Quantity += stock.Quantity;
                    //        }
                    //        else
                    //        {
                    //            stockRM = new StockRM();
                    //            stockRM.ID = Helper.CreateGuid("S");
                    //            stockRM.MaterialCode = vProductMaster.MaterialCode;
                    //            stockRM.MaterialName = vProductMaster.MaterialName;
                    //            stockRM.Code = StockCode;
                    //            stockRM.LotNumber = inspection.LotNo;
                    //            stockRM.InDate = inspection.InDate;
                    //            stockRM.ExpiredDate = newExpiredDate;
                    //            stockRM.Quantity = stock.Quantity;
                    //            stockRM.QtyPerBag = stock.QtyPerBag;
                    //            stockRM.BinRackID = stock.BinRackID;
                    //            stockRM.BinRackCode = stock.BinRackCode;
                    //            stockRM.BinRackName = stock.BinRackName;
                    //            stockRM.ReceivedAt = DateTime.Now;
                    //        }

                    //        db.StockRMs.Add(stockRM);

                    //        stock.Quantity = 0;
                    //    }

                    //}
                    //else
                    //{
                    //    IQueryable<StockSFG> query = query = db.StockSFGs.Where(m => m.MaterialCode.Equals(inspection.MaterialCode) && m.LotNumber.Equals(inspection.LotNo) && m.InDate.Value.Equals(inspection.InDate) && m.ExpiredDate.Value.Equals(inspection.ExpDate) && m.Quantity > 0).AsQueryable();
                    //    foreach (StockSFG stock in query)
                    //    {
                    //        string StockCode = Helper.StockCode(inspection.MaterialCode, stock.QtyPerBag, inspection.LotNo, inspection.InDate, newExpiredDate);

                    //        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(stock.BinRackCode)).FirstOrDefaultAsync();
                    //        if (stockSFG != null)
                    //        {
                    //            stockSFG.Quantity += stock.Quantity;
                    //        }
                    //        else
                    //        {
                    //            stockSFG = new StockSFG();
                    //            stockSFG.ID = Helper.CreateGuid("S");
                    //            stockSFG.MaterialCode = vProductMaster.MaterialCode;
                    //            stockSFG.MaterialName = vProductMaster.MaterialName;
                    //            stockSFG.Code = StockCode;
                    //            stockSFG.LotNumber = inspection.LotNo;
                    //            stockSFG.InDate = inspection.InDate;
                    //            stockSFG.ExpiredDate = newExpiredDate;
                    //            stockSFG.Quantity = stock.Quantity;
                    //            stockSFG.QtyPerBag = stock.QtyPerBag;
                    //            stockSFG.BinRackID = stock.BinRackID;
                    //            stockSFG.BinRackCode = stock.BinRackCode;
                    //            stockSFG.BinRackName = stock.BinRackName;
                    //            stockSFG.ReceivedAt = DateTime.Now;
                    //        }

                    //        db.StockSFGs.Add(stockSFG);

                    //        stock.Quantity = 0;
                    //    }
                    //}

                    //insert to table QCExtend
                    var stockList = inspection.QCPickings.GroupBy(u => new { Qty = u.Qty, QtyPerBag = u.QtyPerBag }).Select(s => new { QtyPerBag = s.Key.QtyPerBag, TotalQty = s.Sum(x => x.Qty) }).ToList();
                    foreach (var stk in stockList)
                    {
                        QCExtend qCExtend = new QCExtend();
                        qCExtend.ID = Helper.CreateGuid("QCe");
                        qCExtend.QCInspectionID = inspection.ID;
                        qCExtend.StockCode = Helper.StockCode(inspection.MaterialCode, stk.QtyPerBag, inspection.LotNo, inspection.InDate, newExpiredDate);
                        qCExtend.LotNo = inspection.LotNo;
                        qCExtend.InDate = inspection.InDate;
                        qCExtend.ExpDate = newExpiredDate;
                        qCExtend.QtyPerBag = stk.QtyPerBag;
                        qCExtend.Qty = stk.TotalQty;
                        qCExtend.LastSeries = 0;

                        db.QCExtends.Add(qCExtend);
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Judgement succeeded.";
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
        public async Task<IHttpActionResult> Return(QCReturnVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.InspectionID))
                    {
                        throw new Exception("Inspection ID is required.");
                    }

                    QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }

                    decimal pickingqty = db.QCPickings.Where(m => m.QCInspectionID.Equals(inspection.ID)).Sum(i => i.Qty);
                    decimal putawayqty = 0;

                    IQueryable<QCPutaway> query1 = query1 = db.QCPutaways.Where(m => m.StockCode.Contains(inspection.MaterialCode) && m.LotNo.Equals(inspection.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(m.ExpDate) == DbFunctions.TruncateTime(inspection.ExpDate) && m.PutawayMethod.Equals("SCAN")).AsQueryable();
                    foreach (QCPutaway putaway in query1)
                    {
                        BinRack binrackcode = await db.BinRacks.Where(s => s.Code.Equals(putaway.BinRackCode)).FirstOrDefaultAsync();
                        if (binrackcode.WarehouseCode.Equals("4001"))
                        {
                            putawayqty = putawayqty + putaway.PutawayQty;
                        }
                    }

                    if (putawayqty <= 0)
                    {
                        throw new Exception("Not allowed, pastikan ada material yang dipicking dan putaway sebagai sample ke lokasi QC.");
                    }

                    if (inspection.TransactionStatus.Equals("CONFIRMED") || inspection.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Not allowed, Inspection finished.");
                    }

                    if (!string.IsNullOrEmpty(inspection.InspectionStatus) && inspection.InspectionStatus.Equals("RETURN"))
                    {
                        throw new Exception("Return already done.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(inspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    if (dataVM.ReturnQty <= 0)
                    {
                        ModelState.AddModelError("QCInspection.ReturnQty", "Return Qty can not below zero.");
                    }

                    if (string.IsNullOrEmpty(dataVM.ReturnType))
                    {
                        ModelState.AddModelError("QCInspection.ReturnQty", "Return Type is required.");
                    }
                    else
                    {
                        if (!dataVM.ReturnType.Equals("bag") && !dataVM.ReturnType.Equals("reminder"))
                        {
                            ModelState.AddModelError("QCInspection.ReturnQty", "Return is not recognized.");
                        }
                    }

                    decimal totalQty = 0;
                    int fullBag = 0;
                    if (dataVM.ReturnType.Equals("bag"))
                    {
                        totalQty = dataVM.ReturnQty * vProductMaster.QtyPerBag;
                        fullBag = dataVM.ReturnQty;
                    }
                    else
                    {
                        totalQty = dataVM.ReturnQty;
                    }

                    decimal stockqty = db.vStockAlls.Where(s => s.MaterialCode.Equals(inspection.MaterialCode) && s.LotNumber.Equals(inspection.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(s.ExpiredDate) == DbFunctions.TruncateTime(inspection.ExpDate) && s.Quantity > 0 && s.OnInspect == true).Sum(i => i.Quantity);
                    if (totalQty > stockqty)
                    {
                        ModelState.AddModelError("QCInspection.ReturnQty", string.Format("Total Qty exceeded. Available Qty : {0}", Helper.FormatThousand(stockqty)));
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

                    inspection.TransactionStatus = "CONFIRMED";
                    inspection.InspectionStatus = "RETURN";
                    inspection.InspectedBy = activeUser;
                    inspection.InspectedOn = DateTime.Now;

                    QCPutaway getputaway = await db.QCPutaways.Where(m => m.StockCode.Contains(inspection.MaterialCode) && m.LotNo.Equals(inspection.LotNo) && DbFunctions.TruncateTime(m.InDate) == DbFunctions.TruncateTime(inspection.InDate) && DbFunctions.TruncateTime(m.ExpDate) == DbFunctions.TruncateTime(inspection.ExpDate) && m.PutawayMethod.Equals("SCAN")).FirstOrDefaultAsync();

                    QCReturn qCReturn = new QCReturn();
                    qCReturn.ID = Helper.CreateGuid("QCe");
                    qCReturn.QCPutawayID = getputaway.ID;
                    qCReturn.StockCode = getputaway.StockCode;
                    qCReturn.NewStockCode = Helper.StockCode(inspection.MaterialCode, vProductMaster.QtyPerBag, inspection.LotNo, inspection.InDate, inspection.ExpDate);
                    qCReturn.LotNo = inspection.LotNo;
                    qCReturn.InDate = inspection.InDate;
                    qCReturn.ExpDate = inspection.ExpDate;
                    qCReturn.NewExpDate = inspection.ExpDate;
                    qCReturn.PrevBinRackID = getputaway.PrevBinRackID;
                    qCReturn.PrevBinRackCode = getputaway.PrevBinRackCode;
                    qCReturn.PrevBinRackName = getputaway.PrevBinRackName;
                    qCReturn.BinRackID = getputaway.BinRackID;
                    qCReturn.BinRackCode = getputaway.BinRackCode;
                    qCReturn.BinRackName = getputaway.BinRackName;
                    qCReturn.PutawayQty = fullBag * vProductMaster.QtyPerBag;
                    qCReturn.QtyPerBag = vProductMaster.QtyPerBag;
                    qCReturn.PutawayMethod = "INSPECT";
                    qCReturn.PutOn = DateTime.Now;
                    qCReturn.PutBy = activeUser;

                    int startSeries = 0;
                    int lastSeries = await db.LogPrintRMs.Where(m => m.StockCode.Equals(qCReturn.NewStockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                    if (lastSeries == 0)
                    {
                        startSeries = 1;
                    }
                    else
                    {
                        startSeries = lastSeries + 1;
                    }

                    lastSeries = startSeries + fullBag - 1;

                    db.QCReturns.Add(qCReturn);

                    if (lastSeries > 0)
                    {
                        //add to Log Print RM
                        LogPrintRM logPrintRM = new LogPrintRM();
                        logPrintRM.ID = Helper.CreateGuid("LOG");
                        logPrintRM.Remarks = "QC Return";
                        logPrintRM.StockCode = qCReturn.NewStockCode;
                        logPrintRM.MaterialCode = inspection.MaterialCode;
                        logPrintRM.MaterialName = inspection.MaterialName;
                        logPrintRM.LotNumber = inspection.LotNo;
                        logPrintRM.InDate = inspection.InDate;
                        logPrintRM.ExpiredDate = inspection.ExpDate;
                        logPrintRM.StartSeries = startSeries;
                        logPrintRM.LastSeries = lastSeries;
                        logPrintRM.PrintDate = DateTime.Now;

                        db.LogPrintRMs.Add(logPrintRM);
                    }

                    decimal RemainderQty = totalQty % vProductMaster.QtyPerBag;
                    if (RemainderQty > 0)
                    {
                        QCReturn qCReturn1 = new QCReturn();
                        qCReturn1.ID = Helper.CreateGuid("QCe");
                        qCReturn1.QCPutawayID = getputaway.ID;
                        qCReturn1.StockCode = getputaway.StockCode;
                        qCReturn1.NewStockCode = Helper.StockCode(inspection.MaterialCode, RemainderQty, inspection.LotNo, inspection.InDate, inspection.ExpDate);
                        qCReturn1.LotNo = inspection.LotNo;
                        qCReturn1.InDate = inspection.InDate;
                        qCReturn1.ExpDate = inspection.ExpDate;
                        qCReturn1.NewExpDate = inspection.ExpDate;
                        qCReturn1.PrevBinRackID = getputaway.PrevBinRackID;
                        qCReturn1.PrevBinRackCode = getputaway.PrevBinRackCode;
                        qCReturn1.PrevBinRackName = getputaway.PrevBinRackName;
                        qCReturn1.BinRackID = getputaway.BinRackID;
                        qCReturn1.BinRackCode = getputaway.BinRackCode;
                        qCReturn1.BinRackName = getputaway.BinRackName;
                        qCReturn1.PutawayQty = RemainderQty;
                        qCReturn1.QtyPerBag = RemainderQty;
                        qCReturn1.PutawayMethod = "INSPECT";
                        qCReturn1.PutOn = DateTime.Now;
                        qCReturn1.PutBy = activeUser;

                        int startSeries1 = 0;
                        int lastSeries1 = await db.LogPrintRMs.Where(m => m.StockCode.Equals(qCReturn1.NewStockCode)).OrderByDescending(m => m.LastSeries).Select(m => m.LastSeries).FirstOrDefaultAsync();
                        if (lastSeries1 == 0)
                        {
                            startSeries1 = 1;
                        }
                        else
                        {
                            startSeries1 = lastSeries1 + 1;
                        }

                        lastSeries1 = startSeries1;

                        db.QCReturns.Add(qCReturn1);

                        if (lastSeries1 > 0)
                        {
                            //add to Log Print RM
                            LogPrintRM logPrintRM1 = new LogPrintRM();
                            logPrintRM1.ID = Helper.CreateGuid("LOG");
                            logPrintRM1.Remarks = "QC Return";
                            logPrintRM1.StockCode = qCReturn1.NewStockCode;
                            logPrintRM1.MaterialCode = inspection.MaterialCode;
                            logPrintRM1.MaterialName = inspection.MaterialName;
                            logPrintRM1.LotNumber = inspection.LotNo;
                            logPrintRM1.InDate = inspection.InDate;
                            logPrintRM1.ExpiredDate = inspection.ExpDate;
                            logPrintRM1.StartSeries = startSeries1;
                            logPrintRM1.LastSeries = lastSeries1;
                            logPrintRM1.PrintDate = DateTime.Now;

                            db.LogPrintRMs.Add(logPrintRM1);
                        }
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
        //[HttpPost]
        //public async Task<IHttpActionResult> PickingDispose(QCPickingDisposeVM dataVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();

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

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            if (string.IsNullOrEmpty(dataVM.PutawayID))
        //            {
        //                throw new Exception("PutawayID is required.");
        //            }

        //            QCPutaway picking = await db.QCPutaways.Where(m => m.ID.Equals(dataVM.PutawayID)).FirstOrDefaultAsync();
        //            if (picking == null)
        //            {
        //                throw new Exception("Data is not recognized.");
        //            }
        //            else
        //            {
        //                if (!string.IsNullOrEmpty(picking.PickedMethod))
        //                {
        //                    throw new Exception("Material is already picked.");
        //                }
        //            }

        //            TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
        //            DateTime now = DateTime.Now;
        //            DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

        //            picking.PickedMethod = "MANUAL";
        //            picking.PickedOn = transactionDate;
        //            picking.PickedBy = activeUser;


        //            await db.SaveChangesAsync();

        //            int count = db.QCPutaways.Where(m => m.QCInspectionID.Equals(picking.QCInspectionID) && string.IsNullOrEmpty(m.PickedMethod)).Count();
        //            if(count == 0)
        //            {
        //                QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(picking.QCInspectionID)).FirstOrDefaultAsync();
        //                inspection.TransactionStatus = "CLOSED";
        //                await db.SaveChangesAsync();
        //            }

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

        //    return Ok(obj);
        //}

        ////[HttpPost]
        ////public async Task<IHttpActionResult> Dispose(QCDisposeVM dataVM)
        ////{
        ////    Dictionary<string, object> obj = new Dictionary<string, object>();
        ////    List<CustomValidationMessage> customValidationMessages = new List<CustomValidationMessage>();

        ////    string message = "";
        ////    bool status = false;
        ////    var re = Request;
        ////    var headers = re.Headers;

        ////    try
        ////    {
        ////        string token = "";

        ////        if (headers.Contains("token"))
        ////        {
        ////            token = headers.GetValues("token").First();
        ////        }

        ////        string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

        ////        if (activeUser != null)
        ////        {
        ////            if (string.IsNullOrEmpty(dataVM.InspectionID))
        ////            {
        ////                throw new Exception("Inspection ID is required.");
        ////            }

        ////            QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

        ////            if (inspection == null)
        ////            {
        ////                throw new Exception("Data is not recognized.");
        ////            }

        ////            if (inspection.QCJudgements.Count() > 0)
        ////            {
        ////                throw new Exception("Disposal not allowed, judgement already done.");
        ////            }

        ////            if (string.IsNullOrEmpty(dataVM.StockCode))
        ////            {
        ////                throw new Exception("Stock is required.");
        ////            }

        ////            var stock = db.QCPutaways.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(dataVM.StockCode))
        ////            .GroupBy(u => new { StockCode = u.StockCode, QtyPerBag = u.QtyPerBag }).Select(s => new { StockCode = s.Key.StockCode, QtyPerBag = s.Key.QtyPerBag, TotalQty = s.Sum(x => x.PutawayQty) }).FirstOrDefaultAsync();

        ////            if (stock.Result == null)
        ////            {
        ////                throw new Exception("Data not found.");
        ////            }


        ////            if (!ModelState.IsValid)
        ////            {
        ////                foreach (var state in ModelState)
        ////                {
        ////                    string field = state.Key.Split('.')[1];
        ////                    string value = state.Value.Errors.Select(x => x.ErrorMessage).ToArray()[0];
        ////                    customValidationMessages.Add(new CustomValidationMessage(field, value));
        ////                }

        ////                throw new Exception("Input is not valid");
        ////            }

        ////            //check quantity picked available

        ////            TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
        ////            DateTime now = DateTime.Now;
        ////            DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

        ////            QCDispose dispose = new QCDispose();
        ////            dispose.ID = Helper.CreateGuid("QCd");
        ////            dispose.QCInspectionID = dataVM.InspectionID;
        ////            dispose.StockCode = stock.Result.StockCode;
        ////            dispose.DisposedQty = dataVM.BagQty * stock.Result.QtyPerBag;
        ////            dispose.QtyPerBag = stock.Result.QtyPerBag;
        ////            dispose.DisposedBy = activeUser;
        ////            dispose.DisposedOn = transactionDate;

        ////            db.QCDisposes.Add(dispose);

        ////            await db.SaveChangesAsync();

        ////            status = true;
        ////            message = "Dispose succeeded.";

        ////        }
        ////        else
        ////        {
        ////            message = "Token is no longer valid. Please re-login.";
        ////        }
        ////    }
        ////    catch (HttpRequestException reqpEx)
        ////    {
        ////        message = reqpEx.Message;
        ////    }
        ////    catch (HttpResponseException respEx)
        ////    {
        ////        message = respEx.Message;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        message = ex.Message;
        ////    }

        ////    obj.Add("status", status);
        ////    obj.Add("message", message);
        ////    obj.Add("error_validation", customValidationMessages);

        ////    return Ok(obj);
        ////}

        //[HttpPost]
        //public async Task<IHttpActionResult> Revert(QCRevertVM dataVM)
        //{
        //    Dictionary<string, object> obj = new Dictionary<string, object>();

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

        //        string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

        //        if (activeUser != null)
        //        {
        //            if (string.IsNullOrEmpty(dataVM.InspectionID))
        //            {
        //                throw new Exception("Inspection ID is required.");
        //            }

        //            QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

        //            if (inspection == null)
        //            {
        //                throw new Exception("Data is not recognized.");
        //            }

        //            if (!inspection.TransactionStatus.Equals("CONFIRMED"))
        //            {
        //                throw new Exception("Disposal not allowed, judgement already done.");
        //            }


        //            QCDispose dispose = await db.QCDisposes.Where(m => m.ID.Equals(dataVM.DisposeID)).FirstOrDefaultAsync();

        //            db.QCDisposes.Remove(dispose);

        //            await db.SaveChangesAsync();

        //            status = true;
        //            message = "Revert Disposal succeeded.";

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

        //    return Ok(obj);
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> DatatableReturn(string id)
        //{
        //    int draw = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("draw")[0]);
        //    int start = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("start")[0]);
        //    int length = Convert.ToInt32(HttpContext.Current.Request.Form.GetValues("length")[0]);
        //    string search = HttpContext.Current.Request.Form.GetValues("search[value]")[0];
        //    string orderCol = HttpContext.Current.Request.Form.GetValues("order[0][column]")[0];
        //    string sortName = HttpContext.Current.Request.Form.GetValues("columns[" + orderCol + "][name]")[0];
        //    string sortDirection = HttpContext.Current.Request.Form.GetValues("order[0][dir]")[0];

        //    Dictionary<string, object> obj = new Dictionary<string, object>();
        //    string message = "";
        //    bool status = false;
        //    HttpRequest request = HttpContext.Current.Request;

        //    IEnumerable<QCReturn> list = Enumerable.Empty<QCReturn>();
        //    IEnumerable<QCReturnDTO> pagedData = Enumerable.Empty<QCReturnDTO>();

        //    IQueryable<QCReturn> query = null;

        //    int recordsTotal = 0;
        //    query = db.QCReturns.Where(s => s.QCInspectionID.Equals(id)).AsQueryable();

        //    recordsTotal = query.Count();
        //    int recordsFiltered = 0;

        //    try
        //    {
        //        Dictionary<string, Func<QCReturn, object>> cols = new Dictionary<string, Func<QCReturn, object>>();
        //        cols.Add("ReturnQty", x => x.PutawayQty);
        //        cols.Add("QtyPerBag", x => x.QtyPerBag);
        //        cols.Add("BagQty", x => x.PutawayQty / x.QtyPerBag);
        //        cols.Add("ReturnBy", x => x.PutBy);
        //        cols.Add("ReturnOn", x => x.PutOn);
        //        cols.Add("LotNo", x => x.LotNo);
        //        cols.Add("InDate", x => x.InDate);
        //        cols.Add("ExpDate", x => x.ExpDate);
        //        cols.Add("NewExpDate", x => x.NewExpDate);


        //        if (sortDirection.Equals("asc"))
        //            list = query.OrderBy(cols[sortName]);
        //        else
        //            list = query.OrderByDescending(cols[sortName]);

        //        recordsFiltered = list.Count();

        //        list = list.Skip(start).Take(length).ToList();

        //        if (list != null && list.Count() > 0)
        //        {
        //            pagedData = from x in list
        //                        select new QCReturnDTO
        //                        {
        //                            ID = x.ID,
        //                            StockCode = x.StockCode,
        //                            ReturnQty = Helper.FormatThousand(x.PutawayQty),
        //                            QtyPerBag = Helper.FormatThousand(x.QtyPerBag),
        //                            BagQty = Helper.FormatThousand(Convert.ToInt32(x.PutawayQty / x.QtyPerBag)),
        //                            ReturnBy = x.PutBy?? "-",
        //                            ReturnOn = Helper.NullDateTimeToString(x.PutOn),
        //                            LotNo = x.LotNo,
        //                            InDate = Helper.NullDateToString(x.InDate),
        //                            ExpDate = Helper.NullDateToString(x.ExpDate),
        //                            NewExpDate = Helper.NullDateToString(x.NewExpDate),
        //                        };
        //        }

        //        status = true;
        //        message = "Fetch data succeeded.";
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

        //    obj.Add("draw", draw);
        //    obj.Add("recordsTotal", recordsTotal);
        //    obj.Add("recordsFiltered", recordsFiltered);
        //    obj.Add("data", pagedData);
        //    obj.Add("status", status);
        //    obj.Add("message", message);

        //    return Ok(obj);
        //}


        [HttpPost]
        public async Task<IHttpActionResult> ReturnPutaway(QCReturnVM dataVM)
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

                string activeUser = await db.Users.Where(x => x.Token.Equals(token) && x.IsActive).Select(x => x.Username).FirstOrDefaultAsync();

                if (activeUser != null)
                {
                    if (string.IsNullOrEmpty(dataVM.InspectionID))
                    {
                        throw new Exception("Inspection ID is required.");
                    }

                    QCInspection inspection = await db.QCInspections.Where(m => m.ID.Equals(dataVM.InspectionID)).FirstOrDefaultAsync();

                    if (inspection == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }


                    if (string.IsNullOrEmpty(dataVM.StockCode))
                    {
                        throw new Exception("Stock is required.");
                    }

                    int judgeCount = db.QCExtends.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID)).Count();
                    if (judgeCount < 1)
                    {
                        throw new Exception("Return not allowed, inspection not done.");
                    }

                    QCPutaway putaway = await db.QCPutaways.Where(m => m.QCPicking.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(dataVM.StockCode) && m.BinRackID.Equals(dataVM.PrevBinRackID)).FirstOrDefaultAsync();

                    if (putaway == null)
                    {
                        throw new Exception("Data is not recognized.");
                    }


                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(dataVM.BinRackID))
                    {
                        ModelState.AddModelError("QCInspection.ReturnBinRackID", "BinRack is required.");
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.ID.Equals(dataVM.BinRackID)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            ModelState.AddModelError("QCInspection.ReturnBinRackID", "BinRack is not recognized.");
                        }
                    }

                    if (dataVM.BagQty <= 0)
                    {
                        ModelState.AddModelError("QCInspection.ReturnQTY", "Bag Qty can not be empty or below zero.");
                    }
                    else
                    {
                        decimal putQty = 0;
                        decimal returnQty = 0;
                        try
                        {
                            putQty = db.QCPutaways.Where(m => m.QCPicking.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(putaway.StockCode) && m.BinRackID.Equals(dataVM.PrevBinRackID)).Sum(m => m.PutawayQty);
                        }
                        catch
                        {

                        }

                        try
                        {
                            returnQty = db.QCReturns.Where(m => m.QCPutaway.QCPicking.QCInspectionID.Equals(dataVM.InspectionID) && m.StockCode.Equals(putaway.StockCode) && m.BinRackID.Equals(dataVM.PrevBinRackID)).Sum(m => m.PutawayQty);
                        }
                        catch
                        {

                        }

                        decimal availableQty = putQty - returnQty;
                        int availableBagQty = Convert.ToInt32(availableQty / putaway.QtyPerBag);
                        if (dataVM.BagQty > availableBagQty)
                        {
                            ModelState.AddModelError("QCInspection.ReturnQTY", string.Format("Bag Qty exceeded. Available Bag Qty : {0}", availableBagQty));
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

                    QCExtend judgement = await db.QCExtends.Where(m => m.QCInspectionID.Equals(inspection.ID)).FirstOrDefaultAsync();
                    //check quantity picked available

                    TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                    DateTime now = DateTime.Now;
                    DateTime transactionDate = new DateTime(tokenDate.LoginDate.Year, tokenDate.LoginDate.Month, tokenDate.LoginDate.Day, now.Hour, now.Minute, now.Second);

                    DateTime NewExpiredDate = judgement.QCInspection.NewExpDate.Value;
                    string StockCode = string.Format("{0}{1}{2}{3}{4}", inspection.MaterialCode, Helper.FormatThousand(judgement.QtyPerBag), inspection.LotNo, inspection.InDate.ToString("yyyyMMdd").Substring(1), NewExpiredDate.ToString("yyyyMMdd").Substring(2));

                    BinRack prevBinRack = await db.BinRacks.Where(m => m.ID.Equals(dataVM.PrevBinRackID)).FirstOrDefaultAsync();

                    QCReturn retur = new QCReturn();
                    retur.ID = Helper.CreateGuid("QCr");
                    retur.QCPutawayID = putaway.ID;
                    retur.StockCode = judgement.StockCode;
                    retur.NewStockCode = StockCode;
                    retur.BinRackID = binRack.ID;
                    retur.BinRackCode = binRack.Code;
                    retur.BinRackName = binRack.Name;
                    retur.PutawayQty = dataVM.BagQty * judgement.QtyPerBag;
                    retur.QtyPerBag = judgement.QtyPerBag;
                    retur.PutawayMethod = "MANUAL";
                    retur.PutOn = transactionDate;
                    retur.PutBy = activeUser;
                    retur.PrevBinRackID = prevBinRack.ID;
                    retur.PrevBinRackCode = prevBinRack.Code;
                    retur.PrevBinRackName = prevBinRack.Name;


                    retur.LotNo = judgement.LotNo;
                    retur.InDate = judgement.InDate;
                    retur.ExpDate = judgement.ExpDate;
                    retur.NewExpDate = judgement.QCInspection.NewExpDate.Value;

                    db.QCReturns.Add(retur);



                    //update to stock
                    if (inspection.MaterialType.Equals("RM"))
                    {
                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(retur.BinRackCode)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += retur.PutawayQty;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = inspection.MaterialCode;
                            stockRM.MaterialName = inspection.MaterialName;
                            stockRM.Code = StockCode;
                            stockRM.LotNumber = inspection.LotNo;
                            stockRM.InDate = inspection.InDate;
                            stockRM.ExpiredDate = NewExpiredDate;
                            stockRM.Quantity = retur.PutawayQty;
                            stockRM.QtyPerBag = retur.QtyPerBag;
                            stockRM.BinRackID = retur.BinRackID;
                            stockRM.BinRackCode = retur.BinRackCode;
                            stockRM.BinRackName = retur.BinRackName;
                            stockRM.ReceivedAt = retur.PutOn;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(retur.BinRackCode)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += retur.PutawayQty;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = inspection.MaterialCode;
                            stockSFG.MaterialName = inspection.MaterialName;
                            stockSFG.Code = StockCode;
                            stockSFG.LotNumber = inspection.LotNo;
                            stockSFG.InDate = inspection.InDate;
                            stockSFG.ExpiredDate = NewExpiredDate;
                            stockSFG.Quantity = retur.PutawayQty;
                            stockSFG.QtyPerBag = retur.QtyPerBag;
                            stockSFG.BinRackID = retur.BinRackID;
                            stockSFG.BinRackCode = retur.BinRackCode;
                            stockSFG.BinRackName = retur.BinRackName;
                            stockSFG.ReceivedAt = retur.PutOn;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }


                    await db.SaveChangesAsync();

                    decimal judgementQty = 0;
                    decimal putawayQty = 0;

                    try
                    {
                        judgementQty = db.QCExtends.Where(m => m.QCInspectionID.Equals(dataVM.InspectionID)).Sum(m => m.Qty);
                    }
                    catch
                    {

                    }

                    try
                    {
                        putawayQty = db.QCReturns.Where(m => m.QCPutaway.QCPicking.QCInspectionID.Equals(dataVM.InspectionID)).Sum(m => m.PutawayQty);
                    }
                    catch
                    {

                    }

                    if (judgementQty == putawayQty)
                    {
                        //if all material already putaway, update status to close
                        judgement.QCInspection.TransactionStatus = "CLOSED";
                        await db.SaveChangesAsync();
                    }

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
        public async Task<IHttpActionResult> DatatableReceivingRawMaterial()
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

            IEnumerable<ReceivingDetail> list = Enumerable.Empty<ReceivingDetail>();
            IEnumerable<ReceivingDetailDTO> pagedData = Enumerable.Empty<ReceivingDetailDTO>();

            IQueryable<ReceivingDetail> query = db.ReceivingDetails.Where(s => s.COA == false || s.NGQty > 0).AsQueryable();

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.DoNo.Contains(search)
                        || m.LotNo.Contains(search)
                        || m.StockCode.Contains(search)
                        );

                Dictionary<string, Func<ReceivingDetail, object>> cols = new Dictionary<string, Func<ReceivingDetail, object>>();
                cols.Add("DocNo", x => x.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code);
                cols.Add("Origin", x => x.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode);
                cols.Add("Destination", x => x.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode);
                cols.Add("RefNumber", x => x.Receiving.RefNumber);
                cols.Add("MaterialCode", x => x.Receiving.MaterialCode);
                cols.Add("MaterialName", x => x.Receiving.MaterialName);
                cols.Add("DoNo", x => x.DoNo);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("Qty", x => x.Qty);
                cols.Add("QtyPerBag", x => x.QtyPerBag);
                cols.Add("BagQty", x => x.Qty / x.QtyPerBag);
                cols.Add("UoM", x => x.UoM);
                cols.Add("ATA", x => x.ATA);
                cols.Add("Remarks", x => x.Remarks);
                cols.Add("COA", x => x.COA);
                cols.Add("ReceivedBy", x => x.ReceivedBy);
                cols.Add("ReceivedOn", x => x.ReceivedOn);


                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {

                    pagedData = from detail in list
                                select new ReceivingDetailDTO
                                {
                                    ID = detail.ID,
                                    DocNo = detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.Code,
                                    RefNumber = detail.Receiving.RefNumber,
                                    Origin = string.Format("{0} - {1}", detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceCode, detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.SourceName),
                                    Destination = string.Format("{0} - {1}", detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationCode, detail.Receiving.PurchaseRequestDetail.PurchaseRequestHeader.DestinationName),
                                    MaterialCode = detail.Receiving.MaterialCode,
                                    MaterialName = detail.Receiving.MaterialName,
                                    StockCode = detail.StockCode != null ? detail.StockCode : "",
                                    DoNo = detail.DoNo != null ? detail.DoNo : "",
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                                    BagQty = Helper.FormatThousand(Convert.ToInt32(detail.Qty / detail.QtyPerBag)),
                                    ATA = Helper.NullDateToString2(detail.ATA),
                                    UoM = detail.UoM,
                                    Remarks = detail.Remarks,
                                    ReceivedBy = detail.ReceivedBy,
                                    ReceivedOn = Helper.NullDateTimeToString(detail.ReceivedOn),
                                    COA = detail.COA,
                                    CoaAction = !detail.COA ? true : false,
                                    JudgementAction = detail.NGQty > 0 ? true : false,
                                    OKQty = Helper.FormatThousand(detail.Qty - detail.NGQty),
                                    OKBagQty = Helper.FormatThousand(Convert.ToInt32((detail.Qty - detail.NGQty) / detail.QtyPerBag)),
                                    NGQty = Helper.FormatThousand(detail.NGQty),
                                    NGBagQty = Helper.FormatThousand(Convert.ToInt32(detail.NGQty / detail.QtyPerBag)),
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
        public async Task<IHttpActionResult> PickingWaiting(PickingWaitingWebReq req)
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
                    if (string.IsNullOrEmpty(req.InspectionId))
                    {
                        throw new Exception("Inspection Id is required.");
                    }

                    QCInspection header = new QCInspection();
                    header = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (header == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }

                    if (!string.IsNullOrEmpty(header.InspectionStatus))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(header.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }

                    //string MaterialCode = header.MaterialCode;
                    //string QtyPerBag = req.QtyPerBag.ToString().Replace(',', '.');
                    //string LotNumber = req.LotNumber;
                    //string ExpiredDate = Convert.ToDateTime(req.ExpDate).ToString("yyyyMMdd").Substring(2);
                    //string InDate = Convert.ToDateTime(req.InDate).ToString("yyyyMMdd").Substring(1);

                    //string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    //BinRack binRack = null;
                    //if (string.IsNullOrEmpty(req.BinRackCode))
                    //{
                    //    throw new Exception("BinRack harus diisi.");
                    //}
                    //else
                    //{
                    //    binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                    //    if (binRack == null)
                    //    {
                    //        throw new Exception("BinRack tidak ditemukan.");
                    //    }
                    //}

                    IQueryable<QCPicking> query = db.QCPickings.Where(m => m.QCInspectionID.Equals(req.InspectionId)).AsQueryable();
                    IEnumerable<QCPicking> list = query.ToList();

                    int recordsTotal = query.Count();
                    if (recordsTotal > 0)
                    {
                        foreach (QCPicking rec in list)
                        {
                            rec.PickedMethod = "SCAN";
                            rec.PickedOn = DateTime.Now;
                            rec.PickedBy = activeUser;
                        }
                    }
                    else
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    await db.SaveChangesAsync();

                    status = true;
                    message = "Picking berhasil.";
                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            return Ok(obj);
        }
        [HttpPost]
        public async Task<IHttpActionResult> PickingDispose(PickingDisposeWebReq req)
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


                    if (string.IsNullOrEmpty(req.InspectionId))
                    {
                        throw new Exception("Inspection Id is required.");
                    }

                    QCInspection header = await db.QCInspections.Where(s => s.ID.Equals(req.InspectionId)).FirstOrDefaultAsync();

                    if (header == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (header.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(header.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }
                    string MaterialCode = header.MaterialCode;
                    string QtyPerBag = req.QtyPerBag.ToString().Replace(',', '.');
                    string LotNumber = req.LotNumber;
                    string ExpiredDate = Convert.ToDateTime(req.ExpDate).ToString("yyyyMMdd").Substring(2);
                    string InDate = Convert.ToDateTime(req.InDate).ToString("yyyyMMdd").Substring(1);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        throw new Exception("BinRack harus diisi.");

                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack tidak ditemukan.");
                        }

                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int bagQty = Convert.ToInt32(stockAll.Quantity / stockAll.QtyPerBag);

                        if (req.BagQty > bagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah yang dibutuhkan. Bag Qty tersedia : {0}", bagQty));
                        }
                        else
                        {
                            int availableBagQty = Convert.ToInt32(Math.Ceiling(stockAll.Quantity / stockAll.QtyPerBag));

                            if (req.BagQty > availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", availableBagQty));
                            }
                        }
                    }

                    if (stockAll.Type.Equals("RM"))
                    {
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                    }
                    else if (stockAll.Type.Equals("SFG"))
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                    }



                    await db.SaveChangesAsync();

                    IQueryable<vStockAll> query = query = db.vStockAlls.Where(m => m.MaterialCode.Equals(header.MaterialCode) && m.LotNumber.Equals(header.LotNo) && m.InDate.Equals(header.InDate) && m.ExpiredDate.Equals(header.ExpDate) && m.Quantity > 0).AsQueryable();


                    IEnumerable<vStockAll> tempList = await query.ToListAsync();

                    if (tempList.Count() < 1)
                    {
                        header.TransactionStatus = "CLOSED";
                        await db.SaveChangesAsync();
                    }

                    status = true;
                    message = "Picking berhasil.";

                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            return Ok(obj);
        }
        public static decimal Normalize(decimal value)
        {
            return value / 1.000000000000000000000000000000000m;
        }
        [HttpPost]
        public async Task<IHttpActionResult> PutawayExtend(PutawayExtendWebReq req)
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


                    if (string.IsNullOrEmpty(req.PutawayId))
                    {
                        throw new Exception("Putaway Id is required.");
                    }

                    QCPutaway putaway = await db.QCPutaways.Where(s => s.ID.Equals(req.PutawayId)).FirstOrDefaultAsync();

                    if (putaway == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (putaway.QCPicking.QCInspection.TransactionStatus.Equals("CLOSED"))
                    {
                        throw new Exception("Putaway sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(putaway.QCPicking.QCInspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }


                    string MaterialCode = vProductMaster.MaterialCode;
                    string QtyPerBag = decimal.Round(putaway.QtyPerBag, 2).ToString().Replace(',', '.');
                    string LotNumber = req.LotNumber;
                    string ExpiredDate = Convert.ToDateTime(putaway.NewExpDate.Value).ToString("yyyyMMdd").Substring(2);
                    string InDate = Convert.ToDateTime(req.InDate).ToString("yyyyMMdd").Substring(1);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        req.BinRackCode = putaway.BinRackCode;
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack tidak ditemukan.");
                        }
                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.ID.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack tidak ditemukan.");
                        }

                    }


                    //if (!putaway.NewStockCode.Equals(StockCode))
                    //{
                    //    throw new Exception("Stock tidak ditemukan. Mohon cek kembali.");
                    //}

                    //do stock movement in here
                    //insert to qc putaway
                    //update stock location



                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(putaway.BinRackCode)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }


                    //check if old stock, dont allow stock movement
                    if (putaway.PutawayMethod.Equals("INSPECT"))
                    {
                        //same location with previous
                        if (!putaway.BinRackCode.Equals(binRack.Code))
                        {
                            throw new Exception("Bin/Rack harus diisi sesuai dengan Bin/Rack sebelumnya.");
                        }
                    }
                    else
                    {
                        //new location, cannot same with old location
                        if (putaway.BinRackCode.Equals(binRack.Code))
                        {
                            throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                        }

                        if (putaway.PutawayQty <= 0)
                        {
                            throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                        }
                        else
                        {
                            int putBagQty = Convert.ToInt32(putaway.PutawayQty / putaway.QtyPerBag);
                            int returnBagQty = Convert.ToInt32(putaway.QCReturns.Sum(s => s.PutawayQty / s.QtyPerBag));
                            int availableBagQty = putBagQty - returnBagQty;
                            if (putaway.PutawayQty > availableBagQty)
                            {
                                throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                            }
                        }

                    }


                    if (putaway.PutawayMethod.Equals("INSPECT"))
                    {
                        putaway.PutawayQty = Convert.ToInt32(putaway.PutawayQty / putaway.QtyPerBag);
                    }

                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        //update stock
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= putaway.PutawayQty * stockAll.QtyPerBag;
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(putaway.NewStockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += putaway.PutawayQty * putaway.QtyPerBag;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = vProductMaster.MaterialCode;
                            stockRM.MaterialName = vProductMaster.MaterialName;
                            stockRM.Code = putaway.NewStockCode;
                            stockRM.LotNumber = stockAll.LotNumber;
                            stockRM.InDate = stockAll.InDate;
                            stockRM.ExpiredDate = putaway.NewExpDate.Value;
                            stockRM.Quantity = putaway.PutawayQty * putaway.QtyPerBag;
                            stockRM.QtyPerBag = putaway.QtyPerBag;
                            stockRM.BinRackID = binRack.ID;
                            stockRM.BinRackCode = binRack.Code;
                            stockRM.BinRackName = binRack.Name;
                            stockRM.ReceivedAt = DateTime.Now;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= putaway.PutawayQty * stockAll.QtyPerBag;

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(putaway.NewStockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += putaway.PutawayQty * putaway.QtyPerBag;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = vProductMaster.MaterialCode;
                            stockSFG.MaterialName = vProductMaster.MaterialName;
                            stockSFG.Code = putaway.NewStockCode;
                            stockSFG.LotNumber = stockAll.LotNumber;
                            stockSFG.InDate = stockAll.InDate;
                            stockSFG.ExpiredDate = putaway.NewExpDate.Value;
                            stockSFG.Quantity = putaway.PutawayQty * putaway.QtyPerBag;
                            stockSFG.QtyPerBag = putaway.QtyPerBag;
                            stockSFG.BinRackID = binRack.ID;
                            stockSFG.BinRackCode = binRack.Code;
                            stockSFG.BinRackName = binRack.Name;
                            stockSFG.ReceivedAt = DateTime.Now;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }


                    QCReturn _return = new QCReturn();
                    _return.ID = Helper.CreateGuid("QCr");
                    _return.QCPutawayID = putaway.ID;
                    _return.StockCode = putaway.StockCode;
                    _return.NewStockCode = putaway.NewStockCode;
                    _return.LotNo = stockAll.LotNumber;
                    _return.InDate = stockAll.InDate.Value;
                    _return.ExpDate = stockAll.ExpiredDate.Value;
                    _return.NewExpDate = putaway.NewExpDate.Value;
                    _return.PrevBinRackID = putaway.BinRackID;
                    _return.PrevBinRackCode = putaway.BinRackCode;
                    _return.PrevBinRackName = putaway.BinRackName;
                    _return.BinRackID = binRack.ID;
                    _return.BinRackCode = binRack.Code;
                    _return.BinRackName = binRack.Name;
                    _return.PutawayQty = putaway.PutawayQty * putaway.QtyPerBag;
                    _return.QtyPerBag = putaway.QtyPerBag;
                    _return.PutawayMethod = "SCAN";
                    _return.PutOn = DateTime.Now;
                    _return.PutBy = activeUser;


                    db.QCReturns.Add(_return);


                    await db.SaveChangesAsync();

                    status = true;
                    message = "Putaway berhasil.";

                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> PutawayWaiting(PutawayWaitingWebReq req)
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


                    if (string.IsNullOrEmpty(req.PickingId))
                    {
                        throw new Exception("Picking Id is required.");
                    }

                    QCPicking picking = await db.QCPickings.Where(s => s.ID.Equals(req.PickingId)).FirstOrDefaultAsync();

                    if (picking == null)
                    {
                        throw new Exception("Data tidak dikenali.");
                    }


                    if (!string.IsNullOrEmpty(picking.QCInspection.InspectionStatus))
                    {
                        throw new Exception("Picking sudah tidak dapat dilakukan lagi.");
                    }

                    vProductMaster vProductMaster = await db.vProductMasters.Where(m => m.MaterialCode.Equals(picking.QCInspection.MaterialCode)).FirstOrDefaultAsync();
                    if (vProductMaster == null)
                    {
                        throw new Exception("Material tidak dikenali.");
                    }
                    string MaterialCode = vProductMaster.MaterialCode;
                    string QtyPerBag = req.QtyPerBag.ToString().Replace(',', '.');
                    string LotNumber = req.LotNumber;
                    string ExpiredDate = Convert.ToDateTime(req.ExpDate).ToString("yyyyMMdd").Substring(2);
                    string InDate = Convert.ToDateTime(req.InDate).ToString("yyyyMMdd").Substring(1);

                    string StockCode = string.Format("{0}{1}{2}{3}{4}", MaterialCode.Trim(), QtyPerBag, LotNumber, InDate, ExpiredDate);

                    BinRack binRack = null;
                    if (string.IsNullOrEmpty(req.BinRackCode))
                    {
                        throw new Exception("BinRack harus diisi.");

                    }
                    else
                    {
                        binRack = await db.BinRacks.Where(m => m.Code.Equals(req.BinRackCode)).FirstOrDefaultAsync();
                        if (binRack == null)
                        {
                            throw new Exception("BinRack tidak ditemukan.");
                        }

                    }

                    if (!picking.BinRackCode.Equals(binRack.Code) && !picking.StockCode.Equals(StockCode))
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }

                    if (string.IsNullOrEmpty(picking.PickedMethod))
                    {
                        throw new Exception("Stock belum diambil.");
                    }


                    //do stock movement in here
                    //insert to qc putaway
                    //update stock location

                    if (picking.BinRackCode.Equals(req.BinRackCode))
                    {
                        throw new Exception("Bin/Rack baru tidak bisa sama dengan Bin/Rack sebelumnya.");
                    }


                    if (req.BagQty <= 0)
                    {
                        throw new Exception("Bag Qty tidak boleh kosong atau tidak boleh kurang dari 1.");
                    }
                    else
                    {
                        int pickedBagQty = Convert.ToInt32(picking.Qty / picking.QtyPerBag);
                        int putBagQty = Convert.ToInt32(picking.QCPutaways.Sum(s => s.PutawayQty / s.QtyPerBag));
                        int availableBagQty = pickedBagQty - putBagQty;
                        if (req.BagQty > availableBagQty)
                        {
                            throw new Exception(string.Format("Bag Qty melewati jumlah tersedia. Bag Qty tersedia : {0}", Helper.FormatThousand(availableBagQty)));
                        }
                    }

                    vStockAll stockAll = db.vStockAlls.Where(m => m.Code.Equals(StockCode) && m.BinRackCode.Equals(picking.BinRackCode)).FirstOrDefault();
                    if (stockAll == null)
                    {
                        throw new Exception("Stock tidak ditemukan.");
                    }


                    if (vProductMaster.ProdType.Equals("RM"))
                    {
                        //update stock
                        StockRM stock = db.StockRMs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;
                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockRM stockRM = await db.StockRMs.Where(m => m.Code.Equals(picking.StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockRM != null)
                        {
                            stockRM.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockRM = new StockRM();
                            stockRM.ID = Helper.CreateGuid("S");
                            stockRM.MaterialCode = vProductMaster.MaterialCode;
                            stockRM.MaterialName = vProductMaster.MaterialName;
                            stockRM.Code = stockAll.Code;
                            stockRM.LotNumber = stockAll.LotNumber;
                            stockRM.InDate = stockAll.InDate;
                            stockRM.ExpiredDate = stockAll.ExpiredDate;
                            stockRM.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockRM.QtyPerBag = stockAll.QtyPerBag;
                            stockRM.BinRackID = binRack.ID;
                            stockRM.BinRackCode = binRack.Code;
                            stockRM.BinRackName = binRack.Name;
                            stockRM.ReceivedAt = DateTime.Now;
                            stockRM.OnInspect = true;

                            db.StockRMs.Add(stockRM);
                        }
                    }
                    else
                    {
                        StockSFG stock = db.StockSFGs.Where(m => m.ID.Equals(stockAll.ID)).FirstOrDefault();
                        stock.Quantity -= req.BagQty * stockAll.QtyPerBag;

                        //insert to Stock if not exist, update quantity if barcode, indate and location is same

                        StockSFG stockSFG = await db.StockSFGs.Where(m => m.Code.Equals(picking.StockCode) && m.BinRackCode.Equals(binRack.Code)).FirstOrDefaultAsync();
                        if (stockSFG != null)
                        {
                            stockSFG.Quantity += req.BagQty * stockAll.QtyPerBag;
                        }
                        else
                        {
                            stockSFG = new StockSFG();
                            stockSFG.ID = Helper.CreateGuid("S");
                            stockSFG.MaterialCode = vProductMaster.MaterialCode;
                            stockSFG.MaterialName = vProductMaster.MaterialName;
                            stockSFG.Code = stockAll.Code;
                            stockSFG.LotNumber = stockAll.LotNumber;
                            stockSFG.InDate = stockAll.InDate;
                            stockSFG.ExpiredDate = stockAll.ExpiredDate;
                            stockSFG.Quantity = req.BagQty * stockAll.QtyPerBag;
                            stockSFG.QtyPerBag = stockAll.QtyPerBag;
                            stockSFG.BinRackID = binRack.ID;
                            stockSFG.BinRackCode = binRack.Code;
                            stockSFG.BinRackName = binRack.Name;
                            stockSFG.ReceivedAt = DateTime.Now;
                            stockSFG.OnInspect = true;

                            db.StockSFGs.Add(stockSFG);
                        }
                    }

                    QCPutaway putaway = new QCPutaway();
                    putaway.ID = Helper.CreateGuid("QCp");
                    putaway.QCPickingID = picking.ID;
                    putaway.StockCode = stockAll.Code;
                    putaway.LotNo = stockAll.LotNumber;
                    putaway.InDate = stockAll.InDate.Value;
                    putaway.ExpDate = stockAll.ExpiredDate.Value;
                    putaway.PrevBinRackID = picking.BinRackID;
                    putaway.PrevBinRackCode = picking.BinRackCode;
                    putaway.PrevBinRackName = picking.BinRackName;
                    putaway.BinRackID = binRack.ID;
                    putaway.BinRackCode = binRack.Code;
                    putaway.BinRackName = binRack.Name;
                    putaway.PutawayQty = req.BagQty * stockAll.QtyPerBag;
                    putaway.QtyPerBag = stockAll.QtyPerBag;
                    putaway.PutawayMethod = "SCAN";
                    putaway.PutOn = DateTime.Now;
                    putaway.PutBy = activeUser;

                    db.QCPutaways.Add(putaway);

                    //


                    await db.SaveChangesAsync();


                    status = true;
                    message = "Putaway berhasil.";

                }
                else
                {
                    message = "Token sudah berakhir, silahkan login kembali.";
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

            return Ok(obj);
        }

        [HttpPost]
        public async Task<IHttpActionResult> ListDataReturn(ListDataReturnReq req)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;

            string inDate = request["InDate"].ToString();
            string expDate = request["ExpDate"].ToString();
            DateTime xInDate = new DateTime();
            DateTime xExpDate = new DateTime();
            DateTime temp;
            DateTime temp1;

            if (DateTime.TryParse(inDate, out temp))
            {
                xInDate = Convert.ToDateTime(inDate);
            }
            if (DateTime.TryParse(expDate, out temp1))
            {
                xExpDate = Convert.ToDateTime(expDate);
            }

            IEnumerable<vStockAll> list = Enumerable.Empty<vStockAll>();
            List<ListDataReturnDTO> data = new List<ListDataReturnDTO>();

            IQueryable<vStockAll> query = db.vStockAlls.Where(s => s.MaterialCode.Equals(req.MaterialCode) && s.LotNumber.Equals(req.LotNo) && DbFunctions.TruncateTime(s.InDate) == DbFunctions.TruncateTime(xInDate) && DbFunctions.TruncateTime(s.ExpiredDate) == DbFunctions.TruncateTime(xExpDate) && s.Quantity > 0 && s.OnInspect == true ).AsQueryable();

            try
            {
                list = query.ToList();
                if (list != null && list.Count() > 0)
                {
                    foreach (vStockAll detail in list)
                    {
                        ListDataReturnDTO dat = new ListDataReturnDTO
                        {
                            MaterialCode = detail.MaterialCode,
                            MaterialName = detail.MaterialName,
                            InDate = Helper.NullDateToString(detail.InDate),
                            ExpDate = Helper.NullDateToString(detail.ExpiredDate),
                            LotNo = detail.LotNumber,
                            BinRackCode = detail.BinRackCode,
                            Quantity   = decimal.Round(detail.Quantity, 2).ToString().Replace(',', '.'),
                            QtyPerBag = Helper.FormatThousand(detail.QtyPerBag),
                            BagQty = Convert.ToInt32(detail.BagQty),
                        };

                        data.Add(dat);
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

            obj.Add("data", data);
            obj.Add("status", status);
            obj.Add("message", message);

            return Ok(obj);
        }


        [HttpPost]
        public async Task<IHttpActionResult> DatatableDetailInspection()
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
            string warehouseCode = request["warehouseCode"].ToString();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<vQCInspection> list = Enumerable.Empty<vQCInspection>();
            IEnumerable<InspectionReportDTO> pagedData = Enumerable.Empty<InspectionReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vQCInspection> query;

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                query = db.vQCInspections.Where(s => DbFunctions.TruncateTime(s.CreatedOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreatedOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouseCode));
            }
            else
            {
                query = db.vQCInspections.Where(s => DbFunctions.TruncateTime(s.CreatedOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreatedOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.RMCode.Contains(search)
                        || m.RMName.Contains(search)
                        );

                Dictionary<string, Func<vQCInspection, object>> cols = new Dictionary<string, Func<vQCInspection, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Bag", x => x.Bag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("PickingBag", x => x.PickingBag);
                cols.Add("PickingFullBag", x => x.PickingFullBag);
                cols.Add("PickingTotal", x => x.PickingTotal);
                cols.Add("PickingBinRack", x => x.PickingBinRack);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("ActionExtendDuration", x => x.ActionExtendDuration);
                cols.Add("ActionExpDate", x => x.ActionExpDate);
                cols.Add("ActionDispose", x => x.ActionDispose);
                cols.Add("ApproveBy", x => x.ApproveBy);
                cols.Add("ApproveOn", x => x.ApproveOn);
                cols.Add("PrintLabelBy", x => x.PrintLabelBy);
                cols.Add("PrintLabelOn", x => x.PrintLabelOn);
                cols.Add("PutawayBag", x => x.PutawayBag);
                cols.Add("PutawayFullBag", x => x.PutawayFullBag);
                cols.Add("PutawayTotal", x => x.PutawayTotal);
                cols.Add("PutawayBinRack", x => x.PutawayBinRack);
                cols.Add("PutawayBy", x => x.PutawayBy);
                cols.Add("PutawayOn", x => x.PutawayOn);
                cols.Add("Status", x => x.Status);
                cols.Add("Memo", x => x.Memo);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new InspectionReportDTO
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    Bag = Helper.FormatThousand(detail.Bag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(detail.Total),
                                    CreatedBy = detail.CreatedBy,
                                    CreatedOn = detail.CreatedOn,
                                    PickingBag = Helper.FormatThousand(detail.PickingBag),
                                    PickingFullBag = Helper.FormatThousand(detail.PickingFullBag),
                                    PickingTotal = Helper.FormatThousand(detail.PickingTotal),
                                    PickingBinRack = detail.PickingBinRack,
                                    PickingBy = detail.PickingBy,
                                    PickingOn = Convert.ToDateTime(detail.PickingOn),
                                    ActionExtendDuration = Helper.FormatThousand(detail.ActionExtendDuration),
                                    ActionExpDate = Helper.NullDateToString2(detail.ActionExpDate),
                                    ActionDispose = detail.ActionDispose,
                                    ApproveBy = detail.ApproveBy,
                                    ApproveOn = Convert.ToDateTime(detail.ApproveOn),
                                    PrintLabelBy = detail.PrintLabelBy,
                                    PrintLabelOn = detail.PrintLabelOn,
                                    PutawayBag = Helper.FormatThousand(detail.PutawayBag),
                                    PutawayFullBag = Helper.FormatThousand(detail.PutawayFullBag),
                                    PutawayTotal = Helper.FormatThousand(detail.PutawayTotal),
                                    PutawayBinRack = detail.PutawayBinRack,
                                    PutawayBy = detail.PutawayBy,
                                    PutawayOn = detail.PutawayOn,
                                    Status = detail.Status,
                                    Memo = detail.Memo,
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
        public async Task<IHttpActionResult> GetDataReportInspection(string date, string enddate, string warehouse)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate) && string.IsNullOrEmpty(warehouse))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vQCInspection> list = Enumerable.Empty<vQCInspection>();
            IEnumerable<InspectionReportDTO> pagedData = Enumerable.Empty<InspectionReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vQCInspection> query;

            if (!string.IsNullOrEmpty(warehouse))
            {
                query = db.vQCInspections.Where(s => DbFunctions.TruncateTime(s.CreatedOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreatedOn) <= DbFunctions.TruncateTime(endfilterDate)
                        && s.WHName.Equals(warehouse));
            }
            else
            {
                query = db.vQCInspections.Where(s => DbFunctions.TruncateTime(s.CreatedOn) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.CreatedOn) <= DbFunctions.TruncateTime(endfilterDate));
            }

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vQCInspection, object>> cols = new Dictionary<string, Func<vQCInspection, object>>();
                cols.Add("DocumentNo", x => x.DocumentNo);
                cols.Add("WHName", x => x.WHName);
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("ExpDate", x => x.ExpDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Bag", x => x.Bag);
                cols.Add("FullBag", x => x.FullBag);
                cols.Add("Total", x => x.Total);
                cols.Add("CreatedBy", x => x.CreatedBy);
                cols.Add("CreatedOn", x => x.CreatedOn);
                cols.Add("PickingBag", x => x.PickingBag);
                cols.Add("PickingFullBag", x => x.PickingFullBag);
                cols.Add("PickingTotal", x => x.PickingTotal);
                cols.Add("PickingBinRack", x => x.PickingBinRack);
                cols.Add("PickingBy", x => x.PickingBy);
                cols.Add("PickingOn", x => x.PickingOn);
                cols.Add("ActionExtendDuration", x => x.ActionExtendDuration);
                cols.Add("ActionExpDate", x => x.ActionExpDate);
                cols.Add("ActionDispose", x => x.ActionDispose);
                cols.Add("ApproveBy", x => x.ApproveBy);
                cols.Add("ApproveOn", x => x.ApproveOn);
                cols.Add("PrintLabelBy", x => x.PrintLabelBy);
                cols.Add("PrintLabelOn", x => x.PrintLabelOn);
                cols.Add("PutawayBag", x => x.PutawayBag);
                cols.Add("PutawayFullBag", x => x.PutawayFullBag);
                cols.Add("PutawayTotal", x => x.PutawayTotal);
                cols.Add("PutawayBinRack", x => x.PutawayBinRack);
                cols.Add("PutawayBy", x => x.PutawayBy);
                cols.Add("PutawayOn", x => x.PutawayOn);
                cols.Add("Status", x => x.Status);
                cols.Add("Memo", x => x.Memo);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new InspectionReportDTO
                                {
                                    DocumentNo = detail.DocumentNo,
                                    WHName = detail.WHName,
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    ExpDate = Helper.NullDateToString2(detail.ExpDate),
                                    LotNo = detail.LotNo != null ? detail.LotNo : "",
                                    Bag = Helper.FormatThousand(detail.Bag),
                                    FullBag = Helper.FormatThousand(detail.FullBag),
                                    Total = Helper.FormatThousand(detail.Total),
                                    CreatedBy = detail.CreatedBy,
                                    CreatedOn = detail.CreatedOn,
                                    PickingBag = Helper.FormatThousand(detail.PickingBag),
                                    PickingFullBag = Helper.FormatThousand(detail.PickingFullBag),
                                    PickingTotal = Helper.FormatThousand(detail.PickingTotal),
                                    PickingBinRack = detail.PickingBinRack,
                                    PickingBy = detail.PickingBy,
                                    PickingOn = Convert.ToDateTime(detail.PickingOn),
                                    ActionExtendDuration = Helper.FormatThousand(detail.ActionExtendDuration),
                                    ActionExpDate = Helper.NullDateToString2(detail.ActionExpDate),
                                    ActionDispose = detail.ActionDispose,
                                    ApproveBy = detail.ApproveBy,
                                    ApproveOn = Convert.ToDateTime(detail.ApproveOn),
                                    PrintLabelBy = detail.PrintLabelBy,
                                    PrintLabelOn = detail.PrintLabelOn,
                                    PutawayBag = Helper.FormatThousand(detail.PutawayBag),
                                    PutawayFullBag = Helper.FormatThousand(detail.PutawayFullBag),
                                    PutawayTotal = Helper.FormatThousand(detail.PutawayTotal),
                                    PutawayBinRack = detail.PutawayBinRack,
                                    PutawayBy = detail.PutawayBy,
                                    PutawayOn = detail.PutawayOn,
                                    Status = detail.Status,
                                    Memo = detail.Memo,
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
        public async Task<IHttpActionResult> DatatableDetailShelfLifeExtension()
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

            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;

            IEnumerable<vShelfLifeExtension> list = Enumerable.Empty<vShelfLifeExtension>();
            IEnumerable<ShelfLifeExtensionReportDTO> pagedData = Enumerable.Empty<ShelfLifeExtensionReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vShelfLifeExtension> query;
                       
            query = db.vShelfLifeExtensions.Where(s => DbFunctions.TruncateTime(s.ExpiredDate) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.ExpiredDate) <= DbFunctions.TruncateTime(endfilterDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                query = query
                        .Where(m => m.RMCode.Contains(search)
                        || m.RMName.Contains(search)
                        );

                Dictionary<string, Func<vShelfLifeExtension, object>> cols = new Dictionary<string, Func<vShelfLifeExtension, object>>();
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Qty", x => x.Qty);
                cols.Add("ExpiredDate", x => x.ExpiredDate);
                cols.Add("Extension", x => x.Extension);
                cols.Add("Remark", x => x.Remark);
                cols.Add("ShelfLifeBaseOnCOA", x => x.ShelfLifeBaseOnCOA);
                cols.Add("Note", x => x.Note);

                if (sortDirection.Equals("asc"))
                    list = query.OrderBy(cols[sortName]);
                else
                    list = query.OrderByDescending(cols[sortName]);

                recordsFiltered = list.Count();

                list = list.Skip(start).Take(length).ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ShelfLifeExtensionReportDTO
                                {
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    LotNo = detail.LotNo,
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    ExpiredDate = Helper.NullDateToString2(detail.ExpiredDate),
                                    Extension = detail.Extension,
                                    Remark = detail.Remark,
                                    ShelfLifeBaseOnCOA = detail.ShelfLifeBaseOnCOA,
                                    Note = detail.Note,
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
        public async Task<IHttpActionResult> GetDataReportShelfLifeExtension(string date, string enddate)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "";
            bool status = false;
            HttpRequest request = HttpContext.Current.Request;


            if (string.IsNullOrEmpty(date) && string.IsNullOrEmpty(enddate))
            {
                throw new Exception("Parameter is required.");
            }

            IEnumerable<vShelfLifeExtension> list = Enumerable.Empty<vShelfLifeExtension>();
            IEnumerable<ShelfLifeExtensionReportDTO> pagedData = Enumerable.Empty<ShelfLifeExtensionReportDTO>();

            DateTime filterDate = Convert.ToDateTime(date);
            DateTime endfilterDate = Convert.ToDateTime(enddate);
            IQueryable<vShelfLifeExtension> query;

            query = db.vShelfLifeExtensions.Where(s => DbFunctions.TruncateTime(s.ExpiredDate) >= DbFunctions.TruncateTime(filterDate)
                        && DbFunctions.TruncateTime(s.ExpiredDate) <= DbFunctions.TruncateTime(endfilterDate));

            int recordsTotal = query.Count();
            int recordsFiltered = 0;

            try
            {
                Dictionary<string, Func<vShelfLifeExtension, object>> cols = new Dictionary<string, Func<vShelfLifeExtension, object>>();
                cols.Add("RMCode", x => x.RMCode);
                cols.Add("RMName", x => x.RMName);
                cols.Add("InDate", x => x.InDate);
                cols.Add("LotNo", x => x.LotNo);
                cols.Add("Qty", x => x.Qty);
                cols.Add("ExpiredDate", x => x.ExpiredDate);
                cols.Add("Extension", x => x.Extension);
                cols.Add("Remark", x => x.Remark);
                cols.Add("ShelfLifeBaseOnCOA", x => x.ShelfLifeBaseOnCOA);
                cols.Add("Note", x => x.Note);

                recordsFiltered = list.Count();
                list = query.ToList();

                if (list != null && list.Count() > 0)
                {
                    pagedData = from detail in list
                                select new ShelfLifeExtensionReportDTO
                                {
                                    RMCode = detail.RMCode,
                                    RMName = detail.RMName,
                                    InDate = Helper.NullDateToString2(detail.InDate),
                                    LotNo = detail.LotNo,
                                    Qty = Helper.FormatThousand(detail.Qty),
                                    ExpiredDate = Helper.NullDateToString2(detail.ExpiredDate),
                                    Extension = detail.Extension,
                                    Remark = detail.Remark,
                                    ShelfLifeBaseOnCOA = detail.ShelfLifeBaseOnCOA,
                                    Note = detail.Note,
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
