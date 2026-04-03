using Offtube.Api.AppStart;
using Offtube.Api.AppStart.Extensions;
using Offtube.Api.Hub;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.ApplyCors();
}

app.MapControllers();

app.MapHub<DownloadHub>("/downloadHub");

app.Run();
