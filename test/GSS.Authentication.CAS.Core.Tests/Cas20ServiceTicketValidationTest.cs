﻿using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Validation;
using Microsoft.Extensions.FileProviders;
using RichardSzalay.MockHttp;
using Xunit;

namespace GSS.Authentication.CAS.Testing.Validation
{
    public class Cas20ServiceTicketValidationTest : IClassFixture<CasFixture>
    {
        private readonly CasFixture _fixture;

        public Cas20ServiceTicketValidationTest(CasFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ValidateServiceTicket_SuccessAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_fixture.FileProvider.ReadAsHttpContent("Cas20ValidationSuccess.xml", mediaType: "application/xml"));
            var validator = new Cas20ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act
            var principal = await validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None);

            //Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Assertion);
            Assert.Equal(principal.GetPrincipalName(), principal.Assertion.PrincipalName);
            Assert.Empty(principal.Assertion.Attributes);
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
        
        [Fact]
        public async Task ValidateServiceTicket_FailAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(_fixture.FileProvider.ReadAsHttpContent("Cas20ValidationFail.xml", mediaType: "application/xml"));
            var validator = new Cas20ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None));
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
        
        [Fact]
        public async Task ValidateServiceTicket_ErrorStatusCodeAsync()
        {
            // Arrange
            var ticket = Guid.NewGuid().ToString();
            var requestUrl = $"{_fixture.Options.CasServerUrlBase}/serviceValidate?ticket={ticket}&service={Uri.EscapeDataString(_fixture.Service)}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, requestUrl)
              .Respond(HttpStatusCode.BadRequest);
            var validator = new Cas20ServiceTicketValidator(_fixture.Options, new HttpClient(mockHttp));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => validator.ValidateAsync(ticket, _fixture.Service, CancellationToken.None));
            mockHttp.VerifyNoOutstandingRequest();
            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
