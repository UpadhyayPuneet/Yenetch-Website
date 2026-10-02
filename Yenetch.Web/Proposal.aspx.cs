using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Yenetch.Crm;

namespace Yenetch.Web
{
    /// <summary>
    /// /proposal/{token}: the client's private proposal page. Counts client views (not staff), confirms a Razorpay
    /// payment when the client comes back from checkout, and lets the client accept (typed name) or decline.
    /// </summary>
    public partial class ProposalPage : Page
    {
        protected Proposal P;
        protected bool IsStaff;
        protected string Portal, StatusHtml;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.AppendHeader("X-Robots-Tag", "noindex, nofollow");
            Db.EnsureSchema();
            P = Proposals.ByToken(Convert.ToString(RouteData.Values["token"]));
            if (P == null) { Response.StatusCode = 404; Response.TrySkipIisCustomErrors = true; return; }
            IsStaff = Auth.Current != null;

            // Back from Razorpay checkout: trust it only with Razorpay's signature.
            var q = Request.QueryString;
            if (q["razorpay_payment_id"] != null && !P.IsPaid && q["razorpay_payment_link_status"] == "paid" && q["razorpay_payment_link_id"] == P.PayLinkId
                && Razorpay.CallbackValid(q["razorpay_payment_link_id"], q["razorpay_payment_link_reference_id"], q["razorpay_payment_link_status"], q["razorpay_payment_id"], q["razorpay_signature"]))
            {
                Proposals.Paid(P, P.PayAmount ?? P.DueNow, q["razorpay_payment_id"], "Razorpay checkout");
                P = Proposals.Get(P.Id);
            }
            if (!IsStaff && P.Status != "Draft") Proposals.Viewed(P, Analytics.ClientIp(Request));
            Portal = Proposals.PortalFor(P);
            StatusHtml = P.IsPaid ? "<span class=\"prop-pill prop-pill--ok\">Paid</span>"
                       : P.AcceptedOn.HasValue ? "<span class=\"prop-pill prop-pill--ok\">Accepted</span>"
                       : P.DeclinedOn.HasValue ? "<span class=\"prop-pill\">Declined</span>"
                       : P.IsExpired ? "<span class=\"prop-pill\">Expired</span>"
                       : "<span class=\"prop-pill prop-pill--open\">Awaiting your approval</span>";
        }

        protected static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }

        protected static string Per(string billing) { return billing == "monthly" ? "<small>/month</small>" : billing == "yearly" ? "<small>/year</small>" : ""; }

        protected static string Paragraphs(string text)
        {
            return string.Join("", (text ?? "").Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Select(p => "<p>" + H(p.Trim()).Replace("\n", "<br>") + "</p>"));
        }
    }
}
