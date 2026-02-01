# BankMore.TransferService API

A API **BankMore.TransferService** é um serviço de transferências bancárias entre contas da mesma instituição, desenvolvido em **.NET 8**. O projeto utiliza **Domain-Driven Design (DDD)**, **CQRS** com **MediatR** e implementa o padrão **Saga** para garantir consistência transacional distribuída com compensação automática.

## 🚀 Tecnologias e Padrões
- **.NET 8** - Core da aplicação
- **DDD (Domain-Driven Design)** - Organização em camadas (Domain, Application, Infrastructure, API)
- **CQRS & MediatR** - Separação clara entre comandos de escrita e consultas
- **Saga Pattern** - Orquestração de transações distribuídas com compensação
- **JWT Authentication** - Segurança via tokens Bearer (integrado com Account Service)
- **FluentValidation** - Validação de entrada de dados
- **Dapper** - Micro-ORM de alta performance
- **DbUp** - Migrations versionadas para SQLite
- **SQLite** - Persistência relacional (pronto para Postgres/SQL Server)
- **Serilog** - Logs estruturados com correlationId
- **Swagger/OpenAPI 3.0** - Documentação interativa da API
- **Docker** - Containerização multi-stage para produção

## ✨ Funcionalidades

### Transações Distribuídas
- 🔄 **Saga Pattern**: Orquestração de débito → crédito → compensação
- 🔁 **Idempotência**: Evita duplicação de transferências via `requestId`
- ⚡ **Compensação Automática**: Retry 3x com backoff exponencial (1s, 2s, 4s)
- 🛡️ **Resiliência**: Timeout de 30s e tratamento de falhas críticas
- 📊 **Rastreabilidade**: Logs estruturados com correlationId

### Segurança
- 🔐 Autenticação JWT (mesma chave do Account Service)
- 🔒 Autorização por token em todos os endpoints
- 🚫 Não armazena dados sensíveis (CPF, número de conta)
- ✅ Validação de origem via claim `sub` do token

### Arquitetura
- 🏛️ DDD com separação de responsabilidades
- 🔄 CQRS para escalabilidade
- 📦 Microsserviço independente que consome Account Service
- 🐳 Docker multi-stage para builds otimizados

## 🛠️ Instalação e Execução

### Execução Local

1. **Clonagem e Dependências**:
   ```bash
   git clone https://github.com/pedrobono/BankMore.Transferencia.git
   cd BankMore.Transferencia/src/BankMore.TransferService
   dotnet restore
   ```

2. **Configurar appsettings.json**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=transfers.db"
     },
     "Jwt": {
       "Secret": "sua-chave-secreta-super-segura-com-pelo-menos-32-caracteres",
       "Issuer": "BankMore",
       "Audience": "BankMore"
     },
     "AccountService": {
       "BaseUrl": "http://localhost:8081",
       "TimeoutSeconds": 30
     }
   }
   ```

3. **Executar**:
   ```bash
   dotnet run
   ```

Acesse: `http://localhost:8082/swagger`

### Execução com Docker

1. **Build da imagem**:
   ```bash
   cd src/BankMore.TransferService
   docker build -t bankmore-transfer-service:latest .
   ```

2. **Executar container**:
   ```bash
   docker run -d -p 8082:8080 \
     -e Jwt__Secret="sua-chave-secreta-super-segura-com-pelo-menos-32-caracteres" \
     -e AccountService__BaseUrl="http://account-service:8080" \
     -e ConnectionStrings__DefaultConnection="Data Source=/app/data/transfers.db" \
     -v $(pwd)/data:/app/data \
     --name transfer-service \
     bankmore-transfer-service:latest
   ```

Acesse: `http://localhost:8082/swagger`

### Docker Compose (com Account Service)

```bash
docker-compose up -d
```

Serviços disponíveis:
- Transfer Service: `http://localhost:8082/swagger`
- Account Service: `http://localhost:8081/swagger`

## 📍 Endpoints da API

### 💸 Transferências (`/transfers`)

#### POST `/transfers` 🔒
Efetua transferência entre contas da mesma instituição.

**Headers:** `Authorization: Bearer <token>`

