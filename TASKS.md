# Project Backlog — KPSGroup Ticket Platform

This document is the source of truth for the project's Trello board. Each entry below is a single Trello card.

## Suggested Trello list (column) structure

```
Backlog   →   In Progress   →   In Review   →   Done
```

(Optional swimlanes / labels: `infra`, `backend`, `frontend`, `docs`.)

## How to use this document

1. Create a Trello board called **KPSGroup Ticket Platform** with the four lists above.
2. For each task below, create a card in **Done** with:
   - Title = the heading (e.g. `TICK-005: Domain entities`)
   - Description = everything under that heading
   - Label = the `Track` field
   - Checklist = the `Acceptance criteria` block
3. Fill in `Assignee` based on who actually picked up which card.
4. Date fields are left blank — set "Created" / "Completed" based on the order of work; a sensible spread for a ~2-week sprint is one card every half-day, with frontend cards clustered toward the end.

## Dependency graph (read top-to-bottom)

```
Phase 1 — Infrastructure
  TICK-001 → TICK-002 → TICK-003 → TICK-004

Phase 2 — Data layer
  TICK-005 → TICK-006 → TICK-007

Phase 3 — Business logic
  TICK-008
  TICK-009 (needs 006)
  TICK-010 (Strategy pattern)
  TICK-011 (Decorator + background)
                                  ↘
Phase 4 — API layer                 TICK-014 (needs 008-011)
  TICK-012 (Auth)
  TICK-013 (Events) ← needs 009
  TICK-014 (Orders) ← needs 010, 011
  TICK-015 (Audit interceptor + DI wiring)

Phase 5 — Frontend
  TICK-016 (foundation) → TICK-017 → TICK-018 → TICK-019 → TICK-020

Phase 6 — Docs & QA
  TICK-021, TICK-022
```

---

# Phase 1 — Infrastructure

## TICK-001: Initialise repository and ignore rules
**Track:** infra
**Estimate:** 0.5h
**Assignee:** _TBD_

**Description:** Create the project root, initialise git, add a `.gitignore` covering .NET, Node, IDE, and OS noise. This is the foundation every other card builds on.

**Files added:**
- `.gitignore`

**Acceptance criteria:**
- [ ] `git status` from project root works and ignores `bin/`, `obj/`, `node_modules/`, `dist/`, `.DS_Store`, IDE folders.

---

## TICK-002: Add Docker Compose for PostgreSQL
**Track:** infra
**Estimate:** 0.5h
**Assignee:** _TBD_

**Description:** Run Postgres 16 in Docker so the team doesn't need a local install. One database, one user, persistent volume so data survives container restarts.

**Files added:**
- `docker-compose.yml`

**Acceptance criteria:**
- [ ] `docker compose up -d` starts a `kps_postgres` container.
- [ ] Container exposes 5432 on the host.
- [ ] DB `ticketplatform`, user `ticket_user`, password set in compose file.
- [ ] Volume `kps_pgdata` persists across `docker compose down`.

---

## TICK-003: Scaffold .NET solution and 3-tier project structure
**Track:** backend / infra
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Create the solution and three class-library/web-API projects that enforce the 3-tier architecture (Presentation / Business Logic / Data Access). Wire project references so the dependency direction (`Api → Business → Data`) is enforced by the compiler. Add required NuGet packages.

**Files added:**
- `backend/TicketPlatform.sln`
- `backend/TicketPlatform.Api/TicketPlatform.Api.csproj`
- `backend/TicketPlatform.Business/TicketPlatform.Business.csproj`
- `backend/TicketPlatform.Data/TicketPlatform.Data.csproj`
- `backend/dotnet-tools.json` (dotnet-ef as local tool)

**Packages added:**
- Data: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql`, `Dapper`
- Business: `Microsoft.Extensions.Hosting.Abstractions`, `BCrypt.Net-Next`
- Api: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`

**Acceptance criteria:**
- [ ] `dotnet build` from `backend/` produces no errors.
- [ ] `Data` project has zero references to `Business` or `Api`.
- [ ] `Business` references only `Data`.
- [ ] `Api` references `Business` and `Data`.

