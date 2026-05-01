# NumMatch Prototype

A high-performance puzzle game developed in Unity C#. Players match pairs of numbers based on specific logic to clear the board and collect gems.

![NumMatch Banner](https://via.placeholder.com/800x400?text=NumMatch+Gameplay+Banner)

---

## Core Features

### 1. Number Grid & Match Logic
*   **Dynamic Grid:** Starts with 27 numbers. Supports expansion up to $1728$ numbers ($27 \times 2^6$) via the "Add More" mechanic.
*   **Matching Rules:** Pairs are valid if their **sum equals 10** or if the numbers are **identical**.
*   **Pathfinding:** Supports horizontal, vertical, diagonal, and "wrap-around" matching.
    *   *Logic:* `MatchManager.CanMatch()`

### 2. Smooth User Experience (UX)
*   **Input Handling:** Tap to select/deselect. Includes auto-deselect for incorrect matches.
*   **Visual Feedback:** High-fidelity animations powered by **DOTween**, including pulsing hints, line-matching, and horizontal shake effects.
*   **VFX:** Dynamic line drawing and row-clearing animations.
    *   *Logic:* `VFXManager.DrawMatchLine`

### 3. Advanced Mechanics
*   **Audio System:** Managed via `AudioManager.cs` using enums: `ChooseNumber`, `PairClear`, `Pop`, `RowClear`, `GemCollect`, `Write`, `Wrong`.
*   **Hint & Suggestion:** 
    *   **Smart Suggest:** Highlights neighbors when a cell is selected.
    *   **Auto-Hint:** Triggers after 10 seconds of inactivity to reduce player stress.

---

## Technical Specifications

*   **Unity Version:** 2021.3 LTS
*   **Architecture:** 
    *   **SOLID Principles:** High adherence to SRP (Single Responsibility Principle).
    *   **Design Patterns:** Singleton pattern for Manager classes.
    *   **Data Structures:** 1D List management for performance-oriented board state handling.
*   **Essential Plugins:**
    *   **DOTween (Demigiant):** Used for all procedural animations, UI transitions, and game-feel effects (scaling, fading, shaking).
    *   **TextMesh Pro:** High-quality text rendering using the *Nunito* font family.

---

## Project Structure

Below is the organized directory structure of the project:

```text
Assets/
├── Font/               # Nunito Font family (SDF assets)
├── Nummatch/           # Sprites for number cells (1-9) and masks
├── Plugins/
│   └── Demigiant/      # DOTween library core
├── Prefabs/
│   └── Cell.prefab     # The primary game cell unit
├── Resources/          # DOTween and TMP configuration files
├── Scenes/
│   └── SampleScene.unity
├── Scripts/
│   ├── Audio/          # AudioManager.cs
│   ├── Board/          # Board, Match, and Generate Managers
│   ├── Core/           # App, Game, and Constants
│   ├── FX/             # VFXManager.cs (DOTween logic)
│   ├── Gameplay/       # Gem, Hint, and Suggest Managers
│   └── UI/             # UIManager.cs
├── Sounds/             # SFX assets (wav files)
├── TextMesh Pro/       # TMP Essential resources and shaders
└── UI/                 # Game UI sprites (Icons, Backgrounds, Gems)
```

---

## Gameplay Logic

### Level Generation
The algorithm ensures every stage is solvable:
*   **Shuffle Distribution:** Ensures numbers 1-9 are distributed evenly.
*   **Constraint Satisfaction:** Fills the board while maintaining a specific initial pair count without creating unsolvable states.
*   **Performance:** Generates complex boards in **< 1s** with a 99% success rate.

### Win/Loss Conditions
*   **Win:** Triggered when the goal in `GemManager` is reached.
*   **Loss:** Triggered when no further matches are possible and "Add More" is exhausted.

---

## Development Timeline
*   **Start:** April 24, 2026
*   **Completion:** May 1, 2026

---