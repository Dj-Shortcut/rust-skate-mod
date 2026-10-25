using System;
using System.Collections.Generic;

namespace Shortcut.RustMod
{
    // BEGIN SHARED SKATE PARK PRESET
    // A whole park as one set: existing park pieces on a slab of stock square foundations, laid out
    // on the 3 m support grid. Cell (0, 0) is the slab's near middle; right and forward follow the
    // placing administrator's heading snapped to a quarter turn. Each piece is anchored on a cell
    // centre and turned in quarter turns, so its support cells land exactly on slab foundations.
    public struct SkateParkPresetPiece
    {
        public SkateParkKind Kind;
        public int Right, Forward, Turns;
        public SkateParkPresetPiece(SkateParkKind kind, int right, int forward, int turns)
        { Kind = kind; Right = right; Forward = forward; Turns = turns; }
    }

    public static class SkateParkPreset
    {
        public const double CellSize = SkateParkLayout.CellSize;
        // The slab starts one cell ahead of the administrator's feet, with a 1.5 m gap before it.
        public const double NearGap = 3;

        public static readonly string[] Names = { "plaza" };

        // "plaza": a street plaza in the spirit of a classic warehouse level, built only from the
        // project's own pieces. Banks along the near edge and quarterpipes along the far edge give a
        // back-and-forth line; a funbox in the middle; long ledges along both sides for grinds.
        private static readonly SkateParkPresetPiece[] plaza =
        {
            new SkateParkPresetPiece(SkateParkKind.Bank, -3, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, -2, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, -1, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, 0, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, 1, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, 2, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.Bank, 3, 0, 2),
            new SkateParkPresetPiece(SkateParkKind.LongLedge, -3, 3, 1),
            new SkateParkPresetPiece(SkateParkKind.Funbox, 0, 3, 0),
            new SkateParkPresetPiece(SkateParkKind.LongLedge, 3, 3, 1),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, -3, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, -2, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, -1, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, 0, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, 1, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, 2, 5, 0),
            new SkateParkPresetPiece(SkateParkKind.QuarterPipe, 3, 5, 0),
        };

        public static bool TryPreset(string name, out SkateParkPresetPiece[] pieces,
            out int minRight, out int maxRight, out int minForward, out int maxForward)
        {
            pieces = null; minRight = maxRight = minForward = maxForward = 0;
            if (!string.Equals(name, "plaza", StringComparison.OrdinalIgnoreCase)) return false;
            pieces = (SkateParkPresetPiece[])plaza.Clone();
            minRight = -3; maxRight = 3; minForward = 0; maxForward = 6;
            return true;
        }

        public static string Usage()
        { return "Usage: skate.park <" + string.Join("|", Names) + ">. Stand on open, roughly flat dry ground and look along the park's run."; }

        // The slab's cell (0, 0) centre for an administrator standing at (feetX, feetZ) facing yaw.
        public static bool TryOrigin(double feetX, double feetZ, double yaw, out double x, out double z)
        {
            x = z = 0;
            if (!Finite(feetX) || !Finite(feetZ) || !Finite(yaw)) return false;
            double reach = NearGap + CellSize / 2, angle = yaw * Math.PI / 180;
            x = feetX + reach * Math.Sin(angle); z = feetZ + reach * Math.Cos(angle);
            return Finite(x) && Finite(z);
        }

        // A slab cell's world centre; the same right/forward convention as SkateParkLayout.TryTransform.
        public static bool TryCell(double originX, double originZ, double yaw, int right, int forward, out double x, out double z)
        {
            x = z = 0;
            if (!Finite(originX) || !Finite(originZ) || !Finite(yaw)) return false;
            double angle = yaw * Math.PI / 180, r = right * CellSize, f = forward * CellSize;
            x = originX + r * Math.Cos(angle) + f * Math.Sin(angle);
            z = originZ - r * Math.Sin(angle) + f * Math.Cos(angle);
            return Finite(x) && Finite(z);
        }

        public static bool TryPieceAnchor(SkateParkPresetPiece piece, double originX, double originZ, double yaw,
            out double x, out double z, out double pieceYaw)
        {
            pieceYaw = Normalize(yaw + piece.Turns * 90);
            return TryCell(originX, originZ, yaw, piece.Right, piece.Forward, out x, out z);
        }

        // Every support cell of every piece lies on the slab and no two pieces share a cell.
        public static bool Validate(string name, out string failure)
        {
            SkateParkPresetPiece[] pieces;
            int minRight, maxRight, minForward, maxForward;
            failure = "Unknown park preset " + (name ?? "(none)") + ".";
            if (!TryPreset(name, out pieces, out minRight, out maxRight, out minForward, out maxForward)) return false;
            var used = new Dictionary<long, int>();
            for (int p = 0; p < pieces.Length; p++)
            {
                SkateParkPresetPiece piece = pieces[p];
                double ax, az, pieceYaw;
                int cells = SkateParkLayout.SupportCellCount(piece.Kind);
                if (cells == 0 || piece.Kind == SkateParkKind.MegaHalfpipe || piece.Turns < 0 || piece.Turns > 3 ||
                    !TryPieceAnchor(piece, 0, 0, 0, out ax, out az, out pieceYaw))
                { failure = "Preset piece " + p + " has no supported grid layout."; return false; }
                for (int i = 0; i < cells; i++)
                {
                    SkateParkTransform cell;
                    if (!SkateParkLayout.TrySupportCellPose(piece.Kind, i, ax, az, pieceYaw, out cell))
                    { failure = "Preset piece " + p + " cell " + i + " is outside world bounds."; return false; }
                    double fr = cell.X / CellSize, ff = cell.Z / CellSize;
                    int r = (int)Math.Round(fr), f = (int)Math.Round(ff);
                    if (Math.Abs(fr - r) > 1e-6 || Math.Abs(ff - f) > 1e-6)
                    { failure = "Preset piece " + p + " cell " + i + " is off the 3 m grid."; return false; }
                    if (r < minRight || r > maxRight || f < minForward || f > maxForward)
                    { failure = "Preset piece " + p + " cell (" + r + "," + f + ") is off the slab."; return false; }
                    long key = ((long)r << 32) ^ (uint)f;
                    int other;
                    if (used.TryGetValue(key, out other))
                    { failure = "Preset pieces " + other + " and " + p + " share cell (" + r + "," + f + ")."; return false; }
                    used.Add(key, p);
                }
            }
            failure = null;
            return true;
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) <= 100000; }
        private static double Normalize(double yaw) { yaw %= 360; return yaw < 0 ? yaw + 360 : yaw; }
    }
    // END SHARED SKATE PARK PRESET
}
