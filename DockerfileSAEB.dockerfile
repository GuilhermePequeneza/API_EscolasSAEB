# Estágio de build (compilação)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IdebAPI.csproj", "./"] 
RUN dotnet restore "IdebAPI.csproj"
COPY . .
# Remove a linha WORKDIR se ela não for necessária, pois já estamos em /src
# Se o .csproj estiver na raiz de /src, então o comando publish abaixo funcionará

# Mude esta linha:
# RUN dotnet publish "IdebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false
# Para esta:
RUN dotnet publish "IdebAPI.csproj" -c Release -o /app/publish --no-build -r linux-x64 --self-contained false /p:UseAppHost=false

# Estágio final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdebAPI.dll"]