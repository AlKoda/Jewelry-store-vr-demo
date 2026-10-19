#if UNITY_EDITOR
using UnityEngine;
using static DemoGeometry;

// Every police-specific choice of the night store in one setting: the police car and its
// lamp anchors, the light-bar colours and rate, the barrier-tape text, the car's idle,
// siren and radio sounds and an optional police figure. The country and the police
// service are Koda's open decision #1 (listed in Docs/DECISIONS.md), so no other code picks
// them: the builder, the lighting, the tools and the audio read PoliceLook.Active (and
// ActiveCar for the car). Provisional reproduces today's look exactly and is the default;
// once the service is decided it becomes one more Service value with its own Preset.
public static class PoliceLook
{
    public enum Service
    {
        // Today's look: the KayKit car with its anchors and light-bar boxes, the CRIME
        // SCENE tape, no sirens, no radio and no police figure.
        Provisional,
        // For review only: the photoreal 1980s US sedan, US sirens, radio traffic and an
        // armed officer. Selected only by editing Current below.
        ProvisionalRealisticUS
    }

    // The one switch: change only this line.
    public static readonly Service Current = Service.Provisional;

    // Packs the preset's file names live in (the car names its own pack).
    public const string SoundPack = "NightSoundscape", FigurePack = "PeopleModels";

    // A police car model and its anchors in the car's space (nose +z, x right, y up from
    // the wheels' contact): headlight lens centres, the light-bar centre, the two bar
    // halves (centred at +-LightBarCentre along x, each 2 x LightBarHalf) and the
    // headlight lens quad size.
    public struct Car
    {
        public string Pack, Model;
        public float Scale, LightBarCentre;
        // Turns the model inside the car root, for a model authored facing another axis.
        public Vector3 ModelEuler, HeadlightLeft, HeadlightRight, LightBar, LightBarHalf;
        public Vector2 LensSize;
    }

    public struct Preset
    {
        public string Name, TapeText;
        // NightSoundscape file names (null: none); GuardFigure is a PeopleModels model.
        public string CarIdle, RadioLoop, GuardFigure;
        // In order of preference: the first whose model exists is placed (ActiveCar); the
        // last entry is the fallback (the KayKit car or its stand-in).
        public Car[] Cars;
        // Light-bar emission when lit (LightBarBlink.RedOn / BlueOn), flashes per second.
        public Color LightBarLeft, LightBarRight;
        public float LightBarHertz, GuardYaw;
        // NightSoundscape one-shots (empty: none).
        public string[] Sirens, Radio;
        public Vector3 GuardPosition;
    }

    // Today's car: KayKit's car_police at the kit scale with NightStoreLayout's anchors and
    // the light-bar boxes NightLighting always made (0.38 x 0.07 x 0.16 at x +-0.2). Without
    // the KayKit pack ShopCityRefinement's stand-in takes the same anchors.
    public static readonly Car KayKitCar = new Car
    {
        Pack = ShopCityRefinement.KitPack, Model = "car_police", Scale = ShopCityRefinement.KitScale, ModelEuler = Vector3.zero,
        HeadlightLeft = NightStoreLayout.HeadlightLeft, HeadlightRight = NightStoreLayout.HeadlightRight, LightBar = NightStoreLayout.LightBar,
        LightBarCentre = 0.2f, LightBarHalf = V(0.19f, 0.035f, 0.08f), LensSize = new Vector2(0.3f, 0.12f)
    };

    private static readonly Color BarRed = new Color(1f, 0.06f, 0.04f) * 4f, BarBlue = new Color(0.08f, 0.25f, 1f) * 4f;

    // The default. Its one addition to today's look is CarIdle: a generic V8 petrol idle
    // (NightSoundscape PoliceCarIdle_Loop, no siren, no radio voice), accepted as fitting
    // any country's patrol car.
    public static readonly Preset Provisional = new Preset
    {
        Name = "Provisional (today's look)",
        TapeText = DemoToolsBuilder.DefaultTapeText,
        CarIdle = "PoliceCarIdle_Loop.wav", RadioLoop = null, GuardFigure = null,
        Cars = new[] { KayKitCar },
        LightBarLeft = BarRed, LightBarRight = BarBlue, LightBarHertz = 1.5f,
        Sirens = new string[0], Radio = new string[0],
        GuardPosition = Vector3.zero, GuardYaw = 0
    };

    // StreetModels/PoliceCar (CC BY 4.0, credited through DemoAssetLibrary), measured on its
    // OBJ: Unity mirrors file x on import and the +90 yaw then maps file (x, y, z) to car
    // space (z, y, x). Headlights span file x 2.189-2.204, y 0.597-0.711, z +-0.351..0.689
    // (centres z +-0.52, y 0.654), so the anchors sit 1 cm ahead of the lenses and the lens
    // quad (0.33 x 0.11) fits inside each; the front bumper reaches x 2.49 below y 0.63.
    // Lightbar_glass spans file x -0.331..-0.177, y 1.429-1.529, z +-0.528 (each side from
    // +-0.144), which the two bar halves (x 0..+-0.54, y 1.425-1.535, z -0.34..-0.17) enclose.
    public static readonly Preset RealisticUS = new Preset
    {
        Name = "Provisional realistic US, for review",
        TapeText = DemoToolsBuilder.DefaultTapeText,
        CarIdle = "PoliceCarIdle_Loop.wav", RadioLoop = "PoliceCarRadio_Loop.ogg", GuardFigure = "PoliceOfficer_Standing",
        Cars = new[]
        {
            new Car
            {
                Pack = "StreetModels", Model = "PoliceCar", Scale = 1, ModelEuler = V(0, 90, 0),
                HeadlightLeft = V(-0.52f, 0.653f, 2.215f), HeadlightRight = V(0.52f, 0.653f, 2.215f), LightBar = V(0, 1.48f, -0.255f),
                LightBarCentre = 0.27f, LightBarHalf = V(0.27f, 0.055f, 0.085f), LensSize = new Vector2(0.33f, 0.11f)
            },
            KayKitCar
        },
        LightBarLeft = BarRed, LightBarRight = BarBlue, LightBarHertz = 1.5f,
        Sirens = new[] { "SirenWailPassDistant_01.wav", "SirenWailYelpPass_02.wav", "SirenDistantCity_03.wav", "SirenDistantPass_04.wav" },
        Radio = new[]
        {
            "RadioChatter_01.wav", "RadioChatter_02.wav", "RadioChatter_03.wav", "RadioChatter_04.wav", "RadioChatter_05.wav",
            "RadioSquelch_01.wav", "RadioSquelch_02.wav", "RadioSquelch_03.wav", "RadioBeep_01.wav", "RadioBeep_02.wav", "RadioBeep_03.wav"
        },
        // The holstered-pistol pose on the pavement right of the shop, 3 m outside the play
        // area, facing the shop front (the figure's front is +z).
        GuardPosition = V(12.5f, 0, -1.6f), GuardYaw = -90
    };

    public static Preset Active => Current == Service.ProvisionalRealisticUS ? RealisticUS : Provisional;

    // The first of Active's cars whose model exists, else the last entry. Stateless: resolved
    // on every call (NightStoreBuilder.PlacedCar records what a build actually placed).
    public static Car ActiveCar
    {
        get
        {
            Car[] cars = Active.Cars;
            foreach (Car c in cars)
                if (DemoAssetLibrary.Has(c.Pack, c.Model)) return c;
            return cars[cars.Length - 1];
        }
    }
}
#endif
