# Unarmed Combo Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Drive a three-hit unarmed combo and upper-body combat animation layer while preserving lower-body locomotion.

**Architecture:** Keep the Animator setup in Unity Editor. Scripts expose configurable trigger/layer names, maintain combo order in pure code, and set combat layer weight/timers at runtime.

**Tech Stack:** Unity 2023.2, C#, Humanoid Animator, Avatar Masks, NUnit editor tests.

---

### Task 1: Combo Sequencing

**Files:**
- Create: `Assets/Scripts/Gameplay/AttackComboCounter.cs`
- Test: `Assets/Tests/Editor/AttackComboCounterTests.cs`

- [ ] Test sequential combo indexes.
- [ ] Test combo reset after timeout.
- [ ] Implement minimal pure combo counter.

### Task 2: Player Animation Layer Hooks

**Files:**
- Modify: `Assets/Scripts/Gameplay/PlayerAnimationController.cs`

- [ ] Add configurable combat layer name.
- [ ] Add three attack trigger names.
- [ ] Add smooth upper-body layer weight.

### Task 3: Player Combat Timing

**Files:**
- Modify: `Assets/Scripts/Gameplay/PlayerCombatController.cs`

- [ ] Replace one trigger with combo trigger sequence.
- [ ] Delay damage application by a configurable hit delay.
- [ ] Keep the combat layer active for a configurable timeout.

### Task 4: Verification

**Files:**
- Verify runtime and editor compile.

- [ ] Build with generated project files or temporary project inclusion when Unity has not regenerated csproj.
