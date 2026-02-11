using System.Text;
using Amazon;
using Amazon.S3;
using Api.Data;
using Api.Models;
using Api.Services;
using Api.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        var conn = configuration.GetConnectionString("DefaultConnection") ?? configuration["ConnectionStrings:DefaultConnection"];

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = builder.Environment.ApplicationName,
                Description = "API Documentation",
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/license/mit/")
                },
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = HeaderNames.Authorization,
                Description = "Insert the Bearer Token",
                Type = SecuritySchemeType.Http,
                //Reference = new OpenApiReference
                //{
                //    Type = ReferenceType.SecurityScheme,
                //    Id = "Bearer"
                //},
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = JwtBearerDefaults.AuthenticationScheme,
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(conn, sqlOptions =>
            {
                sqlOptions.UseCompatibilityLevel(170); // SQL Server 2025 (17.x)
                //sqlOptions.UseCompatibilityLevel(160); // SQL Server 2022 (16.x)
                //sqlOptions.UseCompatibilityLevel(150); // SQL Server 2019 (15.x)
                //...It's possible to set other SQL Server compatibility levels if needed

                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            options.LogTo(Console.WriteLine, [RelationalEventId.CommandExecuted]);
            options.EnableSensitiveDataLogging();
        });

        // Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Auth
        var jwtSection = configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true
            };
        });

        // S3 / MinIO client (AWS SDK)
        var s3Config = configuration.GetSection("S3");
        var s3ServiceUrl = s3Config["ServiceURL"];

        var s3Region = RegionEndpoint.USEast1; // region irrelevant for MinIO
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(s3Config["AccessKey"], s3Config["SecretKey"]);

        var s3ClientConfig = new AmazonS3Config
        {
            ServiceURL = s3ServiceUrl,
            ForcePathStyle = true
        };

        builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(awsCredentials, s3ClientConfig));

        // Storage providers
        builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
        builder.Services.AddScoped<IFileStorage, S3FileStorage>();

        // Token service
        builder.Services.AddScoped<ITokenService, TokenService>();

        var app = builder.Build();
        await SeedData.EnsureSeedDataAsync(app.Services, configuration);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} V1"));
        }

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}