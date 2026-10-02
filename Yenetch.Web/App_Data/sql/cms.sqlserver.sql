-- Blog posts written in the admin (/admin/posts). Added automatically on first run, also to databases created before it existed.
-- Rows override the built-in articles in assets/data (same slug); Status Hidden removes a built-in article from the site.

CREATE TABLE dbo.CmsPosts (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Slug            NVARCHAR(160) NOT NULL,
    Title           NVARCHAR(200) NOT NULL,
    MetaTitle       NVARCHAR(200) NULL,
    MetaDescription NVARCHAR(320) NULL,
    Category        NVARCHAR(80)  NOT NULL,
    Excerpt         NVARCHAR(600) NOT NULL,
    BodyHtml        NVARCHAR(MAX) NOT NULL,
    CoverImage      NVARCHAR(400) NULL,
    Author          NVARCHAR(120) NULL,
    Tags            NVARCHAR(400) NULL,
    Status          NVARCHAR(20)  NOT NULL,          -- Draft | Published | Hidden
    PublishedOn     DATETIME2     NULL,              -- a future date schedules the post
    CreatedBy       INT           NULL,
    CreatedOn       DATETIME2     NOT NULL,
    UpdatedBy       INT           NULL,
    UpdatedOn       DATETIME2     NOT NULL
)
GO
CREATE UNIQUE INDEX UX_CmsPosts_Slug ON dbo.CmsPosts (Slug)
GO
