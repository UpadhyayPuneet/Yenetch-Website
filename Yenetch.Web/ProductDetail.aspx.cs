using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Yenetch.Data;
using Yenetch.Models;

namespace Yenetch.Web
{
    /// <summary>/products/{slug}: Yenetch Ecomm, Leads, Studio AI, Billing &amp; POS and Office.</summary>
    public partial class ProductDetail : Page
    {
        protected Product Prod;
        protected ProductCopy Copy;
        protected List<Product> Others;
        protected string FaqLd, ProductLd, CrumbLd;

        protected void Page_Load(object sender, EventArgs e)
        {
            var slug = Convert.ToString(RouteData.Values["slug"]);
            Prod = SiteContent.Product(slug);
            if (Prod == null) { Server.Transfer("~/NotFound.aspx"); return; }

            Copy = SiteContent.ProductCopy(slug);
            Others = SiteContent.Current.Products.Where(p => p.Slug != slug).ToList();

            Title = string.IsNullOrEmpty(Copy.SeoTitle) ? Prod.Name + " | Yenetch" : Copy.SeoTitle;
            MetaDescription = string.IsNullOrEmpty(Copy.SeoDescription) ? Prod.Summary : Copy.SeoDescription;
            Master.NavKey = "products";

            FaqLd = Seo.FaqPage(Copy.Faqs);
            ProductLd = Seo.Product(Prod, MetaDescription);
            CrumbLd = Seo.Breadcrumbs("Home", "/", "Products", "/products", Prod.Name, Prod.Url);
        }
    }
}
