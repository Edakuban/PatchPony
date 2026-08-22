FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/PatchPony.Worker/PatchPony.Worker.csproj
RUN dotnet publish src/PatchPony.Worker/PatchPony.Worker.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
RUN apk add --no-cache ripgrep && addgroup -S patchpony && adduser -S patchpony -G patchpony
COPY --from=build /app/publish .
USER patchpony
ENTRYPOINT ["dotnet", "PatchPony.Worker.dll"]
