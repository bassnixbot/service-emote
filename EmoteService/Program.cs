using System.Net.Http.Headers;
using EmoteService.Redis;
using EmoteService.Utils;
using WatchDog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at
// https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = builder.Configuration.GetConnectionString("RedisConn");
});

builder.Services.AddSingleton<IRedisCache, RedisCache>();
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
    opt.SetExternalDbConnString = builder.Configuration.GetConnectionString("DefaultConnection");
    opt.DbDriverOption = WatchDog.src.Enums.WatchDogDbDriverEnum.PostgreSql;
});

var app = builder.Build();

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
    opt.WatchPagePassword = "admin";
    opt.WatchPageUsername = "admin";
});

app.Run();