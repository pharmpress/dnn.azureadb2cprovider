using DotNetNuke.Authentication.Azure.B2C.Components;
using DotNetNuke.Services.Authentication.OAuth;
using NUnit.Framework;
using Moq;
using FluentAssertions;
namespace DotNetNuke.Authentication.Entra.ExternalId.Tests
{
    public class AzureConfigTests
    {

        [Test]
        public void AzureConfigInheritsFromOAuthConfigBase()
        {
            var target = new AzureConfig("TestEntra", -1);

            target.Should().NotBeNull();
            target.Should().BeAssignableTo<OAuthConfigBase>();
        }
    }
}