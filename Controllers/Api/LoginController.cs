using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WMS_BE.Models;
using WMS_BE.Utils;

namespace WMS_BE.Controllers.Api
{
    public class LoginController : ApiController
    {
        private EIN_WMSEntities db = new EIN_WMSEntities();

        // POST api/values
        [HttpPost]
        public async Task<IHttpActionResult> Auth(LoginVM LoginVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Invalid username or password.";
            bool status = false;
            try
            {
                if (ModelState.IsValid) 
                {
                    User user = await db.Users.Where(m => m.IsActive == true).Where(m => m.Username.Equals(LoginVM.Username)).FirstOrDefaultAsync();

                    if (user != null && Cryptolib.ValidatePassword(LoginVM.Password, user.Password))
                    {
                        if (user.MobileUser)
                        {
                            throw new Exception("User not authorized.");
                        }
                        string token = Cryptolib.HashPassword(user.Username + DateTime.Now.ToString());

                        user.Token = token;


                        //update token login date here
                        DateTime loginDate = DateTime.ParseExact(LoginVM.LoginDate, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                        TokenDate tokenDate = await db.TokenDates.Where(m => m.Token.Equals(token)).FirstOrDefaultAsync();
                        if(tokenDate == null)
                        {
                            tokenDate = new TokenDate
                            {
                                Token = token,
                                LoginDate = loginDate,
                                Username = user.Username
                            };
                            db.TokenDates.Add(tokenDate);
                        }
                        else
                        {
                            tokenDate.LoginDate = loginDate;
                        }

                        await db.SaveChangesAsync();

                        message = "Authentication succeeded.";
                        obj.Add("username", user.Username);
                        obj.Add("full_name", user.FullName);
                        obj.Add("token", token);
                        obj.Add("login_date", LoginVM.LoginDate);
                        status = true;
                    }                 
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


        // POST api/values
        [HttpPost]
        public async Task<IHttpActionResult> AuthMobile(LoginMobileVM LoginVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Invalid username or password.";
            bool status = false;
            try
            {
                if (ModelState.IsValid)
                {
                    User user = await db.Users.Where(m => m.IsActive == true).Where(m => m.Username.Equals(LoginVM.Username)).FirstOrDefaultAsync();

                    if (user != null && Cryptolib.ValidatePassword(LoginVM.Password, user.Password))
                    {
                        if (!user.MobileUser)
                        {
                            throw new Exception("User not authorized.");
                        }

                        string token = Cryptolib.HashPassword(user.Username + DateTime.Now.ToString());

                        user.Token = token;

                        message = "Authentication succeeded.";
                        obj.Add("username", user.Username);
                        obj.Add("token", token);
                        obj.Add("full_name", user.FullName);
                        await db.SaveChangesAsync();
                        status = true;
                    }
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


        [HttpPost]
        public async Task<IHttpActionResult> LogoutMobile(LoginReq LoginVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Invalid username.";
            bool status = false;
            try
            {
                if (ModelState.IsValid)
                {
                    User user = await db.Users.Where(m => m.IsActive == true).Where(m => m.Username.Equals(LoginVM.Username)).FirstOrDefaultAsync();

                    if (user != null)
                    {

                        user.Token = null;

                        message = "Logout berhasil.";
                        await db.SaveChangesAsync();
                        status = true;
                    }
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

        [HttpPost]
        public async Task<IHttpActionResult> ForgotPassword(ForgotPassVM dataVM)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            string message = "Username is required.";
            bool status = false;
            try
            {
                if (string.IsNullOrEmpty(dataVM.Username))
                {
                    throw new Exception(message);
                }
                User user = await db.Users.Where(m => m.IsActive == true).Where(m => m.Username.Equals(dataVM.Username)).FirstOrDefaultAsync();

                if (user == null)
                {
                    message = "Username not found.";
                    throw new Exception(message);
                }

                Random random = new Random();
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                string tempPassword = new string(Enumerable.Repeat(chars, 5)
                  .Select(s => s[random.Next(s.Length)]).ToArray());

                user.Password = Cryptolib.HashPassword(tempPassword);

                String body = String.Format("EMIX - RMI <br><br> Hello {0}, <br> New password : {1} <br><br> Thank you, <br> System", user.Username, tempPassword);

                Mailing mailing = new Mailing();
                status = mailing.SendSimpleEmail("EMIX RMI - Forgot Password", body);
                //status = true;
                if (status)
                {
                    message = "Forgot password succeeded. Please ask administrator for new password.";
                    await db.SaveChangesAsync();
                }
                else
                {
                    message = "Forgot password failed. Please try again or ask administrator for further information.";
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

    }
}