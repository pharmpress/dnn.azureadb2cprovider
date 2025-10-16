using AwesomeAssertions;
using DotNetNuke.Abstractions.Application;
using DotNetNuke.Abstractions.Portals;
using DotNetNuke.Authentication.Azure.B2C.Components;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication.OAuth;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using DotNetNuke.Common.Utilities;
using DotNetNuke.ComponentModel;
using DotNetNuke.Services.Cache;

namespace DotNetNuke.Authentication.Entra.ExternalId.Tests
{
    public class AzureConfigTests
    {
        const string service = "TestEntra";
        const string apiKey = "Test_Key";
        const string secret = "Test_Secret";
        private Mock<IServiceProvider> mockProvider = new Mock<IServiceProvider>();
        private Mock<IPortalController> portalController = new Mock<IPortalController>();
        private Mock<IHostSettingsService> hostSettings = new Mock<IHostSettingsService>();
        private Mock<IHostSettings> hostSettingsMock = new Mock<IHostSettings>();
        private Mock<CachingProvider> cacheProvider;
        private Dictionary<string, string> portalSettings = new Dictionary<string, string>
        {
            { $"{service}_APIKey", apiKey },
            { $"{service}_APISecret", secret },
            { $"{service}_Enabled", "True" },
            { $"{service}_TenantName", "TestTenant"},
            { $"{service}_RedirectUri", "/TestUri"}
        };

        private System.Type globalType = typeof(Globals);
        private System.Type configType = typeof(AzureConfig);
        private FieldInfo serviceProviderField = null;


        [SetUp]
        public void Init()
        {
            serviceProviderField =
                globalType.GetField("dependencyProvider", BindingFlags.NonPublic | BindingFlags.Static);

            serviceProviderField?.SetValue(null, mockProvider.Object);

            portalController.Setup(p => p.GetPortalSettings(It.IsAny<int>())).Returns(portalSettings);
            portalController.Setup(p => p.UpdatePortalSetting(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), true, null, false))
                .Callback<int, string, string, bool, string, bool>((_, key, value, __, ___, ____) =>
                {
                    portalSettings[key] = value;
                });

            hostSettings.Setup(h => h.GetString(It.Is<string>(k => k == $"{service}_UseGlobalSettings"), It.IsAny<string>())).Returns<string, string>((k, d) => "False");
            mockProvider.Setup(s => s.GetService(It.Is<Type>(t => t.Name == nameof(IPortalController)))).Returns<Type>(t => portalController.Object);
            mockProvider.Setup(s => s.GetService(It.Is<Type>(t => t.Name == nameof(IHostSettingsService)))).Returns<Type>(t => hostSettings.Object);

            cacheProvider = new Mock<CachingProvider>(hostSettingsMock.Object);

            var container = new SimpleContainer();
            container.RegisterComponentInstance<CachingProvider>(cacheProvider.Object);
            ComponentFactory.Container = container;
        }

        [Test]
        public void AzureConfigInheritsFromOAuthConfigBase()
        {
            var target = new AzureConfig(service, -1);

            target.Should().NotBeNull();
            target.Should().BeAssignableTo<OAuthConfigBase>();

            target.APISecret.Should().NotBeNull();
            target.APISecret.Should().Be(secret);

            target.APIKey.Should().NotBeNull();
            target.APIKey.Should().Be(apiKey, "because this should be set in the base ctor");
        }

        [Test]
        public void AzureConfigTestAllSettingsAreUpdated()
        {
            portalController = new Mock<IPortalController>();

            // Force call init again after reconstructing mock to ensure links with other mock values 
            Init();

            var target = new AzureConfig(service, -1);

            portalController.Setup(p => p.UpdatePortalSetting(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), true, null, false)).Verifiable();

            target.Update(target);

            portalController.Verify(p => p.UpdatePortalSetting(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), true, null, false), Times.Exactly(27));   
        }

        [Test]
        [TestCase("_TenantName", "NewTenantName")]
        [TestCase("_RedirectUri", "/newUriTest")]
        public void AzureConfigTestSpecificSettingGetsUpdatedOnlyOnce(string testScopedSetting, string testValue)
        {
            // Get clean portalController
            portalController = new Mock<IPortalController>();

            // Force call init again after reconstructing mock to ensure links with other mock values 
            Init();

            var target = new AzureConfig(service, -1);

            var newConfig = new AzureConfig(service, -1)
            {
                TenantName = "NewTenantName",
                RedirectUri = "/newUriTest"
            };

            portalController.Setup(p => p.UpdatePortalSetting(-1, service + testScopedSetting, testValue, true, null, false)).Verifiable();

            target.Update(newConfig);

            portalController.Verify(p => p.UpdatePortalSetting(It.IsAny<int>(), service + testScopedSetting, testValue, true, null, false), Times.Once);

        }
    }
}