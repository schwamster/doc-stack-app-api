using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using doc_stack_app_api.Store;
using Microsoft.Extensions.PlatformAbstractions;
using Swashbuckle.AspNetCore.Swagger;
using CorrelationId;
using SerilogEnricher;
using CustomSerilogFormatter;
using HealthCheck;
using PerformanceLog;

namespace doc_stack_app_api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            CustomSerilogConfigurator.Setup("doc-stack-app-api", false);
        }

        public static ILoggerFactory LoggerFactory { get; set; }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var dockStackAppUrl = Configuration["DocStackApp"];
            var identityServer = Configuration["AlternativeIdentityServer"];

            services.AddCors(options =>
            {
                // this defines a CORS policy called "default"
                options.AddPolicy("default", policy =>
                {
                    policy.WithOrigins(dockStackAppUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Add framework services.
            services.AddMvc();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IQueueService, RedisQueueService>();
            
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "doc-stack-app-api",
                    Description = "manage the document store",
                    TermsOfService = "None",
                    Contact = new Contact { Name = "Bastian Töpfer", Email = "bastian.toepfer@gmail.com", Url = "http://github.com/schwamster/docStack" }
                });

                options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = $"{identityServer}/connect/authorize",
                    Scopes = new Dictionary<string, string>
                    {
                        { "doc-stack-app-api", "doc-stack-app-api" }
                    }
                });

                // Assign scope requirements to operations based on AuthorizeAttribute
                options.OperationFilter<SecurityRequirementsOperationFilter>();

                //Determine base path for the application.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                options.IncludeXmlComments(GetXmlCommentsPath());
            });
            
        }

        private string GetXmlCommentsPath()
        {
            var app = PlatformServices.Default.Application;
            return System.IO.Path.Combine(app.ApplicationBasePath, System.IO.Path.ChangeExtension(app.ApplicationName, "xml"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var identityServer = Configuration["IdentityServerUrl"];
            var alternativeIdentityServer = Configuration["AlternativeIdentityServer"];

            app.UseCorrelationIdMiddleware(new CorrelationIdMiddlewareOptions());
            app.UseSerilogEnricherMiddleware();
            app.UseCors("default");

            var x = new Microsoft.IdentityModel.Tokens.TokenValidationParameters();
            var options = new IdentityServerAuthenticationOptions
            {
                Authority = $"{identityServer}",
                ApiName = "doc-stack-app-api",
                RequireHttpsMetadata = false,
            };
            var options2 = IdentityServer4.AccessTokenValidation.CombinedAuthenticationOptions.FromIdentityServerAuthenticationOptions(options);
            options2.JwtBearerOptions.TokenValidationParameters.ValidIssuers = new List<string>() {identityServer, alternativeIdentityServer};

            app.UseIdentityServerAuthentication(options2);

            app.UsePerformanceLog(new PerformanceLogOptions());
            app.UseHealthcheckEndpoint(new HealthCheckOptions() { Message = "Its alive"});

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUi(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "doc-stack-app-api");

                c.ConfigureOAuth2("doc-stack-app-api-swagger", null, "swagger-ui-realm", "Swagger UI");
            });
        }
    }
}
