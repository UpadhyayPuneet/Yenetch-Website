-- Newsletter sending queue: one row per recipient, so a send can pause, resume after a restart and retry failures.
CREATE TABLE dbo.NewsDeliveries (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    CampaignId   INT            NOT NULL,
    SubscriberId INT            NOT NULL,
    Email        NVARCHAR(160)  NOT NULL,
    Status       NVARCHAR(20)   NOT NULL DEFAULT 'Pending',   -- Pending | Sent | Failed | Skipped
    Attempts     INT            NOT NULL DEFAULT 0,
    Error        NVARCHAR(400)  NULL,
    SentOn       DATETIME2      NULL
)
GO
CREATE UNIQUE INDEX IX_NewsDeliveries_Campaign ON dbo.NewsDeliveries (CampaignId, SubscriberId)
GO
CREATE INDEX IX_NewsDeliveries_Status ON dbo.NewsDeliveries (CampaignId, Status)
GO
CREATE INDEX IX_NewsDeliveries_SentOn ON dbo.NewsDeliveries (SentOn)
GO
ALTER TABLE dbo.NewsCampaigns ADD ScheduledFor DATETIME2 NULL
GO
