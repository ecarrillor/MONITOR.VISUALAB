FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MONITOR.SERVICE.VISUALAB.csproj .
RUN dotnet restore
COPY . .

RUN dotnet build "MONITOR.SERVICE.VISUALAB.csproj" -c Release -o /app/build

RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 7020
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MONITOR.SERVICE.VISUALAB.dll"]
