// Player-facing text: English only.
// The controller's flick tricks as cards, the way a skate game shows them: the trick's name and the
// right-stick gesture drawn as an arrow, no sentence to read. The HUD shows them while Back is held.
// Each card's picture is drawn here into pixels once; the HUD makes a texture of them.
// Stick coordinates: x to the right, y forward (towards the nose), the rim at 1. Sides are those of
// a regular rider going nose first; goofy or fakie mirror them.
// The gestures are the ones the client reads today (SkateKeys, SkateFlick). When a gesture changes
// there, its card changes here.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkateFlickCards
{
    public struct Card
    {
        public string Name, Buttons;
        // Each stroke a run of x, y pairs; the last one ends in an arrowhead.
        public float[][] Strokes;
        // Points the stick is held at, as x, y pairs.
        public float[] Holds;
    }

    public struct Section { public string Title; public Card[] Cards; }

    private const float Rim = 0.82f, Turn = 0.7f;

    public static readonly Section[] Sections = {
        new Section { Title = "POP", Cards = new[] {
            Flick("Ollie", Path().Rim(270).Rim(90)),
            Flick("Nollie", Path().Rim(90).Rim(270)),
            Flick("Kickflip", Path().Rim(270).To(0f, 0f).Rim(50)),
            Flick("Heelflip", Path().Rim(270).To(0f, 0f).Rim(130)),
            Flick("Varial Kickflip", Path().Rim(225).Rim(45)),
            Flick("Varial Heelflip", Path().Rim(315).Rim(135)),
            Flick("Hardflip", Path().Arc(270, 320, Rim, Rim).To(Cos(320) * Rim, 0.75f)),
            Flick("Inward Heelflip", Path().Arc(270, 220, Rim, Rim).To(Cos(220) * Rim, 0.75f)),
            Flick("360 Flip", Path().Arc(180, 270, Rim, Rim).Rim(45)),
            Flick("Laser Flip", Path().Arc(360, 270, Rim, Rim).Rim(135)),
            Flick("360 Hardflip", Path().Arc(360, 270, Rim, Rim).Rim(45)),
            Flick("360 Inward Heelflip", Path().Arc(180, 270, Rim, Rim).Rim(135)),
        } },
        new Section { Title = "SHOVE-ITS AND IMPOSSIBLES", Cards = new[] {
            Flick("Pop Shove-it", Path().Arc(270, 360, Turn, Turn)),
            Flick("FS Pop Shove-it", Path().Arc(270, 180, Turn, Turn)),
            Flick("360 Shove-it", Path().Arc(270, 450, Turn, Turn)),
            Flick("Impossible", Path().Rim(270).Rim(90), "LB"),
            Flick("Front Foot Impossible", Path().Rim(270).Rim(90), "RB"),
            Flick("Underflip", Path().Rim(270).Rim(90), "LB + RB"),
        } },
        new Section { Title = "MANUALS AND LATE FLIPS", Cards = new[] {
            Held("Manual", 0f, -0.45f),
            Held("Nose Manual", 0f, 0.45f),
            Flick("No Comply", Path().Rim(270).Rim(90), "A"),
            Flick("Late Kickflip", Path().To(0f, 0f).Rim(0)),
            Flick("Late Heelflip", Path().To(0f, 0f).Rim(180)),
        } },
    };

    private sealed class Stroke
    {
        private readonly List<float> points = new List<float>();
        public Stroke To(float x, float y) { points.Add(x); points.Add(y); return this; }
        public Stroke Rim(float degrees) { return To(Cos(degrees) * SkateFlickCards.Rim, Sin(degrees) * SkateFlickCards.Rim); }
        // Round from one angle to another; the radius can close in on the way, so a full turn ends
        // inside where it started.
        public Stroke Arc(float from, float to, float r0, float r1)
        {
            var n = Math.Max(6, (int)Math.Ceiling(Math.Abs(to - from) / 8f));
            for (var i = 0; i <= n; i++)
            {
                float a = from + (to - from) * i / n, r = r0 + (r1 - r0) * i / n;
                To(Cos(a) * r, Sin(a) * r);
            }
            return this;
        }
        public float[] Points() { return points.ToArray(); }
    }

    private static Stroke Path() { return new Stroke(); }
    private static float Cos(float degrees) { return (float)Math.Cos(degrees * Math.PI / 180.0); }
    private static float Sin(float degrees) { return (float)Math.Sin(degrees * Math.PI / 180.0); }
    private static Card Flick(string name, Stroke s, string buttons = null) { return new Card { Name = name, Buttons = buttons, Strokes = new[] { s.Points() }, Holds = new float[0] }; }
    private static Card Held(string name, float x, float y) { return new Card { Name = name, Strokes = new float[0][], Holds = new[] { x, y } }; }

    // --- the picture: a stick seen from above, the gesture on it in amber
    private static readonly Color32 Base = new Color32(20, 21, 19, 215), Ring = new Color32(96, 97, 88, 255),
        Amber = new Color32(246, 178, 58, 255), Hold = new Color32(219, 77, 46, 255), Halo = new Color32(219, 77, 46, 70);

    // size by size pixels, bottom row first, as a texture takes them.
    public static Color32[] Pixels(Card card, int size)
    {
        var pixels = new Color32[size * size];
        float half = size * 0.5f, scale = size * 0.42f, rim = size * 0.46f, line = size * 0.055f;
        // The strokes in pixels, and the arrowhead at the end of the last one.
        var strokes = new float[card.Strokes.Length][];
        for (var s = 0; s < strokes.Length; s++)
        {
            var src = card.Strokes[s]; var dst = new float[src.Length];
            for (var i = 0; i < src.Length; i += 2) { dst[i] = half + src[i] * scale; dst[i + 1] = half + src[i + 1] * scale; }
            strokes[s] = dst;
        }
        float tipX = 0f, tipY = 0f, leftX = 0f, leftY = 0f, rightX = 0f, rightY = 0f; var head = false;
        if (strokes.Length > 0 && strokes[strokes.Length - 1].Length >= 4)
        {
            var p = strokes[strokes.Length - 1]; var n = p.Length;
            float dx = p[n - 2] - p[n - 4], dy = p[n - 1] - p[n - 3], d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 0.001f)
            {
                dx /= d; dy /= d;
                float length = size * 0.13f, width = size * 0.085f;
                tipX = p[n - 2] + dx * size * 0.035f; tipY = p[n - 1] + dy * size * 0.035f;
                leftX = tipX - dx * length - dy * width; leftY = tipY - dy * length + dx * width;
                rightX = tipX - dx * length + dy * width; rightY = tipY - dy * length - dx * width;
                head = true;
            }
        }
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f, r = Dist(px, py, half, half);
                float cr = 0f, cg = 0f, cb = 0f, ca = 0f;
                Over(ref cr, ref cg, ref cb, ref ca, Base, Cover(rim - r));
                Over(ref cr, ref cg, ref cb, ref ca, Ring, Cover(size * 0.012f - Math.Abs(r - rim + size * 0.012f)));
                Over(ref cr, ref cg, ref cb, ref ca, Ring, Cover(size * 0.008f - Math.Abs(r - size * 0.1f)));
                for (var h = 0; h + 1 < card.Holds.Length; h += 2)
                {
                    var hr = Dist(px, py, half + card.Holds[h] * scale, half + card.Holds[h + 1] * scale);
                    Over(ref cr, ref cg, ref cb, ref ca, Halo, Cover(size * 0.11f - hr));
                    Over(ref cr, ref cg, ref cb, ref ca, Hold, Cover(size * 0.065f - hr));
                }
                var near = float.MaxValue;
                foreach (var p in strokes)
                    for (var i = 0; i + 3 < p.Length; i += 2) near = Math.Min(near, Segment(px, py, p[i], p[i + 1], p[i + 2], p[i + 3]));
                Over(ref cr, ref cg, ref cb, ref ca, Amber, Cover(line * 0.5f - near));
                if (head) Over(ref cr, ref cg, ref cb, ref ca, Amber, Cover(Inside(px, py, tipX, tipY, leftX, leftY, rightX, rightY)));
                // The start of each stroke: a ring, so it reads as where the thumb begins.
                foreach (var p in strokes)
                {
                    if (p.Length < 2) continue;
                    var sr = Dist(px, py, p[0], p[1]);
                    Over(ref cr, ref cg, ref cb, ref ca, Amber, Cover(size * 0.06f - sr));
                    Over(ref cr, ref cg, ref cb, ref ca, Base, Cover(size * 0.032f - sr));
                }
                pixels[y * size + x] = ca <= 0f ? new Color32(0, 0, 0, 0) :
                    new Color32((byte)(cr / ca * 255f), (byte)(cg / ca * 255f), (byte)(cb / ca * 255f), (byte)(ca * 255f));
            }
        return pixels;
    }

    // How much of a pixel an edge covers, from the signed distance inside it.
    private static float Cover(float inside) { return inside <= -0.5f ? 0f : inside >= 0.5f ? 1f : inside + 0.5f; }

    // A colour laid over what is there, kept premultiplied until the end.
    private static void Over(ref float r, ref float g, ref float b, ref float a, Color32 c, float cover)
    {
        if (cover <= 0f) return;
        var alpha = c.a / 255f * cover;
        r = c.r / 255f * alpha + r * (1f - alpha); g = c.g / 255f * alpha + g * (1f - alpha); b = c.b / 255f * alpha + b * (1f - alpha);
        a = alpha + a * (1f - alpha);
    }

    private static float Dist(float x, float y, float cx, float cy) { float dx = x - cx, dy = y - cy; return (float)Math.Sqrt(dx * dx + dy * dy); }

    private static float Segment(float x, float y, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay, l = dx * dx + dy * dy;
        var t = l < 1e-6f ? 0f : ((x - ax) * dx + (y - ay) * dy) / l;
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return Dist(x, y, ax + dx * t, ay + dy * t);
    }

    // The signed distance inside a triangle: positive within, by the nearest edge.
    private static float Inside(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
    {
        var sign = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax) < 0f ? -1f : 1f;
        return Math.Min(Edge(x, y, ax, ay, bx, by) * sign, Math.Min(Edge(x, y, bx, by, cx, cy) * sign, Edge(x, y, cx, cy, ax, ay) * sign));
    }

    private static float Edge(float x, float y, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax, dy = by - ay, l = (float)Math.Sqrt(dx * dx + dy * dy);
        return l < 1e-6f ? 0f : ((x - ax) * dy - (y - ay) * dx) / l * -1f;
    }
}
