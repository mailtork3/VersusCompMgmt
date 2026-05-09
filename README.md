# Company Management — Full-Stack Application

A full-stack application for managing company information. Built with a **.NET 8 Web API** backend and an **Angular 21** frontend. Companies are stored in-memory during runtime and exposed through a clean REST API, with a responsive single-page application for creating, listing, and searching companies.

---

## Table of Contents

1. [Features](#features)
2. [Project Structure](#project-structure)
3. [Architecture](#architecture)
4. [Prerequisites](#prerequisites)
5. [Running the Application](#running-the-application)
6. [API Reference](#api-reference)
7. [Validation Rules](#validation-rules)
8. [Frontend Components](#frontend-components)
9. [Docker & Deployment](#docker--deployment)
10. [Testing](#testing)
11. [Future Enhancements](#future-enhancements)

---

## Features..

- **Create Companies** — submit a company name and website URL with full server-side and client-side validation
- **List Companies** — view all stored companies in a sortable table
- **Search by Name** — partial, case-insensitive name matching
- **Search by Domain** — filter by website domain string
- **Relevance Search** — rank companies by a keyword-match relevance score against a search term
- **Validation**
  - Name: required, minimum 3 characters
  - URL: must be a valid, well-formed HTTP/HTTPS URL
  - Relevance: company name must share at least one meaningful keyword with the website domain (minimum 25% score)
- **In-Memory Storage** — thread-safe dictionary; data lives for the duration of the process
- **Swagger UI** — interactive API documentation available in development mode
- **Dockerised** — both services containerised, orchestrated with Docker Compose

---

## Project Structure

```
Ravi/
├── backend/                          # .NET 8 Web API (C#)
│   ├── Controllers/
│   │   └── CompaniesController.cs    # HTTP endpoints — routes requests to CompanyService
│   ├── Models/
│   │   └── Company.cs                # Domain model + CreateCompanyRequest DTO
│   ├── Repository/
│   │   ├── ICompanyRepository.cs     # Repository interface (enables DB swap)
│   │   └── InMemoryCompanyRepository.cs  # Thread-safe in-memory implementation
│   ├── Services/
│   │   ├── CompanyService.cs         # Orchestrates validation + repository calls
│   │   ├── ValidationService.cs      # Name, URL, and relevance validation logic
│   │   └── RelevanceService.cs       # Keyword extraction and domain scoring
│   ├── Properties/
│   │   └── launchSettings.json       # Dev profiles (http: 5206, https: 7021/5206)
│   ├── Dockerfile                    # Multi-stage build: sdk:8.0 → aspnet:8.0
│   ├── Program.cs                    # App bootstrap, DI registration, CORS, Swagger
│   ├── CompanyAPI.csproj             # Target: net8.0, Swashbuckle 6.4
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── frontend/                         # Angular 21 SPA (TypeScript 5.9)
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── company-form/     # Reactive form for creating companies
│   │   │   │   ├── company-list/     # Table view of all companies
│   │   │   │   ├── company-search/   # Tabbed search (name / domain / relevance)
│   │   │   │   └── company-detail/   # Route-based detail view (route stub — see note)
│   │   │   ├── models/
│   │   │   │   └── company.ts        # Company, CreateCompanyRequest, ApiError interfaces
│   │   │   ├── services/
│   │   │   │   └── company.service.ts  # Typed HttpClient wrapper for all API calls
│   │   │   ├── app.ts                # Root component — signals, refresh counter
│   │   │   ├── app.html              # Layout: form (left) + search & list (right)
│   │   │   ├── app.config.ts         # provideHttpClient(), provideRouter()
│   │   │   └── app.routes.ts         # Route definitions (currently empty)
│   │   ├── main.ts                   # bootstrapApplication entry point
│   │   ├── styles.css                # Global styles
│   │   └── index.html
│   ├── Dockerfile                    # Multi-stage: node:20-alpine build → nginx:alpine serve
│   ├── nginx.conf                    # Gzip, security headers, cache, SPA fallback, API proxy
│   ├── proxy.conf.json               # Dev proxy: /api → http://localhost:5206
│   ├── angular.json
│   ├── package.json                  # Angular 21.2, RxJS 7.8, Vitest
│   └── tsconfig.json                 # strict, noPropertyAccessFromIndexSignature, ES2022
│
├── docker-compose.yml                # Orchestrates backend (:5000) + frontend (:80)
└── Ravi.sln                          # .NET solution file
```

> **Note — `company-detail` component:** The component exists and handles routing via `ActivatedRoute` (loads a company by ID from the URL parameter), but `app.routes.ts` currently has no routes defined. It is a ready-to-wire stub. To activate it, add a route like `{ path: 'company/:id', component: CompanyDetailComponent }` to `app.routes.ts`.

---

## Architecture

### Backend

```
HTTP Request
    └── CompaniesController        (routes, HTTP status codes, request/response DTOs)
            └── CompanyService     (business logic, orchestration)
                    ├── ValidationService  (name rules, URL rules, relevance threshold)
                    │       └── RelevanceService  (keyword extraction, domain scoring)
                    └── ICompanyRepository  (data access abstraction)
                            └── InMemoryCompanyRepository  (in-process Dictionary<int, Company>)
```

**Dependency Injection (Program.cs):**
| Registration | Lifetime | Reason |
|---|---|---|
| `InMemoryCompanyRepository` | Singleton | One shared store across all requests |
| `ValidationService` | Scoped | Stateless, created per request |
| `RelevanceService` | Scoped | Stateless, created per request |
| `CompanyService` | Scoped | Stateless, created per request |

**Thread safety:** The in-memory repository uses a `private readonly object _lock` and `lock (_lock)` blocks around every read and write to the `Dictionary<int, Company>`. This prevents data races under concurrent HTTP requests.

**CORS:** A permissive `AllowFrontend` policy (`AllowAnyOrigin / Method / Header`) is applied. This is intentional for development; tighten `AllowAnyOrigin` to a specific origin for production.

**Health endpoint:** `GET /health` returns `{ "status": "healthy" }` and is used by Docker health checks.

---

### Frontend

```
App (root)
 ├── signals: title, refreshCounter
 ├── CompanyFormComponent      — Reactive form, emits (companyCreated) on success
 ├── CompanySearchComponent    — Three search tabs, own state, ChangeDetectorRef
 └── CompanyListComponent      — Receives [refreshTrigger], reloads on change, ChangeDetectorRef
```

**Key design decisions:**

- **Standalone components** — every component declares its own `imports` array; no `NgModule` is used.
- **`provideHttpClient()` in `app.config.ts`** — the correct way to provide `HttpClient` in Angular 15+. Using `HttpClientModule` in a component's `imports` array can cause HTTP responses to not trigger zone-based change detection reliably.
- **`ChangeDetectorRef.markForCheck()`** — `CompanyListComponent` and `CompanySearchComponent` explicitly call `this.cdr.markForCheck()` inside every `subscribe` callback. Without this, HTTP responses arriving asynchronously can be missed by Angular's change detection when the parent component uses signals.
- **Angular Signals** — the root `App` component uses `signal(0)` for `refreshCounter`. When a company is created, `onCompanyCreated()` increments the counter via `refreshCounter.update(v => v + 1)`, which flows down to `[refreshTrigger]="refreshCounter()"` on `CompanyListComponent`, triggering `ngOnChanges` and a list reload.
- **Reactive Forms** — `CompanyFormComponent` uses `FormBuilder` + `Validators` (required, minLength(3), URL pattern) for client-side validation before any API call is made.
- **Strict TypeScript** — `tsconfig.json` enables `strict`, `noImplicitOverride`, `noPropertyAccessFromIndexSignature`, and `noImplicitReturns`. The `noPropertyAccessFromIndexSignature` flag requires bracket notation (`changes['refreshTrigger']`) instead of dot notation when accessing `SimpleChanges`.

---

### Relevance Algorithm

The `RelevanceService` scores how well a company name matches a website URL.

**Step 1 — Extract domain base:**

```
"https://www.apple.com" → host = "www.apple.com" → strip "www." → "apple.com" → first part = "apple"
```

**Step 2 — Extract keywords from company name:**
Split on spaces, hyphens, ampersands, and commas. Remove words that are ≤2 characters or in the stop-word list:

```
stop words: { "inc", "ltd", "llc", "corp", "corporation", "company", "co" }
"Apple Inc."  → ["apple"]
"Microsoft Corporation" → ["microsoft"]
"Bank of America" → ["bank", "america"]  (note: "of" is 2 chars, removed)
```

**Step 3 — Score:**

```
score = (keywords that appear in domain / total keywords) × 100
```

**Threshold:** A score of **< 25%** fails validation when creating a company.

**Examples:**
| Company Name | Website URL | Score | Result |
|---|---|---|---|
| Apple Inc. | https://apple.com | 100% | Pass |
| Microsoft Corporation | https://microsoft.com | 100% | Pass |
| Microsoft | https://apple.com | 0% | Fail |
| Bank of America | https://bankofamerica.com | 50% | Pass |

**Relevance search** works differently from creation validation — it uses the _search term_ as the "company name" and scores each stored company's URL against it, then returns results sorted highest score first (only those with score > 0).

---

## Prerequisites

### Docker (recommended)

- Docker Desktop with Docker Compose v3.8+

### Local Development

- **.NET 8.0 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+** — [download](https://nodejs.org)
- **Angular CLI 21+** — `npm install -g @angular/cli@21`

---

## Running the Application

### Option 1: Docker Compose

```bash
cd /path/to/Ravi
docker-compose up --build
```

| Service          | URL                           |
| ---------------- | ----------------------------- |
| Frontend (Nginx) | http://localhost              |
| Backend API      | http://localhost:5000         |
| Swagger UI       | http://localhost:5000/swagger |

Both containers have health checks configured. The frontend container waits for the backend to be healthy before starting.

To stop:

```bash
docker-compose down
```

---

### Option 2: Local Development

#### 1. Start the backend

```bash
cd backend
dotnet run
# Starts on http://localhost:5206 (default from launchSettings.json)
```

Swagger UI is available at http://localhost:5206/swagger in development mode.

To override the port:

```bash
ASPNETCORE_URLS=http://localhost:5000 dotnet run
```

If you change the port, update `proxy.conf.json` in the frontend accordingly.

#### 2. Start the frontend

```bash
cd frontend
npm install
npm start
# Starts on http://localhost:4200
# /api/* requests are proxied to http://localhost:5206 via proxy.conf.json
```

The `npm start` script runs `ng serve --proxy-config proxy.conf.json`. The proxy rewrites any request beginning with `/api` to the backend, so the frontend never makes cross-origin requests during development.

---

## API Reference

All endpoints are prefixed with `/api/companies`.

### POST /api/companies — Create a company

```http
POST /api/companies
Content-Type: application/json

{
  "name": "Apple Inc.",
  "websiteUrl": "https://www.apple.com"
}
```

**Responses:**

| Status            | Meaning                                                                                           |
| ----------------- | ------------------------------------------------------------------------------------------------- |
| `201 Created`     | Company created. Body contains the full `Company` object including assigned `id` and `createdAt`. |
| `400 Bad Request` | Validation failed. Body: `{ "error": "<reason>" }`                                                |

---

### GET /api/companies — Get all companies

```http
GET /api/companies
```

Returns a JSON array of all stored companies. Returns `[]` when none exist.

---

### GET /api/companies/{id} — Get a single company

```http
GET /api/companies/1
```

| Status          | Meaning                 |
| --------------- | ----------------------- |
| `200 OK`        | Company found           |
| `404 Not Found` | No company with that ID |

---

### GET /api/companies/search?name= — Search by name

```http
GET /api/companies/search?name=Apple
```

Case-insensitive substring match on `Company.Name`. Returns all companies whose name contains the search string.

---

### GET /api/companies/search?domain= — Search by domain

```http
GET /api/companies/search?domain=apple.com
```

Substring match on `Company.WebsiteUrl`. Useful for finding all companies under a given domain.

---

### GET /api/companies/relevance?search= — Relevance-ranked search

```http
GET /api/companies/relevance?search=Microsoft
```

Scores every stored company's website URL against the search term using the same keyword-extraction and domain-matching algorithm used during creation (see [Relevance Algorithm](#relevance-algorithm)). Returns only companies with a score > 0, ordered highest score first.

---

## Validation Rules

### Company Name

- Must not be empty or whitespace
- Must be at least **3 characters** long

### Website URL

- Must not be empty
- Must be a valid, absolute URI (`Uri.TryCreate` with `UriKind.Absolute`)
- Scheme must be `http` or `https`

### Relevance

- Computed by `RelevanceService.CalculateRelevance(name, websiteUrl)`
- Must reach a minimum score of **25%**
- Fails with: `"Company name does not appear relevant to the website URL. Relevance score: X%."`

### Error Response Format

All `400 Bad Request` responses follow this shape:

```json
{
  "error": "Descriptive message about what failed."
}
```

### Example Errors

```json
// Empty name
{ "error": "Company name must not be empty." }

// Name too short
{ "error": "Company name must contain at least 3 characters." }

// Invalid URL
{ "error": "Website URL must be a valid, well-formed URL (e.g., https://www.example.com)." }

// Wrong scheme
{ "error": "Website URL must use HTTP or HTTPS scheme." }

// Low relevance
{ "error": "Company name does not appear relevant to the website URL. Relevance score: 0%." }
```

---

## Frontend Components

### `CompanyFormComponent` (`app-company-form`)

- Reactive form built with `FormBuilder`
- Client-side validators: `Validators.required`, `Validators.minLength(3)`, URL pattern (`/^https?:\/\/.+/`)
- Calls `CompanyService.createCompany()` on valid submit
- Emits `(companyCreated)` event to the parent on success so the list can refresh
- Shows a "Company created successfully!" banner for 3 seconds after creation, then clears it
- Shows server-side validation errors (e.g., relevance failure) inline

### `CompanyListComponent` (`app-company-list`)

- Accepts `@Input() refreshTrigger: number` — any change (except the first) triggers `loadCompanies()`
- Loads all companies from `GET /api/companies` on `ngOnInit`
- Displays a loading spinner, an error message, a "no data" message, or the company table
- Uses `ChangeDetectorRef.markForCheck()` after every HTTP response to ensure the view updates

### `CompanySearchComponent` (`app-company-search`)

- Three tabs: **By Name**, **By Domain**, **By Relevance**
- Each tab has its own input field and search button
- Results shown in a table below the active tab
- Uses `ChangeDetectorRef.markForCheck()` after every HTTP response

### `CompanyDetailComponent` (`app-company-detail`)

- Reads a company ID from the URL parameter via `ActivatedRoute`
- Loads `GET /api/companies/:id` and displays full company details
- Has a "Back" button that navigates to `/`
- **Currently not reachable** — `app.routes.ts` has no routes defined. Wire it up by adding:
  ```typescript
  // app.routes.ts
  import { CompanyDetailComponent } from "./components/company-detail/company-detail";
  export const routes: Routes = [
    { path: "company/:id", component: CompanyDetailComponent },
  ];
  ```

### `CompanyService`

Central HTTP service, injected into all components. Methods:

| Method                      | HTTP Call                              |
| --------------------------- | -------------------------------------- |
| `createCompany(req)`        | `POST /api/companies`                  |
| `getAllCompanies()`         | `GET /api/companies`                   |
| `getCompanyById(id)`        | `GET /api/companies/:id`               |
| `searchByName(name)`        | `GET /api/companies/search?name=`      |
| `searchByDomain(domain)`    | `GET /api/companies/search?domain=`    |
| `searchByRelevance(search)` | `GET /api/companies/relevance?search=` |

All methods return `Observable<T>` and pipe through a shared `handleError` that extracts readable error messages from the server response.

---

## Docker & Deployment

### Backend Dockerfile (multi-stage)

| Stage     | Base image                            | What it does                                  |
| --------- | ------------------------------------- | --------------------------------------------- |
| `build`   | `mcr.microsoft.com/dotnet/sdk:8.0`    | Restores NuGet packages, compiles             |
| `publish` | (same)                                | `dotnet publish -c Release` to `/app/publish` |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Copies publish output, runs on port 5000      |

Environment variables set in the runtime image:

```
ASPNETCORE_URLS=http://+:5000
ASPNETCORE_ENVIRONMENT=Production
```

### Frontend Dockerfile (multi-stage)

| Stage   | Base image       | What it does                                     |
| ------- | ---------------- | ------------------------------------------------ |
| `build` | `node:20-alpine` | `npm ci`, `ng build --configuration production`  |
| runtime | `nginx:alpine`   | Copies `dist/frontend/browser` to Nginx web root |

The production Angular build is minified, tree-shaken, and ahead-of-time compiled.

### Nginx Configuration

The Nginx server (`nginx.conf`) handles four concerns:

1. **Gzip compression** — enabled for `application/javascript`, `text/css`, `application/json` (min 1000 bytes)
2. **Security headers** — `X-Frame-Options: SAMEORIGIN`, `X-XSS-Protection`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`
3. **Static asset caching** — JS, CSS, images, and fonts get `Cache-Control: public, immutable` with a 1-year expiry
4. **API reverse proxy** — requests to `/api/` are forwarded to `http://backend:5000` (the Docker service name) with proper `proxy_set_header` directives
5. **SPA fallback** — all unmatched paths return `index.html` so Angular's client-side router handles deep links

### Docker Compose

```
company-network (bridge)
├── backend  (company-api)    → port 5000:5000
└── frontend (company-frontend) → port 80:80, depends_on: backend
```

Both services have Docker health checks that poll every 30 seconds.

---

## Testing

### Backend

No automated tests are currently implemented. The `CompanyAPI.http` file in the backend directory can be used for manual endpoint testing in IDEs that support `.http` files (VS Code REST Client, JetBrains HTTP Client).

### Frontend

The project is configured to use **Vitest** (not Karma/Jasmine). To run:

```bash
cd frontend
npx vitest
```

No test files currently exist — the test infrastructure is in place via the `vitest` dev dependency and `tsconfig.spec.json`.

### Manual Testing via curl

```bash
# Create a company
curl -X POST http://localhost:5000/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name": "Apple Inc.", "websiteUrl": "https://www.apple.com"}'

# Get all companies
curl http://localhost:5000/api/companies

# Get by ID
curl http://localhost:5000/api/companies/1

# Search by name (partial match)
curl "http://localhost:5000/api/companies/search?name=Apple"

# Search by domain
curl "http://localhost:5000/api/companies/search?domain=apple.com"

# Relevance-ranked search
curl "http://localhost:5000/api/companies/relevance?search=Apple"

# Trigger validation errors
curl -X POST http://localhost:5000/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name": "Microsoft", "websiteUrl": "https://www.apple.com"}'
# → {"error":"Company name does not appear relevant to the website URL. Relevance score: 0%."}
```

---

## Extending the Application

### Add a new API endpoint

1. Add the method signature to `ICompanyRepository` (if data access is needed)
2. Implement it in `InMemoryCompanyRepository`
3. Add business logic in `CompanyService`
4. Add the route method in `CompaniesController` with an `[Http*]` attribute
5. Add the corresponding method to the Angular `CompanyService`
6. Add or update the UI component

### Swap to a real database

The repository interface (`ICompanyRepository`) is the only contract the rest of the system depends on. To add SQL persistence:

1. Create `SqlCompanyRepository : ICompanyRepository` using Entity Framework Core or Dapper
2. Change one line in `Program.cs`:
   ```csharp
   // Before
   builder.Services.AddSingleton<ICompanyRepository, InMemoryCompanyRepository>();
   // After
   builder.Services.AddScoped<ICompanyRepository, SqlCompanyRepository>();
   ```
3. No other code changes required.

---

## Future Enhancements

- [ ] Database persistence (PostgreSQL or SQL Server via EF Core)
- [ ] Activate `company-detail` route in `app.routes.ts`
- [ ] Edit and delete company endpoints
- [ ] Pagination for large datasets
- [ ] Unit tests (xUnit for backend, Vitest for frontend)
- [ ] Authentication & authorisation (JWT)
- [ ] API rate limiting
- [ ] Redis caching layer
- [ ] WebSocket / Server-Sent Events for real-time list updates
- [ ] Restrict CORS to known frontend origin in production

---

## algorithm

The algorithm works like this:

Extract domain — strips www. and TLD, leaving just the base (e.g. apple from https://www.apple.com)
Extract keywords — splits company name by spaces/hyphens, drops stop words (inc, ltd, corp, etc.) and words ≤ 2 chars
Score = (keywords found in domain / total keywords) × 100
Here are concrete examples:

Company Name URL Keywords Domain Matched Score
Apple https://www.apple.com [apple] apple 1/1 100% ✅
PayPal Holdings https://www.paypal.com [paypal, holdings] paypal 1/2 50% ✅
Amazon Web Services https://www.amazon.com [amazon, web, services] amazon 1/3 33.3% ✅
The New York Times https://www.nytimes.com [the, new, york, times] nytimes 1/4 25% ✅ (exactly at limit)
Meta Platforms https://www.facebook.com [meta, platforms] facebook 0/2 0% ❌
Twitter https://www.x.com [twitter] x 0/1 0% ❌
Apple https://www.microsoft.com [apple] microsoft 0/1 0% ❌
Bank of America https://www.bankofamerica.com [bank, america]\* bankofamerica 2/2 100% ✅

- "of" is 2 chars — filtered out by the length > 2 rule.

Key quirk: the domain check is a Contains (substring), not an exact match. So "times" matches inside "nytimes", and "bank" matches inside "bankofamerica". That's how the 25% / 100% cases above pass.
