using Offtube.Api.AppStart.Extensions;
using Offtube.Api.Configuration;
using Offtube.Api.Services;
using Offtube.Api.Services.Abstract;

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

            // Регистрируем сервисы
            _builder.Services.AddScoped<IAnalyticsTrackingService, AnalyticsTrackingService>();
            _builder.Services.AddScoped<IYoutubeDownloadService, YoutubeDownloadService>();
            _builder.Services.AddHttpContextAccessor();

            _builder.Services.AddControllers();
        }

        private void InitConfigs()
        {
            if (!_builder.Environment.IsDevelopment())
            {
                _builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
            }

            _builder.Services.Configure<ProxyConfig>(_builder.Configuration.GetSection(ProxyConfig.SectionName));
            //_builder.Services.Configure<AppConfig>(_builder.Configuration.GetSection(AppConfig.SectionName));
            _builder.Services.Configure<GoogleRecaptchaConfig>(_builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName));

            var configSection = _builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName);
            //var cap = configSection.Get<GoogleRecaptchaConfig>();

            //if (cap.SecretKeyForOfftube.Length > 0)
            //{
            //    _logger.LogInformation("captcha: " + cap.SecretKeyForOfftube);
            //}
                //_logger.LogInformation($"cap.len > 0, len:{cap.SecretKeyForOfftube.Length}");
        }
    }
}
