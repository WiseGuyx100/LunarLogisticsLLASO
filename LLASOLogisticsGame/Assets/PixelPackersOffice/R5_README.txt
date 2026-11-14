PIXEL PACKERS R5 - FACILITY, OFFICE AND ROVER

This is an incremental update for the existing project with R3 and R4 imported.
It does not contain or overwrite scene files. Import all files and wait for Unity
to finish compiling. The new menu items end in (R5).

INSTALL / APPLY
1. Exit Play Mode. Open GameTest.
2. Tools > Pixel Packers > Repair Facility and Trees (R5).
   This also installs office interactions, balances the sky, lights the warehouse
   emissive panels, and connects the previously imported Flame Pack.
3. Save GameTest (Ctrl+S).
4. Open Moon Test. Tools > Pixel Packers > Upgrade Moon Rooms (R5). Save.
5. Open Lunar Service Level. Tools > Pixel Packers > Repair Underground Layout (R5).
   Save. Do NOT use Build Lunar Preview: it recreates scenes.
6. Enter Play Mode again to load the updated rover code.

CHANGES
- Downloaded Heavy Steel Truss Beam, charcoal/grey steel, placed across roof bays.
  Original roof-level steel triangles are replaced; lower geometry is retained.
- Two symmetrical round duct runs with hangers, eight flipped fluorescent fixtures
  with actual downward spotlights, high warehouse panels with inward light.
- Photographed 2K partly cloudy sky; balanced ambient/sun/reflections.
- Real painted plaster diffuse and normal maps for facility wall panels.
- Visible overhead cable bundles and rack drops in Equipment and power.
- Low lunar rocks up to 0.65 m get a smooth scripted chassis rise. Taller rocks
  still block. Holding Shift builds speed over four seconds; release eases off.
  This is a scripted driving controller, not a WheelCollider vehicle simulation.
- Aim at chair, press E for a short first-person sit/stand transition.
- Aim at cabinet drawer or wardrobe door, press E to open/close.
- Glass-fronted suit-bay doors, round keyed latches (E toggles open/closed),
  corrected EMU facing, restored embedded EMU patch textures/material colours,
  and a subtle woven finish on the Apollo suit's light fabric material.
  The latch is visual and interactive; there is no inventory/key requirement.
- Recessed hygiene partition, wash fixture alignment, clearer bike space and a
  shorter medical privacy partition. Scene/cargo positions elsewhere are retained.

LIMITATIONS / VERIFICATION
C# compiled against Unity 6000.6.2f1 assemblies. Model geometry/material names and
package texture import settings were checked. Unity Play Mode and final scene
lighting have NOT been verified here. The first-person seat transition moves the
view/player; it is not a rigged third-person body animation. The baked office clock
has been left unchanged. The EMU source has patch maps and material colours but
no complete cloth texture set; those available source finishes are restored.
The Apollo download's 62-tile artwork does not match the rigged model's three-tile
UV layout. A neutral cloth finish is used instead; the complete original artwork
still requires a matching model/texture set. This update does not claim to restore it.

CHECK AFTER APPLYING
- Console has no red errors; R5 menu visible.
- Both duct runs and trusses sit below the roof; ceiling lights face down.
- Office drawer handles move with drawers; E stands up safely.
- Drive slowly over a small rock, then hold Shift; larger rocks still stop rover.
- Check Moon Test and Lunar Service Level separately, then save each scene.

ASSET SOURCES / CREDITS
Heavy Steel Truss Beam by lmsteel, modified colour/detail density for this project:
https://sketchfab.com/3d-models/heavy-steel-truss-beam-e1db8560e5474bc3a50e66ebeee42535
Painted Plaster Wall by Amal Kumar, Poly Haven, CC0:
https://polyhaven.com/a/painted_plaster_wall
Kloofendal 48d Partly Cloudy (Pure Sky), Greg Zaal / Jarod Guest, Poly Haven, CC0:
https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky
Suit models and their embedded maps are from your existing project.
Cloth normal detail: Cotton Jersey, Poly Haven, CC0 (visual approximation):
https://polyhaven.com/a/cotton_jersey

OPTIONAL DOWNLOADS - NOT INCLUDED OR INSTALLED BY THIS PACKAGE
Round industrial launch button with press blend shape:
https://sketchfab.com/3d-models/industrial-button-a7e7c1772d764c3b913cccac66932236
Entrance post replacement (bollard pack):
https://sketchfab.com/3d-models/bollard-pack-2851f2df1cd948f3adc5dacb5f268a74
Office chair:
https://sketchfab.com/3d-models/office-chair-69208f9465c346308a2bf423c7e5b8f6
Office filing cabinet:
https://sketchfab.com/3d-models/office-filing-cabinet-free-cd6f9016db8644129af1b6026f9e46bd
Desk lamp (clamps to existing desk, little space required):
https://polyhaven.com/a/desk_lamp_arm_01
Alternative wall finish for concrete service areas:
https://polyhaven.com/a/precast_concrete_wall

COMPACT WAREHOUSE DECOR IDEAS
Security camera above the entrance, aimed across the cargo aisle:
https://polyhaven.com/a/security_camera_01
Fire alarm pull station on wall beside the personnel exit:
https://polyhaven.com/a/fire_alarm
Power box on the service wall, above floor level:
https://polyhaven.com/a/power_box_01
Fire extinguisher next to the exit, outside the marked walking lane:
https://sketchfab.com/3d-models/fire-extinguisher-game-ready-7f7489290b3e4d1eb30d43b025fa8b83
First aid kit on a shallow shelf near the office:
https://sketchfab.com/3d-models/first-aid-kit-a6cc12f1d4a14a0fb3aed520cb196549
Prefer 2K textures and FBX or GLB model downloads. Preserve source credits/license
information for Sketchfab assets. These are recommendations to download next;
the package cannot install files you have not downloaded/imported yet.
