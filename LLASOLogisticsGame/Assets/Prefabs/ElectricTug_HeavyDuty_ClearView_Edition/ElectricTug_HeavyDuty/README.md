# Electric Tug — Clear-View Edition

This is a replacement for the earlier Heavy Duty Edition. Remove the previous tug instance from the scene before creating this version.

## Install
1. Back up the Unity project.
2. Extract this folder into `Assets` (keep the `Scripts` and `Editor` folders together under `Assets/ElectricTug_HeavyDuty_ClearView`).
3. Wait for Unity to compile.
4. Use **GameObject → Logistics Game → Create Electric Tug (Heavy Duty Clear-View Edition)**.
5. Remove any older tug object from the scene so there is only one active tug.

## Changes
- First-person avatar renderers are hidden while seated, then their original enabled states are restored on exit. This prevents the player's own head/body mesh from blocking the camera.
- Driver seat mount is lowered/positioned to put the camera near the cab's clear viewing zone (based on the supplied FPSMovement script's typical camera local Y of about 1.6 m).
- Front panoramic windshield and left/right/rear windows are transparent decorative panes with no colliders. The cab has four corner posts and a roof, with no center post across the main view.
- Steering wheel and column are kept below the expected seated eye line; hood is low and forward.
- Four approach triggers let the player enter from the left, right, front, or rear. Press F while within an approach zone; entry does not require facing a particular direction. The player is aligned to the vehicle forward direction when seated.
- Tug remains heavy-duty (4,200 kg) with damping and a low center of mass.
- Existing player pickup/inventory scripts are not changed. Tug itself has no cargo pickup controls.

## Controls
- F: enter / exit
- W/S or Up/Down: drive / reverse
- A/D or Left/Right: steer

## Notes
- The transparent window appearance depends on the project's render pipeline and shader support. The builder configures transparency for URP Lit and Standard shaders.
- The camera alignment is estimated from the supplied FPSMovement code; verify in Play Mode because camera hierarchy, avatar mesh, and local offsets vary between projects.
- This package has been source-checked, but not compiled or run inside the user's Unity Editor. If the camera remains obstructed, send a screenshot of the player hierarchy (showing the camera child and its local Transform) and the cab in Scene view.
