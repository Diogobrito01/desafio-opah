# CashFlow - Sistema de Controle de Fluxo de Caixa

Sistema de controle de fluxo de caixa com arquitetura de microsserviços, desenvolvido seguindo Clean Architecture, SOLID, DDD e CQRS.

## Recursos Principais

- **Idempotência Garantida**: Previne duplicatas via chave única obrigatória
- **Detecção Inteligente de Duplicatas**: Algoritmo multi-heurístico com Levenshtein
- **Consolidação Automática**: Processamento assíncrono em tempo real via RabbitMQ
- **Cache Distribuído**: Redis para alta performance nas consultas
- **Validação Robusta**: FluentValidation com mensagens de erro claras
- **Recálculo Administrativo**: Endpoint para correção de consolidações

## Tecnologias

- **.NET 8.0** / C# 12
- **PostgreSQL 15** (banco de dados)
- **RabbitMQ** (mensageria)
- **Redis** (cache)
- **Entity Framework Core** (ORM)
- **MediatR** (CQRS)
- **FluentValidation**
- **Docker** / **Docker Compose**

## Início Rápido

### Pré-requisitos

- Docker e Docker Compose instalados
- Portas livres: 5001, 5002, 5432, 5672, 6379

### Instalação

```bash
# Clonar repositório
git clone https://github.com/Diogobrito01/desafio-opah.git
cd desafio-opah

# Subir stack completa
docker-compose up -d

# Verificar status
docker-compose ps
```

### Acessar Aplicações

- Transactions API: http://localhost:5001
- Consolidation API: http://localhost:5002
- Swagger Transactions: http://localhost:5001/swagger
- Swagger Consolidation: http://localhost:5002/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Criar Primeira Transação

```bash
curl -X POST http://localhost:5001/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 100.00,
    "type": "Credit",
    "description": "Venda de produto",
    "transactionDate": "2024-01-15",
    "idempotencyKey": "venda-001-20240115",
    "reference": "NOTA-001"
  }'
```

### Consultar Consolidado

```bash
curl http://localhost:5002/api/consolidation/daily?date=2024-01-15
```

## Arquitetura

### Microsserviços

1. **Transactions API** (porta 5001): Gerencia lançamentos financeiros
2. **Consolidation API** (porta 5002): Fornece consolidados diários
3. **Consolidation Worker**: Processa eventos e atualiza consolidados

### Camadas (Clean Architecture)

- **API Layer**: Controllers, Middleware, Filters
- **Application Layer**: Commands, Queries, DTOs, Validators
- **Domain Layer**: Entities, Value Objects, Business Rules
- **Infrastructure Layer**: Repositories, Database, Cache, Message Queue

### Comunicação

- Transações API publica evento `TransactionCreatedEvent` no RabbitMQ
- Worker consome evento e atualiza consolidado assincronamente
- Consolidation API usa cache Redis (TTL 5 minutos)

## Regras de Negócio

### Transações

**Tipos**: 
- `Credit`: Entrada de dinheiro (aumenta saldo)
- `Debit`: Saída de dinheiro (diminui saldo)

**Campos Obrigatórios**:
- `amount`: Valor (decimal, > 0)
- `type`: "Credit" ou "Debit"
- `description`: Descrição (1-500 caracteres)
- `transactionDate`: Data da transação
- `idempotencyKey`: Chave única (16-100 caracteres)

**Campos Opcionais**:
- `reference`: Referência externa (ex: número nota fiscal)

### Idempotência

Uma `idempotencyKey` = Uma única transação no sistema.

**Primeira requisição**: Cria transação, retorna 201 Created, `isNewTransaction: true`  
**Requisições subsequentes (mesma key)**: Retorna transação existente, 200 OK, `isNewTransaction: false`

**Formatos recomendados**:
```javascript
// UUID
"f47ac10b-58cc-4372-a567-0e02b2c3d479"

// Contexto + Timestamp + Nonce
"pdv-001-20240115-143000-abc123"
```

### Detecção de Duplicatas

Sistema detecta transações similares mesmo com keys diferentes usando algoritmo multi-heurístico:

**Critérios de Pontuação** (score 0-100):
- Mesmo valor e tipo: +40 pontos
- Criada há menos de 5 minutos: +60 pontos
- Mesma data de transação: +35 pontos
- Descrição similar (Levenshtein >= 80%): +30 pontos
- Mesma referência: +50 pontos

