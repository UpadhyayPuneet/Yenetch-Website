using System;
using System.Collections.Generic;
using System.Linq;
using Yenetch.Crm;

namespace Yenetch.Web.Admin
{
    /// <summary>/admin/team. Admins add teammates, change roles, switch sign-in off and reset passwords.</summary>
    public partial class Team : AdminPage
    {
        public override string Section { get { return "team"; } }
        protected override bool Allowed(CrmUser u) { return u.IsAdmin; }

        protected List<CrmUser> Users;
        protected CrmUser Editing;

        protected void Page_Load(object sender, EventArgs e)
        {
            Users = Auth.Users();
            var editId = QInt("edit");
            Editing = editId > 0 ? Users.FirstOrDefault(u => u.Id == editId) : null;
            EditPanel.Visible = Editing != null;
            AddPanel.Visible = Editing == null;
            if (IsPostBack) return;

            foreach (var list in new[] { NewRole, EditRole })
                foreach (var r in Lists.Roles) list.Items.Add(r);
            NewRole.SelectedValue = "Sales";
            if (Editing != null)
            {
                EditName.Text = Editing.Name;
                EditRole.SelectedValue = Editing.Role;
                EditActive.Checked = Editing.IsActive;
            }
        }

        protected void AddButton_Click(object sender, EventArgs e)
        {
            var email = NewEmail.Text.Trim();
            var name = NewName.Text.Trim();
            if (name.Length == 0 || !Newsletter.IsEmail(email)) { RedirectWith(Request.RawUrl, "Enter a name and a valid email."); return; }
            if (Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase))) { RedirectWith(Request.RawUrl, email + " is already on the team."); return; }
            var problem = Auth.PasswordProblem(NewPassword.Text);
            if (problem != null) { RedirectWith(Request.RawUrl, problem); return; }
            Auth.CreateUser(email, name, NewRole.SelectedValue, NewPassword.Text);
            RedirectWith("/admin/team", name + " can now sign in at /admin with " + email + ".");
        }

        protected void SaveButton_Click(object sender, EventArgs e)
        {
            if (Editing == null) return;
            var name = EditName.Text.Trim();
            if (name.Length == 0) { RedirectWith(Request.RawUrl, "Enter a name."); return; }
            var role = EditRole.SelectedValue;
            var active = EditActive.Checked;

            // Never leave the team without an active admin (including locking yourself out).
            var losesAdmin = Editing.IsAdmin && Editing.IsActive && (role != "Admin" || !active);
            if (losesAdmin && Users.Count(u => u.IsAdmin && u.IsActive) <= 1) { RedirectWith(Request.RawUrl, "Keep at least one active Admin. Make someone else an Admin first."); return; }
            if (Editing.Id == Me.Id && !active) { RedirectWith(Request.RawUrl, "You cannot switch off your own sign-in."); return; }

            if (EditPassword.Text.Length > 0)
            {
                var problem = Auth.PasswordProblem(EditPassword.Text);
                if (problem != null) { RedirectWith(Request.RawUrl, problem); return; }
                Auth.SetPassword(Editing.Id, EditPassword.Text);
            }
            Auth.UpdateUser(Editing.Id, name, role, active);
            RedirectWith("/admin/team", name + " saved" + (EditPassword.Text.Length > 0 ? " with a new password." : "."));
        }
    }
}
