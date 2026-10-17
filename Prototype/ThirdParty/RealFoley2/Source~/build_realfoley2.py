"""Build Prototype/ThirdParty/RealFoley2 from downloaded CC0 / CC BY recordings.

Usage: python3 build_realfoley2.py <download_root> <out_dir>
Needs numpy, scipy and ffmpeg (libvorbis). Uses the same helpers and the same targets as
RealFoley/Source~/build_realfoley.py (lib.py is a verbatim copy of its first 262 lines).
Writes the clips and build_log.json (per-file processing record used to write sources.json).
"""
import sys, json, os
import numpy as np
import lib
from lib import load, hpf, fade, trim, onsets, norm_oneshot, loopify, lufs, write, oneshot, split_set, SR, LOG
from scipy.signal import butter, sosfilt

lib.DL = sys.argv[1]
lib.OUT = sys.argv[2]
os.makedirs(lib.OUT, exist_ok=True)
E = "esc/audio/"


def clock_loop(name, src, periods=4, hp=60):
    """Seamless ticking loop: an exact whole number of tick periods, wrapped between ticks."""
    x = hpf(load(src), hp)
    st = np.array(onsets(x, thresh_db=-20, gap=0.3))
    d = np.diff(st)
    per = float(np.median(d))
    # unit = two ticks if the clip alternates tick/tock, else one tick
    unit = per
    start = max(0, st[0] - 0.35 * per) / SR
    n = periods
    while start + (n + 1) * unit / SR > len(x) / SR - 0.06 and n > 1:
        n -= 1
    length = n * unit / SR
    xf = 0.06
    y = loopify(x, length, xf, start)
    y = y * 10 ** ((-24 - lufs(y)) / 20)
    pk = np.max(np.abs(y))
    if pk > 10 ** (-1 / 20):
        y *= 10 ** (-1 / 20) / pk
    write(name, y, "loop", [src],
          "mono 48 kHz; %d Hz high-pass; loop of %d tick periods (period %.3f s from onset detection) = %.3f s starting %.3f s, wrapped between ticks with a %.2f s equal-power crossfade; integrated loudness to -24 LUFS (capped at -1 dBFS peak)"
          % (hp, n, unit / SR, length, start, xf), loop=True)


def lp_loop(name, src, length, xf, start, hp=30, mono=True, lp=None, target=-24.0):
    lib_loop = lib.loop
    lib_loop(name, src, length, xf=xf, start=start, mono=mono, hp=hp, lp=lp, target=target)


# ---------------- footsteps ----------------
# carpet (mlsulli, ESC-50 excerpt of Freesound 234855)
k = split_set("Footstep_Carpet", E + "5-234855-A-25.wav", 8, start_index=6, thresh_db=-26, gap=0.25, maxlen=0.6)
# pavement / police boots
k = split_set("Footstep_Pavement", E + "5-244310-A-25.wav", 8, thresh_db=-22, gap=0.25, maxlen=0.55)
k = split_set("Footstep_Pavement", E + "3-94343-A-25.wav", 6, start_index=k, thresh_db=-22, gap=0.25, maxlen=0.55)
k = split_set("Footstep_Pavement", E + "3-249913-A-25.wav", 4, start_index=k, thresh_db=-20, gap=0.3, maxlen=0.55)
for i, n in enumerate([1, 2, 3, 4, 5, 6]):
    oneshot("Footstep_Boot_%02d.wav" % (i + 1), "fedraw/step_asphalt_%02d.wav" % n)
for i, n in enumerate([1, 2, 3, 5, 6, 7, 8, 9]):
    oneshot("Footstep_BootLeather_%02d.wav" % (i + 1), "kenney/footstep%02d.ogg" % n)
# HQ replacements for Footstep_Concrete_01..06 (same recording, PCM ESC-50 excerpts instead of 64-112 kbps MP3)
hq = []
for c in "BCDE":
    x = hpf(load(E + "1-155858-%s-25.wav" % c), 40)
    for a, e in lib.split(x, 0.55, -22, 0.3):
        if e - a > int(0.12 * SR):
            hq.append((c, a, e, x))
