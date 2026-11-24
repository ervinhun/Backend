FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["./", "."]
# Match the build host (arm64 on Apple Silicon, x64 elsewhere); a hardcoded x64 binary fails to start in an arm64 image.
RUN ARCH=$([ "$(uname -m)" = "aarch64" ] && echo arm64 || echo x64) && \
    dotnet publish "Api/Api.csproj" \
    -c Release \
    -o /app/publish \
    --runtime linux-musl-$ARCH \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:OpenApiGenerateDocumentsOnBuild=false

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["./Api"]