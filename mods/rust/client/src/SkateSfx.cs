using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.IO;
using BepInEx;
using Shortcut.RustMod;
using UnityEngine;

public static class SkateSfx
{
    public const int Rate = 44100;
    public static bool Ready;
    public static string State = "not started";
    private static GameObject audio;
    private static GameObject boardEmitter;
    private const int ShotVoices = 8;
    private static readonly AudioSource[] shotPool = new AudioSource[ShotVoices];
    private static readonly float[] shotTimes = new float[ShotVoices];
    private static int shotStep;
    private const int Smooth = 0, Rough = 1, Wood = 2, Grit = 3, Whirr = 4, Push = 5,
        Ollie = 6, Landing = 7, Drop = 8, Seamed = 9, GrindIn = 10, Grind = 11,
        GrindOut = 12, SlideIn = 13, DeckSlide = 14, Scrape = 15, Slam = 16, Bail = 17;
    public const float HeavyLandingImpact = 7.5f;
    private static readonly AudioClip[] ownedClips = new AudioClip[SkateSoundPack.Count * SkateSoundPack.Variations];
    private static int ownedCount;
    private static AudioSource roll, rough, grind, slide, shots, hollow, grit, whirr;
    private static int pops, lands, bails, pushes, grindStarts, grindEnds, slams, reverts, stepUps;
    private static bool wasOn, wasAir, wasSliding;
    private static bool rollRun, roughRun, woodRun, gritRun, whirrRun, grindRun, slideRun;
    private static float rollVolume, roughVolume, grindVolume, slideVolume, hollowVolume, gritVolume, whirrVolume, whirrPitch = 1f;
    // What the wheels are on, looked at a few times a second: a wooden park piece booms, dirt and grass
    // crunch, a foundation clacks at its seams every Seam metres.
    public enum Ground { Hard, Wood, Dirt, Build }
    public static Ground Surface = Ground.Hard;
    public const float SurfaceEvery = 0.1f, Seam = 3f;
    private static float surfaceAt, seamRun;
    private static int terrainLayer = -2, buildLayer = -2;
    public static float SlideVolumeForTest { get { return slideVolume; } }
    private static bool failed, suspended, deaf, ranked = true;
    public static bool Failed { get { return failed; } }
    private static Il2CppStructArray<float> block;
    public static float RollHeard, GrindHeard, ShotsHeard;

    private static SkateSoundPack.Reader reader;
    private static string directory;
    private static int preparation, slot, phase, uploaded, uploadCount, sourceStep;
    private static float[] samples, upload;

    private static AudioSource Source(GameObject go, AudioClip clip, bool loop)
    {
        var s = go.AddComponent<AudioSource>();
        s.spatialBlend = 1f; s.dopplerLevel = 0f;
        s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1f; s.maxDistance = 25f;
        if (s.spatialBlend != 1f || s.dopplerLevel != 0f || s.rolloffMode != AudioRolloffMode.Linear ||
            s.minDistance != 1f || s.maxDistance != 25f)
            throw new InvalidOperationException("Native skateboard audio source rejected its 3D configuration");
        s.loop = loop; s.volume = loop ? 0f : 1f;
        if (ranked)
        {
            // When more sources play than the engine mixes, it drops the least important ones first.
            try { s.priority = 16; }
            catch (Exception e) { ranked = false; Out.Say("AUDIO source priority unavailable: " + e.GetType().Name + ": " + e.Message); }
        }
        if (clip != null) { s.clip = clip; if (!suspended) s.Play(); }
        return s;
    }

