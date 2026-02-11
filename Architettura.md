# Architettura generale (high level)

## Backend (host):
- ASP.NET Core Web App (MVC) per pagine server-side amministrative/gestionali e per ospitare servizi di autenticazione se vuoi usare hosting unificato.
- ASP.NET Core Web API (rest) per tutte le operazioni del CRM (entità: contatti, aziende, opportunità, attività, note, ecc.).
- ASP.NET Core Identity (persisted con EF Core + SQL Server/Postgres) per gestione utenti, ruoli e claim.
- Auth server / OIDC layer: puoi usare direttamente Identity con JWT (Auth endpoints custom) oppure usare un provider OIDC (Duende/IdentityServer o Azure AD B2C) per flussi OIDC/OAuth2.

## Frontend (web):
- Blazor WebAssembly (hosted) come SPA che consuma le API tramite token (Authorization header).
- Componenti UI in Razor Class Libraries (RCL) per riuso tra Web e MAUI.

## Mobile:
- .NET MAUI Blazor (Blazor Hybrid) o MAUI + WebView che ospita componenti Blazor condivisi.
- Per funzionalità native (camera, notifiche, geolocalizzazione) usare Dependency Injection / servizi platform-specific.

## Real-time / Notifications:
- SignalR (server + client Blazor/MAUI) per notifiche in tempo reale, aggiornamenti dashboard, chat interne.

## Persistenza e pattern:
- EF Core + repository/unit-of-work o CQRS con MediatR per separare comandi e query.
- Automapper / DTO per mapping API.

## Infrastruttura:
- Docker, orchestrazione (Kubernetes) o App Service, CI/CD (GitHub Actions/Azure DevOps).
- Telemetria: Serilog, Application Insights.

## Autenticazione & Autorizzazione (scelta critica)
- Raccomandazione: Authorization Code + PKCE (secure) per Blazor WASM. Non usare implicit flow.

### Opzioni:
- Identity con JWT + refresh tokens (self-hosted): semplice da implementare; implementa rotazione/revoca dei refresh token.
- Identity + Duende IdentityServer (full OIDC): raccomandato per scenari aziendali, SSO e client multipli (web, mobile, API). (Nota: Duende richiede licenza per uso commerciale).
- Azure AD / Azure AD B2C / Auth0: se vuoi managed identity/SSO esterno.

### Best practices:
- Usare HTTPS sempre; proteggere refresh token (store sicuro); usare refresh token rotation; mettere scadenze sensate sui JWT.
- Ruoli e policy claims-based authorization su API.

## Integrazione Blazor WASM e MAUI Hybrid

### Condivisione codice:
- Metti componenti UI e logica condivisibile in Razor Class Libraries (RCL) e librerie .NET Standard/.NET 7+.
- Blazor WASM userà gli stessi componenti; MAUI Blazor Hybrid può hostare le stesse componenti attraverso BlazorWebView.

### Autenticazione su MAUI:
- Se usi OIDC, MAUI può usare sistem-specific browser (system browser) con PKCE.
- Per token storage su mobile: usare SecureStorage (MAUI Essentials) per proteggere token.

### Limitazioni:
- Alcune API web-only (es. session cookie) non sono adeguate per WASM/mobile — preferire token.
- Blazor WASM e MAUI Hybrid possono condividere UI ma per UX mobile potresti creare componenti specifici.

## Sincronizzazione / Offline (mobile)

### Se il mobile deve funzionare offline:
- Client locale: SQLite + ORM (e.g. EF Core for SQLite) o Realm/Couchbase Mobile.
- Sincronizzazione: implementa endpoint di sync (delta/timestamp), risoluzione conflitti (strategia: last-write-wins, merge manuale).
- Gestione file/attachments: upload in background, CDN/S3 storage.

### API design e qualità
- Versioning (v1, v2…) e documentazione (Swagger / OpenAPI).
- Validazione (FluentValidation), mapping (AutoMapper).
- Logging/Tracing, rate-limiting, CORS, health checks.
- Test: unit, integration, API tests (Postman/Newman, Playwright per UI).

