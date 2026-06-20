# Pedestrian Density Routing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make pedestrians spawn closer/more densely and walk with persistent sidewalk-like direction instead of fully random wandering.

**Architecture:** Keep `PedestrianSpawner` responsible for population and visibility-safe spawning. Keep `PedestrianController` as the `MonoBehaviour` bridge to `NavMeshAgent`, with non-`MonoBehaviour` FSM states. Add pure route helper methods so core direction behavior can be tested outside Play Mode.

**Tech Stack:** Unity 2023.2, C#, NavMeshAgent, NUnit editor tests.

---

### Task 1: Route Rules

**Files:**
- Create: `Assets/Scripts/NPCs/PedestrianRouteRules.cs`
- Test: `Assets/Tests/Editor/PedestrianRouteRulesTests.cs`

- [ ] Write tests for normalized route directions, jittered direction output, and planar stuck-distance checks.
- [ ] Implement the pure route helpers used by `PedestrianController`.
- [ ] Verify editor compile includes the test file after Unity regenerates projects.

### Task 2: Population Defaults

**Files:**
- Modify: `Assets/Scripts/NPCs/PedestrianSpawner.cs`

- [ ] Increase default `maxAlive`.
- [ ] Reduce spawn interval and spawn/despawn distances.
- [ ] Keep camera viewport rejection so NPCs do not appear on-screen.

### Task 3: Persistent Routing

**Files:**
- Modify: `Assets/Scripts/NPCs/PedestrianController.cs`
- Modify: `Assets/Scripts/NPCs/PedestrianWanderState.cs`

- [ ] Replace random circular wandering with long steps along a remembered route direction.
- [ ] Add slight direction jitter so movement is not robotic.
- [ ] Add simple stuck detection and route reset.
- [ ] Keep FSM states as plain C# classes.

### Task 4: Verification

**Files:**
- Verify: `Assembly-CSharp.csproj`
- Verify: temporary generated project including new files if Unity has not regenerated `.csproj`.

- [ ] Build runtime scripts.
- [ ] Report warnings separately from errors.
