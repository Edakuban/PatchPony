FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/PatchPony.Gateway/PatchPony.Gateway.csproj
RUN dotnet publish src/PatchPony.Gateway/PatchPony.Gateway.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
RUN apk add --no-cache ripgrep && addgroup -S patchpony && adduser -S patchpony -G patchpony
COPY --from=build /app/publish .
USER patchpony
EXPOSE 8080
ENTRYPOINT ["dotnet", "PatchPony.Gateway.dll"]
