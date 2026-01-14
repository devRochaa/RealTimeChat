# Imagem base para build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia csproj e restaura dependências
COPY *.csproj .
RUN dotnet restore

# Copia o resto do código
COPY . .
RUN dotnet publish -c Release -o out

# 2️⃣ Imagem final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copia build publicado
COPY --from=build /app/out .

# Expõe a porta usada pelo app
EXPOSE 5000

# Inicia a aplicação
ENTRYPOINT ["dotnet", "WebSocket.dll"]