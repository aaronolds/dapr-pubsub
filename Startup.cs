using System.Text.Json;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaprIntro
{
    public class Startup
    {
        private const string StateStore = "statestore";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDaprClient();

            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JsonSerializerOptions serializerOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();

                endpoints.MapGet("{id}", Balance);
                endpoints.MapPost("deposit", Deposit).WithTopic("messagebus", "deposit");
                endpoints.MapPost("withdraw", Withdraw).WithTopic("messagebus","withdraw");
            });

            async Task Balance(HttpContext context)
            {
                var client = context.RequestServices.GetRequiredService<DaprClient>();

                var id = (string)context.Request.RouteValues["id"];
                var account = await client.GetStateAsync<Account>(StateStore, id);
                if (account == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, account, serializerOptions);
            }

            async Task Deposit(HttpContext context, ILogger<Startup> logger)
            {

                logger.LogDebug("Enter deposit");
                var client = context.RequestServices.GetRequiredService<DaprClient>();

                var transaction = await JsonSerializer.DeserializeAsync<Transaction>(context.Request.Body, serializerOptions);
                var account = await client.GetStateAsync<Account>(StateStore, transaction.Id);
                if (account == null)
                {
                    account = new Account() { Id = transaction.Id, };
                }

                if (transaction.Amount < 0m)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                account.Balance += transaction.Amount;
                await client.SaveStateAsync(StateStore, transaction.Id, account);

                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, account, serializerOptions);
            }

            async Task Withdraw(HttpContext context, ILogger<Startup> logger)
            {
                logger.LogDebug("Enter withdraw");
                var client = context.RequestServices.GetRequiredService<DaprClient>();

                var transaction = await JsonSerializer.DeserializeAsync<Transaction>(context.Request.Body, serializerOptions);
                var account = await client.GetStateAsync<Account>(StateStore, transaction.Id);
                if (account == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                if (transaction.Amount < 0m)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                account.Balance -= transaction.Amount;
                await client.SaveStateAsync(StateStore, transaction.Id, account);

                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, account, serializerOptions);
            }
        }
    }

    /// <summary>
    /// Class representing an Account for samples.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Gets or sets account id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets account balance.
        /// </summary>
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// Represents a transaction used by sample code.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Gets or sets account id for the transaction.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets amount for the transaction.
        /// </summary>
        public decimal Amount { get; set; }
    }
}