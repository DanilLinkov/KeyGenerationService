using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KeyGenerationService.BackgroundTasks;
using KeyGenerationService.Data;
using KeyGenerationService.KeyDatabaseSeeders;
using KeyGenerationService.Services;
using KeyGenerationService.Services.KeyCacheService;
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

namespace KeyGenerationService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "KeyGenerationService", Version = "v1"});
            });
            
            services.AddAutoMapper(typeof(Startup));

            services.AddDbContext<DataContext>(o =>
                o.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddSingleton<IKeyCacheService, KeyCacheService>(o =>
            {
                var redisCacheOptions = new RedisCacheOptions()
                {
                    Configuration = Configuration.GetConnectionString("RedisConnection"),
                };
                
                return new KeyCacheService(new RedisCache(redisCacheOptions),"test");
            });

            services.AddSingleton<IKeyDatabaseSeeder, KeyDatabaseSeeder>(o =>
            {
                var dbContext = services.BuildServiceProvider().GetRequiredService<DataContext>();
                var allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
                var randomNumberGenerator = RandomNumberGenerator.Create();

                return new KeyDatabaseSeeder(dbContext,allowedChars,randomNumberGenerator);
            });
            services.AddScoped<IKeyService, KeyService>();

            services.AddSingleton<RefillKeysInCacheTask>(o =>
            {
                var dbContext = services.BuildServiceProvider().GetRequiredService<DataContext>();
                var databaseSeeder = o.GetRequiredService<IKeyDatabaseSeeder>();
                var keyCacheService = o.GetRequiredService<IKeyCacheService>();
                var maxKeysInCache = 3;

                return new RefillKeysInCacheTask(dbContext, databaseSeeder, keyCacheService, maxKeysInCache);
            });
            
            services.AddHostedService<RefillKeysInCacheTask>(o => o.GetRequiredService<RefillKeysInCacheTask>());
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}