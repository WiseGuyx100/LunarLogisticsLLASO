# CyberHex Pack Mule Forklift (Medium)

Medium vehicle in the CyberHex family (large = Electric Tug, small = Gorilla Cart).
A stand-on scoop forklift: a Pac-Man style hex glass scoop on a mast picks boxes up off the floor and swallows them into a
big see-through hexagon cargo bin. Colours, hexagons and glow match the large tug.

## Setup
1. Overwrite your `HexPackMule_Medium` folder with this one and let Unity compile.
2. Menu: **GameObject -> Logistics Game -> Create CyberHex Pack Mule Forklift (Medium)**.
3. Meshes/materials/prefab (`HexPackMule_Forklift.prefab`) are saved to `Assets/HexPackMule_Medium/`.
   Delete the old mule from your scene and drop in the new prefab.

## Controls
- **F** step on / off the platform (stand next to the mule)
- **W / S** drive, **A / D** steer
- **Z (tap)** scoop cycle (works while mounted or standing next to it)
  1. tap: scoop lowers to the floor, jaws open - drive forward into the boxes
  2. tap: jaws chomp shut, scoop lifts, tips back and swallows everything into the hex bin, then stows. Repeat as needed.
- **Z (hold ~0.6 s)** unload: scoop lowers, bin tilts, boxes slide back out through the scoop. Reverse slowly to lay them down.
  Hold Z again to level the bin and stow the scoop.
- While the scoop is not stowed the mule is speed-limited (`scoopSpeedFactor`).

## View
You stand on the rear platform: tower behind you, low hex yoke at your waist, glass guards, see-through bin. Nothing sits in
front of your eyes. Eye height assumes a standard 1.8 m capsule player; move **Player Stand Mount** up/down to adjust.

## Tuning
All scoop angles/speeds are on `HexPackMuleController` (jaw angle, load tilt, unload bin angle, animation speed).
Cargo physics are driven by moving colliders, so if boxes are being squeezed out, lower `animationSpeed`.
