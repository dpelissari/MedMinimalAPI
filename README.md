# PiraREMUME (API)

PiraREMUME é a consulta pública da Relação Municipal de Medicamentos Essenciais de Piracicaba. REMUME é o elenco da cidade: os medicamentos que a Prefeitura se compromete a oferecer na rede pública.

O projeto não inventa essa lista. Organiza o que já está no site da Prefeitura para ficar fácil de buscar. Dá para saber se o medicamento faz parte da REMUME, se sai em toda a rede ou só em algumas farmácias, e onde fica a unidade do posto. Não diz se tem o item na prateleira hoje. Isso só a farmácia confirma. Farmácia particular (Drogal, Drogaria São Paulo e outras) não entra.

Este repositório é o **backend**. Guarda o catálogo, responde as buscas e cuida do login admin. O site que a pessoa usa está em [PiraREMUME (client)](../MedClient).

Fontes oficiais:

- [Página da REMUME](https://piracicaba.sp.gov.br/servicos/relacao-municipal-de-medicamentos-essenciais-remume/)
- [Farmácias municipais](https://piracicaba.sp.gov.br/servicos/farmacias-municipais/)

## O que esta API faz

Na subida, aplica as migrations e carrega medicamentos e farmácias a partir dos JSON em `Data/`.

A consulta pública lista medicamentos (por nome ou grupo farmacológico) e farmácias municipais (atenção básica, alto custo e CEDIC). Cada medicamento traz as apresentações e se elas saem em toda a rede ou só em unidades específicas.

Um job olha as páginas da Prefeitura de tempos em tempos. Se o PDF ou a lista de farmácias mudar, a API tenta ler de novo, gravar os JSON e atualizar o banco. Se a leitura vier incompleta, o catálogo fica como está. Quem acompanha isso é a tela de sincronismo no client.

Stack: ASP.NET Core 10 (Minimal APIs), EF Core, PostgreSQL, FluentValidation, JWT.

## Autenticação JWT

A consulta que o site mostra **não pede login**. `GET /medicamentos` e `GET /farmacias` são públicos. O client chama isso na home, no detalhe do medicamento e na lista de farmácias sem token.

O JWT é só para a área admin do client (`/login` e `/admin/sincronismo`). Um usuário com perfil Admin autentica em `POST /login` e recebe um token. O client guarda esse token e envia no header `Authorization: Bearer ...`. Com ele dá para:

- ver e alterar as URLs vigiadas (página da REMUME e página das farmácias)
- disparar uma verificação na hora
- cadastrar outro usuário em `POST /registrar`

Quem só consulta o catálogo no site não usa JWT.

## Endpoints

O client usa estes caminhos. A documentação interativa fica no Scalar, em Development.

Consulta pública (o site chama sem token):

| Método | Caminho | Onde o client usa |
| --- | --- | --- |
| GET | `/medicamentos` | Home. Query: `termo`, `tipo` (`nome` ou `grupo`), `pagina`, `tamanho` |
| GET | `/medicamentos/grupos` | Filtro por grupo na consulta |
| GET | `/medicamentos/{id}` | Página do medicamento |
| GET | `/farmacias` | Lista de farmácias. Query: `termo`, `tipo` (`AtencaoBasica`, `AltoCusto`, `Cedic`) |
| GET | `/farmacias/{slug}` | Página da farmácia |

Autenticação:

| Método | Caminho | Quem pode | Onde o client usa |
| --- | --- | --- | --- |
| POST | `/login` | Público | `/login` |
| POST | `/registrar` | Só Admin | Não tem tela. Fica para a API |

Sincronismo (JWT de Admin):

| Método | Caminho | Onde o client usa |
| --- | --- | --- |
| GET | `/admin/sincronismo` | `/admin/sincronismo` |
| PUT | `/admin/sincronismo` | Salvar URLs, intervalo (1, 7 ou 30 dias) e se a verificação está ligada |
| POST | `/admin/sincronismo/verificar` | Botão Verificar agora |

## Como rodar

PostgreSQL local. A connection string fica em `appsettings.Development.json`.

```bash
dotnet ef database update
dotnet run
```

Sobe em `http://localhost:5272`. O client espera esse endereço. Depois: [subir o site](../MedClient#como-rodar).
