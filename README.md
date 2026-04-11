# ToroFitDreaming4

ToroFitDreaming4 is a full-stack fitness app with a **.NET 10** minimal API backend and a **Vue 3 + TypeScript** frontend. It supports JWT auth, workout tracking, a searchable exercise library, and an admin workflow for catalog maintenance.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (`^20.19.0 || >=22.12.0`)
- SQL Server for the backend app outside the test suite

## Running the app

### Backend

```bash
cd backend
dotnet run --urls http://localhost:5000
```

The API listens on `http://localhost:5000`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server starts on `http://localhost:5173`.

## Verification commands

Use the existing project checks:

```bash
cd backend.Tests
dotnet test

cd ../frontend
npm run build
npm run type-check
```

There is currently **no dedicated frontend test runner configured**, so frontend changes are validated with the supported build and type-check commands above.

## Architecture

- `backend/` — ASP.NET Core 10 minimal API with EF Core and SQL Server
- `backend/SeedData/exercise-catalog.v1.json` — repo-owned exercise catalog source of truth
- `frontend/` — Vue 3 + TypeScript (Vite) app

## Exercise catalog operations

### Import the repo-owned catalog

```bash
cd backend
dotnet run -- --import-exercises
```

You can also pass a custom catalog path:

```bash
dotnet run -- --import-exercises path\to\catalog.json
```

The import is idempotent and upserts by `Slug`.

### Backfill legacy exercise rows

```bash
cd backend
dotnet run -- --backfill-exercises
```

Use this after upgrading an existing database that may contain pre-catalog exercise rows with missing slugs, missing metadata defaults, blank search terms, or zero-value timestamps.

## Catalog JSON format

Each exercise entry in `backend/SeedData/exercise-catalog.v1.json` includes:

- `slug`
- `name`
- `description`
- `category`
- `bodyRegion`
- `movementPattern`
- `primaryMuscleGroup`
- `primaryEquipment`
- `secondaryEquipment`
- `difficultyLevel`
- `trainingStyle`
- `isUnilateral`
- `isArchived`
- `aliases`
- `secondaryMuscleGroups`

## Admin workflow

The `/admin/exercises` screen supports:

- full exercise metadata create/edit
- server-side search, filters, sorting, and paging
- archive and unarchive actions
- workout reference counts so admins can see when delete falls back to archive
- catalog status visibility and repo-catalog re-import actions

## Catalog maintenance process

The repo-owned JSON catalog is the **source of truth**. Use this workflow for future exercise additions or edits:

1. Update `backend/SeedData/exercise-catalog.v1.json`.
2. Run `dotnet run -- --import-exercises` against the target environment.
3. If the environment contains older hand-entered exercise rows, run `dotnet run -- --backfill-exercises`.
4. Validate with `dotnet test`, `npm run build`, and `npm run type-check`.

Admin UI edits are useful for immediate catalog maintenance, but they should be copied back into the repo catalog so production-only changes do not become the long-term source of truth.
