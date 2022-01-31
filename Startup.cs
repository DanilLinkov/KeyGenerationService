using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AspNetCore.Authentication.ApiKey;
using AutoMapper;
using KeyGenerationService.Auth;
using KeyGenerationService.Auth.RateLimiters;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.BackgroundTasks.BackgroundTaskQueues;
using KeyGenerationService.Data;
using KeyGenerationService.KeyCachers;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.KeyRetrievers;
using KeyGenerationService.KeyReturners;
using KeyGenerationService.Services;
using KeyGenerationService.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace KeyGenerationService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "KeyGenerationService", Version = "v1"});
            });
            
            services.AddAutoMapper(typeof(Startup));

            services.AddHttpContextAccessor();

            services.AddDbContext<DataContext>(o =>
                o.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton<IKeyDatabaseSeeder, KeyDatabaseSeeder>(o =>
            {
                var serviceScopeFactory = o.GetService<IServiceScopeFactory>();
                var allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
                var randomNumberGenerator = RandomNumberGenerator.Create();

                return new KeyDatabaseSeeder(serviceScopeFactory, allowedChars, randomNumberGenerator);
            });
            
            services.AddSingleton<IKeyCacher, KeyCacher>(o =>
            {
                var redisCacheOptions = new RedisCacheOptions()
                {
                    Configuration = Configuration.GetConnectionString("RedisConnection"),
                };

                string cacheKey = Configuration.GetSection("CacheKey").Value;
                var buildCacheKey = new Func<string, int, string>((key, size) => key + $"_size_${size}");
                
                return new KeyCacher(new RedisCache(redisCacheOptions),cacheKey, buildCacheKey);
            });

            services.AddScoped<IKeyRetriever, KeyRetriever>();
            services.AddScoped<IKeyReturner, KeyReturner>();
            services.AddScoped<IKeyService, KeyService>(o =>
            {
                var rateLimiter = o.GetRequiredService<IRateLimiter>();
                var mapper = o.GetRequiredService<IMapper>();
                var context = o.GetRequiredService<DataContext>();
                var keyCacher = o.GetRequiredService<IKeyCacher>();
                var keyReturner = o.GetRequiredService<IKeyReturner>();
                var keyRetriever = o.GetRequiredService<IKeyRetriever>();
                var databaseSeeder = o.GetRequiredService<IKeyDatabaseSeeder>();
                var refillKeysInCacheTask = o.GetRequiredService<RefillKeysInCacheTask>();
                var backgroundTaskQueue = o.GetRequiredService<IBackgroundTaskQueue>();
                var keysToGenerateOnEmptyCache = Configuration.GetSection("KeysToGenerateOnEmptyCache").Get<int>();
                
                return new KeyService(rateLimiter, mapper,context, databaseSeeder, refillKeysInCacheTask, backgroundTaskQueue, keyCacher, keyRetriever, keyReturner, keysToGenerateOnEmptyCache);
            });

            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            
            services.AddSingleton<RefillKeysInCacheTask>(o =>
            {
                var serviceScopeFactory = o.GetService<IServiceScopeFactory>();
                var databaseSeeder = o.GetRequiredService<IKeyDatabaseSeeder>();
                var keyCacheService = o.GetRequiredService<IKeyCacher>();
                var maxKeysInCache = Configuration.GetValue<int>("MaxKeysInCache");
                var backgroundTaskQueue = o.GetRequiredService<IBackgroundTaskQueue>();
                var keysToGenerateOnEmptyCache = Configuration.GetValue<int>("KeysToGenerateOnEmptyCache");

                return new RefillKeysInCacheTask(serviceScopeFactory, databaseSeeder, keyCacheService, maxKeysInCache, backgroundTaskQueue, keysToGenerateOnEmptyCache);
            });
            services.AddHostedService<RefillKeysInCacheTask>(o => o.GetRequiredService<RefillKeysInCacheTask>());

            services.AddScoped<IRateLimiter, RateLimiter>();
            
            services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
                .AddApiKeyInHeader<ApiKeyProvider>(o =>
                {
                    o.Realm = "KeyGenerationService";
                    o.KeyName = "API-KEY";
                });
            
            services.AddAuthorization(options =>
            {
            	options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KeyGenerationService v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}