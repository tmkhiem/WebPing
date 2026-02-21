FROM ://mcr.microsoft.com AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM ://mcr.microsoft.com AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebPing.csproj", "."]
RUN dotnet restore "./WebPing.csproj"
COPY . .
RUN dotnet build "./WebPing.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "./WebPing.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebPing.dll"]