---

## TICK-004: Scaffold React + Vite + TypeScript frontend with API proxy
**Track:** frontend / infra
**Estimate:** 0.5h
**Assignee:** _TBD_

**Description:** Create the SPA project using Vite. Configure the dev server to proxy `/api` to the backend at `http://localhost:5050`, so the frontend can call the API without CORS issues during development. Pin the backend port in `launchSettings.json`.

**Files added / modified:**
- `frontend/package.json`, `frontend/index.html`, `frontend/tsconfig.json` (scaffolded by `npm create vite`)
- `frontend/vite.config.ts` (port + proxy config)
- `backend/TicketPlatform.Api/Properties/launchSettings.json` (pinned to port 5050, removed HTTPS profile)

**Acceptance criteria:**
- [ ] `npm run dev` from `frontend/` starts Vite on port 5173.
- [ ] `npm run build` produces a `dist/` output.
- [ ] `curl http://localhost:5173/api/...` is proxied to `http://localhost:5050/api/...`.

---

# Phase 2 — Data layer

## TICK-005: Define domain entities
**Track:** backend
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Model the core domain in five POCO entities. Each event has many ticket categories; each order has many order items; orders belong to a user. Concurrency token (`Xmin`) lives on `Event` and `TicketCategory` because those are the rows admins edit and that get reserved during checkout.

**Files added:**
- `backend/TicketPlatform.Data/Entities/User.cs` (+ `UserRole` enum)
- `backend/TicketPlatform.Data/Entities/Event.cs` (+ `PricingStrategyType` enum)
- `backend/TicketPlatform.Data/Entities/TicketCategory.cs`
- `backend/TicketPlatform.Data/Entities/Order.cs` (+ `OrderStatus` enum)
- `backend/TicketPlatform.Data/Entities/OrderItem.cs`

**Acceptance criteria:**
- [ ] All five entities compile.
- [ ] Navigation properties exist (`Event.Categories`, `Order.Items`, `OrderItem.TicketCategory`, etc.).
- [ ] `User.Role` defaults to `Customer`.

---

## TICK-006: Implement AppDbContext with optimistic concurrency mapping
**Track:** backend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** Wire EF Core to PostgreSQL via Npgsql. Configure entity properties (max lengths, decimal precision, unique email index, cascade delete on event → categories). Map PostgreSQL's `xmin` system column as the optimistic concurrency token for `Event` and `TicketCategory`. This is the foundation of the "warn on conflict" requirement.

**Files added:**
- `backend/TicketPlatform.Data/AppDbContext.cs`

**Acceptance criteria:**
- [ ] `DbSet<>` exposed for every entity.
- [ ] Unique index on `User.Email`.
- [ ] `Event.Xmin` and `TicketCategory.Xmin` mapped with `.HasColumnName("xmin").HasColumnType("xid").IsConcurrencyToken()`.

---

## TICK-007: Generate initial EF migration + Dapper search repository
**Track:** backend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** Use `dotnet ef migrations add Initial` to scaffold the schema. Strip the generated `xmin` column-creation lines (those columns already exist in every Postgres row — the migration would fail otherwise). Add a Dapper-based repository for event search/list — this satisfies the "Data Mapper" half of the requirement (EF Core covers the ORM half).

**Files added:**
- `backend/TicketPlatform.Data/Migrations/20260523115624_Initial.cs` (xmin lines manually removed; comment added)
- `backend/TicketPlatform.Data/Migrations/20260523115624_Initial.Designer.cs`
- `backend/TicketPlatform.Data/Migrations/AppDbContextModelSnapshot.cs`
- `backend/TicketPlatform.Data/Repositories/EventSearchRepository.cs` (`IEventSearchRepository`, `EventListItem`, Dapper query with `@q` parameter)

**Acceptance criteria:**
- [ ] `dotnet ef database update` applies cleanly against a fresh Postgres.
- [ ] All tables exist; `\dt` in psql shows them.
- [ ] `IEventSearchRepository.SearchAsync(null, ...)` returns an empty list against a fresh DB.
- [ ] Dapper query uses parameter `@q` (no string concatenation).

