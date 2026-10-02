-- Yenetch CRM, analytics and newsletter schema (SQL Server 2016+ / LocalDB / Express).
-- Applied automatically on first run by Yenetch.Crm.DbSchema when dbo.CrmUsers does not exist.
-- Statements are separated by lines containing only GO.

CREATE TABLE dbo.CrmUsers (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Email         NVARCHAR(160) NOT NULL CONSTRAINT UQ_CrmUsers_Email UNIQUE,
    Name          NVARCHAR(120) NOT NULL,
    Role          NVARCHAR(20)  NOT NULL,          -- Admin | Manager | Sales
    PasswordHash  NVARCHAR(200) NOT NULL,
    IsActive      BIT           NOT NULL DEFAULT 1,
    FailedLogins  INT           NOT NULL DEFAULT 0,
    LockedUntil   DATETIME2     NULL,
    LastLoginOn   DATETIME2     NULL,
    CreatedOn     DATETIME2     NOT NULL
)
GO
CREATE TABLE dbo.CrmLeads (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    CreatedOn     DATETIME2     NOT NULL,
    UpdatedOn     DATETIME2     NOT NULL,
    Name          NVARCHAR(120) NOT NULL,
    Email         NVARCHAR(160) NULL,
    Phone         NVARCHAR(40)  NULL,
    Company       NVARCHAR(160) NULL,
    City          NVARCHAR(80)  NULL,
    Need          NVARCHAR(2000) NULL,
    Interest      NVARCHAR(160) NULL,          -- service or product asked about
    LeadType      NVARCHAR(30)  NOT NULL,      -- Marketing | Development | Talent | Product | General
    Source        NVARCHAR(30)  NOT NULL,      -- Contact form | Chatbot | Solution finder | Manual | Phone | WhatsApp | Referral | Email | Event | Other
    Channel       NVARCHAR(30)  NULL,          -- Organic search | Paid | Social | Referral | Direct | Email (from the visitor record)
    Status        NVARCHAR(30)  NOT NULL,      -- New | Contacted | Qualified | Proposal | Negotiation | Won | Lost | Junk
    Priority      NVARCHAR(10)  NOT NULL,      -- Hot | Warm | Cold
    EstValue      DECIMAL(14,2) NULL,          -- expected deal value in INR
    AssignedTo    INT           NULL REFERENCES dbo.CrmUsers(Id),
    NextFollowUp  DATETIME2     NULL,
    LostReason    NVARCHAR(300) NULL,
    Tags          NVARCHAR(300) NULL,
    VisitorId     NVARCHAR(40)  NULL,
    Page          NVARCHAR(300) NULL,
    ContextJson   NVARCHAR(MAX) NULL,
    CreatedBy     INT           NULL
)
GO
CREATE INDEX IX_CrmLeads_Created ON dbo.CrmLeads (CreatedOn DESC)
GO
CREATE INDEX IX_CrmLeads_Assigned ON dbo.CrmLeads (AssignedTo, Status)
GO
CREATE TABLE dbo.CrmActivities (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    LeadId     INT           NOT NULL REFERENCES dbo.CrmLeads(Id) ON DELETE CASCADE,
    UserId     INT           NULL,
    Kind       NVARCHAR(20)  NOT NULL,         -- Note | Call | Email | WhatsApp | Meeting | FollowUp | Status | Assign | System
    Body       NVARCHAR(2000) NULL,
    CreatedOn  DATETIME2     NOT NULL,
    DueOn      DATETIME2     NULL,             -- follow-up tasks only
    DoneOn     DATETIME2     NULL
)
GO
CREATE INDEX IX_CrmActivities_Lead ON dbo.CrmActivities (LeadId, CreatedOn DESC)
GO
CREATE INDEX IX_CrmActivities_Due ON dbo.CrmActivities (DoneOn, DueOn)
GO
CREATE TABLE dbo.WebVisitors (
    Id            NVARCHAR(40)  PRIMARY KEY,   -- first-party cookie id (only set after analytics consent)
    FirstSeen     DATETIME2     NOT NULL,
    LastSeen      DATETIME2     NOT NULL,
    Sessions      INT           NOT NULL DEFAULT 0,
    Pageviews     INT           NOT NULL DEFAULT 0,
    Country       NVARCHAR(60)  NULL,
    Region        NVARCHAR(80)  NULL,
    City          NVARCHAR(80)  NULL,
    Device        NVARCHAR(20)  NULL,
    Browser       NVARCHAR(40)  NULL,
    Os            NVARCHAR(40)  NULL,
    FirstChannel  NVARCHAR(30)  NULL,
    FirstSource   NVARCHAR(120) NULL,
    FirstLanding  NVARCHAR(300) NULL,
    LastIp        NVARCHAR(64)  NULL,
    LeadId        INT           NULL
)
GO
CREATE INDEX IX_WebVisitors_LastSeen ON dbo.WebVisitors (LastSeen DESC)
GO
CREATE TABLE dbo.WebSessions (
    Id           NVARCHAR(40)  PRIMARY KEY,
    VisitorId    NVARCHAR(40)  NULL,
    StartedOn    DATETIME2     NOT NULL,
    LastOn       DATETIME2     NOT NULL,
    Pageviews    INT           NOT NULL DEFAULT 0,
    Landing      NVARCHAR(300) NULL,
    ExitPage     NVARCHAR(300) NULL,
    Referrer     NVARCHAR(400) NULL,
    Channel      NVARCHAR(30)  NULL,
    Source       NVARCHAR(120) NULL,
    Medium       NVARCHAR(60)  NULL,
    Campaign     NVARCHAR(120) NULL,
    Device       NVARCHAR(20)  NULL,
    Browser      NVARCHAR(40)  NULL,
    Os           NVARCHAR(40)  NULL,
    Screen       NVARCHAR(20)  NULL,
    Lang         NVARCHAR(20)  NULL,
    TimeZone     NVARCHAR(60)  NULL,
    Country      NVARCHAR(60)  NULL,
    Region       NVARCHAR(80)  NULL,
    City         NVARCHAR(80)  NULL,
    Ip           NVARCHAR(64)  NULL,
    Consent      BIT           NOT NULL DEFAULT 0
)
GO
CREATE INDEX IX_WebSessions_Started ON dbo.WebSessions (StartedOn)
GO
CREATE INDEX IX_WebSessions_Visitor ON dbo.WebSessions (VisitorId)
GO
CREATE TABLE dbo.WebPageviews (
    Id          NVARCHAR(40)  PRIMARY KEY,
    SessionId   NVARCHAR(40)  NULL,
    VisitorId   NVARCHAR(40)  NULL,
    ViewedOn    DATETIME2     NOT NULL,
    Path        NVARCHAR(300) NOT NULL,
    Title       NVARCHAR(200) NULL,
    DurationSec INT           NULL,
    ScrollPct   INT           NULL
)
GO
CREATE INDEX IX_WebPageviews_Viewed ON dbo.WebPageviews (ViewedOn)
GO
CREATE INDEX IX_WebPageviews_Session ON dbo.WebPageviews (SessionId)
GO
CREATE TABLE dbo.WebEvents (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    SessionId   NVARCHAR(40)  NULL,
    VisitorId   NVARCHAR(40)  NULL,
    OccurredOn  DATETIME2     NOT NULL,
    Name        NVARCHAR(60)  NOT NULL,         -- cta_click | call | whatsapp | email | outbound | chat_open | finder_done | lead | newsletter
    Label       NVARCHAR(200) NULL,
    Path        NVARCHAR(300) NULL
)
GO
CREATE INDEX IX_WebEvents_Occurred ON dbo.WebEvents (OccurredOn)
GO
CREATE TABLE dbo.GeoCache (
    Ip         NVARCHAR(64) PRIMARY KEY,
    Country    NVARCHAR(60) NULL,
    Region     NVARCHAR(80) NULL,
    City       NVARCHAR(80) NULL,
    CachedOn   DATETIME2    NOT NULL
)
GO
CREATE TABLE dbo.NewsSubscribers (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Email           NVARCHAR(160) NOT NULL CONSTRAINT UQ_NewsSubscribers_Email UNIQUE,
    Name            NVARCHAR(120) NULL,
    Status          NVARCHAR(20)  NOT NULL,     -- Active | Unsubscribed
    Source          NVARCHAR(40)  NULL,
    Token           NVARCHAR(40)  NOT NULL,
    VisitorId       NVARCHAR(40)  NULL,
    CreatedOn       DATETIME2     NOT NULL,
    UnsubscribedOn  DATETIME2     NULL
)
GO
CREATE TABLE dbo.NewsCampaigns (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Subject     NVARCHAR(200) NOT NULL,
    Preheader   NVARCHAR(200) NULL,
    BodyHtml    NVARCHAR(MAX) NOT NULL,
    Status      NVARCHAR(20)  NOT NULL,          -- Draft | Sending | Sent
    CreatedBy   INT           NULL,
    CreatedOn   DATETIME2     NOT NULL,
    UpdatedOn   DATETIME2     NOT NULL,
    SentOn      DATETIME2     NULL,
    Recipients  INT           NOT NULL DEFAULT 0,
    SentCount   INT           NOT NULL DEFAULT 0,
    FailedCount INT           NOT NULL DEFAULT 0
)
GO
