FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Gasoft.Api/Gasoft.Api.csproj
RUN dotnet publish src/Gasoft.Api/Gasoft.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migration
WORKDIR /src
COPY . .
RUN dotnet restore src/Gasoft.Api/Gasoft.Api.csproj
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.14
ENV PATH="/tools:${PATH}"
ENTRYPOINT ["dotnet", "ef"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gasoft.Api.dll"]
