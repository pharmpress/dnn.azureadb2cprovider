#region Copyright

// 
// Intelequia Software solutions - https://intelequia.com
// Copyright (c) 2019
// by Intelequia Software Solutions
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

#region Usings
using DotNetNuke.Abstractions;
using DotNetNuke.Abstractions.Logging;
using DotNetNuke.Abstractions.Portals;
using DotNetNuke.Authentication.Azure.B2C.Components;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication;
using DotNetNuke.Services.Authentication.OAuth;
using DotNetNuke.Services.Localization;
using log4net;
using System;
using System.Collections.Specialized;
#endregion

namespace DotNetNuke.Authentication.Azure.B2C
{
    public partial class Login : OAuthLoginBase
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(Login));
        private AzureConfig _config;

        private LoginRequestManager _loginRequestManager;

        protected override string AuthSystemApplicationName => AzureConfig.ServiceName;

        public override bool SupportsRegistration => true;

        protected override void AddCustomProperties(NameValueCollection properties)
        {
            ((AzureClient)OAuthClient).AddCustomProperties(properties);
        }

        protected override UserData GetCurrentUser()
        {
            return OAuthClient.GetCurrentUser<AzureUserData>();
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            loginButton.Click += LoginButton_Click;
            registerButton.Click += LoginButton_Click;

            OAuthClient = new AzureClient(PortalId, Mode, DependencyProvider.GetService(typeof(IEventLogService)) as IEventLogService, DependencyProvider.GetService(typeof(IPortalAliasService)) as IPortalAliasService);

            loginItem.Visible = Mode == AuthMode.Login;
            registerItem.Visible = Mode == AuthMode.Register;

            _config = new AzureConfig(AzureConfig.ServiceName, PortalId);

            _loginRequestManager = new LoginRequestManager(
                Request, 
                OAuthClient, 
                _logger, 
                Localization.GetString, 
                LocalResourceFile, 
                Localization.SharedResourceFile, 
                PortalSettings.Current,
                DependencyProvider.GetService(typeof(INavigationManager)) as INavigationManager, 
                _config);

            _loginRequestManager.LogInit(Request.RawUrl);

            if(_loginRequestManager.InitiateLogin) LoginButton_Click(null, null);
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            var (outcome, messageOrUri, messageType) = _loginRequestManager.ProcessRequest();

            if (outcome == LoginRequestManager.LoginOutcome.ShowMessage)
            {
                UI.Skins.Skin.AddModuleMessage(this, messageOrUri, messageType);
            }
            else if (outcome == LoginRequestManager.LoginOutcome.Redirect)
            {
                Response.Redirect(messageOrUri);
            }

            _loginRequestManager.LogEnd();
        }
    }
}