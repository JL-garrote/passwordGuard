# Etapa 1: compilar con el SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero el csproj: si solo cambia el código, el restore sale de caché
COPY PasswordGuard.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out --no-restore

# Etapa 2: ejecutar con la imagen ligera (solo el runtime de ASP.NET)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

# La clave de Gemini no se copia: llega como variable de entorno desde docker-compose
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PasswordGuard.Api.dll"]
