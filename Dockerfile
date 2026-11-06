# --- build the frontend ---
FROM node:22-alpine AS web-build
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

# --- build the backend ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /server
COPY src/server/ ./
RUN dotnet restore Api/Api.csproj
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Uploads are transcoded with ffmpeg; the checks fail the build if its H.264 or AV1 encoder is missing.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/* \
    && ffmpeg -hide_banner -encoders | grep -q " libx264 " \
    && ffmpeg -hide_banner -encoders | grep -q " libsvtav1 "

WORKDIR /app
COPY --from=server-build /app/publish ./
COPY --from=web-build /web/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
