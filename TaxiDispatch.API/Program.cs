using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SendGrid.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using TaxiDispatch.API.Configuration;
using TaxiDispatch.Configuration;
using TaxiDispatch.Data;
using TaxiDispatch.Domain.IdealPostcodes;
using TaxiDispatch.Features.AccountUsers.CreateUser;
using TaxiDispatch.Features.AdminUsers.CreateUser;
using TaxiDispatch.Features.DispatchUsers.CreateUser;
using TaxiDispatch.Features.DriverUsers.CreateUser;
using TaxiDispatch.Features.Membership;
using TaxiDispatch.Interfaces;
using TaxiDispatch.Modules.Membership;
using TaxiDispatch.Modules.Membership.Security;
using TaxiDispatch.Modules.Messaging;
using TaxiDispatch.Modules.Messaging.Services;
using TaxiDispatch.Services;
using TaxiDispatch.Services.Cache;

namespace TaxiDispatch.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cultureInfo = new CultureInfo("en-GB");
            cultureInfo.NumberFormat.CurrencySymbol = "£";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;

            var getAddressApiKey = config["GetAddress:ApiKey"]
                ?? throw new InvalidOperationException("GetAddress:ApiKey missing");
            var getAddressAdminKey = config["GetAddress:AdminKey"]
                ?? throw new InvalidOperationException("GetAddress:AdminKey missing");

            builder.Services.AddSingleton(_ => new GetAddress.ApiKeys(getAddressApiKey, getAddressAdminKey));

            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var logdirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            var logFile = Path.Combine($"taxidispatch-api-{DateTime.Today:dd-MM-yyyy}.log");
            builder.Logging.AddProvider(new SimpleFileLoggerProvider(Path.Combine(logdirectory, logFile)));

            builder.Services.Configure<DropBoxConfig>(config.GetSection("Dropbox"));
            builder.Services.Configure<CallEventsConfig>(config.GetSection("CallEvents"));
            builder.Services.Configure<SmsQueueConfig>(config.GetSection("SmsQueue"));

            builder.Services.AddHttpLogging(o =>
            {
                o.LoggingFields = HttpLoggingFields.None;
            });

            builder.Services.AddCors(o => o.AddPolicy("MyPolicy", corsBuilder =>
            {
                corsBuilder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

            builder.Services.Configure<TenancyOptions>(config.GetSection("Tenancy"));
            builder.Services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();

            Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext = (sp, options) =>
            {
                var connectionResolver = sp.GetRequiredService<ITenantConnectionResolver>();
                var connectionString = connectionResolver.ResolveConnectionString();

                options.UseSqlServer(connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(2),
                            errorNumbersToAdd: null);
                    });
            };

            builder.Services.AddDbContext<TaxiDispatchContext>(configureDbContext);
            builder.Services.AddDbContextFactory<TaxiDispatchContext>(configureDbContext, ServiceLifetime.Scoped);

            builder.Services.AddDefaultIdentity<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 4;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<TaxiDispatchContext>()
                .AddDefaultTokenProviders();

            builder.Services.Configure<JwtConfig>(config.GetSection("JwtConfig"));

            var jwtKey = config["JwtConfig:Key"] ?? throw new InvalidOperationException("JwtConfig:Key missing");
            var jwtIssuer = config["JwtConfig:Issuer"] ?? throw new InvalidOperationException("JwtConfig:Issuer missing");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtIssuer
            };

            builder.Services.AddSingleton(tokenValidationParameters);
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<TenantUserService>();
            builder.Services.AddScoped<CreateAdminUserService>();
            builder.Services.AddScoped<CreateDispatchUserService>();
            builder.Services.AddScoped<CreateDriverUserService>();
            builder.Services.AddScoped<CreateAccountUserService>();

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("pushNotificationConfig.json")
                });
            }

            builder.Services.Configure<MessagingConfig>(config.GetSection("MessagingConfig"));
            builder.Services.AddSendGrid(options =>
                options.ApiKey = config.GetValue<string>("MessagingConfig:Email:SendGridApiKey")
                    ?? throw new Exception("The 'SendGridApiKey' is not configured"));
            builder.Services.AddTransient<IEmailSender, MessageService>();
            builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaxiDispatch API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
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
                        new List<string>()
                    }
                });
            });

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
                var instanceName = builder.Configuration["Redis:InstanceName"];

                options.InstanceName = instanceName;
                options.ConfigurationOptions = new ConfigurationOptions
                {
                    EndPoints = { redisConnectionString },
                    AbortOnConnectFail = false,
                    ConnectRetry = 5,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000,
                    KeepAlive = 30,
                    ReconnectRetryPolicy = new ExponentialRetry(5000),
                    SocketManager = new SocketManager(
                        name: "TaxiDispatch.Redis",
                        workerCount: 2,
                        useHighPrioritySocketThreads: true)
                };

                options.ConfigurationOptions.ClientName = "TaxiDispatch.API";
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisConfig = ConfigurationOptions.Parse(
                    builder.Configuration["Redis:ConnectionString"],
                    true);

                redisConfig.AbortOnConnectFail = false;
                redisConfig.ConnectRetry = 3;
                redisConfig.ReconnectRetryPolicy = new ExponentialRetry(5000);

                return ConnectionMultiplexer.Connect(redisConfig);
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            builder.Services.AddAuthorization();

            builder.Services.AddMemoryCache();
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<TariffService>();
            builder.Services.AddScoped<LocalPOIService>();
            builder.Services.AddScoped<UserProfileService>();
            builder.Services.AddScoped<AccountsService>();
            builder.Services.AddScoped<AceMessagingService>();
            builder.Services.AddScoped<AvailabilityService>();
            builder.Services.AddScoped<RevoluttService>();
            builder.Services.AddScoped<DispatchService>();
            builder.Services.AddScoped<AdminUIService>();
            builder.Services.AddScoped<ReportingService>();
            builder.Services.AddScoped<DocumentService>();
            builder.Services.AddScoped<UINotificationService>();
            builder.Services.AddScoped<UserActionsService>();
            builder.Services.AddScoped<GeoZoneService>();
            builder.Services.AddScoped<WebBookingService>();
            builder.Services.AddScoped<DriverAppService>();
            builder.Services.AddScoped<CallEventsService>();
            builder.Services.AddScoped<SmsQueueService>();
            builder.Services.AddScoped<WhatsAppService>();
            builder.Services.AddScoped<UrlTrackingService>();

            builder.Services.AddScoped<UserLocationCacheService>();
            builder.Services.AddScoped<UserProfileCacheService>();
            builder.Services.AddHostedService<StartupCacheService>();

            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<AddressLookupService>();
            builder.Services.AddScoped<IAddressLookupService, AddressLookupService>();

            builder.Services.AddHttpClient<IdealPostcodesClient>((_, _) => { })
                .ConfigureHttpClient(_ => { });

            builder.Services.AddSingleton(sp =>
                new IdealPostcodesClient(
                    builder.Configuration["IdealPostcodes:ApiKey"]
                        ?? throw new InvalidOperationException("IdealPostcodes:ApiKey missing"),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IdealPostcodesClient))));

            var app = builder.Build();

            if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                using var scope = app.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TaxiDispatchContext>>();
                using var dbContext = dbFactory.CreateDbContext();

                logger.LogInformation("Applying pending EF migrations on startup.");
                dbContext.Database.Migrate();
            }

            using (var membershipScope = app.Services.CreateScope())
            {
                var membershipService = membershipScope.ServiceProvider.GetRequiredService<TenantUserService>();
                membershipService.EnsureCompatibilityRolesAsync().GetAwaiter().GetResult();
            }

            app.UseCors("MyPolicy");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<ApiJwtContextMiddleware>();
            app.UseAuthorization();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 API");
            });

            app.MapControllers();

            try
            {
                app.Run();
            }
            catch
            {
                throw;
            }
        }
    }
}
