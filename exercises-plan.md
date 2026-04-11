# Exercise Library Implementation Plan

## Goal

Expand the current minimal exercise feature into a complete exercise library that:

1. imports a curated catalog of standard gym and athletic exercises into the database,
2. lets users quickly search and add exercises while building workouts, and
3. gives admins a proper exercise management area for ongoing CRUD and catalog maintenance.

## Current State

- The backend already has authenticated exercise endpoints and admin-only create/update/delete endpoints.
- The `Exercise` entity currently stores only `Id`, `Name`, and `Description`.
- The workout flow already lets users add exercises, but the UI uses a plain dropdown over the full exercise list and has no search or filtering.
- The frontend already has a basic `/admin/exercises` screen for add/edit/delete, but it only manages name and description.
- Automated backend tests already cover exercise CRUD, authorization, and workout flows.

## Confirmed Planning Decisions

- **Confirmed:** v1 of the catalog is curated and versioned inside the repo instead of depending on a third-party API at runtime.
- **Assumption:** search should match exercise name, aliases, muscles, movement pattern, and equipment.
- **Assumption:** exercises already used in completed workouts should be archived instead of hard-deleted.
- **Assumption:** the first import should ship as an idempotent app-owned seed/import process so every environment can get the same catalog.

## Proposed Scope

### In Scope

- richer exercise schema and importable seed dataset,
- idempotent exercise import into SQL Server,
- searchable exercise picker in workout creation/edit flow,
- improved admin exercise management with richer metadata,
- validation, tests, and migration/backfill work.

### Out of Scope for v1

- AI-generated exercise recommendations,
- video hosting or media upload pipelines,
- public community exercise submissions,
- localization beyond a single primary language dataset,
- advanced programming templates tied to specific exercises.

## Recommended Data Model Direction

Keep `Exercise` as the root entity, but expand it so the library is actually searchable and administrable.

### Core Exercise Fields

- `Id`
- `Slug` (stable unique identifier for seed/import upserts)
- `Name`
- `Description`
- `Category` (for example: squat, hinge, push, pull, carry, core, plyometric, conditioning)
- `BodyRegion` (upper body, lower body, full body, core)
- `MovementPattern` (squat, hip hinge, horizontal push, vertical push, horizontal pull, vertical pull, rotation, carry, locomotion)
- `PrimaryMuscleGroup`
- `SecondaryMuscleGroups` (via join table or normalized child rows)
- `PrimaryEquipment`
- `SecondaryEquipment`
- `DifficultyLevel`
- `TrainingStyle` (strength, hypertrophy, power, athletic, conditioning, mobility)
- `IsUnilateral`
- `IsArchived`
- `SearchTerms` / normalized alias text for fast lookup
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Supporting Tables

- `ExerciseAliases`
- `ExerciseMuscleTargets`
- optional lookup tables for `Equipment`, `MuscleGroup`, and `ExerciseCategory` if the team wants stronger referential consistency
- optional `ExerciseImportBatches` table to track versioned imports

## Epics and Tasks

### Epic 1 - Remodel the Exercise Domain **(Done)**

**Outcome:** the database can store a real exercise library instead of just freeform names.

**Tasks**

- **E1-T1** Define the canonical metadata model for exercises, aliases, muscles, equipment, and archive state. **Done**
- **E1-T2** Add EF Core entity changes and a migration for the expanded exercise schema. **Done**
- **E1-T3** Decide which fields stay on `Exercises` versus which become child tables to keep queries simple. **Done**
- **E1-T4** Add indexes for `Slug`, `Name`, archive state, category, equipment, and search-heavy fields. **Done**
- **E1-T5** Update API DTOs and request contracts so frontend/admin screens can read and edit the richer model. **Done**
- **E1-T6** Replace unsafe hard-delete behavior with archive or guarded delete logic when an exercise is already referenced by workouts. **Done**

### Epic 2 - Build the Import and Seed Pipeline **(Done)**

**Outcome:** every environment can load the same exercise catalog repeatedly without duplicate rows.

**Tasks**

