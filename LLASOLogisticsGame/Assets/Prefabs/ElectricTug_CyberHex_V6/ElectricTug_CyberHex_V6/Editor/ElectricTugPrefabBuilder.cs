#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Programmatically generates the CyberHex Glass Cargo Electric Tug (V7).
///
/// V7 highlights
///  - Cargo bay is a single true hexagonal tube: every glass panel is a mitered prism, so panels meet
///    edge-to-edge with no gaps and no overlaps. Glowing hex ribs, hex bulkhead and a hex rear hatch.
///  - No two visible parts share the same volume or the same coplanar surface (verified offline).
///  - Sloped, hex-themed nose with hex headlights, glowing grille, and a hex bumper.
///  - Tiny, low, open hex steering yoke + low dash so the driver can see the road.
///  - Raked windshield, hex wheel arches, hex hub caps, real headlight Lights, rolling/steering wheels.
///
/// All custom meshes are saved as assets in Assets/ElectricTug_CyberHex_V7 so the prefab stays intact.
/// </summary>
public static class ElectricTugPrefabBuilder
{
    // ------------------------------------------------------------------
    // Layout constants (root space: +X right, +Y up, +Z forward, origin at ground level)
    // ------------------------------------------------------------------
    private const string AssetFolderParent = "Assets";
    private const string AssetFolderName = "ElectricTug_CyberHex_V7";
    private const string AssetFolder = AssetFolderParent + "/" + AssetFolderName;

    private const float CH_TOP = 0.40f;      // chassis top surface
    private const float CH_BOT = 0.14f;      // chassis bottom surface
    private const float CH_HW = 0.84f;       // chassis half width
    private const float Z_FRONT = 2.04f;     // front face of the nose / chassis
    private const float Z_REAR = -4.44f;     // rear face of the last cargo rib / chassis

    private const float BELT = 0.88f;        // bottom of all cab glass
    private const float ROOF_BOT = 1.96f;    // underside of cab roof
    private const float ROOF_TOP = 2.04f;    // top of cab roof (also top of the cargo hex)
    private const float FLOOR_TOP = 0.475f;  // cab floor == cargo floor surface

    private const float WHEEL_R = 0.37f;
    private const float WHEEL_HALF_W = 0.12f;
    private const float WHEEL_X = 0.975f;

    private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

