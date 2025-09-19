# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MinimalApi.csproj ./
RUN dotnet restore MinimalApi.csproj

COPY . .
RUN dotnet publish MinimalApi.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80 443
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MinimalApi.dll"]


