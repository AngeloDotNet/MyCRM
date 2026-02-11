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

Storage
- Local: files salvati su disco (wwwroot/uploads)
- S3/MinIO: se abilitato via config (AWS S3 SDK, compatibile MinIO)

Admin seed (dev)
- Admin user creato in fase di seed:
  - email: admin@local
  - password: Admin123!

```