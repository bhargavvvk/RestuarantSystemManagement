using QRCoder;
using RestaurantAPI.Exceptions;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class QrCodeService : IQrCodeService
{
    private readonly IRestaurentTableRepository _tableRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(IRestaurentTableRepository tableRepository, IConfiguration configuration, ILogger<QrCodeService> logger)
    {
        _tableRepository = tableRepository;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<byte[]> GenerateTableQr(int tableId)
    {
        var table = await _tableRepository.Get(tableId);

        if (table == null)
        {
            _logger.LogWarning("Table {TableId} not found while generating QR.", tableId);
            throw new TableNotFoundException();
        }

        var baseUrl = _configuration["Frontend:BaseUrl"];
        var joinRoute = _configuration["Frontend:JoinRoute"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new Exception("Frontend BaseUrl is not configured.");

        if (string.IsNullOrWhiteSpace(joinRoute))
            joinRoute = "/join";

        var qrUrl =
            $"{baseUrl.TrimEnd('/')}" +
            $"{joinRoute}" +
            $"/{table.QrIdentifier}";

        var generator = new QRCodeGenerator();

        var qrData = generator.CreateQrCode(
            qrUrl,
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrData);

        return qrCode.GetGraphic(20);
    }
}
