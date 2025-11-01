FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar .csproj e restaurar dependências
COPY IdebAPI.csproj .
RUN dotnet restore

# Copiar todo o código fonte
COPY . .

# Publicar a aplicação
RUN dotnet publish IdebAPI.csproj \
    -c Release \
    -o /app/publish

# Imagem final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Configurar porta e ambiente
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

# Copiar arquivos publicados
COPY --from=build /app/publish .

# Ponto de entrada
ENTRYPOINT ["dotnet", "IdebAPI.dll"]