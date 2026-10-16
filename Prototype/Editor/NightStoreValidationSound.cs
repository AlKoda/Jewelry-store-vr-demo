#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = NightStoreLayout;

// The night store's sound as the realism wiring built it (NightAudioBuilder's soundscape, street
// events, reverb zones, alarm bell and music; Footsteps on the rig; the bank's preferred takes and
// set-downs; the start menu's MenuMusic, built here by DemoBuild.AddMenuMusic), checked in Play
// mode at Tick stage 1 (SoundPlayChecks, right after StagedSoundChecks): every component has run
// Start, the walker is back at the spawn and VR is off. No audio device is assumed, so nothing
// asserts isPlaying: the checks read back what each component set on its sources and filters,
// drive AmbientZones.Step, AmbientEvents.Fire and Footsteps.Advance directly, run the private
// Update of Footsteps and DemoMusic (and the menu music's Start) by reflection (DemoMusic's fades
// at the last frame's unscaled delta) and find the one-shot a set-down leaves among the scene's
// new AudioSources. A check that needs NightSoundscape or Music logs an Info
// line and skips, or checks the silent fallback, without that pack, as in a project unpacked from
// the ZIP alone; a bed or event the pack has a clip for but the build left out fails. File-local
// members carry the Sound prefix, so the other area files of this partial class never collide.
public static partial class NightStoreValidation
{
    private static readonly string[] SoundPacks = { NightAudioBuilder.SoundPack, NightAudioBuilder.MusicPack, NightStoreBuilder.AudioPack };
    // NightAudioBuilder's object names and NightSoundscape files for the beds it builds by name.
    private const string SoundRoomTone = "RoomTone", SoundBell = "Bell", SoundDog = "DogBarkDistant_04";
    private static readonly string[][] SoundBeds =
    {
        new[] { "StreetWind", "StreetWind_Loop.ogg" }, new[] { SoundRoomTone, "HVACRoomTone_Loop.ogg" },
        new[] { NightAudioBuilder.DoorGapName, "WindWhistleGap_Loop.ogg" }
    };

    private static void SoundPlayChecks()
    {
        Transform root = Root();
        List<string> packs = new List<string>();
        foreach (string pack in SoundPacks)
            packs.Add(pack + (AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + pack) ? " present" : " absent"));
        DemoValidation.Info("Sound packs: " + string.Join(", ", packs));

        Transform scape = root.Find(NightAudioBuilder.SoundscapeName);
        AmbientZones zones = scape != null ? scape.GetComponent<AmbientZones>() : null;
        AmbientSound ambience = UnityEngine.Object.FindFirstObjectByType<AmbientSound>();
        AudioSource street = ambience != null ? ambience.GetComponent<AudioSource>() : null;
        DemoValidation.Check(zones != null && street != null, "Night store soundscape and its street bed are built");
        DemoSoundBank bank = UnityEngine.Object.FindFirstObjectByType<DemoSoundBank>();
        ShopWalkController walker = DemoValidation.Find<ShopWalkController>();
        Footsteps steps = walker.GetComponent<Footsteps>();

