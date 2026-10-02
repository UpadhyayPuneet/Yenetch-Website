-- Yenetch website database (SQL Server 2016+)
-- Blog posts: switch the site to this table by setting appSettings BlogSource = "Sql".
CREATE TABLE dbo.BlogPosts (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Slug            NVARCHAR(160)  NOT NULL CONSTRAINT UQ_BlogPosts_Slug UNIQUE,
    Title           NVARCHAR(200)  NOT NULL,
    MetaTitle       NVARCHAR(70)   NULL,     -- shown in Google results; falls back to Title
    MetaDescription NVARCHAR(160)  NULL,     -- falls back to Excerpt
    Category        NVARCHAR(60)   NOT NULL,
    Excerpt         NVARCHAR(400)  NOT NULL,
    BodyHtml        NVARCHAR(MAX)  NULL,
    Author          NVARCHAR(100)  NULL,
    CoverImage      NVARCHAR(300)  NULL,
    ReadMinutes     INT            NOT NULL DEFAULT 5,
    PublishedOn     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    IsPublished     BIT            NOT NULL DEFAULT 0,
    UpdatedOn       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_BlogPosts_Published ON dbo.BlogPosts (IsPublished, PublishedOn DESC) INCLUDE (Slug, Title, Category);

CREATE TABLE dbo.BlogTags (
    PostId INT NOT NULL REFERENCES dbo.BlogPosts(Id) ON DELETE CASCADE,
    Tag    NVARCHAR(60) NOT NULL,
    PRIMARY KEY (PostId, Tag)
);

-- Leads from the chatbot, service finder and contact form.
CREATE TABLE dbo.Leads (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(120) NOT NULL,
    Contact     NVARCHAR(160) NOT NULL,   -- phone or email
    Need        NVARCHAR(1000) NULL,
    Topic       NVARCHAR(200) NULL,
    Source      NVARCHAR(40)  NOT NULL,   -- chatbot | finder | contact-form
    ContextJson NVARCHAR(MAX) NULL,       -- finder answers, page, etc.
    Page        NVARCHAR(300) NULL,
    CreatedOn   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status      NVARCHAR(30) NOT NULL DEFAULT 'New'
);
