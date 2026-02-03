FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CashFlow.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/BuildingBlocks/CashFlow.BuildingBlocks.Core/CashFlow.BuildingBlocks.Core.csproj", "src/BuildingBlocks/CashFlow.BuildingBlocks.Core/"]
COPY ["src/BuildingBlocks/CashFlow.BuildingBlocks.EventBus/CashFlow.BuildingBlocks.EventBus.csproj", "src/BuildingBlocks/CashFlow.BuildingBlocks.EventBus/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Domain/CashFlow.Consolidation.Domain.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Domain/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Application/CashFlow.Consolidation.Application.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Application/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Infrastructure/CashFlow.Consolidation.Infrastructure.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Infrastructure/"]
COPY ["src/Services/Consolidation/CashFlow.Consolidation.Worker/CashFlow.Consolidation.Worker.csproj", "src/Services/Consolidation/CashFlow.Consolidation.Worker/"]

# Restore dependencies
RUN dotnet restore "src/Services/Consolidation/CashFlow.Consolidation.Worker/CashFlow.Consolidation.Worker.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Services/Consolidation/CashFlow.Consolidation.Worker"
RUN dotnet build "CashFlow.Consolidation.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CashFlow.Consolidation.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Consolidation.Worker.dll"]
