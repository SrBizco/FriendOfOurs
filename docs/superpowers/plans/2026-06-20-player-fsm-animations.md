# Player FSM Animations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the player locomotion into a class-based FSM and expose Animator parameters for idle, walk, run, and jump.

**Architecture:** `PlayerController` remains the only main MonoBehaviour. Movement behavior is delegated to non-MonoBehaviour states inheriting from `PlayerState`, while `PlayerAnimationController` wraps Animator parameter writes.

**Tech Stack:** Unity 2023.2.20f1, C#, Rigidbody, legacy Input Manager, Animator Blend Tree, Unity EditMode tests.

---

### Task 1: Pure Locomotion Rules

**Files:**
- Modify: `Assets/Scripts/Gameplay/TopDownControlMath.cs`
- Create: `Assets/Scripts/Gameplay/PlayerJumpCounter.cs`
- Modify: `Assets/Tests/Editor/TopDownControlMathTests.cs`
- Create: `Assets/Tests/Editor/PlayerJumpCounterTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for sprint animation speed and configurable consecutive jump consumption/reset.

- [ ] **Step 2: Implement minimal logic**

Add `GetAnimationSpeed(inputMagnitude, isSprinting)` and `PlayerJumpCounter`.

### Task 2: FSM Classes

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerState.cs`
- Create: `Assets/Scripts/Gameplay/PlayerIdleState.cs`
- Create: `Assets/Scripts/Gameplay/PlayerMoveState.cs`
- Create: `Assets/Scripts/Gameplay/PlayerJumpState.cs`
- Create: `Assets/Scripts/Gameplay/PlayerStateMachine.cs`

- [ ] **Step 1: Implement non-MonoBehaviour states**

Idle switches to Move on input and Jump on jump input. Move applies horizontal movement and switches to Idle when input stops. Jump applies impulse on enter and allows air movement.

### Task 3: Animation Wrapper And Controller Integration

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerAnimationController.cs`
- Modify: `Assets/Scripts/Gameplay/PlayerController.cs`

- [ ] **Step 1: Integrate Animator parameters**

Write `Speed`, `IsGrounded`, `VerticalSpeed`, and `Jump` trigger from code. Keep Animator Controller setup manual in Unity.

### Editor Setup

1. On the visual child/player model, add `Animator`.
2. Create an Animator Controller manually.
3. Add parameters: `Speed` float, `IsGrounded` bool, `VerticalSpeed` float, `Jump` trigger.
4. Create a 1D Blend Tree driven by `Speed`: idle at `0`, walking around `0.5`, running at `1`.
5. Add jump state/clip triggered by `Jump`, returning to locomotion when grounded.
