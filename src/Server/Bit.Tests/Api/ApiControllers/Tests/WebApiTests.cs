﻿using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class WebApiTests
    {
        [TestMethod]
        [TestCategory("WebApi")]
        public virtual async Task WebApiShouldReturnOk()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                HttpClient client = testEnvironment.Server.GetHttpClient(token: token);

                HttpResponseMessage response = await client.GetAsync("api/customers/1/orders");

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
