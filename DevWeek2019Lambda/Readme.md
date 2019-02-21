# DeveloperWeek 2019 - AWS Lambda with C# #

This project demostrates concepts covered in my presentation at DevWeek 2019 in Oakland, California.


## Binary Content ##

To allow Lambda to server binary content, you need to specify the content types in LambdaEntryPoint.cs, along with configuring the API Gateway

```csharp
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseStartup<Startup>();

        this.RegisterResponseContentEncodingForContentType("application/vnd.ms-excel",
            ResponseContentEncoding.Base64);
    }
```

To test this, use the FileController's "sample" route. The file returned will be encoded as a base64 string.

## S3 Secure URLs ##

Sometimes, it is better to have Lambda return a secure URL to a file in an S3 bucket.

```csharp
    var linkRequest = new GetPreSignedUrlRequest()
    {
        BucketName = _s3Bucket,
        Key = s3File,
        Expires = DateTime.Now.AddMinutes(1),
        Verb = HttpVerb.GET
    };

    var response = _s3Client.GetPreSignedURL(linkRequest);
```

Private buckets which allow file downloads over secure links must have CORS configured. The serverless.template of this project creates a bucket and does some initial setup.

```json
    "Bucket" : {
        "Type" : "AWS::S3::Bucket",
        "Condition" : "CreateS3Bucket",
        "Properties" : {
            "BucketName" : { "Fn::If" : ["BucketNameGenerated", {"Ref" : "AWS::NoValue" }, { "Ref" : "BucketName" } ] },
            "CorsConfiguration" : {
				"CorsRules" : [ 
					{
					  "AllowedHeaders" : [ "*" ],
					  "AllowedMethods" : [ "GET","OPTIONS" ],
					  "AllowedOrigins" : [ "*" ]
					}
				]
			}
        }
    }
```

To test this, use the FileController's "link" route. The URL returned should be able to download the file, but you must configure your bucket correctly.

## Logging ##

The LoggingController represents several different methods of logging.

### API Gateway Logging ###

If your API Gateway is configured for logging, as the presentation demostrates, the below endpoint will overflow the response buffer, causing the API Gateway to log an error in CloudWatch.

```csharp
    [HttpGet("flood")]
    public string Flood()
    {
        var builder = new StringBuilder();

        //1024 bytes * 1024 kb * 8 mb should overflow lambda
        for(int i = 0; i < (1024 * 1024 * 8); i++)
        {
            builder.Append("A");
        }

        return builder.ToString();
    }
```

### Console Logging ###

All console messages will be written to the Lambda's default Cloudwatch Stream.

```csharp
    [HttpGet("console/{msg}")]
    public IActionResult GetConsole(string msg)
    {
        Console.WriteLine($"CONSOLE: {msg}");

        return Ok($"Message logged to console: {msg}");
    }
```

### Custom Logging ###

Injecting ILogger is preferrable to writing to the console. The following example shows how to use the CloudWatch SDK to log to a custom stream.

In appsettings.json:
```json
  "AWS.Logging": {
    "Region": "us-east-1",
    "LogGroup": "DevWeek2019Lambda",
    "MaxQueuedMessages": 1,
    "LogLevel": {
      "Default": "Warning",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
```
In Startup.cs
```csharp
    public Startup(IConfiguration rootConfiguration, IHostingEnvironment env)
    {
        // Read the appsetting.json file for the configuration details
        var builder = new ConfigurationBuilder()
            .AddConfiguration(rootConfiguration)
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    public static IConfiguration Configuration { get; private set; }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
        var config = Configuration.GetAWSLoggingConfigSection();
            
        // Create a logging provider based on the configuration information passed through the appsettings.json
        loggerFactory.AddAWSProvider(Configuration.GetAWSLoggingConfigSection());
    }
```

Finally, in the LoggingController:
```csharp
    private readonly ILogger<LoggingController> _logger;

    public LoggingController(ILogger<LoggingController> logger) {
        this._logger = logger;
    }

    [HttpGet("custom/{msg}")]
    public IActionResult GetCustom(string msg)
    {
        //Messages may take a while to appear in this log
        _logger.LogCritical("LOGGER: {0}", msg);

        return Ok($"Message logged to cloudwatch: {msg}");
    }
```
## Environment Variables ##

The EnvironmentController demonstrates how Lambda reads environment variables.

In the base appsettings.json, the variable EnvironmentOverride is configured. This is the default value.
```json
  "EnvironmentOverride": "default"
```

In the appsettings.Development.json, there is a override that is used when the environment variable "ASPNETCORE_Environment" is set to "Development"
```json
  "EnvironmentOverride": "development"
```

The "ASPNETCORE_Environment" variable is set by the serverless template:
```json
    "AspNetCoreFunction" : {
      "Type" : "AWS::Serverless::Function",
      "Properties": {
        "Environment" : {
          "Variables" : {
            "AppS3Bucket" : { "Fn::If" : ["CreateS3Bucket", {"Ref":"Bucket"}, { "Ref" : "BucketName" } ] },
			"ASPNETCORE_Environment" : "Development"
          }
        },
```

Finally, this value can be changed at runtime by adding "EnvironmentOverride" as an environment variable in the Lambda console.
The current value will be returned by the EnvironmentController
```csharp
    // GET api/override
    [HttpGet("override")]
    public IActionResult Get()
    {            
        return Ok(_config.GetValue<string>("EnvironmentOverride"));
    }
```

## Serverless Templates ##

The serverless.template defines how your function is created and deployed, along with any resources created for it.

### Variables ###

The serverless template defines a parameter for the function's memory allocation.
```json
  "Parameters" : {
	"MemorySize" : {
		"Type": "Number",
		"Description" : "The amount of memory to allocate to this lamdba function",
		"Default" : "128",
		"MaxValue": "3008",
		"MinValue": "128"
	}
  },
```

It then uses the value of this parameter when configuring the function.
```json
"AspNetCoreFunction" : {
      "Type" : "AWS::Serverless::Function",
      "Properties": {
        "Handler": "DevWeek2019Lambda::DevWeek2019Lambda.LambdaEntryPoint::FunctionHandlerAsync",
        "MemorySize": {"Ref" : "MemorySize"},
      }
    },
```

### Defaults ###

The file aws-lambda-tools-defaults.json defines default values for paramters, among other things.
```json
    "template-parameters" : "\"ShouldCreateBucket\"=\"true\";\"BucketName\"=\"devweek-2019-download\";\"MemorySize\"=\"512\"",
```

## Offloading Long-Running Tasks ##

This project includes an example of offloading long-running work to a second lambda.

In the QueueController, the PostMessage endpoint will put a message on the configured queue. The queue is configured by the serverless.template when the API is deployed. (You will need to enter your own SQS url in appsettings.Private.json.)

```csharp
	[HttpPost]
	public async Task<IActionResult> PostMessage([FromBody] QueueRequest message)
	{
		var request = new SendMessageRequest
		{
			MessageBody = message.Message,
			QueueUrl = _queueUrl
		};

		await _sqsClient.SendMessageAsync(request);

		return Ok($"Message '{message.Message}' from user {message.User} queued.");
	}
```
Deploy DevWeek2019LambdaWorker, and configure it with an SQS trigger on the created queue from the Lambda console.

When a message is posted to the queue, it logs a message to CloudWatch to simulate work.

```csharp
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogLine($"Processed message {message.Body}");

        // TODO: Do interesting work based on the new message
        await Task.CompletedTask;
    }
```


## More Information ##

I highly recommend looking that the [GitHub for AWS Lambda](https://github.com/aws/aws-lambda-dotnet), it provides extensive information.

