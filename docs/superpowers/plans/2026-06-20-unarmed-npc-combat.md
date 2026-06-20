# Unarmed NPC Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a first playable unarmed combat loop: left click punches NPCs, NPCs lose health, flee or rarely fight back, and die with an animation trigger.

**Architecture:** Add generic damage primitives under Gameplay and plug them into pedestrians. Keep NPC behavior inside the existing non-MonoBehaviour FSM. Keep player combat as a separate controller from movement so weapon actions can replace unarmed punch later.

**Tech Stack:** Unity 2023.2, C#, Rigidbody player, NavMeshAgent pedestrians, NUnit editor tests.

---

### Task 1: Health and Damage Primitives

**Files:**
- Create: `Assets/Scripts/Gameplay/DamageInfo.cs`
- Create: `Assets/Scripts/Gameplay/IDamageable.cs`
- Create: `Assets/Scripts/Gameplay/HitPoints.cs`
- Create: `Assets/Scripts/Gameplay/Health.cs`
- Test: `Assets/Tests/Editor/HitPointsTests.cs`

- [ ] Write tests proving hit points clamp initial health and report death at zero.
- [ ] Implement pure `HitPoints`.
- [ ] Implement `Health` MonoBehaviour as the scene-facing adapter.

### Task 2: Player Unarmed Attack

**Files:**
- Create: `Assets/Scripts/Gameplay/PlayerCombatController.cs`
- Modify: `Assets/Scripts/Gameplay/PlayerAnimationController.cs`

- [ ] Add left-click punch input.
- [ ] Apply damage to `IDamageable` targets in a frontal sphere.
- [ ] Trigger `Punch` on the player animator when attacking.

### Task 3: Pedestrian Reactions

**Files:**
- Create: `Assets/Scripts/NPCs/PedestrianFleeState.cs`
- Create: `Assets/Scripts/NPCs/PedestrianCombatState.cs`
- Create: `Assets/Scripts/NPCs/PedestrianDeadState.cs`
- Modify: `Assets/Scripts/NPCs/PedestrianController.cs`

- [ ] Subscribe NPCs to `Health` damage/death events.
- [ ] On non-lethal damage, choose flee or combat by `fightBackChance`.
- [ ] On death, stop the NavMeshAgent and trigger death animation.
- [ ] In flee, move away from the attacker for a short duration.
- [ ] In combat, approach the player and punch with cooldown.

### Task 4: Verification

**Files:**
- Verify runtime scripts and editor tests compile.

- [ ] Run runtime build.
- [ ] Run editor build or note lock if Unity holds the DLL.
