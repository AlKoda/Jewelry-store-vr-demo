#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using static DemoGeometry;
using L = NightStoreLayout;

// The night store's sound, built by NightStoreBuilder.Build after the tool station (Apply)
// and on the player (AttachPlayer). Every clip is an asset of Prototype/ThirdParty/
// NightSoundscape, NightAudio or Music loaded through DemoAssetLibrary.Audio (which records
// any credit); a missing file drops only its own emitter, so a project unpacked without
// the new packs keeps the NightAudio clips or the generated sounds.
//
// - StagedSounds (beside the tool station's StreetAmbience): the DemoSoundBank. The
//   NightSoundscape shutter, torch click and stereo city bed (2D) replace NightAudio's
//   where present; cone and marker set-downs and both footstep sets are variations.
// - NightStore/Soundscape: AmbientZones over the exterior beds (the street ambience, the
//   wind, the police car's idle at its bonnet and PoliceLook's radio loop, the street
//   events, the alarm bell) and the interior room tone, inside the showroom and office
//   rects; the wind through the forced doors in neither list; AmbientEvents (passing cars,
//   the CC0 dog, and PoliceLook's sirens and radio traffic, none by default); three Room
//   reverb zones across the showroom and one in the office; the AlarmBell presenter set,
//   silent at arrival (whether the alarm still rings is a scenario choice).
// - NightStore/Music: DemoMusic with the investigation underscore (off at arrival, a
//   presenter switch) and the reset sting.
// Levels are the packs' suggested starting levels; nothing has been auditioned in Unity
// or on the Quest yet.
public static class NightAudioBuilder
{
    public const string SoundPack = PoliceLook.SoundPack, MusicPack = "Music";
    public const string BankName = "StagedSounds", SoundscapeName = "Soundscape", EventsName = "Events", MusicName = "Music";
    public const string AlarmSetName = "AlarmBell", CarIdleName = "CarIdle", RadioLoopName = "CarRadio", DoorGapName = "DoorGap";
    public const string CarPassGroup = "CarPass", DogGroup = "Dog", SirenGroup = "Sirens", RadioGroup = "Radio";
    // The one distant dog without an attribution requirement: DogBarkDistant_01..03 are
    // treated as CC BY 3.0 (conflicting licence evidence, NightSoundscape README).
    public const string DogBark = "DogBarkDistant_04.wav";
    public static readonly string[] CarPasses = { "CarPassWet_01.wav", "CarPass_02.wav", "CarPass_03.wav" };
    public const string AlarmBellClip = "AlarmBell_Close_Loop.wav", InvestigationBed = "InvestigationLoop.ogg", ResetSting = "ResetSting.ogg";

    // Reverb zones (AudioReverbPreset.Room): centre in the store root's space, full effect
    // within the first distance, none beyond the second. The showroom (15.4 by 8 m) is wider
    // than one sphere can cover without reaching the street, so three zones stand in a row at
    // mid-depth: each ends at z -0.25, so the pavement (z -0.35 and beyond) stays dry, and
    // at mid-depth the reverb is full from side wall to side wall (x -7.7 to 7.7).
    public static readonly Vector3[] ShowroomReverbs = { V(-4.75f, 1.5f, 4.25f), V(0, 1.5f, 4.25f), V(4.75f, 1.5f, 4.25f) };
    public static readonly string[] ShowroomReverbNames = { "Reverb_ShowroomLeft", "Reverb_Showroom", "Reverb_ShowroomRight" };
    public static readonly Vector3 OfficeReverb = V(0, 1.5f, 10.25f);
    public const float ShowroomReverbMin = 3, ShowroomReverbMax = 4.5f, OfficeReverbMin = 1.5f, OfficeReverbMax = 3;
    public const float FootstepVolume = 0.22f, FootstepGlassVolume = 0.15f;

    public static void Apply(Transform root)
    {
        AmbientSound ambience = UnityEngine.Object.FindFirstObjectByType<AmbientSound>();
        Bank(ambience);
        Soundscape(root, ambience);
        Music(root);
    }

