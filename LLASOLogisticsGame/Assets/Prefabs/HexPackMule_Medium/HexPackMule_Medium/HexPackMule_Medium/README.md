# CyberHex Pack Mule - Claw Crane (Medium)

Medium vehicle in the CyberHex family (large = Electric Tug, small = Gorilla Cart).
A stand-on mule with a hex glass cargo bin and a boom crane carrying a **sideways-chomping hex claw**.
Colours, hexagons and glow match the large tug.

## Setup
1. Overwrite your `HexPackMule_Medium` folder with this one and let Unity compile.
2. Menu: **GameObject -> Logistics Game -> Create CyberHex Pack Mule Claw Crane (Medium)**.
3. Meshes/materials/prefab (`HexPackMule_ClawCrane.prefab`) are saved to `Assets/HexPackMule_Medium/`.
   Delete the old mule from your scene and drop in the new prefab.

## Controls
- **F** step on / off the platform (stand next to the mule)
- **W / S** drive, **A / D** steer
- **Z (tap)** load cycle (works while mounted or standing next to it)
  1. tap: the boom lowers the claw to the floor and the two jaws swing open sideways. There is no floor while it is open,
     so nothing is pushed away. Drive up to the boxes.
  2. tap: the jaws chomp shut, a magnet field pulls the boxes in, the boom lifts and swings the claw up and over the bin,
     the jaws open and the boxes drop in. The claw parks itself.
- **Z (hold ~0.6 s)** unload: the claw swings over the bin, takes the boxes out, carries them to the floor in front of the
  mule, opens and sets them down.
- While the claw is not parked the mule is speed-limited (`clawSpeedFactor`).

## How it works (no rolling / tilting needed)
Boxes are carried by the claw: when the jaws close, every loose rigidbody inside the pickup zone is pulled into the claw and
held (kinematic) until the claw opens. Gravity does the rest. Nothing depends on friction or on boxes sliding.
Tune the pickup zones, `maxCarried` and `pullSeconds` on `HexPackMuleController`.

### What the claw is allowed to grab
Only real cargo: the claw ignores the player, itself, anything with vehicle-like scripts or names (`ignoreKeywords`:
Tug, Cart, Mule, Vehicle, Truck, Forklift, Player), anything bigger than `maxCarrySize` (1.3 m), anything with more than
`maxCollidersPerObject` colliders, and anything whose centre is not really inside the pickup zone.
For a guaranteed result, set **`requiredTag`** to your cargo-box tag and/or **`carryLayers`** to your cargo-box layer.

## View
You stand on the rear platform: tower behind you, low hex yoke at your waist, glass guards, see-through bin. Eye height assumes
a standard 1.8 m capsule player; move **Player Stand Mount** up/down to adjust.
