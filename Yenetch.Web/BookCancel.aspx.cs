using System;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web
{
    /// <summary>/book/cancel?t={token}: the link in booking emails. Shows the call and lets the visitor cancel it.
    /// Cancelling needs a button press, so email link scanners cannot cancel calls.</summary>
    public partial class BookCancel : Page
    {
        protected Booking B;
        protected bool Done;

        protected void Page_Load(object sender, EventArgs e)
        {
            Db.EnsureSchema();
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            B = Bookings.ByToken(Request.HttpMethod == "POST" ? Request.Form["t"] : Request.QueryString["t"]);
            if (B != null && Request.HttpMethod == "POST" && Request.Form["cancel"] == "1") Done = Bookings.Cancel(B, true);
        }
    }
}
