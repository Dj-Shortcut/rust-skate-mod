using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shortcut.RustMod
{
    // Local owner approval is separate from producer review metadata and pins the exact pack bytes.
    public sealed class SkateCaptureApproval
    {
        public const string FileName = "skate_captures.approved.txt";
        public const int MaximumBytes = 32768;
        public readonly string Sha256;
        private readonly string[] ids;
        private SkateCaptureApproval(string digest, string[] clips) { Sha256 = digest; ids = clips; }
        public int Count { get { return ids.Length; } }
        public string GetId(int n) { return n >= 0 && n < ids.Length ? ids[n] : null; }
        public bool Contains(string id) { return Array.IndexOf(ids, id) >= 0; }
        public static bool TryRead(Stream stream, out SkateCaptureApproval approval, out string error)
        {
            approval = null; error = null;
            if (stream == null || !stream.CanRead) { error = "Capture approval is unreadable."; return false; }
            try
            {
                var bytes = new byte[MaximumBytes + 1]; int count = 0, got;
                while (count < bytes.Length && (got = stream.Read(bytes, count, bytes.Length - count)) > 0) count += got;
                if (count > MaximumBytes) { error = "Capture approval exceeds its byte limit."; return false; }
                var text = new UTF8Encoding(false, true).GetString(bytes, 0, count);
                if (text.IndexOf('\r') >= 0) text = text.Replace("\r\n", "\n");
                var lines = text.Split('\n'); int length = lines.Length;
                if (length > 0 && lines[length - 1].Length == 0) length--;
                if (length < 3 || length > 258 || lines[0] != "SKMCAP-APPROVAL-1" || !lines[1].StartsWith("sha256=", StringComparison.Ordinal))
                { error = "Invalid capture approval header or count."; return false; }
                string digest = lines[1].Substring(7);
                if (digest.Length != 64) { error = "Invalid capture approval digest."; return false; }
                foreach (char c in digest) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
                { error = "Invalid capture approval digest."; return false; }
                var clips = new string[length - 2]; var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int n = 2; n < length; n++)
                {
                    if (!lines[n].StartsWith("clip=", StringComparison.Ordinal)) { error = "Invalid capture approval entry."; return false; }
                    string id = lines[n].Substring(5);
                    if (!ValidId(id) || !unique.Add(id)) { error = "Invalid or duplicate approved capture id."; return false; }
                    clips[n - 2] = id;
                }
                approval = new SkateCaptureApproval(digest, clips); return true;
            }
            catch (Exception) { error = "Capture approval decoding failed."; return false; }
        }
        private static bool ValidId(string id)
        {
            if (id.Length == 0 || id.Length > 64) return false;
            for (int n = 0; n < id.Length; n++)
            {
                char c = id[n]; if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || n > 0 && (c == '-' || c == '_'))) return false;
            }
            return true;
        }
    }

    public readonly struct SkateCaptureTrickSignature
    {
        public readonly SkatePopKind Pop;
        public readonly bool Switch, TailLeads;
        public readonly double Flip, Shove, Spin, Body;
        public SkateCaptureTrickSignature(SkatePopKind pop, bool sw, bool tail, double flip, double shove, double spin, double body)
        { Pop = pop; Switch = sw; TailLeads = tail; Flip = flip; Shove = shove; Spin = spin; Body = body; }
        public string MovementKind
        {
            get
            {
                if (!Near(Body, 0)) return null;
                if (Pop == SkatePopKind.Ollie && Switch && !TailLeads && Near(Flip, -360) && Near(Shove, 0) && Near(Spin, 0)) return "switch-heelflip";
                if (Pop == SkatePopKind.Ollie && Switch && TailLeads && Near(Flip, 360) && Near(Shove, 0) && Near(Spin, 0)) return "fakie-kickflip";
                if (Pop != SkatePopKind.Nollie || Switch || TailLeads) return null;
                if (Near(Flip, -360) && Near(Shove, 0))
                    return Near(Spin, 0) ? "nollie-heelflip" : Near(Spin, -180) ? "nollie-backside-180-heelflip" : null;
                return Near(Flip, 360) && Near(Shove, -360) && Near(Spin, 0) ? "nollie-360-flip" : null;
            }
        }
        private static bool Near(double a, double b) { return !double.IsNaN(a) && !double.IsInfinity(a) && Math.Abs(a - b) < .01; }
        public bool Same(SkateCaptureTrickSignature b)
        { return Pop == b.Pop && Switch == b.Switch && TailLeads == b.TailLeads && Near(Flip, b.Flip) && Near(Shove, b.Shove) && Near(Spin, b.Spin) && Near(Body, b.Body); }
        public double RotationProgress(double flip, double shove, double spin)
        {
            // The last unfinished live rotation owns the catch. A new requested rotation changes the signature.
            double progress = 1;
            if (Math.Abs(Flip) > .01) progress = Math.Min(progress, flip / Flip);
            if (Math.Abs(Shove) > .01) progress = Math.Min(progress, shove / Shove);
            if (Math.Abs(Spin) > .01) progress = Math.Min(progress, spin / Spin);
            return Math.Max(0, Math.Min(1, progress));
        }
    }

    // Phase clock and retargeting only. Callers retain input, scoring, movement and landing authority.
    public sealed class SkateCapturePlayback
    {
        public readonly SkateCaptureClip Clip;
        private readonly double load, pop, air, caught, land, recovery;
        private SkateCapturePlayback(SkateCaptureClip clip, double[] times)
        { Clip = clip; load = times[1]; pop = times[2]; air = times[3]; caught = times[4]; land = times[5]; recovery = times[6]; }
        public static bool TryCreate(SkateCaptureClip clip, out SkateCapturePlayback playback, out string error)
        {
            playback = null; error = null;
            if (clip == null || !clip.Ready) { error = "Capture is unavailable."; return false; }
            var times = new double[7]; var seen = new bool[7];
            const SkateCaptureContacts feet = SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot;
            var previousContacts = feet; int previousPhase = 0; double previousTime = -1;
            for (int n = 0; n < clip.EventCount; n++)
            {
                var e = clip.GetEvent(n); int phase = (int)e.Phase;
                if (phase < 1 || phase > 6 || phase < previousPhase || e.Time <= previousTime ||
                    (e.Contacts & ~feet) != 0 || n == 0 && (phase != 1 || e.Contacts != feet))
                { error = "Capture has unsupported or unordered presentation events."; return false; }
                // A sliding/flick foot may leave before the pop foot. Keep each
                // source-observed release instead of requiring a simultaneous detach.
                // Catch contacts may then accumulate, but never drop before landing.
                if (phase < 4 && (e.Contacts & ~previousContacts) != 0 ||
                    phase >= 4 && (previousContacts & ~e.Contacts) != 0 ||
                    phase == 1 && e.Contacts == SkateCaptureContacts.None ||
                    phase == 3 && e.Contacts != SkateCaptureContacts.None ||
                    phase == 4 && e.Contacts == SkateCaptureContacts.None ||
                    phase >= 5 && e.Contacts != feet)
                { error = "Capture foot contacts do not match its presentation phases."; return false; }
                if (!seen[phase]) { seen[phase] = true; times[phase] = e.Time; }
                previousContacts = e.Contacts; previousPhase = phase; previousTime = e.Time;
            }
            for (int n = 1; n <= 6; n++) if (!seen[n] || n > 1 && times[n] <= times[n - 1])
            { error = "Capture needs ordered load, pop, air, catch, land and recovery phases."; return false; }
            playback = new SkateCapturePlayback(clip, times); return true;
        }
        public bool Finished(double sinceLanding) { return sinceLanding >= Clip.Duration - land; }
        public double SourceTime(SkateCapturePhase phase, double value)
        {
            if (phase == SkateCapturePhase.Load) return load + Math.Max(0, Math.Min(.999999, value)) * (pop - load);
            if (phase == SkateCapturePhase.Pop)
                return pop + Math.Max(0, Math.Min(.999999, value)) * (air - pop);
            if (phase == SkateCapturePhase.Air)
                return air + Math.Max(0, Math.Min(.999999, value)) * (caught - air);
            if (phase == SkateCapturePhase.Catch) return Math.Min(land - .000001, caught + Math.Max(0, value));
            // Land/recovery starts only from the caller's observed physical landing, never a source timestamp.
            return Math.Max(land, Math.Min(Clip.Duration, land + Math.Max(0, value)));
        }
        public bool TrySample(SkateCapturePhase phase, double value, SkateRiderRig rig, SkateStance stance,
            SkateBoardPose liveBoard, SkateVector liveHeading, SkateVector supportUp,
            out SkateCaptureFrame shown, out string error)
        {
            shown = default(SkateCaptureFrame); error = null;
            if (!SkateBoardPath.Valid(liveBoard) || !Finite(value) || (phase < SkateCapturePhase.Load || phase > SkateCapturePhase.Recovery))
            { error = "Invalid live capture presentation frame."; return false; }
            SkateCaptureFrame source;
            if (!SkateCaptureSampler.TrySample(Clip, SourceTime(phase, value), rig, stance, out source, out error)) return false;
            // The pack is canonical board space. Keep real pelvic facing/anticipation; hips are NOT a camera carrier.
            // Only the explicit body-spin descriptor removes source world heading. It has no shove,
            // so board forward isolates that heading even while its local longitudinal flip turns.
            var along = new SkateVector(0, 0, 1);
            if (Clip.MovementKind == "nollie-backside-180-heelflip")
                along = Unit(new SkateVector(source.Board.Forward.X, 0, source.Board.Forward.Z));
            var from = new SkateBoardPose(source.Board.Origin, along, new SkateVector(0, 1, 0));
            var to = new SkateBoardPose(liveBoard.Origin, liveHeading, supportUp);
            SkateRiderPose body;
            if (!SkateBoardPath.Valid(from) || !SkateBoardPath.Valid(to) || !SkateRider.TryTransformPose(source.Pose, from, to, out body))
            { error = "Capture body carrier is degenerate."; return false; }
            var leftUp = Rotate(source.LeftSoleUp, from, to); var rightUp = Rotate(source.RightSoleUp, from, to);
            var la = body.LeftAnkle; var ra = body.RightAnkle; var lt = body.LeftToe; var rt = body.RightToe;
            var contacts = source.Observation.Contacts;
            if ((contacts & SkateCaptureContacts.LeftFoot) != 0)
            { la = Point(source.Pose.LeftAnkle, source.Board, liveBoard); lt = Point(source.Pose.LeftToe, source.Board, liveBoard); leftUp = Rotate(source.LeftSoleUp, source.Board, liveBoard); }
            if ((contacts & SkateCaptureContacts.RightFoot) != 0)
            { ra = Point(source.Pose.RightAnkle, source.Board, liveBoard); rt = Point(source.Pose.RightToe, source.Board, liveBoard); rightUp = Rotate(source.RightSoleUp, source.Board, liveBoard); }
            var hipRight = Unit(body.RightHip - body.LeftHip); var hipOffset = hipRight * (rig.HipWidth * .5); SkateVector pelvis;
            if (!SkateCaptureSampler.FitPelvis(body.Pelvis, la + hipOffset, ra - hipOffset, rig.ThighLength + rig.ShinLength, out pelvis))
            { error = "Live board contacts exceed the measured capture reach bound."; return false; }
            var shift = pelvis - body.Pelvis; var lh = body.LeftHip + shift; var rh = body.RightHip + shift; SkateVector lk, rk;
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, body.LeftKnee - body.LeftHip, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, body.RightKnee - body.RightHip, out rk, out error)) return false;
            var pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, lt, rt, body.Chest + shift, body.Neck + shift, body.Head + shift,
                body.LeftShoulder + shift, body.RightShoulder + shift, body.LeftElbow + shift, body.RightElbow + shift,
                body.LeftHand + shift, body.RightHand + shift, body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp, body.HeadForward);
            double height = SkateVector.Dot(pose.Pelvis - liveBoard.Origin, supportUp);
            if (SkateVector.Dot(pose.PelvisUp, supportUp) < .25 || height < .15 || height > 1.5)
            { error = "Capture phase does not fit the current upright support."; return false; }
            shown = new SkateCaptureFrame(pose, liveBoard, FrameRotation(liveBoard), leftUp, rightUp, true,
                source.Observation, Rotate(source.PelvisCorrection, from, to) + shift, Rotate(source.LeftHandCorrection, from, to), Rotate(source.RightHandCorrection, from, to), to.Up); return true;
        }
        // A presentation crossfade must solve the measured chains again; point lerps alone shrink limbs.
        public static bool TryBlend(SkateRiderRig rig, SkateRiderPose from, SkateVector fromLeftUp, SkateVector fromRightUp,
            SkateCaptureFrame target, double weight, out SkateCaptureFrame shown, out string error, bool pinFeet = false)
        {
            shown = target; error = null;
            if (!Finite(weight) || weight < 0 || weight > 1) { error = "Invalid capture transition weight."; return false; }
            if (weight >= 1) return true;
            var b = target.Pose;
            var pelvis = Lerp(from.Pelvis, b.Pelvis, weight);
            var across = Unit(Lerp(from.RightHip - from.LeftHip, b.RightHip - b.LeftHip, weight));
            var pu = Unit(Project(Lerp(from.PelvisUp, b.PelvisUp, weight), across));
            var cu = Unit(Lerp(from.ChestUp, b.ChestUp, weight));
            var shoulders = Unit(Project(Lerp(from.RightShoulder - from.LeftShoulder, b.RightShoulder - b.LeftShoulder, weight), cu));
            var la = pinFeet ? b.LeftAnkle : Lerp(from.LeftAnkle, b.LeftAnkle, weight);
            var ra = pinFeet ? b.RightAnkle : Lerp(from.RightAnkle, b.RightAnkle, weight);
            var hipOffset = across * (rig.HipWidth * .5); SkateVector fitted;
            if (!SkateCaptureSampler.FitPelvis(pelvis, la + hipOffset, ra - hipOffset, rig.ThighLength + rig.ShinLength, out fitted))
            { error = "Capture transition cannot preserve the measured leg contacts."; return false; }
            var blendFit = fitted - pelvis; pelvis = fitted; var lh = pelvis - hipOffset; var rh = pelvis + hipOffset;
            var chest = pelvis + cu * rig.SpineLength; var neck = pelvis + cu * (rig.SpineLength * 1.12);
            var head = neck + Unit(Lerp(from.Head - from.Neck, b.Head - b.Neck, weight)) * rig.NeckToHeadLength;
            var shoulderCentre = chest + cu * rig.ShoulderRise;
            var ls = shoulderCentre - shoulders * (rig.ShoulderWidth * .5); var rs = shoulderCentre + shoulders * (rig.ShoulderWidth * .5);
            var leftHand = BoundHand(ls, Lerp(from.LeftHand, b.LeftHand, weight), rig.UpperArmLength + rig.ForearmLength);
            var rightHand = BoundHand(rs, Lerp(from.RightHand, b.RightHand, weight), rig.UpperArmLength + rig.ForearmLength);
            var lu = pinFeet ? target.LeftSoleUp : Unit(Lerp(fromLeftUp, target.LeftSoleUp, weight));
            var ru = pinFeet ? target.RightSoleUp : Unit(Lerp(fromRightUp, target.RightSoleUp, weight));
            var lt = pinFeet ? b.LeftToe : la + Unit(Project(Lerp(from.LeftToe - from.LeftAnkle, b.LeftToe - b.LeftAnkle, weight), lu)) * rig.FootLength;
            var rt = pinFeet ? b.RightToe : ra + Unit(Project(Lerp(from.RightToe - from.RightAnkle, b.RightToe - b.RightAnkle, weight), ru)) * rig.FootLength;
            SkateVector lk, rk, le, re;
            if (across.LengthSquared < .9 || pu.LengthSquared < .9 || cu.LengthSquared < .9 || shoulders.LengthSquared < .9 || lu.LengthSquared < .9 || ru.LengthSquared < .9 ||
                !SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, Lerp(from.LeftKnee - from.LeftHip, b.LeftKnee - b.LeftHip, weight), out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, Lerp(from.RightKnee - from.RightHip, b.RightKnee - b.RightHip, weight), out rk, out error) ||
                !SkateOllieStyles.TryLimb(ls, leftHand, rig.UpperArmLength, rig.ForearmLength, Lerp(from.LeftElbow - from.LeftShoulder, b.LeftElbow - b.LeftShoulder, weight), out le, out error) ||
                !SkateOllieStyles.TryLimb(rs, rightHand, rig.UpperArmLength, rig.ForearmLength, Lerp(from.RightElbow - from.RightShoulder, b.RightElbow - b.RightShoulder, weight), out re, out error))
            { if (error == null) error = "Capture transition frame is degenerate."; return false; }
            var pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, lt, rt, chest, neck, head, ls, rs, le, re, leftHand, rightHand,
                SkateBoardPath.Cross(across, pu), pu, SkateBoardPath.Cross(shoulders, cu), cu, Unit(Lerp(from.HeadForward, b.HeadForward, weight)));
            shown = new SkateCaptureFrame(pose, target.Board, target.Rotation, lu, ru, target.HasEvent, target.Observation,
                target.PelvisCorrection + blendFit, target.LeftHandCorrection, target.RightHandCorrection, target.RiderUp); return true;
        }
        public static SkateCaptureFrame Transport(SkateCaptureFrame frame, SkateBoardPose from, SkateBoardPose to)
        {
            SkateRiderPose pose;
            if (!SkateRider.TryTransformPose(frame.Pose, from, to, out pose)) return default(SkateCaptureFrame);
            return new SkateCaptureFrame(pose, to, FrameRotation(to), Rotate(frame.LeftSoleUp, from, to), Rotate(frame.RightSoleUp, from, to),
                frame.HasEvent, frame.Observation, Rotate(frame.PelvisCorrection, from, to), Rotate(frame.LeftHandCorrection, from, to), Rotate(frame.RightHandCorrection, from, to), to.Up);
        }
        private static SkateCaptureRotation FrameRotation(SkateBoardPose frame)
        {
            var r = frame.Right; var u = frame.Up; var f = frame.Forward; double x, y, z, w, s, trace = r.X + u.Y + f.Z;
            if (trace > 0) { s = Math.Sqrt(trace + 1) * 2; w = .25 * s; x = (u.Z - f.Y) / s; y = (f.X - r.Z) / s; z = (r.Y - u.X) / s; }
            else if (r.X > u.Y && r.X > f.Z) { s = Math.Sqrt(1 + r.X - u.Y - f.Z) * 2; w = (u.Z - f.Y) / s; x = .25 * s; y = (u.X + r.Y) / s; z = (f.X + r.Z) / s; }
            else if (u.Y > f.Z) { s = Math.Sqrt(1 + u.Y - r.X - f.Z) * 2; w = (f.X - r.Z) / s; x = (u.X + r.Y) / s; y = .25 * s; z = (f.Y + u.Z) / s; }
            else { s = Math.Sqrt(1 + f.Z - r.X - u.Y) * 2; w = (r.Y - u.X) / s; x = (f.X + r.Z) / s; y = (f.Y + u.Z) / s; z = .25 * s; }
            return new SkateCaptureRotation(x, y, z, w).Unit();
        }
        private static SkateVector BoundHand(SkateVector shoulder, SkateVector hand, double radius)
        { var d = hand - shoulder; return d.Length > radius ? shoulder + d * ((radius - 1e-9) / d.Length) : hand; }
        private static SkateVector Project(SkateVector v, SkateVector normal) { return v - normal * SkateVector.Dot(v, normal); }
        private static SkateVector Lerp(SkateVector a, SkateVector b, double t) { return a + (b - a) * t; }
        private static SkateVector Local(SkateVector v, SkateBoardPose f)
        { return new SkateVector(SkateVector.Dot(v, f.Right), SkateVector.Dot(v, f.Up), SkateVector.Dot(v, f.Forward)); }
        private static SkateVector Point(SkateVector p, SkateBoardPose from, SkateBoardPose to) { return to.Point(Local(p - from.Origin, from)); }
        private static SkateVector Rotate(SkateVector p, SkateBoardPose from, SkateBoardPose to) { return to.Rotate(Local(p, from)); }
        private static SkateVector Unit(SkateVector v) { return v.LengthSquared > .00000001 ? v * (1 / v.Length) : default(SkateVector); }
        private static bool Finite(double n) { return !double.IsNaN(n) && !double.IsInfinity(n); }
    }
}