        SoundZones(root, zones, street);
        SoundLists(scape, zones, street, bank, steps);
        SoundBankTakes(bank, street);
        SoundOffsets(scape, zones);
        SoundEvents(scape, zones);
        SoundCarIdle(root, scape, zones);
        SoundAlarmBell(root, scape, zones);
        SoundReverb(root, scape, bank);
        SoundFootsteps(walker, steps, bank);
        SoundRoomScale(walker, steps);
        SoundSetDowns(bank);
        SoundUnderscore(root);
        SoundMenuMusic();
        DemoFeedback.Clear();
    }

    // ---------------------------------------------------------------- zones

    // AmbientZones.Step jumps (10 s, beyond Smoothing) between the pavement at the spawn and the
    // showroom and office centres, with one half-smoothing step from the street towards the
    // showroom in between. On the street every exterior source plays at its Start level with its
    // filter off and the room tone is silent; inside each is low-passed to about 1.5 kHz (the
    // zones' Cutoff, read back from every filter) at about 55 % and the room tone is at its level;
    // half-way the blend is 0.5 at the log-frequency midpoint, so the door fades rather than
    // switches. The doorway (z 0) counts as street. The blend ends at the listener again.
    private static void SoundZones(Transform root, AmbientZones zones, AudioSource street)
    {
        Vector3 outside = root.TransformPoint(L.PlayerSpawn) + Vector3.up * 1.6f;
        Vector3 showroom = SoundCentre(root, L.Showroom, 1.6f), office = SoundCentre(root, L.Office, 1.6f);
        float streetBase = zones.BaseVolume(street);
        zones.Step(10, outside);
        bool open = zones.Blend == 0 && Mathf.Abs(zones.Cutoff - zones.OutsideCutoff) < 1 && SoundBlended(zones, 0);
        zones.Step(zones.Smoothing * 0.5f, showroom);
        float halfBlend = zones.Blend, halfCutoff = zones.Cutoff, midpoint = Mathf.Sqrt(zones.OutsideCutoff * zones.InsideCutoff);
        bool fades = zones.Smoothing >= 0.5f && Mathf.Abs(halfBlend - 0.5f) < 0.001f && Mathf.Abs(halfCutoff / midpoint - 1) < 0.01f
            && SoundBlended(zones, 0.5f);
        zones.Step(10, showroom);
        bool shop = zones.Blend == 1 && Mathf.Abs(zones.Cutoff - zones.InsideCutoff) < 1 && SoundBlended(zones, 1);
        float muffled = street.volume, cutoff = zones.Cutoff;
        zones.Step(10, outside);
        open &= zones.Blend == 0 && SoundBlended(zones, 0);
        zones.Step(10, office);
        bool back = zones.Blend == 1 && SoundBlended(zones, 1);
        float ratio = streetBase > 0 ? muffled / streetBase : 0;
        bool amount = streetBase > 0 && cutoff > 1000 && cutoff < 2000 && ratio > 0.4f && ratio < 0.7f;
        bool doorway = !zones.IsInside(root.TransformPoint(Vector3.up * 1.6f)) && !zones.IsInside(outside) && zones.IsInside(showroom) && zones.IsInside(office);
        AudioListener listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
        zones.Step(10, listener != null ? listener.transform.position : outside);
        DemoValidation.Info("Zones: " + zones.Exterior.Length + " exterior and " + zones.Interior.Length + " interior sources; street bed "
            + streetBase.ToString("0.00") + " outside, inside at " + (ratio * 100).ToString("0") + " % below " + cutoff.ToString("0") + " Hz; half-way "
            + halfCutoff.ToString("0") + " Hz at blend " + halfBlend.ToString("0.00") + " after " + (zones.Smoothing * 0.5f).ToString("0.00") + " s; street bed "
            + SoundCurve(street));
        DemoValidation.Check(open && shop && back && amount && doorway, "The street is muffled inside the shop and full range on the pavement");
        DemoValidation.Check(fades, "Walking through the door fades the street (half the smoothing time gives half the blend at the log-frequency midpoint)");
    }

    // Every exterior source at its Start level times Lerp(1, InsideExteriorVolume, blend), its
    // low-pass filter on (at the zones' Cutoff) only while blended; every interior bed at its
    // Start level times blend.
    private static bool SoundBlended(AmbientZones zones, float blend)
    {
        bool ok = true;
        float level = Mathf.Lerp(1, zones.InsideExteriorVolume, blend);
        foreach (AudioSource s in zones.Exterior)
        {
            if (s == null) { ok = false; continue; }
            AudioLowPassFilter filter = s.GetComponent<AudioLowPassFilter>();
            ok &= Mathf.Abs(s.volume - zones.BaseVolume(s) * level) < 0.001f && filter != null && filter.enabled == (blend > 0.001f)
                && (!filter.enabled || Mathf.Abs(filter.cutoffFrequency - zones.Cutoff) < 1);
        }
        foreach (AudioSource s in zones.Interior)
            ok &= s != null && Mathf.Abs(s.volume - zones.BaseVolume(s) * blend) < 0.001f;
        return ok;
    }

    // Every source under the soundscape (the inactive bell's included) and the street bed: the
    // room tone in the interior list only, the door whistle in neither, every other one in the
    // exterior list only with its low-pass filter (Prepare adds them at Start, the inactive bell's
    // too). The lists hold nothing else, every bed and event the pack has a clip for is there, and
    // the music, the bank's interface source and the footsteps are never blended.
    private static void SoundLists(Transform scape, AmbientZones zones, AudioSource street, DemoSoundBank bank, Footsteps steps)
    {
        List<AudioSource> sources = new List<AudioSource>(scape.GetComponentsInChildren<AudioSource>(true));
        if (!sources.Contains(street)) sources.Add(street);
        bool sorted = true;
        List<string> names = new List<string>();
        foreach (AudioSource s in sources)
        {
            bool outside = SoundListed(zones.Exterior, s), inside = SoundListed(zones.Interior, s);
            names.Add(s.name + (outside ? " (exterior)" : inside ? " (interior)" : " (neither)"));
            if (s.name == SoundRoomTone) sorted &= inside && !outside;
            else if (s.name == NightAudioBuilder.DoorGapName) sorted &= !inside && !outside;
            else sorted &= outside && !inside && s.GetComponent<AudioLowPassFilter>() != null;
        }
        foreach (AudioSource s in zones.Exterior) sorted &= s != null && sources.Contains(s);
        foreach (AudioSource s in zones.Interior) sorted &= s != null && sources.Contains(s);

        string sound = NightAudioBuilder.SoundPack;
        bool built = true;
        foreach (string[] bed in SoundBeds)
            if (SoundClipAt(sound, bed[1]) != null) built &= SoundNamed(sources, bed[0]) != null;
        string idle = PoliceLook.Active.CarIdle;
        if (!string.IsNullOrEmpty(idle) && SoundClipAt(sound, idle) != null) built &= SoundNamed(sources, NightAudioBuilder.CarIdleName) != null;
        if (SoundClipAt(sound, NightAudioBuilder.AlarmBellClip) != null) built &= SoundNamed(sources, SoundBell) != null;
        bool cars = false;
        foreach (string file in NightAudioBuilder.CarPasses) cars |= SoundClipAt(sound, file) != null;
        if (cars) built &= SoundNamed(sources, NightAudioBuilder.CarPassGroup) != null;
        if (SoundClipAt(sound, NightAudioBuilder.DogBark) != null) built &= SoundNamed(sources, NightAudioBuilder.DogGroup) != null;

        List<AudioSource> dry = new List<AudioSource>();
        DemoMusic music = DemoMusic.Instance;
        if (music != null) { dry.Add(music.BedSource); dry.Add(music.StingSource); }
        if (bank != null) dry.Add(bank.Interface);
        if (steps != null) dry.Add(steps.Source);
        bool apart = true;
        foreach (AudioSource s in dry) apart &= s != null && !SoundListed(zones.Exterior, s) && !SoundListed(zones.Interior, s);
        if (!AssetDatabase.IsValidFolder(DemoAssetLibrary.Root + "/" + sound))
            DemoValidation.Info("NightSoundscape not present: the street bed is the only blended source");
        DemoValidation.Info("Soundscape sources: " + string.Join(", ", names) + "; kept out of the zones: " + dry.Count);
        DemoValidation.Check(sorted && built && apart,
            "Exterior beds, street events and the alarm bell blend with the zones; the door whistle, the music, the interface and the footsteps stay out");
    }

    // AmbientZones.Start chose a start offset inside its clip for every active looped bed in
    // either list (two different ones at least when there are several, so the layers never line
    // up); the door whistle, in neither list, has none and starts by itself from its beginning (a
    // 3D loop on playOnAwake), and the bell, inactive at Start, has none either.
    private static void SoundOffsets(Transform scape, AmbientZones zones)
    {
        List<AudioSource> beds = new List<AudioSource>(zones.Exterior);
        beds.AddRange(zones.Interior);
        List<float> offsets = new List<float>();
        List<string> shown = new List<string>();
        bool chosen = true;
        foreach (AudioSource s in beds)
        {
            if (s == null || s.clip == null || !s.loop || !s.isActiveAndEnabled) continue;
            float offset = zones.StartOffset(s);
            chosen &= offset >= 0 && offset < s.clip.length;
            offsets.Add(offset);
            shown.Add(s.name + " " + offset.ToString("0.0") + " of " + s.clip.length.ToString("0.0") + " s");
        }
        int distinct = 0;
        for (int i = 0; i < offsets.Count; i++)
        {
            bool seen = false;
            for (int j = 0; j < i; j++) seen |= offsets[j] == offsets[i];
            if (!seen) distinct++;
        }
        Transform gapObject = scape.Find(NightAudioBuilder.DoorGapName);
        AudioSource gap = gapObject != null ? gapObject.GetComponent<AudioSource>() : null;
        bool whistle = gapObject == null || (gap != null && zones.StartOffset(gap) == -1 && gap.clip != null && gap.loop && gap.playOnAwake
            && gap.spatialBlend == 1 && gap.isActiveAndEnabled);
        Transform bellObject = scape.Find(NightAudioBuilder.AlarmSetName + "/" + SoundBell);
        AudioSource bell = bellObject != null ? bellObject.GetComponent<AudioSource>() : null;
        bool silentBell = bellObject == null || (bell != null && zones.StartOffset(bell) == -1);
        bool several = offsets.Count >= 2;
        if (!several) DemoValidation.Info("Only " + offsets.Count + " looped bed (NightSoundscape not present): the different-offsets test skipped");
        DemoValidation.Info("Bed start offsets: " + string.Join(", ", shown) + (gapObject != null ? "; door whistle from its start" : "; no door whistle"));
        DemoValidation.Check(offsets.Count >= 1 && chosen && (!several || distinct >= 2) && whistle && silentBell,
            "Looped beds start at random points; the door whistle starts at its beginning");
    }

    // ---------------------------------------------------------------- street events

    // AmbientEvents' groups: the car passes (every CarPasses take the pack has) and the dog with
    // DogBarkDistant_04 alone (01..03 carry a CC BY 3.0 question), no other dog take in any group;
    // each group plays through its own 2D, unlooped child source in the exterior list; the first
    // events are due MinSeconds to MaxSeconds out, and a car fired now schedules the next one after
    // its clip has ended (its length at either pitch-jitter extreme) plus the gap.
    private static void SoundEvents(Transform scape, AmbientZones zones)
    {
        string sound = NightAudioBuilder.SoundPack;
        List<string> carStems = new List<string>();
        foreach (string file in NightAudioBuilder.CarPasses)
            if (SoundClipAt(sound, file) != null) carStems.Add(System.IO.Path.GetFileNameWithoutExtension(file));
        bool dogFile = SoundClipAt(sound, NightAudioBuilder.DogBark) != null;
        if (carStems.Count == 0 && !dogFile)
        {
            DemoValidation.Info("NightSoundscape car passes and dog not present: no street events, AmbientEvents checks skipped");
            return;
        }
        Transform holder = scape.Find(NightAudioBuilder.EventsName);
        AmbientEvents events = holder != null ? holder.GetComponent<AmbientEvents>() : null;
        int car = events != null ? events.IndexOf(NightAudioBuilder.CarPassGroup) : -1, dog = events != null ? events.IndexOf(NightAudioBuilder.DogGroup) : -1;
        bool groups = events != null && (carStems.Count > 0) == (car >= 0) && dogFile == (dog >= 0);
        bool licence = true;
        if (events != null)
            foreach (AmbientEvents.Group g in events.Groups)
                if (g != null && g.Clips != null)
                    foreach (AudioClip clip in g.Clips)
                        licence &= clip != null && (!clip.name.StartsWith("DogBarkDistant_", StringComparison.Ordinal) || clip.name == SoundDog);
        if (dog >= 0)
        {
            AudioClip[] clips = events.Groups[dog].Clips;
            licence &= clips != null && clips.Length == 1 && clips[0] != null && clips[0].name == SoundDog;
        }
        if (car >= 0)
        {
            AudioClip[] clips = events.Groups[car].Clips;
            groups &= clips != null && clips.Length == carStems.Count;
            if (clips != null) foreach (AudioClip clip in clips) groups &= clip != null && carStems.Contains(clip.name);
        }

        float now = Time.time;
        bool sources = true, scheduled = true;
        string due = "";
        foreach (int g in new[] { car, dog })
        {
            if (g < 0) continue;
            AmbientEvents.Group group = events.Groups[g];
            AudioSource s = group.Source;
            sources &= s != null && s.transform.parent == events.transform && s.spatialBlend == 0 && !s.loop && !s.playOnAwake && SoundListed(zones.Exterior, s);
            float next = events.NextTime(g);
            scheduled &= next >= group.MinSeconds && next - now <= SoundLongestWait(group);
            due += (due.Length > 0 ? ", " : "") + group.Name + " in " + (next - now).ToString("0.0") + " s (every " + group.MinSeconds.ToString("0") + "-" + group.MaxSeconds.ToString("0") + " s)";
        }
        bool fired = true;
        if (car >= 0)
        {
            AmbientEvents.Group group = events.Groups[car];
            int played = events.Played;
            events.Fire(car);
            float wait = events.NextTime(car) - now;
            float shortest = SoundClipLength(group, false) / (1 + group.PitchJitter) + Mathf.Max(0.5f, group.MinSeconds) - 0.01f;
            float longest = SoundLongestWait(group);
            fired = events.Played == played + 1 && wait >= shortest && wait <= longest;
            due += "; a car fired now schedules the next in " + wait.ToString("0.0") + " s (" + shortest.ToString("0.0") + ".." + longest.ToString("0.0") + ")";
        }
        DemoValidation.Info("Street events: " + due);
        DemoValidation.Check(groups && licence && sources && scheduled && fired,
            "Cars pass and the CC0 dog barks now and then, each through its own 2D source and after the last has ended");
    }

    // The bank prefers NightSoundscape's takes where the pack has them (NightAudio's or the
    // generated clips otherwise): the DSLR shutter, the torch click and the stereo city bed, which
    // AmbientSound then plays 2D (NightAudio's mono air loop keeps the shop's 0.6 blend).
    private static void SoundBankTakes(DemoSoundBank bank, AudioSource street)
    {
        string sound = NightAudioBuilder.SoundPack;
        AudioClip shutter = SoundClipAt(sound, "CameraShutterDSLR_01.wav"), torch = SoundClipAt(sound, "FlashlightClick_01.wav"),
            city = SoundClipAt(sound, "NightCity_Loop.ogg");
        if (shutter == null && torch == null && city == null)
        {
            DemoValidation.Info("NightSoundscape shutter, torch click and city bed not present: the bank keeps NightAudio's clips, or there is none");
            return;
        }
        bool ok = bank != null && (shutter == null || bank.CameraClick == shutter) && (torch == null || bank.FlashlightSwitch == torch)
            && (city == null || (bank.Ambience == city && street.clip == city && street.spatialBlend == 0));
        DemoValidation.Info("Bank takes: shutter " + (bank != null && bank.CameraClick != null ? bank.CameraClick.name : "none")
            + ", torch " + (bank != null && bank.FlashlightSwitch != null ? bank.FlashlightSwitch.name : "none")
            + "; street bed " + (street.clip != null ? street.clip.name : "none") + " at spatial blend " + street.spatialBlend.ToString("0.0"));
        DemoValidation.Check(ok, "The bank plays NightSoundscape's shutter, torch click and city bed, the bed in 2D");
    }

    // The longest clip of the group at the lowest pitch plus its longest gap (AmbientEvents.Fire), plus 10 ms.
    private static float SoundLongestWait(AmbientEvents.Group group)
        => SoundClipLength(group, true) / Mathf.Max(0.5f, 1 - group.PitchJitter) + Mathf.Max(group.MinSeconds, group.MaxSeconds) + 0.01f;

    // The longest (or shortest) clip length of the group; 0 without clips.
    private static float SoundClipLength(AmbientEvents.Group group, bool longest)
    {
        float length = longest ? 0 : float.MaxValue;
        if (group.Clips != null)
            foreach (AudioClip clip in group.Clips)
                if (clip != null) length = longest ? Mathf.Max(length, clip.length) : Mathf.Min(length, clip.length);
        return length == float.MaxValue ? 0 : length;
    }

    // PoliceLook's engine idle (the default V8 loop) plays 3D from the bonnet of the car the build
    // placed: inside its box collider, on its centre line between the cabin and the headlamps
    // (within a metre behind them), heard at the spawn and, muffled, at the showroom centre, and
    // listed as an exterior bed. The filter's custom cutoff curve is logged: Unity may drive a 3D
    // source's low-pass cutoff by distance through it, over AmbientZones' setting.
    private static void SoundCarIdle(Transform root, Transform scape, AmbientZones zones)
    {
        string file = PoliceLook.Active.CarIdle;
        if (string.IsNullOrEmpty(file) || SoundClipAt(NightAudioBuilder.SoundPack, file) == null)
        {
            DemoValidation.Info("NightSoundscape car idle not present: the police car stays silent, idle check skipped");
            return;
        }
        Transform idleObject = scape.Find(NightAudioBuilder.CarIdleName);
        AudioSource idle = idleObject != null ? idleObject.GetComponent<AudioSource>() : null;
        Transform car = root.Find("Exterior/PoliceCar");
        BoxCollider body = car != null ? car.GetComponent<BoxCollider>() : null;
        Transform left = car != null ? car.Find("HeadlightLeft") : null, right = car != null ? car.Find("HeadlightRight") : null;
        bool ok = idle != null && body != null && left != null && right != null;
        string detail = "idle, car body or headlamp anchors missing";
        if (ok)
        {
            Vector3 local = car.InverseTransformPoint(idle.transform.position), lamps = car.InverseTransformPoint((left.position + right.position) * 0.5f);
            Vector3 d = local - body.center, half = body.size * 0.5f + Vector3.one * 0.001f;
            bool inside = Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y && Mathf.Abs(d.z) <= half.z;
            bool bonnet = local.z > body.center.z && local.z < lamps.z && lamps.z - local.z < 1 && Mathf.Abs(local.x - lamps.x) < 0.1f;
            float eye = Vector3.Distance(root.TransformPoint(L.PlayerSpawn) + Vector3.up * 1.6f, idle.transform.position);
            float shop = Vector3.Distance(SoundCentre(root, L.Showroom, 1.6f), idle.transform.position);
            ok = inside && bonnet && idle.clip != null && idle.clip.name == System.IO.Path.GetFileNameWithoutExtension(file) && idle.loop && idle.isActiveAndEnabled
                && idle.spatialBlend == 1 && eye < idle.maxDistance && shop < idle.maxDistance && SoundListed(zones.Exterior, idle);
            detail = "car space (" + local.x.ToString("0.00") + ", " + local.y.ToString("0.00") + ", " + local.z.ToString("0.00") + "), "
                + (lamps.z - local.z).ToString("0.00") + " m behind the headlamps, " + eye.ToString("0.0") + " m from the spawn eye, maxDistance "
                + idle.maxDistance.ToString("0") + "; " + SoundCurve(idle);
        }
        DemoValidation.Info("Car idle: " + detail);
        DemoValidation.Check(ok, "The V8 idle plays 3D at the police car's bonnet, heard at the spawn and muffled inside with the street");
    }

    // The alarm bell is a presenter set under the soundscape: at arrival its Bell child is inactive
    // (silent); shown, it rings, a 3D loop on playOnAwake at the facade's bell box, heard at the
    // spawn and on the far pavement, an exterior source (muffled inside); hidden again, it stops.
    private static void SoundAlarmBell(Transform root, Transform scape, AmbientZones zones)
    {
        if (SoundClipAt(NightAudioBuilder.SoundPack, NightAudioBuilder.AlarmBellClip) == null)
        {
            DemoValidation.Info("NightSoundscape alarm bell not present: no bell set, bell check skipped");
            return;
        }
        Transform holder = scape.Find(NightAudioBuilder.AlarmSetName);
        PresenterSet set = holder != null ? holder.GetComponent<PresenterSet>() : null;
        AudioSource bell = set != null && set.Content != null ? set.Content.GetComponent<AudioSource>() : null;
        Transform boxObject = root.Find("Facade/BellBox");
        Renderer box = boxObject != null ? boxObject.GetComponent<Renderer>() : null;
        bool arrival = bell != null && set.Content != set.gameObject && set.Content.transform.IsChildOf(holder) && !set.ShownAtStart && !set.Shown
            && !bell.isActiveAndEnabled && bell.clip != null && bell.clip.name == "AlarmBell_Close_Loop" && bell.loop && bell.playOnAwake
            && bell.spatialBlend == 1 && SoundListed(zones.Exterior, bell);
        float offset = box != null && bell != null ? Vector3.Distance(bell.transform.position, box.bounds.center) : -1;
        float spawn = bell != null ? Vector3.Distance(root.TransformPoint(L.PlayerSpawn) + Vector3.up * 1.6f, bell.transform.position) : -1;
        float far = bell != null ? Vector3.Distance(root.TransformPoint(new Vector3(0, 1.6f, (L.FarKerbZ + L.FarPavementZ) * 0.5f)), bell.transform.position) : -1;
        bool placed = offset >= 0 && offset < 0.3f && spawn < bell.maxDistance && far < bell.maxDistance;
        bool rings = false, stops = false;
        if (set != null && bell != null)
        {
            set.SetShown(true);
            rings = set.Shown && bell.isActiveAndEnabled;
            set.SetShown(false);
            stops = !set.Shown && !bell.gameObject.activeSelf;
        }
        DemoValidation.Info("Alarm bell: " + offset.ToString("0.000") + " m from the bell box's centre, " + spawn.ToString("0.0") + " m from the spawn eye and "
            + far.ToString("0.0") + " m from the far pavement" + (bell != null ? " (maxDistance " + bell.maxDistance.ToString("0") + ")" : ""));
        DemoValidation.Check(arrival && placed && rings && stops, "The alarm bell rings from the bell box only while its set is shown");
    }

    // ---------------------------------------------------------------- reverb

    // Every reverb zone in the scene is an enabled Room zone of the soundscape standing in the
    // showroom or the office: the showroom's and the office's centres are each within a zone of
    // their room's minDistance (full reverb) and the spawn eye is beyond every zone's maxDistance
    // (none); at ear height the reverb is full across the showroom at mid-depth from side wall to
    // side wall, and none along the pavement's inner edge (z -0.35), as NightAudioBuilder lays the
    // zones out; the music and the interface clicks bypass them. How wet the pavement outside the
    // door, the showroom's front corners and its side-wall cases are (a linear estimate between
    // minDistance and maxDistance) is logged, for listening on the PC.
    private static void SoundReverb(Transform root, Transform scape, DemoSoundBank bank)
    {
        AudioReverbZone[] all = UnityEngine.Object.FindObjectsByType<AudioReverbZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Vector3 eye = root.TransformPoint(L.PlayerSpawn) + Vector3.up * 1.6f;
        Vector3 showroomCentre = SoundCentre(root, L.Showroom, 1.5f), officeCentre = SoundCentre(root, L.Office, 1.5f);
        bool rooms = all.Length > 0, showroomFull = false, officeFull = false;
        foreach (AudioReverbZone zone in all)
        {
            Vector3 at = zone.transform.position, local = root.InverseTransformPoint(at);
            bool inShowroom = L.Showroom.Contains(local), inOffice = L.Office.Contains(local);
            rooms &= zone.isActiveAndEnabled && zone.transform.IsChildOf(scape) && zone.reverbPreset == AudioReverbPreset.Room
                && (inShowroom || inOffice) && Vector3.Distance(eye, at) > zone.maxDistance;
            showroomFull |= inShowroom && Vector3.Distance(showroomCentre, at) <= zone.minDistance;
            officeFull |= inOffice && Vector3.Distance(officeCentre, at) <= zone.minDistance;
        }
        DemoMusic music = DemoMusic.Instance;
        bool dry = (music == null || (music.BedSource != null && music.BedSource.bypassReverbZones && music.StingSource != null && music.StingSource.bypassReverbZones))
            && (bank == null || (bank.Interface != null && bank.Interface.bypassReverbZones));
        float pavement = SoundWet(all, root.TransformPoint(new Vector3(0, 1.6f, -1.5f)));
        float corner = SoundWet(all, root.TransformPoint(new Vector3(L.Showroom.MaxX, 1.5f, L.Showroom.MinZ)));
        float side = SoundWet(all, root.TransformPoint(new Vector3(L.Showroom.MaxX, 1.5f, (L.Showroom.MinZ + L.Showroom.MaxZ) * 0.5f)));
        // 29 ear-height points each along the showroom's mid-depth line and the pavement's inner edge.
        float leastAcross = 1, mostOnEdge = 0;
        for (int i = 0; i <= 28; i++)
        {
            float t = i / 28f;
            leastAcross = Mathf.Min(leastAcross, SoundWet(all, root.TransformPoint(new Vector3(Mathf.Lerp(L.Showroom.MinX, L.Showroom.MaxX, t), 1.6f,
                (L.Showroom.MinZ + L.Showroom.MaxZ) * 0.5f))));
            mostOnEdge = Mathf.Max(mostOnEdge, SoundWet(all, root.TransformPoint(new Vector3(Mathf.Lerp(L.Pavement.MinX, L.Pavement.MaxX, t), 1.6f, L.Pavement.MaxZ))));
        }
        DemoValidation.Info("Reverb: " + all.Length + " zones; estimated wet share on the pavement outside the door " + pavement.ToString("0.00")
            + ", at the showroom's front corners " + corner.ToString("0.00") + ", at its side-wall cases " + side.ToString("0.00")
            + ", least across mid-depth " + leastAcross.ToString("0.00") + ", most along the pavement's inner edge " + mostOnEdge.ToString("0.00") + " (1 full, 0 dry)");
        DemoValidation.Check(rooms && showroomFull && officeFull && leastAcross >= 1 && mostOnEdge <= 0 && dry,
            "Room reverb fills the showroom wall to wall and the office, and stops before the pavement; music and interface sounds stay dry");
    }

    // The largest share of reverb any zone gives at p: 1 within its minDistance, 0 beyond its
    // maxDistance, linear in between (an estimate; Unity's own falloff curve may differ).
    private static float SoundWet(AudioReverbZone[] zones, Vector3 p)
    {
        float wet = 0;
        foreach (AudioReverbZone zone in zones)
        {
            float d = Vector3.Distance(p, zone.transform.position), span = zone.maxDistance - zone.minDistance;
            wet = Mathf.Max(wet, d <= zone.minDistance ? 1 : span > 0 ? Mathf.Clamp01((zone.maxDistance - d) / span) : 0);
        }
        return wet;
    }

    // ---------------------------------------------------------------- footsteps

    // Desktop footsteps on the rig root (the walker's object, with XRLocomotion), with the bank's
    // hard and glass takes and the dressing's glass zones, through a 2D source. Driven by Advance
    // in 5 cm moves: 1.6 m on the pavement left of the entrance fans steps after 0.70-0.75 m and
    // 1.40-1.45 m, hard both times; the same walk through the widest glass zone (the outside fan)
    // steps on glass both times; a 2 m move is a jump: not walked, and the stride starts again.
    private static void SoundFootsteps(ShopWalkController walker, Footsteps steps, DemoSoundBank bank)
    {
        if (steps == null)
        {
            DemoValidation.Info("NightSoundscape footsteps not present: the walker stays silent");
            DemoValidation.Check(bank == null || (!DemoSoundBank.Has(bank.FootstepsHard) && !DemoSoundBank.Has(bank.FootstepsGlass)),
                "The walker has footsteps whenever the bank has footstep takes");
            return;
        }
        Rect[] planned = NightStoreDressing.GlassZones();
        Rect[] zones = steps.GlassZones;
        bool wired = steps.transform == walker.transform && walker.GetComponent<XRLocomotion>() != null && bank != null
            && SoundSameClips(steps.Hard, bank.FootstepsHard) && SoundSameClips(steps.Glass, bank.FootstepsGlass)
            && (DemoSoundBank.Has(steps.Hard) || DemoSoundBank.Has(steps.Glass))
            && steps.Source != null && steps.Source.spatialBlend == 0 && !steps.Source.playOnAwake && !steps.Source.loop
            && zones != null && zones.Length == planned.Length;
        int widest = -1;
        for (int i = 0; wired && i < planned.Length; i++)
        {
            wired &= Mathf.Abs(zones[i].xMin - planned[i].xMin) < 1e-4f && Mathf.Abs(zones[i].yMin - planned[i].yMin) < 1e-4f
                && Mathf.Abs(zones[i].width - planned[i].width) < 1e-4f && Mathf.Abs(zones[i].height - planned[i].height) < 1e-4f;
            if (widest < 0 || zones[i].width > zones[widest].width) widest = i;
        }
        bool hard = SoundStride(steps, new Vector3(-6, 0, -1.2f), false, out string hardTrace);
        bool glass = false, jump = false;
        string glassTrace = "no glass zone 1.8 m wide";
        if (widest >= 0 && zones[widest].width >= 1.8f)
        {
            Vector3 start = new Vector3(zones[widest].center.x - 0.8f, 0, zones[widest].center.y), end = start + Vector3.right * 1.6f;
            glass = SoundStride(steps, start, true, out glassTrace);
            int walked = steps.Steps;
            jump = !steps.Advance(end + Vector3.right * 2) && !steps.Advance(end + Vector3.right * 2.6f) && steps.Steps == walked;
        }
        steps.Anchor(steps.transform.position);
        DemoValidation.Info("Footsteps: " + (steps.Hard != null ? steps.Hard.Length : 0) + " hard and " + (steps.Glass != null ? steps.Glass.Length : 0)
            + " glass takes, " + (zones != null ? zones.Length : 0) + " glass zones; pavement walk stepped at " + hardTrace + ", glass walk at " + glassTrace);
        DemoValidation.Check(wired && hard && glass && jump, "Desktop footsteps step every 0.7 m, crunch in the glass zones and skip jumps");
    }

    // Anchors at start and walks 1.6 m along +x in 32 moves of 5 cm: true when exactly two steps
    // fell, after 0.70-0.75 m and 1.40-1.45 m of travel, each on glass exactly when onGlass (and
    // the walk's ends lie on that surface).
    private static bool SoundStride(Footsteps steps, Vector3 start, bool onGlass, out string trace)
    {
        steps.Anchor(start);
        int before = steps.Steps;
        List<int> at = new List<int>();
        bool surface = steps.OnGlass(start) == onGlass && steps.OnGlass(start + Vector3.right * 1.6f) == onGlass;
        trace = "";
        for (int k = 1; k <= 32; k++)
            if (steps.Advance(start + Vector3.right * (0.05f * k)))
            {
                at.Add(k);
                surface &= steps.LastOnGlass == onGlass;
                trace += (trace.Length > 0 ? " and " : "") + (0.05f * k).ToString("0.00") + " m" + (steps.LastOnGlass ? " (glass)" : " (hard)");
            }
        if (trace.Length == 0) trace = "no step";
        return surface && steps.Steps == before + 2 && at.Count == 2 && (at[0] == 14 || at[0] == 15) && (at[1] == 28 || at[1] == 29);
    }

    // VR: Footsteps follows the rig root, so the head walking a metre in 5 cm frames (room-scale,
    // every move below the jump speed) never steps; a teleport to the pavement and two snap turns
    // (each swings the root round the head, a metre off by then) are jumps. One Footsteps.Update
    // follows every move; the head pose, desktop mode and the spawn are put back.
    private static void SoundRoomScale(ShopWalkController walker, Footsteps steps)
    {
        if (steps == null)
        {
            DemoValidation.Info("NightSoundscape footsteps not present: VR footstep check skipped");
            return;
        }
        DemoModeSwitch mode = DemoValidation.Find<DemoModeSwitch>();
        XRLocomotion locomotion = DemoValidation.Find<XRLocomotion>();
        System.Reflection.MethodInfo update = typeof(Footsteps).GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (update == null) throw new Exception("FAILED: Footsteps.Update not found for the VR footstep check");
        Transform head = locomotion.Head;
        Vector3 headAt = head.localPosition;
        Quaternion headTurn = head.localRotation;
        mode.Apply(true);
        steps.Anchor(steps.transform.position);
        int n = steps.Steps;
        for (int i = 0; i < 20; i++)
        {
            head.localPosition += new Vector3(0.05f, 0, 0);
            update.Invoke(steps, null);
        }
        bool roomScale = steps.Steps == n && steps.transform == locomotion.transform;
        Vector3 before = locomotion.transform.position;
        bool teleported = locomotion.TryTeleport(new Vector3(-1, 0, -1.5f));
        float jump = Flat(locomotion.transform.position - before);
        update.Invoke(steps, null);
        teleported &= steps.Steps == n;
        float yaw = locomotion.transform.eulerAngles.y;
        for (int i = 0; i < 2; i++)
        {
            locomotion.SnapRight();
            update.Invoke(steps, null);
        }
        float turn = Mathf.DeltaAngle(yaw, locomotion.transform.eulerAngles.y);
        bool turned = steps.Steps == n && Mathf.Abs(turn) > 30;
        int stepped = steps.Steps - n;
        head.localPosition = headAt;
        head.localRotation = headTurn;
        mode.Apply(false);
        walker.ResetPosition();
        steps.Anchor(steps.transform.position);
        DemoValidation.Info("VR footsteps: head walked 1 m, the teleport moved the rig " + jump.ToString("0.00") + " m, two snap turns turned it "
            + turn.ToString("0") + " degrees; steps played " + stepped);
        DemoValidation.Check(roomScale && teleported && turned && !mode.VRActive, "Room-scale walking, teleports and snap turns make no footsteps in VR");
    }

    // ---------------------------------------------------------------- set-downs

    // DemoSounds.SetDownClip gives the bank's cone and marker takes (two cone set-downs in a row
    // differ) and the Thud for every other kind (all of them thud without the takes); placing a cone
    // and a scale on the pavement with the desktop pointer (ToolHolder.Settle) plays that clip there,
    // the cone take at the bank's cone level. No marker is spawned: the marker sequence stays as it is.
    private static void SoundSetDowns(DemoSoundBank bank)
    {
        AudioClip thud = DemoSounds.Clip(DemoSound.Thud);
        bool cones = bank != null && DemoSoundBank.Has(bank.ConeSetDown), markers = bank != null && DemoSoundBank.Has(bank.MarkerSetDown);
        AudioClip a = DemoSounds.SetDownClip(DemoToolKind.Cone), b = DemoSounds.SetDownClip(DemoToolKind.Cone);
        bool mapped = thud != null && (cones ? SoundHolds(bank.ConeSetDown, a) && SoundHolds(bank.ConeSetDown, b) && (bank.ConeSetDown.Length < 2 || a != b)
            : a == thud && b == thud);
        AudioClip marker = DemoSounds.SetDownClip(DemoToolKind.Marker);
        mapped &= markers ? SoundHolds(bank.MarkerSetDown, marker) : marker == thud;
        foreach (DemoToolKind kind in new[] { DemoToolKind.TapePost, DemoToolKind.Scale, DemoToolKind.Measure, DemoToolKind.Lamp })
            mapped &= DemoSounds.SetDownClip(kind) == thud;

        DesktopInteractor desktop = DemoValidation.Find<DesktopInteractor>();
        Vector3 floor = new Vector3(3, 0, -1.5f);
        AudioSource cone = SoundSetDownShot(desktop, DemoToolKind.Cone, floor), scale = SoundSetDownShot(desktop, DemoToolKind.Scale, floor);
        bool live = cone != null && scale != null && scale.clip == thud
            && (cones ? SoundHolds(bank.ConeSetDown, cone.clip) && Mathf.Abs(cone.volume - bank.SetDownVolume(DemoToolKind.Cone)) < 1e-4f : cone.clip == thud);
        if (!cones) DemoValidation.Info("NightSoundscape cone takes not present: every tool sets down with the Thud");
        DemoValidation.Info("Set-downs on the pavement: cone " + (cone != null ? cone.clip.name + " at " + cone.volume.ToString("0.00") : "none")
            + ", scale " + (scale != null ? scale.clip.name : "none") + (markers ? "; markers have " + bank.MarkerSetDown.Length + " takes" : ""));
        DemoValidation.Check(mapped && live, "Cones and markers set down with their own takes; other tools keep the thud");
    }

    // Places a new tool of kind at floor with the desktop pointer (taken into the hand, carried there
    // from 2 m above, placed) and returns the one-shot its set-down left there: the one new
    // AudioSource within 5 cm of the floor point that is not the pick-up click; null when the tool
    // missed the point or there is not exactly one. The tool is removed again.
    private static AudioSource SoundSetDownShot(DesktopInteractor desktop, DemoToolKind kind, Vector3 floor)
    {
        HashSet<AudioSource> before = new HashSet<AudioSource>(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None));
        DeployedTool tool = desktop.SpawnIntoHand(kind);
        if (tool == null) return null;
        desktop.Carry(new Ray(floor + Vector3.up * 2, Vector3.down));
        desktop.Place();
        bool landed = desktop.Held == null && Vector3.Distance(tool.transform.position, floor) < 0.02f;
        AudioClip click = DemoSounds.Clip(DemoSound.Click);
        AudioSource shot = null;
        int found = 0;
        foreach (AudioSource s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (!before.Contains(s) && s.clip != null && s.clip != click && Vector3.Distance(s.transform.position, floor) < 0.05f)
            {
                shot = s;
                found++;
            }
        desktop.Remove(tool);
        return landed && found == 1 ? shot : null;
    }

    // ---------------------------------------------------------------- music

    // The investigation underscore: DemoMusic on NightStore/Music with InvestigationLoop and
    // ResetSting, wired to the session (for the reset sting), 2D, off and silent at arrival. The
    // presenter switch turns it on (its clip on the bed source) with a monitor-only toast; driven at
    // the last frame's unscaled delta, its Update fades the bed up to BedVolume, a sting ducks it by
    // about 6 dB (to 60 % or less), and switched off it fades to silence.
    private static void SoundUnderscore(Transform root)
    {
        bool bedFile = SoundClipAt(NightAudioBuilder.MusicPack, NightAudioBuilder.InvestigationBed) != null;
        bool stingFile = SoundClipAt(NightAudioBuilder.MusicPack, NightAudioBuilder.ResetSting) != null;
        if (!bedFile && !stingFile)
        {
            DemoValidation.Info("Music InvestigationLoop.ogg and ResetSting.ogg not present: no underscore and no reset sting, music checks skipped");
            return;
        }
        Transform holder = root.Find(NightAudioBuilder.MusicName);
        DemoMusic music = DemoMusic.Instance;
        bool built = music != null && holder != null && music.transform == holder && music.Session != null
            && music.Session == UnityEngine.Object.FindFirstObjectByType<DemoSession>()
            && (bedFile ? music.Bed != null && music.Bed.name == "InvestigationLoop" : music.Bed == null)
            && (stingFile ? music.Sting != null && music.Sting.name == "ResetSting" : music.Sting == null)
            && music.BedSource != null && music.StingSource != null && music.BedSource.spatialBlend == 0 && music.StingSource.spatialBlend == 0;
        DemoValidation.Check(built, "The underscore and the reset sting play from NightStore/Music, 2D, wired to the session");
        bool arrival = !music.BedOn && music.BedSource.volume == 0;
        if (music.Bed == null)
        {
            DemoValidation.Info("Music InvestigationLoop.ogg not present: no underscore switch");
            DemoValidation.Check(arrival, "No underscore plays at arrival");
            return;
        }
        DemoFeedback.Clear();
        music.ToggleBed();
        bool on = music.BedOn && music.BedSource.clip == music.Bed && SoundToast("Music on");
        float dt = Time.unscaledDeltaTime;
        int fade = SoundCalls(music.FadeSeconds, dt), duck = SoundCalls(0.3f, dt);
        System.Reflection.MethodInfo update = typeof(DemoMusic).GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bool level = true, ducked = true;
        string detail = "fades not driven (unscaled frame time " + dt.ToString("0.0000") + " s)";
        if (fade > 0 && duck > 0)
        {
            SoundPump(update, music, fade);
            float full = music.BedSource.volume, under = full;
            level = music.BedVolume > 0 && Mathf.Abs(full - music.BedVolume) < 0.001f;
            if (music.Sting != null)
            {
                int stings = music.StingsPlayed;
                music.PlaySting();
                SoundPump(update, music, duck);
                under = music.BedSource.volume;
                ducked = music.StingsPlayed == stings + 1 && under > 0 && under <= 0.6f * full;
            }
            detail = "bed " + full.ToString("0.000") + " after " + fade + " frames, " + under.ToString("0.000") + " under the sting";
        }
        music.ToggleBed();
        bool off = !music.BedOn && SoundToast("Music off");
        if (fade > 0)
        {
            SoundPump(update, music, fade);
            off &= music.BedSource.volume == 0;
        }
        DemoFeedback.Clear();
        DemoValidation.Info("Underscore: " + detail);
        DemoValidation.Check(arrival && on && level && ducked && off,
            "The underscore is off at arrival and its switch turns it on and off with a monitor-only toast; a sting ducks it");
    }

    // The start menu's music path, on the launcher's own MenuMusic built here as CreateLauncherScene
    // saves it (DemoBuild.AddMenuMusic: its theme, level, fade time and PlayBedAtStart). Its Start
    // (run by reflection: Unity would only run it next frame) plays the theme; driven at the last
    // frame's unscaled delta, its Update fades the bed in over FadeSeconds (barely started after
    // one frame, half-way at half the time) up to its BedVolume, and FadeOut with
    // DemoLauncher.MusicFadeSeconds halves bed and sting half-way and silences both by the time
    // DemoLauncher loads the map. The object is destroyed and the night's DemoMusic is Instance again.
    private static void SoundMenuMusic()
    {
        float dt = Time.unscaledDeltaTime, fadeOut = DemoLauncher.MusicFadeSeconds;
        DemoMusic night = DemoMusic.Instance;
        System.Reflection.BindingFlags own = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.MethodInfo start = typeof(DemoMusic).GetMethod("Start", own), update = typeof(DemoMusic).GetMethod("Update", own);
        DemoMusic menu = null;
        bool built = false, driven = false, fadesIn = false, fadesOut = false;
        string detail = "the unscaled frame time " + dt.ToString("0.0000") + " s cannot drive the fades frame by frame";
        try
        {
            // Awake runs inside AddComponent, before the theme is assigned (the saved scene has it
            // already); SetBed puts the clip on the bed source either way.
            menu = DemoBuild.AddMenuMusic();
            built = menu != null;
            if (built)
            {
                if (start == null) throw new Exception("FAILED: DemoMusic.Start not found for the menu theme check");
                start.Invoke(menu, null);
                AudioClip theme = menu.Bed;
                float fadeIn = menu.FadeSeconds, target = menu.BedVolume;
                int inCalls = SoundCalls(fadeIn, dt), outCalls = SoundCalls(fadeOut, dt);
                // Two frames per fade at least, so the half-way readings mean something.
                driven = inCalls > 0 && outCalls > 0 && dt <= Mathf.Min(fadeIn, fadeOut) * 0.5f;
                if (driven)
                {
                    int half = Mathf.Max(1, Mathf.RoundToInt(fadeIn * 0.5f / dt)), outHalf = Mathf.Max(1, Mathf.RoundToInt(fadeOut * 0.5f / dt));
                    SoundPump(update, menu, 1);
                    float first = menu.BedSource.volume;
                    SoundPump(update, menu, half - 1);
                    float middle = menu.BedSource.volume, expected = target * Mathf.Min(1, half * dt / fadeIn);
                    SoundPump(update, menu, Mathf.Max(0, inCalls - half));
                    float full = menu.BedSource.volume;
                    fadesIn = menu.BedOn && theme != null && menu.BedSource.clip == theme && theme.name == "MenuTheme" && target > 0
                        && first > 0 && first < 0.3f * target && Mathf.Abs(middle - expected) < 0.02f * target && Mathf.Abs(full - target) < 0.001f;

                    menu.FadeOut(fadeOut);
                    SoundPump(update, menu, outHalf);
                    float share = 1 - Mathf.Min(1, outHalf * dt / fadeOut), bedHalf = menu.BedSource.volume, stingHalf = menu.StingSource.volume;
                    SoundPump(update, menu, Mathf.Max(0, outCalls - outHalf));
                    fadesOut = Mathf.Abs(stingHalf - share) < 0.02f && Mathf.Abs(bedHalf - target * share) < 0.02f * target
                        && menu.BedSource.volume < 1e-6f && menu.StingSource.volume < 1e-6f;
                    detail = "bed " + target.ToString("0.00") + " over " + fadeIn.ToString("0.0") + " s; frame " + dt.ToString("0.0000") + " s; "
                        + first.ToString("0.000") + " after one frame, " + middle.ToString("0.000") + " half-way (" + expected.ToString("0.000") + " expected), "
                        + full.ToString("0.000") + " after " + inCalls + " frames; fade-out " + bedHalf.ToString("0.000") + " half-way, "
                        + menu.BedSource.volume.ToString("0.000") + " after " + outCalls + " frames (" + fadeOut + " s)";
                }
            }
        }
        finally
        {
            if (built) UnityEngine.Object.DestroyImmediate(menu.gameObject);
            typeof(DemoMusic).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { night });
        }
        if (!built)
        {
            DemoValidation.Info("Music MenuTheme.ogg not present: the start menu is silent, menu theme fade not checked");
            return;
        }
        if (!driven)
        {
            DemoValidation.Info("Menu theme fade not checked: " + detail);
            return;
        }
        DemoValidation.Info("Menu theme (DemoBuild.AddMenuMusic): " + detail);
        DemoValidation.Check(fadesIn && fadesOut && DemoMusic.Instance == night,
            "The start menu plays its theme, fading in, then to silence over DemoLauncher.MusicFadeSeconds before a map loads");
    }

    // ---------------------------------------------------------------- helpers

    // Update calls covering seconds at the frame delta dt (2 % and two calls over, for float
    // drift), or -1 when dt cannot drive a fade synchronously (no delta yet, or too many calls).
    private static int SoundCalls(float seconds, float dt)
        => dt > 0 && seconds / dt <= 50000 ? Mathf.CeilToInt(seconds / dt * 1.02f) + 2 : -1;

    private static void SoundPump(System.Reflection.MethodInfo update, object target, int calls)
    {
        if (update == null) throw new Exception("FAILED: " + target.GetType().Name + ".Update not found for the fade check");
        for (int i = 0; i < calls; i++) update.Invoke(target, null);
    }

    // The newest toast is text and monitor-only, and the headset has nothing to show (the caller
    // cleared the feed first, so only monitor-only toasts are in it).
    private static bool SoundToast(string text)
    {
        IReadOnlyList<DemoFeedback.Message> recent = DemoFeedback.Recent;
        return recent.Count > 0 && recent[recent.Count - 1].Text == text && recent[recent.Count - 1].DesktopOnly && !DemoFeedback.TryNewestForHeadset(out _);
    }

    // The custom cutoff curve of the source's low-pass filter, for the log: when it has keys, Unity
    // may drive the cutoff by listener distance through it at runtime, over AmbientZones' setting
    // (nothing here can tell; listen on the PC whether the street is muffled inside).
    private static string SoundCurve(AudioSource source)
    {
        AudioLowPassFilter filter = source != null ? source.GetComponent<AudioLowPassFilter>() : null;
        AnimationCurve curve = filter != null ? filter.customCutoffCurve : null;
        if (filter == null) return "no low-pass filter";
        if (curve == null || curve.length == 0) return "low-pass custom cutoff curve none";
        return "low-pass custom cutoff curve " + curve.length + " keys, " + curve.Evaluate(0).ToString("0.00") + " at the source, "
            + curve.Evaluate(1).ToString("0.00") + " at maxDistance";
    }

    // A pack's audio file as an asset, or null (without recording a credit, unlike DemoAssetLibrary.Audio).
    private static AudioClip SoundClipAt(string pack, string file) => AssetDatabase.LoadAssetAtPath<AudioClip>(DemoAssetLibrary.Path(pack, file));

    private static Vector3 SoundCentre(Transform root, InteriorBounds.Region r, float y)
        => root.TransformPoint(new Vector3((r.MinX + r.MaxX) * 0.5f, y, (r.MinZ + r.MaxZ) * 0.5f));

    private static bool SoundListed(AudioSource[] list, AudioSource source)
    {
        if (list == null || source == null) return false;
        foreach (AudioSource s in list) if (s == source) return true;
        return false;
    }

    private static AudioSource SoundNamed(List<AudioSource> sources, string name)
    {
        foreach (AudioSource s in sources) if (s != null && s.name == name) return s;
        return null;
    }

    private static bool SoundHolds(AudioClip[] clips, AudioClip clip)
    {
        if (clips == null || clip == null) return false;
        foreach (AudioClip c in clips) if (c == clip) return true;
        return false;
    }

    private static bool SoundSameClips(AudioClip[] a, AudioClip[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
}
#endif
