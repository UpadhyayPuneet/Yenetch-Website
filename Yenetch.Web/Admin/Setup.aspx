<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="Yenetch.Web.Admin.Setup" %>
<!doctype html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Set up · Yenetch Admin</title>
    <link rel="icon" href="/assets/img/favicon.svg" type="image/svg+xml" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&amp;family=Inter+Tight:wght@600;700&amp;display=swap" />
    <link rel="stylesheet" href="/assets/admin/admin.css?v=10" />
</head>
<body>
<main class="auth">
    <form id="form1" runat="server" class="auth__card">
        <a class="brand" href="/"><%= Yenetch.Web.Admin.AdminMaster.LogoSvg %>Yenetch<small>Admin</small></a>
        <h1>Create the first admin</h1>
        <p>This runs once. Enter the setup key from Web.config (appSettings AdminSetupKey) to prove you manage this server.</p>
        <asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
        <div class="row"><label class="label" for="SetupKey">Setup key</label><asp:TextBox ID="SetupKey" runat="server" CssClass="field" autocomplete="off" /></div>
        <div class="row"><label class="label" for="FullName">Your name</label><asp:TextBox ID="FullName" runat="server" CssClass="field" MaxLength="120" autocomplete="name" /></div>
        <div class="row"><label class="label" for="Email">Email</label><asp:TextBox ID="Email" runat="server" CssClass="field" inputmode="email" MaxLength="160" autocomplete="username" /></div>
        <div class="row"><label class="label" for="Password">Password</label><asp:TextBox ID="Password" runat="server" CssClass="field" TextMode="Password" autocomplete="new-password" /><span class="muted small">At least 10 characters, with letters and a number.</span></div>
        <asp:Button ID="CreateButton" runat="server" Text="Create admin and sign in" CssClass="btn btn--blue" OnClick="CreateButton_Click" />
    </form>
</main>
</body>
</html>
