using Offtube.Api.Configuration;
using Offtube.Api.Services;

namespace Offtube.Api.AppStart
{
    public class Startup
    {
        private WebApplicationBuilder _builder;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void Initialize()
        {
            if (_builder.Environment.IsDevelopment())
            {
                _builder.Services.AddSwaggerGen();
            }

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
        }
    }
}
