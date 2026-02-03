FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CashFlow.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/BuildingBlocks/CashFlow.BuildingBlocks.Core/CashFlow.BuildingBlocks.Core.csproj", "src/BuildingBlocks/CashFlow.BuildingBlocks.Core/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Domain/CashFlow.Consolidation.Domain.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Domain/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Application/CashFlow.Consolidation.Application.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Application/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Infrastructure/CashFlow.Consolidation.Infrastructure.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Infrastructure/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj", "src/Services/Consolidation/CashFlow.Consolidation.API/"]

# Restore dependencies
RUN dotnet restore "src/Services/Consolidation/CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Services/Consolidation/CashFlow.Consolidation.API"
RUN dotnet build "CashFlow.Consolidation.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CashFlow.Consolidation.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Consolidation.API.dll"]
