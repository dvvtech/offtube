using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Offtube.Api.Configuration;
using Offtube.Api.Services;

namespace Offtube.Unit.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task GetQualities_ShouldReturnVideoFormats()
        {
            // Arrange
            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            var options = Options.Create(new AppConfig
            {
                ProxyUrl = "123"
            });

            var service = new YoutubeDownloadService(options, mockEnv.Object);

            var videoUrl = "https://www.youtube.com/watch?v=uVOzD-GX0kM";

            // Act
            await service.GetQualities(videoUrl);

            // Assert
            // Здесь нужно добавить проверки, если метод возвращает данные
        }
    }
}
