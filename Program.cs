using Sotoped.Configuration;

var builder = WebApplication.CreateBuilder(args);

if(!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.ListenAnyIP(int.Parse(port));
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", policy =>
    {
        policy.WithOrigins("https://congres-sotoped.onrender.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("AllowAllOriginsForRoot", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.InitializeDbTestDataAsync();

app.UseCors("MyCorsPolicy");
app.MapGet("/", () => "API is running!")
   .RequireCors("AllowAllOriginsForRoot");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();