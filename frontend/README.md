# Company Management — Frontend

This is the Angular 21 frontend for the Company Management application. It talks to a **.NET 8 Web API** backend over HTTP. Together the two services form a full-stack application for creating, listing, and searching companies.

> For the full project overview (Docker, architecture, API reference, relevance algorithm) see the [root README](../README.md).

---

## Table of Contents

1. [Tech Stack](#tech-stack)
2. [Project Structure](#project-structure)
3. [Prerequisites](#prerequisites)
4. [Running Locally](#running-locally)
   - [Start the Backend First](#1-start-the-backend-first)
   - [Start the Frontend](#2-start-the-frontend)
5. [How the Frontend Connects to the Backend](#how-the-frontend-connects-to-the-backend)
6. [Components](#components)
7. [Services](#services)
8. [Models](#models)
9. [Forms & Validation](#forms--validation)
10. [Change Detection](#change-detection)
11. [Building for Production](#building-for-production)
12. [Testing](#testing)

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | Angular 21.2 |
| Language | TypeScript 5.9 |
| HTTP client | `HttpClient` via `provideHttpClient()` |
| Forms | Reactive Forms (`FormBuilder`, `Validators`) |
| State | Angular Signals (`signal()`) |
| Styling | Plain CSS (no framework) |
| Dev server | `ng serve` with proxy config |
| Test runner | Vitest |
| Package manager | npm 10.8 |

---

## Project Structure

```
frontend/src/
├── app/
│   ├── components/
│   │   ├── company-form/
│   │   │   ├── company-form.ts       # Reactive form — create a company
│   │   │   ├── company-form.html
│   │   │   └── company-form.css
│   │   ├── company-list/
│   │   │   ├── company-list.ts       # Table of all companies, refreshes via Input
│   │   │   ├── company-list.html
│   │   │   └── company-list.css
│   │   ├── company-search/
│   │   │   ├── company-search.ts     # Tabbed search: name / domain / relevance
│   │   │   ├── company-search.html
│   │   │   └── company-search.css
│   │   └── company-detail/
│   │       ├── company-detail.ts     # Route-based detail view (routes not yet wired)
│   │       ├── company-detail.html
│   │       └── company-detail.css
│   ├── models/
│   │   └── company.ts               # Company, CreateCompanyRequest, ApiError interfaces
│   ├── services/
│   │   └── company.service.ts       # All HTTP calls in one place
│   ├── app.ts                       # Root component (signals, refresh counter)
│   ├── app.html                     # Two-column layout
│   ├── app.css
│   ├── app.config.ts                # provideHttpClient(), provideRouter()
│   └── app.routes.ts                # Route definitions (currently empty)
├── main.ts                          # bootstrapApplication entry point
├── styles.css                       # Global styles
└── index.html
```

---

## Prerequisites

- **Node.js 20+**
- **npm 10+** (comes with Node)
- **Angular CLI 21+**: `npm install -g @angular/cli@21`
- **Backend running** — the frontend has no mock server; all data comes from the .NET API

---

## Running Locally

Both the backend and frontend must be running at the same time.

### 1. Start the Backend First

The backend is a **.NET 8 Web API**. You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

```bash
cd ../backend
dotnet run
```

By default it starts on **http://localhost:5206** (configured in `Properties/launchSettings.json`).

The API will be available at:
- `http://localhost:5206/api/companies` — main endpoint
- `http://localhost:5206/swagger` — interactive API documentation (Swagger UI)
- `http://localhost:5206/health` — health check endpoint

To run on a different port:
```bash
ASPNETCORE_URLS=http://localhost:5000 dotnet run
```
If you change the port, update `proxy.conf.json` in the frontend to match.

---

### 2. Start the Frontend

```bash
# from the frontend/ directory
npm install       # first time only
npm start         # runs: ng serve --proxy-config proxy.conf.json
```

Open **http://localhost:4200** in your browser.

The `npm start` script (defined in `package.json`) always passes `--proxy-config proxy.conf.json` to `ng serve`. This is required — without the proxy, requests to `/api/*` will fail with a 404.

---

## How the Frontend Connects to the Backend

### Development (local)

During development, the Angular dev server proxies API calls to the backend using `proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5206",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

Any request the browser makes to `http://localhost:4200/api/...` is silently forwarded to `http://localhost:5206/api/...`. The browser never sees a cross-origin request, so no CORS issues occur in development.

**If the backend is not running**, every API call will return a 502 or connection-refused error. The components will display their error message state instead of data.

### Production (Docker)

In Docker, both services are on the same internal network (`company-network`). Nginx (which serves the built Angular app) forwards `/api/` requests to the backend container by its Docker service name:

```nginx
location /api/ {
    proxy_pass http://backend:5000;
}
```

The built Angular app itself is just static HTML/JS/CSS — it has no knowledge of the backend address. All routing to the backend is handled at the Nginx layer.

---

## Components

### `CompanyFormComponent` — `app-company-form`

**File:** [src/app/components/company-form/company-form.ts](src/app/components/company-form/company-form.ts)

Creates a new company by submitting a form to `POST /api/companies`.

**Inputs / Outputs:**
- `@Output() companyCreated: EventEmitter<Company>` — emitted after a successful creation so the parent can trigger a list refresh

**How it works:**
1. A `FormGroup` is built with `FormBuilder` containing two controls: `name` and `websiteUrl`
2. Client-side validators run before any HTTP call: `required`, `minLength(3)` for name; `required` + URL pattern for websiteUrl
3. On valid submit, `CompanyService.createCompany()` is called
4. On success: form resets, a "Company created successfully!" banner shows for 3 seconds, and `companyCreated` emits the new company
5. On error (e.g., relevance validation failure from the server): the error message is displayed inline

**Client-side validation:**
| Field | Validators | Error shown when |
|---|---|---|
| Company Name | `required`, `minLength(3)` | field touched and invalid |
| Website URL | `required`, pattern `/^https?:\/\/.+/` | field touched and invalid |

---

### `CompanyListComponent` — `app-company-list`

**File:** [src/app/components/company-list/company-list.ts](src/app/components/company-list/company-list.ts)

Displays all companies in a table, with loading and empty states.

**Inputs:**
- `@Input() refreshTrigger: number` — when this number changes (after the first change), `loadCompanies()` is called again

**How refresh works:**
The root `App` component holds a `refreshCounter = signal(0)`. It passes this to the list as `[refreshTrigger]="refreshCounter()"`. When a company is created, `App.onCompanyCreated()` increments the counter, which triggers `ngOnChanges` in `CompanyListComponent`:

```typescript
ngOnChanges(changes: SimpleChanges) {
  if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
    this.loadCompanies();
  }
}
```

`firstChange` is skipped because `ngOnInit` already loads the list on first render.

**States:**
- Loading — shows "Loading companies..."
- Error — shows the error message from the API
- Empty — shows "No companies found."
- Data — renders a table with ID, Name, Website (clickable link), Created At

---

### `CompanySearchComponent` — `app-company-search`

**File:** [src/app/components/company-search/company-search.ts](src/app/components/company-search/company-search.ts)

Three search modes presented as tabs. Each tab has its own input and result table.

| Tab | API call | Description |
|---|---|---|
| By Name | `GET /api/companies/search?name=` | Case-insensitive substring match on company name |
| By Domain | `GET /api/companies/search?domain=` | Substring match on website URL |
| By Relevance | `GET /api/companies/relevance?search=` | Keyword-match score; results sorted highest score first |

Each search validates that the input is not empty before calling the API. Results replace the previous results each time a search runs. A "Clear" button resets all inputs and results.

---

### `CompanyDetailComponent` — `app-company-detail`

**File:** [src/app/components/company-detail/company-detail.ts](src/app/components/company-detail/company-detail.ts)

A route-based detail page that loads a single company by its ID from the URL parameter.

**Currently not reachable** — `app.routes.ts` is empty. To activate it, add this to `app.routes.ts`:

```typescript
import { CompanyDetailComponent } from './components/company-detail/company-detail';

export const routes: Routes = [
  { path: 'company/:id', component: CompanyDetailComponent }
];
```

Once wired up, navigating to `http://localhost:4200/company/1` will load company with ID 1 from `GET /api/companies/1`. The component has a "Back" button that navigates to `/`.

---

## Services

### `CompanyService`

**File:** [src/app/services/company.service.ts](src/app/services/company.service.ts)

The single point of contact between the frontend and the backend API. All HTTP calls go through here.

```
apiUrl = '/api/companies'   ← relative URL, resolved by proxy (dev) or Nginx (prod)
```

| Method | HTTP | Description |
|---|---|---|
| `createCompany(req)` | `POST /api/companies` | Creates a new company |
| `getAllCompanies()` | `GET /api/companies` | Returns all companies |
| `getCompanyById(id)` | `GET /api/companies/:id` | Returns one company |
| `searchByName(name)` | `GET /api/companies/search?name=` | Name substring search |
| `searchByDomain(domain)` | `GET /api/companies/search?domain=` | Domain substring search |
| `searchByRelevance(search)` | `GET /api/companies/relevance?search=` | Relevance-ranked search |

All methods return `Observable<T>`. Errors are caught by `handleError`, which extracts the human-readable message from the backend's `{ "error": "..." }` response and rethrows it as a plain `Error`. Components subscribe and handle both `next` and `error` callbacks.

---

## Models

**File:** [src/app/models/company.ts](src/app/models/company.ts)

```typescript
// Mirrors the C# Company model from the backend
interface Company {
  id: number;
  name: string;
  websiteUrl: string;
  createdAt: Date;   // comes as a string from JSON — components do: new Date(c.createdAt)
}

// Request body for POST /api/companies
interface CreateCompanyRequest {
  name: string;
  websiteUrl: string;
}

// Shape of error responses from the backend
interface ApiError {
  error: string;
}
```

`createdAt` deserialises from JSON as a string. Every component that maps API data converts it explicitly:
```typescript
this.companies = data.map(c => ({ ...c, createdAt: new Date(c.createdAt) }));
```

---

## Forms & Validation

`CompanyFormComponent` uses Angular **Reactive Forms** (not template-driven forms). This means:

- The form model (`FormGroup`) lives in the TypeScript class, not the template
- `FormBuilder` constructs it in the constructor with validators attached per-field
- The template binds to the model with `[formGroup]` and `formControlName`
- Validation state (`form.invalid`, `name.errors`, `name.touched`) is read directly from the `FormGroup` in both the class and template

This approach makes the form state fully testable and keeps the template clean.

---

## Change Detection

Angular's default change detection runs on every browser event (clicks, HTTP responses, timers) via Zone.js. However, HTTP responses can sometimes be missed by the view update cycle when the parent component uses **signals** — Angular may handle signal-tracked components differently, causing async callback results to not immediately re-render.

Both `CompanyListComponent` and `CompanySearchComponent` inject `ChangeDetectorRef` and call `this.cdr.markForCheck()` inside every `subscribe` callback:

```typescript
this.companyService.getAllCompanies().subscribe({
  next: (data) => {
    this.companies = data.map(...);
    this.isLoading = false;
    this.cdr.markForCheck();   // ← tells Angular: re-check this component's view
  },
  error: (error) => {
    this.errorMessage = error.message;
    this.isLoading = false;
    this.cdr.markForCheck();
  }
});
```

Without these calls, the view could stay stuck on "Loading companies..." even after data has arrived.

---

## Building for Production

```bash
npm run build
```

Output goes to `dist/frontend/browser/`. The production build:
- Ahead-of-time (AOT) compiles all templates
- Tree-shakes unused code
- Minifies and bundles JS/CSS
- Generates hashed filenames for cache-busting

This output folder is what the `Dockerfile` copies into the Nginx container.

---

## Testing

The project is configured to use **Vitest** as the test runner (not Karma or Jasmine). The `vitest` package is in `devDependencies` and `tsconfig.spec.json` exists.

```bash
npx vitest        # run all tests
npx vitest --ui   # open the Vitest browser UI
```

No test files currently exist — the infrastructure is ready. Tests should be placed alongside components as `*.spec.ts` files.

---

## Useful Commands

```bash
# Start dev server (with proxy)
npm start

# Build for production
npm run build

# Run tests
npx vitest

# Generate a new component
ng generate component components/my-component

# Check for TypeScript errors
npx tsc --noEmit
```
