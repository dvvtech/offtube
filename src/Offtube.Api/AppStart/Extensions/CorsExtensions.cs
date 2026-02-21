namespace Offtube.Api.AppStart.Extensions
{
    public static class CorsExtensions
    {
        private const string AllowAllPolicy = "AllowAll";
        private const string AllowSpecificOriginPolicy = "AllowSpecificOrigin";

        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AllowSpecificOriginPolicy,
                    policy =>
                    {
                        policy.WithOrigins("offtube.tech")//"https://dvvtech.github.io"
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials(); // Разрешить куки + signal r
                              //.SetIsOriginAllowed(origin => true);
                              //.SetIsOriginAllowedToAllowWildcardSubdomains();
                    });

                options.AddPolicy(AllowAllPolicy, policy =>
                {
                    policy.AllowAnyOrigin()  // Разрешить любой источник
                          //.AllowCredentials() // Разрешить куки + signal r
                          .AllowAnyMethod()  // Разрешить любые HTTP-методы (GET, POST, PUT и т. д.)
                          .AllowAnyHeader(); // Разрешить любые заголовки
                          //.SetIsOriginAllowed(origin => true);
                    //.SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            });
        }

        public static void ApplyCors(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseCors(AllowAllPolicy);
            }
            else
            {
                //app.UseCors(AllowAllPolicy);
                app.UseCors(AllowSpecificOriginPolicy);
            }
        }
    }
}
