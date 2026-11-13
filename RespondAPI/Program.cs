using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums are serialized as strings so the JavaScript client reads "Groq"
// rather than the integer 0, which would silently break on reordering.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// Register DAOs, business services and provider clients here as they are written.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "RespondAPI v1");
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