    [MenuItem("GameObject/Logistics Game/Create CyberHex Glass Cargo Tug V7", false, 17)]
    private static void CreateTug()
    {
        meshCache.Clear();
        EnsureFolder();

        // ---------------------------------------------------------
        // 1. MATERIAL PALETTE (saved as assets so references stay valid)
        // ---------------------------------------------------------
        Material whiteBody = Palette("WhiteBody", MakeMaterial("Tug White Body", new Color(0.94f, 0.96f, 0.99f), 0.2f, 0.7f));
        Material blueAccent = Palette("BlueAccent", MakeMaterial("Tug Blue Accent", new Color(0.06f, 0.35f, 0.90f), 0.4f, 0.8f));
        Material purpleAccent = Palette("PurpleAccent", MakeMaterial("Tug Purple Accent", new Color(0.50f, 0.15f, 0.80f), 0.3f, 0.6f));
        Material greenGlow = Palette("GreenGlow", MakeEmissiveMaterial("Tug Green Glow", new Color(0.0f, 0.98f, 0.45f), 3.0f));
        Material darkChassis = Palette("DarkChassis", MakeMaterial("Tug Carbon Frame", new Color(0.10f, 0.12f, 0.16f), 0.5f, 0.5f));
        Material rubberTire = Palette("RubberTire", MakeMaterial("Tug Rubber Tire", new Color(0.04f, 0.04f, 0.05f), 0.0f, 0.2f));
        Material seatLeather = Palette("SeatLeather", MakeMaterial("Tug Seat Upholstery", new Color(0.08f, 0.09f, 0.12f), 0.0f, 0.3f));
        Material sciFiGlass = Palette("SciFiGlass", MakeGlassMaterial("Tug SeeThrough Glass", new Color(0.22f, 0.62f, 0.88f, 0.22f)));
        Material chromeHub = Palette("ChromeHub", MakeMaterial("Tug Chrome Hub", new Color(0.88f, 0.90f, 0.94f), 0.9f, 0.9f));
        Material lampGlow = Palette("LampGlow", MakeEmissiveMaterial("Tug Headlight Glow", new Color(0.85f, 0.95f, 1.0f), 3.5f));
        Material redGlow = Palette("RedGlow", MakeEmissiveMaterial("Tug Tail Glow", new Color(1.0f, 0.08f, 0.10f), 3.0f));

        PhysicsMaterial slick = MakePhysicsMaterial();

        // ---------------------------------------------------------
        // 2. ROOT, RIGIDBODY, ORGANISING GROUPS (all groups sit at the root origin,
        //    so child local coordinates == root-space coordinates)
        // ---------------------------------------------------------
        GameObject root = new GameObject("Electric Tug - CyberHex Glass Cargo Edition V7");
        if (SceneView.lastActiveSceneView != null)
            root.transform.position = SceneView.lastActiveSceneView.pivot;
        else
            root.transform.position = Vector3.zero;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        ConfigurePhysicsBody(rb);
        Transform t = root.transform;

        Transform chassisG = MakeChild(t, "Chassis", Vector3.zero);
        Transform cabG = MakeChild(t, "Cab", Vector3.zero);
        Transform noseG = MakeChild(t, "Nose", Vector3.zero);
        Transform cargoG = MakeChild(t, "Cargo Bay", Vector3.zero);
        Transform hatchG = MakeChild(t, "Hatch", Vector3.zero);
        Transform wheelsG = MakeChild(t, "Wheels", Vector3.zero);
        Transform trimG = MakeChild(t, "Trim", Vector3.zero);
        Transform lightsG = MakeChild(t, "Lights", Vector3.zero);
        Transform collidersG = MakeChild(t, "Colliders", Vector3.zero);
        Transform triggersG = MakeChild(t, "Entry Triggers", Vector3.zero);

        // ---------------------------------------------------------
        // 3. CHASSIS
        // ---------------------------------------------------------
        Box(chassisG, "Lower Chassis", darkChassis, -CH_HW, CH_HW, CH_BOT, CH_TOP, Z_REAR, Z_FRONT);
        Box(chassisG, "Underbody Battery Skid", darkChassis, -0.60f, 0.60f, 0.10f, CH_BOT, -3.80f, 1.60f);

        float[,] stripZ = { { -4.30f, -2.20f }, { -1.00f, 0.78f } };
        for (int s = -1; s <= 1; s += 2)
        {
            float sx0 = s > 0 ? CH_HW : -CH_HW - 0.01f;
            float sx1 = s > 0 ? CH_HW + 0.01f : -CH_HW;
            for (int i = 0; i < 2; i++)
                Box(trimG, "Chassis Glow Strip " + (s > 0 ? "R" : "L") + i, greenGlow, sx0, sx1, 0.20f, 0.26f, stripZ[i, 0], stripZ[i, 1]);
        }

        // ---------------------------------------------------------
        // 4. CAB: tub, raked windshield, glass, roof (no overlapping pieces)
        // ---------------------------------------------------------
        Box(cabG, "Cab Floor", darkChassis, -0.70f, 0.70f, CH_TOP, FLOOR_TOP, -0.40f, 0.80f);
        Box(cabG, "Tub Panel L", whiteBody, -0.84f, -0.70f, CH_TOP, BELT, -0.50f, 0.80f);
        Box(cabG, "Tub Panel R", whiteBody, 0.70f, 0.84f, CH_TOP, BELT, -0.50f, 0.80f);
        Box(cabG, "Cab Rear Wall", whiteBody, -0.70f, 0.70f, CH_TOP, BELT, -0.50f, -0.40f);

        // Low-profile dash (top flush with the belt line) + small display.
        Box(cabG, "Dashboard", darkChassis, -0.70f, 0.70f, 0.64f, BELT, 0.32f, 0.80f);
        Box(cabG, "Dash Display Bezel", darkChassis, -0.28f, 0.28f, BELT, 0.93f, 0.62f, 0.70f);
        Box(cabG, "Dash Display Screen", greenGlow, -0.20f, 0.20f, 0.89f, 0.915f, 0.61f, 0.62f);

        // Raked A-pillars (parallelogram prisms), rear pillars, glass that butts exactly against them.
        const float pw = 0.06f;
        const float zc0 = 0.78f;   // windshield base centre-line (z at BELT)
        const float zc1 = 0.58f;   // windshield top centre-line (z at ROOF_BOT)
        for (int s = -1; s <= 1; s += 2)
        {
            string side = s > 0 ? "R" : "L";
            float px0 = s * 0.76f - 0.03f, px1 = s * 0.76f + 0.03f;
            float lo = Mathf.Min(px0, px1), hi = Mathf.Max(px0, px1);

            Box(cabG, "Rear Pillar " + side, blueAccent, lo, hi, BELT, ROOF_BOT, -0.48f, -0.42f);

            Vector2[] pillar = {
                new Vector2(zc0 - pw / 2, BELT), new Vector2(zc0 + pw / 2, BELT),
                new Vector2(zc1 + pw / 2, ROOF_BOT), new Vector2(zc1 - pw / 2, ROOF_BOT) };
            PrismX(cabG, "Front Pillar " + side, blueAccent, "FrontPillar" + side, pillar, lo, hi);

            Vector2[] sideGlass = {
                new Vector2(-0.42f, BELT), new Vector2(zc0 - pw / 2, BELT),
                new Vector2(zc1 - pw / 2, ROOF_BOT), new Vector2(-0.42f, ROOF_BOT) };
            PrismX(cabG, "Side Window " + side, sciFiGlass, "SideGlass" + side, sideGlass, s > 0 ? 0.75f : -0.77f, s > 0 ? 0.77f : -0.75f);
        }
        Vector2[] wind = {
            new Vector2(zc0 - 0.01f, BELT), new Vector2(zc0 + 0.01f, BELT),
            new Vector2(zc1 + 0.01f, ROOF_BOT), new Vector2(zc1 - 0.01f, ROOF_BOT) };
        PrismX(cabG, "Windshield", sciFiGlass, "Windshield", wind, -0.73f, 0.73f);
        Box(cabG, "Rear Window", sciFiGlass, -0.73f, 0.73f, BELT, ROOF_BOT, -0.46f, -0.44f);

        Box(cabG, "Cab Roof Shell", whiteBody, -0.84f, 0.84f, ROOF_BOT, ROOF_TOP, -0.52f, 0.66f);
        Box(cabG, "Roof Purple Stripe", purpleAccent, -0.24f, 0.24f, ROOF_TOP, ROOF_TOP + 0.01f, -0.35f, 0.45f);
        {
            Mesh beacon = Cached("Beacon", () => PrismZ(RegHex(0, 0, 0.07f), 0f, 0.07f, "Beacon"));
            MeshObj(cabG, "Roof Beacon", beacon, greenGlow, new Vector3(0f, ROOF_TOP, -0.44f), Quaternion.Euler(-90f, 0f, 0f));
        }

        // Seat (base + cushion + leaned backrest, all stacked, no overlaps)
        Box(cabG, "Seat Base", darkChassis, -0.24f, 0.24f, FLOOR_TOP, 0.55f, -0.30f, 0.02f);
        Box(cabG, "Driver Seat Cushion", seatLeather, -0.30f, 0.30f, 0.55f, 0.66f, -0.34f, 0.06f);
        Vector2[] back = { new Vector2(-0.34f, 0.66f), new Vector2(-0.26f, 0.66f), new Vector2(-0.32f, 1.12f), new Vector2(-0.40f, 1.12f) };
        PrismX(cabG, "Seat Backrest", seatLeather, "SeatBack", back, -0.28f, 0.28f);

        // ---------------------------------------------------------
        // 5. COMPACT HEX STEERING YOKE (small, low, open => clear view of the road)
        // ---------------------------------------------------------
        Vector3 yokePos = new Vector3(0f, 0.98f, 0.40f);
        Vector3 yokeN = new Vector3(0f, 0.72f, -0.69f).normalized;
        Transform yokePivot = MakeChild(cabG, "Steering Yoke Pivot", yokePos);
        yokePivot.localRotation = Quaternion.LookRotation(yokeN, Vector3.up);
        Transform yokeSpin = MakeChild(yokePivot, "Steering Yoke Spin", Vector3.zero);

        Vector2[] yokeOuter = RegHex(0, 0, 0.10f);
        Vector2[] yokeInner = OffsetPoly(yokeOuter, -0.025f);
        MeshObj(yokeSpin, "Yoke Ring", Cached("YokeRing", () => RingZ(yokeOuter, yokeInner, -0.015f, 0.015f, "YokeRing")), darkChassis, Vector3.zero, Quaternion.identity);
        MeshObj(yokeSpin, "Yoke Hub", Cached("YokeHub", () => PrismZ(RegHex(0, 0, 0.035f), -0.025f, 0.025f, "YokeHub")), blueAccent, Vector3.zero, Quaternion.identity);
        float rIn = yokeInner[1].x;
        float aIn = -yokeInner[2].y;
        float spokeC = (0.035f + rIn - 0.004f) * 0.5f;
        float spokeL = rIn - 0.039f;
        LocalBox(yokeSpin, "Yoke Spoke R", darkChassis, new Vector3(spokeC, 0f, 0f), new Vector3(spokeL, 0.016f, 0.012f));
        LocalBox(yokeSpin, "Yoke Spoke L", darkChassis, new Vector3(-spokeC, 0f, 0f), new Vector3(spokeL, 0.016f, 0.012f));
        LocalBox(yokeSpin, "Yoke Spoke Down", darkChassis, new Vector3(0f, -(0.0303f + aIn) * 0.5f, 0f), new Vector3(0.016f, aIn - 0.0303f, 0.012f));

        float stalkT = (yokePos.y - (BELT + 0.013f)) / yokeN.y;
        Beam(cabG, "Steering Stalk", darkChassis, yokePos - yokeN * 0.025f, yokePos - yokeN * stalkT, 0.035f, 0.035f);

        // ---------------------------------------------------------
        // 6. FUTURISTIC SLOPED HEX NOSE
        // ---------------------------------------------------------
        Vector2[] noseBack = HexPoly(0f, 0.66f, 0.60f, 0.84f, 0.26f);
        float frontHH = 0.17f, frontHM = 0.72f;
        float frontHF = frontHM - (0.84f - 0.60f) * (frontHH / 0.26f);   // keeps slanted edges parallel => planar faces
        Vector2[] noseFront = HexPoly(0f, 0.57f, frontHF, frontHM, frontHH);
        MeshObj(noseG, "Hex Nose Cowling", Cached("NoseLoft", () => Loft(noseBack, 0.80f, noseFront, Z_FRONT, "NoseLoft")), whiteBody, Vector3.zero, Quaternion.identity);

        // Hood stripes lie exactly on the hood surface
        System.Func<float, float> hoodY = z => 0.92f - (z - 0.80f) * (0.92f - 0.74f) / (Z_FRONT - 0.80f);
        HoodStripe(noseG, "Hood Stripe Purple", purpleAccent, "HoodPurple", -0.04f, 0.04f, hoodY);
        HoodStripe(noseG, "Hood Stripe Blue L", blueAccent, "HoodBlueL", -0.125f, -0.10f, hoodY);
        HoodStripe(noseG, "Hood Stripe Blue R", blueAccent, "HoodBlueR", 0.10f, 0.125f, hoodY);

        // Front fascia: hex headlights + glowing hex grille (each layer sits on the one below)
        Mesh lampMesh = Cached("Headlight", () => PrismZ(RegHex(0, 0, 0.11f), 0f, 0.03f, "Headlight"));
        MeshObj(noseG, "Headlight L", lampMesh, lampGlow, new Vector3(-0.38f, 0.57f, Z_FRONT), Quaternion.identity);
        MeshObj(noseG, "Headlight R", lampMesh, lampGlow, new Vector3(0.38f, 0.57f, Z_FRONT), Quaternion.identity);
        Vector3 grillePos = new Vector3(0f, 0.57f, Z_FRONT);
        MeshObj(noseG, "Grille Plate", Cached("GrillePlate", () => PrismZ(RegHex(0, 0, 0.19f), 0f, 0.015f, "GrillePlate")), darkChassis, grillePos, Quaternion.identity);
        Vector2[] gOuter = RegHex(0, 0, 0.16f);
        Vector2[] gInner = OffsetPoly(gOuter, -0.03f);
        MeshObj(noseG, "Grille Glow Ring", Cached("GrilleRing", () => RingZ(gOuter, gInner, 0.015f, 0.03f, "GrilleRing")), greenGlow, grillePos, Quaternion.identity);
        MeshObj(noseG, "Grille Glow Core", Cached("GrilleCore", () => PrismZ(RegHex(0, 0, 0.06f), 0.015f, 0.025f, "GrilleCore")), greenGlow, grillePos, Quaternion.identity);

        // Hex bumpers (front + rear) with light bars
        Vector2[] bumper = HexPoly(0f, 0.28f, 0.62f, 0.90f, 0.10f);
        MeshObj(noseG, "Front Hex Bumper", Cached("FrontBumper", () => PrismZ(bumper, Z_FRONT, 2.26f, "FrontBumper")), blueAccent, Vector3.zero, Quaternion.identity);
        Box(noseG, "Front Bumper Light Bar", lampGlow, -0.45f, 0.45f, 0.27f, 0.30f, 2.26f, 2.275f);
        MeshObj(chassisG, "Rear Hex Bumper", Cached("RearBumper", () => PrismZ(bumper, -4.62f, Z_REAR, "RearBumper")), blueAccent, Vector3.zero, Quaternion.identity);
        Box(chassisG, "Rear Bumper Light Bar", redGlow, -0.45f, 0.45f, 0.27f, 0.30f, -4.635f, -4.62f);

        // ---------------------------------------------------------
        // 7. SIDE LOGO DECALS (plate + three non-overlapping swoosh bands per side)
        // ---------------------------------------------------------
        for (int s = -1; s <= 1; s += 2)
        {
            string side = s > 0 ? "R" : "L";
            float p0 = s > 0 ? 0.84f : -0.855f, p1 = s > 0 ? 0.855f : -0.84f;
            float b0 = s > 0 ? 0.855f : -0.865f, b1 = s > 0 ? 0.865f : -0.855f;
            Box(trimG, "Logo Plate " + side, darkChassis, p0, p1, 0.50f, 0.82f, -0.35f, 0.45f);
            LogoBand(trimG, "Logo Purple " + side, purpleAccent, "LogoP" + side, 0.72f, 0.78f, b0, b1);
            LogoBand(trimG, "Logo Blue " + side, blueAccent, "LogoB" + side, 0.64f, 0.70f, b0, b1);
            LogoBand(trimG, "Logo Green " + side, greenGlow, "LogoG" + side, 0.56f, 0.62f, b0, b1);
        }

        // ---------------------------------------------------------
        // 8. TRUE HEXAGONAL GLASS CARGO TUBE
        //    One hexagon profile drives everything: glass, ribs, bulkhead, hatch.
        // ---------------------------------------------------------
        const float cargoCY = 1.22f, cargoHH = 0.785f, cargoHF = 0.58f, cargoHM = 0.90f;
        const float glassIn = -0.04f;      // glass thickness (offset inward)
        const float ribOut = 0.035f;       // ribs stand proud of the glass
        const int ribCount = 5;
        const float ribDepth = 0.10f;
        const float cargoFrontZ = -0.54f;

        Vector2[] hex = HexPoly(0f, cargoCY, cargoHF, cargoHM, cargoHH);
        Vector2[] glassOuter = hex;
        Vector2[] glassInner = OffsetPoly(hex, glassIn);
        Vector2[] ribOuter = OffsetPoly(hex, ribOut);

        float segLen = ((cargoFrontZ - Z_REAR) - ribCount * ribDepth) / (ribCount - 1);
        float[] ribZ = new float[ribCount];
        for (int i = 0; i < ribCount; i++) ribZ[i] = cargoFrontZ - ribDepth * 0.5f - i * (segLen + ribDepth);

        // Glass panels: one mitered prism per hex edge per bay. Edge 2 is the floor (opaque).
        for (int j = 0; j < ribCount - 1; j++)
        {
            float zc = cargoFrontZ - ribDepth - segLen * 0.5f - j * (segLen + ribDepth);
            for (int k = 0; k < 6; k++)
            {
                int kk = k;
                Mesh m = Cached("CargoEdge" + k, () => EdgePrismZ(glassOuter, glassInner, kk, -segLen * 0.5f, segLen * 0.5f, "CargoEdge" + kk));
                MeshObj(cargoG, "Hex " + (k == 2 ? "Deck Floor" : "Glass") + " e" + k + " bay" + j, m, k == 2 ? darkChassis : sciFiGlass, new Vector3(0f, 0f, zc), Quaternion.identity);
            }
        }

        // Ribs: white / glowing / white rings, all mitered hex rings
        Mesh ribWhiteA = Cached("RibWhiteA", () => RingZ(ribOuter, glassInner, -ribDepth * 0.5f, -0.015f, "RibWhiteA"));
        Mesh ribGlow = Cached("RibGlow", () => RingZ(ribOuter, glassInner, -0.015f, 0.015f, "RibGlow"));
        Mesh ribWhiteB = Cached("RibWhiteB", () => RingZ(ribOuter, glassInner, 0.015f, ribDepth * 0.5f, "RibWhiteB"));
        for (int i = 0; i < ribCount; i++)
        {
            Vector3 rp = new Vector3(0f, 0f, ribZ[i]);
            MeshObj(cargoG, "Hex Rib " + i + " A", ribWhiteA, whiteBody, rp, Quaternion.identity);
            MeshObj(cargoG, "Hex Rib " + i + " Glow", ribGlow, greenGlow, rp, Quaternion.identity);
            MeshObj(cargoG, "Hex Rib " + i + " B", ribWhiteB, whiteBody, rp, Quaternion.identity);
        }

        // Front bulkhead: hex glass pane inside the first rib
        MeshObj(cargoG, "Hex Glass Bulkhead", Cached("Bulkhead", () => PrismZ(glassInner, -0.0125f, 0.0125f, "Bulkhead")), sciFiGlass, new Vector3(0f, 0f, ribZ[0]), Quaternion.identity);

        // Interior lighting strips (touching the surfaces, never inside the ribs)
        Box(cargoG, "Roof LED Strip", greenGlow, -0.30f, 0.30f, glassInner[0].y - 0.02f, glassInner[0].y, Z_REAR + 0.14f, cargoFrontZ - 0.14f);
        for (int s = -1; s <= 1; s += 2)
            Box(cargoG, "Floor Guide Strip " + (s > 0 ? "R" : "L"), greenGlow, s * 0.45f - 0.03f, s * 0.45f + 0.03f, glassInner[2].y, glassInner[2].y + 0.008f, Z_REAR + 0.20f, cargoFrontZ - 0.20f);

        // ---------------------------------------------------------
        // 9. HEXAGONAL REAR HATCH (Z key target). Pivot = top rear edge of the last rib.
        // ---------------------------------------------------------
        float topY = float.MinValue, botY = float.MaxValue;
        for (int i = 0; i < ribOuter.Length; i++) { topY = Mathf.Max(topY, ribOuter[i].y); botY = Mathf.Min(botY, ribOuter[i].y); }
        Vector2[] doorInnerWorld = OffsetPoly(hex, -0.10f);
        Vector2[] doorGlowInnerWorld = OffsetPoly(hex, -0.13f);

        Transform hatchPivot = MakeChild(hatchG, "Cargo Hatch Pivot", new Vector3(0f, topY, Z_REAR));
        Vector2[] dOuter = ShiftY(ribOuter, -topY);
        Vector2[] dInner = ShiftY(doorInnerWorld, -topY);
        Vector2[] dGlow = ShiftY(doorGlowInnerWorld, -topY);
        Mesh doorFrameMesh = Cached("DoorFrame", () => RingZ(dOuter, dInner, -0.09f, 0f, "DoorFrame"));
        MeshObj(hatchPivot, "Hex Hatch Frame", doorFrameMesh, whiteBody, Vector3.zero, Quaternion.identity);
        MeshObj(hatchPivot, "Hex Hatch Glass", Cached("DoorGlass", () => PrismZ(dInner, -0.06f, -0.04f, "DoorGlass")), sciFiGlass, Vector3.zero, Quaternion.identity);
        MeshObj(hatchPivot, "Hex Hatch Glow Outline", Cached("DoorGlow", () => RingZ(dInner, dGlow, -0.075f, -0.06f, "DoorGlow")), greenGlow, Vector3.zero, Quaternion.identity);

        float latchY = (botY + doorInnerWorld[2].y) * 0.5f - topY;
        MeshObj(hatchPivot, "Hex Hatch Latch", Cached("Latch", () => PrismZ(RegHex(0, 0, 0.06f), -0.115f, -0.09f, "Latch")), blueAccent, new Vector3(0f, latchY, 0f), Quaternion.identity);
        MeshObj(hatchPivot, "Hex Hatch Latch Core", Cached("LatchCore", () => PrismZ(RegHex(0, 0, 0.03f), -0.125f, -0.115f, "LatchCore")), greenGlow, new Vector3(0f, latchY, 0f), Quaternion.identity);
        Mesh tailMesh = Cached("TailLamp", () => PrismZ(RegHex(0, 0, 0.055f), -0.115f, -0.09f, "TailLamp"));
        MeshObj(hatchPivot, "Hex Tail Lamp L", tailMesh, redGlow, new Vector3(-0.42f, latchY, 0f), Quaternion.identity);
        MeshObj(hatchPivot, "Hex Tail Lamp R", tailMesh, redGlow, new Vector3(0.42f, latchY, 0f), Quaternion.identity);

        // Hatch collider: convex hexagonal slab, so it matches the hexagon exactly
        {
            GameObject hc = new GameObject("Hatch Collider");
            hc.transform.SetParent(hatchPivot, false);
            MeshCollider mc = hc.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("DoorCollider", () => PrismZ(dOuter, -0.09f, 0f, "DoorCollider"));
        }

        // ---------------------------------------------------------
        // 10. WHEELS: steer pivot -> spin pivot -> tire + hex hubs. Hex wheel arches above.
        // ---------------------------------------------------------
        List<Transform> spinPivots = new List<Transform>();
        List<Transform> frontSteerPivots = new List<Transform>();
        float[] wheelZ = { 1.35f, -1.60f };
        string[] wheelZName = { "Front", "Rear" };

        Vector2[] archOuter = RegHex(0, 0, 0.52f);
        Vector2[] archInner = OffsetPoly(archOuter, -0.05f);
        int[] archEdges = { 4, 5, 0 };   // the three upper edges of the hex ring (open at the bottom)

        for (int s = -1; s <= 1; s += 2)
        {
            string side = s > 0 ? "R" : "L";
            for (int w = 0; w < 2; w++)
            {
                float zz = wheelZ[w];
                string label = wheelZName[w] + " " + side;

                Transform steer = MakeChild(wheelsG, "Wheel Steer " + label, new Vector3(s * WHEEL_X, WHEEL_R, zz));
                Transform spin = MakeChild(steer, "Wheel Spin " + label, Vector3.zero);
                spinPivots.Add(spin);
                if (w == 0) frontSteerPivots.Add(steer);

                GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = "Tire " + label;
                tire.transform.SetParent(spin, false);
                tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tire.transform.localScale = new Vector3(WHEEL_R * 2f, WHEEL_HALF_W, WHEEL_R * 2f);
                tire.GetComponent<Renderer>().sharedMaterial = rubberTire;
                Object.DestroyImmediate(tire.GetComponent<Collider>());

                Quaternion outward = Quaternion.Euler(0f, s * 90f, 0f);   // local +Z -> world outward X
                MeshObj(spin, "Hub Chrome " + label, Cached("HubChrome", () => PrismZ(RegHex(0, 0, 0.24f), WHEEL_HALF_W, WHEEL_HALF_W + 0.02f, "HubChrome")), chromeHub, Vector3.zero, outward);
                MeshObj(spin, "Hub Blue " + label, Cached("HubBlue", () => PrismZ(RegHex(0, 0, 0.13f), WHEEL_HALF_W + 0.02f, WHEEL_HALF_W + 0.03f, "HubBlue")), blueAccent, Vector3.zero, outward);
                MeshObj(spin, "Hub Glow " + label, Cached("HubGlow", () => PrismZ(RegHex(0, 0, 0.05f), WHEEL_HALF_W + 0.03f, WHEEL_HALF_W + 0.04f, "HubGlow")), greenGlow, Vector3.zero, outward);

                // Wheel arch (dark body + blue trim), attached to the chassis side
                Vector3 archPos = new Vector3(s * CH_HW, WHEEL_R, zz);
                for (int a = 0; a < archEdges.Length; a++)
                {
                    int ek = archEdges[a];
                    Mesh body = Cached("ArchBody" + ek, () => EdgePrismZ(archOuter, archInner, ek, 0f, 0.26f, "ArchBody" + ek));
                    Mesh trim = Cached("ArchTrim" + ek, () => EdgePrismZ(archOuter, archInner, ek, 0.26f, 0.30f, "ArchTrim" + ek));
                    MeshObj(trimG, "Arch Body " + label + " e" + ek, body, darkChassis, archPos, outward);
                    MeshObj(trimG, "Arch Trim " + label + " e" + ek, trim, blueAccent, archPos, outward);
                }
            }
        }

        // ---------------------------------------------------------
        // 11. REAL LIGHTS (headlights follow driver occupancy, cargo bay has a soft glow)
        // ---------------------------------------------------------
        List<Light> headlights = new List<Light>();
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject lg = new GameObject("Headlight Spot " + (s > 0 ? "R" : "L"));
            lg.transform.SetParent(lightsG, false);
            lg.transform.localPosition = new Vector3(s * 0.38f, 0.57f, Z_FRONT + 0.06f);
            lg.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(0.86f, 0.95f, 1f);
            l.range = 28f;
            l.spotAngle = 70f;
            l.intensity = 2.5f;
            l.shadows = LightShadows.None;
            l.enabled = false;
            headlights.Add(l);
        }
        {
            GameObject cl = new GameObject("Cargo Bay Glow");
            cl.transform.SetParent(lightsG, false);
            cl.transform.localPosition = new Vector3(0f, 1.50f, -2.49f);
            Light l = cl.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 1f, 0.6f);
            l.range = 5f;
            l.intensity = 0.8f;
            l.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------
        // 12. COLLIDERS (wheels carry the tug; bay walls use exact convex hex prisms)
        // ---------------------------------------------------------
        for (int s = -1; s <= 1; s += 2)
            for (int w = 0; w < 2; w++)
                AddSphereCollider(collidersG, "Wheel Collider " + (w == 0 ? "F" : "R") + (s > 0 ? "R" : "L"),
                    new Vector3(s * WHEEL_X, WHEEL_R, wheelZ[w]), WHEEL_R, slick);

