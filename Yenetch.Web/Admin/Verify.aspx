<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Verify.aspx.cs" Inherits="Yenetch.Web.Admin.VerifyPage" %>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Two-step sign-in · Yenetch Admin</title>
    <link rel="icon" href="/assets/img/favicon.svg" type="image/svg+xml" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&amp;family=Inter+Tight:wght@600;700&amp;display=swap" />
    <link rel="stylesheet" href="/assets/admin/admin.css?v=10" />
</head>
<body>
<main class="auth">
    <form method="post" class="auth__card" autocomplete="off">
        <a class="brand" href="/"><%= Yenetch.Web.Admin.AdminMaster.LogoSvg %>Yenetch<small>Admin</small></a>
        <h1>Two-step sign-in</h1>
        <% if (MethodName == "app") { %>
        <p>Open your authenticator app and enter the 6-digit code for Yenetch.</p>
        <% } else { %>
        <p>We emailed a 6-digit code to <b><%: MaskedEmail %></b>. It works for 10 minutes.</p>
        <% } %>
        <% if (Err != null) { %><div class="form-error" role="alert"><%: Err %></div><% } %>
        <% if (Info != null) { %><div class="form-ok" role="status"><%: Info %></div><% } %>
        <div class="row"><label class="label" for="code">Code</label>
            <input class="field otp" id="code" name="code" inputmode="numeric" autocomplete="one-time-code" maxlength="12" autofocus required placeholder="123456" /></div>
        <% if (AllowTrust) { %><div class="row"><label class="check"><input type="checkbox" name="trust" value="1" /> Trust this browser for 30 days</label></div><% } %>
        <button class="btn btn--blue" type="submit" name="act" value="verify">Verify and sign in</button>
        <div class="auth__alt">
            <% if (MethodName == "app") { %>
            <button class="linkbtn" type="submit" name="act" value="email" formnovalidate>Email me a code instead</button>
            <span>Lost your phone? Enter one of your backup codes above.</span>
            <% } else { %>
            <button class="linkbtn" type="submit" name="act" value="email" formnovalidate>Send a new code</button>
            <% } %>
            <a href="/admin/login?signout=1">Start again</a>
        </div>
    </form>
</main>
</body>
</html>
