# Multi-stage build for the .NET 8 API.
# Build context is /src/api (where CxPlatform.sln sits).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CxPlatform.sln ./
COPY CxPlatform.Api/ ./CxPlatform.Api/
COPY CxPlatform.Application/ ./CxPlatform.Application/
COPY CxPlatform.Domain/ ./CxPlatform.Domain/
COPY CxPlatform.Infrastructure/ ./CxPlatform.Infrastructure/
RUN dotnet restore CxPlatform.sln
RUN dotnet publish CxPlatform.Api/CxPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 5001
ENTRYPOINT ["dotnet", "CxPlatform.Api.dll"]
