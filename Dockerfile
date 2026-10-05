FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WRMS.sln ./
COPY src/ ./src/
RUN dotnet restore WRMS.sln
RUN dotnet publish src/WRMS.Web/WRMS.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/App_Data/Uploads /app/Logs \
    && chown -R $APP_UID:$APP_UID /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet WRMS.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