### Librerie e tool consigliati
- Backend: ASP.NET Core 7/8, EF Core, Microsoft.AspNetCore.Identity, Swashbuckle, MediatR, FluentValidation, AutoMapper, Serilog.
- Auth: Duende IdentityServer (if needed), Microsoft.Identity.Web (Azure), or custom JWT + refresh token implementation.
- Frontend: Blazor WebAssembly (hosted), RCLs, MudBlazor / Radzen / Telerik / Syncfusion (UI libs).
- Mobile: .NET MAUI (Blazor Hybrid) + SecureStorage, Essentials APIs.
- Real-time: SignalR.
- DevOps: Docker, GitHub Actions, Terraform (infra as code) se serve.

## Struttura di soluzione consigliata (esempio)

### src/
- MyCrm.sln
- MyCrm.Api (ASP.NET Core Web API + Identity endpoints)
- MyCrm.Web (ASP.NET Core MVC app se serve interfaccia server-side)
- MyCrm.Blazor.Client (Blazor WebAssembly Hosted client)
- MyCrm.Shared (DTOs, models, enums)
- MyCrm.Core (domain, interfaces, services, MediatR)
- MyCrm.Infrastructure (EF Core DbContext, repositories, migrations)
- MyCrm.RCL (shared Razor components)
- MyCrm.Maui (MAUI app hosting Razor components)

### tests/
- MyCrm.UnitTests
- MyCrm.IntegrationTests

## Piano di implementazione minimo (MVP)
- Giorno 1–3: Scaffold solution (hosted Blazor template), aggiungi Identity + EF Core, initial data model (Contact, Account).
- Giorno 4–7: Implementare API CRUD per Contact/Account, Swagger, CORS.
- Giorno 8–10: Autenticazione (register/login + JWT/refresh), protezione API.
- Giorno 11–14: Blazor client: login, token handling, pagine list/detail, componenti RCL.
- Giorno 15–20: MAUI Blazor: integra RCL, login mobile, secure storage tokens.
- A seguire: SignalR, notifications, offline sync, background jobs, multi-tenancy, RBAC, audit trail.

## Rischi e scelte da prendere ora (da definire)
- Vuoi OIDC/SSO (Azure/IdentityServer) o una soluzione JWT interna?
- Offline mobile necessario?
- Multi-tenant o single-tenant?
- Budget/licensing per UI libraries o Duende?

## Vuoi che parta con uno scheletro/progetto di esempio? Posso:
- Generare uno scheletro di soluzione (Blazor Hosted + ASP.NET Core API + ASP.NET Core Identity) con login JWT e un modello Contact CRUD.
- Mostrare il flusso di autenticazione (server-side endpoints + Blazor WASM AuthenticationStateProvider).
- Creare un esempio di progetto MAUI che riusa i componenti RCL e memorizza token in SecureStorage.

---

Spiegazione:

Spiegazione componenti principali

- Backend API: espone tutte le entità del CRM (Contact, Account, Opportunity, Activity, Note, Attachment) tramite RESTful endpoints versionati (es. /api/v1/...).
- Identity: ASP.NET Core Identity con supporto JWT + refresh tokens oppure OIDC (Duende/Azure AD) a seconda della scelta. Gestisce utenti, ruoli, claim e amministrazione utenti.
- Blazor WASM (hosted): SPA che consuma API, gestisce autenticazione via Authorization header. Componenti UI condivisi in RCL.
- MAUI Blazor Hybrid: riusa componenti RCL per logica e UI; usa SecureStorage per token e system browser per flussi OIDC/PKCE.
- SignalR: notifiche in tempo reale (assegnazioni, aggiornamenti opportunità, chat interna).
- Storage esterno: per attachments (S3/Azure Blob) con URL firmati per download/upload.
- Background worker: per job asincroni (sync, notifiche email, processamenti batch).
- DB relazionale: modello normalizzato per CRM; possibili estensioni per multitenancy.