    // Open, native create, bounded decode, and native upload are separate preparation units.
    // Native operations remain uninterruptible; the existing caller measures/quiet-waits each unit.
    // Existing explicit audio checks may still ask for synchronous Start; boarding never does.
    public static void Start() { while (!PrepareStep()) { } }
    public static bool PrepareStep()
    {
        if (Ready || failed) return true;
        try
        {
            if (preparation == 0)
            {
                directory = Path.Combine(Paths.PluginPath, "skate_sounds");
                SkateSoundPack.ResetVariants(1u);
                samples = new float[32768];
                audio = new GameObject("skate_audio");
                UnityEngine.Object.DontDestroyOnLoad(audio);
                State = "preparing WAV sound pack";
                preparation = 1;
            }
            else if (slot < ownedClips.Length) PrepareClip();
            else if (shotStep < ShotVoices)
            {
                var voice = new GameObject("skate_audio_shot_" + shotStep);
                voice.transform.SetParent(audio.transform, false);
                shotPool[shotStep] = Source(voice, null, false);
                shotTimes[shotStep++] = -1f;
            }
            else if (sourceStep < 7)
            {
                if (sourceStep == 0)
                {
                    boardEmitter = new GameObject("skate_audio_board");
                    boardEmitter.transform.SetParent(audio.transform, false);
                    roll = Source(boardEmitter, ownedClips[Smooth * SkateSoundPack.Variations], true);
                }
                else if (sourceStep == 1) rough = Source(boardEmitter, ownedClips[Rough * SkateSoundPack.Variations], true);
                else if (sourceStep == 2) hollow = Source(boardEmitter, ownedClips[Wood * SkateSoundPack.Variations], true);
                else if (sourceStep == 3) grit = Source(boardEmitter, ownedClips[Grit * SkateSoundPack.Variations], true);
                else if (sourceStep == 4) whirr = Source(boardEmitter, ownedClips[Whirr * SkateSoundPack.Variations], true);
                else if (sourceStep == 5) grind = Source(boardEmitter, ownedClips[Grind * SkateSoundPack.Variations], true);
                else slide = Source(boardEmitter, ownedClips[DeckSlide * SkateSoundPack.Variations], true);
                sourceStep++;
            }
            else
            {
                samples = upload = null; directory = null;
                Sync(); wasOn = false;
                Ready = true; State = "ready";
                Out.Say("AUDIO ready: " + ownedClips.Length + " selected WAVs at " + Rate +
                    " Hz; seven 3D board loops, eleven one-shot types, eight fixed-position voices, linear range 1-25 m");
            }
        }
        catch (Exception e) { Fail("failed", e); }
        return Ready || failed;
    }

    private static void PrepareClip()
    {
        var stem = slot / SkateSoundPack.Variations;
        var variant = slot % SkateSoundPack.Variations + 1;
        if (phase == 0)
        {
            string error;
            if (!SkateSoundPack.TryOpen(directory, stem, variant, out reader, out error))
                throw new InvalidDataException(error);
            uploaded = 0; phase = 1;
        }
        else if (phase == 1)
        {
            var clip = Out.Keep(AudioClip.Create(SkateSoundPack.Stem(stem) + "_0" + variant,
                reader.SampleCount, 1, Rate, false));
            // Register before any upload so cancellation/failure owns every partial clip.
            ownedClips[slot] = clip; ownedCount = slot + 1;
            if (clip == null) throw new InvalidOperationException("WAV clip creation failed");
            phase = 2;
        }
        else if (phase == 2)
        {
            string error;
            if (!reader.Read(samples, out uploadCount, out error)) throw new InvalidDataException(error);
            if (uploadCount == 0)
            {
                if (uploaded != reader.SampleCount) throw new InvalidDataException("Incomplete WAV upload");
                reader.Dispose(); reader = null; slot++; phase = 0;
            }
            else
            {
                if (uploadCount == samples.Length) upload = samples;
                else { upload = new float[uploadCount]; Array.Copy(samples, upload, uploadCount); }
                phase = 3;
            }
        }
        else
        {
            if (uploaded > reader.SampleCount - uploadCount || !ownedClips[slot].SetData(upload, uploaded))
                throw new InvalidOperationException("WAV clip upload failed: " + SkateSoundPack.Stem(stem));
            uploaded += uploadCount; upload = null; phase = 2;
        }
    }

    public static void Shutdown()
    {
        failed = !Cleanup();
        suspended = false; deaf = false; ranked = true;
        State = failed ? "cleanup incomplete" : "stopped";
        if (failed) Out.Err("AUDIO " + State);
    }

