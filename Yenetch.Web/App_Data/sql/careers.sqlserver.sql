-- Job applications from the Careers page (CVs stored in App_Data/resumes).
CREATE TABLE dbo.CrmApplications (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(120)  NOT NULL,
    Email        NVARCHAR(160)  NOT NULL,
    Phone        NVARCHAR(40)   NULL,
    Job          NVARCHAR(160)  NOT NULL,
    Category     NVARCHAR(80)   NULL,
    Experience   DECIMAL(4,1)   NOT NULL DEFAULT 0,
    City         NVARCHAR(80)   NULL,
    Notice       NVARCHAR(40)   NULL,
    Skills       NVARCHAR(400)  NULL,
    ResumeUrl    NVARCHAR(400)  NULL,
    Portfolio    NVARCHAR(400)  NULL,
    Note         NVARCHAR(2000) NULL,
    FileName     NVARCHAR(200)  NULL,
    StoredName   NVARCHAR(200)  NULL,
    ContentType  NVARCHAR(120)  NULL,
    Size         INT            NOT NULL DEFAULT 0,
    Status       NVARCHAR(20)   NOT NULL DEFAULT 'New',
    Rating       INT            NOT NULL DEFAULT 0,
    AdminNotes   NVARCHAR(MAX)  NULL,
    Ip           NVARCHAR(64)   NULL,
    CreatedOn    DATETIME2      NOT NULL,
    UpdatedOn    DATETIME2      NOT NULL
)
GO
CREATE INDEX IX_CrmApplications_Created ON dbo.CrmApplications (CreatedOn DESC)
GO
