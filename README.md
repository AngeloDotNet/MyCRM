```markdown
# MyCrm - Skeleton

Scopo: scaffold backend di esempio per un CRM generico
- ASP.NET Core API (.NET 8)
- ASP.NET Core Identity + JWT + Refresh Tokens (rotation)
- SQL Server (container)
- Storage: Local disk + MinIO (S3 API via AWS SDK)
- Docker / docker-compose per sviluppo
- Sync endpoint di base per offline/mobile (delta-based)
- Seed admin user per sviluppo

Prerequisiti
- Docker & Docker Compose
- .NET 8 SDK (per sviluppo locale se non in container)
- (Per mobile) Visual Studio con workload .NET MAUI / emulatori

Avvio rapido (Docker)
1. Copia .env.example in .env e modifica se necessario.
2. Esegui:
   docker compose up --build

3. Esegui migration & seed (dalla cartella Api, se non usi container):
   dotnet ef database update --project Api --startup-project Api

Endpoint principali (dev)

- API root: http://localhost:5000 (quando il container è esposto)
- Swagger: http://localhost:5000/swagger
- Auth:
  - POST /api/v1/auth/register
  - POST /api/v1/auth/login
  - POST /api/v1/auth/refresh
  - POST /api/v1/auth/revoke
- Contacts CRUD: /api/v1/contacts
- Sync: POST /api/v1/sync

Comandi utili (local development)
- Build:
  dotnet build

- Run (local, project Api):
  dotnet run --project Api

- EF Core migrations (create):
  dotnet ef migrations add AddRefreshTokenAudit -p Api -s Api

- EF Core apply migrations:
  dotnet ef database update -p Api -s Api

- Eseguire i test (unit + integration):
  dotnet test

Note su refresh tokens e sicurezza
- Rotazione dei refresh token: quando il client richiede `/auth/refresh` con un refresh token valido il token viene marcato come revocato e viene emesso un nuovo refresh token (rotate).
- Rilevamento reuse: se il server riceve un refresh token già revocato (possibile furto/riuso) il server revoca tutte le sessioni (tutti i refresh tokens) dell'utente — ciò forza il logout su tutti i client e richiede nuovo login.
- Audit: ogni RefreshToken memorizza IP e UserAgent (quando disponibili) per investigazioni.

Storage
- Local: files salvati su disco (wwwroot/uploads)
- S3/MinIO: se abilitato via config (AWS S3 SDK, compatibile MinIO)

Setup CI (GitHub Actions)
- Workflow presente in `.github/workflows/ci.yml`.
- Esegue:
  - dotnet restore
  - dotnet build
  - dotnet test (esegue unit e integration tests)

Esempio di flusso client per login/refresh (semplice)
1. POST /api/v1/auth/login { email, password } -> ricevi { access_token, refresh_token }
2. Usa access_token come Bearer per chiamate protette.
3. Quando access token scade, chiama POST /api/v1/auth/refresh { email, refreshToken } -> ottieni nuovo pair.
4. Se ricevi 401 su refresh, il token è invalido/riutilizzato e devi re-login.

Admin seed (dev)
- Admin user creato in fase di seed:
  - email: admin@local
  - password: Admin123!

Esecuzione dei test
- Eseguire tutti i test (unit + integration):
  dotnet test

- Nota: i test di integrazione usano WebApplicationFactory e un database InMemory (non toccano il container SQL).

Migrazione EF Core consigliata (esempio)
- Per aggiungere una migration con i nuovi campi a RefreshToken:
  dotnet ef migrations add AddRefreshTokenAudit -p Api -s Api

- Per applicare le migration al DB (dev oppure container SQL):
  dotnet ef database update -p Api -s Api

Prossimi passi suggeriti
- Generare Blazor WASM Hosted client (AuthenticationStateProvider + login UI)
- Generare .NET MAUI Blazor Hybrid project che riusa RCL + SQLite + background sync
- Estendere test di integrazione per scenari di sicurezza e per il sync endpoint

```