using System;
using System.Web.UI;

namespace Yenetch.Web
{
    /// <summary>404 page. Sends a real 404 status so search engines drop dead links.</summary>
    public partial class NotFound : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Title = "Page not found | Yenetch";
            MetaDescription = "The page you were looking for could not be found.";
        }
    }
}
