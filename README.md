# Boss Rush ⚔️🛡️

A 2D Action-Platformer Unity project featuring dynamic form-switching combat mechanics and a boss encounter.

## 🎮 Controls

| Action | Key | Notes |
| :--- | :--- | :--- |
| **Move** | `A` / `D` or `Arrow Keys` | Standard movement |
| **Jump** | `Space` | |
| **Attack** | `J` | Hold `W` / `S` (or Up/Down) to aim attacks |
| **Switch Form** | `Q` | Toggles between **Sword** and **Tank** forms |
| **Block** | `K` | **Tank Form Only**. Negates incoming damage |

## ✨ Features

### 🔄 Form Switching System
The player can instantly switch between two distinct forms using the `PlayerController`:
* **Sword Form**: High agility, fast movement (`speed: 8`), and high jump (`jump: 12`).
* **Tank Form**: Slower movement (`speed: 4`), lower jump, but has the ability to **Block** incoming attacks.

### ⚔️ Combat Mechanics
* **Directional Attacks**: Attack Up, Down, Left, or Right based on input.
* **Pogo Mechanic**: Attacking **Down** (`S + J`) on an enemy propels the player upward.
* **Recoil/Knockback**: Hitting an enemy pushes the player back slightly, adding weight to combat.
* **Hitboxes**: Uses `Physics2D.OverlapBox` for precise hit detection.

### 🤖 Boss AI
* **Tracking**: The boss constantly tracks the active player form (even after switching).
* **Behavior**: Moves towards the player and stops at a specific `attackRange` to engage.
* **Collision Damage**: Deals contact damage to the player unless the player is blocking or in I-Frames.

### ❤️ Health & Damage
* **Invincibility Frames (I-Frames)**: Player flashes and becomes intangible briefly after taking damage.
* **Blocking**: In Tank form, holding block turns the player Cyan and prevents damage logic from running.
* **Visual Feedback**: Sprites flash red when taking damage.

## 🛠️ Technical Details
* **Engine**: Unity 2D
* **Scripts**:
    * `PlayerController.cs`: Handles movement, input, and form swapping.
    * `PlayerAttack.cs`: Manages hit detection, attack direction, and knockback forces.
    * `BossController.cs`: Simple AI for tracking and facing the player.
    * `Health.cs`: Modular health system used by both Player and Boss.

## 🚀 Getting Started
1.  Open the project in Unity.
2.  Open the scene `Assets/Scenes/SampleScene.unity`.
3.  Press **Play** to start the fight!