---

# Phase 3 — Business logic

## TICK-008: Authentication service with BCrypt and first-user-is-admin rule
**Track:** backend
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Register and authenticate users. Hash passwords with BCrypt. Normalise email to lower-case. The very first registered user is auto-promoted to `Admin` so the demo isn't stuck without one.

**Files added:**
- `backend/TicketPlatform.Business/Services/IAuthService.cs` (+ `AuthResult` record)
- `backend/TicketPlatform.Business/Services/AuthService.cs`

**Acceptance criteria:**
- [ ] Registering twice with the same email throws.
- [ ] Password verification rejects wrong passwords.
- [ ] First-ever user gets `Role = Admin`; second user gets `Customer`.

---

## TICK-009: Event service with optimistic-locking update
**Track:** backend
**Estimate:** 2h
**Assignee:** _TBD_

**Description:** CRUD for events. The Update method takes the `expectedXmin` the client last saw and sets it as `OriginalValue` before `SaveChangesAsync` — so EF's UPDATE includes `WHERE xmin = @expected` and throws `DbUpdateConcurrencyException` if anyone else moved the row. Category list is synced by diff (incoming Ids vs existing). `GetAsync` uses `AsNoTracking()` so a fresh DB read is returned to the conflict handler.

**Files added:**
- `backend/TicketPlatform.Business/Services/IEventService.cs` (+ `EventInput`, `EventInputCategory`)
- `backend/TicketPlatform.Business/Services/EventService.cs`

**Acceptance criteria:**
- [ ] Create + Get + Delete work.
- [ ] Update with a stale `expectedXmin` throws `DbUpdateConcurrencyException`.
- [ ] Update with the correct `expectedXmin` succeeds and bumps the xmin.

---

## TICK-010: Pricing strategies (Strategy design pattern)
**Track:** backend
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Define a pricing-strategy interface and two implementations (Regular = no change; EarlyBird = 20% off when the event is more than 30 days out). A factory resolves the right one based on the event's `PricingStrategy` enum. **Adding a new strategy = one new class + one new DI registration. No edits to existing code.** This satisfies the "Extensibility / Strategy" requirement.

**Files added:**
- `backend/TicketPlatform.Business/Pricing/IPricingStrategy.cs`
- `backend/TicketPlatform.Business/Pricing/RegularPricingStrategy.cs`
- `backend/TicketPlatform.Business/Pricing/EarlyBirdPricingStrategy.cs`
- `backend/TicketPlatform.Business/Pricing/PricingStrategyFactory.cs` (+ `IPricingStrategyFactory`)

**Acceptance criteria:**
- [ ] Calling `RegularPricingStrategy.CalculatePrice(100, event)` returns 100.
- [ ] `EarlyBirdPricingStrategy.CalculatePrice(100, event)` returns 80 when event is >30 days away, 100 otherwise.
- [ ] Factory throws if asked for an unregistered type.

---

## TICK-011: Payment processor with Logging Decorator + Background email queue
**Track:** backend
**Estimate:** 2h
**Assignee:** _TBD_

**Description:** Two extensibility requirements bundled because they're tightly related to the order flow.

1. **Decorator pattern (payment):** `IPaymentProcessor` with a `MockPaymentProcessor`. `LoggingPaymentProcessorDecorator` wraps it — logs START/END around the inner call. Toggleable via `appsettings.json`. Adding e.g. a fraud-check decorator = one new class.
2. **Async / non-blocking (email):** `IEmailQueue` backed by `System.Threading.Channels`. `EmailBackgroundService` is a `BackgroundService` that drains the queue off-thread. Producers `EnqueueAsync` and return immediately; the actual "send" (a 2s simulated delay) happens later — browser is never blocked.

