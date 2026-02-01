# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj e restaurar dependências
COPY ["BankMore.TransferService.csproj", "./"]
RUN dotnet restore "BankMore.TransferService.csproj"

# Copiar código fonte e buildar
COPY . .
RUN dotnet build "BankMore.TransferService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BankMore.TransferService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Criar diretório para o banco de dados
RUN mkdir -p /app/data

ENTRYPOINT ["dotnet", "BankMore.TransferService.dll"]
