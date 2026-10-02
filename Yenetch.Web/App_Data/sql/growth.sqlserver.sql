-- Growth features (v10): automatic emails, bookings, website audits, review requests, two-step sign-in.
-- Automatic email series: one row per person per series (welcome, lead follow-up, review request).
CREATE TABLE dbo.AutoQueue (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Sequence   NVARCHAR(20) NOT NULL,
    Email      NVARCHAR(160) NOT NULL,
    Name       NVARCHAR(120) NULL,
    RefId      INT NULL,
    Step       INT NOT NULL DEFAULT 0,
    DueOn      DATETIME2 NOT NULL,
    Status     NVARCHAR(20) NOT NULL DEFAULT 'Active',
    Token      NVARCHAR(40) NOT NULL,
    LastError  NVARCHAR(400) NULL,
    Attempts   INT NOT NULL DEFAULT 0,
    CreatedOn  DATETIME2 NOT NULL,
    LastSentOn DATETIME2 NULL
)
GO
CREATE INDEX IX_AutoQueue_Due ON dbo.AutoQueue (Status, DueOn)
GO
CREATE INDEX IX_AutoQueue_Token ON dbo.AutoQueue (Token)
GO
-- Calls booked from /book.
CREATE TABLE dbo.Bookings (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(120) NOT NULL,
    Email      NVARCHAR(160) NOT NULL,
    Phone      NVARCHAR(40) NULL,
    Company    NVARCHAR(160) NULL,
    Topic      NVARCHAR(160) NULL,
    Notes      NVARCHAR(2000) NULL,
    Mode       NVARCHAR(30) NULL,
    StartOn    DATETIME2 NOT NULL,
    EndOn      DATETIME2 NOT NULL,
    Status     NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    Token      NVARCHAR(40) NOT NULL,
    LeadId     INT NULL,
    Reminded   INT NOT NULL DEFAULT 0,
    VisitorId  NVARCHAR(40) NULL,
    Ip         NVARCHAR(64) NULL,
    AdminNotes NVARCHAR(MAX) NULL,
    CreatedOn  DATETIME2 NOT NULL,
    UpdatedOn  DATETIME2 NOT NULL
)
GO
CREATE INDEX IX_Bookings_Start ON dbo.Bookings (StartOn)
GO
CREATE INDEX IX_Bookings_Token ON dbo.Bookings (Token)
GO
-- Free website audits run from /website-audit.
CREATE TABLE dbo.WebAudits (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Url        NVARCHAR(400) NOT NULL,
    FinalUrl   NVARCHAR(400) NULL,
    Name       NVARCHAR(120) NULL,
    Email      NVARCHAR(160) NOT NULL,
    Phone      NVARCHAR(40) NULL,
    Score      INT NOT NULL DEFAULT 0,
    ResultJson NVARCHAR(MAX) NULL,
    LeadId     INT NULL,
    Token      NVARCHAR(40) NOT NULL,
    VisitorId  NVARCHAR(40) NULL,
    Ip         NVARCHAR(64) NULL,
    CreatedOn  DATETIME2 NOT NULL
)
GO
CREATE INDEX IX_WebAudits_Created ON dbo.WebAudits (CreatedOn DESC)
GO
CREATE INDEX IX_WebAudits_Token ON dbo.WebAudits (Token)
GO
-- Requests sent to clients asking for a Google review.
CREATE TABLE dbo.ReviewRequests (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    LeadId    INT NULL,
    Name      NVARCHAR(120) NULL,
    Email     NVARCHAR(160) NOT NULL,
    Token     NVARCHAR(40) NOT NULL,
    SentOn    DATETIME2 NOT NULL,
    ClickedOn DATETIME2 NULL,
    SentBy    INT NULL
)
GO
CREATE INDEX IX_ReviewRequests_Token ON dbo.ReviewRequests (Token)
GO
ALTER TABLE dbo.CrmUsers ADD TwoFactor NVARCHAR(10) NULL
GO
ALTER TABLE dbo.CrmUsers ADD TwoFactorSecret NVARCHAR(400) NULL
GO
ALTER TABLE dbo.CrmUsers ADD BackupCodes NVARCHAR(2000) NULL
GO
ALTER TABLE dbo.CrmUsers ADD EmailCode NVARCHAR(200) NULL
GO
ALTER TABLE dbo.CrmUsers ADD EmailCodeUntil DATETIME2 NULL
GO
ALTER TABLE dbo.CrmUsers ADD LastLoginIp NVARCHAR(64) NULL
GO
ALTER TABLE dbo.CrmUsers ADD KnownDevices NVARCHAR(MAX) NULL
GO
