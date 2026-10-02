-- Sales tools (v11): quotes from the plan builder, proposals with payment links, the AI assistant, lead scores and webhooks.
-- Quotes built by visitors on /pricing (each one also creates or updates a lead).
CREATE TABLE Quotes (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Token       TEXT  NOT NULL,
    LeadId      INT           NULL,
    Name        TEXT NULL,
    Email       TEXT NULL,
    Phone       TEXT  NULL,
    Company     TEXT NULL,
    Notes       TEXT NULL,
    ItemsJson   TEXT NOT NULL,
    Currency    TEXT   NOT NULL,
    Rate        NUMERIC NOT NULL DEFAULT 1,
    OneTimeInr  NUMERIC NOT NULL DEFAULT 0,
    MonthlyInr  NUMERIC NOT NULL DEFAULT 0,
    DiscountInr NUMERIC NOT NULL DEFAULT 0,
    OfferCode   TEXT  NULL,
    VisitorId   TEXT  NULL,
    Ip          TEXT  NULL,
    CreatedOn   DATETIME     NOT NULL
)
GO
CREATE INDEX IX_Quotes_Created ON Quotes (CreatedOn)
GO
CREATE INDEX IX_Quotes_Token ON Quotes (Token)
GO
-- Proposals sent to clients, shared at /proposal/{token}.
CREATE TABLE Proposals (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    Number         TEXT  NOT NULL,
    Token          TEXT  NOT NULL,
    LeadId         INT           NULL,
    QuoteId        INT           NULL,
    Title          TEXT NOT NULL,
    ClientName     TEXT NULL,
    ClientCompany  TEXT NULL,
    ClientEmail    TEXT NULL,
    ClientPhone    TEXT  NULL,
    Intro          TEXT NULL,
    ItemsJson      TEXT NOT NULL,
    Currency       TEXT   NOT NULL,
    TaxName        TEXT  NULL,
    TaxPct         NUMERIC  NOT NULL DEFAULT 0,
    DiscountPct    NUMERIC  NOT NULL DEFAULT 0,
    Subtotal       NUMERIC NOT NULL DEFAULT 0,
    Discount       NUMERIC NOT NULL DEFAULT 0,
    Tax            NUMERIC NOT NULL DEFAULT 0,
    Total          NUMERIC NOT NULL DEFAULT 0,
    DepositPct     NUMERIC  NOT NULL DEFAULT 0,
    Terms          TEXT NULL,
    ValidUntil     DATETIME     NULL,
    Status         TEXT  NOT NULL DEFAULT 'Draft',
    SentOn         DATETIME     NULL,
    FirstViewedOn  DATETIME     NULL,
    LastViewedOn   DATETIME     NULL,
    Views          INT           NOT NULL DEFAULT 0,
    AcceptedOn     DATETIME     NULL,
    AcceptedName   TEXT NULL,
    AcceptedIp     TEXT  NULL,
    DeclinedOn     DATETIME     NULL,
    DeclineReason  TEXT NULL,
    PayLinkId      TEXT  NULL,
    PayLinkUrl     TEXT NULL,
    PayAmount      NUMERIC NULL,
    PayStatus      TEXT  NULL,
    PaidOn         DATETIME     NULL,
    AmountPaid     NUMERIC NULL,
    PaymentId      TEXT  NULL,
    PortalUrl      TEXT NULL,
    CreatedBy      INT           NULL,
    CreatedOn      DATETIME     NOT NULL,
    UpdatedOn      DATETIME     NOT NULL
)
GO
CREATE INDEX IX_Proposals_Token ON Proposals (Token)
GO
CREATE INDEX IX_Proposals_Lead ON Proposals (LeadId)
GO
-- What happened to each proposal: sent (with recipients), viewed, accepted, payment link, paid.
CREATE TABLE ProposalEvents (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ProposalId  INT           NOT NULL REFERENCES Proposals(Id) ON DELETE CASCADE,
    Kind        TEXT  NOT NULL,
    Detail      TEXT NULL,
    UserId      INT           NULL,
    Ip          TEXT  NULL,
    CreatedOn   DATETIME     NOT NULL
)
GO
CREATE INDEX IX_ProposalEvents_Proposal ON ProposalEvents (ProposalId, CreatedOn)
GO
-- API keys for the AI assistant, tried in order; a key that hits its limit rests and the next one answers.
CREATE TABLE AiKeys (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    Label          TEXT  NOT NULL,
    Model          TEXT  NOT NULL,
    KeySecret      TEXT NOT NULL,
    KeyHint        TEXT  NULL,
    Sort           INT           NOT NULL DEFAULT 0,
    IsActive       INTEGER           NOT NULL DEFAULT 1,
    DailyLimit     INT           NOT NULL DEFAULT 0,
    TodayDate      TEXT  NULL,
    TodayCount     INT           NOT NULL DEFAULT 0,
    RestUntil      DATETIME     NULL,
    Status         TEXT  NULL,
    LastError      TEXT NULL,
    LastErrorOn    DATETIME     NULL,
    LastUsedOn     DATETIME     NULL,
    Requests       INT           NOT NULL DEFAULT 0,
    InputTokens    INTEGER        NOT NULL DEFAULT 0,
    OutputTokens   INTEGER        NOT NULL DEFAULT 0,
    CreatedOn      DATETIME     NOT NULL
)
GO
-- Conversations with the AI assistant (kept for quality review; deleted with analytics retention).
CREATE TABLE AiChats (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    ChatKey      TEXT  NOT NULL,
    VisitorId    TEXT  NULL,
    Ip           TEXT  NULL,
    Page         TEXT NULL,
    Messages     INT           NOT NULL DEFAULT 0,
    LeadId       INT           NULL,
    Transcript   TEXT NULL,
    StartedOn    DATETIME     NOT NULL,
    LastOn       DATETIME     NOT NULL
)
GO
CREATE INDEX IX_AiChats_Key ON AiChats (ChatKey)
GO
CREATE INDEX IX_AiChats_Last ON AiChats (LastOn)
GO
-- Outgoing webhooks to other apps (billing / project tracking), with the last answer for troubleshooting.
CREATE TABLE WebhookLog (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Event       TEXT  NOT NULL,
    Url         TEXT NOT NULL,
    StatusCode  INT           NULL,
    Response    TEXT NULL,
    Payload     TEXT NULL,
    CreatedOn   DATETIME     NOT NULL
)
GO
CREATE INDEX IX_WebhookLog_Created ON WebhookLog (CreatedOn)
GO
ALTER TABLE CrmLeads ADD Score INT NULL
GO
ALTER TABLE CrmLeads ADD ScoreJson TEXT NULL
GO
ALTER TABLE CrmLeads ADD ScoredOn DATETIME NULL
GO
ALTER TABLE CrmLeads ADD PriorityLocked INTEGER NOT NULL DEFAULT 0
GO
ALTER TABLE CrmLeads ADD PortalUrl TEXT NULL
GO