**Request Body:**
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "destinationAccountNumber": "85381-6",
  "value": 150.75
}
```

**Campos:**
- `requestId`: UUID para idempotência (obrigatório)
- `destinationAccountNumber`: Número da conta de destino (obrigatório)
- `value`: Valor da transferência (deve ser > 0)

**Response (204):** No Content

**Fluxo da Saga:**
1. ✅ Valida idempotência (requestId + originAccountId)
2. ✅ Débito na conta de origem (via Account Service)
3. ✅ Crédito na conta de destino (via Account Service)
4. ⚠️ Se crédito falhar: Compensação automática (crédito na origem)
5. ✅ Persistência do registro de transferência

**Validações:**
- ✅ Token JWT válido e não expirado
- ✅ Valor deve ser positivo (> 0)
- ✅ RequestId não pode estar vazio
- ✅ Conta de origem extraída do token (claim `sub`)
- ✅ Idempotente: mesmo `requestId` não duplica operação

**Erros:**
- `400 BAD REQUEST`:
  - `INVALID_VALUE`: Valor inválido (≤ 0)
  - `INVALID_ACCOUNT`: Conta não encontrada (propagado do Account Service)
  - `INACTIVE_ACCOUNT`: Conta inativa (propagado do Account Service)
  - `INSUFFICIENT_BALANCE`: Saldo insuficiente (propagado do Account Service)
  - `VALIDATION_ERROR`: Dados de entrada inválidos
- `403 FORBIDDEN`: Token inválido ou expirado
- `500 INTERNAL SERVER ERROR`: Falha crítica na compensação (COMPENSATION_ERROR)

## 🔄 Fluxo da Saga Detalhado

### Cenário 1: Transferência Bem-Sucedida
```
1. Cliente → POST /transfers
2. Transfer Service → Valida idempotência
3. Transfer Service → POST /movements (débito origem)
4. Account Service → Débito realizado ✅
5. Transfer Service → POST /movements (crédito destino)
6. Account Service → Crédito realizado ✅
7. Transfer Service → Persiste status SUCCESS
8. Transfer Service → Retorna 204 ao cliente
```

### Cenário 2: Falha no Débito
```
1. Cliente → POST /transfers
2. Transfer Service → POST /movements (débito origem)
3. Account Service → Erro (ex: saldo insuficiente) ❌
4. Transfer Service → Persiste status FAILED
5. Transfer Service → Retorna 400 ao cliente
```

### Cenário 3: Falha no Crédito (com Compensação)
```
1. Cliente → POST /transfers
2. Transfer Service → POST /movements (débito origem)
3. Account Service → Débito realizado ✅
4. Transfer Service → POST /movements (crédito destino)
5. Account Service → Erro (ex: conta inativa) ❌
6. Transfer Service → Inicia compensação
7. Transfer Service → POST /movements (crédito origem) - Tentativa 1
8. Account Service → Compensação realizada ✅
9. Transfer Service → Persiste status COMPENSATED
10. Transfer Service → Retorna 400 ao cliente (com erro original)
```

### Cenário 4: Falha Crítica na Compensação
```
1-5. (igual ao cenário 3)
6. Transfer Service → Inicia compensação
7. Transfer Service → POST /movements (crédito origem) - Tentativa 1 ❌
8. Transfer Service → Aguarda 1s e tenta novamente - Tentativa 2 ❌
9. Transfer Service → Aguarda 2s e tenta novamente - Tentativa 3 ❌
10. Transfer Service → Persiste status COMPENSATION_FAILED
11. Transfer Service → Loga alerta CRÍTICO
12. Transfer Service → Retorna 500 ao cliente
```

## 🛡️ Tratamento de Erros Padronizado

Todas as respostas de falha seguem o mesmo padrão do Account Service:

### Códigos HTTP
- **400 (Bad Request)**: Erros de validação ou regras de negócio
- **403 (Forbidden)**: Token ausente, inválido ou expirado
- **500 (Internal Server Error)**: Falha crítica na compensação

### Formato de Erro
```json
{
  "message": "Descrição amigável do erro",
  "failureType": "TIPO_DO_ERRO"
}
```

### Tipos de Falha (failureType)
| Tipo | Descrição | HTTP |
|------|-------------|------|
| `INVALID_VALUE` | Valor inválido (≤ 0) | 400 |
| `INVALID_ACCOUNT` | Conta não encontrada | 400 |
| `INACTIVE_ACCOUNT` | Conta inativa | 400 |
| `INSUFFICIENT_BALANCE` | Saldo insuficiente | 400 |
| `VALIDATION_ERROR` | Dados de entrada inválidos | 400 |
| `UNAUTHORIZED` | Token inválido/expirado | 403 |
| `COMPENSATION_ERROR` | Falha crítica na compensação | 500 |
| `ACCOUNT_SERVICE_ERROR` | Erro ao comunicar com Account Service | 400 |
| `ACCOUNT_SERVICE_UNAVAILABLE` | Account Service indisponível | 400 |
| `ACCOUNT_SERVICE_TIMEOUT` | Timeout ao chamar Account Service | 400 |

## 🔍 Idempotência

A API é **totalmente idempotente**. Se repetir o mesmo `requestId` para a mesma conta de origem:

| Status da Transferência | Comportamento |
|-------------------------|---------------|
| `Success` | Retorna **204** (sem reprocessar) |
| `Failed` | Retorna **400** com erro original |
| `Compensated` | Retorna **400** com erro original |
| `CompensationFailed` | Retorna **500** |

**Exemplo:**
```bash
# Primeira chamada
POST /transfers { "requestId": "abc-123", ... }
→ 204 No Content (transferência realizada)

