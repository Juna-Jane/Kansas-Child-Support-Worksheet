# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY KansasChildSupport.sln ./
COPY src/KansasChildSupport.Web/KansasChildSupport.Web.csproj src/KansasChildSupport.Web/
RUN dotnet restore src/KansasChildSupport.Web/KansasChildSupport.Web.csproj

COPY src/KansasChildSupport.Web/ src/KansasChildSupport.Web/
RUN dotnet publish src/KansasChildSupport.Web/KansasChildSupport.Web.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "KansasChildSupport.Web.dll"]
