# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#
#
## Esta fase se usa para compilar el proyecto de servicio
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["MONITOR.SERVICE.VISUALAB/MONITOR.SERVICE.VISUALAB.csproj", "MONITOR.SERVICE.VISUALAB/"]
#RUN dotnet restore "./MONITOR.SERVICE.VISUALAB/MONITOR.SERVICE.VISUALAB.csproj"
#COPY . .
#WORKDIR "/src/MONITOR.SERVICE.VISUALAB"
#RUN dotnet build "./MONITOR.SERVICE.VISUALAB.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./MONITOR.SERVICE.VISUALAB.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "MONITOR.SERVICE.VISUALAB.dll"]


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MONITOR.SERVICE.VISUALAB.csproj .
RUN dotnet restore
COPY . .

RUN dotnet build "MONITOR.SERVICE.VISUALAB.csproj" -c Release -o /app/build

RUN dotnet publish -c release -o /app

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MONITOR.SERVICE.VISUALAB.dll"]


#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#WORKDIR /src
#COPY ["MONITOR.SERVICE.VISUALAB.csproj", "./"]
#RUN dotnet restore
#COPY . .
#WORKDIR "/src/."
#RUN dotnet build "MONITOR.SERVICE.VISUALAB.csproj" -c Release -o /app/build
#
#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
#WORKDIR /app
#COPY --from=build /app/build .
#ENTRYPOINT ["dotnet", "MONITOR.SERVICE.VISUALAB.dll"]