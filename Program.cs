using todo_app.Services;
using todo_app.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<ImageCacheService>();
builder.Services.AddSingleton<IImageCacheService, ImageCacheService>();

builder.Services.AddHttpClient("todosHttpClient", x =>
{
    x.BaseAddress = new Uri(builder.Configuration["todosHttpClientBaseAddress"]);
});

var app = builder.Build();

var portEnv = Environment.GetEnvironmentVariable("PORT");

if (!int.TryParse(portEnv, out int port))
{
    port = 8080;
}

// Конфігуримо Kestrel слухати цей порт
app.Urls.Clear();
app.Urls.Add($"http://*:{port}");

// Лог на старті
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"Server started in port {port}");
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Ok");

app.Run();
