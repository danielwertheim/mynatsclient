FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build src/MyNatsClient.sln -c Release --no-incremental
RUN dotnet test src/testing/UnitTests/UnitTests.csproj -c Release --no-build
RUN dotnet test src/testing/IntegrationTests/IntegrationTests.csproj -c Release --no-build