# Segunda chamada (mesmo requestId)
POST /transfers { "requestId": "abc-123", ... }
→ 204 No Content (não reprocessa, retorna sucesso)
```

## 🗄️ Database

### Migrations com DbUp

O projeto usa **DbUp** para migrations versionadas e automáticas.

**Localização:** `Infrastructure/Data/Migrations/`

**Adicionar nova migration:**
```bash
# 1. Criar arquivo SQL
002_AddNewColumn.sql

# 2. Escrever SQL
ALTER TABLE transfers ADD COLUMN new_field TEXT;

# 3. Pronto! Será executado automaticamente no próximo start
```

### Schema

**Tabela `transferencia`:**
```sql
CREATE TABLE transferencia (
    idtransferencia TEXT(37) PRIMARY KEY,
    idcontacorrente_origem TEXT(37) NOT NULL,
    idcontacorrente_destino TEXT(37) NOT NULL,
    datamovimento TEXT(25) NOT NULL,
    valor REAL NOT NULL
);
```

**Tabela `idempotencia`:**
```sql
CREATE TABLE idempotencia (
    chave_idempotencia TEXT(37) PRIMARY KEY,
    requisicao TEXT(1000),
    resultado TEXT(1000)
);
```

**Idempotência:**
- Chave: `{idContaOrigem}:{requestId}`
- Armazena requisição e resultado para evitar duplicação

**Tabela de controle DbUp:**
```sql
-- Criada automaticamente pelo DbUp
CREATE TABLE SchemaVersions (
    Id INTEGER PRIMARY KEY,
    ScriptName TEXT NOT NULL,
    Applied DATETIME NOT NULL
);
```

## 📊 Logs Estruturados

O projeto usa **Serilog** para logs estruturados com correlationId.

**Exemplo de log de transferência bem-sucedida:**
```
[INF] Recebida requisição de transferência. RequestId: 550e8400-..., Origin: 123e4567-...
[INF] Iniciando transferência. RequestId: 550e8400-..., Origin: 123e4567-..., Destination: 85381-6, Value: 150.75
[INF] Etapa 1: Debitando conta de origem. RequestId: 550e8400-...
[INF] Chamando Account Service - POST /movements. RequestId: 550e8400-..., Type: D
[INF] Movimento criado com sucesso. RequestId: 550e8400-...
[INF] Etapa 2: Creditando conta de destino. RequestId: 550e8400-...
[INF] Chamando Account Service - POST /movements. RequestId: 550e8400-..., Type: C
[INF] Movimento criado com sucesso. RequestId: 550e8400-...
[INF] Transferência concluída com sucesso. RequestId: 550e8400-...
```

**Exemplo de log de compensação:**
```
[WRN] Falha na transferência. RequestId: 550e8400-..., Error: Conta de destino inativa
[WRN] Iniciando compensação. RequestId: 550e8400-...
[INF] Tentativa de compensação 1/3. RequestId: 550e8400-...
[INF] Compensação bem-sucedida. RequestId: 550e8400-...
```

**Exemplo de log de falha crítica:**
```
[CRT] COMPENSAÇÃO FALHOU após 3 tentativas! RequestId: 550e8400-.... INTERVENÇÃO MANUAL NECESSÁRIA!
```

## 🛡️ Resiliência

### Timeouts
- **Chamadas HTTP ao Account Service**: 30 segundos (configurável)
- **Publicação Kafka** (opcional): 5 segundos

### Retry Policy
- **Aplicado apenas na compensação**
- **Tentativas**: 3
- **Backoff exponencial**: 1s, 2s, 4s
- **Condições**: Qualquer erro na compensação

### Circuit Breaker (Futuro)
- Pode ser adicionado com **Polly**
- Threshold: 5 falhas consecutivas
- Duração: 60 segundos

## 📝 Variáveis de Ambiente

| Variável | Descrição | Padrão | Obrigatório |
|----------|-----------|--------|-------------|
| `ConnectionStrings__DefaultConnection` | Connection string SQLite | `Data Source=transfers.db` | ✅ |
| `Jwt__Secret` | Chave secreta JWT (mesma do Account Service) | - | ✅ |
| `Jwt__Issuer` | Emissor do token | `BankMore` | ✅ |
| `Jwt__Audience` | Audiência do token | `BankMore` | ✅ |
| `AccountService__BaseUrl` | URL do Account Service | `http://localhost:8081` | ✅ |
| `AccountService__TimeoutSeconds` | Timeout HTTP | `30` | ❌ |

