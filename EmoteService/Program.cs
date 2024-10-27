using System.Net.Http.Headers;
using EmoteService.Utils;
using WatchDog;
using WatchDog.src.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("twitch_");

builder.Services.AddWatchDogServices(opt =>
{
    opt.SetExternalDbConnString = builder.Configuration.GetConnectionString("PostgresEmotes");
    opt.DbDriverOption = WatchDogDbDriverEnum.PostgreSql;
});

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

// configure the related library
RedisLib.RedisClient.SetupConnection(builder.Configuration.GetConnectionString("RedisConn"));

builder
    .Services.AddSevenTvClient()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("SevenTVURI"));
        // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        //     "Bearer",
        //     builder.Configuration.GetConnectionString("SevenTVToken")
        // );
        client.DefaultRequestHeaders.Add("Cookie", builder.Configuration.GetConnectionString("SevenTVToken"));
    });

var app = builder.Build();

app.Configuration.GetSection("redis").Bind(UtilsLib.Config.redis);
app.Configuration.GetSection("watchdog").Bind(UtilsLib.Config.watchdog);

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseWatchDogExceptionLogger();

app.UseWatchDog(opt =>
{
    opt.WatchPageUsername = UtilsLib.Config.watchdog.username;
    opt.WatchPagePassword = UtilsLib.Config.watchdog.password;
});

app.Run();