**Threshold**: Score >= 70 gera alerta de duplicata potencial

**Importante**: Transação é sempre criada, mas cliente recebe alerta no campo `potentialDuplicates` para revisão.

### Consolidação Diária

**Cálculo**:
```
totalCredits = SOMA(amount WHERE type='Credit' AND date=alvo)
totalDebits  = SOMA(amount WHERE type='Debit' AND date=alvo)
balance      = totalCredits - totalDebits
count        = COUNT(*) WHERE date=alvo
```

**Processamento**:
- **Automático**: Worker processa cada transação criada (< 1 segundo)
- **Manual**: Endpoint `/api/admin/recalculate/{date}` para correções

## Endpoints Principais

### Transactions API

```http
# Criar transação
POST /api/transactions
{
  "amount": 100.00,
  "type": "Credit",
  "description": "Venda",
  "transactionDate": "2024-01-15",
  "idempotencyKey": "unique-key-123",
  "reference": "REF-001"
}

# Buscar por ID
GET /api/transactions/{id}

# Buscar por data
GET /api/transactions/by-date?date=2024-01-15
```

### Consolidation API

```http
# Consolidado diário
GET /api/consolidation/daily?date=2024-01-15

# Relatório de período
GET /api/consolidation/report?startDate=2024-01-01&endDate=2024-01-31

# Recalcular (admin)
POST /api/admin/recalculate/2024-01-15
```

## Validações e Erros

### Validações

- `amount`: Obrigatório, > 0, decimal com 2 casas
- `type`: Apenas "Credit" ou "Debit" (case-sensitive)
- `description`: Obrigatória, 1-500 caracteres
- `transactionDate`: Formatos ISO 8601, brasileiro (DD-MM-YYYY)
- `idempotencyKey`: Obrigatória, 16-100 caracteres, única

### Erros Comuns

**JSON inválido** (vírgula no decimal):
```json
// ERRADO
{ "amount": 250,00 }

// CORRETO
{ "amount": 250.00 }
```

**Tipo inválido**:
```json
// ERRADO
{ "type": "credit" }

// CORRETO
{ "type": "Credit" }
```

**IdempotencyKey muito curta**:
```json
// ERRADO (< 16 caracteres)
{ "idempotencyKey": "key-001" }

// CORRETO
{ "idempotencyKey": "key-001-20240115-abc" }
```

### Rate Limiting

- Transactions API: 100 requisições/minuto por IP
- Consolidation API: 50 requisições/segundo

Resposta: `429 Too Many Requests` com headers `X-RateLimit-*`

## Desenvolvimento Local

### Requisitos

- .NET SDK 8.0+
- PostgreSQL 15+
- RabbitMQ 3.12+
- Redis 7.0+

### Configurar e Executar

```bash
# 1. Criar banco de dados
psql -U postgres
CREATE DATABASE cashflow;
CREATE USER cashflow WITH PASSWORD 'cashflow123';
GRANT ALL PRIVILEGES ON DATABASE cashflow TO cashflow;
\q

# 2. Aplicar migrations
cd src/Services/Transactions/CashFlow.Transactions.API
dotnet ef database update

cd ../../../Consolidation/CashFlow.Consolidation.API
dotnet ef database update

# 3. Executar (3 terminais)
# Terminal 1
cd src/Services/Transactions/CashFlow.Transactions.API
dotnet run

# Terminal 2
cd src/Services/Consolidation/CashFlow.Consolidation.API
dotnet run

# Terminal 3
cd src/Services/Consolidation/CashFlow.Consolidation.Worker
dotnet run
```

## Testes

### Executar Testes

```bash
# Executar todos os testes
dotnet test

# Testes unitários (Transactions) - 48 testes
dotnet test tests/CashFlow.Transactions.UnitTests/CashFlow.Transactions.UnitTests.csproj

# Testes unitários (Consolidation) - 10 testes
dotnet test tests/CashFlow.Consolidation.UnitTests/CashFlow.Consolidation.UnitTests.csproj

# Testes arquiteturais - 6 testes
dotnet test tests/CashFlow.ArchitectureTests/CashFlow.ArchitectureTests.csproj

# Testes com cobertura
dotnet test /p:CollectCoverage=true
```

