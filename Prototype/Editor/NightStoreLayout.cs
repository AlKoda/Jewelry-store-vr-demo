#if UNITY_EDITOR
using UnityEngine;
using static DemoGeometry;

// The one layout definition of the night store: shell dimensions, named anchors,
// fixture placements, movement regions, tool station and map framing. The builder,
// the dressing, the bounds and the validator all read it, so a tuned dimension
// changes in one place. Working estimates from Docs/REFERENCE_LAYOUT_PLAN.md.
//
// Metres. Origin at the centre of the shop's front facade line at floor level; +x to
// the right standing on the street facing the shop, +z into the shop, y up. The
// street is negative z. StoreKit fixtures face -z at yaw 0 (towards the street).
public static class NightStoreLayout
{
    public const string Root = "NightStore";
    public const string SceneName = "JewelryStoreNight";
    public static readonly string[] Zones = { "Architecture", "Facade", "Exterior", "Showroom", "Office", "Dressing", "Anchors" };
    public const string CeilingPath = "Architecture/Ceiling";

    // ---------------------------------------------------------------- shell
    public const float MinX = -8, MaxX = 8, MinZ = 0, MaxZ = 12;
    public const float CeilingY = 3.0f, Slab = 0.2f, Wall = 0.25f, ParapetY = 3.6f;
    public const float Partition = 0.12f;
    // Showroom depth: the rear partition line; the office spans OfficeHalf either side of x 0.
    public const float RearZ = 8.5f, OfficeHalf = 3f;
    // Office front opening (flush threshold) and its lintel.
    public const float OfficeOpeningHalf = 0.8f, OfficeOpeningHeight = 2.4f;

    // ---------------------------------------------------------------- facade (z 0)
    public const float BayWidth = 2f;
    // Stone-clad end piers outside the side walls; the neighbouring buildings abut them.
    public const float Pier = 0.6f;
    public const int Bays = 8; // x -8..8; bays 3 and 4 (x -2..2) hold the entrance.
    public const float DoorHalf = 1f, DoorHeight = 2.4f, SidelightWidth = 1f;
    // Leaves forced inward. The plan's 60 degrees for the left leaf leaves 1.2 m between
    // the leaves; 75 keeps the 1.4 m clear the walkthrough needs.
    public const float DoorLeftOpen = 75, DoorRightOpen = 85;
    public static float BayCentre(int bay) => MinX + BayWidth * (bay + 0.5f);
    public static bool IsEntranceBay(int bay) => bay == 3 || bay == 4;
    public static StoreKit.Pane BayState(int bay) => bay == 2 ? StoreKit.Pane.Cracked : StoreKit.Pane.Intact;
    public const StoreKit.Pane LeftSidelight = StoreKit.Pane.Intact, RightSidelight = StoreKit.Pane.Broken;

    // ---------------------------------------------------------------- exterior
    public const float StreetY = -0.12f;
    public const float PavementHalfX = 10, KerbZ = -3, FarKerbZ = -14, FarPavementZ = -17;
    public const float StreetHalfX = 32;
    public static readonly float[] BollardX = { -5, -2.5f, 2.5f, 5 };
    public const float BollardZ = -1.6f;
    // KayKit streetlight bases; their heads (light anchors for the night lighting) hang
    // over the street. Near-side arms point to -z, far-side ones to +z.
    public static readonly Vector3[] Streetlights = { V(-9, 0, -2.6f), V(9, 0, -2.6f), V(-6, 0, -14.6f), V(7, 0, -14.6f) };
    public static float StreetlightYaw(int i) => Streetlights[i].z > FarKerbZ * 0.5f ? -90 : 90;

    // Police car on the street, nose aimed at the entrance. KayKit's car_police is
    // modelled nose towards +z (front wheels at +z, the hood is the longer lower end,
    // the cabin and light bar sit 0.06 units behind the centre), so yaw turns +z onto
    // the aim. Anchors in the car's space (nose +z): headlights at the front corners,
    // light bar on the roof.
    public static readonly Vector3 CarPosition = V(4.0f, StreetY, -6.2f);
    public static readonly Vector3 CarAim = V(0, 0, 2);
    public static float CarYaw => Mathf.Atan2(CarAim.x - CarPosition.x, CarAim.z - CarPosition.z) * Mathf.Rad2Deg;
    public static readonly Vector3 HeadlightLeft = V(-0.8f, 0.7f, 2.05f), HeadlightRight = V(0.8f, 0.7f, 2.05f);
    public static readonly Vector3 LightBar = V(0, 1.86f, -0.27f);

