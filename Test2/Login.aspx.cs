﻿using System;
using System.Web.Security;
using System.Data.SqlClient;

using Authentication;
using DbAccess;
using System.Web;

namespace Test2
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            dbLogin();
        }

        protected void dbLogin()
        {
            /**
             * Authenticates based on current AD username and Users table in DB
             * */
            Auth auth = new Auth();
            string username = auth.getCurrentUser();
            try
            {
                /*
                 * Structure of DB
                 * 0    |   1       |   2
                 * ID   |   Name    |   Role
                 * 
                 * Example below:
                 * 1    |   John Smith  |   ADMIN
                 */
                Db db = new Db();
                SqlConnection cnn = db.getConnection();
                cnn.Open();

                string sql = "SELECT [Name], [Group] FROM [dbo].[Users]";
                SqlCommand command = db.getCommand(sql, cnn);
                SqlDataReader dataReader = db.getDataReader(command);

                if (auth.isCurrentUserInActiveDirectory())
                {
                    while (dataReader.Read())
                    {
                        if (dataReader["Name"].ToString().Trim().Equals(username))
                        {
                            // set cookie 
                            FormsAuthenticationTicket tkt = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddMinutes(30), false, dataReader.GetValue(1).ToString());
                            string cookiestr = FormsAuthentication.Encrypt(tkt);
                            HttpCookie ck = new HttpCookie(FormsAuthentication.FormsCookieName, cookiestr);
                            ck.Path = FormsAuthentication.FormsCookiePath;
                            Response.Cookies.Add(ck);

                            Session["role"] = dataReader["Group"].ToString();

                            string strRedirect = Request["ReturnUrl"];
                            if (strRedirect == null)
                                strRedirect = "Default.aspx";
                            Response.Redirect(strRedirect, true);
                            break;
                        }
                    }
                }

                // close off connections
                db.closeOff(cnn, command, dataReader);
            } catch (Exception err)
            {
                Response.Write(err);
            }
        }
    }
}