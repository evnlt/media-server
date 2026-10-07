using Api.Extensions;
using Api.Services;
using Application;
using Application.Exceptions;
using Infrastructure;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddScoped<IMultipartFileReader, MultipartFileReader>();

var app = builder.Build();

app.Services.MigrateDatabase();
await app.Services.SeedAdminAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Services throw an HttpStatusException subclass to pick their response; this is the one
// place that maps it, so controllers don't need a try/catch per action.
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        (int statusCode, string title) = exception switch
        {
            HttpStatusException httpStatusException => (httpStatusException.StatusCode, httpStatusException.Title),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception?.Message
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
