using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using IdentityServiceApi.Service;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// AWS Config
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

// Add AWS Services
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonDynamoDB>();

// Register AmazonS3Client manually
//builder.Services.AddSingleton<IAmazonS3>(sp =>
//{
//    return new AmazonS3Client(awsCredentials, RegionEndpoint.APSouth2); // Replace with your AWS region
//});

//builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
//{
//    return new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.APSouth2); // Replace with your AWS region
//});

builder.Services.AddScoped<IdentityVerificationServices>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (exceptionHandlerPathFeature?.Error != null)
        {
            logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception occurred.");
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred. Please try again later."
        });
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