    // Footsteps on the rig root with the bank's footstep sets and the dressing's glass
    // zones; nothing without footstep clips (the walker stays silent, as before).
    public static Footsteps AttachPlayer(ShopWalkController walker)
    {
        if (walker == null) return null;
        DemoSoundBank bank = UnityEngine.Object.FindFirstObjectByType<DemoSoundBank>();
        if (bank == null || !DemoSoundBank.Has(bank.FootstepsHard) && !DemoSoundBank.Has(bank.FootstepsGlass)) return null;
        Footsteps steps = walker.gameObject.AddComponent<Footsteps>();
        steps.Hard = bank.FootstepsHard;
        steps.Glass = bank.FootstepsGlass;
        steps.GlassZones = NightStoreDressing.GlassZones();
        steps.Volume = FootstepVolume;
        steps.GlassVolume = FootstepGlassVolume;
        steps.PitchJitter = .018f;
        return steps;
    }

    // ---------------------------------------------------------------- bank

    // The staged clips (the old StagedSounds): NightSoundscape's takes where present (at
    // their README levels), NightAudio's otherwise; the bank goes when it holds nothing.
    private static DemoSoundBank Bank(AmbientSound ambience)
    {
        GameObject holder = new GameObject(BankName);
        if (ambience != null) holder.transform.SetParent(ambience.transform.parent, false);
        DemoSoundBank bank = holder.AddComponent<DemoSoundBank>();
        string old = NightStoreBuilder.AudioPack;
        bank.CameraClick = Prefer("CameraShutterDSLR_01.wav", 0.5f, "CameraClick.wav", ref bank.CameraClickVolume);
        bank.FlashlightSwitch = Prefer("FlashlightClick_01.wav", 0.4f, "FlashlightSwitch.wav", ref bank.FlashlightSwitchVolume);
        bank.ToolSetDown = DemoAssetLibrary.Audio(old, "ToolSetDown.wav");
        bank.PanelSelect = DemoAssetLibrary.Audio(old, "PanelSelect.wav");
        // The stereo city bed plays 2D; NightAudio's mono air loop keeps the shop's 0.6 blend.
        bank.Ambience = DemoAssetLibrary.Audio(SoundPack, "NightCity_Loop.ogg");
        if (bank.Ambience != null)
        {
            bank.AmbienceVolume = 0.12f;
            bank.AmbienceSpatialBlend = 0;
        }
        else
        {
            bank.Ambience = DemoAssetLibrary.Audio(old, "DistantAir_Loop.wav");
            bank.AmbienceVolume = 0.15f;
            bank.AmbienceSpatialBlend = 0.6f;
        }
        bank.ConeSetDown = Series("ConePlace_", 3);
        bank.MarkerSetDown = Series("EvidenceMarkerPlace_", 3);
        bank.FootstepsHard = RecordedSeries("HardWalk_", 6);
        bank.FootstepsGlass = RecordedSeries("GlassWalk_", 8);
        if (bank.Any) return bank;
        UnityEngine.Object.DestroyImmediate(holder);
        return null;
    }

    // NightSoundscape's file at its level, else NightAudio's fallback at the bank's default.
    private static AudioClip Prefer(string file, float level, string fallback, ref float volume)
    {
        AudioClip clip = DemoAssetLibrary.Audio(SoundPack, file);
        if (clip == null) return DemoAssetLibrary.Audio(NightStoreBuilder.AudioPack, fallback);
        volume = level;
        return clip;
    }

