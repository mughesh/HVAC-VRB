# Sequence Builder: User Guide

This guide explains how to set up your Unity scene and create training sequences using the Sequence Builder tool.

## 1. Prerequisites
*   **Unity 6.0** or higher.
*   **AutoHands** package installed.
*   **Sequence Builder** package imported.

## 2. Scene Setup
The tool provides automated setup for essential components.

1.  **Add Sequence Manager and Autohands Player Controller**:
    *   Go to `Sequence Builder > Create Sequence Manager`.
    *   *Alternatively, check the "Setup" tab in the Setup Window.*
    *   This adds the `SequenceManager` prefab which contains the Controller, Step Handlers, and Scene Registry and the Autohands Player Controller.



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
You can create a new Sequence Asset in two ways:

*   **Option A (Project View)**: Right-click in Project view -> `Create > Sequence Builder > Training Sequence`.
*   **Option B (Setup Window)**: Open `Sequence Builder > Setup Assistant`, go to the **Sequence Tab**, and click **New**.


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
Before playing, you must link your sequence to the scene controllers.

1.  Select the `SequenceManager` object in your scene.
2.  **Assign Controller**: Find the `ModularTrainingSequenceController` component and assign your **Training Sequence Asset** to the "Current Program" field.
3.  **Assign Registry**: Expand `SequenceManager` to find the `SceneRegistry` child object.
    *   Find the `SequenceRegistry` component.
    *   Assign the same **Training Sequence Asset** to it.
4.  Press **Play**.
5.  The system will guide you through the steps in order.

## 7. Custom Conditions (Wait For Script)
For complex logic not covered by standard interactions (e.g., "Wait for wire cut"), you can use the **Wait for Condition** step type.

1.  **Create Script**: Create a new C# script inheriting from `BaseSequenceCondition`.
2.  **Logic**: Calling `SetConditionMet()` will satisfy the step.
3.  **Setup**: Add this script to any GameObject in the scene and assign it to the Step's **Target Object** field.

**Example Template:**
```csharp
public class MyCustomCondition : BaseSequenceCondition
{
    void Update()
    {
        // Example: Wait for Space Key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetConditionMet(); // Completes the step
        }
    }
}
```
