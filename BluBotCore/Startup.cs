using BluBotCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace BluBotCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTwitchLibEventSubWebhooks(config =>
            {
                config.CallbackPath = "/webhooks";
                config.Secret = "supersecuresecret";
                config.EnableLogging = true;
            });

            services.AddHostedService<EventSubHostedService>();
            services.AddHostedService<LiveMonitor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseTwitchLibEventSubWebhooks();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
