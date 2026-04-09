# Authentication Plan

## Overview

Add JWT-based authentication to ToroFitDreaming4.

- **Strategy**: JWT (stateless, `Bearer` tokens)
- **User store**: SQL Server via EF Core
- **Registration**: No self-sign-up. Admin creates users via a .NET CLI command.
- **Protection scope**: All API endpoints require a valid JWT by default (including `GET /api/hello`).

The frontend gains a Login page. On successful login the JWT is stored in `localStorage` and attached to every API request. Unauthenticated users are redirected to `/login`.

---

## Epic 1: Backend — Database & User Model ✅ DONE

### Task 1.1 — Add NuGet packages
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools` (for migrations)
- `BCrypt.Net-Next` (password hashing)

### Task 1.2 — Create `User` entity
- File: `backend/Models/User.cs`
- Fields: `int Id`, `string Username` (unique), `string PasswordHash`

### Task 1.3 — Create `AppDbContext`
- File: `backend/Data/AppDbContext.cs`
- `DbSet<User> Users`
- Unique index on `Username`

### Task 1.4 — Connection string
- Add `"DefaultConnection"` to `appsettings.json` and `appsettings.Development.json`
- Register `AppDbContext` in `Program.cs`

### Task 1.5 — EF Core migration
- Run `dotnet ef migrations add InitialCreate`
- Run `dotnet ef database update`

---

## Epic 2: Backend — JWT Authentication ✅ DONE

### Task 2.1 — Add NuGet package
- `Microsoft.AspNetCore.Authentication.JwtBearer`

### Task 2.2 — JWT settings in `appsettings.json`
```json
"Jwt": {
  "Secret": "<long-random-secret-min-32-chars>",
  "Issuer": "ToroFitDreaming4",
  "Audience": "ToroFitDreaming4",
  "ExpiryMinutes": 60
}
```

### Task 2.3 — Configure JWT in `Program.cs`
- Register `Authentication` + `JwtBearer` services
- Add `app.UseAuthentication()` and `app.UseAuthorization()` to the middleware pipeline

### Task 2.4 — Protect all endpoints by default
- Add a global `RequireAuthorization()` fallback policy so every endpoint requires auth unless explicitly marked `AllowAnonymous`

### Task 2.5 — Create `POST /api/auth/login` endpoint
- File: `backend/Endpoints/AuthEndpoints.cs` (or inline in `Program.cs`)
- Accept `{ username, password }`
- Verify password with BCrypt against DB
- On success, generate and return a signed JWT
- On failure, return `401 Unauthorized`
- Mark as `AllowAnonymous`

---

## Epic 3: Backend — Admin CLI for User Creation ✅ DONE

### Task 3.1 — Add CLI argument handling in `Program.cs`
- If `--create-user <username> <password>` args are detected at startup, run user creation mode instead of starting the web server
- Example: `dotnet run -- --create-user alice s3cr3tP@ss`

### Task 3.2 — User creation logic
- Hash password with BCrypt
- Check for duplicate username and return a clear error if taken
- Insert the new `User` row into the database
- Print confirmation and exit

---

## Epic 4: Frontend — Login Flow

### Task 4.1 — Install Vue Router
- `npm install vue-router`
- Register router in `main.ts`
- Define two routes: `/` (home) and `/login`

### Task 4.2 — Create `LoginView.vue`
- File: `frontend/src/views/LoginView.vue`
- Form with `username` and `password` fields
- On submit, `POST /api/auth/login`
- On success, store JWT in `localStorage`, redirect to `/`
- On failure, display an error message

### Task 4.3 — Auth composable
- File: `frontend/src/composables/useAuth.ts`
- `getToken()` — reads JWT from `localStorage`
- `isAuthenticated()` — returns true if a token exists
- `logout()` — clears token, redirects to `/login`

### Task 4.4 — Route guard
- In the router config, add a `beforeEach` guard
- Redirect to `/login` if the user is not authenticated and the route is not `/login`

### Task 4.5 — Authenticated API calls
- Update `HelloWorld.vue` to include `Authorization: Bearer <token>` header in the `fetch` call
- On `401` response, call `logout()` to clear stale token and redirect to login

---

## Epic 5: Verification

### Task 5.1 — Verify protected endpoint
- Without a token, `GET /api/hello` must return `401`

### Task 5.2 — End-to-end login flow
- Create a user via CLI
- Open `http://localhost:5173`, confirm redirect to `/login`
- Log in, confirm redirect to `/` and the hello message is displayed

### Task 5.3 — Verify token expiry/logout
- Click logout (or clear localStorage), confirm redirect back to `/login`