- **E2-T1** Create a versioned source file for the master exercise catalog (recommended: JSON in `backend\SeedData\`). **Done**
- **E2-T2** Define the import contract with stable slugs, categories, aliases, muscles, and equipment. **Done**
- **E2-T3** Implement an idempotent import service that upserts by `Slug` instead of by display name. **Done**
- **E2-T4** Add a CLI entry point, such as `dotnet run -- --import-exercises`, following the existing CLI pattern already used for user/role management. **Done**
- **E2-T5** Add validation so the import fails loudly on duplicate slugs, missing required metadata, invalid enums, or malformed aliases. **Done**
- **E2-T6** Decide whether first-run environments import automatically or only through explicit CLI/admin action. **Done** — explicit CLI import only for v1.
- **E2-T7** Add automated tests for import idempotency and for safe re-import after catalog edits. **Done**

### Epic 3 - Make Exercise Search and Insert Fast in Workout Flow **(Done)**

**Outcome:** users can find exercises quickly without scrolling through a giant dropdown.

**Tasks**

- **E3-T1** Extend `GET /api/exercises` to support search, filters, sorting, paging, and archive exclusion. **Done**
- **E3-T2** Add filter parameters for `search`, `category`, `movementPattern`, `equipment`, `bodyRegion`, and `trainingStyle`. **Done**
- **E3-T3** Replace the workout view's plain `<select>` with a searchable picker/combobox. **Done**
- **E3-T4** Debounce search requests and keep the UX responsive for large catalogs. **Done**
- **E3-T5** Show enough exercise context in search results to disambiguate similar names (equipment, movement pattern, primary muscle, or short description). **Done**
- **E3-T6** Preserve existing workout insertion behavior, ordering, and set tracking after the picker changes. **Done**
- **E3-T7** Add empty, loading, and error states that make a large searchable library usable on mobile. **Done**

### Epic 4 - Upgrade the Admin Exercise Area

**Outcome:** admins can maintain the catalog safely after the initial import.

**Tasks**

- **E4-T1** Expand the admin form from name/description to full metadata editing.
- **E4-T2** Add admin-side search, filters, and sorting so admins can manage a large library efficiently.
- **E4-T3** Add archive/unarchive controls and surface whether an exercise is already referenced by workouts.
- **E4-T4** Add bulk import/re-import controls or at minimum a visible status page describing current catalog version and last import result.
- **E4-T5** Add form validation and duplicate-name or duplicate-slug protection.
- **E4-T6** Add pagination or virtualized rendering if the admin grid becomes large.

### Epic 5 - Quality, Backfill, and Release Safety

**Outcome:** the expanded library is safe to ship and easy to evolve.

**Tasks**

- **E5-T1** Backfill existing exercises into the new schema with generated slugs and default metadata where necessary.
- **E5-T2** Update backend tests to cover search, archive rules, import behavior, and richer admin CRUD.
- **E5-T3** Add frontend tests for searchable picker and admin edit flows if frontend test infrastructure already exists; otherwise cover via current supported checks.
- **E5-T4** Document the import command, catalog format, and admin workflow in the repo docs.
- **E5-T5** Define a maintenance process for future catalog additions so ad hoc production-only edits do not become the source of truth.

## Recommended Delivery Order

1. Epic 1 - schema and domain model
2. Epic 2 - import pipeline and seed dataset
3. Epic 3 - searchable workout picker
4. Epic 4 - richer admin UX
5. Epic 5 - tests, backfill, and documentation

## API/UX Notes Worth Preserving in the Plan

- Keep the current authenticated exercise listing route, but extend it instead of creating a parallel public endpoint unless requirements change.
- Preserve admin authorization with the existing `AdminOnly` policy.
- Avoid loading the full exercise list into the workout page on every mount once the catalog becomes large.
- Treat archive as the default delete path for referenced exercises because workouts already depend on `ExerciseId`.
- Use stable slugs for imports so exercise names can evolve without breaking idempotent re-imports.

## Exercise Catalog to Import into the Database

This is the proposed **v1 seed catalog** for the database. It covers standard commercial gym, strength, hypertrophy, and athletic training use cases.

### Squat, Lunge, and Lower-Body Strength

- Back Squat
- Front Squat
- Overhead Squat
- Box Squat
- Zercher Squat
- Bulgarian Split Squat
- Split Squat
- Walking Lunge
- Reverse Lunge
- Forward Lunge
- Lateral Lunge
- Cossack Squat
- Step-Up
- Goblet Squat
- Belt Squat
- Leg Press
- Hack Squat
- Leg Extension
- Standing Calf Raise
- Seated Calf Raise
- Tibialis Raise

### Hinge and Posterior Chain

- Conventional Deadlift
- Sumo Deadlift
- Romanian Deadlift
- Stiff-Leg Deadlift
- Trap Bar Deadlift
- Deficit Deadlift
- Rack Pull
- Good Morning
- Hip Thrust
- Barbell Glute Bridge
- Back Extension
- Glute-Ham Raise
- Nordic Hamstring Curl
- Lying Leg Curl
- Seated Leg Curl
- Single-Leg Romanian Deadlift

### Olympic Lifts and Power

- Power Clean
- Hang Power Clean
- Clean
- Hang Clean
- Power Snatch
- Hang Power Snatch
- Snatch
- Hang Snatch
- Clean Pull
- Snatch Pull
- High Pull
- Push Press
- Push Jerk
- Split Jerk
- Thruster

### Chest and Horizontal Push

- Bench Press
- Incline Bench Press
- Decline Bench Press
- Close-Grip Bench Press
- Dumbbell Bench Press
- Incline Dumbbell Bench Press
- Dumbbell Fly
- Incline Dumbbell Fly
- Chest Dip
- Machine Chest Press
- Incline Machine Chest Press
- Pec Deck
- Cable Fly
- Push-Up
- Incline Push-Up
- Decline Push-Up

### Back, Pulling, and Upper-Back Development

- Pull-Up
- Chin-Up
- Neutral-Grip Pull-Up
- Lat Pulldown
- Close-Grip Lat Pulldown
- Seated Cable Row
- One-Arm Cable Row
- Barbell Row
- Pendlay Row
- T-Bar Row
- Chest-Supported Row
- One-Arm Dumbbell Row
- Machine Row
- Inverted Row
- Straight-Arm Pulldown
- Face Pull
- Rear Delt Fly
- Dumbbell Shrug
- Barbell Shrug

### Vertical Push, Delts, and Triceps/Biceps Accessories

- Standing Overhead Press
- Seated Dumbbell Shoulder Press
- Arnold Press
- Dumbbell Lateral Raise
- Cable Lateral Raise
- Front Raise
- Upright Row
- Reverse Pec Deck
- Barbell Curl
- EZ-Bar Curl
- Hammer Curl
- Incline Dumbbell Curl
- Preacher Curl
- Cable Curl
- Skull Crusher
- Triceps Pushdown
- Overhead Triceps Extension
- Close-Grip Push-Up
- Bench Dip

### Kettlebell, Loaded Carries, and Full-Body Tools

- Kettlebell Swing
- Kettlebell Clean
- Kettlebell Snatch
- Turkish Get-Up
- Kettlebell Press
- Kettlebell Push Press
- Kettlebell Front Rack Reverse Lunge
- Kettlebell Romanian Deadlift
- Kettlebell Windmill
- Farmer's Carry
- Suitcase Carry
- Front Rack Carry
- Overhead Carry
- Sled Push
- Sled Pull

### Core and Trunk Training

- Plank
- Side Plank
- Dead Bug
- Bird Dog
- Hollow Hold
- Ab Wheel Rollout
- Hanging Knee Raise
- Hanging Leg Raise
- Toes-to-Bar
- Cable Crunch
- Pallof Press
- Russian Twist
- Sit-Up
- V-Up

### Plyometric, Athletic, and Conditioning Movements

- Box Jump
- Broad Jump
- Depth Jump
- Vertical Jump
- Jump Squat
- Skater Jump
- Lateral Bound
- Split Squat Jump
- Medicine Ball Slam
- Rotational Medicine Ball Throw
- Medicine Ball Chest Pass
- Overhead Medicine Ball Throw
- Sprint
- Hill Sprint
- Shuttle Run
- Lateral Shuffle
- A-Skip
- B-Skip
- High Knees
- Burpee
- Bear Crawl
- Battle Ropes
- Jump Rope
- Rowing Ergometer Sprint
- Assault Bike Sprint

## Risks and Design Decisions to Resolve Early

- Future catalog source changes should not break the repo-owned seed pipeline introduced in v1.
- Whether muscles and equipment should be normalized tables or stored as validated enums plus child rows.
- Whether exercise delete should be fully disabled once referenced, or replaced with archive-only semantics.
- Whether search should stay on simple indexed `LIKE` queries first or include SQL Server full-text search later.

## Definition of Done for This Plan

The implementation should be considered complete when:

- every exercise in the above catalog can be imported idempotently into the database,
- users can search and add exercises to workouts without browsing a massive dropdown,
- admins can create, read, update, archive, and safely manage the exercise library,
- existing workout history remains valid after the schema expansion,
- and test coverage exists for import, search, admin permissions, and workout insertion behavior.
