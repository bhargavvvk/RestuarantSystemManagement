using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;

[TestFixture]
public class TaxConfigurationServiceTests
{
    private Mock<ITaxConfigurationRepository> _repoMock ;
    private Mock<IMapper>      _mapperMock;
    private Mock<IAuditService>    _auditMock;
    private RestaurantContext      _context;  
    private TaxConfigurationService    _taxConfigurationService; 

    [SetUp]
    public void SetUp()
    {
        _repoMock   = new Mock<ITaxConfigurationRepository>();
        _mapperMock = new Mock<IMapper>();
        _auditMock  = new Mock<IAuditService>();

        var options = new DbContextOptionsBuilder<RestaurantContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new RestaurantContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuditAction>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _taxConfigurationService = new TaxConfigurationService(
            _repoMock.Object,
            _mapperMock.Object,
            _context,
            _auditMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Test]
    public async Task GetTaxConfiguration_Sucess()
    {
        var configuration = new TaxConfiguration
        {
            Id                      = 1,
            CgstPercentage          = 2.5m,
            SgstPercentage          = 2.5m,
            ServiceChargePercentage = 5m
        };

        var response = new TaxConfigurationResponseDto
        {
            CgstPercentage          = 2.5m,
            SgstPercentage          = 2.5m,
            ServiceChargePercentage = 5m
        };

        _repoMock.Setup(x => x.GetActiveConfiguration())
                 .ReturnsAsync(configuration);

        _mapperMock.Setup(x => x.Map<TaxConfigurationResponseDto>(configuration))
                   .Returns(response);

        var result = await _taxConfigurationService.GetTaxConfiguration();

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void GetTaxConfiguration_NoActiveConfiguration()
    {
        _repoMock.Setup(x => x.GetActiveConfiguration())
                 .ReturnsAsync((TaxConfiguration?)null);

        Assert.ThrowsAsync<Exception>(() => _taxConfigurationService.GetTaxConfiguration());
    }


    [Test]
    public async Task UpdateTaxConfiguration_sucess()
    {
        var existing = new TaxConfiguration
        {
            Id                      = 1,
            CgstPercentage          = 2.5m,
            SgstPercentage          = 2.5m,
            ServiceChargePercentage = 5m,
            IsActive                = true,
            EffectiveFrom           = DateTime.UtcNow.AddDays(-1)
        };

        _repoMock.Setup(x => x.GetActiveConfiguration())
                 .ReturnsAsync(existing);

        _repoMock.Setup(x => x.Create(It.IsAny<TaxConfiguration>()))
                 .Returns<TaxConfiguration>(c => Task.FromResult(c));

        _repoMock.Setup(x => x.SaveChangesAsync())
                 .ReturnsAsync(0);

        var request = new UpdateTaxConfigurationDto { CgstPercentage = 5m };

        await _taxConfigurationService.UpdateTaxConfiguration(request);

        _repoMock.Verify(x => x.Create(It.IsAny<TaxConfiguration>()), Times.Once);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));

        _auditMock.Verify(x => x.LogAsync(
            nameof(TaxConfiguration),
            It.IsAny<string>(),
            AuditAction.Updated,
            It.IsAny<object>(),
            It.IsAny<object>(),
            "Tax configuration updated"),
            Times.Once);
    }

    [Test]
    public void UpdateTaxConfiguration_NegativeTaxProvided()
    {
        _repoMock.Setup(x => x.GetActiveConfiguration())
                 .ReturnsAsync(new TaxConfiguration
                 {
                     CgstPercentage          = 2.5m,
                     SgstPercentage          = 2.5m,
                     ServiceChargePercentage = 5m,
                     IsActive                = true
                 });

        var ex = Assert.ThrowsAsync<Exception>(
            () => _taxConfigurationService.UpdateTaxConfiguration(new UpdateTaxConfigurationDto { CgstPercentage = -1m }));

        Assert.That(ex!.Message, Is.EqualTo("Tax percentages cannot be negative"));
    }
}