        AddBoxCollider(collidersG, "Chassis Collider", new Vector3(0f, 0.31f, (Z_FRONT + Z_REAR) * 0.5f), new Vector3(CH_HW * 2f, 0.18f, Z_FRONT - Z_REAR));
        AddBoxCollider(collidersG, "Cab Collider", new Vector3(0f, (CH_TOP + ROOF_TOP) * 0.5f, 0.15f), new Vector3(CH_HW * 2f, ROOF_TOP - CH_TOP, 1.30f));
        AddBoxCollider(collidersG, "Nose Collider", new Vector3(0f, 0.66f, 1.42f), new Vector3(CH_HW * 2f, 0.52f, 1.24f));
        AddBoxCollider(collidersG, "Front Bumper Collider", new Vector3(0f, 0.28f, 2.15f), new Vector3(1.80f, 0.20f, 0.22f));
        AddBoxCollider(collidersG, "Rear Bumper Collider", new Vector3(0f, 0.28f, -4.53f), new Vector3(1.80f, 0.20f, 0.18f));

        Vector2[] colOuter = OffsetPoly(hex, 0.04f);
        Vector2[] colInner = OffsetPoly(hex, -0.04f);
        float bayLen = cargoFrontZ - Z_REAR;
        float bayMid = (cargoFrontZ + Z_REAR) * 0.5f;
        for (int k = 0; k < 6; k++)
        {
            int kk = k;
            GameObject wall = new GameObject("Hex Wall Collider e" + k);
            wall.transform.SetParent(collidersG, false);
            wall.transform.localPosition = new Vector3(0f, 0f, bayMid);
            MeshCollider mc = wall.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("CargoCollider" + k, () => EdgePrismZ(colOuter, colInner, kk, -bayLen * 0.5f, bayLen * 0.5f, "CargoCollider" + kk));
        }
        {
            GameObject bh = new GameObject("Hex Bulkhead Collider");
            bh.transform.SetParent(collidersG, false);
            bh.transform.localPosition = new Vector3(0f, 0f, ribZ[0]);
            MeshCollider mc = bh.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = Cached("BulkheadCollider", () => PrismZ(glassInner, -ribDepth * 0.5f, ribDepth * 0.5f, "BulkheadCollider"));
        }

