using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetNuke.Abstractions.Application;
using DotNetNuke.Authentication.Azure.B2C.Components;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Authentication.OAuth;
using NUnit.Framework;
using Moq;
using AwesomeAssertions;
namespace DotNetNuke.Authentication.Entra.ExternalId.Tests
{
    public class AzureConfigTests
    {
        [Test]
        public void AzureConfigInheritsFromOAuthConfigBase()
        {
            const string service = "TestEntra";
            const string apiKey = "Test_Key";
            const string secret = "Test_Secret";
            var mockProvider = new Mock<IServiceProvider>();
            var portalController = new Mock<IPortalController>();
            var hostSettings = new Mock<IHostSettingsService>();
            var globalType = typeof(Globals);
            var portalSettings = new Dictionary<string, string>
            {
                { $"{service}_APIKey", apiKey },
                { $"{service}_APISecret", secret },
                { $"{service}_Enabled", "True" }
            };
            var serviceProviderField =
                globalType.GetField("dependencyProvider", BindingFlags.NonPublic | BindingFlags.Static);

            serviceProviderField?.SetValue(null, mockProvider.Object);

            portalController.Setup(p => p.GetPortalSettings(It.IsAny<int>())).Returns(portalSettings);
            hostSettings.Setup(h => h.GetString(It.Is<string>(k => k == $"{service}_UseGlobalSettings"),It.IsAny<string>())).Returns<string,string>((k,d) => "False");
            mockProvider.Setup(s => s.GetService(It.Is<Type>(t => t.Name == nameof(IPortalController)))).Returns<Type>(t => portalController.Object);
            mockProvider.Setup(s => s.GetService(It.Is<Type>(t => t.Name == nameof(IHostSettingsService)))).Returns<Type>(t => hostSettings.Object);

            var target = new AzureConfig(service, -1);

            target.Should().NotBeNull();
            target.Should().BeAssignableTo<OAuthConfigBase>();

            target.APISecret.Should().NotBeNull();
            target.APISecret.Should().Be(secret);

            target.APIKey.Should().NotBeNull();
            target.APIKey.Should().Be(apiKey, "because this should be set in the base ctor");
        }
    }
}