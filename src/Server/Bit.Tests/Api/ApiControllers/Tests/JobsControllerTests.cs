﻿using System.Threading.Tasks;
using Bit.OData.ODataControllers;
using Bit.Model.Dtos;
using Bit.Owin.Metadata;
using IdentityModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Bit.Tests.Api.ApiControllers.Tests
{
    [TestClass]
    public class JobsControllerTests
    {
        [TestMethod]
        [TestCategory("WebApi"), TestCategory("BackgroundJobs")]
        public virtual async Task JobsControllerMustThrowAnExceptionWhenJobIsNotPresents()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                TokenResponse token = await testEnvironment.Server.Login("ValidUserName", "ValidPassword", clientId: "TestResOwner");

                ODataClient client = testEnvironment.Server.BuildODataClient(token: token, edmName: "Bit");

                try
                {
                    await client.Controller<JobsInfoController, JobInfoDto>()
                        .Key("1")
                        .FindEntryAsync();

                    Assert.Fail();
                }
                catch (WebRequestException ex)
                {
                    Assert.IsTrue(ex.Response.Contains(BitMetadataBuilder.JobNotFound));
                }
            }
        }
    }
}
