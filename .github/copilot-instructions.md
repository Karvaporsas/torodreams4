# Copilot Instructions

## Project Overview

ToroFitDreaming4 is a full-stack fitness app with a .NET 10 REST API backend and a Vue 3 + TypeScript (Vite) frontend. The app supports JWT-based authentication, workout tracking, an exercise library, and an admin area for managing exercises.

## Architecture

- `backend/` — ASP.NET Core 10 minimal API (`Program.cs`) using EF Core with SQL Server. The backend exposes authentication, exercise, and workout endpoints, and includes CLI commands for creating users and assigning roles.
- `frontend/` — Vue 3 + TypeScript (Vite) app with Vue Router. Key screens include login, workout history, active workout flow, workout details, and an admin exercise management view.

## Build, Test & Lint

```bash
# Backend
cd backend
dotnet run          # starts on http://localhost:5000

# Backend tests
cd ../backend.Tests
dotnet test

# Frontend
cd frontend
npm install         # install dependencies
npm run dev         # starts on http://localhost:5173
npm run build       # production build
npm run type-check  # TypeScript type checking
```

## Key Conventions

- Backend uses .NET 10 minimal APIs (no Controllers).
- Frontend uses Vue 3 Composition API with `<script setup lang="ts">`.
- API base URL is hardcoded as `http://localhost:5000` in the frontend for local dev.
- Frontend CSS lives exclusively in dedicated `.css` files under `src/assets/`. Do **not** use `<style>` blocks inside `.vue` components.

