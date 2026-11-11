PIXEL PACKERS / LUNAR HABITAT

START HERE: EXTRACT THE OUTER ZIP FIRST
Right-click the downloaded ZIP in Windows > Extract All > Extract.
Do not select a unitypackage from a file browser whose address still ends in .zip.

IMPORT IN THE UNITY EDITOR (not in Unity Hub)
1. Open the recipient's Unity 6 project. URP or Built-in render pipeline supported.
2. Assets > Import Package > Custom Package: choose PixelPackers_LunarHabitat.unitypackage
   from the EXTRACTED folder, then Import All. Wait for scripts to compile.
3. In the Project window open Assets/PixelPackersLunarHabitat.
4. Select OPEN_SCENE and press the Open Scene button in the Inspector.
   Alternatively: Tools > Pixel Packers Lunar Habitat > Open Scene.
5. The first open creates editable geometry and saves meshes/materials under Generated.
6. Press Play and click the Game view: WASD move, mouse look, Shift sprint, Space jump.
   Escape releases the cursor. Stop Play to edit the architecture.

IF UNITY STILL SAYS COULDN'T DECOMPRESS PACKAGE
A raw Assets fallback is included. Close Unity, copy the extracted
Assets/PixelPackersLunarHabitat folder into THEIR project's existing Assets folder.
Do not create Assets/Assets. Reopen the project, allow compilation, then use Open Scene.
The repaired menu can be copied the same way using Assets/PixelPackersMenuKit.
Do not copy ProjectSettings, Library, Temp, or an entire different project.

WHAT YOU GET
- Glass dome: 96 m diameter, 36 m height; structural ribs and a loading entrance.
- Flat work area: 160 x 240 m with invisible site boundaries.
- Receiving airlock: 14 m wide, 8 m tall, with a clear entrance and delivery apron.
- Receiving pad: 26 x 22 m; marked central cargo aisle.
- Six storage racks with sample containers and water/food/medical colors.
- Raised operations deck: 48 x 18 m at 4.4 m high, accessible by a 5 m wide ramp.
- Operations observation room, guardrails, utility tanks, lights, and star field.
- Standalone preview walker, collision floors/walls, and editable scene hierarchy.
- Custom regolith texture and procedural mountain landscape; no Asset Store download needed.

IMPORTANT SCOPE
This is an architectural blockout and walkthrough, not the packing gameplay system.
The sample containers are static scenery. Pickup, manifest, mission logic, VR rigs and
multiplayer are not included. Attach the recipient game's systems after approving the layout.
The supplied mountain mesh preserves the flat-site/mountain-backdrop style; it is NOT a
redistribution of the third-party Lunar Landscape 3D Asset Store package.
Your existing game project and original lunar terrain are unchanged.

EDIT / FORM THE HABITAT
Open Habitat Layout in the Hierarchy. Every rib, shelf, crate, wall, ramp and platform
is an ordinary editable scene object. Move and resize individual objects as you like.
Select Lunar Habitat Blueprint to find Rebuild Habitat Layout and Save Editable Scene.
Rebuild REPLACES edits under Habitat Layout; it leaves other root objects alone.
Generated assets are saved in this project so scene edits survive reopening.
To view an empty lunar base, disable Build Habitat on the blueprint, then Rebuild.
Keep world scale at 1,1,1. One Unity unit represents approximately one metre.
For Unity Terrain assets, adjust TerrainData dimensions instead of nonuniform Transform scale.

SUGGESTED NEXT DESIGN PASS
1. Walk from the airlock to each shelf, checking aisle width and visibility.
2. Move shelving to suit the team's actual cargo dimensions.
3. Reserve a straight receiving-to-storage route before adding decorative furniture.
4. Replace the static sample cases with actual CTBs and storage-zone components.
5. Add airlock interactions and an operations manifest screen after the layout is approved.

MENU PACKAGE
PixelPackers_MenuOnly_Repaired.unitypackage is also supplied. It contains the same
portable menu UI as before with corrected archive directory entries. Its setup instructions
are in Assets/PixelPackersMenuKit/README_IMPORT.txt after import.

VALIDATION
Runtime scripts compiled against Unity 6000.6.2f1 using both input backends.
Editor shortcuts/inspectors compiled against installed editor references.
Archives use gzip + classic USTAR headers with explicit directories and checked GUIDs.
No Unity import or scene walkthrough has been run here; test in a copy of the recipient project.