    // Spawn on the street beside the driver's door, facing the shop.
    public static readonly Vector3 PlayerSpawn = V(1.6f, StreetY, -5.4f);
    public const float PlayerYaw = 0;

    // Folding equipment table on the pavement facing the shop (long side along x), and
    // the easel with the photo frame and status text beside it.
    public static readonly Vector3 Table = V(5.4f, 0, -2.0f);
    public const float TableTop = 0.74f, TableWidth = 1.4f, TableDepth = 0.6f;
    public static readonly Vector3 Easel = V(7.3f, 0, -2.0f);
    public const float EaselWidth = 1.8f, EaselBottom = 0.85f, EaselTop = 2.35f;

    // ---------------------------------------------------------------- fixtures
    public struct Fixture
    {
        public string Zone, Name, Kit;
        public Vector3 Position;
        public float Yaw;
        public Fixture(string zone, string name, string kit, float x, float y, float z, float yaw)
        {
            Zone = zone; Name = name; Kit = kit; Position = V(x, y, z); Yaw = yaw;
        }
    }

    // Wall pieces (signs, keypad, camera, exit sign) pivot at their back. Left-wall
    // pieces face +x (yaw -90), right-wall ones -x (yaw 90).
    public static readonly Fixture[] Fixtures =
    {
        // Central cluster: three cases side by side; the middle one's drawer pulled out.
        new Fixture("Showroom", "CentralCase_Left", "display_case", -1.9f, 0, 5.2f, 0),
        new Fixture("Showroom", "CentralCase_Middle", "display_case", 0, 0, 5.2f, 0),
        new Fixture("Showroom", "CentralCase_Right", "display_case", 1.9f, 0, 5.2f, 0),
        new Fixture("Showroom", "LeftIsland_Front", "display_case", -4.6f, 0, 4.0f, 90),
        new Fixture("Showroom", "LeftIsland_Rear", "display_case", -4.6f, 0, 5.8f, 90),
        // Shoved 8 degrees off square; its drawer lies on the floor (dressing).
        new Fixture("Showroom", "KnockedCase", "display_case", -5.6f, 0, 2.3f, 98),
        new Fixture("Showroom", "RightIsland_Front", "display_case", 4.6f, 0, 4.2f, -90),
        new Fixture("Showroom", "RightIsland_Rear", "display_case", 4.6f, 0, 6.0f, -90),
        new Fixture("Showroom", "WallCase_Left0", "wall_case", -7.6f, 0, 2.4f, -90),
        new Fixture("Showroom", "WallCase_Left1", "wall_case", -7.6f, 0, 4.4f, -90),
        new Fixture("Showroom", "WallCase_Left2", "wall_case", -7.6f, 0, 6.4f, -90),
        new Fixture("Showroom", "WallCase_Right0", "wall_case", 7.6f, 0, 4.2f, 90),
        new Fixture("Showroom", "WallCase_Right1", "wall_case", 7.6f, 0, 6.2f, 90),
        new Fixture("Showroom", "WallCase_Rear", "wall_case", -5.5f, 0, 7.85f, 0),
        new Fixture("Showroom", "Counter", "counter", 5.5f, 0, 7.85f, 0),
        new Fixture("Showroom", "PosTerminal", "pos_terminal", 5.95f, 0.95f, 7.8f, 0),
        new Fixture("Showroom", "Sign_Left0", "sign_frame", MinX, 2.0f, 2.4f, -90),
        new Fixture("Showroom", "Sign_Left1", "sign_frame", MinX, 2.0f, 4.4f, -90),
        new Fixture("Showroom", "Sign_Left2", "sign_frame", MinX, 2.0f, 6.4f, -90),
        new Fixture("Showroom", "Sign_Right0", "sign_frame", MaxX, 2.0f, 4.2f, 90),
        new Fixture("Showroom", "Sign_Right1", "sign_frame", MaxX, 2.0f, 6.2f, 90),
        new Fixture("Showroom", "Sign_Rear", "sign_frame", -5.5f, 2.0f, RearZ - Partition * 0.5f, 0),
        new Fixture("Showroom", "Sign_Counter", "sign_frame", 5.5f, 2.0f, RearZ - Partition * 0.5f, 0),
        // Seating in the front-right corner, facing into the shop.
        new Fixture("Showroom", "Sofa", "waiting_sofa", 5.6f, 0, 1.1f, 180),
        new Fixture("Showroom", "SideTable", "side_table", 7.0f, 0, 1.1f, 90),
        new Fixture("Showroom", "Planter_Front", "planter", 7.5f, 0, 0.6f, 0),
        new Fixture("Showroom", "Planter_RearLeft", "planter", -7.4f, 0, 8.0f, 0),
        new Fixture("Showroom", "Planter_Office", "planter", 2.6f, 0, 8.0f, 0),
        // Exit sign on the entrance transom and the keypad on the mullion left of the
        // entrance (the plan's points 0.2 and 0.15 m in would float in front of glass).
        new Fixture("Showroom", "ExitSign", "exit_sign", 0, 2.62f, 0.065f, 180),
        new Fixture("Showroom", "AlarmKeypad", "alarm_keypad", -2.0f, 1.4f, 0.065f, 180),
        // Corner camera on the rear-left partition, turned towards the entrance.
        new Fixture("Showroom", "SecurityCamera", "security_camera", -7.7f, 2.78f, RearZ - Partition * 0.5f, -42.5f),
        // Office: exactly two safes, open (angles come from the dressing's case state).
        new Fixture("Office", "SafeLarge", "safe_large", 1.3f, 0, 11.35f, 0),
        new Fixture("Office", "SafeTall", "safe_tall", -1.0f, 0, 11.45f, 0),
        new Fixture("Office", "Shelving", "shelving_unit", -2.6f, 0, 10.4f, -90),
        new Fixture("Office", "Desk", "office_desk", -1.7f, 0, 9.4f, 180),
        new Fixture("Office", "DrawerUnit", "drawer_unit", 2.5f, 0, 9.2f, 90),
        new Fixture("Office", "Locker", "locker_cabinet", 0.2f, 0, 11.6f, 0),
        // Closed doors to the rear side rooms, beside the office walls (the plan's x 5.5
        // would put them behind the rear wall case and the counter).
        new Fixture("Office", "OfficeDoor", "office_door", -3.9f, 0, RearZ, 0),
        new Fixture("Office", "ServiceDoor", "service_door", 3.9f, 0, RearZ, 0),
    };

