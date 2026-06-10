---
name: openmemory-startup
description: Start the OpenMemory local service if not already running, and verify health.
---
# openmemory-startup

Start the OpenMemory local service if it is not already running.

## When to use

Call this skill when:
- A new Reasonix session begins and you need memory
- The user asks to start OpenMemory
- MCP `openmemory_*` tools return connection errors

## How to run

### Step 1: Check if already running

```powershell
curl -s http://localhost:8080/health 2>&1
```

If the response contains `"ok":true`, OpenMemory is already running. Stop here and report it.

### Step 2: Start the service (if not running)

```powershell
cd D:\OpenMemory\packages\openmemory-js; npx tsx src/server/index.ts 2>&1
```

Start this as a **background job** (`run_in_background: true`) so it persists.

### Step 3: Poll until ready

Read output with `bash_output(job_id)` repeatedly until you see:
`[SERVER] Running on http://localhost:8080`

Then confirm with:
```powershell
curl -s http://localhost:8080/health
```

### Step 4: Report status

Tell the user the service is running and the MCP tools are available.

## Important notes

- The OpenMemory repo is at `D:\OpenMemory`
- Database is at `A:\Memory\openmemory.sqlite` (SQLite, hybrid tier, no API key required)
- MCP endpoint: `http://localhost:8080/mcp`
- If the MCP tools still fail after startup, reinstall with `install_source` (apply=true, kind=mcp, name=openmemory, source=http://localhost:8080/mcp, transport=http, scope=project, replace=true)
