# Electric Tug — CyberHex Glass Cargo Edition (V7)

Drop-in replacement for V6 (same folder name and script GUIDs, so just overwrite the files).

## What changed in V7
- **True hexagonal cargo tube**: every glass panel is a mitered prism cut from one hexagon profile, so panels meet edge-to-edge with no gaps. Glowing hex ribs, hex front bulkhead.
- **Hex rear hatch**: frame, glass, glow outline, latch and tail lamps are all the same hexagon as the bay. Convex hex collider.
- **No overlapping parts**: every visible piece touches its neighbours instead of sharing space (checked offline: 0 overlaps), so no z-fighting.
- **Futuristic nose**: sloped hex cowling, hex headlights (with real spotlights while driving), glowing hex grille, hood stripes, hex bumpers with light bars.
- **Clear driver view**: the big black steering wheel disc is replaced by a tiny open hex yoke set low on a low dash. Raked windshield.
- **Animated rig**: wheels roll, front wheels steer, the yoke turns. Hex hub caps and hex wheel arches.
- **Physics**: the tug now rides on four wheel spheres (no more sinking into the ground); bay walls use exact convex hex colliders; cab, nose and bumpers have colliders.
- **No new keybinds.**

## Setup
1. Overwrite the `ElectricTug_CyberHex_V6` folder in your project with this one and let Unity compile.
2. Menu: **GameObject → Logistics Game → Create CyberHex Glass Cargo Tug V7**.
3. Generated meshes/materials/prefab land in `Assets/ElectricTug_CyberHex_V7/`. Replace your old tug in the scene with the new prefab.

## Controls (unchanged)
F enter/exit · Z hatch · W/S drive · A/D steer