    private static void Fail(string phase, Exception e)
    {
        failed = true;
        State = "WAV sound pack unavailable (" + phase + "): " + e.GetType().Name + ": " + e.Message;
        if (!Cleanup()) State += "; cleanup incomplete";
        Out.Say("AUDIO " + State);
    }

    private static bool Cleanup()
    {
        Ready = false; preparation = slot = phase = uploaded = uploadCount = sourceStep = shotStep = 0;
        var clean = true;
        try { if (reader != null) reader.Dispose(); reader = null; }
        catch (Exception) { clean = false; }
        samples = upload = null; directory = null;
        SilenceAll();
        if (DestroyOwned(audio))
        {
            audio = boardEmitter = null; roll = rough = grind = slide = shots = hollow = grit = whirr = null;
            for (var i = 0; i < ShotVoices; i++) { shotPool[i] = null; shotTimes[i] = -1f; }
        }
        else clean = false;
        for (var i = 0; i < ownedCount; i++)
        {
            if (DestroyOwned(ownedClips[i])) ownedClips[i] = null;
            else clean = false;
        }
        if (clean) ownedCount = 0;
        block = null;
        ResetPlayback();
        Surface = said = Ground.Hard; surfaceAt = -99f; seamRun = 0f;
        RollHeard = GrindHeard = ShotsHeard = 0f;
        pops = lands = bails = pushes = 0;
        SkateSoundPack.ResetVariants(1u);
        return clean;
    }

    private static void ResetPlayback()
    {
        wasOn = wasAir = wasSliding = false;
        rollRun = roughRun = woodRun = gritRun = whirrRun = grindRun = slideRun = false;
        rollVolume = roughVolume = grindVolume = slideVolume = hollowVolume = gritVolume = whirrVolume = 0f;
        whirrPitch = 1f;
    }

    private static void SilenceAll()
    {
        Silence(roll); Silence(rough); Silence(grind); Silence(slide); Silence(hollow); Silence(grit); Silence(whirr);
        for (var i = 0; i < ShotVoices; i++) Silence(shotPool[i]);
    }

    public static void Suspend()
    {
        suspended = true; SilenceAll(); ResetPlayback();
        if (Ready) Sync();
        else if (!failed)
        {
            // A disabled rig must not retain an unfinished file reader indefinitely.
            // A complete cache survives suspension; partial preparation restarts off-board.
            if (Cleanup()) State = "WAV sound pack preparation paused";
            else
            {
                failed = true; State = "WAV sound pack unavailable (suspend): cleanup incomplete";
                Out.Err("AUDIO " + State);
            }
        }
    }

    public static void Resume()
    {
        if (!suspended) return;
        suspended = false; ResetPlayback();
        if (!Ready || failed) return;
        try
        {
            Sync();
            roll.Play(); rough.Play(); hollow.Play(); grit.Play(); whirr.Play(); grind.Play(); slide.Play();
        }
        catch (Exception e) { Fail("failed while resuming", e); }
    }

    private static bool DestroyOwned(UnityEngine.Object value)
    {
        try { if (value != null) UnityEngine.Object.Destroy(value); return true; }
        catch (Exception) { return false; }
    }

    private static void Silence(AudioSource source)
    {
        try { if (source != null) source.volume = 0f; }
        catch (Exception) { }
        try { if (source != null) source.Stop(); }
        catch (Exception) { }
    }

    private static void Sync()
    {
        pops = SkateRide.Pops; lands = SkateRide.Lands; bails = SkateRide.Bails; pushes = SkateRide.Pushes;
        grindStarts = SkateRide.GrindStarts; grindEnds = SkateRide.GrindEnds; slams = SkateRide.WallSlams; reverts = SkateRide.Reverts; stepUps = SkateRide.StepUps;
    }

    private static AudioClip Variant(int stem)
    {
        var clip = ownedClips[stem * SkateSoundPack.Variations + SkateSoundPack.NextVariant(stem) - 1];
        if (clip == null) throw new InvalidOperationException("Missing cached WAV: " + SkateSoundPack.Stem(stem));
        return clip;
    }

