using DotNetNuke.Abstractions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication.OAuth;
using DotNetNuke.UI.Skins.Controls;
using log4net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DotNetNuke.Authentication.Azure.B2C.Components
{
    internal class LoginRequestManager
    {
        private enum LoginStageEnum
        {
            Init,
            Start,
            Cancelled,
            OtherProblem,
            PreAuthorise,
            PostAuthorise,
            Denied,
            Finish
        }

        internal enum LoginOutcome
        {
            None,
            ShowMessage,
            Redirect
        }

        private readonly HttpRequest _request;
        private readonly OAuthClientBase _oAuthClient;
        private readonly ILog _log;
        private readonly Func<string, string, string> _getResource;
        private readonly string _sharedResourceFile;
        private readonly PortalSettings _portalSettings;
        private readonly INavigationManager _navigationManager;
        private readonly AzureConfig _config;
        private readonly List<(LoginStageEnum stage, string message)> _loginStageMessageBases;
        private const string ForgottenPasswordErrorCode = "AADB2C90118";
        private const string UserCancellationErrorCode = "AADB2C90118";

        internal bool InError { get; set; }
        internal string Error { get; set; }
        internal string ErrorDescription { get; set; }

        internal bool ForgottenPassword =>
            Error.IndexOf(ForgottenPasswordErrorCode, StringComparison.OrdinalIgnoreCase) == 0;

        internal bool ProblemThatIsNotPasswordForgotten => InError && !ForgottenPassword;

        internal bool UserCancelled => InError && Error.IndexOf(UserCancellationErrorCode, StringComparison.OrdinalIgnoreCase) == 0;

        private bool OnErrorUriUnset => string.IsNullOrEmpty(_config.OnErrorUri);

        private string PrivateConfirmation => _getResource("PrivateConfirmationMessage", _sharedResourceFile);

        private string DeniedReason => ((AzureClient)_oAuthClient).UnauthorizedReason ?? PrivateConfirmation;

        internal LoginRequestManager(HttpRequest request, OAuthClientBase oAuthClient, ILog log, Func<string,string,string> getResource, string localResourceFile,string sharedResourceFile,  PortalSettings portalSettings, INavigationManager navigationManager, AzureConfig config)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request), "Cannot construct manager with null request");
            _oAuthClient = oAuthClient;
            _log = log;
            _getResource = getResource;
            _sharedResourceFile = sharedResourceFile;
            _portalSettings = portalSettings;
            _navigationManager = navigationManager;
            _config = config;

            Error = request["error"] ?? string.Empty;
            InError = !string.IsNullOrEmpty(Error);
            ErrorDescription = request["error_description"] ?? string.Empty;

            _loginStageMessageBases = new List<(LoginStageEnum, string)>
            {
                (LoginStageEnum.Init,"Login.OnInit: Request URL = {0}" ),
                (LoginStageEnum.Start,"Login.loginButton_Click Start" ),
                (LoginStageEnum.Cancelled,"Login.loginButton_Click: AADB2C90091: The user has cancelled entering self-asserted information. User clicked on Cancel when resetting the password => Redirect to the login page"),
                (LoginStageEnum.OtherProblem, getResource("LoginError", localResourceFile)),
                (LoginStageEnum.PreAuthorise, "Login.loginButton_Click: Calling Authorize"),
                (LoginStageEnum.PostAuthorise,"Login.loginButton_Click: result={0}" ),
                (LoginStageEnum.Denied, "Login control - Authorization has been denied"),
                (LoginStageEnum.Finish,"Login.loginButton_Click End")
            };
        }

        internal (LoginOutcome outcome , string messageOrUri, ModuleMessage.ModuleMessageType messageType) ProcessRequest()
        {
            Log(LoginStageEnum.Start, LogLevel.Debug);

            if (ProblemThatIsNotPasswordForgotten) return ProcessNonPasswordForgottenIssue();

            UpdatePolicyIfForgottenPassword();

            Log(LoginStageEnum.PreAuthorise, LogLevel.Debug);
            var result = _oAuthClient.Authorize();
            Log(LoginStageEnum.PostAuthorise, LogLevel.Debug, result);

            if (result != AuthorisationResult.Denied) return (LoginOutcome.None, string.Empty, ModuleMessage.ModuleMessageType.GreenSuccess);

            Log(LoginStageEnum.Denied, LogLevel.Debug);

            return OnErrorUriUnset
                ? (LoginOutcome.ShowMessage, PrivateConfirmation, ModuleMessage.ModuleMessageType.YellowWarning)
                : (LoginOutcome.Redirect, $"{_config.OnErrorUri}?error=Denied&error_description={HttpContext.Current.Server.UrlEncode(DeniedReason)}", ModuleMessage.ModuleMessageType.YellowWarning);

        }

        internal void LogEnd()
        {
            Log(LoginStageEnum.Finish, LogLevel.Debug);
        }

        internal void LogInit(string url)
        {
            Log(LoginStageEnum.Init, LogLevel.Debug, url);
        }

        private (LoginOutcome outcome, string messageOrUri, ModuleMessage.ModuleMessageType messageType) ProcessNonPasswordForgottenIssue()
        {
            if (UserCancelled)
            {
                Log(LoginStageEnum.Cancelled, LogLevel.Debug);

                return (LoginOutcome.Redirect, Common.Utils.GetLoginUrl(_portalSettings, _request, _navigationManager), ModuleMessage.ModuleMessageType.GreenSuccess);
            }

            var errorMessage = Log(LoginStageEnum.OtherProblem, LogLevel.Error, Error, ErrorDescription);

            return OnErrorUriUnset
                ? (LoginOutcome.ShowMessage, errorMessage, ModuleMessage.ModuleMessageType.RedError)
                : (LoginOutcome.Redirect, $"{_config.OnErrorUri}?error={Error}&error_description={HttpContext.Current.Server.UrlEncode(ErrorDescription)}", ModuleMessage.ModuleMessageType.RedError);
        }

        private void UpdatePolicyIfForgottenPassword()
        {
            if(ForgottenPassword) ((AzureClient)_oAuthClient).Policy = AzureClient.PolicyEnum.PasswordResetPolicy;
        }

        private string Log(LoginStageEnum stage, LogLevel level, params object[] logDetails)
        {
            var baseMessage = _loginStageMessageBases.FirstOrDefault(l => l.stage == stage).message;

            var formatStages = new[]
                { LoginStageEnum.Init, LoginStageEnum.OtherProblem, LoginStageEnum.PostAuthorise };

            if (formatStages.Contains(stage))
            {
                baseMessage = string.Format(baseMessage, logDetails);
            }

            switch (level)
            {
                case LogLevel.None:
                    break;
                case LogLevel.Information:
                    _log.Info(baseMessage);
                    break;
                case LogLevel.Debug:
                    _log.Debug(baseMessage);
                    break;
                case LogLevel.Error:
                    _log.Error(baseMessage);
                    break;
                case LogLevel.Trace:
                case LogLevel.Warning:
                case LogLevel.Critical:
                default:
                    _log.Warn(baseMessage);
                    break;
            }

            return baseMessage;
        }

    }
}
