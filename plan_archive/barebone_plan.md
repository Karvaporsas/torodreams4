# ToroFitDreaming4 — Implementation Plan ✅ DONE

## Overview

A barebone web app with a **Vue 3 + TypeScript** (Vite) frontend and a **.NET 9** REST API backend. The two run as separate dev servers. The frontend fetches from a Hello World endpoint on the backend and displays the response.

## Folder Structure

```
/
├── backend/          # .NET 9 Web API
├── frontend/         # Vue 3 + TypeScript (Vite)
├── .github/
│   └── copilot-instructions.md
├── .gitignore
├── README.md
└── plan.md
```

---

## Epic 1: .NET Backend

### Task 1.1 — Scaffold .NET 9 Web API
- Run `dotnet new webapi -n ToroFitDreaming4 -o backend`
- Remove the default WeatherForecast template files

### Task 1.2 — Add Hello World endpoint
- Create `HelloWorldController.cs` in `backend/Controllers/`
- Route: `GET /api/hello`
- Response: `{ "message": "Hello, World!" }`

### Task 1.3 — Configure CORS
- In `backend/Program.cs`, add a CORS policy allowing `http://localhost:5173` (Vite default)
- Apply the policy to the middleware pipeline

### Task 1.4 — Verify API
- Run with `dotnet run` inside `backend/`
- Confirm `GET http://localhost:5000/api/hello` returns expected JSON

---

## Epic 2: Vue Frontend

### Task 2.1 — Scaffold Vue 3 + TypeScript app
- Run `npm create vue@latest` inside `frontend/`
- Select: TypeScript ✓, Vue Router ✗ (not needed yet), Pinia ✗, Vitest ✗

### Task 2.2 — Fetch from Hello World API
- In `HelloWorld.vue` (or `App.vue`), use `fetch` or `axios` to call `GET http://localhost:5000/api/hello` on component mount
- Store the response message in a `ref`

### Task 2.3 — Display the message
- Render the message in the component template
- Show a loading state while the request is in-flight

### Task 2.4 — Verify end-to-end
- Run backend (`dotnet run`) and frontend (`npm run dev`) simultaneously
- Open `http://localhost:5173` and confirm the message from the API is displayed

---

## Epic 3: Project Setup

### Task 3.1 — Root .gitignore
- Cover .NET artifacts: `bin/`, `obj/`, `*.user`
- Cover Node.js artifacts: `node_modules/`, `dist/`

### Task 3.2 — README.md
- Document prerequisites (.NET 9 SDK, Node.js)
- Include run commands for both servers

### Task 3.3 — Update copilot-instructions.md
- Fill in architecture, build/run commands, and conventions once both apps are scaffolded