    private static void PlaceBoardEmitter()
    {
        // Audio reads the existing board state; it must not initialize the
        // renderer's contact cache or loose-board/bail trajectory by querying a pose.
        var position = SkateRide.Position + SkateBoard.TruckPlaneOffset +
            SkateRide.ViewNormal * (RiderRig.DeckTop + SkateBoard.ShowLift + SkateBoard.Pop);
        if (SkateBoard.RollingOut) position = SkateBoard.RollOutAt;
        else if (SkateRide.On && SkateCapturedTricks.HasFrame)
        {
            var p = SkateCapturedTricks.DisplayFrame.Origin;
            position = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
            throw new InvalidOperationException("Invalid skateboard audio position");
        boardEmitter.transform.position = position;
    }

    private static void Play(int stem, float volume)
    {
        var index = 0;
        for (var i = 0; i < ShotVoices; i++)
        {
            if (!shotPool[i].isPlaying) { index = i; break; }
            if (shotTimes[i] < shotTimes[index]) index = i;
        }
        // Loops follow the board; a completed impact stays where it happened.
        // Reuse only a free voice, or the oldest tail when the bounded pool is full.
        shots = shotPool[index]; shots.Stop();
        shots.transform.position = stem == Slam ? SkateRide.Position + Vector3.up * .45f :
            stem == Push ? SkateRide.Position + Vector3.up * .03f : boardEmitter.transform.position;
        shotTimes[index] = Time.realtimeSinceStartup;
        shots.PlayOneShot(Variant(stem), volume);
    }

    private static void ContactLoop(AudioSource source, int stem, bool active, ref bool running)
    {
        if (active && !running && source.volume <= 0.001f)
        { source.clip = Variant(stem); source.Play(); }
        // Briefly interrupted contacts retain their playing clip through its audible fade.
        running = active;
    }

    // The nearest of the terrain, a building block or a park piece (the default layer) under the
    // board; anything else, rock and monument concrete, is hard ground.
    private static void Feel(float now)
    {
        if (now - surfaceAt < SurfaceEvery) return;
        surfaceAt = now;
        if (terrainLayer == -2) { try { terrainLayer = LayerMask.NameToLayer("Terrain"); buildLayer = LayerMask.NameToLayer("Construction"); } catch (Exception) { terrainLayer = buildLayer = -1; } }
        var from = SkateRide.Position + Vector3.up * 0.5f;
        RaycastHit hit;
        if (!SkateParks.Ray(from, Vector3.down, out hit, 1f, SkateRide.GroundMask)) return;
        var d = hit.m_Distance + 0.02f;
        SkateParkMaterial material;
        if (SkateParks.TrySoundMaterial(hit.collider, hit.point, out material)) Surface = material == SkateParkMaterial.Wood ? Ground.Wood : Ground.Hard;
        else if (buildLayer >= 0 && SkateParks.Ray(from, Vector3.down, out hit, d, 1 << buildLayer)) Surface = Ground.Build;
        else if (terrainLayer >= 0 && SkateParks.Ray(from, Vector3.down, out hit, d, 1 << terrainLayer)) Surface = Ground.Dirt;
        else Surface = Ground.Hard;
        if (Surface != said) { said = Surface; if (surfaceSays++ < 40) Out.Say("AUDIO wheels on " + Surface); }
    }
    private static Ground said = Ground.Hard; private static int surfaceSays;

    // The loudest sample each source has put out so far, as the engine reports it: what a scripted
    // check can know about sound without ears.
    public static void Listen()
    {
        if (!Ready || failed || deaf) return;
        try
        {
            if (block == null) block = new Il2CppStructArray<float>(256);
            var wheels = Math.Max(Math.Max(Peak(roll), Peak(rough)), Math.Max(Peak(hollow), Math.Max(Peak(grit), Peak(whirr))));
            RollHeard = Math.Max(RollHeard, wheels); GrindHeard = Math.Max(GrindHeard, Math.Max(Peak(grind), Peak(slide)));
            for (var i = 0; i < ShotVoices; i++) ShotsHeard = Math.Max(ShotsHeard, Peak(shotPool[i]));
        }
        catch (Exception e) { deaf = true; Out.Say("AUDIO output cannot be read back: " + e.GetType().Name + ": " + e.Message); }
    }

    private static float Peak(AudioSource source)
    {
        source.GetOutputData(block, 0);
        var peak = 0f;
        for (var i = 0; i < block.Length; i++) { var v = Math.Abs(block[i]); if (v > peak) peak = v; }
        return peak;
    }

    // The engine reports the output after the listener's volume; the figures are scaled back to full volume.
    public static string Heard
    {
        get
        {
            if (deaf) return "unreadable";
            var listener = AudioListener.volume;
            if (listener < 0.0001f) return "nothing, the listener is at zero";
            return "roll " + (RollHeard / listener).ToString("F3") + " grind " + (GrindHeard / listener).ToString("F3") + " one-shots " + (ShotsHeard / listener).ToString("F3") + " of full scale (listener at " + listener.ToString("F3") + ")";
        }
    }

    public static void Update(float frameSeconds)
    {
        if (!Ready || failed || suspended) return;
        try
        {
            PlaceBoardEmitter();
            var speed = Math.Abs(SkateRide.Speed);
            if (float.IsNaN(speed) || float.IsInfinity(speed)) speed = 0f;
            if (float.IsNaN(frameSeconds) || float.IsInfinity(frameSeconds) || frameSeconds < 0f) frameSeconds = 0f;
            var ratio = SkateRide.Clamp(speed / SkateRide.MaxSpeed, 0f, 1f);
            var on = SkateRide.On;
            var onEdge = on && SkateRide.Mode == RideMode.Grind && speed > 0.1f;
            var deck = SkateRide.GrindKind == SkateGrindKind.Boardslide || SkateRide.GrindKind == SkateGrindKind.Lipslide ||
                SkateRide.GrindKind == SkateGrindKind.Noseslide || SkateRide.GrindKind == SkateGrindKind.Tailslide ||
                SkateRide.GrindKind == SkateGrindKind.Bluntslide || SkateRide.GrindKind == SkateGrindKind.Nosebluntslide || SkateRide.GrindKind == SkateGrindKind.Darkslide;
            var rolling = on && (SkateRide.Mode == RideMode.Ground || SkateSurfaceRide.Active);
            // A landing is heard on what it lands on, not on what the board left.
            if (SkateRide.Lands != lands) surfaceAt = -99f;
            if (rolling && !SkateSurfaceRide.Active) Feel(Time.time);
            var rollShare = rolling ? SkateRide.Clamp(speed / 9f, 0f, 1f) : 0f;
            // These WAVs are complete wheel beds. Use one substrate bed, not smooth+wood.
            // Dirt intentionally uses rough rolling+grit; no native asphalt identity is inferred.
            var wantRoll = Surface == Ground.Hard || Surface == Ground.Build ? rollShare * 0.55f : 0f;
            var wantRough = Surface == Ground.Dirt ? rollShare * 0.55f : 0f;
            var wantHollow = Surface == Ground.Wood ? rollShare * 0.55f : 0f;
            var wantGrit = Surface == Ground.Dirt ? rollShare * 0.45f : 0f;
            // Off the ground the wheels spin on at the speed they rolled and wind down; back on, they
            // are the rolling sound again.
            var airborne = on && SkateRide.Mode == RideMode.Air && !SkateSurfaceRide.Active;
            if (airborne && !wasAir) { whirrVolume = 0.08f + ratio * 0.22f; whirrPitch = 0.75f + ratio * 0.5f; }
            wasAir = airborne;
            var edgeVolume = 0.3f + ratio * 0.3f;
            var wantGrind = onEdge && !deck ? edgeVolume : 0f;
            // A powerslide on the ground scrubs with the deck's own slide sound, louder the faster.
            var sliding = on && SkateRide.Mode == RideMode.Ground && SkateRide.Powerslide > 0.3f && speed > 0.8f;
            var beganSlide = sliding && !wasSliding; wasSliding = sliding;
            var wantSlide = onEdge && deck ? edgeVolume : sliding ? (0.25f + ratio * 0.35f) * SkateRide.Powerslide : 0f;
            ContactLoop(roll, Smooth, wantRoll > 0.001f, ref rollRun);
            ContactLoop(rough, Rough, wantRough > 0.001f, ref roughRun);
            ContactLoop(hollow, Wood, wantHollow > 0.001f, ref woodRun);
            ContactLoop(grit, Grit, wantGrit > 0.001f, ref gritRun);
            ContactLoop(whirr, Whirr, airborne && whirrVolume > 0.001f, ref whirrRun);
            ContactLoop(grind, Grind, wantGrind > 0.001f, ref grindRun);
            ContactLoop(slide, DeckSlide, wantSlide > 0.001f, ref slideRun);
            var k = 1f - (float)Math.Exp(-12f * SkateRide.Clamp(frameSeconds, 0f, 0.1f));
            rollVolume += (wantRoll - rollVolume) * k;
            roughVolume += (wantRough - roughVolume) * k;
            grindVolume += (wantGrind - grindVolume) * k; slideVolume += (wantSlide - slideVolume) * k;
            hollowVolume += (wantHollow - hollowVolume) * k; gritVolume += (wantGrit - gritVolume) * k;
            if (airborne) { whirrVolume *= (float)Math.Exp(-SkateRide.Clamp(frameSeconds, 0f, 0.1f) / 1.4f); whirrPitch = Math.Max(0.6f, whirrPitch - frameSeconds * 0.15f); }
            else whirrVolume += (0f - whirrVolume) * k;
            var pitch = 0.75f + ratio * 0.55f;
            roll.volume = rollVolume; roll.pitch = pitch;
            rough.volume = roughVolume; rough.pitch = pitch * 0.85f;
            hollow.volume = hollowVolume; hollow.pitch = 0.85f + ratio * 0.3f;
            grit.volume = gritVolume; grit.pitch = 0.8f + ratio * 0.5f;
            whirr.volume = whirrVolume; whirr.pitch = whirrPitch;
            grind.volume = grindVolume; slide.volume = slideVolume;
            grind.pitch = slide.pitch = 0.9f + ratio * 0.35f;
            if (!on)
            {
                wasOn = false; Sync(); return;
            }
            if (!wasOn) { wasOn = true; seamRun = 0f; }
            var bailed = SkateRide.Bails != bails;
            // The two authored stems are distinct body impact and escaping-board events.
            // WallSlams and Bails may change together; this branch emits each only once.
            if (bailed) { Play(Slam, 0.6f); Play(Bail, 0.45f); }
            else
            {
                var popped = SkateRide.Pops != pops;
                if (SkateRide.Pushes != pushes) Play(Push, 0.8f);
                if (popped) Play(Ollie, 0.8f);
                if (SkateRide.Lands != lands)
                {
                    var hard = SkateRide.Clamp(SkateRide.LandImpact / 15f, 0f, 1f);
                    Play(SkateRide.LandImpact >= HeavyLandingImpact ? Drop : Landing,
                        SkateRide.Clamp(0.3f + hard, 0.3f, 0.85f));
                }
                if (SkateRide.GrindStarts != grindStarts && SkateRide.GrindKind != SkateGrindKind.Handplant)
                    Play(deck ? SlideIn : GrindIn, 0.45f + ratio * 0.4f);
                if (SkateRide.GrindEnds != grindEnds && !popped) Play(GrindOut, 0.5f);
                if (SkateRide.WallSlams != slams) Play(Slam, 0.6f);
                if (SkateRide.Reverts != reverts || beganSlide) Play(Scrape, 0.3f + ratio * 0.3f);
                var seamed = SkateRide.StepUps != stepUps;
                if (seamed) Play(Seamed, 0.6f);
                if (rolling && Surface == Ground.Build && speed > 1f)
                {
                    seamRun += speed * frameSeconds;
                    if (seamRun >= Seam) { seamRun %= Seam; if (!seamed) Play(Seamed, 0.2f + ratio * 0.35f); }
                }
            }
            Sync();
        }
        catch (Exception e) { Fail("failed while playing", e); }
    }
}
