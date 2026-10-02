using System;
using System.Collections.Generic;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/blog/author/{slug}: an author's profile, their articles, and schema.org ProfilePage + Person data for Google.</summary>
    public partial class AuthorPage : Page
    {
        protected Author A;
        protected List<BlogPost> Posts;
        protected string Ld;

        protected void Page_Load(object sender, EventArgs e)
        {
            A = Authors.Find(Convert.ToString(RouteData.Values["author"]));
            if (A == null) { Server.Transfer("~/NotFound.aspx"); return; }
            Posts = Authors.Posts(A);
            Title = A.Name + (string.IsNullOrEmpty(A.Role) ? "" : ", " + A.Role) + " | Yenetch Insights";
            MetaDescription = string.IsNullOrEmpty(A.Bio) ? "Articles by " + A.Name + " on the Yenetch blog." : A.Bio;
            Master.NavKey = "insights";
            if (A.HasPhoto) { var src = Photos.Src(A.Photo); Master.OgImage = src.StartsWith("http") ? src : Seo.Root + src; }
            var person = Authors.PersonLd(A);
            Ld = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                { "@context", "https://schema.org" }, { "@type", "ProfilePage" }, { "url", Seo.Root + A.Url }, { "mainEntity", person }
            }).Replace("</", "<\\/");
        }
    }
}