    public static Fixture Find(string name)
    {
        foreach (Fixture f in Fixtures) if (f.Name == name) return f;
        throw new System.ArgumentException("Unknown night store fixture: " + name);
    }

    // Ceiling fittings (all off): a downlight grid every 2.5 m and a track over each wall case.
    public static Vector3[] Downlights()
    {
        System.Collections.Generic.List<Vector3> points = new System.Collections.Generic.List<Vector3>();
        for (float x = -6.25f; x <= 6.3f; x += 2.5f)
            for (float z = 1.25f; z <= 6.3f; z += 2.5f) points.Add(V(x, CeilingY, z));
        points.Add(V(-1.25f, CeilingY, 10.25f));
        points.Add(V(1.25f, CeilingY, 10.25f));
        return points.ToArray();
    }

    // Track rails (1.2 m, cans aimed 30 degrees towards the case they light), as position and yaw.
    public static readonly Vector4[] Tracks =
    {
        new Vector4(-6.7f, CeilingY, 2.4f, 90), new Vector4(-6.7f, CeilingY, 4.4f, 90), new Vector4(-6.7f, CeilingY, 6.4f, 90),
        new Vector4(6.7f, CeilingY, 4.2f, -90), new Vector4(6.7f, CeilingY, 6.2f, -90),
        new Vector4(-5.5f, CeilingY, 6.9f, 180), new Vector4(5.5f, CeilingY, 6.9f, 180)
    };