**Files added:**
- `backend/TicketPlatform.Business/Payments/IPaymentProcessor.cs` (+ `PaymentRequest`, `PaymentResult`)
- `backend/TicketPlatform.Business/Payments/MockPaymentProcessor.cs`
- `backend/TicketPlatform.Business/Payments/LoggingPaymentProcessorDecorator.cs`
- `backend/TicketPlatform.Business/Background/IEmailQueue.cs` (+ `EmailJob`, `InMemoryEmailQueue`)
- `backend/TicketPlatform.Business/Background/EmailBackgroundService.cs`

**Acceptance criteria:**
- [ ] `MockPaymentProcessor.ChargeAsync` returns success.
- [ ] When the decorator is wired in DI, the logger emits `Payment.Charge START` / `Payment.Charge END` lines.
- [ ] Enqueuing an email returns immediately; the `[email-sent]` log line appears later from the background service.

---

# Phase 4 — API layer

## TICK-012: JWT auth setup + AuthController
**Track:** backend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** Configure the JWT bearer scheme with issuer/audience/key from `appsettings.json`. Issue tokens on register/login with claims `NameIdentifier`, `Name`, `Email`, `Role`. Stateless — no server-side session, which is what makes "multiple tabs same account" work.

**Files added:**
- `backend/TicketPlatform.Api/Dtos/AuthDtos.cs` (`RegisterRequest`, `LoginRequest`, `AuthResponse`)
- `backend/TicketPlatform.Api/Controllers/AuthController.cs`
- `backend/TicketPlatform.Api/appsettings.json` (Jwt section)

**Acceptance criteria:**
- [ ] `POST /api/auth/register` returns 200 + token for a new email.
- [ ] Re-registering same email returns 409.
- [ ] `POST /api/auth/login` with wrong password returns 401.
- [ ] Issued token includes the user's role claim.

---

## TICK-013: EventsController with 409 concurrency handling
**Track:** backend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** REST endpoints for browsing (anonymous) and managing (admin) events. List uses the Dapper repository (Data Mapper); single-event GET, create, update, delete use `EventService` (EF Core ORM). On `DbUpdateConcurrencyException`, return **409 Conflict** with the current server state, so the React client can show a "Reload / Overwrite" dialog. Includes a DTO mapper that runs the pricing strategy to expose `effectivePrice`.

**Files added:**
- `backend/TicketPlatform.Api/Dtos/EventDtos.cs` (`EventDto`, `TicketCategoryDto`, `EventListItemDto`)
- `backend/TicketPlatform.Api/Controllers/EventsController.cs`

**Acceptance criteria:**
- [ ] `GET /api/events` returns the Dapper-shaped list.
- [ ] `POST /api/events` rejected without `Admin` role.
- [ ] `PUT /api/events/{id}` with stale `Xmin` returns 409 + body including `error: "concurrency_conflict"` and a `current` snapshot.

---

## TICK-014: OrdersController + order placement flow
**Track:** backend
**Estimate:** 2h
**Assignee:** _TBD_

**Description:** `OrderService.PlaceOrderAsync` is where everything comes together: validates seat availability, runs the pricing **strategy** for each category, persists the order, calls the payment processor (wrapped by the **decorator**), and enqueues the confirmation email onto the **background queue**. The controller is thin — extracts user id from JWT claims, returns 200 or 409. Includes `GET /api/orders/mine`.

**Files added:**
- `backend/TicketPlatform.Business/Services/IOrderService.cs` (+ `OrderInput`, `OrderItemInput`)
- `backend/TicketPlatform.Business/Services/OrderService.cs`
- `backend/TicketPlatform.Api/Dtos/OrderDtos.cs` (`PlaceOrderRequest`, `OrderItemRequest`, `OrderResponse`, `OrderItemResponse`)
- `backend/TicketPlatform.Api/Controllers/OrdersController.cs`

**Acceptance criteria:**
- [ ] `POST /api/orders` requires a valid JWT.
- [ ] Placing an order for 2 Standing tickets updates `SoldQuantity += 2`.
- [ ] Payment decorator emits START/END log lines.
- [ ] `[email-sent]` log appears AFTER the HTTP response is returned (proof of async).
- [ ] Over-buying (more tickets than `Total - Sold`) returns 400.

