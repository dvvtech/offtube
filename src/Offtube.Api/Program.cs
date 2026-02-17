using Offtube.Api.AppStart;
using Offtube.Api.AppStart.Extensions;
using Offtube.Api.Hub;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.Initialize();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.ApplyCors();
}

app.UseAuthorization();

app.MapControllers();

app.MapHub<DownloadHub>("/downloadHub");

app.Run();
