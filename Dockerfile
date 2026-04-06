FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["CareConnectEMR.API/CareConnectEMR.API.csproj", "CareConnectEMR.API/"]
COPY ["CareConnectEMR.Application/CareConnectEMR.Application.csproj", "CareConnectEMR.Application/"]
COPY ["CareConnectEMR.Domain/CareConnectEMR.Domain.csproj", "CareConnectEMR.Domain/"]
COPY ["CareConnectEMR.Infrastructure/CareConnectEMR.Infrastructure.csproj", "CareConnectEMR.Infrastructure/"]

RUN dotnet restore "CareConnectEMR.API/CareConnectEMR.API.csproj"

COPY . .
WORKDIR "/src/CareConnectEMR.API"
RUN dotnet build "CareConnectEMR.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CareConnectEMR.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CareConnectEMR.API.dll"]
