# Todo API

API de tarefas (ToDo) feita em .NET 8 com EF Core e SQLite, com uma tela simples pra usar a API pelo navegador.

Stack: ASP.NET Core Web API, EF Core 8, SQLite, Swagger, xUnit + Moq + FluentAssertions, Docker.

## Rodando

Com Docker:

```bash
docker compose up --build
```

Tela em http://localhost:8080 e Swagger em http://localhost:8080/swagger. O banco fica no volume `todo-data` (`docker compose down -v` apaga).

Sem Docker (precisa do SDK do .NET 8):

```bash
dotnet run --project src/TodoApi
```

Tela em http://localhost:5000 e Swagger em http://localhost:5000/swagger.

Não precisa rodar nada de banco: as migrations são aplicadas na inicialização e o `todo.db` é criado na primeira execução.

Testes:

```bash
dotnet test
```

## Como o projeto foi desenvolvido

Segue a ordem em que fui construindo, que também é a ordem mais fácil pra ler o código.

### 1. Modelo da tarefa

Comecei pela entidade `TodoItem` (`Domain/`): id, título, descrição, status, data de vencimento e as datas de criação/atualização. O status é um enum `TodoStatus` com `Pendente`, `EmAndamento` e `Concluido`.

### 2. Banco de dados com EF Core

Com o modelo pronto, criei o `AppDbContext` (`Data/`) e configurei a tabela pela Fluent API:

- título obrigatório com até 200 caracteres, descrição até 2000;
- status salvo como texto (`"EmAndamento"` em vez de `1`), fica mais legível no banco;
- índice em `Status` e `DueDate`, que são as colunas usadas nos filtros;
- conversão pra devolver `CreatedAt`/`UpdatedAt` como UTC, porque o SQLite não guarda o `Kind` do `DateTime`.

Escolhi SQLite pra quem for avaliar conseguir rodar sem instalar banco nenhum. Depois gerei a migration `InitialCreate`, e o `Program.cs` chama `Database.Migrate()` ao subir. Pra trocar por SQL Server ou Postgres basta mudar o provider e a connection string (`appsettings.json` ou variável `ConnectionStrings__DefaultConnection`).

### 3. Repository e Unit of Work

Em cima do DbContext fiz a camada de acesso a dados (`Repositories/`):

- `IRepository<T>` / `Repository<T>`: CRUD genérico (buscar por id, listar, adicionar, atualizar, remover);
- `ITodoRepository` / `TodoRepository`: herda o genérico e acrescenta `GetFilteredAsync`, que monta a query com os filtros opcionais e ordena por vencimento. As leituras usam `AsNoTracking`.

O `UnitOfWork` (`Data/`) expõe o repositório e concentra o `SaveChangesAsync`. Assim o repositório só prepara as alterações e quem confirma é o service, numa única chamada.

### 4. Regras de negócio (Service)

O `TodoService` (`Services/`) fica entre o controller e os dados e depende só de interfaces:

- valida que `dueDateFrom` não seja maior que `dueDateTo` (senão lança `ArgumentException`, que vira 400);
- se `dueDateTo` vier sem hora (ex. `2026-12-31`), considera o dia inteiro;
- preenche `CreatedAt` e `UpdatedAt` e tira espaços do título/descrição;
- converte a entidade em DTO antes de devolver.

A data atual vem de um `TimeProvider` injetado em vez de `DateTime.Now`, pra conseguir testar as datas.

### 5. DTOs e validação

Separei os objetos de entrada e saída da entidade (`Dtos/`): `CreateTodoRequest`, `UpdateTodoRequest`, `UpdateTodoStatusRequest`, `TodoFilter` e `TodoResponse`. Assim a API não expõe o modelo do banco e o cliente não consegue mexer em campos como `createdAt`. A validação é por DataAnnotations (`[Required]`, `[StringLength]`) com mensagens em português, e o `[ApiController]` devolve 400 sozinho quando algo está inválido.

### 6. Controller e endpoints

O `TodosController` só recebe a requisição, chama o service e traduz o resultado em status HTTP:

| Método | Rota | O que faz | Respostas |
|---|---|---|---|
| GET | `/api/todos` | lista, com filtros opcionais | 200, 400 |
| GET | `/api/todos/{id}` | busca por id | 200, 404 |
| POST | `/api/todos` | cria | 201, 400 |
| PUT | `/api/todos/{id}` | atualiza tudo | 200, 400, 404 |
| PATCH | `/api/todos/{id}/status` | muda só o status | 200, 400, 404 |
| DELETE | `/api/todos/{id}` | remove | 204, 404 |

Filtros da listagem (todos opcionais e combináveis): `status`, `dueDateFrom` e `dueDateTo`. Exemplo:

```bash
curl -X POST http://localhost:8080/api/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"Estudar EF Core","status":"Pendente","dueDate":"2026-12-31T18:00:00"}'

curl "http://localhost:8080/api/todos?status=Pendente&dueDateFrom=2026-12-01&dueDateTo=2026-12-31"

curl -X PATCH http://localhost:8080/api/todos/1/status \
  -H "Content-Type: application/json" \
  -d '{"status":"Concluido"}'
```

### 7. Tratamento de erros

Pra não espalhar try/catch, criei o `GlobalExceptionHandler` (`Middleware/`), registrado com `AddExceptionHandler`. `ArgumentException` vira 400 com a mensagem; qualquer outra exceção vira 500 e é logada. Todos os erros (inclusive o 404 do controller) saem no formato ProblemDetails.

### 8. Injeção de dependência e Swagger

Tudo é registrado no `Program.cs` com o container nativo: DbContext, UnitOfWork e Service como *scoped* (um por requisição) e o `TimeProvider` como singleton. O enum é serializado como texto no JSON.

O Swagger usa os comentários XML dos DTOs e do controller, então os campos aparecem com descrição e exemplo.

### 9. Testes

Com a API funcionando, escrevi os testes (`tests/TodoApi.Tests`), 31 no total:

- `Services/`: regras do service com o UnitOfWork mockado (Moq);
- `Controllers/`: se cada ação devolve o status HTTP certo;
- `Repositories/`: repositório e UnitOfWork com SQLite em memória. Usei o mesmo banco da aplicação, em vez do InMemory do EF, pra pegar query que não traduz pra SQL;
- `Integration/`: sobe a API inteira com `WebApplicationFactory` e testa o fluxo completo, as validações e o Swagger.

Cobertura (ignora as migrations):

```bash
dotnet test --settings tests/TodoApi.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"
```

### 10. Docker e CI

O `Dockerfile` é multi-stage: compila com a imagem do SDK e roda só com a do runtime, como usuário não-root. O `docker-compose.yml` sobe a API na porta 8080 com um volume pro SQLite. No GitHub Actions (`.github/workflows/ci.yml`) cada push compila, roda os testes e builda a imagem.

### 11. Front

Por último fiz uma tela simples em `src/TodoApi/wwwroot` (HTML, CSS e JS puro, sem build), servida pela própria API com `UseStaticFiles`. Ela usa os mesmos endpoints: dá pra criar, editar, mudar status, excluir e filtrar, e os erros de validação da API aparecem na tela.

## Estrutura

```
src/TodoApi
  Domain/        entidade e enum
  Data/          DbContext, UnitOfWork, migrations
  Repositories/  repositório genérico e de tarefas
  Services/      regras de negócio
  Dtos/          entrada e saída da API
  Controllers/   endpoints
  Middleware/    tratamento de erros
  wwwroot/       front
  Program.cs     DI, EF, Swagger e pipeline
tests/TodoApi.Tests
```
