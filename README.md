# ToroFitDreaming4

A minimal full-stack web app: **.NET 9** REST API + **Vue 3 + TypeScript** (Vite) frontend.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)

## Running the backend

```bash
cd backend
dotnet run
```

The API listens on `http://localhost:5000`. Endpoint: `GET /api/hello`

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

The dev server starts at `http://localhost:5173`.

## Architecture

- `backend/` — ASP.NET Core 9 minimal API
- `frontend/` — Vue 3 + TypeScript (Vite)

The frontend fetches `GET http://localhost:5000/api/hello` on page load and displays the response message.
