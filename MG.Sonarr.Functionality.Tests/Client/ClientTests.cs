using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using MG.Api.Json;
using MG.Sonarr.Functionality.Client;
using MG.Sonarr.Functionality.Collections;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MG.Sonarr.Functionality.Strings;

namespace MG.Sonarr.Functionality.Tests.Client
{
    [TestClass]
    public class ClientTests
    {
        private const string LOCAL_URL = "http://localhost:8989";
        private const string PUBLIC_URL = "https://cloud-sonarr.api.com";
        private const string API_KEY = "24er3es29yaenawz65znpywfrb3rbz0z";

        [TestMethod]
        public void TestUrlFactory()
        {
            ISonarrUrl url = SonarrFactory.GenerateSonarrUrl(new Uri(LOCAL_URL), true);
            Assert.IsNotNull(url);
            Assert.IsTrue(url.IncludeApiPrefix);
            Assert.AreEqual("http://localhost:8989", url.BaseUrl);

            ISonarrUrl pubUrl = SonarrFactory.GenerateSonarrUrl(new Uri(PUBLIC_URL), false);
            Assert.IsNotNull(pubUrl);
            Assert.IsFalse(pubUrl.IncludeApiPrefix);
            Assert.AreEqual(PUBLIC_URL, pubUrl.BaseUrl);
            Assert.AreNotEqual(PUBLIC_URL, pubUrl.Url);
        }

        [TestMethod]
        public void TestClientFactory()
        {
            var mockUrl = new Mock<ISonarrUrl>();
            var url = new Uri(LOCAL_URL, UriKind.Absolute);
            mockUrl.SetupGet(x => x.Url).Returns(url);

            var mockKey = new Mock<IApiKey>();
            mockKey.Setup(x => x.ToTuple()).Returns(("X-Api-Key", API_KEY));

            ISonarrClient client = SonarrFactory.GenerateClient(new HttpClientHandler(), mockUrl.Object, mockKey.Object,
                false, null, null, false);

            Assert.IsNotNull(client);
            Assert.IsTrue(client.IsAuthenticated);
            Assert.AreEqual(url, client.BaseAddress);
        }

        //[TestMethod]
    }
}
