using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using NotificationService.Infraestructure.Persistence;

namespace NotificationService.UnitTests
{
    public class UnitOfWorkTests
    {
        private readonly Mock<AppDbContext> _dbContextMock;

        public UnitOfWorkTests()
        {
            _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        }

        [Fact]
        public async Task RollbackAsync_ShouldHandleTransactionDisposal()
        {
            // Arrange
            var dbContextMock = new Mock<AppDbContext>();
            var transactionMock = new Mock<IDbContextTransaction>();

            dbContextMock.Setup(d => d.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            var unitOfWork = new UnitOfWork(dbContextMock.Object);

            // Act
            await unitOfWork.BeginTransactionAsync();
            await unitOfWork.RollbackAsync();

            // Assert
            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldPropagateExceptions()
        {
            // Arrange
            var dbContextMock = new Mock<AppDbContext>();
            dbContextMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Database error"));

            var unitOfWork = new UnitOfWork(dbContextMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
        }

    }

}