pick = hq[1::max(1, len(hq) // 6)][:6]
for i, (c, a, e, x) in enumerate(pick):
    seg = fade(trim(x[a:e]))
    y, capped = norm_oneshot(seg)
    write("Footstep_Concrete_%02d_HQ.wav" % (i + 1), y, "one-shot", [E + "1-155858-%s-25.wav" % c],
          "step cut at %.3f-%.3f s by onset detection; mono 48 kHz; 40 Hz high-pass; trimmed; fades; active-region loudness to -18 LUFS%s; same Freesound recording as RealFoley/Footstep_Concrete_01..06 (a different step of it), PCM source instead of the MP3"
          % (a / SR, e / SR, ", peak-capped at -1 dBFS" if capped else ""))

# ---------------- bells and alarms ----------------
lib.loop("AlarmBell_Loop.ogg", "ve/65_school_bell.mp3", 2.0, xf=0.3, start=0.25, mono=True, hp=150)
lib.loop("AlarmBellSmall_Loop.ogg", E + "3-131943-A-37.wav", 4.0, xf=0.5, start=0.3, mono=True, hp=150)
lib.loop("AlarmBellClock_Loop.ogg", E + "2-102435-A-37.wav", 1.8, xf=0.25, start=0.2, mono=True, hp=150)
oneshot("AlarmBell_Stop_01.wav", "ve/65_school_bell.mp3", segment=(1.8, 3.55), hp=150,
        proc_note="the bell's last strokes and ring-out; play when the alarm is switched off")
oneshot("ShopBell_01.wav", "forkraw/cp_bell.ogg", hp=150)
oneshot("ShopBell_02.wav", "forkraw/cp_bell_short.ogg", hp=150)
oneshot("ShopDoorBell_01_HQ.wav", "ve/86_doorbell.mp3", hp=150,
        proc_note="same Freesound recording as RealFoley/ShopDoorBell_01, 160 kbps source instead of 64 kbps")

# ---------------- sirens and vehicles ----------------
oneshot("Siren_US_Wail_01.wav", E + "5-133989-A-42.wav", hp=150)
oneshot("Siren_US_Wail_02.wav", E + "5-133989-B-42.wav", hp=150)
oneshot("Siren_Police_01.wav", E + "2-70936-A-42.wav", hp=150)
oneshot("Siren_Police_02.wav", E + "2-70938-A-42.wav", hp=150)
oneshot("Siren_Police_03.wav", E + "5-150409-A-42.wav", hp=150)
oneshot("Siren_Police_04.wav", "ss14raw/Effects_Vehicle_policesiren.ogg", hp=150)
oneshot("Siren_EuroTwoTone_01.wav", "fedraw/police_siren_01.wav", hp=150)
lib.loop("EngineIdle_Car_Loop.ogg", E + "3-154758-A-44.wav", 4.0, xf=0.6, start=0.2, mono=True, hp=30)
lib.loop("EngineIdle_CarB_Loop.ogg", E + "4-186935-A-44.wav", 3.2, xf=0.5, start=0.1, mono=True, hp=30)
lib.loop("EngineIdle_Police_Loop.ogg", "fedraw/police_engine_idle_01.wav", 5.0, xf=0.6, start=0.2, mono=True, hp=30)
lib.loop("EngineIdle_Diesel_Loop.ogg", E + "4-186940-A-44.wav", 3.2, xf=0.5, start=0.1, mono=True, hp=30)
oneshot("EngineStart_01.wav", "fedraw/police_engine_start_01.wav")
for i in (1, 2, 3):
    oneshot("CarDoorClose_%02d.wav" % i, "fedraw/door_front_close_%02d.wav" % i)
    oneshot("CarDoorOpen_%02d.wav" % i, "fedraw/door_front_open_%02d.wav" % i)

# ---------------- night ambience ----------------
lib.loop("NightCrickets_Loop.ogg", "ve/12_cricket.mp3", 7.0, xf=0.8, start=0.1, mono=False, hp=80)
lib.loop("NightCrickets_Soft_Loop.ogg", E + "4-202749-A-13.wav", 4.2, xf=0.7, start=0.05, mono=True, hp=80)
lib.loop("NightCrickets_Far_Loop.ogg", E + "4-181362-A-13.wav", 4.2, xf=0.7, start=0.05, mono=True, hp=80)
lib.loop("NightInsects_Loop.ogg", E + "3-155766-A-13.wav", 4.2, xf=0.7, start=0.05, mono=True, hp=80)
lib.loop("ElectricalHum_Loop.ogg", "forkraw/dv_electrical_hum.ogg", 8.0, xf=1.0, start=0.5, mono=True, hp=40)

# ---------------- clocks ----------------
clock_loop("ClockTick_Wall_Loop.ogg", E + "3-170377-A-38.wav")
clock_loop("ClockTick_Close_Loop.ogg", E + "1-62849-A-38.wav")
clock_loop("ClockTick_Quartz_Loop.ogg", E + "2-141584-A-38.wav")
clock_loop("ClockTick_WallB_Loop.ogg", E + "5-210571-A-38.wav")
clock_loop("ClockTick_Fast_Loop.ogg", E + "3-164688-A-38.wav", periods=8)

# ---------------- hand props ----------------
oneshot("TorchClick_01.wav", "hp/flashlight_click.mp3", hp=60)
oneshot("RelayClick_01.wav", "forkraw/dv_relay_click.ogg", hp=60)
oneshot("PenClick_01.wav", "ss14raw/Items_pen_click.ogg", hp=60)
for i in range(1, 6):
    oneshot("PenWriting_%02d.wav" % i, "ss14raw/Items_Paper_paper_scribble%d.ogg" % i, hp=80)
for i in range(1, 4):
    oneshot("PageTurn_%02d.wav" % i, "kenney/bookFlip%d.ogg" % i, hp=60)
    oneshot("NotebookSetDown_%02d.wav" % i, "kenney/bookPlace%d.ogg" % i)
oneshot("NotebookOpen_01.wav", "kenney/bookOpen.ogg", hp=60)
oneshot("NotebookClose_01.wav", "kenney/bookClose.ogg", hp=60)
split_set("PaperRustle", "hp/note_rustle.mp3", 6, thresh_db=-14, gap=0.6, maxlen=1.2, hp=80)
oneshot("KitCaseOpen_01.wav", "ss14raw/Items_toolbox_open.ogg")
oneshot("KitCaseClose_01.wav", "ss14raw/Items_toolbox_close.ogg")
oneshot("PouchHandle_01.wav", "kenney/handleSmallLeather.ogg")
oneshot("PouchHandle_02.wav", "kenney/handleSmallLeather2.ogg")
oneshot("DoorKnob_01.wav", "forkraw/cp_doorknob_2.ogg")
oneshot("CashRegister_01.wav", "forkraw/cp_cash.ogg", hp=80)

# ---------------- other HQ replacement ----------------
oneshot("GlassBreak_01_HQ.wav", "ve/90_breaking-glass.mp3",
        proc_note="same Freesound recording as RealFoley/GlassBreak_01, 160 kbps source instead of the hillbomb MP3")

# CC BY items (attribution required)
lib.loop("EngineIdle_InspectorJ_Loop.ogg", "ss14raw/Effects_Vehicle_vehicleengineidle.ogg", 2.2, xf=0.4, start=0.1, mono=True, hp=30)
oneshot("EngineStart_02.wav", "ss14raw/Effects_Vehicle_vehiclestartup.ogg")

json.dump(LOG, open(os.path.join(lib.OUT, "..", "build_log.json"), "w"), indent=1)
print("files", len(LOG))