    // ---------------------------------------------------------------- regions
    public static readonly InteriorBounds.Region Showroom = new InteriorBounds.Region(MinX + 0.3f, MaxX - 0.3f, MinZ + 0.25f, RearZ - 0.25f);
    public static readonly InteriorBounds.Region Office = new InteriorBounds.Region(-OfficeHalf + 0.2f, OfficeHalf - 0.2f, RearZ - 0.25f, MaxZ - 0.25f);
    public static readonly InteriorBounds.Region Threshold = new InteriorBounds.Region(-DoorHalf + 0.1f, DoorHalf - 0.1f, -0.35f, 0.35f);
    public static readonly InteriorBounds.Region Pavement = new InteriorBounds.Region(-PavementHalfX + 0.5f, PavementHalfX - 0.5f, KerbZ + 0.1f, -0.35f);
    public static readonly InteriorBounds.Region StagingBay = new InteriorBounds.Region(-2, 9, -8, KerbZ + 0.1f);
    public static InteriorBounds.Region[] Regions() => new[] { Showroom, Office, Threshold, Pavement, StagingBay };

    // Invisible walls just outside the accessible exterior (centre, size), so the
    // walker, teleport arcs and pointer stop at the edge of the play area. Derived from
    // the pavement, the kerb and the staging bay (declared above), so they follow a retune.
    public const float BoundaryMargin = 0.3f, BoundaryThickness = 0.1f;
    public static readonly Vector3[] BoundaryWalls = Boundary();

    private static Vector3[] Boundary()
    {
        float t = BoundaryThickness, h = 2.6f, y = 1.2f;
        float x0 = StagingBay.MinX - BoundaryMargin, x1 = StagingBay.MaxX + BoundaryMargin, zb = StagingBay.MinZ - BoundaryMargin;
        float kerbWall = KerbZ - 0.15f, bayZ = (zb + KerbZ) * 0.5f - t * 0.5f;
        return new[]
        {
            // Pavement ends, from the kerb line to the facade.
            V(-PavementHalfX + t * 0.5f, y, KerbZ * 0.5f - 0.1f), V(t, h, -KerbZ + 0.2f),
            V(PavementHalfX - t * 0.5f, y, KerbZ * 0.5f - 0.1f), V(t, h, -KerbZ + 0.2f),
            // Kerb either side of the staging bay.
            V((-PavementHalfX + x0) * 0.5f, y, kerbWall), V(x0 + PavementHalfX, h, t),
            V((PavementHalfX + x1) * 0.5f, y, kerbWall), V(PavementHalfX - x1, h, t),
            // Staging bay sides and back.
            V(x0, y, bayZ), V(t, h, KerbZ - zb),
            V(x1, y, bayZ), V(t, h, KerbZ - zb),
            V((x0 + x1) * 0.5f, y, zb), V(x1 - x0 + t, h, t),
        };
    }

    // ---------------------------------------------------------------- tools and map
    public static readonly Vector2 MapCentre = new Vector2(0.5f, 2f);
    public const float MapHalfSize = 10.5f;

    public static DemoToolsBuilder.ToolLayout Tools => new DemoToolsBuilder.ToolLayout
    {
        StoreRoot = Root, CeilingPath = CeilingPath,
        // The shop rack's sample spacing, on the table top (the lamp beside the table).
        Cone = Table + V(-0.52f, TableTop, 0), Marker = Table + V(-0.26f, TableTop, 0), Scale = Table + V(-0.04f, TableTop, -0.04f),
        TapePost = Table + V(0.26f, TableTop, 0), Measure = Table + V(0.52f, TableTop, 0),
        // The lamp's tripod would crowd the table top: it stands on the pavement at the
        // table's left end, clear of the bollard behind the table.
        Lamp = Table + V(-1.0f, 0, -0.05f),
        SpawnPoint = Table + V(0, TableTop, 0), Camera = Table + V(0, TableTop + 0.2f, 0), CameraYaw = 90,
        // Facing the visitor on the pavement (looking +z); 3 cm in front of the board.
        PhotoFrame = Easel + V(0, 1.85f, -0.045f), PhotoFrameYaw = 0,
        StatusBoard = Easel + V(0, 1.15f, -0.045f), StatusBoardYaw = 0,
        Ambience = V(0, 2.5f, -8f),
        MapCentre = MapCentre, MapHalfSize = MapHalfSize
    };

