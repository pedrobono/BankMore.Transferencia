using BankMore.TransferService.Application.Commands;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Infrastructure.Data;
using BankMore.TransferService.Infrastructure.ExternalServices;
using BankMore.TransferService.Infrastructure.Repositories;
using BankMore.TransferService.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog
    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // Configurações
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string não configurada");

    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret não configurado");

    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"]
        ?? throw new InvalidOperationException("Account Service URL não configurada");

    var accountServiceTimeout = int.Parse(builder.Configuration["AccountService:TimeoutSeconds"] ?? "30");

    // Inicializar banco de dados
    var dbInitializer = new DatabaseInitializer(connectionString);
    dbInitializer.Initialize();
    Log.Information("Banco de dados inicializado com sucesso");

    // Adicionar serviços
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<BankMore.TransferService.Infrastructure.HealthChecks.DatabaseHealthCheck>("database")
        .AddCheck<BankMore.TransferService.Infrastructure.HealthChecks.AccountServiceHealthCheck>("account-service");

    // Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "BankMore Transfer Service API",
            Version = "v1",
            Description = "API para transferências entre contas da mesma instituição"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    builder.Services.AddAuthorization();

    // MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // Repositories
    builder.Services.AddScoped<ITransferenciaRepository>(sp => new TransferenciaRepository(connectionString));

    // HttpClient para Account Service
    builder.Services.AddHttpClient<IContaCorrenteServiceClient, ContaCorrenteServiceClient>(client =>
    {
        client.BaseAddress = new Uri(accountServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(accountServiceTimeout);
    });

    var app = builder.Build();

    // Middleware de exceções (deve ser o primeiro)
    app.UseCustomExceptionHandler();

    // Serilog request logging
    app.UseSerilogRequestLogging();

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transfer Service API v1");
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    Console.WriteLine("\n==================================================");
    Console.WriteLine("🚀 Transfer Service PRONTO!");
    Console.WriteLine("📖 Swagger: http://localhost:8082/swagger");
    Console.WriteLine("❤️  Health: http://localhost:8082/health");
    Console.WriteLine("🏦 Account Service: " + accountServiceUrl);
    Console.WriteLine("==================================================\n");

    Log.Information("==================================================");
    Log.Information("Transfer Service iniciado com sucesso!");
    Log.Information("Swagger: http://localhost:8082/swagger");
    Log.Information("Health Check: http://localhost:8082/health");
    Log.Information("Account Service: {AccountServiceUrl}", accountServiceUrl);
    Log.Information("==================================================");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
