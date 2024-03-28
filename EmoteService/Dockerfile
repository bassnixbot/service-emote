FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish Emote/EmoteService -c release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS http://*:80
ENV APNETCORE_ENVIRONMENT Production
ENTRYPOINT dotnet EmoteService.dll
