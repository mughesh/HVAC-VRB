# Sequence Builder: Overview
> **Version 1.0 | Framework: AutoHands**

## What is it?
The **Sequence Builder** is a low-code tool designed to help you create complex VR training scenarios without writing code. It bridges the gap between your 3D models and the **AutoHands** physics engine.

Instead of manually adding scripts and tweaking physics settings for every single screw or tool, this system allows you to:

1.  **Tag** your 3D objects (e.g., "This is a valve").
2.  **Scan** the scene to automatically find them.
3.  **Apply** pre-made behavior profiles in one click.
4.  **Sequence** the training steps (e.g., "First Loosen Valve A, then Grab Tool B").

## Key Features

### Low-Code Workflow
No need to program C# scripts for standard interactions. The **Interaction Setup Window** handles the heavy lifting. You simply define *what* an object is, and the tool configures *how* it works.

### AutoHands Integration
Built specifically for high-fidelity hand physics. Interactions feel natural because they utilize the AutoHands grabbing, weight, and collision systems under the hood, but you don't need to be an AutoHands expert to set them up.

### Profile System
Define your settings **once**, apply them **everywhere**.
*   Want all "Small Valves" to turn easily? Create a "Small Valve Profile".
*   Want "Heavy Tools" to be hard to lift? Create a "Heavy Tool Profile".
*   If you change the profile later, you can re-apply it to all objects instantly.

### Step-by-Step Sequencing
Create structured training programs with a visual editor. Break down complex procedures into:
*   **Modules** (e.g., "Pump Maintenance")
*   **Tasks** (e.g., "Remove Cover")
*   **Steps** (e.g., "Unscrew bolt 1", "Unscrew bolt 2")

The system automatically tracks user progress and prevents them from skipping ahead if desired.

---

## Available Interactions

The tool currently supports the following AutoHands-based interactions:

### 1. Grabbable Objects
*   **Use Case**: Tools, parts, debris, or portable equipment.
*   **Features**:
    *   Configurable weight and throw power.
    *   One-handed or two-handed grabbing.
    *   Highlight effects on hover.

### 2. Snapping / Sockets
*   **Use Case**: Placing a fuse in a box, putting a tool back on a shadow board, or assembling parts to a machine.
*   **Features**:
    *   Magnetic snap zones.
    *   Validates correct placement (e.g., "Wrong Fuse" rejection).
    *   Visual "ghost" indicators for guidance.

### 3. Screws & Bolts
*   **Use Case**: Industrial bolts, nuts, and rotating controls.
*   **Unique Feature**: Uses a realistic **Tighten / Loosen** workflow.
    *   **Locked State**: Cannot be turned.
    *   **Turning**: Requires circular hand motion.
    *   **Threads**: Moves in/out as it turns.
    *   **Detachable**: Can fall off when fully unscrewed (great for disassembly tasks).

### 4. Knobs & Dials
*   **Use Case**: Control panels, volume dials, or gauges.
*   **Features**:
    *   Restricted rotation (e.g., Min 0° to Max 180°).
    *   Notched steps (optional).
    *   Reading values (0.0 to 1.0) for sequence logic.

### 5. Wait for Step Condition
*   **Use Case**: Waiting for a custom script event (e.g., "Wait for wire cut", "Wait for button press").
*   **Features**:
    *   Allows any custom logic to block the sequence.
    *   Great for interactions not covered by standard physics profiles.

---

## Why Use This?
*   **Speed**: Setup a room full of interactables in minutes, not hours.
*   **Consistency**: All valves behave the same way; no "magic numbers" scattered across different objects.
*   **Clarity**: The Sequence Editor confirms exactly what the user needs to do next, removing ambiguity from your training design.
