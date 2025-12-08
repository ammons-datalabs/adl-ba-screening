FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/AmmonsDataLabs.BuyersAgent.Screening.Api/AmmonsDataLabs.BuyersAgent.Screening.Api.csproj"
RUN dotnet publish "src/AmmonsDataLabs.BuyersAgent.Screening.Api/AmmonsDataLabs.BuyersAgent.Screening.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Copy local flood data into the container at /data
# data/ is .gitignored but available to Docker build
# Layout: data/flood/bcc/*.ndjson
COPY data /data

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AmmonsDataLabs.BuyersAgent.Screening.Api.dll"]