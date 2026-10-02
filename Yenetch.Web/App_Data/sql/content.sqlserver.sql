-- Website content managed in /admin/content: company details, team, clients, case studies, services, products,
-- page copy, legal pages and more. One row per item; Data holds the item's fields as JSON. Filled from the
-- files in assets/data and App_Data/seed on first run, then the site reads only these tables.

CREATE TABLE dbo.CmsItems (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Collection  NVARCHAR(40)  NOT NULL,          -- company | services | products | caseStudies | clients | team | legal | ...
    ItemKey     NVARCHAR(160) NULL,              -- slug or id where the item has one
    Title       NVARCHAR(300) NOT NULL,          -- shown in admin lists
    Sort        INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1,
    Data        NVARCHAR(MAX) NOT NULL,
    UpdatedBy   INT           NULL,
    UpdatedOn   DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_CmsItems_Collection ON dbo.CmsItems (Collection, Sort)
GO
-- Site settings (email server, sender, alert addresses). Secrets are encrypted by the app.
CREATE TABLE dbo.CmsSettings (
    Name        NVARCHAR(80)  NOT NULL PRIMARY KEY,
    Value       NVARCHAR(MAX) NULL,
    UpdatedOn   DATETIME2     NOT NULL
)
GO
-- Files attached to website enquiries (stored in App_Data/attachments).
CREATE TABLE dbo.CrmAttachments (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    LeadId      INT           NOT NULL REFERENCES dbo.CrmLeads(Id) ON DELETE CASCADE,
    FileName    NVARCHAR(200) NOT NULL,
    StoredName  NVARCHAR(200) NOT NULL,
    ContentType NVARCHAR(120) NOT NULL,
    Size        INT           NOT NULL,
    CreatedOn   DATETIME2     NOT NULL
)
GO
