[![CI](https://github.com/MatheusMunduruca/ecommerce-api/actions/workflows/ci.yml/badge.svg)](https://github.com/MatheusMunduruca/ecommerce-api/actions/workflows/ci.yml)
# 🧪 Empório do Rudolf — E-Commerce API

API REST de e-commerce com temática de alquimia, construída em **C# .NET 8**. É o backend da loja do alquimista **Rudolf**, que compartilha login e economia de ouro com a [Taverna do Gregor](https://github.com/MatheusMunduruca/todo-api).

![CI](https://github.com/MatheusMunduruca/ecommerce-api/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-blue?logo=csharp)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-purple)
![MySQL](https://img.shields.io/badge/MySQL-8.0-orange?logo=mysql)
![JWT](https://img.shields.io/badge/Auth-JWT%20compartilhado-green)
![Tests](https://img.shields.io/badge/Tests-33%20passing-brightgreen?logo=checkmarx)
![Swagger](https://img.shields.io/badge/Docs-Swagger-green?logo=swagger)

---

## 🌌 O universo compartilhado

Este projeto faz parte de um **ecossistema de duas aplicações que dividem usuários e economia**:

```
                ┌──────────────────────────┐
                │   Mesma tabela de Users   │
                │   Mesma chave JWT         │
                │   (banco todo_db)         │
                └────────────┬─────────────┘
                             │
         ┌───────────────────┴────────────────────┐
         │                                         │
┌────────────────────┐                  ┌─────────────────────┐
│  Taverna do Gregor  │                  │  Empório do Rudolf  │
│  (todo-api)         │                  │  (este projeto)     │
│  Porta 5000         │                  │  Porta 5252         │
│  → ganha ouro       │                  │  → gasta ouro       │
│    cumprindo tarefas│                  │    comprando itens  │
└────────────────────┘                  └─────────────────────┘
```

- **Login único:** uma conta criada em qualquer um dos dois sistemas funciona no outro (mesma chave JWT + mesma tabela de usuários).
- **Economia única:** o saldo de **Gold Coins** é persistido no banco e compartilhado. O que você ganha na Taverna, gasta no Empório.
- Ao criar uma conta, o usuário recebe **1.000 de ouro** de boas-vindas.

Para isso, a API usa **dois DbContexts**:
- `AppDbContext` → banco `ecommerce_db` (produtos, categorias, carrinho, pedidos)
- `SharedDbContext` → banco `todo_db` (usuários e saldo de ouro, compartilhado com o todo-api)

---

## 🚀 Funcionalidades

- ✅ Autenticação JWT **compatível com o todo-api** (token gerado em um vale no outro)
- ✅ Saldo de ouro compartilhado (consultar / deduzir / creditar)
- ✅ CRUD de produtos e categorias com filtros (categoria, faixa de preço)
- ✅ Carrinho de compras por usuário
- ✅ Criação de pedidos com controle de estoque
- ✅ **Estoque randomizado a cada 6 horas** — funciona mesmo se a API ficou offline
- ✅ **Sistema de descontos** com probabilidades por categoria
- ✅ Documentação interativa via Swagger/OpenAPI

---

## 🛠️ Tech Stack

| Categoria | Tecnologia |
|-----------|-----------|
| **Framework** | .NET 8 / ASP.NET Core Web API |
| **ORM** | Entity Framework Core 8 + Pomelo MySQL Driver |
| **Banco de Dados** | MySQL 8.0 (`ecommerce_db` + `todo_db` compartilhado) |
| **Autenticação** | JWT Bearer (chave compartilhada) + BCrypt |
| **Mapeamento** | AutoMapper (Model → DTO) |
| **Background jobs** | `IHostedService` (randomização periódica do estoque) |
| **Documentação** | Swagger / OpenAPI (Swashbuckle) |
| **Container** | Docker Compose (MySQL) |

---

## ⏳ Estoque randomizado a cada 6 horas

O estoque e os descontos de Rudolf são re-sorteados a cada **janela de 6 horas**.

A randomização **não depende da API estar ligada o tempo todo**: a "janela" é calculada a partir do relógio real (`segundos desde 1970 ÷ 21600`). O banco guarda qual janela já foi sorteada (`StockStates`). Se a API ficou fechada durante a virada e volta depois, a primeira requisição a `/products` detecta que a janela mudou e re-sorteia tudo. Enquanto a API roda, um `IHostedService` também verifica a cada 10 minutos.

### Faixas de estoque por categoria

| Categoria | Estoque |
|---|---|
| Poções | 0 – 25 |
| Ingredientes | 0 – 50 |
| Grimórios | 0 – 10 |
| Equipamentos | 0 – 5 |

### Sistema de descontos

Cada item tem uma chance independente de receber desconto (não se limita a um por categoria):

| Categoria | Chance de desconto |
|---|---|
| Poções | 7% |
| Ingredientes | 12% |
| Grimórios | 3% |
| Equipamentos | 1% |

Quando um desconto é aplicado, seu valor segue a distribuição:

| Desconto | Probabilidade |
|---|---|
| 5% | 70% |
| 10% | 25% |
| 25% | 5% |

O preço final com desconto é refletido no carrinho e no total do pedido.

---

## 📡 Endpoints

### Auth (compartilhado com a Taverna)
| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `POST` | `/api/auth/register` | Cria usuário, concede 1.000 de ouro, retorna JWT | ❌ |
| `POST` | `/api/auth/login` | Autentica, retorna JWT + saldo | ❌ |
| `GET`  | `/api/auth/gold` | Saldo de ouro do usuário | ✅ |
| `POST` | `/api/auth/gold/deduct` | Deduz ouro (compra) | ✅ |

### Products
| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `GET` | `/api/products` | Lista produtos (filtros: `category`, `minPrice`, `maxPrice`) | ❌ |
| `GET` | `/api/products/{id}` | Detalhe do produto | ❌ |
| `POST` | `/api/products` | Cria produto | ✅ |
| `PUT` | `/api/products/{id}` | Atualiza produto | ✅ |
| `DELETE` | `/api/products/{id}` | Remove produto | ✅ |
| `POST` | `/api/products/refresh-stock` | Renova o estoque na hora (somente contas `@adm`) | ✅ |

### Categories / Cart / Orders
| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `GET` | `/api/categories` | Lista categorias | ❌ |
| `GET` | `/api/cart` | Carrinho do usuário | ✅ |
| `POST` | `/api/cart/items` | Adiciona item ao carrinho | ✅ |
| `DELETE` | `/api/cart/items/{productId}` | Remove item | ✅ |
| `POST` | `/api/orders` | Cria pedido a partir do carrinho | ✅ |

---

## 📁 Estrutura

```
ecommerce-api/
├── src/ECommerceApi/
│   ├── Controllers/        # Auth, Products, Categories, Cart, Order
│   ├── Data/
│   │   ├── AppDbContext.cs        # ecommerce_db (produtos, carrinho, pedidos)
│   │   ├── SharedDbContext.cs     # todo_db (usuários + ouro, compartilhado)
│   │   └── AppDbContextFactory.cs # factory design-time para migrations
│   ├── Models/             # Product, Category, Cart, Order, Payment, User, StockState…
│   ├── DTOs/Dtos.cs
│   ├── Mappings/MappingProfile.cs
│   ├── Services/
│   │   ├── TokenService.cs               # geração de JWT
│   │   ├── RudolfStockService.cs         # randomização de estoque/descontos (6h)
│   │   └── StockRandomizerHostedService.cs # verificação periódica em background
│   ├── Migrations/
│   ├── Program.cs
│   └── appsettings.Example.json   # modelo — copie para appsettings.json
├── tests/
│   ├── ECommerceApi.UnitTests/         # xUnit + Moq + FluentAssertions
│   ├── ECommerceApi.IntegrationTests/  # WebApplicationFactory + Bogus
│   └── ECommerceApi.BddTests/          # SpecFlow (Gherkin)
├── .github/workflows/ci.yml           # build + test a cada push/PR
└── docker-compose.yml
```

---

## ⚙️ Como rodar localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- MySQL 8.0 (local ou via Docker)
- A [Taverna do Gregor (todo-api)](https://github.com/MatheusMunduruca/todo-api) compartilha o banco `todo_db` — rode as migrations dela também.

### 1. Clone
```bash
git clone https://github.com/MatheusMunduruca/ecommerce-api.git
cd ecommerce-api
```

### 2. Configure as credenciais
Copie o modelo e preencha com seus valores:
```bash
cp src/ECommerceApi/appsettings.Example.json src/ECommerceApi/appsettings.json
```
> ⚠️ A `Jwt:Key` **deve ser idêntica** à do todo-api, senão o login não será compartilhado.

### 3. Aplique as migrations
```bash
cd src/ECommerceApi
dotnet ef database update
```

### 4. Rode a API
```bash
dotnet run
```

A API sobe em:
- **HTTP:** `http://localhost:5252`
- **Swagger:** `http://localhost:5252/swagger`

---

## 🔐 Autenticação compartilhada

Como o todo-api e este projeto usam a **mesma chave JWT** e a **mesma tabela de usuários**, um token obtido em qualquer um deles é aceito no outro. É isso que permite o login único e o saldo de ouro compartilhado entre a Taverna e o Empório.

---

## 🧪 Testes

**33 testes automatizados** em três níveis, executados a cada push pelo GitHub Actions.

```bash
dotnet test
```

| Projeto | Qtd | Stack | Cobre |
|---|---|---|---|
| `ECommerceApi.UnitTests` | 15 | xUnit · Moq · FluentAssertions | Preço final com desconto, geração de JWT (TokenService), faixas de estoque e descontos do `RudolfStockService` (EF InMemory) |
| `ECommerceApi.IntegrationTests` | 14 | WebApplicationFactory · Bogus · FluentAssertions | Fluxo HTTP real: registro/login, catálogo, carrinho com desconto, checkout debitando estoque, controle de estoque, refresh admin (403/200) |
| `ECommerceApi.BddTests` | 4 | SpecFlow (Gherkin) | Cenários de checkout e de reposição de estoque em linguagem de negócio |

Os testes usam **banco InMemory** e configuração injetada — não precisam de MySQL nem de `appsettings.json`, então rodam de forma isolada e determinística (inclusive no CI).

---

## 🗺️ Roadmap

- [x] Modelagem (produtos, categorias, carrinho, pedidos, pagamentos)
- [x] Autenticação JWT compartilhada + economia de ouro
- [x] Estoque randomizado a cada 6h + sistema de descontos
- [x] Suíte de testes Unitários (xUnit + Moq + FluentAssertions)
- [x] Testes de Integração (WebApplicationFactory + Bogus)
- [x] Testes BDD (SpecFlow)
- [x] GitHub Actions CI

---

## 👨‍💻 Autor

**Matheus Munduruca**

[![GitHub](https://img.shields.io/badge/GitHub-MatheusMunduruca-black?logo=github)](https://github.com/MatheusMunduruca)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-matheusmunduruca-blue?logo=linkedin)](linkedin.com/in/matheusmunduruca)

---

## 📄 Licença

Este projeto está sob a licença MIT.
