-- Newsletter sending queue: one row per recipient, so a send can pause, resume after a restart and retry failures.
CREATE TABLE NewsDeliveries (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    CampaignId   INT   NOT NULL,
    SubscriberId INT   NOT NULL,
    Email        TEXT  NOT NULL,
    Status       TEXT  NOT NULL DEFAULT 'Pending',   -- Pending | Sent | Failed | Skipped
    Attempts     INT   NOT NULL DEFAULT 0,
    Error        TEXT  NULL,
    SentOn       DATETIME NULL
)
GO
CREATE UNIQUE INDEX IX_NewsDeliveries_Campaign ON NewsDeliveries (CampaignId, SubscriberId)
GO
CREATE INDEX IX_NewsDeliveries_Status ON NewsDeliveries (CampaignId, Status)
GO
CREATE INDEX IX_NewsDeliveries_SentOn ON NewsDeliveries (SentOn)
GO
ALTER TABLE NewsCampaigns ADD COLUMN ScheduledFor DATETIME NULL
GO
