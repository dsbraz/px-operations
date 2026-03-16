# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS source
WORKDIR /src

COPY PX-Operations.sln ./
COPY src/Server/PxOperations.Api/PxOperations.Api.csproj src/Server/PxOperations.Api/
COPY src/Server/PxOperations.Application/PxOperations.Application.csproj src/Server/PxOperations.Application/
COPY src/Server/PxOperations.Domain/PxOperations.Domain.csproj src/Server/PxOperations.Domain/
COPY src/Server/PxOperations.Infrastructure/PxOperations.Infrastructure.csproj src/Server/PxOperations.Infrastructure/
COPY src/Client/PxOperations.BlazorWasm/PxOperations.BlazorWasm.csproj src/Client/PxOperations.BlazorWasm/
RUN dotnet restore src/Server/PxOperations.Api/PxOperations.Api.csproj

COPY . .
RUN dotnet publish src/Server/PxOperations.Api/PxOperations.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish src/Client/PxOperations.BlazorWasm/PxOperations.BlazorWasm.csproj -c Release -o /app/client-publish

FROM source AS build-migrate
ENV EXCLUDE_CLIENT_PROJECT_REFERENCE=true
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.0
RUN dotnet build src/Server/PxOperations.Api/PxOperations.Api.csproj -c Release -p:OpenApiGenerateDocumentsOnBuild=false

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS migrate
WORKDIR /src
ENV EXCLUDE_CLIENT_PROJECT_REFERENCE=true
COPY --from=build-migrate /tools /tools
COPY --from=build-migrate /root/.nuget /root/.nuget
COPY --from=build-migrate /src /src
ENTRYPOINT ["/tools/dotnet-ef", "database", "update", "--project", "src/Server/PxOperations.Infrastructure/PxOperations.Infrastructure.csproj", "--startup-project", "src/Server/PxOperations.Api/PxOperations.Api.csproj", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=source /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PxOperations.Api.dll"]

FROM nginx:1.27-alpine AS frontend
COPY src/Client/PxOperations.BlazorWasm/Hosting/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=source /app/client-publish/wwwroot /usr/share/nginx/html
RUN set -e; \
    framework_dir="/usr/share/nginx/html/_framework"; \
    dotnet_loader="$(find "${framework_dir}" -maxdepth 1 -type f -name 'dotnet.*.js' ! -name 'dotnet.native.*.js' ! -name 'dotnet.runtime.*.js' | head -n 1)"; \
    if [ -n "${dotnet_loader}" ]; then cp "${dotnet_loader}" "${framework_dir}/dotnet.js"; fi
ENV PX_OPERATIONS_API_BASE_URL=http://px-operations-api:8080
EXPOSE 8080