## 🤝 Integração com Account Service

O Transfer Service **depende** do Account Service para todas as operações.

### Endpoints Consumidos

#### 1. POST `/api/Conta/resolve` (Resolver ID da Conta)
```json
{
  "numeroConta": "70110-0"
}
```
**Response:**
```json
{
  "contaId": "ca960f46-ef11-4846-abfa-e2a98cbdd263",
  "numeroConta": "70110-0"
}
```
- Usado para obter o ID da conta pelo número

#### 2. POST `/api/Movimento` (Débito)
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "contaId": null,
  "valor": 150.75,
  "tipo": "D"
}
```
- Usado para debitar a conta de origem
- `contaId` null = usa conta do token

#### 3. POST `/api/Movimento` (Crédito)
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "contaId": "ca960f46-ef11-4846-abfa-e2a98cbdd263",
  "valor": 150.75,
  "tipo": "C"
}
```
- Usado para creditar a conta de destino
- `contaId` obrigatório (ID resolvido)

#### 4. POST `/api/Movimento` (Compensação)
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000-COMP",
  "contaId": null,
  "valor": 150.75,
  "tipo": "C"
}
```
- Usado para compensar (estornar) em caso de falha
- `requestId` com sufixo `-COMP`
- `contaId` null = credita na conta do token

### Autenticação
- Todas as chamadas repassam o token JWT do cliente
- Header: `Authorization: Bearer <token>`

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Executar com cobertura
dotnet test /p:CollectCoverage=true
```

### Cobertura Planejada
- ✅ Testes unitários de handlers
- ✅ Testes de validação (FluentValidation)
- ✅ Testes de idempotência
- ✅ Testes de compensação com retry
- ✅ Testes de integração com Account Service mockado

## 🤝 Contribuição

1. Fork o projeto.
2. Crie sua Feature Branch (`git checkout -b feature/NovaFeature`).
3. Commit suas mudanças (`git commit -m 'feat: Descrição da feature'`).
4. Push para a Branch (`git push origin feature/NovaFeature`).
5. Abra um Pull Request.

## ⚖️ Licença

Este projeto está sob a licença **MIT**.

---

## 👨‍💻 Autor

**Pedro Bono**

* [GitHub](https://github.com/pedrobono)
* [LinkedIn](https://www.linkedin.com/in/pedro-h-bono/)
