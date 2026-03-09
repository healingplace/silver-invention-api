using DocumentUploaderAPI.Models;
using DocumentUploaderAPI.Services;

namespace DocumentUploaderAPI.Extensions;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddDocumentUploaderCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    // Specific origins configured
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback to AllowAnyOrigin for development
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        return services;
    }
}
