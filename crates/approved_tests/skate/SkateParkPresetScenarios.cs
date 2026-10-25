using System;
using System.Collections.Generic;
using Shortcut.RustMod;

// The park preset (skate.park): every piece of every preset sits on its own slab cells, and anchors
// follow the same right/forward convention as SkateParkLayout.TryTransform.
internal static class SkateParkPresetScenarios
{
    private static int checks;

    private static void Check(bool condition, string what)
    {
        if (!condition) throw new Exception("FAIL: " + what);
        checks++;
    }

    private static int Main()
    {
        foreach (string name in SkateParkPreset.Names)
        {
            string failure;
            Check(SkateParkPreset.Validate(name, out failure), name + " validates: " + failure);
            SkateParkPresetPiece[] pieces;
            int minRight, maxRight, minForward, maxForward;
            Check(SkateParkPreset.TryPreset(name.ToUpperInvariant(), out pieces, out minRight, out maxRight, out minForward, out maxForward),
                name + " is found case-insensitively");
            Check(pieces.Length > 0 && minRight <= maxRight && minForward <= maxForward, name + " has pieces and a slab");
            foreach (SkateParkPresetPiece piece in pieces)
                Check(piece.Kind != SkateParkKind.MegaHalfpipe && piece.Kind != SkateParkKind.HalfWallKicker, name + " uses grid pieces only");
        }
        string unknown;
        Check(!SkateParkPreset.Validate("missing", out unknown) && unknown != null, "unknown preset is refused");

        double ox, oz;
        Check(SkateParkPreset.TryOrigin(10, 20, 0, out ox, out oz) && Math.Abs(ox - 10) < 1e-9 && Math.Abs(oz - 24.5) < 1e-9,
            "origin lies 4.5 m ahead at yaw 0");
        Check(SkateParkPreset.TryOrigin(10, 20, 90, out ox, out oz) && Math.Abs(ox - 14.5) < 1e-9 && Math.Abs(oz - 20) < 1e-9,
            "origin lies 4.5 m ahead at yaw 90");
        for (int turn = 0; turn < 4; turn++)
        {
            double yaw = turn * 90, x, z;
            SkateParkTransform pose;
            Check(SkateParkPreset.TryCell(0, 0, yaw, 2, 3, out x, out z), "cell resolves");
            Check(SkateParkLayout.TryTransform(SkateParkKind.LedgeLow, 0, 0, 0, 0, yaw, out pose), "transform resolves");
            double angle = yaw * Math.PI / 180;
            Check(Math.Abs(x - (6 * Math.Cos(angle) + 9 * Math.Sin(angle))) < 1e-9 &&
                Math.Abs(z - (-6 * Math.Sin(angle) + 9 * Math.Cos(angle))) < 1e-9, "cell follows the layout convention at yaw " + yaw);
        }
        Console.WriteLine("SkateParkPresetScenarios: " + checks + " checks passed");
        return 0;
    }
}
