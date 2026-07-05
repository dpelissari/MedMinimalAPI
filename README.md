# MedMinimalAPI

API REST em **.NET** para cadastro e consulta de medicamentos e fabricantes, com autenticação JWT. Consumida pelo frontend [MedClient](../MedClient).

## O que faz hoje

- CRUD de medicamentos e fabricantes
- Registro e login de usuários com JWT
- Endpoints `GET` públicos para consulta
- `POST`, `PUT` e `DELETE` exigem autenticação

## Tecnologias

- .NET 10
- Minimal API
- Entity Framework Core + PostgreSQL
- FluentValidation
- JWT Bearer
- Scalar (documentação em desenvolvimento)

## Como rodar

**1. Banco de dados**

Configure a connection string em `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=medicamentos_db;Username=postgres;Password=SUA_SENHA"
}
