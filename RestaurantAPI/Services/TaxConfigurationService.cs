using AutoMapper;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class TaxConfigurationService:ITaxConfigurationService
{
    private readonly ITaxConfigurationRepository _taxConfigurationRepository;
    private readonly IMapper _mapper;
    private readonly RestaurantContext _context;
    private readonly IAuditService _auditService;
    public TaxConfigurationService(ITaxConfigurationRepository taxConfigurationRepository,
    IMapper mapper,RestaurantContext context,IAuditService auditService)
    {
        _taxConfigurationRepository = taxConfigurationRepository;
        _mapper = mapper;
        _context = context;
        _auditService=auditService;
    }
    public async Task<TaxConfigurationResponseDto> GetTaxConfiguration()
    {
        var configuration = await _taxConfigurationRepository.GetActiveConfiguration();

        if (configuration == null)
        {
            throw new Exception("Tax configuration not found");
        }
        return _mapper.Map<TaxConfigurationResponseDto>(configuration);
    }
    public async Task UpdateTaxConfiguration(UpdateTaxConfigurationDto request)
    {
        var currentConfiguration =await _taxConfigurationRepository.GetActiveConfiguration();

        if (currentConfiguration == null)
        {
            throw new Exception("Active tax configuration not found");
        }

        if (request.CgstPercentage < 0 ||
            request.SgstPercentage < 0 ||
            request.ServiceChargePercentage < 0)
        {
            throw new Exception("Tax percentages cannot be negative");
        }
        if(request.CgstPercentage>100 
            ||request.SgstPercentage>100||
            request.ServiceChargePercentage>100)
        {
            throw new Exception("Tax percentage cannot exceed 100");
        }
        if (!request.CgstPercentage.HasValue &&
            !request.SgstPercentage.HasValue &&
            !request.ServiceChargePercentage.HasValue)
        {
            throw new Exception("At least one tax percentage must be provided");
        }
        if ((request.CgstPercentage == null ||
            request.CgstPercentage == currentConfiguration.CgstPercentage)
        &&
            (request.SgstPercentage == null ||
            request.SgstPercentage == currentConfiguration.SgstPercentage)
        &&
            (request.ServiceChargePercentage == null ||
            request.ServiceChargePercentage == currentConfiguration.ServiceChargePercentage))
        {
            throw new Exception("New values must be different from current values");
        }

        await using var transaction =await _context.Database.BeginTransactionAsync();

        try
        {
            currentConfiguration.IsActive =false;
            var newConfiguration =
                new TaxConfiguration
                {
                    CgstPercentage =request.CgstPercentage
                        ?? currentConfiguration.CgstPercentage,

                    SgstPercentage =request.SgstPercentage
                        ?? currentConfiguration.SgstPercentage,

                    ServiceChargePercentage =request.ServiceChargePercentage
                        ?? currentConfiguration.ServiceChargePercentage,

                    IsActive = true,
                    EffectiveFrom=DateTime.UtcNow
                };

            await _taxConfigurationRepository.Create(newConfiguration);

            await _taxConfigurationRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                nameof(TaxConfiguration),
                newConfiguration.Id.ToString(),
                AuditAction.Updated,
                new
                {
                    currentConfiguration.CgstPercentage,
                    currentConfiguration.SgstPercentage,
                    currentConfiguration.ServiceChargePercentage
                },
                new
                {
                    newConfiguration.CgstPercentage,
                    newConfiguration.SgstPercentage,
                    newConfiguration.ServiceChargePercentage
                },
                "Tax configuration updated");

            await _taxConfigurationRepository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
