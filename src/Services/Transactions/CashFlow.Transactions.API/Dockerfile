FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CashFlow.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/BuildingBlocks/CashFlow.BuildingBlocks.Core/CashFlow.BuildingBlocks.Core.csproj", "src/BuildingBlocks/CashFlow.BuildingBlocks.Core/"]
COPY ["src/BuildingBlocks/CashFlow.BuildingBlocks.EventBus/CashFlow.BuildingBlocks.EventBus.csproj", "src/BuildingBlocks/CashFlow.BuildingBlocks.EventBus/"]
COPY ["src/Services/Transactions/CashFlow.Transactions.Domain/CashFlow.Transactions.Domain.csproj", "src/Services/Transactions/CashFlow.Transactions.Domain/"]
COPY ["src/Services/Transactions/CashFlow.Transactions.Application/CashFlow.Transactions.Application.csproj", "src/Services/Transactions/CashFlow.Transactions.Application/"]
COPY ["src/Services/Transactions/CashFlow.Transactions.Infrastructure/CashFlow.Transactions.Infrastructure.csproj", "src/Services/Transactions/CashFlow.Transactions.Infrastructure/"]
COPY ["src/Services/Transactions/CashFlow.Transactions.API/CashFlow.Transactions.API.csproj", "src/Services/Transactions/CashFlow.Transactions.API/"]

# Restore dependencies
RUN dotnet restore "src/Services/Transactions/CashFlow.Transactions.API/CashFlow.Transactions.API.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Services/Transactions/CashFlow.Transactions.API"
RUN dotnet build "CashFlow.Transactions.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CashFlow.Transactions.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CashFlow.Transactions.API.dll"]
