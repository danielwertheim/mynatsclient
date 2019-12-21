FROM mcr.microsoft.com/dotnet/core/sdk:3.1
COPY . ./
RUN dotnet build src/MyNatsClient.sln -c Release --no-incremental
RUN dotnet test src/UnitTests/UnitTests.csproj -c Release --no-build
RUN dotnet test src/IntegrationTests/IntegrationTests.csproj -c Release --no-build