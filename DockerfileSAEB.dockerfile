# Estágio de build (compilação)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copia o arquivo .csproj primeiro para otimizar o cache do Docker
# O caminho é relativo ao WORKDIR atual (/src), e IdebAPI.csproj está na raiz do WORKDIR
COPY ["IdebAPI.csproj", "./"] 
RUN dotnet restore "IdebAPI.csproj"
# Copia o restante do código
COPY . .
# O WORKDIR já é /src, onde o .csproj está
# Certifique-se de que o nome do arquivo .csproj está correto aqui
RUN dotnet publish "IdebAPI.csproj" -c Release -o /app/publish

# Estágio final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Copia os arquivos publicados do estágio de build
COPY --from=build /app/publish .
# Define o comando de entrada para rodar a aplicação .NET
# Certifique-se de que o nome da DLL principal está correto aqui
ENTRYPOINT ["dotnet", "IdebAPI.dll"]