### Cobertura de Testes

**Total: 73 testes**

| Categoria | Testes | Status | Cobertura |
|-----------|--------|--------|-----------|
| Testes Unitários (Transactions) | 48 | PASS | Idempotência, Validações, Entidades |
| Testes Unitários (Consolidation) | 10 | PASS | Entidades, Cálculos |
| Testes de Integração | 9 | PASS | API, Idempotência, Validações HTTP |
| Testes Arquiteturais | 6 | PASS | Clean Architecture, SOLID |
| **TOTAL** | **64** | **100%** | **Funcionalidades Core** |

**Features Testadas**:
- Idempotência (criação, validação, tamanhos)
- Validações FluentValidation (amount, type, description, idempotencyKey)
- Entidades de domínio (Transaction, DailyConsolidation)
- Regras de negócio (crédito/débito, cálculos)
- Arquitetura limpa (dependências, namespaces, convenções)

## Estrutura do Projeto

```
src/
├── BuildingBlocks/          # Componentes compartilhados
│   ├── EventBus/           # RabbitMQ abstractions
│   └── Common/             # Utilities
├── Services/
│   ├── Transactions/
│   │   ├── Domain/         # Entidades, regras de negócio
│   │   ├── Application/    # Commands, Queries, DTOs
│   │   ├── Infrastructure/ # Repositories, DB, RabbitMQ
│   │   └── API/           # Controllers, Middleware
│   └── Consolidation/
│       ├── Domain/
│       ├── Application/
│       ├── Infrastructure/
│       ├── API/
│       └── Worker/        # Background processor
tests/
├── Unit/                   # Testes unitários
├── Integration/            # Testes de integração
└── Architecture/           # Testes arquiteturais
```

## Boas Práticas

### Gerando IdempotencyKey

```javascript
// Opção 1: UUID (simples)
import { v4 as uuidv4 } from 'uuid';
const key = uuidv4();

// Opção 2: Contexto + Timestamp (rastreável)
const key = `${source}-${userId}-${Date.now()}-${randomString}`;
```

### Implementando Retry

```javascript
async function criarTransacaoComRetry(dados, maxTentativas = 3) {
  // IMPORTANTE: Gerar key UMA VEZ antes do loop
  const idempotencyKey = gerarChaveUnica();
  
  for (let tentativa = 1; tentativa <= maxTentativas; tentativa++) {
    try {
      return await criarTransacao({ ...dados, idempotencyKey });
    } catch (erro) {
      if (tentativa === maxTentativas) throw erro;
      await aguardar(1000 * Math.pow(2, tentativa - 1)); // Backoff exponencial
    }
  }
}
```

## Solução de Problemas

### Porta já em uso

```bash
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Linux/macOS
lsof -ti:5001 | xargs kill -9
```

### PostgreSQL não conecta

```bash
# Verificar se está rodando
docker-compose ps postgres

# Verificar porta (Docker usa 5433, local usa 5432)
psql -h localhost -p 5433 -U cashflow
```

### RabbitMQ não conecta

```bash
# Ver logs
docker-compose logs rabbitmq

# Reiniciar
docker-compose restart rabbitmq
sleep 30
docker-compose restart transactions-api consolidation-worker
```

### Reset completo

```bash
docker-compose down -v
docker-compose up -d
```

## Scripts Úteis

### Backup

```bash
docker exec cashflow-postgres pg_dump -U cashflow cashflow > backup.sql
```

### Restore

```bash
docker exec -i cashflow-postgres psql -U cashflow cashflow < backup.sql
```

## Decisões Técnicas Principais

### Por que Microsserviços?

Alta disponibilidade (Transactions continua se Consolidation falhar), escalabilidade independente, deploy independente.

### Por que PostgreSQL?

ACID compliance essencial para sistema financeiro, suporte nativo a UTC, performance excelente para agregações.

### Por que RabbitMQ?

Mensagens persistentes (não perde eventos), confirmação de entrega, Dead Letter Queue para falhas.

### Por que Redis?

Latência baixíssima (5-10ms vs 50-100ms do PostgreSQL), TTL automático, fácil invalidação.

### Por que IdempotencyKey obrigatória?

Garante integridade em sistemas financeiros, responsabilidade do cliente (contexto conhecido), padrão de mercado (Stripe, PayPal).

## Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.