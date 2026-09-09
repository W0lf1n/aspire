# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Aspire.Domain/Aspire.Domain.csproj src/Aspire.Domain/
COPY src/Aspire.Infrastructure/Aspire.Infrastructure.csproj src/Aspire.Infrastructure/
COPY src/Aspire.Api/Aspire.Api.csproj src/Aspire.Api/
RUN dotnet restore src/Aspire.Api/Aspire.Api.csproj
COPY src/ src/
RUN dotnet publish src/Aspire.Api/Aspire.Api.csproj -c Release -o /app

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The media root, owned by the app user before the user switch. A named volume
# mounted over an existing directory copies that directory's ownership on its
# first mount, which is the only reason a non-root API can write into it.
RUN mkdir -p /data/media && chown -R $APP_UID:$APP_UID /data/media

# Not root. The API writes only under /data/media — Postgres holds the rest.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Aspire.Api.dll"]
