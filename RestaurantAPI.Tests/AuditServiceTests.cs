using System.Text.Json;
using Moq;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.Services;

namespace RestaurantAPI.Tests;
public class AuditServiceTests
{
    private Mock<IAuditLogRepository> _auditRepoMock;
    private AuditService _auditService;

    [SetUp]
    public void SetUp()
    {
        _auditRepoMock = new Mock<IAuditLogRepository>();

        _auditRepoMock
            .Setup(r => r.Create(It.IsAny<AuditLog>()))
            .Returns<AuditLog>(log => Task.FromResult(log));

        _auditService = new AuditService(_auditRepoMock.Object);
    }


    [Test]
    public async Task LogAsync_WithOldAndNewValues_ShouldCreateAuditLog()
    {
        var newValues = new { Status = "Active" };

        AuditLog? captured = null;
        _auditRepoMock
            .Setup(r => r.Create(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => captured = log)
            .Returns<AuditLog>(log => Task.FromResult(log));

        await _auditService.LogAsync(
            entityName: nameof(User),
            entityId:   "42",
            action:     AuditAction.Updated,
            oldValues:  null,
            newValues:  newValues,
            remarks:    "Status toggled");

        _auditRepoMock.Verify(r => r.Create(It.IsAny<AuditLog>()), Times.Once);

        Assert.That(captured,              Is.Not.Null);
        Assert.That(captured!.EntityName,  Is.EqualTo(nameof(User)));
        Assert.That(captured.EntityId,     Is.EqualTo("42"));
        Assert.That(captured.Action,       Is.EqualTo(AuditAction.Updated));
        Assert.That(captured.Remarks,      Is.EqualTo("Status toggled"));
        Assert.That(captured.OldValues,    Is.Null);
        Assert.That(captured.NewValues,    Is.Not.Null);
    }
}
