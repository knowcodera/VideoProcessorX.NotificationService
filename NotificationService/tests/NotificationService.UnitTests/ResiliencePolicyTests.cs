namespace NotificationService.UnitTests
{
    using Moq;
    using Polly;

    public class ResiliencePolicyTests
    {
        [Fact]
        public async Task RetryPolicy_ShouldRetryThreeTimes()
        {
            // Arrange
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(10));

            var mockService = new Mock<IFailingService>();
            mockService.SetupSequence(s => s.Call())
                .Throws<Exception>()
                .Throws<Exception>()
                .Throws<Exception>()
                .Returns(true);

            // Act
            var result = await policy.ExecuteAsync(() => Task.FromResult(mockService.Object.Call()));

            // Assert
            mockService.Verify(s => s.Call(), Times.Exactly(3));
        }

        public interface IFailingService
        {
            bool Call();
        }
    }
}
