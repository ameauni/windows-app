using System;
using System.Drawing;
using System.Windows.Forms;
using EduroamConfigure;
using System.Linq;
using System.Threading.Tasks;
using ManagedNativeWifi;

namespace EduroamApp
{
	public partial class frmLogin : Form
	{
		private readonly frmParent frmParent;
		private readonly EapConfig.AuthenticationMethod authMethod;
		private bool usernameDefault = true;
		private bool passwordDefault = true;
		private bool passwordSet;
		private bool usernameValid = false;
		private string realm;
		private bool hint;
		public bool connected;

		public frmLogin(frmParent parentInstance)
		{
			frmParent = parentInstance;
			authMethod = frmParent.eapConfig.AuthenticationMethods.First();
			InitializeComponent();
		}

		// TODO: if pressing enter in username field and Connect not active, display rules

		private void frmLogin_Load(object sender, EventArgs e)
		{
			if (!frmParent.EduroamAvailable)
			{
				// prompt user to save config if not
				SaveAndQuit();
			}

			realm = authMethod.ClientInnerIdentitySuffix;
			hint = authMethod.ClientInnerIdentityHint;

			usernameValid = false;
			lblRules.Visible = true;
			frmParent.BtnNextEnabled = false;
			// shows helping (placeholder) text by default
			txtUsername.Text = "Username";
			txtUsername.ForeColor = SystemColors.GrayText;
			txtPassword.Text = "Password";
			txtPassword.ForeColor = SystemColors.GrayText;
			txtPassword.UseSystemPasswordChar = false;

			if (!string.IsNullOrEmpty(frmParent.InstId))
			{
				lblInst.Text = "@" + authMethod.ClientInnerIdentitySuffix;
			}
			else
			{
				lblInst.Text = "";
			}

			// if realm is provided and subrealms not allowed always show realm at end
			if (!string.IsNullOrEmpty(realm) && hint)
			{
				lblInst.Visible = true;
			}

		}

		// removes helping text when field is in focus
		private void txtUsername_Enter(object sender, EventArgs e)
		{
			//lblRules.Visible = false;
			if (txtUsername.Text == "Username" && usernameDefault)
			{
				txtUsername.Text = "";
				txtUsername.ForeColor = SystemColors.ControlText;
				usernameDefault = false;
			}
		}

		// removes helping text when field is in focus
		private void txtPassword_Enter(object sender, EventArgs e)
		{
			if (txtPassword.Text == "Password" && passwordDefault)
			{
				txtPassword.Text = "";
				txtPassword.ForeColor = SystemColors.ControlText;
				txtPassword.UseSystemPasswordChar = true;
				passwordDefault = false;
			}
		}

		// shows helping text when field loses focus and is empty
		private void txtUsername_Leave(object sender, EventArgs e)
		{
			lblRules.Visible = true;
			if (txtUsername.Text == "")
			{
				txtUsername.Text = "Username";
				txtUsername.ForeColor = SystemColors.GrayText;
				usernameDefault = true;
				lblRules.Text = "";
			}
			else
			{
				usernameDefault = false;
			}

			if (!txtUsername.Text.Contains('@') && !string.IsNullOrEmpty(realm) && !usernameDefault)
			{
				lblInst.Visible = true;
			}

		}

		public void ValidateFields()
		{
			string username = txtUsername.Text;
			if (username == "Username" || username == "" )
			{
				usernameValid = false;
				lblRules.Text = "";
				return;
			}

			// if username does not contain '@' and realm is given then show realm added to end
			if (!username.Contains('@') && !string.IsNullOrEmpty(realm))
			{
				username += "@" + realm;
			}


			string brokenRules = IdentityProviderParser.GetBrokenRules(username, realm, hint);
			usernameValid = string.IsNullOrEmpty(brokenRules);
			lblRules.Text = "";
			if (!usernameValid && !usernameDefault)
			{
				lblRules.Text = brokenRules;
			}
			else
			{
				lblRules.Text = "";
			}

			frmParent.BtnNextEnabled = (passwordSet && usernameValid) || connected;
		}

		// shows helping text when field loses focus and is empty
		private void txtPassword_Leave(object sender, EventArgs e)
		{
			if (txtPassword.Text == "")
			{
				txtPassword.Text = "Password";
				txtPassword.ForeColor = SystemColors.GrayText;
				txtPassword.UseSystemPasswordChar = false;
				passwordDefault = true;
			}
			else
			{
				passwordDefault = false;
			}
		}

		public void ConnectWithLogin()
		{
			string username = txtUsername.Text;
			if (lblInst.Visible)
			{
				username += lblInst.Text;
			}
			string password = txtPassword.Text;

			ConnectToEduroam.InstallUserProfile(username, password, frmParent.AuthMethod);
			Connect();

		}

		private void txtUsername_TextChanged(object sender, EventArgs e)
		{
			lblRules.Visible = false;
			lblInst.Visible = false;
			ValidateFields();
		}

		private void txtPassword_TextChanged(object sender, EventArgs e)
		{
			passwordSet = !string.IsNullOrEmpty(txtPassword.Text) && !passwordDefault && txtPassword.ContainsFocus;
			ValidateFields();
		}



		private async void Connect()
		{
			// displays loading animation while attempt to connect
			lblStatus.Text = "Connecting...";
			pbxStatus.Image = Properties.Resources.loading_gif;
			lblStatus.Visible = true;
			pbxStatus.Visible = true;

			bool connectSuccess;
			// tries to connect
			try
			{
				connectSuccess = await Task.Run(ConnectToEduroam.TryToConnect);
			}
			catch (Exception ex)
			{
				// if an exception is thrown, connection has not succeeded
				connectSuccess = false;
				MessageBox.Show("Could not connect. \nException: " + ex.Message);
			}

			// double check to validate wether eduroam really is an active connection
			var eduConnected = false;
			if (connectSuccess)
			{
				var checkConnected = NativeWifi.EnumerateConnectedNetworkSsids();
				foreach (NetworkIdentifier network in checkConnected)
				{
					if (network.ToString() == "eduroam")
					{
						eduConnected = true;
					}
				}
			}

			if (eduConnected)
			{
				lblStatus.Text = "You are now connected to eduroam.\n\nPress Close to exit the wizard.";
				pbxStatus.Image = Properties.Resources.green_checkmark;
				frmParent.BtnNextText = "Close";
				frmParent.BtnNextEnabled = true;
				frmParent.BtnBackVisible = false;
				frmParent.ProfileCondition = "GOODPROFILE";
			}
			else
			{
				lblStatus.Text = "Connection to eduroam failed.";
				pbxStatus.Image = Properties.Resources.red_x;
				lblConnectFailed.Visible = true;
				frmParent.BtnBackEnabled = true;
				frmParent.ProfileCondition = "BADPROFILE";
			}
			connected = eduConnected;
		}

		// gives user choice of wether they want to save the configuration before quitting
		private void SaveAndQuit()
		{
			frmParent.ProfileCondition = "GOODPROFILE";
			pnlEduNotAvail.Visible = true;
		}

	}
}
