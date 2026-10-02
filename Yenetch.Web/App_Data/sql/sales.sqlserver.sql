-- Sales tools (v11): quotes from the plan builder, proposals with payment links, the AI assistant, lead scores and webhooks.
-- Quotes built by visitors on /pricing (each one also creates or updates a lead).
CREATE TABLE dbo.Quotes (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Token       NVARCHAR(40)  NOT NULL,
    LeadId      INT           NULL,
    Name        NVARCHAR(120) NULL,
    Email       NVARCHAR(160) NULL,
    Phone       NVARCHAR(40)  NULL,
    Company     NVARCHAR(160) NULL,
    Notes       NVARCHAR(2000) NULL,
    ItemsJson   NVARCHAR(MAX) NOT NULL,
    Currency    NVARCHAR(3)   NOT NULL,
    Rate        DECIMAL(18,8) NOT NULL DEFAULT 1,
    OneTimeInr  DECIMAL(14,2) NOT NULL DEFAULT 0,
    MonthlyInr  DECIMAL(14,2) NOT NULL DEFAULT 0,
    DiscountInr DECIMAL(14,2) NOT NULL DEFAULT 0,
    OfferCode   NVARCHAR(40)  NULL,
    VisitorId   NVARCHAR(40)  NULL,
    Ip          NVARCHAR(64)  NULL,
    CreatedOn   DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_Quotes_Created ON dbo.Quotes (CreatedOn DESC)
GO
CREATE INDEX IX_Quotes_Token ON dbo.Quotes (Token)
GO
-- Proposals sent to clients, shared at /proposal/{token}.
CREATE TABLE dbo.Proposals (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Number         NVARCHAR(30)  NOT NULL,
    Token          NVARCHAR(40)  NOT NULL,
    LeadId         INT           NULL,
    QuoteId        INT           NULL,
    Title          NVARCHAR(200) NOT NULL,
    ClientName     NVARCHAR(160) NULL,
    ClientCompany  NVARCHAR(160) NULL,
    ClientEmail    NVARCHAR(160) NULL,
    ClientPhone    NVARCHAR(40)  NULL,
    Intro          NVARCHAR(MAX) NULL,
    ItemsJson      NVARCHAR(MAX) NOT NULL,
    Currency       NVARCHAR(3)   NOT NULL,
    TaxName        NVARCHAR(40)  NULL,
    TaxPct         DECIMAL(6,3)  NOT NULL DEFAULT 0,
    DiscountPct    DECIMAL(6,3)  NOT NULL DEFAULT 0,
    Subtotal       DECIMAL(14,2) NOT NULL DEFAULT 0,
    Discount       DECIMAL(14,2) NOT NULL DEFAULT 0,
    Tax            DECIMAL(14,2) NOT NULL DEFAULT 0,
    Total          DECIMAL(14,2) NOT NULL DEFAULT 0,
    DepositPct     DECIMAL(6,3)  NOT NULL DEFAULT 0,
    Terms          NVARCHAR(MAX) NULL,
    ValidUntil     DATETIME2     NULL,
    Status         NVARCHAR(20)  NOT NULL DEFAULT 'Draft',
    SentOn         DATETIME2     NULL,
    FirstViewedOn  DATETIME2     NULL,
    LastViewedOn   DATETIME2     NULL,
    Views          INT           NOT NULL DEFAULT 0,
    AcceptedOn     DATETIME2     NULL,
    AcceptedName   NVARCHAR(160) NULL,
    AcceptedIp     NVARCHAR(64)  NULL,
    DeclinedOn     DATETIME2     NULL,
    DeclineReason  NVARCHAR(1000) NULL,
    PayLinkId      NVARCHAR(60)  NULL,
    PayLinkUrl     NVARCHAR(400) NULL,
    PayAmount      DECIMAL(14,2) NULL,
    PayStatus      NVARCHAR(20)  NULL,
    PaidOn         DATETIME2     NULL,
    AmountPaid     DECIMAL(14,2) NULL,
    PaymentId      NVARCHAR(60)  NULL,
    PortalUrl      NVARCHAR(400) NULL,
    CreatedBy      INT           NULL,
    CreatedOn      DATETIME2     NOT NULL,
    UpdatedOn      DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_Proposals_Token ON dbo.Proposals (Token)
GO
CREATE INDEX IX_Proposals_Lead ON dbo.Proposals (LeadId)
GO
-- What happened to each proposal: sent (with recipients), viewed, accepted, payment link, paid.
CREATE TABLE dbo.ProposalEvents (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProposalId  INT           NOT NULL REFERENCES dbo.Proposals(Id) ON DELETE CASCADE,
    Kind        NVARCHAR(30)  NOT NULL,
    Detail      NVARCHAR(2000) NULL,
    UserId      INT           NULL,
    Ip          NVARCHAR(64)  NULL,
    CreatedOn   DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_ProposalEvents_Proposal ON dbo.ProposalEvents (ProposalId, CreatedOn)
GO
-- API keys for the AI assistant, tried in order; a key that hits its limit rests and the next one answers.
CREATE TABLE dbo.AiKeys (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Label          NVARCHAR(80)  NOT NULL,
    Model          NVARCHAR(60)  NOT NULL,
    KeySecret      NVARCHAR(1000) NOT NULL,
    KeyHint        NVARCHAR(20)  NULL,
    Sort           INT           NOT NULL DEFAULT 0,
    IsActive       BIT           NOT NULL DEFAULT 1,
    DailyLimit     INT           NOT NULL DEFAULT 0,
    TodayDate      NVARCHAR(10)  NULL,
    TodayCount     INT           NOT NULL DEFAULT 0,
    RestUntil      DATETIME2     NULL,
    Status         NVARCHAR(30)  NULL,
    LastError      NVARCHAR(400) NULL,
    LastErrorOn    DATETIME2     NULL,
    LastUsedOn     DATETIME2     NULL,
    Requests       INT           NOT NULL DEFAULT 0,
    InputTokens    BIGINT        NOT NULL DEFAULT 0,
    OutputTokens   BIGINT        NOT NULL DEFAULT 0,
    CreatedOn      DATETIME2     NOT NULL
)
GO
-- Conversations with the AI assistant (kept for quality review; deleted with analytics retention).
CREATE TABLE dbo.AiChats (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    ChatKey      NVARCHAR(40)  NOT NULL,
    VisitorId    NVARCHAR(40)  NULL,
    Ip           NVARCHAR(64)  NULL,
    Page         NVARCHAR(300) NULL,
    Messages     INT           NOT NULL DEFAULT 0,
    LeadId       INT           NULL,
    Transcript   NVARCHAR(MAX) NULL,
    StartedOn    DATETIME2     NOT NULL,
    LastOn       DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_AiChats_Key ON dbo.AiChats (ChatKey)
GO
CREATE INDEX IX_AiChats_Last ON dbo.AiChats (LastOn DESC)
GO
-- Outgoing webhooks to other apps (billing / project tracking), with the last answer for troubleshooting.
CREATE TABLE dbo.WebhookLog (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Event       NVARCHAR(60)  NOT NULL,
    Url         NVARCHAR(400) NOT NULL,
    StatusCode  INT           NULL,
    Response    NVARCHAR(1000) NULL,
    Payload     NVARCHAR(MAX) NULL,
    CreatedOn   DATETIME2     NOT NULL
)
GO
CREATE INDEX IX_WebhookLog_Created ON dbo.WebhookLog (CreatedOn DESC)
GO
ALTER TABLE dbo.CrmLeads ADD Score INT NULL
GO
ALTER TABLE dbo.CrmLeads ADD ScoreJson NVARCHAR(MAX) NULL
GO
ALTER TABLE dbo.CrmLeads ADD ScoredOn DATETIME2 NULL
GO
ALTER TABLE dbo.CrmLeads ADD PriorityLocked BIT NOT NULL DEFAULT 0
GO
ALTER TABLE dbo.CrmLeads ADD PortalUrl NVARCHAR(400) NULL
GO
