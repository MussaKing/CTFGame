# Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CTFGame.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

# Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Каталог для ключей Data Protection (ими подписаны cookie входа).
# Он монтируется в volume, чтобы игроков не разлогинивало при перезапуске контейнера.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet

COPY --from=build /app ./

USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CTFGame.dll"]