        // ---------------------------------------------------------
        // 13. PLAYER MOUNT & ENTRY TRIGGERS
        // ---------------------------------------------------------
        Transform seatMount = MakeChild(t, "Player Seat Mount", new Vector3(0f, 0.72f, -0.15f));
        Transform exitPoint = MakeChild(t, "Player Exit Point", new Vector3(-1.80f, 0.15f, -0.15f));

        AddEntryTrigger(triggersG, "Trigger Left", new Vector3(-1.45f, 0.95f, 0f), new Vector3(1.30f, 2.10f, 2.40f));
        AddEntryTrigger(triggersG, "Trigger Right", new Vector3(1.45f, 0.95f, 0f), new Vector3(1.30f, 2.10f, 2.40f));
        AddEntryTrigger(triggersG, "Trigger Front", new Vector3(0f, 0.95f, 2.80f), new Vector3(1.90f, 2.10f, 1.10f));
        AddEntryTrigger(triggersG, "Trigger Rear", new Vector3(0f, 0.95f, -5.10f), new Vector3(1.90f, 2.10f, 1.20f));

        // ---------------------------------------------------------
        // 14. CONTROLLER ATTACHMENT & PREFAB CREATION
        // ---------------------------------------------------------
        ElectricTugController controller = root.AddComponent<ElectricTugController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("seatMount").objectReferenceValue = seatMount;
        so.FindProperty("exitPoint").objectReferenceValue = exitPoint;
        so.FindProperty("cargoHatchPivot").objectReferenceValue = hatchPivot;
        so.FindProperty("steeringYoke").objectReferenceValue = yokeSpin;
        SetObjectArray(so, "wheelSpinPivots", spinPivots);
        SetObjectArray(so, "frontSteerPivots", frontSteerPivots);
        SetObjectArray(so, "headlights", headlights);
        so.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = AssetFolder + "/ElectricTug_CyberHex_V7.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));

