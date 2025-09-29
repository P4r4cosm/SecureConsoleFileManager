FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SecureConsoleFileManager/SecureConsoleFileManager.csproj", "SecureConsoleFileManager/"]
RUN dotnet restore "SecureConsoleFileManager/SecureConsoleFileManager.csproj"
COPY . .
WORKDIR "/src/SecureConsoleFileManager"
RUN dotnet build "./SecureConsoleFileManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SecureConsoleFileManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecureConsoleFileManager.dll"]
