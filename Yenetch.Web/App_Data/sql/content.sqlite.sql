-- SQLite version of content.sqlserver.sql.

CREATE TABLE CmsItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Collection  TEXT NOT NULL,
    ItemKey     TEXT NULL,
    Title       TEXT NOT NULL,
    Sort        INT NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    Data        TEXT NOT NULL,
    UpdatedBy   INT NULL,
    UpdatedOn   DATETIME NOT NULL
)
GO
CREATE INDEX IX_CmsItems_Collection ON CmsItems (Collection, Sort)
GO
CREATE TABLE CmsSettings (
    Name        TEXT NOT NULL PRIMARY KEY,
    Value       TEXT NULL,
    UpdatedOn   DATETIME NOT NULL
)
GO
CREATE TABLE CrmAttachments (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    LeadId      INT NOT NULL REFERENCES CrmLeads(Id) ON DELETE CASCADE,
    FileName    TEXT NOT NULL,
    StoredName  TEXT NOT NULL,
    ContentType TEXT NOT NULL,
    Size        INT NOT NULL,
    CreatedOn   DATETIME NOT NULL
)
GO
