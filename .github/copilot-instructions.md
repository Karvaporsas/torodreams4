# Copilot Instructions

## Project Overview

ToroFitDreaming4 is a minimal full-stack web app with a .NET 9 REST API backend and a Vue 3 + TypeScript (Vite) frontend. The frontend fetches from a Hello World endpoint on the backend and displays the response.

## Architecture

- `backend/` — ASP.NET Core 9 minimal API (`Program.cs`). Single endpoint: `GET /api/hello` → `{ "message": "Hello, World!" }`. CORS is configured to allow `http://localhost:5173`.
- `frontend/` — Vue 3 + TypeScript (Vite). `src/components/HelloWorld.vue` fetches from the API on mount and displays the message. `src/App.vue` is the root component.

## Build, Test & Lint

```bash
# Backend
cd backend
dotnet run          # starts on http://localhost:5000

# Frontend
cd frontend
npm install         # install dependencies
npm run dev         # starts on http://localhost:5173
npm run build       # production build
npm run type-check  # TypeScript type checking
```

## Key Conventions

- Backend uses .NET 9 minimal APIs (no Controllers).
- Frontend uses Vue 3 Composition API with `<script setup lang="ts">`.
- API base URL is hardcoded as `http://localhost:5000` in the frontend for local dev.

