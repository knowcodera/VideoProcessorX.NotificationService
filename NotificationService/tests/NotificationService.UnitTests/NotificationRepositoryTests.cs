using Microsoft.EntityFrameworkCore;
using Moq;
using NotificationService.Domain.Entities;
using NotificationService.Infraestructure.Persistence;

namespace NotificationService.UnitTests
{
    public class NotificationRepositoryTests
    {
        private readonly Mock<AppDbContext> _dbContextMock;
        private readonly NotificationRepository _repository;

        public NotificationRepositoryTests()
        {
            _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _repository = new NotificationRepository(_dbContextMock.Object);
        }

        [Fact]
        public async Task CreateAsync_DeveAdicionarNotificacao()
        {
            var notification = new Notification { Email = "teste@teste.com", Subject = "Teste", Body = "Corpo" };
            await _repository.CreateAsync(notification);
            _dbContextMock.Verify(db => db.Notifications.AddAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarNotificacao()
        {
            var notification = new Notification { Id = 1, Email = "teste@teste.com" };
            _dbContextMock.Setup(db => db.Notifications.FindAsync(1)).ReturnsAsync(notification);

            var result = await _repository.GetByIdAsync(1);
            Assert.Equal(notification.Email, result.Email);
        }
    }

}
