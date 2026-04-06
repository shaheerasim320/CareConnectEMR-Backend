
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Infrastructure.Persistence;
using CareConnectEMR.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Cryptography;

namespace CareConnectEMR.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured. Set ConnectionStrings__DefaultConnection."
                );
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT key is not configured. Set Jwt__Key."
                );
            var enableHttpsRedirection = builder.Configuration.GetValue("HttpsRedirection:Enabled", true);
            var enableSwagger = builder.Environment.IsDevelopment()
                || builder.Configuration.GetValue("Swagger:Enabled", false);
            var requireSwaggerAuth = builder.Configuration.GetValue("Swagger:RequireAuth", false);
            var swaggerUsername = builder.Configuration["Swagger:Username"];
            var swaggerPassword = builder.Configuration["Swagger:Password"];

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpContextAccessor();
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPatientService, PatientService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
            builder.Services.AddScoped<IDashboardService,DashboardService>();


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions.EnableRetryOnFailure()
                )
            );

            builder.Services.AddIdentity<ApplicationUser,IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole("Doctor"));
                options.AddPolicy("RequireReceptionistRole", policy => policy.RequireRole("Receptionist"));
                options.AddPolicy("AdminOrReceptionist", policy => policy.RequireRole("Admin", "Receptionist"));
                options.AddPolicy("AdminOrDoctor", policy => policy.RequireRole("Admin","Doctor"));
                options.AddPolicy("AdminOrDoctorOrReceptionist", policy => policy.RequireRole("Admin","Doctor","Receptionist"));
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular",policy=>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CareConnect EMR API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>(); 
                await IdentitySeeder.SeedAsync(userManager, roleManager);
            }

                // Configure the HTTP request pipeline.
                if (enableSwagger)
                {
                    if (requireSwaggerAuth)
                    {
                        app.Use(async (context, next) =>
                        {
                            if (context.Request.Path.StartsWithSegments("/swagger"))
                            {
                                if (
                                    string.IsNullOrWhiteSpace(swaggerUsername)
                                    || string.IsNullOrWhiteSpace(swaggerPassword)
                                )
                                {
                                    throw new InvalidOperationException(
                                        "Swagger authentication is enabled but Swagger:Username or Swagger:Password is missing."
                                    );
                                }

                                var authorizationHeader = context.Request.Headers.Authorization.ToString();
                                if (
                                    !authorizationHeader.StartsWith(
                                        "Basic ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    ChallengeSwagger(context);
                                    return;
                                }

                                try
                                {
                                    var encodedCredentials = authorizationHeader["Basic ".Length..].Trim();
                                    var decodedCredentials = Encoding.UTF8.GetString(
                                        Convert.FromBase64String(encodedCredentials)
                                    );
                                    var separatorIndex = decodedCredentials.IndexOf(':');

                                    if (separatorIndex < 0)
                                    {
                                        ChallengeSwagger(context);
                                        return;
                                    }

                                    var providedUsername = decodedCredentials[..separatorIndex];
                                    var providedPassword = decodedCredentials[(separatorIndex + 1)..];

                                    if (
                                        !AreEqual(providedUsername, swaggerUsername)
                                        || !AreEqual(providedPassword, swaggerPassword)
                                    )
                                    {
                                        ChallengeSwagger(context);
                                        return;
                                    }
                                }
                                catch
                                {
                                    ChallengeSwagger(context);
                                    return;
                                }
                            }

                            await next();
                        });
                    }

                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

            app.UseForwardedHeaders();

            if (enableHttpsRedirection)
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void ChallengeSwagger(HttpContext context)
        {
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Swagger\"");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

        private static bool AreEqual(string provided, string expected)
        {
            var providedBytes = Encoding.UTF8.GetBytes(provided);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
        }
    }
}
