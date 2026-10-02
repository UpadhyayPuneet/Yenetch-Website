-- Yenetch CRM, analytics and newsletter schema (SQLite 3, used for local testing and small single-server installs).
-- Applied automatically on first run by Yenetch.Crm.DbSchema when CrmUsers does not exist.
-- Statements are separated by lines containing only GO.

CREATE TABLE CrmUsers (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Email         TEXT NOT NULL UNIQUE,
    Name          TEXT NOT NULL,
    Role          TEXT  NOT NULL,          -- Admin | Manager | Sales
    PasswordHash  TEXT NOT NULL,
    IsActive      INTEGER           NOT NULL DEFAULT 1,
    FailedLogins  INT           NOT NULL DEFAULT 0,
    LockedUntil   DATETIME     NULL,
    LastLoginOn   DATETIME     NULL,
    CreatedOn     DATETIME     NOT NULL
)
GO
CREATE TABLE CrmLeads (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    CreatedOn     DATETIME     NOT NULL,
    UpdatedOn     DATETIME     NOT NULL,
    Name          TEXT NOT NULL,
    Email         TEXT NULL,
    Phone         TEXT  NULL,
    Company       TEXT NULL,
    City          TEXT  NULL,
    Need          TEXT NULL,
    Interest      TEXT NULL,          -- service or product asked about
    LeadType      TEXT  NOT NULL,      -- Marketing | Development | Talent | Product | General
    Source        TEXT  NOT NULL,      -- Contact form | Chatbot | Solution finder | Manual | Phone | WhatsApp | Referral | Email | Event | Other
    Channel       TEXT  NULL,          -- Organic search | Paid | Social | Referral | Direct | Email (from the visitor record)
    Status        TEXT  NOT NULL,      -- New | Contacted | Qualified | Proposal | Negotiation | Won | Lost | Junk
    Priority      TEXT  NOT NULL,      -- Hot | Warm | Cold
    EstValue      NUMERIC NULL,          -- expected deal value in INR
    AssignedTo    INT           NULL REFERENCES CrmUsers(Id),
    NextFollowUp  DATETIME     NULL,
    LostReason    TEXT NULL,
    Tags          TEXT NULL,
    VisitorId     TEXT  NULL,
    Page          TEXT NULL,
    ContextJson   TEXT NULL,
    CreatedBy     INT           NULL
)
GO
CREATE INDEX IX_CrmLeads_Created ON CrmLeads (CreatedOn)
GO
CREATE INDEX IX_CrmLeads_Assigned ON CrmLeads (AssignedTo, Status)
GO
CREATE TABLE CrmActivities (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    LeadId     INT           NOT NULL REFERENCES CrmLeads(Id) ON DELETE CASCADE,
    UserId     INT           NULL,
    Kind       TEXT  NOT NULL,         -- Note | Call | Email | WhatsApp | Meeting | FollowUp | Status | Assign | System
    Body       TEXT NULL,
    CreatedOn  DATETIME     NOT NULL,
    DueOn      DATETIME     NULL,             -- follow-up tasks only
    DoneOn     DATETIME     NULL
)
GO
CREATE INDEX IX_CrmActivities_Lead ON CrmActivities (LeadId, CreatedOn)
GO
CREATE INDEX IX_CrmActivities_Due ON CrmActivities (DoneOn, DueOn)
GO
CREATE TABLE WebVisitors (
    Id            TEXT  PRIMARY KEY,   -- first-party cookie id (only set after analytics consent)
    FirstSeen     DATETIME     NOT NULL,
    LastSeen      DATETIME     NOT NULL,
    Sessions      INT           NOT NULL DEFAULT 0,
    Pageviews     INT           NOT NULL DEFAULT 0,
    Country       TEXT  NULL,
    Region        TEXT  NULL,
    City          TEXT  NULL,
    Device        TEXT  NULL,
    Browser       TEXT  NULL,
    Os            TEXT  NULL,
    FirstChannel  TEXT  NULL,
    FirstSource   TEXT NULL,
    FirstLanding  TEXT NULL,
    LastIp        TEXT  NULL,
    LeadId        INT           NULL
)
GO
CREATE INDEX IX_WebVisitors_LastSeen ON WebVisitors (LastSeen)
GO
CREATE TABLE WebSessions (
    Id           TEXT  PRIMARY KEY,
    VisitorId    TEXT  NULL,
    StartedOn    DATETIME     NOT NULL,
    LastOn       DATETIME     NOT NULL,
    Pageviews    INT           NOT NULL DEFAULT 0,
    Landing      TEXT NULL,
    ExitPage     TEXT NULL,
    Referrer     TEXT NULL,
    Channel      TEXT  NULL,
    Source       TEXT NULL,
    Medium       TEXT  NULL,
    Campaign     TEXT NULL,
    Device       TEXT  NULL,
    Browser      TEXT  NULL,
    Os           TEXT  NULL,
    Screen       TEXT  NULL,
    Lang         TEXT  NULL,
    TimeZone     TEXT  NULL,
    Country      TEXT  NULL,
    Region       TEXT  NULL,
    City         TEXT  NULL,
    Ip           TEXT  NULL,
    Consent      INTEGER           NOT NULL DEFAULT 0
)
GO
CREATE INDEX IX_WebSessions_Started ON WebSessions (StartedOn)
GO
CREATE INDEX IX_WebSessions_Visitor ON WebSessions (VisitorId)
GO
CREATE TABLE WebPageviews (
    Id          TEXT  PRIMARY KEY,
    SessionId   TEXT  NULL,
    VisitorId   TEXT  NULL,
    ViewedOn    DATETIME     NOT NULL,
    Path        TEXT NOT NULL,
    Title       TEXT NULL,
    DurationSec INT           NULL,
    ScrollPct   INT           NULL
)
GO
CREATE INDEX IX_WebPageviews_Viewed ON WebPageviews (ViewedOn)
GO
CREATE INDEX IX_WebPageviews_Session ON WebPageviews (SessionId)
GO
CREATE TABLE WebEvents (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId   TEXT  NULL,
    VisitorId   TEXT  NULL,
    OccurredOn  DATETIME     NOT NULL,
    Name        TEXT  NOT NULL,         -- cta_click | call | whatsapp | email | outbound | chat_open | finder_done | lead | newsletter
    Label       TEXT NULL,
    Path        TEXT NULL
)
GO
CREATE INDEX IX_WebEvents_Occurred ON WebEvents (OccurredOn)
GO
CREATE TABLE GeoCache (
    Ip         TEXT PRIMARY KEY,
    Country    TEXT NULL,
    Region     TEXT NULL,
    City       TEXT NULL,
    CachedOn   DATETIME    NOT NULL
)
GO
CREATE TABLE NewsSubscribers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Email           TEXT NOT NULL UNIQUE,
    Name            TEXT NULL,
    Status          TEXT  NOT NULL,     -- Active | Unsubscribed
    Source          TEXT  NULL,
    Token           TEXT  NOT NULL,
    VisitorId       TEXT  NULL,
    CreatedOn       DATETIME     NOT NULL,
    UnsubscribedOn  DATETIME     NULL
)
GO
CREATE TABLE NewsCampaigns (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Subject     TEXT NOT NULL,
    Preheader   TEXT NULL,
    BodyHtml    TEXT NOT NULL,
    Status      TEXT  NOT NULL,          -- Draft | Sending | Sent
    CreatedBy   INT           NULL,
    CreatedOn   DATETIME     NOT NULL,
    UpdatedOn   DATETIME     NOT NULL,
    SentOn      DATETIME     NULL,
    Recipients  INT           NOT NULL DEFAULT 0,
    SentCount   INT           NOT NULL DEFAULT 0,
    FailedCount INT           NOT NULL DEFAULT 0
)
GO
