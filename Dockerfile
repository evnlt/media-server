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
WORKDIR /app
COPY --from=server-build /app/publish ./
COPY --from=web-build /web/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
