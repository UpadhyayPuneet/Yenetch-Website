-- Blog posts written in the admin (/admin/posts). SQLite version of cms.sqlserver.sql.

CREATE TABLE CmsPosts (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Slug            TEXT NOT NULL UNIQUE,
    Title           TEXT NOT NULL,
    MetaTitle       TEXT NULL,
    MetaDescription TEXT NULL,
    Category        TEXT NOT NULL,
    Excerpt         TEXT NOT NULL,
    BodyHtml        TEXT NOT NULL,
    CoverImage      TEXT NULL,
    Author          TEXT NULL,
    Tags            TEXT NULL,
    Status          TEXT NOT NULL,
    PublishedOn     DATETIME NULL,
    CreatedBy       INT NULL,
    CreatedOn       DATETIME NOT NULL,
    UpdatedBy       INT NULL,
    UpdatedOn       DATETIME NOT NULL
)
GO
