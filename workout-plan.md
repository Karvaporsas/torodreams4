# Workout Implementation Plan

## Overview

Add a full workout tracking feature to ToroFitDreaming4.

- **Exercises**: shared library managed by admins via a dedicated UI page
- **Workouts**: per-user; each workout contains one or more exercises, each exercise has one or more sets (weight kg + reps)
- **Sets**: each set can be individually checked off as done
- **Duration**: tracked from workout start to completion
- **History**: every completed workout is saved and browsable

### Roles
A separate role/permission system (`UserRole` table). Roles: `User` (default), `Admin`.
Admin role is assigned via extended CLI: `dotnet run -- --assign-role <username> Admin`.
The JWT token carries role claims so the frontend can gate admin-only UI.

---

## Data Model

```
Exercise        — Id, Name, Description
Workout         — Id, UserId, StartedAt, CompletedAt?, DurationSeconds?
WorkoutExercise — Id, WorkoutId, ExerciseId, Order
WorkoutSet      — Id, WorkoutExerciseId, SetNumber, WeightKg, Reps, IsDone
UserRole        — UserId, Role (string: "Admin" | "User")
```

---

## Epic 1: Backend — Data Model & Migration ✅ DONE

### Task 1.1 — Add `UserRole` entity
- File: `backend/Models/UserRole.cs`
- Fields: `int UserId`, `string Role` (e.g. "Admin")
- FK to `User`

### Task 1.2 — Add `Exercise` entity
- File: `backend/Models/Exercise.cs`
- Fields: `int Id`, `string Name`, `string? Description`

### Task 1.3 — Add `Workout`, `WorkoutExercise`, `WorkoutSet` entities
- `backend/Models/Workout.cs`: `Id`, `int UserId`, `DateTime StartedAt`, `DateTime? CompletedAt`, `int? DurationSeconds`, nav `ICollection<WorkoutExercise>`
- `backend/Models/WorkoutExercise.cs`: `Id`, `int WorkoutId`, `int ExerciseId`, `int Order`, navs to `Workout` and `Exercise`, `ICollection<WorkoutSet>`
- `backend/Models/WorkoutSet.cs`: `Id`, `int WorkoutExerciseId`, `int SetNumber`, `decimal WeightKg`, `int Reps`, `bool IsDone`

### Task 1.4 — Update `AppDbContext`
- Add `DbSet<UserRole>`, `DbSet<Exercise>`, `DbSet<Workout>`, `DbSet<WorkoutExercise>`, `DbSet<WorkoutSet>`
- Configure FK relationships and cascade deletes
- Composite PK on `UserRole` (UserId + Role)

### Task 1.5 — EF migration
- `dotnet ef migrations add AddWorkouts`
- `dotnet ef database update`

---

## Epic 2: Backend — Roles & Admin CLI ✅ DONE

### Task 2.1 — Admin authorization policy
- Register a named `"AdminOnly"` policy in `Program.cs` that requires the `"Admin"` role claim
- Include roles as JWT claims in `AuthEndpoints.cs` when generating the token

### Task 2.2 — Extend CLI: `--assign-role`
- `backend/Cli/UserCreator.cs` (or new `RoleAssigner.cs`): `dotnet run -- --assign-role <username> <role>`
- Validates that user exists and role is valid, upserts `UserRole` row, prints confirmation
- Wire into `Program.cs` CLI dispatch

---

## Epic 3: Backend — Exercise API ✅ DONE

### Task 3.1 — `GET /api/exercises`
- Returns all exercises; requires authentication (any role)

### Task 3.2 — `POST /api/exercises`
- Creates an exercise; requires `AdminOnly` policy
- Body: `{ name, description? }`

### Task 3.3 — `PUT /api/exercises/{id}`
- Updates name/description; requires `AdminOnly` policy

### Task 3.4 — `DELETE /api/exercises/{id}`
- Deletes exercise; requires `AdminOnly` policy
- File: `backend/Endpoints/ExerciseEndpoints.cs`

---

## Epic 4: Backend — Workout API ✅ DONE

### Task 4.1 — `POST /api/workouts`
- Creates a new workout for the authenticated user; sets `StartedAt = UtcNow`
- Returns the new workout

### Task 4.2 — `GET /api/workouts`
- Returns the authenticated user's workouts (summary list, newest first)

