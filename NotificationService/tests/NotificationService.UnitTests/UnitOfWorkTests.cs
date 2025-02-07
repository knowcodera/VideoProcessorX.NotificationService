using Microsoft.EntityFrameworkCore;
using Moq;
using NotificationService.Infraestructure.Persistence;

namespace NotificationService.UnitTests
{
    public class UnitOfWorkTests
    {
        private readonly Mock<AppDbContext> _dbContextMock;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            _dbContextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _unitOfWork = new UnitOfWork(_dbContextMock.Object);
        }

        [Fact]
        public async Task BeginTransactionAsync_DeveCriarTransacao()
        {
            await _unitOfWork.BeginTransactionAsync();
            Assert.NotNull(_unitOfWork);
        }

        [Fact]
        public async Task CommitAsync_DeveSalvarAlteracoes()
        {
            await _unitOfWork.CommitAsync();
            _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RollbackAsync_DeveDesfazerTransacao()
        {
            await _unitOfWork.RollbackAsync();
            _dbContextMock.Verify(db => db.Dispose(), Times.Once);
        }
    }

}
