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
else
{
    app.ApplyCors();
}

app.UseHttpsRedirection();

//if (!app.Environment.IsDevelopment())
//{
//    app.ApplyCors();
//}

app.UseAuthorization();

app.MapControllers();

//app.MapHub<DownloadHub>("/offtube/downloadHub").RequireCors("AllowSpecificOrigin"); // SignalR endpoint1
app.MapHub<DownloadHub>("/offtube/downloadHub"); // SignalR endpoint1
//app.MapHub<DownloadHub>("/downloadHub"); // SignalR endpoint1

app.Run();