---

## TICK-015: Audit interceptor (toggleable) + Program.cs DI wiring
**Track:** backend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** `BusinessLogicAuditFilter` is an `IAsyncActionFilter` that logs user name, roles, controller.action, and elapsed time before/after every controller action — the cross-cutting / interceptor requirement. The filter is registered globally **only if** `BusinessLogic:AuditLogging` is true in `appsettings.json` — so it can be disabled without recompiling. `Program.cs` wires up everything: DbContext, scoped services, all strategies (registered in the DI collection so the factory can resolve them), the conditional decorator chain for `IPaymentProcessor`, the singleton email queue + hosted service, the conditional audit filter, JWT auth, CORS for the React dev origin.

**Files added:**
- `backend/TicketPlatform.Api/Filters/BusinessLogicAuditFilter.cs`
- `backend/TicketPlatform.Api/Program.cs` (overwrites template default)
- `backend/TicketPlatform.Api/appsettings.json` (`BusinessLogic`, `Payments`, `Cors`, `ConnectionStrings:Postgres` sections)

**Acceptance criteria:**
- [ ] Every API call produces a `[audit]` log line with user / roles / method / elapsedMs.
- [ ] Setting `BusinessLogic:AuditLogging` to `false` and restarting silences audit logs (no code change).
- [ ] Setting `Payments:EnableLoggingDecorator` to `false` removes the payment-logging lines.
- [ ] Solution builds clean with `dotnet build`.

---

# Phase 5 — Frontend

## TICK-016: Auth context, fetch client, and shared types
**Track:** frontend
**Estimate:** 1.5h
**Assignee:** _TBD_

**Description:** The frontend foundation. `api/client.ts` is a tiny `fetch` wrapper that auto-stringifies JSON, sets the bearer token, and throws a typed `ConcurrencyConflictError` on 409 (so pages can render the conflict UX without sniffing strings). `AuthContext` provides `user`, `login`, `register`, `logout` and persists the token in `localStorage` — **each browser tab reads the same auth independently**, which is what makes the multi-tab requirement work.

**Files added:**
- `frontend/src/api/client.ts` (`api()`, `ConcurrencyConflictError`)
- `frontend/src/auth/AuthContext.tsx` (`AuthProvider`, `useAuth`)
- `frontend/src/auth/types.ts` (shared DTO types)

**Acceptance criteria:**
- [ ] `useAuth()` outside `<AuthProvider>` throws a clear error.
- [ ] Token survives a page reload (localStorage round-trip).
- [ ] A 409 response with `error: "concurrency_conflict"` throws `ConcurrencyConflictError` (not a generic error).

---

## TICK-017: Login + Register pages
**Track:** frontend
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Two minimal forms that call the API via `useAuth().login` / `register` and route to `/` on success.

**Files added:**
- `frontend/src/pages/LoginPage.tsx`
- `frontend/src/pages/RegisterPage.tsx`

**Acceptance criteria:**
- [ ] Submitting valid credentials navigates to the events list.
- [ ] Server error message is shown inline (e.g. "Invalid credentials", "Email already registered").
- [ ] Submit button is disabled while a request is in flight.

---

## TICK-018: Events list + event detail (with cart and checkout)
**Track:** frontend
**Estimate:** 2.5h
**Assignee:** _TBD_

**Description:** Public browsing. `EventsListPage` has a debounced search box that hits the Dapper-backed endpoint. `EventDetailPage` shows ticket categories with prices (including any active strategy discount), a per-category quantity input, a running total, and a Buy button that POSTs the order. Includes the async demo — on success the page shows "Order placed, confirmation email is being sent in the background" while the email is still being processed server-side.

**Files added:**
- `frontend/src/pages/EventsListPage.tsx`
- `frontend/src/pages/EventDetailPage.tsx`

**Acceptance criteria:**
- [ ] Search input updates the list with a 200ms debounce.
- [ ] EventDetail shows `effectivePrice` and crosses out the original `basePrice` when discounted.
- [ ] Buying when not logged in redirects to `/login`.
- [ ] After successful purchase the page refreshes the event so updated availability shows.

