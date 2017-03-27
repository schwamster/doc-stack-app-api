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
using IdentityServer4;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using IdentityModel.Client;
using Newtonsoft.Json;

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
            var identityServer = Configuration["IdentityServerUrl"];

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
                        { "doc-stack-app-api", "doc-stack-app-api" },
                        { "doc-store", "doc-store" },

                        //this does not work - since we only ask for response type token...
                        //{ "tenant", "tenant info" },
                        //{ "openid", "openid" },
                        //{ "profile", "profile" }
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

            app.UseCorrelationIdMiddleware(new CorrelationIdMiddlewareOptions());
            app.UseSerilogEnricherMiddleware();
            app.UseCors("default");

            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = identityServer,
                RequireHttpsMetadata = false,

                ApiName = "doc-stack-app-api"
            });

            //var options = new IdentityServerAuthenticationOptions
            //{
            //    Authority = $"{identityServer}",
            //    ApiName = "doc-stack-app-api",
            //    RequireHttpsMetadata = false,
            //    SaveToken = true,

            //    JwtBearerEvents = new JwtBearerEvents()
            //    {
            //        OnTokenValidated = async (context) =>
            //        {
            //            var principal = (ClaimsPrincipal)context.Ticket.Principal;
            //            var identity = (ClaimsIdentity)principal.Identity;

            //            var accessToken = ((JwtSecurityToken)context.SecurityToken).RawData;
            //            identity.AddClaim(new Claim("token", accessToken));
            //            try
            //            {

            //                Console.WriteLine($"MÖÖÖÖÖÖÖÖÖÖÖÖÖÖP: {context.Options.Authority}");
            //                var discoveryClient = new DiscoveryClient(context.Options.Authority, null);
            //                var doc = await discoveryClient.GetAsync();

            //                var json = Newtonsoft.Json.JsonConvert.SerializeObject(doc, Formatting.Indented);
            //                Console.WriteLine(json);
                            
            //                Console.WriteLine($"MÖÖÖÖÖÖÖÖÖÖÖÖÖÖP: {doc.UserInfoEndpoint}");
            //                var userInfoClient = new UserInfoClient(doc.UserInfoEndpoint);

            //                var response = await userInfoClient.GetAsync(accessToken);

            //                json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Formatting.Indented);
            //                Console.WriteLine(json);
            //                if (response.Claims != null)
            //                {
            //                    identity.AddClaims(response.Claims);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine($"Claims could not be retrieved: {ex}");
            //            }

            //            context.Ticket = new AuthenticationTicket(principal, context.Ticket.Properties, context.Ticket.AuthenticationScheme);
            //        }
            //    }
            //};
            //var options2 = IdentityServer4.AccessTokenValidation.CombinedAuthenticationOptions.FromIdentityServerAuthenticationOptions(options);

            //options2.JwtBearerOptions.TokenValidationParameters.ValidIssuers = new List<string>() { identityServer };

            //app.UseIdentityServerAuthentication(options2);

            app.UsePerformanceLog(new PerformanceLogOptions());
            app.UseHealthcheckEndpoint(new HealthCheckOptions() { Message = "Its alive" });

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

        public static string GetIp(string hostname)
                        => System.Net.Dns.GetHostEntryAsync(hostname)
                            .Result
                            .AddressList
                            .First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .ToString();
    }
}
