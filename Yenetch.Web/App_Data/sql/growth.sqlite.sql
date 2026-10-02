-- Growth features (v10): automatic emails, bookings, website audits, review requests, two-step sign-in.
-- Automatic email series: one row per person per series (welcome, lead follow-up, review request).
CREATE TABLE AutoQueue (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Sequence   TEXT NOT NULL,
    Email      TEXT NOT NULL,
    Name       TEXT NULL,
    RefId      INT NULL,
    Step       INT NOT NULL DEFAULT 0,
    DueOn      DATETIME NOT NULL,
    Status     TEXT NOT NULL DEFAULT 'Active',
    Token      TEXT NOT NULL,
    LastError  TEXT NULL,
    Attempts   INT NOT NULL DEFAULT 0,
    CreatedOn  DATETIME NOT NULL,
    LastSentOn DATETIME NULL
)
GO
CREATE INDEX IX_AutoQueue_Due ON AutoQueue (Status, DueOn)
GO
CREATE INDEX IX_AutoQueue_Token ON AutoQueue (Token)
GO
-- Calls booked from /book.
CREATE TABLE Bookings (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Name       TEXT NOT NULL,
    Email      TEXT NOT NULL,
    Phone      TEXT NULL,
    Company    TEXT NULL,
    Topic      TEXT NULL,
    Notes      TEXT NULL,
    Mode       TEXT NULL,
    StartOn    DATETIME NOT NULL,
    EndOn      DATETIME NOT NULL,
    Status     TEXT NOT NULL DEFAULT 'Confirmed',
    Token      TEXT NOT NULL,
    LeadId     INT NULL,
    Reminded   INT NOT NULL DEFAULT 0,
    VisitorId  TEXT NULL,
    Ip         TEXT NULL,
    AdminNotes TEXT NULL,
    CreatedOn  DATETIME NOT NULL,
    UpdatedOn  DATETIME NOT NULL
)
GO
CREATE INDEX IX_Bookings_Start ON Bookings (StartOn)
GO
CREATE INDEX IX_Bookings_Token ON Bookings (Token)
GO
-- Free website audits run from /website-audit.
CREATE TABLE WebAudits (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Url        TEXT NOT NULL,
    FinalUrl   TEXT NULL,
    Name       TEXT NULL,
    Email      TEXT NOT NULL,
    Phone      TEXT NULL,
    Score      INT NOT NULL DEFAULT 0,
    ResultJson TEXT NULL,
    LeadId     INT NULL,
    Token      TEXT NOT NULL,
    VisitorId  TEXT NULL,
    Ip         TEXT NULL,
    CreatedOn  DATETIME NOT NULL
)
GO
CREATE INDEX IX_WebAudits_Created ON WebAudits (CreatedOn DESC)
GO
CREATE INDEX IX_WebAudits_Token ON WebAudits (Token)
GO
-- Requests sent to clients asking for a Google review.
CREATE TABLE ReviewRequests (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    LeadId    INT NULL,
    Name      TEXT NULL,
    Email     TEXT NOT NULL,
    Token     TEXT NOT NULL,
    SentOn    DATETIME NOT NULL,
    ClickedOn DATETIME NULL,
    SentBy    INT NULL
)
GO
CREATE INDEX IX_ReviewRequests_Token ON ReviewRequests (Token)
GO
ALTER TABLE CrmUsers ADD COLUMN TwoFactor TEXT NULL
GO
ALTER TABLE CrmUsers ADD COLUMN TwoFactorSecret TEXT NULL
GO
ALTER TABLE CrmUsers ADD COLUMN BackupCodes TEXT NULL
GO
ALTER TABLE CrmUsers ADD COLUMN EmailCode TEXT NULL
GO
ALTER TABLE CrmUsers ADD COLUMN EmailCodeUntil DATETIME NULL
GO
ALTER TABLE CrmUsers ADD COLUMN LastLoginIp TEXT NULL
GO
ALTER TABLE CrmUsers ADD COLUMN KnownDevices TEXT NULL
GO