        Debug.Log("CyberHex Glass Cargo Tug V7 created at: " + prefabPath);
    }

    // =====================================================================
    // Scene object helpers
    // =====================================================================
    private static void ConfigurePhysicsBody(Rigidbody rb)
    {
        rb.mass = 5500f;
        rb.linearDamping = 2.2f;
        rb.angularDamping = 5.0f;
        rb.centerOfMass = new Vector3(0f, 0.30f, -0.9f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>Axis-aligned box from min/max coordinates (root space), visual only.</summary>
    private static GameObject Box(Transform parent, string name, Material mat, float x0, float x1, float y0, float y1, float z0, float z1)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
        cube.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    /// <summary>Box with a position/size in the parent's local space.</summary>
    private static GameObject LocalBox(Transform parent, string name, Material mat, Vector3 localPos, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    /// <summary>Rectangular beam between two points (length along the beam, w/h cross section).</summary>
    private static GameObject Beam(Transform parent, string name, Material mat, Vector3 a, Vector3 b, float w, float h)
    {
        Vector3 d = b - a;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = (a + b) * 0.5f;
        cube.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        cube.transform.localScale = new Vector3(w, h, d.magnitude);
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    private static GameObject MeshObj(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Quaternion localRot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject PrismX(Transform parent, string name, Material mat, string meshKey, Vector2[] polyZY, float x0, float x1)
    {
        Mesh m = Cached(meshKey, () => PrismXZY(polyZY, x0, x1, meshKey));
        return MeshObj(parent, name, m, mat, Vector3.zero, Quaternion.identity);
    }

    private static void HoodStripe(Transform parent, string name, Material mat, string key, float x0, float x1, System.Func<float, float> hoodY)
    {
        const float z0 = 0.95f, z1 = 1.95f, thick = 0.012f;
        Vector2[] poly = {
            new Vector2(z0, hoodY(z0)), new Vector2(z1, hoodY(z1)),
            new Vector2(z1, hoodY(z1) + thick), new Vector2(z0, hoodY(z0) + thick) };
        PrismX(parent, name, mat, key, poly, x0, x1);
    }

    private static void LogoBand(Transform parent, string name, Material mat, string key, float y0, float y1, float x0, float x1)
    {
        Vector2[] poly = { new Vector2(-0.28f, y0), new Vector2(0.28f, y0), new Vector2(0.40f, y1), new Vector2(-0.16f, y1) };
        PrismX(parent, name, mat, key, poly, x0, x1);
    }

    private static void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;
        obj.AddComponent<BoxCollider>().size = size;
    }

    private static void AddSphereCollider(Transform parent, string name, Vector3 center, float radius, PhysicsMaterial mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;
        SphereCollider sc = obj.AddComponent<SphereCollider>();
        sc.radius = radius;
        sc.sharedMaterial = mat;
    }

    private static void AddEntryTrigger(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
    }

    private static Transform MakeChild(Transform parent, string name, Vector3 pos)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = pos;
        return child.transform;
    }

    private static void SetObjectArray<T>(SerializedObject so, string propName, List<T> items) where T : Object
    {
        SerializedProperty prop = so.FindProperty(propName);
        prop.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    // =====================================================================
    // Procedural mesh toolkit (all faces are flat-shaded, closed, outward-wound)
    // =====================================================================
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<int> tris = new List<int>();

        /// <summary>Adds a planar quad, flipping its winding if needed so its normal points along 'hint'.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hint)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hint) < 0f) { Vector3 tmp = b; b = d; d = tmp; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 hint)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hint) < 0f) { Vector3 tmp = b; b = c; c = tmp; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public Mesh Build(string name)
        {
            Mesh m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    private static Vector3 V3(Vector2 p, float z) { return new Vector3(p.x, p.y, z); }

    /// <summary>Hexagon with flat top/bottom: half flat width hf, half max width hm (at mid height), half height hh.</summary>
    private static Vector2[] HexPoly(float cx, float cy, float hf, float hm, float hh)
    {
        return new[]
        {
            new Vector2(cx + hf, cy + hh), new Vector2(cx + hm, cy), new Vector2(cx + hf, cy - hh),
            new Vector2(cx - hf, cy - hh), new Vector2(cx - hm, cy), new Vector2(cx - hf, cy + hh)
        };
    }

    /// <summary>Regular hexagon (flat top/bottom) with circumradius r.</summary>
    private static Vector2[] RegHex(float cx, float cy, float r)
    {
        return HexPoly(cx, cy, r * 0.5f, r, r * 0.8660254f);
    }

    private static Vector2 EdgeNormal(Vector2[] poly, int k)
    {
        int n = poly.Length;
        Vector2 cen = Vector2.zero;
        for (int i = 0; i < n; i++) cen += poly[i];
        cen /= n;
        Vector2 a = poly[k], b = poly[(k + 1) % n];
        Vector2 dir = (b - a).normalized;
        Vector2 nm = new Vector2(dir.y, -dir.x);
        if (Vector2.Dot(nm, (a + b) * 0.5f - cen) < 0f) nm = -nm;
        return nm;
    }

    /// <summary>Offsets a convex polygon (positive = outward) with perfectly mitered corners.</summary>
    private static Vector2[] OffsetPoly(Vector2[] poly, float d)
    {
        int n = poly.Length;
        Vector2[] lp = new Vector2[n];
        Vector2[] ld = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % n];
            ld[i] = (b - a).normalized;
            lp[i] = a + EdgeNormal(poly, i) * d;
        }
        Vector2[] res = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            int h = (i + n - 1) % n;
            Vector2 p1 = lp[h], d1 = ld[h], p2 = lp[i], d2 = ld[i];
            Vector2 r = p2 - p1;
            float det = -d1.x * d2.y + d2.x * d1.y;
            float t1 = (-r.x * d2.y + d2.x * r.y) / det;
            res[i] = p1 + d1 * t1;
        }
        return res;
    }

    private static Vector2[] ShiftY(Vector2[] poly, float dy)
    {
        Vector2[] r = new Vector2[poly.Length];
        for (int i = 0; i < poly.Length; i++) r[i] = new Vector2(poly[i].x, poly[i].y + dy);
        return r;
    }

    /// <summary>Filled convex polygon extruded along Z.</summary>
    private static Mesh PrismZ(Vector2[] poly, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = poly.Length;
        Vector2 c2 = Vector2.zero;
        for (int i = 0; i < n; i++) c2 += poly[i];
        c2 /= n;
        Vector3 cen = new Vector3(c2.x, c2.y, (z0 + z1) * 0.5f);
        for (int k = 0; k < n; k++)
        {
            Vector2 a = poly[k], b = poly[(k + 1) % n];
            Vector3 q0 = V3(a, z0), q1 = V3(b, z0), q2 = V3(b, z1), q3 = V3(a, z1);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(V3(poly[0], z1), V3(poly[k], z1), V3(poly[k + 1], z1), Vector3.forward);
            mb.Tri(V3(poly[0], z0), V3(poly[k], z0), V3(poly[k + 1], z0), Vector3.back);
        }
        return mb.Build(name);
    }

    /// <summary>Hollow polygon ring (outer/inner polygons with matching vertex order) extruded along Z.</summary>
    private static Mesh RingZ(Vector2[] outer, Vector2[] inner, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = outer.Length;
        for (int k = 0; k < n; k++)
        {
            int k2 = (k + 1) % n;
            Vector2 nm = EdgeNormal(outer, k);
            Vector3 h = new Vector3(nm.x, nm.y, 0f);
            mb.Quad(V3(outer[k], z0), V3(outer[k2], z0), V3(outer[k2], z1), V3(outer[k], z1), h);
            mb.Quad(V3(inner[k], z0), V3(inner[k2], z0), V3(inner[k2], z1), V3(inner[k], z1), -h);
            mb.Quad(V3(outer[k], z1), V3(outer[k2], z1), V3(inner[k2], z1), V3(inner[k], z1), Vector3.forward);
            mb.Quad(V3(outer[k], z0), V3(outer[k2], z0), V3(inner[k2], z0), V3(inner[k], z0), Vector3.back);
        }
        return mb.Build(name);
    }

    /// <summary>One side (edge k) of a mitered ring: a closed convex prism. Neighbouring edges share their miter planes exactly.</summary>
    private static Mesh EdgePrismZ(Vector2[] outer, Vector2[] inner, int k, float z0, float z1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int k2 = (k + 1) % outer.Length;
        Vector3[] p =
        {
            V3(outer[k], z0), V3(outer[k2], z0), V3(inner[k2], z0), V3(inner[k], z0),
            V3(outer[k], z1), V3(outer[k2], z1), V3(inner[k2], z1), V3(inner[k], z1)
        };
        Vector3 cen = Vector3.zero;
        for (int i = 0; i < 8; i++) cen += p[i];
        cen /= 8f;
        int[][] faces =
        {
            new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 },
            new[] { 3, 2, 6, 7 }, new[] { 0, 3, 7, 4 }, new[] { 1, 2, 6, 5 }
        };
        foreach (int[] f in faces)
        {
            Vector3 a = p[f[0]], b = p[f[1]], c = p[f[2]], d = p[f[3]];
            mb.Quad(a, b, c, d, (a + b + c + d) * 0.25f - cen);
        }
        return mb.Build(name);
    }

    /// <summary>Solid loft between two convex polygons with the same vertex count (use parallel edges to keep faces planar).</summary>
    private static Mesh Loft(Vector2[] a, float za, Vector2[] b, float zb, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = a.Length;
        Vector3 cen = Vector3.zero;
        for (int i = 0; i < n; i++) cen += V3(a[i], za) + V3(b[i], zb);
        cen /= (2f * n);
        for (int k = 0; k < n; k++)
        {
            int k2 = (k + 1) % n;
            Vector3 q0 = V3(a[k], za), q1 = V3(a[k2], za), q2 = V3(b[k2], zb), q3 = V3(b[k], zb);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(V3(a[0], za), V3(a[k], za), V3(a[k + 1], za), Vector3.back);
            mb.Tri(V3(b[0], zb), V3(b[k], zb), V3(b[k + 1], zb), Vector3.forward);
        }
        return mb.Build(name);
    }

    /// <summary>Convex polygon given in (z, y) extruded along X between x0 and x1 (vertices in parent space).</summary>
    private static Mesh PrismXZY(Vector2[] polyZY, float x0, float x1, string name)
    {
        MeshBuilder mb = new MeshBuilder();
        int n = polyZY.Length;
        Vector2 c2 = Vector2.zero;
        for (int i = 0; i < n; i++) c2 += polyZY[i];
        c2 /= n;
        Vector3 cen = new Vector3((x0 + x1) * 0.5f, c2.y, c2.x);
        for (int k = 0; k < n; k++)
        {
            Vector2 a = polyZY[k], b = polyZY[(k + 1) % n];
            Vector3 q0 = new Vector3(x0, a.y, a.x), q1 = new Vector3(x0, b.y, b.x);
            Vector3 q2 = new Vector3(x1, b.y, b.x), q3 = new Vector3(x1, a.y, a.x);
            mb.Quad(q0, q1, q2, q3, (q0 + q1 + q2 + q3) * 0.25f - cen);
        }
        for (int k = 1; k < n - 1; k++)
        {
            mb.Tri(new Vector3(x1, polyZY[0].y, polyZY[0].x), new Vector3(x1, polyZY[k].y, polyZY[k].x), new Vector3(x1, polyZY[k + 1].y, polyZY[k + 1].x), Vector3.right);
            mb.Tri(new Vector3(x0, polyZY[0].y, polyZY[0].x), new Vector3(x0, polyZY[k].y, polyZY[k].x), new Vector3(x0, polyZY[k + 1].y, polyZY[k + 1].x), Vector3.left);
        }
        return mb.Build(name);
    }

    // =====================================================================
    // Asset helpers
    // =====================================================================
    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder(AssetFolderParent, AssetFolderName);
    }

    /// <summary>Builds a mesh once per run and stores it as an asset (re-using/overwriting an existing asset of that name).</summary>
    private static Mesh Cached(string key, System.Func<Mesh> build)
    {
        if (meshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

        Mesh fresh = build();
        string path = AssetFolder + "/Mesh_" + key + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Mesh result;
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            result = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(fresh, path);
            result = fresh;
        }
        meshCache[key] = result;
        return result;
    }

    /// <summary>Saves a material as an asset (or refreshes the existing one) and returns the persistent asset.</summary>
    private static Material Palette(string fileName, Material fresh)
    {
        string path = AssetFolder + "/" + fileName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }
        AssetDatabase.CreateAsset(fresh, path);
        return fresh;
    }

    private static PhysicsMaterial MakePhysicsMaterial()
    {
        string path = AssetFolder + "/TugSlick.asset";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("Tug Slick");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0.05f;
        pm.staticFriction = 0.30f;
        pm.bounciness = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum;
        pm.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(pm);
        return pm;
    }

    private static Material MakeMaterial(string name, Color col, float metallic = 0.0f, float smoothness = 0.5f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = col };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    private static Material MakeEmissiveMaterial(string name, Color col, float intensity)
    {
        Material mat = MakeMaterial(name, col, 0.2f, 0.8f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    private static Material MakeGlassMaterial(string name, Color col)
    {
        Material mat = MakeMaterial(name, col, 0.0f, 0.92f);
        if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        return mat;
    }
}
#endif
