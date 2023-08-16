using Jaeger.Samplers;
using Jaeger;
using OpenTracing;
using Jaeger.Reporters;
using Jaeger.Senders.Thrift;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Util;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ITracer>(serviceProvider =>
{
    var serviceName = serviceProvider.GetRequiredService<IWebHostEnvironment>().ApplicationName;
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    var reporter = new RemoteReporter.Builder()
                                     .WithLoggerFactory(loggerFactory)
                                     .WithSender(new UdpSender("host.docker.internal", 6831, 0))
                                     .Build();

    var tracer = new Tracer.Builder(serviceName)
        .WithSampler(new ConstSampler(true))
        .WithReporter(reporter)
        .Build();

    if (!GlobalTracer.IsRegistered())
    {
        GlobalTracer.Register(tracer);
    }

    return tracer;
});

builder.Services.AddOpenTracing();

builder.Services.Configure<AspNetCoreDiagnosticOptions>(options =>
{
    options.Hosting.IgnorePatterns.Add(context => context.Request.Path.Value.StartsWith("/status"));
    options.Hosting.IgnorePatterns.Add(context => context.Request.Path.Value.StartsWith("/metrics"));
    options.Hosting.IgnorePatterns.Add(context => context.Request.Path.Value.StartsWith("/swagger"));
});

builder.Services.Configure<HttpHandlerDiagnosticOptions>(options =>
        options.OperationNameResolver =
            request => $"{request.Method.Method}: {request?.RequestUri?.AbsoluteUri}");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();