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

using DotNetNuke.Abstractions.Application;
using DotNetNuke.Abstractions.Portals;
using DotNetNuke.Authentication.Azure.B2C.Components.Graph;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication.OAuth;
using DotNetNuke.UI.WebControls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotNetNuke.Authentication.Azure.B2C.Components
{
    public class AzureConfig : OAuthConfigBase
    {
        public const string ServiceName = "AzureB2C";
        private const string ApiKeyNamePart = "_APIKey";
        private const string ApiSecretNamePart = "_APISecret";
        private readonly IHostSettingsService _hostSettingsService;
        private readonly IPortalController _portalController;
        private readonly Dictionary<string, string> _portalSettings;
        private readonly string _service;
        private readonly int _portalId;
        private const string _cacheKey = "Authentication";

        #region config items
        [SortOrder(1)]
        public string TenantName { get; set; }

        [SortOrder(2)]
        public string TenantId { get; set; }
        [SortOrder(3)]
        public bool AutoRedirect { get; set; }
        [SortOrder(4)]
        public string SignUpPolicy { get; set; }
        [SortOrder(5)]
        public string ProfilePolicy { get; set; }
        [SortOrder(6)]
        public string PasswordResetPolicy { get; set; }
        [SortOrder(7)]
        public string AADApplicationId { get; set; }
        [SortOrder(8)]
        public string AADApplicationKey { get; set; }
        [SortOrder(8)]
        public string JwtAudiences { get; set; }
        [SortOrder(9)]
        public bool RoleSyncEnabled { get; set; }
        [SortOrder(10)]
        public bool ProfileSyncEnabled { get; set; }
        [SortOrder(11)]
        public bool JwtAuthEnabled { get; set; }

        [SortOrder(12)]
        public string APIResource { get; set; }
        [SortOrder(13)]
        public string Scopes { get; set; }
        [SortOrder(14)]
        public bool UseGlobalSettings { get; set; }
        [SortOrder(15)]
        public string RedirectUri { get; set; }
        [SortOrder(16)]
        public string B2cApplicationId { get; set; }

        [SortOrder(17)]
        public bool UsernamePrefixEnabled { get; set; }
        [SortOrder(18)]
        public bool GroupNamePrefixEnabled { get; set; }
        [SortOrder(19)]
        public string RopcPolicy { get; set; }
        [SortOrder(20)]
        public string ImpersonatePolicy { get; set; }
        [SortOrder(21)]
        public bool AutoAuthorize { get; set; }
        [SortOrder(22)]
        public string OnErrorUri { get; set; }
        [SortOrder(23)]
        public bool UserSyncEnabled { get; set; }
        [SortOrder(24)]
        public bool AutoMatchExistingUsers { get; set; }
        [SortOrder(25)]
        public bool ExpiredRolesSyncEnabled { get; set; }
        #endregion

        private AzureConfig() : base("", 0)
        { }
        protected internal AzureConfig(string service, int portalId) : base(service, portalId)
        {
            _hostSettingsService = DependencyProvider.GetService(typeof(IHostSettingsService)) as IHostSettingsService ?? throw new InvalidOperationException();
            _portalController = DependencyProvider.GetService(typeof(IPortalController)) as IPortalController ?? throw new InvalidOperationException();


            _service = service;
            _portalId = portalId;

            // Gets the settings scope (global or per portal)
            UseGlobalSettings = bool.Parse(_hostSettingsService.GetString(_service + "_UseGlobalSettings", "false"));

            if (!UseGlobalSettings)
            {
                _portalSettings = _portalController.GetPortalSettings(_portalId);
            }

            // Loads the scoped settings
            APIKey                  = GetScopedSetting(ApiKeyNamePart, "");
            APISecret               = GetScopedSetting(ApiSecretNamePart, "");
            RedirectUri             = GetScopedSetting("_RedirectUri", "");
            OnErrorUri              = GetScopedSetting("_OnErrorUri", "");
            TenantName              = GetScopedSetting("_TenantName", "");
            TenantId                = GetScopedSetting("_TenantId", "");
            SignUpPolicy            = GetScopedSetting("_SignUpPolicy", "");
            ProfilePolicy           = GetScopedSetting("_ProfilePolicy", "");
            PasswordResetPolicy     = GetScopedSetting("_PasswordResetPolicy", "");
            AutoRedirect            = bool.Parse(GetScopedSetting("_AutoRedirect", "false"));
            Enabled                 = bool.Parse(GetScopedSetting("_Enabled", "false"));
            AADApplicationId        = GetScopedSetting("_AADApplicationId", "");
            AADApplicationKey       = GetScopedSetting("_AADApplicationKey", "");
            JwtAudiences            = GetScopedSetting("_JwtAudiences", "");
            RoleSyncEnabled         = bool.Parse(GetScopedSetting("_RoleSyncEnabled", "false"));
            UserSyncEnabled         = bool.Parse(GetScopedSetting("_UserSyncEnabled", "false"));
            ProfileSyncEnabled      = bool.Parse(GetScopedSetting("_ProfileSyncEnabled", "false"));
            JwtAuthEnabled          = bool.Parse(GetScopedSetting("_JwtAuthEnabled", "false"));
            APIResource             = GetScopedSetting("_APIResource", "");
            Scopes                  = GetScopedSetting("_Scopes", "");
            B2cApplicationId        = GetScopedSetting("_B2CApplicationId", "");
            UsernamePrefixEnabled   = bool.Parse(GetScopedSetting("_UsernamePrefixEnabled", "true"));
            GroupNamePrefixEnabled  = bool.Parse(GetScopedSetting("_GroupNamePrefixEnabled", "true"));
            RopcPolicy              = GetScopedSetting("_RopcPolicy", "");
            ImpersonatePolicy       = GetScopedSetting("_ImpersonatePolicy", "");
            AutoAuthorize           = bool.Parse(GetScopedSetting("_AutoAuthorize", "true"));
            AutoMatchExistingUsers  = bool.Parse(GetScopedSetting("_AutoMatchExistingUsers", "false"));
            ExpiredRolesSyncEnabled = bool.Parse(ConfigurationManager.AppSettings["AzureB2C_ExpiredRolesSyncEnabled"] ?? "false");
        }

        private string GetScopedSetting(string key, string defaultValue)
        {
            return UseGlobalSettings 
                ? _hostSettingsService.GetString(_service + key, defaultValue) 
                : _portalSettings.TryGetValue(_service + key, out var result) 
                    ? result 
                    : defaultValue;
        }

        private void UpdateScopedSetting(string key, string value)
        {
            if (UseGlobalSettings)
                _hostSettingsService.Update(_service + key, value, true);
            else
                _portalController.UpdatePortalSetting(_portalId, _service + key, value, true, null, false);
        }

        //TODO: Part of the DataCache, understand usage
        private static string GetCacheKey(string service, int portalId)
        {
            return $"{_cacheKey}.{service}_{portalId}"; //_cacheKey + "." + service + "_" + portalId;
        }

        //TODO: Verify this usage - called from Settings.ascx.cs
        public new static AzureConfig GetConfig(string service, int portalId)
        {
            var key = GetCacheKey(service, portalId);
            var config = (AzureConfig)DataCache.GetCache(key);
            if (config == null)
            {
                config = new AzureConfig(service, portalId);
                DataCache.SetCache(key, config);
            }
            return config;
        }

        public void Update(AzureConfig newConfig)
        {
            _hostSettingsService.Update(_service + "_UseGlobalSettings", UseGlobalSettings.ToString(), true);

            UpdateScopedSetting(ApiKeyNamePart, newConfig.APIKey);
            UpdateScopedSetting(ApiSecretNamePart, newConfig.APISecret);
            UpdateScopedSetting("_RedirectUri", newConfig.RedirectUri);
            UpdateScopedSetting("_OnErrorUri", newConfig.OnErrorUri);
            UpdateScopedSetting("_TenantName", newConfig.TenantName);
            UpdateScopedSetting("_TenantId", newConfig.TenantId);
            UpdateScopedSetting("_AutoRedirect", newConfig.AutoRedirect.ToString());
            UpdateScopedSetting("_Enabled", newConfig.Enabled.ToString());
            UpdateScopedSetting("_SignUpPolicy", newConfig.SignUpPolicy);
            UpdateScopedSetting("_ProfilePolicy", newConfig.ProfilePolicy);
            UpdateScopedSetting("_PasswordResetPolicy", newConfig.PasswordResetPolicy);
            UpdateScopedSetting("_AADApplicationId", newConfig.AADApplicationId);
            UpdateScopedSetting("_AADApplicationKey", newConfig.AADApplicationKey);
            UpdateScopedSetting("_JwtAudiences", newConfig.JwtAudiences);
            UpdateScopedSetting("_RoleSyncEnabled", newConfig.RoleSyncEnabled.ToString());
            UpdateScopedSetting("_UserSyncEnabled", newConfig.UserSyncEnabled.ToString());
            UpdateScopedSetting("_ProfileSyncEnabled", newConfig.ProfileSyncEnabled.ToString());
            UpdateScopedSetting("_JwtAuthEnabled", newConfig.JwtAuthEnabled.ToString());
            UpdateScopedSetting("_APIResource", newConfig.APIResource);
            UpdateScopedSetting("_Scopes", newConfig.Scopes);
            UpdateScopedSetting("_UsernamePrefixEnabled", newConfig.UsernamePrefixEnabled.ToString());
            UpdateScopedSetting("_GroupNamePrefixEnabled", newConfig.GroupNamePrefixEnabled.ToString());
            UpdateScopedSetting("_RopcPolicy", newConfig.RopcPolicy);
            UpdateScopedSetting("_ImpersonatePolicy", newConfig.ImpersonatePolicy);
            UpdateScopedSetting("_AutoAuthorize", newConfig.AutoAuthorize.ToString());
            UpdateScopedSetting("_AutoMatchExistingUsers", newConfig.AutoMatchExistingUsers.ToString());

            newConfig.B2cApplicationId = UpdateB2CApplicationId(newConfig);

            UpdateConfig((OAuthConfigBase)newConfig);

            //TODO: Understand this DataCache usage
            //Clear config after update
            DataCache.RemoveCache(GetCacheKey(_service, _portalId));
        }

        private string UpdateB2CApplicationId(AzureConfig newConfig)
        {
            var b2cApplicationId = "";
            if (!string.IsNullOrEmpty(newConfig.AADApplicationId)
                && !string.IsNullOrEmpty(newConfig.AADApplicationKey)
                && !string.IsNullOrEmpty(newConfig.TenantId))
            {
                var graphClient = new Graph.GraphClient(newConfig.AADApplicationId, newConfig.AADApplicationKey, newConfig.TenantId);
                var extensionApp = graphClient.GetB2CExtensionApplication();
                b2cApplicationId = extensionApp?.AppId;
                if (string.IsNullOrEmpty(b2cApplicationId))
                {
                    throw new ConfigurationErrorsException("Can't find B2C Application on current tenant. Ensure the application 'b2c-extensions-app' exists.");
                } 
            }
            UpdateScopedSetting("_B2CApplicationId", b2cApplicationId);
            return b2cApplicationId;
        }
    }
}