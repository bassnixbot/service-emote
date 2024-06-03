using System.Net.Http.Headers;
using EmoteService.Utils;
using WatchDog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// configure the related library
RedisLib.RedisClient.SetupConnection(builder.Configuration.GetConnectionString("RedisConn"));

builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

builder
    .Services.AddSevenTvClient()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("SevenTVURI"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            builder.Configuration.GetConnectionString("SevenTVToken")
        );
    });

builder.Services.AddWatchDogServices(opt =>
{
    opt.SetExternalDbConnString = builder.Configuration.GetConnectionString("PostgresEmotes");
    opt.DbDriverOption = WatchDog.src.Enums.WatchDogDbDriverEnum.PostgreSql;
});

builder.Configuration.AddEnvironmentVariables("twitch_");

var app = builder.Build();

app.Configuration.GetSection("redis").Bind(Config.redis);
app.Configuration.GetSection("watchdog").Bind(Config.watchdog);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// inject into the middleware
app.UseWatchDogExceptionLogger();

app.UseWatchDog(opt =>
{
    opt.WatchPageUsername = Config.watchdog.username;
    opt.WatchPagePassword = Config.watchdog.password;
});

app.Run();
