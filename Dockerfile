FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/TodoApi/TodoApi.csproj src/TodoApi/
RUN dotnet restore src/TodoApi/TodoApi.csproj

COPY src/ src/
RUN dotnet publish src/TodoApi/TodoApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# pasta do sqlite (volume no compose)
RUN mkdir -p /app/data && chown -R $APP_UID /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/todo.db"

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoApi.dll"]