### Task 4.3 — `GET /api/workouts/{id}`
- Returns full workout detail: exercises + sets (must belong to current user)

### Task 4.4 — `POST /api/workouts/{id}/exercises`
- Adds an exercise to the workout; body: `{ exerciseId, order? }`

### Task 4.5 — `DELETE /api/workouts/{id}/exercises/{workoutExerciseId}`
- Removes a workout exercise (cascades to its sets)

### Task 4.6 — `POST /api/workouts/{id}/exercises/{workoutExerciseId}/sets`
- Adds a set; body: `{ weightKg, reps }`; auto-increments `SetNumber`

### Task 4.7 — `PATCH /api/workouts/sets/{setId}`
- Updates `WeightKg`, `Reps`, and/or `IsDone` on a single set

### Task 4.8 — `DELETE /api/workouts/sets/{setId}`
- Deletes a set

### Task 4.9 — `PATCH /api/workouts/{id}/complete`
- Sets `CompletedAt = UtcNow`; calculates `DurationSeconds = CompletedAt - StartedAt`
- File: `backend/Endpoints/WorkoutEndpoints.cs`

---

## Epic 5: Frontend — Admin Exercise Management ✅ DONE

### Task 5.1 — Role-aware auth composable
- Extend `useAuth.ts`: `isAdmin()` reads the `role` claim from the decoded JWT payload

### Task 5.2 — Admin route guard
- Add `/admin/exercises` route
- `beforeEach` redirects non-admin users attempting to access `/admin/*`

### Task 5.3 — `AdminExercisesView.vue`
- File: `frontend/src/views/AdminExercisesView.vue`
- Lists all exercises
- Inline form to add a new exercise (name + optional description)
- Edit button per row (inline or modal)
- Delete button per row (with confirmation)

### Task 5.4 — `useExercises.ts` composable
- File: `frontend/src/composables/useExercises.ts`
- `fetchExercises()`, `createExercise()`, `updateExercise()`, `deleteExercise()`
- All calls include `Authorization: Bearer` header; redirect to `/login` on 401

### Task 5.5 — Admin nav link
- In `HomeView.vue` toolbar, show an **Admin** link only when `isAdmin()` is true

---

## Epic 6: Frontend — Workout Flow ✅ DONE

### Task 6.1 — Routes
- `/workouts` — workout history list
- `/workouts/active` — active (in-progress) workout
- `/workouts/:id` — completed workout detail (read-only)

### Task 6.2 — `useWorkout.ts` composable
- File: `frontend/src/composables/useWorkout.ts`
- `startWorkout()`, `fetchWorkouts()`, `fetchWorkout(id)`, `addExercise()`, `removeExercise()`, `addSet()`, `updateSet()`, `deleteSet()`, `completeWorkout()`

### Task 6.3 — `WorkoutsView.vue`
- File: `frontend/src/views/WorkoutsView.vue`
- Lists past workouts (date, duration, exercise count)
- **Start New Workout** button → calls `startWorkout()` → navigates to `/workouts/active`

### Task 6.4 — `WorkoutActiveView.vue`
- File: `frontend/src/views/WorkoutActiveView.vue`
- Shows elapsed time (live timer ticking from `StartedAt`)
- Exercise picker (dropdown/search from exercise library) + **Add** button
- Per exercise: list of sets (weight, reps, checkbox IsDone), **Add Set** button, **Remove Exercise** button
- **Complete Workout** button → calls `completeWorkout()` → navigates to history

### Task 6.5 — `WorkoutDetailView.vue`
- File: `frontend/src/views/WorkoutDetailView.vue`
- Read-only view of a completed workout: date, duration, exercises, sets with done status

### Task 6.6 — Navigation
- Add **Workouts** link to the `HomeView.vue` toolbar (visible to all authenticated users)

---

## Epic 7: Verification

### Task 7.1 — Admin exercise management
- Assign admin role via CLI, log in, confirm admin nav appears
- Create, edit, and delete exercises via admin UI

### Task 7.2 — Full workout flow
- Start a new workout, add exercises, add sets, check individual sets done
- Verify live timer counts up
- Complete the workout; confirm duration is saved

### Task 7.3 — Workout history
- Confirm completed workout appears in history list with correct date and duration

### Task 7.4 — Authorization checks
- Non-admin cannot access `/admin/exercises`
- User cannot read/write another user'\''s workouts (API returns 403/404)
