# KPSGroup — Ticket Platform

University group project. Event/concert ticket-buying platform with a 3-tier .NET architecture, React SPA frontend, and PostgreSQL.

## Stack
- **Frontend:** React 18 + Vite + TypeScript (SPA, port 5173)
- **Backend:** ASP.NET Core 10 Web API (controllers, port 5050)
- **Database:** PostgreSQL 16 (Docker, port 5432)
- **ORM:** EF Core 10 (Npgsql) — used for writes/CRUD
- **Data Mapper:** Dapper — used for event search (Data Mapper requirement)
- **Auth:** JWT (stateless)

## One-time setup
Requires: .NET 10 SDK, Node 20+, Docker Desktop.

```bash
# 1. Start PostgreSQL (port 5432)
docker compose up -d

# 2. Restore + apply EF migrations
cd backend
dotnet restore
dotnet ef database update --project TicketPlatform.Data --startup-project TicketPlatform.Api

# 3. Install frontend deps
cd ../frontend
npm install
```

## Running (two terminals)
```bash
# terminal 1 — backend (http://localhost:5050)
cd backend/TicketPlatform.Api
dotnet run

# terminal 2 — frontend (http://localhost:5173)
cd frontend
npm run dev
```

Open <http://localhost:5173>. Register a user — **the first registered user is automatically promoted to Admin**, subsequent users are Customers.

## Manual demo of every quality requirement

| Requirement | How to demo |
|---|---|
| Multi-tab same session | Sign in once, open the app in two tabs. Both work independently (JWT in localStorage, no server session). |
| SQL-injection safe | Try `'; DROP TABLE Users; --` in the events search box — server logs the parameterised SQL, no damage. |
| ORM + Data Mapper | Event list (`/`) uses Dapper; event detail and admin CRUD use EF Core. |
| Single-request transaction | Watch any controller — `SaveChangesAsync` runs inside the HTTP request, never spans user interaction. |
| Optimistic locking | Open the same event in two admin tabs. Edit + save in tab A → succeeds. Edit + save in tab B → "Someone else edited" dialog with **Reload** / **Overwrite** buttons. |
| Memory management | All services Scoped, no static state, no SessionScoped (see Program.cs). |
| Async / responsive | Place an order — the response returns immediately; the "email confirmation" runs 2s later in the background (visible in API logs as `[email-sent]`). |
| Cross-cutting interceptor | Tail API logs — every controller action emits `[audit]` lines with user/roles/method. Toggle off with `BusinessLogic:AuditLogging: false` in `appsettings.json` (no recompile). |
| Strategy extensibility | Set an event's pricing strategy to "Early bird" — price drops 20% if event is >30 days out. Add a new strategy by writing a class implementing `IPricingStrategy` and registering it in `Program.cs`. |
| Decorator extensibility | The mock payment processor is wrapped by `LoggingPaymentProcessorDecorator` — toggle with `Payments:EnableLoggingDecorator` in `appsettings.json`. |

## Useful commands
```bash
# Stop + remove all docker state
docker compose down -v

# Reset DB without rebuilding container
docker exec kps_postgres psql -U ticket_user -d ticketplatform -c 'TRUNCATE "Users","Events","TicketCategories","Orders","OrderItems" RESTART IDENTITY CASCADE;'

# Add a new migration
dotnet ef migrations add MyChange --project TicketPlatform.Data --startup-project TicketPlatform.Api

# Run backend tests / type check
cd backend && dotnet build
cd frontend && npm run build
```

## Project layout
```
backend/
  TicketPlatform.Api/          ← Presentation tier (Controllers, JWT, Filters, Program.cs)
  TicketPlatform.Business/     ← Business Logic tier (Services, Pricing strategies, Payment decorator, background email)
  TicketPlatform.Data/         ← Data Access tier (DbContext, Entities, Dapper repo, EF migrations)
frontend/
  src/
    pages/                     ← Login, Register, EventsList, EventDetail, MyOrders, AdminEvents, AdminEventEdit
    auth/                      ← AuthContext (JWT in localStorage)
    api/                       ← fetch wrapper with ConcurrencyConflictError handling
docker-compose.yml             ← Postgres 16
TECHNICAL_REPORT.md            ← 2-page report with file:line citations for grading
```