---

## TICK-019: My orders page + Admin events list
**Track:** frontend
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** `MyOrdersPage` shows the logged-in user's orders via `GET /api/orders/mine`. `AdminEventsPage` shows all events with links to edit + a button to create a new one (admin-only).

**Files added:**
- `frontend/src/pages/MyOrdersPage.tsx`
- `frontend/src/pages/AdminEventsPage.tsx`

**Acceptance criteria:**
- [ ] My orders shows status, total, item breakdown.
- [ ] Admin page renders "Admin only" for non-Admin users.

---

## TICK-020: Admin event edit page with optimistic-locking conflict UX
**Track:** frontend
**Estimate:** 2.5h
**Assignee:** _TBD_

**Description:** The flagship demo page for the optimistic-locking requirement. Loads an event, lets admin edit fields and add/remove ticket categories, and PUTs the changes back. **On 409 conflict, shows a yellow warning with two buttons:** "Reload latest" (replaces edits with server's current data) and "Keep my edits & overwrite" (re-submits using the new server xmin so the next save wins). Also wires up the App shell, router, NavBar, and global styles.

**Files added:**
- `frontend/src/pages/AdminEventEditPage.tsx`
- `frontend/src/App.tsx` (router + NavBar; overwrites Vite template)
- `frontend/src/main.tsx` (wraps `<App>` in `<BrowserRouter>` + `<AuthProvider>`)
- `frontend/src/index.css` (global styles; overwrites Vite template)

**Acceptance criteria:**
- [ ] Opening the same event in two admin tabs, editing both, and saving both — the second save shows the conflict dialog.
- [ ] Clicking "Reload latest" replaces the form with the server's current data and clears the dialog.
- [ ] Clicking "Keep my edits & overwrite" updates the form's xmin and the next save succeeds.
- [ ] Delete button works (with confirm) and navigates back to `/admin`.

---

# Phase 6 — Documentation & QA

## TICK-021: README with setup instructions and demo recipe
**Track:** docs
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** Top-level README so anyone can clone and run in under 5 minutes. Stack, prerequisites, one-time setup, run instructions, demo recipe for each quality requirement, useful commands, project layout.

**Files added:**
- `README.md`

**Acceptance criteria:**
- [ ] A teammate who has never seen the repo can get the app running by following the README.
- [ ] Each quality requirement has a one-line "how to demo" entry.

---

## TICK-022: Technical report with file:line citations
**Track:** docs
**Estimate:** 1h
**Assignee:** _TBD_

**Description:** The 2-page deliverable required by the assignment. 0.5-page architecture summary + a table mapping every quality requirement to the exact `file:line` location in source.

**Files added:**
- `TECHNICAL_REPORT.md`

**Acceptance criteria:**
- [ ] Architecture section ≤ 0.5 page.
- [ ] All 10 quality requirements covered, each with at least one `file:line` reference.

---

# Appendix — File → Task mapping

Every file in the repo, in order, with the task that produced it. Useful for cross-checking that no file is "homeless".

| File | Created in |
|---|---|
| `.gitignore` | TICK-001 |
| `docker-compose.yml` | TICK-002 |
| `backend/TicketPlatform.sln` | TICK-003 |
| `backend/dotnet-tools.json` | TICK-003 |
| `backend/TicketPlatform.Api/TicketPlatform.Api.csproj` | TICK-003 |
| `backend/TicketPlatform.Business/TicketPlatform.Business.csproj` | TICK-003 |
| `backend/TicketPlatform.Data/TicketPlatform.Data.csproj` | TICK-003 |
| `frontend/package.json`, `vite.config.ts`, `tsconfig.json`, `index.html` | TICK-004 |
| `backend/TicketPlatform.Api/Properties/launchSettings.json` | TICK-004 |
| `backend/TicketPlatform.Data/Entities/User.cs` | TICK-005 |
| `backend/TicketPlatform.Data/Entities/Event.cs` | TICK-005 |
| `backend/TicketPlatform.Data/Entities/TicketCategory.cs` | TICK-005 |
| `backend/TicketPlatform.Data/Entities/Order.cs` | TICK-005 |
| `backend/TicketPlatform.Data/Entities/OrderItem.cs` | TICK-005 |
| `backend/TicketPlatform.Data/AppDbContext.cs` | TICK-006 |
| `backend/TicketPlatform.Data/Migrations/*` | TICK-007 |
| `backend/TicketPlatform.Data/Repositories/EventSearchRepository.cs` | TICK-007 |
| `backend/TicketPlatform.Business/Services/IAuthService.cs` | TICK-008 |
| `backend/TicketPlatform.Business/Services/AuthService.cs` | TICK-008 |
| `backend/TicketPlatform.Business/Services/IEventService.cs` | TICK-009 |
| `backend/TicketPlatform.Business/Services/EventService.cs` | TICK-009 |
| `backend/TicketPlatform.Business/Pricing/IPricingStrategy.cs` | TICK-010 |
| `backend/TicketPlatform.Business/Pricing/RegularPricingStrategy.cs` | TICK-010 |
| `backend/TicketPlatform.Business/Pricing/EarlyBirdPricingStrategy.cs` | TICK-010 |
| `backend/TicketPlatform.Business/Pricing/PricingStrategyFactory.cs` | TICK-010 |
| `backend/TicketPlatform.Business/Payments/IPaymentProcessor.cs` | TICK-011 |
| `backend/TicketPlatform.Business/Payments/MockPaymentProcessor.cs` | TICK-011 |
| `backend/TicketPlatform.Business/Payments/LoggingPaymentProcessorDecorator.cs` | TICK-011 |
| `backend/TicketPlatform.Business/Background/IEmailQueue.cs` | TICK-011 |
| `backend/TicketPlatform.Business/Background/EmailBackgroundService.cs` | TICK-011 |
| `backend/TicketPlatform.Api/Dtos/AuthDtos.cs` | TICK-012 |
| `backend/TicketPlatform.Api/Controllers/AuthController.cs` | TICK-012 |
| `backend/TicketPlatform.Api/Dtos/EventDtos.cs` | TICK-013 |
| `backend/TicketPlatform.Api/Controllers/EventsController.cs` | TICK-013 |
| `backend/TicketPlatform.Business/Services/IOrderService.cs` | TICK-014 |
| `backend/TicketPlatform.Business/Services/OrderService.cs` | TICK-014 |
| `backend/TicketPlatform.Api/Dtos/OrderDtos.cs` | TICK-014 |
| `backend/TicketPlatform.Api/Controllers/OrdersController.cs` | TICK-014 |
| `backend/TicketPlatform.Api/Filters/BusinessLogicAuditFilter.cs` | TICK-015 |
| `backend/TicketPlatform.Api/Program.cs` | TICK-015 |
| `backend/TicketPlatform.Api/appsettings.json` | TICK-015 (final form) |
| `frontend/src/api/client.ts` | TICK-016 |
| `frontend/src/auth/AuthContext.tsx` | TICK-016 |
| `frontend/src/auth/types.ts` | TICK-016 |
| `frontend/src/pages/LoginPage.tsx` | TICK-017 |
| `frontend/src/pages/RegisterPage.tsx` | TICK-017 |
| `frontend/src/pages/EventsListPage.tsx` | TICK-018 |
| `frontend/src/pages/EventDetailPage.tsx` | TICK-018 |
| `frontend/src/pages/MyOrdersPage.tsx` | TICK-019 |
| `frontend/src/pages/AdminEventsPage.tsx` | TICK-019 |
| `frontend/src/pages/AdminEventEditPage.tsx` | TICK-020 |
| `frontend/src/App.tsx` | TICK-020 |
| `frontend/src/main.tsx` | TICK-020 |
| `frontend/src/index.css` | TICK-020 |
| `README.md` | TICK-021 |
| `TECHNICAL_REPORT.md` | TICK-022 |
