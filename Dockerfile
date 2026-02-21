# 1. Build Stage: Requires clang and build-base for AOT compilation
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
RUN apk add --no-cache clang build-base zlib-dev

WORKDIR /src
COPY ["WebPing.csproj", "."]
# Restore for the specific Alpine runtime (musl)
RUN dotnet restore -r linux-musl-x64

COPY . .
# Publish as a native binary
RUN dotnet publish "WebPing.csproj" -c Release -r linux-musl-x64 -o /app/publish \
    --no-restore /p:PublishAot=true /p:PublishTrimmed=true

# 2. Final Stage: Use runtime-deps (no .NET runtime included, binary is self-contained)
FROM alpine:latest
WORKDIR /app

EXPOSE 8080

# Copy only the published native binary and static assets
COPY --from=build /app/publish .

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# The entrypoint is now the binary name itself, not 'dotnet binary.dll'
ENTRYPOINT ["./WebPing"]
