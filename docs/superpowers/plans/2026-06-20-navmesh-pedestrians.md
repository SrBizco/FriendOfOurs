# NavMesh Pedestrians Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spawn pedestrians around the player, outside the camera view, and make them wander only on the baked sidewalk NavMesh.

**Architecture:** `PedestrianSpawner` owns population, pooling, spawn/despawn rules, and prefab selection. `PedestrianController` owns one NPC's NavMeshAgent wandering. `PedestrianSpawnRules` contains pure math for tests.

**Tech Stack:** Unity 2023.2.20f1, C#, UnityEngine.AI NavMeshAgent, com.unity.ai.navigation baked NavMesh, Unity EditMode tests.

---

### Task 1: Spawn Rules

**Files:**
- Create: `Assets/Scripts/NPCs/PedestrianSpawnRules.cs`
- Create: `Assets/Tests/Editor/PedestrianSpawnRulesTests.cs`

- [ ] **Step 1: Test spawn ring and camera visibility rules**
- [ ] **Step 2: Implement pure spawn/despawn helpers**

### Task 2: Pedestrian Controller

**Files:**
- Create: `Assets/Scripts/NPCs/PedestrianController.cs`

- [ ] **Step 1: Use NavMeshAgent to pick random sidewalk destinations**
- [ ] **Step 2: Drive optional Animator speed from agent velocity**

### Task 3: Pedestrian Spawner

**Files:**
- Create: `Assets/Scripts/NPCs/PedestrianSpawner.cs`

- [ ] **Step 1: Spawn near player outside camera view**
- [ ] **Step 2: Despawn far pedestrians only when outside camera view**
- [ ] **Step 3: Reuse inactive pedestrians through a small pool**

### Editor Setup

1. Create an empty `PedestrianSystem` GameObject.
2. Add `PedestrianSpawner`.
3. Assign `Player`, `Target Camera`, and `Pedestrian Prefabs`.
4. Ensure the prefabs have colliders/visuals; runtime adds `NavMeshAgent` and `PedestrianController` if missing.
5. Tune `Min Spawn Distance`, `Max Spawn Distance`, `Despawn Distance`, and `Max Alive`.
