# Mundial - Conferência — Build Plan

Hi! 👋 This folder is **not** the finished app yet. It is a **plan** — like a box of
LEGO instructions — that tells a smart robot helper exactly how to build the app.

A long time ago someone wrote this program in an old computer language. A tool called
**RNC** read that old program, figured out what it does, and wrote everything down here
in plain steps. Now a robot helper called **BMAD** can read the steps and build a brand
new, modern version for you.

## What is in this folder?

- **README.md** — this page (start here).
- **docs/product-brief.md** — what the app is for, in a few sentences.
- **docs/prd.md** — the full list of things the app must do (the "rules").
- **docs/ux/** — what the screens look like and how people move between them.
- **docs/architecture.md** — the building plan: the data and how pieces fit.
- **docs/epics/** — big chunks of work, one per kind of thing the app manages.
- **docs/stories/** — small step-by-step jobs the robot does one at a time.

## What will the app let people manage?

- **Conferencias**
- **Estoqs**

## How do I build the app? (3 easy steps)

You need a computer with **Node.js** installed and a coding helper like
**Cursor** or **Claude Code**. Then:

**Step 1 — Get the BMAD robot helper.** Open a terminal in this folder and type:

```bash
npx bmad-method install
```

Press Enter and wait. This downloads the helper. (It is free and safe.)

**Step 2 — Open this folder in your coding helper** (Cursor or Claude Code).

**Step 3 — Ask the helper to build it.** Tell the **Dev** agent:

> Read `docs/prd.md`, `docs/architecture.md`, and every file in `docs/stories/`,
> then build the whole app exactly as the stories describe. Do one story at a time.

That's it! The robot will write the code for you. When it finishes, run the app and try it.

## The app will be built with

- Screens (frontend): angular
- Brains (backend): dotnet-core + prisma
- Memory (database): sqlserver

---
_Made by RNC from a legacy app. Plan format: BMAD method (https://github.com/bmad-code-org)._
