using System;
using System.Collections.Generic;
using System.IO;
using Shortcut.RustMod;

public static partial class SkateRiggingShowcase
{
    private static SkateCaptureLibrary captureLibrary;
    private static bool captureLoadAttempted;
    private static string captureStatus = "Capture pack not loaded";
    public static string CaptureStatus { get { return captureStatus; } }

    private static void LoadCapturesOnce()
    {
        if (captureLoadAttempted) return;
        captureLoadAttempted = true;
        try
        {
            string path = Path.Combine(BepInEx.Paths.PluginPath, SkateCapturePack.FileName);
            if (!File.Exists(path)) captureStatus = "Capture pack missing: " + SkateCapturePack.FileName + "; built-in poses remain available";
            else
            {
                string error;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    if (!SkateCapturePack.TryRead(stream, out captureLibrary, out error)) captureStatus = error + "; built-in poses remain available";
                if (captureLibrary != null) captureStatus = "Capture pack loaded: " + captureLibrary.Count + " clips; review labels are source metadata";
            }
        }
        catch (Exception e) { captureLibrary = null; captureStatus = "Capture pack unavailable: " + e.GetType().Name + "; built-in poses remain available"; }
        Out.Say("RIGGING " + captureStatus);
    }
    private static string CaptureShort(string value, int limit)
    {
        if (value.Length <= limit) return value;
        int end = limit - 3;
        if (end > 0 && char.IsHighSurrogate(value[end - 1])) end--;
        return value.Substring(0, end) + "...";
    }
    private static void AppendCaptureEntries(List<PreviewEntry> entries)
    {
        LoadCapturesOnce();
        if (captureLibrary == null) return;
        for (int n = 0; n < captureLibrary.Count; n++)
        {
            var clip = captureLibrary.GetClip(n);
            string source = string.IsNullOrWhiteSpace(clip.SourceAuthor) ? "source unspecified" : clip.SourceAuthor;
            entries.Add(new PreviewEntry(PreviewKind.LibraryCapture,
                "Capture: " + CaptureShort(clip.Label, 80) + (clip.Reviewed ? " [REVIEWED]" : " [UNREVIEWED]"),
                CaptureShort(clip.MovementKind, 64) + " | " + CaptureShort(source, 64), n));
        }
    }
    public static double PlaybackSeconds(int catalogIndex)
    {
        var entries = PreviewCatalog();
        if (captureLibrary != null && catalogIndex >= 0 && catalogIndex < entries.Length && entries[catalogIndex].Kind == PreviewKind.LibraryCapture)
        {
            var clip = captureLibrary.GetClip(entries[catalogIndex].A);
            if (clip != null) return Math.Max(CycleSeconds, clip.Duration);
        }
        return CycleSeconds;
    }
    private static bool TryLibraryCapture(PreviewEntry entry, double progress, SkateRiderRig rig, SkateStance stance,
        bool sw, bool reversed, out SkateRiderPose pose, out SkateBoardPose board, out SkateVector up,
        out SkateVector left, out SkateVector right, out string error)
    {
        pose = default(SkateRiderPose); board = default(SkateBoardPose);
        up = left = right = new SkateVector(0, 1, 0); error = null;
        if (sw || reversed)
        { error = "Captured clips keep their recorded trick variant; turn off switch and reverse preview overrides."; return false; }
        var clip = captureLibrary == null ? null : captureLibrary.GetClip(entry.A);
        if (clip == null) { error = "The selected capture clip is unavailable."; return false; }
        SkateCaptureFrame frame;
        if (!SkateCaptureSampler.TrySample(clip, Math.Min(clip.Duration, progress * Math.Max(CycleSeconds, clip.Duration)),
            rig, stance, out frame, out error)) return false;
        pose = frame.Pose; board = frame.Board; up = frame.RiderUp; left = frame.LeftSoleUp; right = frame.RightSoleUp;
        return true;
    }
    private static void DescribeCaptureSource()
    {
        var entries = PreviewCatalog();
        if (captureLibrary == null || index < 0 || index >= entries.Length || entries[index].Kind != PreviewKind.LibraryCapture) return;
        var clip = captureLibrary.GetClip(entries[index].A);
        Out.Say("RIGGING capture id=" + clip.Id + " reviewed=" + clip.Reviewed + " movement=" + clip.MovementKind + " source=" + clip.SourceUrl);
    }
}
