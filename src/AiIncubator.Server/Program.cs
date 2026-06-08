using System.Text.Json.Serialization;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Common.Helpers.Configurations;
using AiIncubator.Server.Common.Middlewares;

var builder = WebApplication.CreateBuilder(args);
if (Environment.GetEnvironmentVariable("AIINCUBATOR_SKIP_DOTENV") != "1")
{
    builder.Configuration.AddDotEnvFile(Path.Combine(builder.Environment.ContentRootPath, ".env"));
}

const string ClientCorsPolicy = "client";
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services
    .AddControllers(options => options.Conventions.Add(new ControllerNameAttributeConvention()))
    .AddJsonOptions(jsonOptions =>
    {
        jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

builder.Services.AddOpenApi();
builder.Services.AddAiIncubatorAuth(builder.Configuration);
builder.Services.AddRagServices(builder.Configuration);
builder.Services.AddTelegramServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();

app.Run();

public partial class Program;
