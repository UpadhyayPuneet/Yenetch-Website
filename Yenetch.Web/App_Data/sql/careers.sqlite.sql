-- Job applications from the Careers page (CVs stored in App_Data/resumes).
CREATE TABLE CrmApplications (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Name         TEXT  NOT NULL,
    Email        TEXT  NOT NULL,
    Phone        TEXT   NULL,
    Job          TEXT  NOT NULL,
    Category     TEXT   NULL,
    Experience   REAL   NOT NULL DEFAULT 0,
    City         TEXT   NULL,
    Notice       TEXT   NULL,
    Skills       TEXT  NULL,
    ResumeUrl    TEXT  NULL,
    Portfolio    TEXT  NULL,
    Note         TEXT NULL,
    FileName     TEXT  NULL,
    StoredName   TEXT  NULL,
    ContentType  TEXT  NULL,
    Size         INT            NOT NULL DEFAULT 0,
    Status       TEXT   NOT NULL DEFAULT 'New',
    Rating       INT            NOT NULL DEFAULT 0,
    AdminNotes   TEXT  NULL,
    Ip           TEXT   NULL,
    CreatedOn    DATETIME      NOT NULL,
    UpdatedOn    DATETIME      NOT NULL
)
GO
CREATE INDEX IX_CrmApplications_Created ON CrmApplications (CreatedOn DESC)
GO
