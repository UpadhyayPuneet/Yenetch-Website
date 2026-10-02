<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="Yenetch.Web.Admin.Login" %>
<!doctype html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Sign in · Yenetch Admin</title>
    <link rel="icon" href="/assets/img/favicon.svg" type="image/svg+xml" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&amp;family=Inter+Tight:wght@600;700&amp;display=swap" />
    <link rel="stylesheet" href="/assets/admin/admin.css?v=10" />
</head>
<body>
<main class="auth">
    <form id="form1" runat="server" class="auth__card">
        <a class="brand" href="/"><%= Yenetch.Web.Admin.AdminMaster.LogoSvg %>Yenetch<small>Admin</small></a>
        <h1>Sign in</h1>
        <p>Leads, follow-ups, website analytics and the newsletter.</p>
        <asp:PlaceHolder ID="ErrorBox" runat="server" Visible="false"><div class="form-error" role="alert"><asp:Literal ID="ErrorText" runat="server" /></div></asp:PlaceHolder>
        <div class="row"><label class="label" for="Email">Email</label><asp:TextBox ID="Email" runat="server" CssClass="field" inputmode="email" autocomplete="username" MaxLength="160" /></div>
        <div class="row"><label class="label" for="Password">Password</label><asp:TextBox ID="Password" runat="server" CssClass="field" TextMode="Password" autocomplete="current-password" /></div>
        <div class="row"><label class="check"><asp:CheckBox ID="Remember" runat="server" /> Keep me signed in for 14 days</label></div>
        <asp:Button ID="SignInButton" runat="server" Text="Sign in" CssClass="btn btn--blue" OnClick="SignInButton_Click" />
    </form>
</main>
</body>
</html>
