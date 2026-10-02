using System;
using System.Configuration;
using System.Threading;

namespace Yenetch.Crm
{
    /// <summary>
    /// Daily clean-up: deletes analytics older than appSettings AnalyticsRetentionMonths (default 25, as stated in the privacy policy)
    /// and the IP location cache after 30 days. Leads and subscribers are never deleted automatically.
    /// </summary>
    public static class Housekeeping
    {
        private static Timer _timer;

        public static void Start()
        {
            if (_timer != null || !Db.IsConfigured) return;
            _timer = new Timer(_ => Run(), null, TimeSpan.FromMinutes(2), TimeSpan.FromHours(24));
        }

        public static void Run()
        {
            try
            {
                Db.EnsureSchema();
                int months;
                if (!int.TryParse(ConfigurationManager.AppSettings["AnalyticsRetentionMonths"], out months) || months < 1) months = 25;
                var cutoff = DateTime.UtcNow.AddMonths(-months);
                Db.Exec("DELETE FROM WebPageviews WHERE ViewedOn < @cutoff", new { cutoff });
                Db.Exec("DELETE FROM WebEvents WHERE OccurredOn < @cutoff", new { cutoff });
                Db.Exec("DELETE FROM WebSessions WHERE StartedOn < @cutoff", new { cutoff });
                Db.Exec("DELETE FROM WebVisitors WHERE LastSeen < @cutoff AND LeadId IS NULL", new { cutoff });
                Db.Exec("DELETE FROM GeoCache WHERE CachedOn < @old", new { old = DateTime.UtcNow.AddDays(-30) });
                Db.Exec("DELETE FROM AiChats WHERE LastOn < @cutoff AND LeadId IS NULL", new { cutoff });
            }
            catch (Exception ex) { Mailer.Log("housekeeping", ex); }
            // Fresh exchange rates, and lead scores that count the latest visits and quiet periods.
            try { Yenetch.Data.Fx.Refresh(); } catch (Exception ex) { Mailer.Log("currency rates", ex); }
            try
            {
                LeadScoring.RecalcOpen();
                Proposals.ExpireOld();
            }
            catch (Exception ex) { Mailer.Log("housekeeping", ex); }
        }
    }
}
