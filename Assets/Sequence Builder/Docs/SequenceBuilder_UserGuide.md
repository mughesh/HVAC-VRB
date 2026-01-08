# Sequence Builder: User Guide

This guide explains how to set up your Unity scene and create training sequences using the Sequence Builder tool.

## 1. Prerequisites
*   **Unity 6.0** or higher.
*   **AutoHands** package installed.
*   **Sequence Builder** package imported.

## 2. Scene Setup
1.  **Add the Manager**: Drag the `SequenceManager` prefab from `VRTrainingKit/Prefabs` into your scene. 
    *   *This object contains the Sequence Controller and all necessary Step Handlers.*
2.  **Setup Player**: Ensure you have an `AutoHandPlayer` rig in the scene.

## 3. Tagging Objects
The system uses **Tags** to identify interactive objects.
1.  Select your object in the hierarchy.
2.  Set the Tag using the dropdown (Standard Unity Tags):
    *   `grab` -> For tools, parts, debris.
    *   `screw` -> For valves, screws, bolts.
    *   `snap` -> For sockets/place points.
    *   `knob` -> For rotating dials/controls.
    *   *(Note: Custom Conditions do not require specific tags)*

## 4. Auto-Setup (The Easy Way)
1.  Go to `Sequence Builder > Setup Assistant` in the top menu.
2.  **Scan Tab**: Click **Scan Scene**.
    *   The tool will list all objects found with the tags above.
3.  **Configure Tab**: Select a Profile for each group.
    *   *Example*: Select "Default Screw Profile" for your screws.
4.  Click **Apply Components**.
    *   *The tool automatically adds Rigidbody, Grabbable, and Controller scripts to your objects.*

## 5. Creating a Sequence
1.  **Create Asset**: Right-click in Project view -> `Create > Sequence Builder > Training Sequence`.
2.  **Open Editor**: Go to the **Sequence Tab** in the Setup Window.
3.  **Load Asset**: Drag your new Sequence Asset into the "Current Sequence" slot.

### Building the Structure
A Sequence is a hierarchy:
*   **Module** (e.g., "Main Task")
    *   **Task Group** (e.g., "Disassembly")
        *   **Step** (The actual action)

### Adding Steps
Click **+ Step** inside a Task Group and choose the action type:

| Step Type | Description |
| :--- | :--- |
| **Grab Object** | User must pick up a specific item. |
| **Snap Object** | User must place an item into a socket. |
| **Tighten / Loosen** | User must rotate a screw to the limit. |
| **Turn Knob** | User must rotate a knob to a specific value. |
| **Wait for Condition** | Sequence waits for a custom script to return true. |

**Assigning Targets**:
*   Drag the specific GameObject from the scene into the **Target Object** slot for the step.
*   *Tip*: The system will validate if you dragged the wrong type (e.g., putting a Valve into a Grab step).

## 6. Runtime Testing
1.  Select the `SequenceManager` in your scene.
2.  Select the `ModularTrainingSequenceController` component on the manager.
3.  Assign your **Training Sequence Asset** to the "Current Program" field.
4.  Press **Play**.
5.  The system will guide you through the steps in order.
