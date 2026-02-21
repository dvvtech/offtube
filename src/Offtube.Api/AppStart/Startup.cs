using Offtube.Api.AppStart.Extensions;
using Offtube.Api.Configuration;
using Offtube.Api.Services;

namespace Offtube.Api.AppStart
{
    public class Startup
    {
        private WebApplicationBuilder _builder;
        private readonly ILogger<Startup> _logger;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            var loggerFactory = _builder
                .Services
                .BuildServiceProvider()
                .GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<Startup>();
        }

        public void Initialize()
        {
            if (_builder.Environment.IsDevelopment())
            {
                _builder.Services.AddSwaggerGen();
            }
            else
            {
                _builder.Services.ConfigureCors();
            }

            // Регистрация HttpClientFactory
            _builder.Services.AddHttpClient();

            InitConfigs();

            // Добавляем SignalR
            _builder.Services.AddSignalR();

            // Регистрируем сервис
            _builder.Services.AddScoped<IYoutubeDownloadService, YoutubeDownloadService>();
            _builder.Services.AddHttpContextAccessor();
        }

        private void InitConfigs()
        {
            _builder.Services.Configure<AppConfig>(_builder.Configuration.GetSection(AppConfig.SectionName));
            _builder.Services.Configure<GoogleRecaptchaConfig>(_builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName));

            var configSection = _builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName);
            var cap = configSection.Get<GoogleRecaptchaConfig>();

            if(cap.SecretKey.Length > 0)
                _logger.LogInformation($"cap.len > 0, len:{cap.SecretKey.Length}");
        }
    }
}
