using Sotoped.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configurer Kestrel pour écouter sur le port défini par Render
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});
Console.WriteLine($"Connection String: {builder.Configuration.GetConnectionString("DefaultConnection")}");


builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.InitializeDbTestDataAsync();

// Activer Swagger même en production pour tester l'API
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

app.UseAuthorization();
app.MapControllers();

// Pour des fins de débogage, ajoutez une route simple pour tester si l'API fonctionne
app.MapGet("/", () => "API is running!");

app.Run();