    // Open showroom floor for the shared tool, photo and reset stages.
    public static DemoValidation.ToolTestPoints TestPoints() => new DemoValidation.ToolTestPoints
    {
        Cone = V(-2, 0, 2.5f), Tape = V(-1.5f, 0, 1.4f), Reels = V(3.3f, 0, 1.8f), Pair = V(-3.2f, 0, 7.0f),
        CameraDrop = V(0, 1.5f, 3), Sketch = V(0, 0, 3)
    };

    // ---------------------------------------------------------------- walk route
    // Car -> street before the kerb -> pavement -> threshold -> showroom -> right-hand
    // aisle round the central cluster -> behind it -> office opening -> in front of the safes.
    public static readonly Vector3[] Route =
    {
        V(1.2f, StreetY, -3.6f), V(0.6f, 0, -1.5f), V(0.1f, 0, 0), V(0.1f, 0, 1.6f), V(3.4f, 0, 3.6f),
        V(3.45f, 0, 6.4f), V(0, 0, 7.4f), V(0, 0, RearZ), V(0, 0, 10.5f)
    };

    // Named empties under NightStore/Anchors (the car's lamp anchors live on the car).
    public static readonly string[] AnchorNames =
    {
        "PlayerSpawn", "EntranceCentre", "OfficeOpening", "SafeLargeFront", "SafeTallFront", "EquipmentTable", "Easel", "StagingBay",
        "StreetlightHead_0", "StreetlightHead_1", "StreetlightHead_2", "StreetlightHead_3"
    };

    // KayKit streetlight head: 0.96 units high, arm 0.24 units, at scale 4.5.
    public static Vector3 StreetlightHead(int i)
    {
        float yaw = StreetlightYaw(i) * Mathf.Deg2Rad;
        Vector3 arm = new Vector3(-Mathf.Cos(yaw), 0, Mathf.Sin(yaw)) * 1.0f;
        return Streetlights[i] + arm + V(0, 4.2f, 0);
    }

    // UV trace placeholders (UVTraceBuilder.NightExamples), derived from the layout so
    // they follow the safe and the office opening: drops on the floor just in front of
    // the large safe's face, a partial sole just inside the office opening.
    public static Vector3 UVDrops => Find("SafeLarge").Position + V(-0.1f, 0, -0.63f);
    public static Vector3 UVSole => Anchor("OfficeOpening") + V(0.3f, 0, 0.6f);
    // On the right entrance leaf's shop-side face, on the lock stile beside the push
    // bar (leaf space: StoreKit.StorefrontDoors puts that face at local z 0).
    public static readonly Vector3 UVDoorSmearLocal = V(-0.88f, 1.15f, 0);

    public static Vector3 Anchor(string name)
    {
        switch (name)
        {
            case "PlayerSpawn": return PlayerSpawn;
            case "EntranceCentre": return V(0, 0, 0);
            case "OfficeOpening": return V(0, 0, RearZ);
            case "SafeLargeFront": return Find("SafeLarge").Position + V(0, 0, -1.0f);
            case "SafeTallFront": return Find("SafeTall").Position + V(0, 0, -0.9f);
            case "EquipmentTable": return Table;
            case "Easel": return Easel;
            case "StagingBay": return V((StagingBay.MinX + StagingBay.MaxX) * 0.5f, StreetY, (StagingBay.MinZ + StagingBay.MaxZ) * 0.5f);
            default:
                if (name.StartsWith("StreetlightHead_")) return StreetlightHead(int.Parse(name.Substring(16)));
                throw new System.ArgumentException("Unknown night store anchor: " + name);
        }
    }
}
#endif
