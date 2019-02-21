using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevWeek2019Lambda
{
    public class Startup
    {
        public const string AppS3BucketKey = "AppS3Bucket";
        public const string AppQueueUrl = "AppQueueUrl";

        public Startup(IConfiguration rootConfiguration, IHostingEnvironment env)
        {
            // Read the appsetting.json file for the configuration details
            var builder = new ConfigurationBuilder()
                .AddConfiguration(rootConfiguration)
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile($"appsettings.Private.json", optional: true) //holds things not committed to source control
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var defaultOptions = Configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(defaultOptions);
                        
            services.AddSingleton<AmazonS3Config>(new AmazonS3Config{
                RegionEndpoint = defaultOptions.Region,
                SignatureVersion = "v4"
            });

            // Add S3 to the ASP.NET Core dependency injection framework.
            services.AddScoped<Amazon.S3.IAmazonS3>(s => {
                var s3Config = s.GetRequiredService<AmazonS3Config>();
                return new AmazonS3Client(s3Config);
            });

            services.AddSingleton<AmazonSQSConfig>(new AmazonSQSConfig
            {
                RegionEndpoint = defaultOptions.Region,
                ServiceURL = $"http://sqs.{defaultOptions.Region.SystemName}.amazonaws.com"
            });

            services.AddScoped<IAmazonSQS>(s =>
            {
                var sqsConfig = s.GetRequiredService<AmazonSQSConfig>();
                return new AmazonSQSClient(sqsConfig);
            });

            services.AddSingleton<IConfiguration>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            var config = Configuration.GetAWSLoggingConfigSection();
                        
            // Create a logging provider based on the configuration information passed through the appsettings.json
            loggerFactory.AddAWSProvider(Configuration.GetAWSLoggingConfigSection());

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
