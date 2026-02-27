using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Desafio.Umbler.Application.Contracts;
using Desafio.Umbler.Application.DTOs;
using Desafio.Umbler.Application.Exceptions;
using Desafio.Umbler.Application.Services;
using Desafio.Umbler.Controllers;
using Desafio.Umbler.Domain.Entities;
using Desafio.Umbler.Infrastructure.Persistence;
using Desafio.Umbler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Desafio.Umbler.Test
{
    [TestClass]
    public class ControllersTest
    {
        [TestMethod]
        public void Home_Index_returns_View()
        {
            var controller = new HomeController();

            var response = controller.Index();
            var result = response as ViewResult;

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Home_Error_returns_View_With_Model()
        {
            var controller = new HomeController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var response = controller.Error();
            var result = response as ViewResult;
            var model = result?.Model as ErrorViewModel;

            Assert.IsNotNull(result);
            Assert.IsNotNull(model);
        }

        [TestMethod]
        public async Task Domain_ReturnsCachedData_WhenTtlIsValid()
        {
            var now = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc);

            using var db = CreateInMemoryDbContext();
            db.Domains.Add(new DomainRecord
            {
                Name = "test.com",
                Ip = "192.168.0.1",
                UpdatedAt = now.AddSeconds(-10),
                HostedAt = "umbler.corp",
                Ttl = 60,
                WhoIs = "Name Server: ns1.test.com\nName Server: ns2.test.com"
            });
            db.SaveChanges();

            var dnsGateway = new Mock<IDnsLookupGateway>(MockBehavior.Strict);
            var whoisGateway = new Mock<IWhoisGateway>(MockBehavior.Strict);
            var service = CreateService(db, dnsGateway, whoisGateway, now);

            var response = await service.GetAsync("TEST.COM");

            Assert.AreEqual("cache", response.Source);
            Assert.AreEqual("test.com", response.Domain);
            Assert.AreEqual("192.168.0.1", response.Ip);
            Assert.IsTrue(response.NameServers.Contains("ns1.test.com"));

            dnsGateway.VerifyNoOtherCalls();
            whoisGateway.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Domain_RefreshesFromExternal_WhenTtlExpired()
        {
            var now = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc);

            using var db = CreateInMemoryDbContext();
            db.Domains.Add(new DomainRecord
            {
                Name = "test.com",
                Ip = "192.168.0.1",
                UpdatedAt = now.AddSeconds(-120),
                HostedAt = "old-host",
                Ttl = 30,
                WhoIs = "old whois"
            });
            db.SaveChanges();

            var dnsGateway = new Mock<IDnsLookupGateway>();
            dnsGateway
                .Setup(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DnsLookupResult
                {
                    Ip = "8.8.8.8",
                    Ttl = 300,
                    NameServers = new[] { "ns2.test.com" }
                });

            var whoisGateway = new Mock<IWhoisGateway>();
            whoisGateway
                .Setup(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WhoisLookupResult
                {
                    Raw = "Name Server: ns1.test.com",
                    OrganizationName = string.Empty
                });
            whoisGateway
                .Setup(gateway => gateway.QueryAsync("8.8.8.8", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WhoisLookupResult
                {
                    Raw = string.Empty,
                    OrganizationName = "Google"
                });

            var service = CreateService(db, dnsGateway, whoisGateway, now);

            var response = await service.GetAsync("test.com");
            var persistedRecord = db.Domains.Single(x => x.Name == "test.com");

            Assert.AreEqual("external", response.Source);
            Assert.AreEqual("8.8.8.8", response.Ip);
            Assert.AreEqual("Google", response.HostedAt);
            Assert.AreEqual(300, persistedRecord.Ttl);
            Assert.AreEqual("8.8.8.8", persistedRecord.Ip);

            dnsGateway.Verify(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()), Times.Once);
            whoisGateway.Verify(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()), Times.Once);
            whoisGateway.Verify(gateway => gateway.QueryAsync("8.8.8.8", It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Domain_InvalidDomain_ThrowsValidationError()
        {
            var now = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc);

            using var db = CreateInMemoryDbContext();
            var dnsGateway = new Mock<IDnsLookupGateway>(MockBehavior.Strict);
            var whoisGateway = new Mock<IWhoisGateway>(MockBehavior.Strict);
            var service = CreateService(db, dnsGateway, whoisGateway, now);

            await Assert.ThrowsExceptionAsync<DomainValidationException>(() => service.GetAsync("umbler"));
        }

        [TestMethod]
        public async Task Domain_Moking_WhoisClient()
        {
            var now = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc);

            using var db = CreateInMemoryDbContext();

            var dnsGateway = new Mock<IDnsLookupGateway>();
            dnsGateway
                .Setup(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DnsLookupResult
                {
                    Ip = "10.0.0.1",
                    Ttl = 120,
                    NameServers = new[] { "ns1.test.com" }
                });

            var whoisGateway = new Mock<IWhoisGateway>();
            whoisGateway
                .Setup(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WhoisLookupResult
                {
                    Raw = "Name Server: ns1.test.com",
                    OrganizationName = string.Empty
                });
            whoisGateway
                .Setup(gateway => gateway.QueryAsync("10.0.0.1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WhoisLookupResult
                {
                    Raw = string.Empty,
                    OrganizationName = "umbler.corp"
                });

            var service = CreateService(db, dnsGateway, whoisGateway, now);

            var response = await service.GetAsync("test.com");

            Assert.IsNotNull(response);
            Assert.AreEqual("10.0.0.1", response.Ip);
            whoisGateway.Verify(gateway => gateway.QueryAsync("test.com", It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Domain_Controller_Returns_Dto_Only()
        {
            var domainLookupService = new Mock<IDomainLookupService>();
            domainLookupService
                .Setup(service => service.GetAsync("umbler.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainLookupResponseDto
                {
                    Domain = "umbler.com",
                    Ip = "177.55.66.99",
                    HostedAt = "Umbler",
                    Whois = "whois output",
                    NameServers = new[] { "ns1.umbler.com" },
                    Source = "external"
                });

            var controller = new DomainController(domainLookupService.Object);

            var response = await controller.Get("umbler.com", CancellationToken.None);
            var result = response.Result as OkObjectResult;
            var dto = result?.Value as DomainLookupResponseDto;

            Assert.IsNotNull(result);
            Assert.IsNotNull(dto);
            Assert.IsNull(dto!.GetType().GetProperty("Id"));
            Assert.IsNull(dto.GetType().GetProperty("Ttl"));
            Assert.IsNull(dto.GetType().GetProperty("UpdatedAt"));
        }

        private static DomainLookupService CreateService(
            DatabaseContext dbContext,
            Mock<IDnsLookupGateway> dnsGateway,
            Mock<IWhoisGateway> whoisGateway,
            DateTime utcNow)
        {
            var repository = new DomainRepository(dbContext);
            return new DomainLookupService(
                repository,
                dnsGateway.Object,
                whoisGateway.Object,
                new FakeClock(utcNow));
        }

        private static DatabaseContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase($"db-{Guid.NewGuid():N}")
                .Options;

            return new DatabaseContext(options);
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; }
        }
    }
}
