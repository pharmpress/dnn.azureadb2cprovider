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
            
            // Gets the settings scope (global or per portal)
            UseGlobalSettings = bool.Parse(_hostSettingsService.GetString(Service + "_UseGlobalSettings", "false"));

            if (!UseGlobalSettings)
            {
                _portalSettings = _portalController.GetPortalSettings(portalId);
            }

            // Loads the scoped settings
            APIKey = GetScopedSetting(Service + ApiKeyNamePart, portalId, "");
            APISecret = GetScopedSetting(Service + ApiSecretNamePart, portalId, "");
            RedirectUri = GetScopedSetting(Service + "_RedirectUri", portalId, "");
            OnErrorUri = GetScopedSetting(Service + "_OnErrorUri", portalId, "");
            TenantName = GetScopedSetting(Service + "_TenantName", portalId, "");
            TenantId = GetScopedSetting(Service + "_TenantId", portalId, "");
            SignUpPolicy = GetScopedSetting(Service + "_SignUpPolicy", portalId, "");
            ProfilePolicy = GetScopedSetting(Service + "_ProfilePolicy", portalId, "");
            PasswordResetPolicy = GetScopedSetting(Service + "_PasswordResetPolicy", portalId, "");
            AutoRedirect = bool.Parse(GetScopedSetting(Service + "_AutoRedirect", portalId, "false"));
            Enabled = bool.Parse(GetScopedSetting(Service + "_Enabled", portalId, "false"));
            AADApplicationId = GetScopedSetting(Service + "_AADApplicationId", portalId, "");
            AADApplicationKey = GetScopedSetting(Service + "_AADApplicationKey", portalId, "");
            JwtAudiences = GetScopedSetting(Service + "_JwtAudiences", portalId, "");
            RoleSyncEnabled = bool.Parse(GetScopedSetting(Service + "_RoleSyncEnabled", portalId, "false"));
            UserSyncEnabled = bool.Parse(GetScopedSetting(Service + "_UserSyncEnabled", portalId, "false"));
            ProfileSyncEnabled = bool.Parse(GetScopedSetting(Service + "_ProfileSyncEnabled", portalId, "false"));
            JwtAuthEnabled = bool.Parse(GetScopedSetting(Service + "_JwtAuthEnabled", portalId, "false"));
            APIResource = GetScopedSetting(Service + "_APIResource", portalId, "");
            Scopes = GetScopedSetting(Service + "_Scopes", portalId, "");
            B2cApplicationId = GetScopedSetting(Service + "_B2CApplicationId", portalId, "");
            UsernamePrefixEnabled = bool.Parse(GetScopedSetting(Service + "_UsernamePrefixEnabled", portalId, "true"));
            GroupNamePrefixEnabled = bool.Parse(GetScopedSetting(Service + "_GroupNamePrefixEnabled", portalId, "true"));
            RopcPolicy = GetScopedSetting(Service + "_RopcPolicy", portalId, "");
            ImpersonatePolicy = GetScopedSetting(Service + "_ImpersonatePolicy", portalId, "");
            AutoAuthorize = bool.Parse(GetScopedSetting(Service + "_AutoAuthorize", portalId, "true"));
            AutoMatchExistingUsers = bool.Parse(GetScopedSetting(Service + "_AutoMatchExistingUsers", portalId, "false"));
            ExpiredRolesSyncEnabled = bool.Parse(ConfigurationManager.AppSettings["AzureB2C_ExpiredRolesSyncEnabled"] ?? "false");
        }

        private string GetScopedSetting(string key, int portalId, string defaultValue)
        {
            return UseGlobalSettings 
                ? _hostSettingsService.GetString(key, defaultValue) 
                : _portalSettings.TryGetValue(key, out var result) 
                    ? result 
                    : defaultValue;
        }

        private void UpdateScopedSetting( int portalId, string key, string value)
        {
            if (UseGlobalSettings)
                _hostSettingsService.Update(key, value, true);
            else
                _portalController.UpdatePortalSetting(portalId, key, value, true, null, false);
        }

        private static string GetCacheKey(string service, int portalId)
        {
            return _cacheKey + "." + service + "_" + portalId;
        }

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

        public void Update(AzureConfig config)
        {
            _hostSettingsService.Update(config.Service + "_UseGlobalSettings", config.UseGlobalSettings.ToString(),
                true);
            UpdateScopedSetting(config.PortalID, config.Service + ApiKeyNamePart, config.APIKey);
            UpdateScopedSetting(config.PortalID, config.Service + ApiSecretNamePart, config.APISecret);
            UpdateScopedSetting(config.PortalID, config.Service + "_RedirectUri", config.RedirectUri);
            UpdateScopedSetting(config.PortalID, config.Service + "_OnErrorUri", config.OnErrorUri);
            UpdateScopedSetting(config.PortalID, config.Service + "_TenantName", config.TenantName);
            UpdateScopedSetting(config.PortalID, config.Service + "_TenantId", config.TenantId);
            UpdateScopedSetting(config.PortalID, config.Service + "_AutoRedirect", config.AutoRedirect.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_Enabled", config.Enabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_SignUpPolicy", config.SignUpPolicy);
            UpdateScopedSetting(config.PortalID, config.Service + "_ProfilePolicy", config.ProfilePolicy);
            UpdateScopedSetting(config.PortalID, config.Service + "_PasswordResetPolicy", config.PasswordResetPolicy);
            UpdateScopedSetting(config.PortalID, config.Service + "_AADApplicationId", config.AADApplicationId);
            UpdateScopedSetting(config.PortalID, config.Service + "_AADApplicationKey", config.AADApplicationKey);
            UpdateScopedSetting(config.PortalID, config.Service + "_JwtAudiences", config.JwtAudiences);
            UpdateScopedSetting(config.PortalID, config.Service + "_RoleSyncEnabled", config.RoleSyncEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_UserSyncEnabled", config.UserSyncEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_ProfileSyncEnabled", config.ProfileSyncEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_JwtAuthEnabled", config.JwtAuthEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_APIResource", config.APIResource);
            UpdateScopedSetting(config.PortalID, config.Service + "_Scopes", config.Scopes);
            UpdateScopedSetting(config.PortalID, config.Service + "_UsernamePrefixEnabled", config.UsernamePrefixEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_GroupNamePrefixEnabled", config.GroupNamePrefixEnabled.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_RopcPolicy", config.RopcPolicy);
            UpdateScopedSetting(config.PortalID, config.Service + "_ImpersonatePolicy", config.ImpersonatePolicy);
            UpdateScopedSetting(config.PortalID, config.Service + "_AutoAuthorize", config.AutoAuthorize.ToString());
            UpdateScopedSetting(config.PortalID, config.Service + "_AutoMatchExistingUsers", config.AutoMatchExistingUsers.ToString());

            config.B2cApplicationId = UpdateB2CApplicationId(config);

            UpdateConfig((OAuthConfigBase)config);

            // Clear config after update
            DataCache.RemoveCache(GetCacheKey(config.Service, config.PortalID));
        }

        private string UpdateB2CApplicationId(AzureConfig config)
        {
            var b2cApplicationId = "";
            if (!string.IsNullOrEmpty(config.AADApplicationId)
                && !string.IsNullOrEmpty(config.AADApplicationKey)
                && !string.IsNullOrEmpty(config.TenantId))
            {
                var graphClient = new Graph.GraphClient(config.AADApplicationId, config.AADApplicationKey, config.TenantId);
                var extensionApp = graphClient.GetB2CExtensionApplication();
                b2cApplicationId = extensionApp?.AppId;
                if (string.IsNullOrEmpty(b2cApplicationId))
                {
                    throw new ConfigurationErrorsException("Can't find B2C Application on current tenant. Ensure the application 'b2c-extensions-app' exists.");
                }
            }
            UpdateScopedSetting(config.PortalID, config.Service + "_B2CApplicationId", b2cApplicationId);
            return b2cApplicationId;
        }
    }
}