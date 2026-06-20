# Player Topdown Camera Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a basic 3D player controller with world-space WASD movement and a smooth topdown orbital camera controlled by holding right mouse.

**Architecture:** Keep Unity components small and inspector-driven. Put movement and camera math in pure static helpers so EditMode tests can verify the important behavior without scene setup.

**Tech Stack:** Unity 2023.2.20f1, C#, Rigidbody, legacy Input Manager axes, Unity Test Framework EditMode tests.

---

### Task 1: Movement And Camera Math

**Files:**
- Create: `Assets/Scripts/Gameplay/TopDownControlMath.cs`
- Create: `Assets/Tests/Editor/TopDownControlMathTests.cs`

- [ ] **Step 1: Write failing tests**

Create tests for world-space input normalization, zero input handling, and camera orbit offset.

- [ ] **Step 2: Run tests and verify RED**

Run Unity EditMode tests. Expected: compile/test failure because `TopDownControlMath` does not exist yet.

- [ ] **Step 3: Implement math helper**

Add `TopDownControlMath` with `GetWorldMoveDirection` and `GetCameraPosition`.

- [ ] **Step 4: Run tests and verify GREEN**

Run Unity EditMode tests. Expected: all new tests pass.

### Task 2: Player Controller

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerController.cs`

- [ ] **Step 1: Implement inspector-driven controller**

Read `Horizontal` and `Vertical`, move a `Rigidbody` in `FixedUpdate`, and rotate toward movement direction.

- [ ] **Step 2: Verify compile**

Run Unity test/compile pass. Expected: no compiler errors.

### Task 3: Topdown Camera Controller

**Files:**
- Create: `Assets/Scripts/Gameplay/TopDownCameraController.cs`

- [ ] **Step 1: Implement camera follow and right mouse orbit**

Follow a target with configurable distance, height, yaw, smooth time, and mouse sensitivity.

- [ ] **Step 2: Verify compile**

Run Unity test/compile pass. Expected: no compiler errors.

### Editor Setup

1. Create a Player GameObject or use a PolygonCity character prefab.
2. Add `Rigidbody`, freeze X/Z rotation, keep Y rotation free.
3. Add `CapsuleCollider`.
4. Add `PlayerController`.
5. Add `TopDownCameraController` to the Main Camera and assign the Player as `Target`.