    // stem01.wav .. stemNN.wav from NightSoundscape, the missing ones left out.
    private static AudioClip[] Series(string stem, int count)
    {
        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 1; i <= count; i++)
        {
            AudioClip clip = DemoAssetLibrary.Audio(SoundPack, stem + i.ToString("00") + ".wav");
            if (clip != null) clips.Add(clip);
        }
        return clips.ToArray();
    }

    private static AudioClip[] RecordedSeries(string stem,int count)
    {
        var clips=new List<AudioClip>();for(int i=1;i<=count;i++){var clip=DemoAssetLibrary.Audio("RealFoley",stem+i.ToString("00")+".wav");if(clip)clips.Add(clip);}
        return clips.ToArray();
    }

    // ---------------------------------------------------------------- soundscape

    private static void Soundscape(Transform root, AmbientSound ambience)
    {
        Transform scape = Group(SoundscapeName, root);
        List<AudioSource> exterior = new List<AudioSource>(), interior = new List<AudioSource>();
        if (ambience != null) Add(exterior, ambience.GetComponent<AudioSource>());
        Add(exterior, Loop(scape, "StreetWind", "StreetWind_Loop.ogg", Vector3.zero, 0.075f, 0, 0));

        // The car a build placed (its anchors), in the soundscape's space: the idle at the
        // bonnet, half a metre behind the headlamps; any radio at the dash, half a metre
        // ahead of the light bar.
        PoliceLook.Preset look = PoliceLook.Active;
        PoliceLook.Car car = NightStoreBuilder.PlacedCar.Model != null ? NightStoreBuilder.PlacedCar : PoliceLook.ActiveCar;
        Transform carRoot = root.Find("Exterior/PoliceCar");
        Matrix4x4 carSpace = scape.worldToLocalMatrix * (carRoot != null ? carRoot.localToWorldMatrix
            : root.localToWorldMatrix * Matrix4x4.TRS(L.CarPosition, Quaternion.Euler(0, L.CarYaw, 0), Vector3.one));
        Vector3 lamps = (car.HeadlightLeft + car.HeadlightRight) * 0.5f;
        Vector3 bonnet = carSpace.MultiplyPoint3x4(V(0, lamps.y, lamps.z - 0.5f));
        Vector3 dash = carSpace.MultiplyPoint3x4(V(0, 1.0f, car.LightBar.z + 0.5f));
        if (!string.IsNullOrEmpty(look.CarIdle)) Add(exterior, Loop(scape, CarIdleName, look.CarIdle, bonnet, 0.18f, 1, 25));
        if (!string.IsNullOrEmpty(look.RadioLoop)) Add(exterior, Loop(scape, RadioLoopName, look.RadioLoop, dash, 0.35f, 1, 20));

        Add(interior, Loop(scape, "RoomTone", "HVACRoomTone_Loop.ogg", Vector3.zero, 0.065f, 0, 0));

        // Wind through the forced doors: heard by distance alone, so in neither list; it
        // starts by itself (Unity folds the stereo loop to mono for 3D, PackImport).
        AudioSource gap = Loop(scape, DoorGapName, "WindWhistleGap_Loop.ogg", V(0, 1.2f, 0), 0.045f, 0.5f, 6);
        if (gap != null) gap.playOnAwake = true;

        Events(scape, look, dash, exterior);
        for (int i = 0; i < ShowroomReverbs.Length; i++)
            Reverb(scape, ShowroomReverbNames[i], ShowroomReverbs[i], ShowroomReverbMin, ShowroomReverbMax);
        Reverb(scape, "Reverb_Office", OfficeReverb, OfficeReverbMin, OfficeReverbMax);
        AlarmBell(scape, root, exterior);

        AmbientZones zones = scape.gameObject.AddComponent<AmbientZones>();
        zones.Exterior = exterior.ToArray();
        zones.Interior = interior.ToArray();
        zones.Inside = new[] { RectOf(L.Showroom), RectOf(L.Office) };
    }

    // A looped bed under parent at position (parent's space): 2D without a maxDistance,
    // else 3D with a logarithmic rolloff. Zones start it (playOnAwake off); null without the clip.
    private static AudioSource Loop(Transform parent, string name, string file, Vector3 position, float volume, float minDistance, float maxDistance)
    {
        AudioClip clip = DemoAssetLibrary.Audio(SoundPack, file);
        if (clip == null) return null;
        Transform t = Group(name, parent);
        t.localPosition = position;
        AudioSource s = t.gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = volume;
        // Beds outrank the one-shots when voices run short; music (priority 0) outranks both.
        s.priority = 64;
        s.dopplerLevel = 0;
        s.rolloffMode = AudioRolloffMode.Logarithmic;
        s.spatialBlend = maxDistance > 0 ? 1 : 0;
        if (maxDistance > 0)
        {
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
        }
        return s;
    }

    private static void Add(List<AudioSource> list, AudioSource source)
    {
        if (source != null) list.Add(source);
    }

    // Passing cars (2D: no car passes on the visible street), the CC0 dog, then
    // PoliceLook's sirens (2D, distant) and radio traffic (at the dash), when it has them.
    private static void Events(Transform scape, PoliceLook.Preset look, Vector3 dash, List<AudioSource> exterior)
    {
        List<AmbientEvents.Group> groups = new List<AmbientEvents.Group>();
        AddGroup(groups, CarPassGroup, CarPasses, 0.2f, 20, 60, false, Vector3.zero, 0);
        AddGroup(groups, DogGroup, new[] { DogBark }, 0.2f, 60, 240, false, Vector3.zero, 0);
        AddGroup(groups, SirenGroup, look.Sirens, 0.35f, 120, 360, false, Vector3.zero, 0);
        AddGroup(groups, RadioGroup, look.Radio, 0.4f, 5, 20, true, dash, 20);
        if (groups.Count == 0) return;
        Transform holder = Group(EventsName, scape);
        AmbientEvents events = holder.gameObject.AddComponent<AmbientEvents>();
        events.Groups = groups.ToArray();
        for (int i = 0; i < events.Groups.Length; i++) Add(exterior, events.EnsureSource(i));
    }

    // A group of the files that exist, each at its README level relative to volume; none
    // without a clip (the default PoliceLook lists no sirens and no radio).
    private static void AddGroup(List<AmbientEvents.Group> groups, string name, string[] files, float volume,
        float minSeconds, float maxSeconds, bool spatial, Vector3 position, float maxDistance)
    {
        if (files == null) return;
        List<AudioClip> clips = new List<AudioClip>();
        List<float> gains = new List<float>();
        foreach (string file in files)
        {
            AudioClip clip = DemoAssetLibrary.Audio(SoundPack, file);
            if (clip == null) continue;
            clips.Add(clip);
            gains.Add(Mathf.Min(1, SuggestedLevel(file, volume) / volume));
        }
        if (clips.Count == 0) return;
        groups.Add(new AmbientEvents.Group
        {
            Name = name, Clips = clips.ToArray(), ClipGains = gains.ToArray(), MinSeconds = minSeconds, MaxSeconds = maxSeconds,
            Volume = volume, PitchJitter = 0.04f, Spatial = spatial, Position = position, MaxDistance = maxDistance > 0 ? maxDistance : 30
        });
    }

    // NightSoundscape README levels where takes of one group differ: SirenDistantPass_04 is
    // about 8 dB denser than the other sirens (0.15 against 0.35); radio squelches 0.35 and
    // beeps 0.3 against the chatter's 0.4.
    private static float SuggestedLevel(string file, float groupVolume)
    {
        if (file.StartsWith("SirenDistantPass_04", System.StringComparison.Ordinal)) return 0.15f;
        if (file.StartsWith("RadioSquelch", System.StringComparison.Ordinal)) return 0.35f;
        if (file.StartsWith("RadioBeep", System.StringComparison.Ordinal)) return 0.3f;
        return groupVolume;
    }

    private static void Reverb(Transform scape, string name, Vector3 centre, float minDistance, float maxDistance)
    {
        Transform t = Group(name, scape);
        t.localPosition = centre;
        AudioReverbZone zone = t.gameObject.AddComponent<AudioReverbZone>();
        zone.reverbPreset = AudioReverbPreset.Room;
        zone.minDistance = minDistance;
        zone.maxDistance = maxDistance;
    }

    // The bell ringing from the bell box (3D, audible down the street), as a presenter set
    // shown on demand: its content stays inactive at arrival and, when shown, the bell
    // starts by itself. Exterior, so it is muffled inside the shop.
    private static void AlarmBell(Transform scape, Transform root, List<AudioSource> exterior)
    {
        Transform set = Group(AlarmSetName, scape);
        AudioSource bell = Loop(set, "Bell", AlarmBellClip, set.InverseTransformPoint(root.TransformPoint(L.BellBox)), 0.8f, 2, 60);
        if (bell == null)
        {
            UnityEngine.Object.DestroyImmediate(set.gameObject);
            return;
        }
        bell.playOnAwake = true;
        PresenterSet presenter = set.gameObject.AddComponent<PresenterSet>();
        presenter.Label = "Alarm bell";
        presenter.Note = "Whether the alarm still rings at arrival is a scenario choice.";
        presenter.Content = bell.gameObject;
        presenter.ShownAtStart = false;
        bell.gameObject.SetActive(false);
        exterior.Add(bell);
    }

    // x/z extent of a movement region (Rect y = world z, as NightStoreLayout.RegionRect).
    private static Rect RectOf(InteriorBounds.Region r) => Rect.MinMaxRect(r.MinX, r.MinZ, r.MaxX, r.MaxZ);

    // ---------------------------------------------------------------- music

    private static void Music(Transform root)
    {
        AudioClip bed = DemoAssetLibrary.Audio(MusicPack, InvestigationBed), sting = DemoAssetLibrary.Audio(MusicPack, ResetSting);
        if (bed == null && sting == null) return;
        DemoMusic music = Group(MusicName, root).gameObject.AddComponent<DemoMusic>();
        music.Bed = bed;
        music.Sting = sting;
        music.BedVolume = 0.18f;
        music.StingVolume = 0.5f;
        music.PlayBedAtStart = false;
        music.Session = UnityEngine.Object.FindFirstObjectByType<DemoSession>();
    }
}
#endif
