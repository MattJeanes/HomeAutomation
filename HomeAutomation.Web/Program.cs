using HomeAutomation.Web.Data;
using HomeAutomation.Web.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using MQTTnet;
using MQTTnet.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithClientId("homeautomation")
            .WithTcpServer(builder.Configuration.GetValue<string>("Mqtt:Server"))
            .WithCredentials(builder.Configuration.GetValue<string>("Mqtt:Username"), builder.Configuration.GetValue<string>("Mqtt:Password"))
            .Build();

builder.Services.AddControllers();
builder.Services.AddSingleton<MqttFactory>();
builder.Services.AddSingleton(mqttClientOptions);
builder.Services.Configure<PackageOptions>(builder.Configuration.GetSection("Package"));
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseHealthChecks(new PathString("/healthz"));
app.UseDefaultFiles();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".pem"] = "application/x-pem-file";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection(); // handled by docker
}

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseRouting();

app.UseApiKeyMiddleware(builder.Configuration["Key"]);

app.MapControllers();

await app.RunAsync();
