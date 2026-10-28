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
            get { return SkateCaptureVariants.MovementKind(this); }
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
        public string SourceMovementKind { get { return SkateCaptureVariants.SourceMovementKind(Clip.MovementKind); } }
        private readonly double load, pop, air, caught, land, recovery;
        private readonly double[] rotationTimes, rotationProgress;
        private readonly int rotationKeys;
        private readonly double[] flightTimes, flightProgress;
        private readonly int flightKeys;
        private readonly double extraTurns, freeProgressStart, freeProgressEnd;
        public double FreeFlightEndProgress { get { return freeProgressEnd; } }
        public bool UsesSourceRotationClock { get { return rotationKeys > 1; } }
        public bool UsesSourceFlightClock { get { return flightKeys > 1; } }
        // A held flip may select another approved recipe without changing its
        // source body/contact clock. Family names alone cannot prove that.
        public bool SharesSourceWith(SkateCapturePlayback other)
        {
            if (other == null || SourceMovementKind != other.SourceMovementKind || Clip.Duration != other.Clip.Duration ||
                Clip.SampleCount != other.Clip.SampleCount || Clip.EventCount != other.Clip.EventCount) return false;
            for (int n = 0; n < Clip.EventCount; n++)
            {
                var a = Clip.GetEvent(n); var b = other.Clip.GetEvent(n);
                if (a.Time != b.Time || a.Phase != b.Phase || a.Contacts != b.Contacts) return false;
            }
            for (int row = 0; row < Clip.SampleCount; row++)
                for (int column = 0; column < 83; column++)
                    if (Clip.Number(row, column) != other.Clip.Number(row, column)) return false;
            return true;
        }
        private SkateCapturePlayback(SkateCaptureClip clip, double[] times)
        {
            Clip = clip; load = times[1]; pop = times[2]; air = times[3]; caught = times[4]; land = times[5]; recovery = times[6];
            BuildRotationClock(clip, air, caught, out rotationTimes, out rotationProgress, out rotationKeys);
            BuildRotationClock(clip, pop, caught, out flightTimes, out flightProgress, out flightKeys);
            int variant = SkateCaptureVariants.IndexOf(clip.MovementKind);
            int source = SkateCaptureVariants.IndexOf(SourceMovementKind);
            if (variant >= 0 && source >= 0 && Math.Abs(SkateCaptureVariants.Get(source).Signature.Flip) > .01)
                extraTurns = Math.Abs(SkateCaptureVariants.Get(variant).Signature.Flip / SkateCaptureVariants.Get(source).Signature.Flip) - 1;
            // Base and derived entries share the same reservation boundary.
            // Extra turns occupy only the fully free body interval, outside
            // the existing .08s release/catch foot-boundary adapters.
            freeProgressStart = ProgressAtSourceTime(air + .08);
            freeProgressEnd = ProgressAtSourceTime(caught - .08);
        }
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
            var candidate = new SkateCapturePlayback(clip, times);
            if (candidate.extraTurns > 0 && (candidate.freeProgressStart < 0 || candidate.freeProgressEnd > 1 ||
                candidate.freeProgressEnd <= candidate.freeProgressStart))
            { error = "Derived capture has no supported free-flight interval."; return false; }
            playback = candidate; return true;
        }
        private double ProgressAtSourceTime(double time)
        {
            if (!UsesSourceFlightClock) return Math.Max(0, Math.Min(1, (time - air) / (caught - air)));
            if (time <= flightTimes[0]) return flightProgress[0];
            for (int n = 1; n < flightKeys; n++) if (time <= flightTimes[n])
            {
                double u = (time - flightTimes[n - 1]) / (flightTimes[n] - flightTimes[n - 1]);
                return flightProgress[n - 1] + u * (flightProgress[n] - flightProgress[n - 1]);
            }
            return flightProgress[flightKeys - 1];
        }
        public double PresentationProgress(double liveProgress)
        {
            if (!Finite(liveProgress) || extraTurns <= 0) return liveProgress;
            double turn = Math.Max(0, Math.Min(1, liveProgress)) * (extraTurns + 1);
            if (turn <= freeProgressStart) return turn;
            if (turn >= freeProgressEnd + extraTurns) return turn - extraTurns;
            // Invert a monotone C2 expansion of only the free-flight interval.
            // The source pop/flick is not stretched across an inverted live deck.
            double low = freeProgressStart, high = freeProgressEnd;
            for (int n = 0; n < 32; n++)
            {
                double middle = (low + high) * .5, u = (middle - freeProgressStart) / (freeProgressEnd - freeProgressStart);
                double ease = u * u * u * (10 + u * (-15 + 6 * u));
                if (middle + extraTurns * ease < turn) low = middle; else high = middle;
            }
            return (low + high) * .5;
        }
        public bool Finished(double sinceLanding) { return sinceLanding >= Clip.Duration - land; }
        // Retain the source impact before handing recovery back to ordinary
        // riding. A short linear fade forces the knees through their entire
        // change of bend direction in a few render frames.
        public double RecoveryWeight(double sinceLanding)
        {
            if (!Finite(sinceLanding)) return 0;
            double remaining = Clip.Duration - land - sinceLanding;
            double duration = Math.Min(.45, (Clip.Duration - land) * .5);
            double u = Math.Max(0, Math.Min(1, remaining / duration));
            return u * u * u * (10 + u * (-15 + 6 * u));
        }
        public double SourceTime(SkateCapturePhase phase, double value)
        {
            if (phase == SkateCapturePhase.Load) return load + Math.Max(0, Math.Min(.999999, value)) * (pop - load);
            if (phase == SkateCapturePhase.Pop)
                return pop + Math.Max(0, Math.Min(.999999, value)) * (air - pop);
            if (phase == SkateCapturePhase.Air)
            {
                double progress = Math.Max(0, Math.Min(.999999, value));
                if (!UsesSourceRotationClock) return air + progress * (caught - air);
                // The first trusted Air pose may already contain some flip. Hold it
                // rather than replaying an earlier pose with a foot still on the deck.
                if (progress <= rotationProgress[0]) return air;
                if (progress >= rotationProgress[rotationKeys - 1]) return caught - .000001;
                int low = 0, high = rotationKeys - 1;
                while (high - low > 1)
                { int middle = (low + high) / 2; if (rotationProgress[middle] <= progress) low = middle; else high = middle; }
                double weight = (progress - rotationProgress[low]) / (rotationProgress[high] - rotationProgress[low]);
                return Math.Min(caught - .000001, rotationTimes[low] + weight * (rotationTimes[high] - rotationTimes[low]));
            }
            // A bounded negative lead finishes the still-free source approach
            // after live rotation stops. Source contacts remain authoritative.
            if (phase == SkateCapturePhase.Catch) return Math.Max(air, Math.Min(land - .000001, caught + value));
            // Land/recovery starts only from the caller's observed physical landing, never a source timestamp.
            return Math.Max(land, Math.Min(Clip.Duration, land + Math.Max(0, value)));
        }
        public double FlightSourceTime(double value)
        {
            double p = Math.Max(0, Math.Min(.999999, value));
            if (!UsesSourceFlightClock) return SourceTime(SkateCapturePhase.Air, value);
            if (p <= flightProgress[0]) return flightTimes[0];
            if (p >= flightProgress[flightKeys - 1]) return caught - .000001;
            int low = 0, high = flightKeys - 1;
            while (high - low > 1)
            { int middle = (low + high) / 2; if (flightProgress[middle] <= p) low = middle; else high = middle; }
            double u = (p - flightProgress[low]) / (flightProgress[high] - flightProgress[low]);
            return Math.Min(caught - .000001, flightTimes[low] + u * (flightTimes[high] - flightTimes[low]));
        }
        // Only these known Y*X*Z tracks have an unambiguous inverse.
        // Unsupported, non-invertible, reversing or near-gimbal tracks retain the old
        // phase clock. Pack validation and gameplay rotation authority are unchanged.
        private static void BuildRotationClock(SkateCaptureClip clip, double air, double caught,
            out double[] times, out double[] progress, out int count)
        {
            times = progress = null; count = 0;
            if (caught - air <= .000001) return;
            double rollTurn, yawTurn = 0;
            switch (SkateCaptureVariants.SourceMovementKind(clip.MovementKind))
            {
                case "heelflip": case "switch-heelflip": case "nollie-heelflip": rollTurn = -360; break;
                case "kickflip": case "fakie-kickflip": rollTurn = 360; break;
                case "nollie-backside-180-heelflip": rollTurn = -360; yawTurn = 180; break;
                case "360-flip": case "nollie-360-flip": rollTurn = 360; yawTurn = 360; break;
                case "bs-pop-shove-it": rollTurn = 0; yawTurn = 180; break;
                case "fs-pop-shove-it": rollTurn = 0; yawTurn = -180; break;
                default: return;
            }
            var samples = new double[clip.SampleCount + 2]; int sampleCount = 0;
            for (int row = 0; row < clip.SampleCount && clip.Number(row, 0) < caught; row++) samples[sampleCount++] = clip.Number(row, 0);
            samples[sampleCount++] = air; samples[sampleCount++] = caught;
            Array.Sort(samples, 0, sampleCount);
            var ts = new double[sampleCount]; var ps = new double[sampleCount]; int keys = 0;
            double previousTime = -1, previousRoll = 0, previousYaw = 0, roll = 0, yaw = 0, lastRollProgress = 0, lastYawProgress = 0;
            const double degrees = 180 / Math.PI;
            for (int n = 0; n < sampleCount; n++)
            {
                double t = samples[n]; if (t == previousTime) continue;
                int low = 0, high = clip.SampleCount - 1;
                while (high - low > 1) { int middle = (low + high) / 2; if (clip.Number(middle, 0) <= t) low = middle; else high = middle; }
                var a = new SkateCaptureRotation(clip.Number(low, 4), clip.Number(low, 5), clip.Number(low, 6), clip.Number(low, 7));
                var b = new SkateCaptureRotation(clip.Number(high, 4), clip.Number(high, 5), clip.Number(high, 6), clip.Number(high, 7));
                double w = Math.Max(0, Math.Min(1, (t - clip.Number(low, 0)) / (clip.Number(high, 0) - clip.Number(low, 0))));
                var q = SkateCaptureRotation.Slerp(a, b, w);
                var right = q.Rotate(new SkateVector(1, 0, 0)); var up = q.Rotate(new SkateVector(0, 1, 0)); var forward = q.Rotate(new SkateVector(0, 0, 1));
                if (Math.Abs(forward.Y) > .94) return; // Y*X*Z pitch must remain away from its 90-degree singularity.
                double r = Math.Atan2(right.Y, up.Y) * degrees, y = Math.Atan2(forward.X, forward.Z) * degrees;
                if (previousTime < 0)
                { if (Math.Abs(r) > 5 || Math.Abs(y) > 5) return; roll = r; yaw = y; }
                else
                {
                    double dr = WrapDegrees(r - previousRoll), dy = WrapDegrees(y - previousYaw);
                    if (Math.Abs(dr) > 90 || Math.Abs(dy) > 90) return;
                    roll += dr; yaw += dy;
                }
                previousRoll = r; previousYaw = y; previousTime = t;
                if (yawTurn == 0 && Math.Abs(yaw) > 5 || rollTurn == 0 && Math.Abs(roll) > 5) return;
                if (t < air) continue;
                double rp = rollTurn != 0 ? roll / rollTurn : 0, yp = yawTurn != 0 ? yaw / yawTurn : 0;
                if (rp < 0 || rp > 1.02 || yp < 0 || yp > 1.02 ||
                    keys > 0 && (rp < lastRollProgress - .00000001 || yp < lastYawProgress - .00000001)) return;
                lastRollProgress = rp; lastYawProgress = yp;
                double p = rollTurn == 0 ? yp : yawTurn != 0 ? Math.Min(rp, yp) : rp;
                // A plateau would make the inverse jump across source body motion.
                if (!Finite(p) || p < 0 || p > 1.02 || keys > 0 && p <= ps[keys - 1] + .00000001) return;
                ts[keys] = t; ps[keys++] = p;
            }
            if (keys < 2 || Math.Abs(roll - rollTurn) > 5 || Math.Abs(yaw - yawTurn) > 5 || ps[0] >= .5) return;
            times = ts; progress = ps; count = keys;
        }
        private static double WrapDegrees(double angle)
        { return angle > 180 ? angle - 360 : angle < -180 ? angle + 360 : angle; }
        public bool TrySample(SkateCapturePhase phase, double value, SkateRiderRig rig, SkateStance stance,
            SkateBoardPose liveBoard, SkateVector liveHeading, SkateVector supportUp,
            out SkateCaptureFrame shown, out string error, SkateCaptureFootSupport footSupport = null, bool includePopSequence = false,
            bool adaptFreeFootBoundaries = false)
        {
            shown = default(SkateCaptureFrame); error = null;
            if (!SkateBoardPath.Valid(liveBoard) || !Finite(value) || (phase < SkateCapturePhase.Load || phase > SkateCapturePhase.Recovery))
            { error = "Invalid live capture presentation frame."; return false; }
            if (footSupport != null && !footSupport.Matches(Clip, stance))
            { error = "Captured foot support belongs to another clip or stance."; return false; }
            SkateCaptureFrame source;
            double sourceTime = includePopSequence && phase == SkateCapturePhase.Air ? FlightSourceTime(value) : SourceTime(phase, value);
            if (!SkateCaptureSampler.TrySample(Clip, sourceTime, rig, stance, out source, out error)) return false;
            // The pack is canonical board space. Keep real pelvic facing/anticipation; hips are NOT a camera carrier.
            // Only the explicit body-spin descriptor removes source world heading. It has no shove,
            // so board forward isolates that heading even while its local longitudinal flip turns.
            var along = new SkateVector(0, 0, 1);
            if (SourceMovementKind == "nollie-backside-180-heelflip")
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
            if (adaptFreeFootBoundaries && footSupport != null)
            {
                if ((contacts & SkateCaptureContacts.LeftFoot) == 0 &&
                    !TryFreeBoundary(source.Pose.LeftAnkle, source.Pose.LeftToe, source.LeftSoleUp,
                        source.Board, liveBoard, footSupport.FreeBoundaryWeight(0, sourceTime), ref la, ref lt, ref leftUp) ||
                    (contacts & SkateCaptureContacts.RightFoot) == 0 &&
                    !TryFreeBoundary(source.Pose.RightAnkle, source.Pose.RightToe, source.RightSoleUp,
                        source.Board, liveBoard, footSupport.FreeBoundaryWeight(1, sourceTime), ref ra, ref rt, ref rightUp))
                { error = "Capture release/catch has a degenerate foot frame."; return false; }
            }
            if (footSupport != null)
            {
                SkateBoardPose leftFoot, rightFoot; SkateVector leftFit, rightFit;
                if (!SkateCaptureFootSupport.TryFootFrame(la, lt, leftUp, out leftFoot) ||
                    !SkateCaptureFootSupport.TryFootFrame(ra, rt, rightUp, out rightFoot))
                { error = "Live captured foot frame is degenerate."; return false; }
                if (!footSupport.TryOffsets(sourceTime, contacts, liveBoard, leftFoot, rightFoot,
                    out leftFit, out rightFit, out error)) return false;
                la += leftFit; lt += leftFit; ra += rightFit; rt += rightFit;
            }
            var hipRight = Unit(body.RightHip - body.LeftHip); var hipOffset = hipRight * (rig.HipWidth * .5); SkateVector pelvis;
            double reach = (rig.ThighLength + rig.ShinLength) * (footSupport == null ? 1 : SkateCaptureFootSupport.ReachShare);
            if (!SkateCaptureSampler.FitPelvis(body.Pelvis, la + hipOffset, ra - hipOffset, reach, out pelvis))
            { error = "Live board contacts exceed the measured capture reach bound."; return false; }
            var shift = pelvis - body.Pelvis; var lh = body.LeftHip + shift; var rh = body.RightHip + shift; SkateVector lk, rk, lp, rp;
            if (!SkateCaptureFootSupport.TryBendPlane(body.LeftHip, body.LeftKnee, body.LeftAnkle, lh, la, out lp) ||
                !SkateCaptureFootSupport.TryBendPlane(body.RightHip, body.RightKnee, body.RightAnkle, rh, ra, out rp))
            { error = "Live board adaptation has an underdetermined leg bend plane."; return false; }
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, lp, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, rp, out rk, out error)) return false;
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
            SkateCaptureFrame target, double weight, out SkateCaptureFrame shown, out string error, bool pinFeet = false,
            SkateCaptureContacts pinContacts = SkateCaptureContacts.None)
        {
            shown = target; error = null;
            if (!Finite(weight) || weight < 0 || weight > 1 ||
                (pinContacts & ~(SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot)) != 0)
            { error = "Invalid capture transition weight or foot mask."; return false; }
            if (weight >= 1) return true;
            bool pinLeft = pinFeet || (pinContacts & SkateCaptureContacts.LeftFoot) != 0;
            bool pinRight = pinFeet || (pinContacts & SkateCaptureContacts.RightFoot) != 0;
            var b = target.Pose;
            var pelvis = Lerp(from.Pelvis, b.Pelvis, weight);
            var across = Unit(Lerp(from.RightHip - from.LeftHip, b.RightHip - b.LeftHip, weight));
            var pu = Unit(Project(Lerp(from.PelvisUp, b.PelvisUp, weight), across));
            var cu = Unit(Lerp(from.ChestUp, b.ChestUp, weight));
            var shoulders = Unit(Project(Lerp(from.RightShoulder - from.LeftShoulder, b.RightShoulder - b.LeftShoulder, weight), cu));
            var la = pinLeft ? b.LeftAnkle : Lerp(from.LeftAnkle, b.LeftAnkle, weight);
            var ra = pinRight ? b.RightAnkle : Lerp(from.RightAnkle, b.RightAnkle, weight);
            var hipOffset = across * (rig.HipWidth * .5); SkateVector fitted;
            double reach = (rig.ThighLength + rig.ShinLength) * (pinLeft || pinRight ? SkateCaptureFootSupport.ReachShare : 1);
            if (!SkateCaptureSampler.FitPelvis(pelvis, la + hipOffset, ra - hipOffset, reach, out fitted))
            { error = "Capture transition cannot preserve the measured leg contacts."; return false; }
            var blendFit = fitted - pelvis; pelvis = fitted; var lh = pelvis - hipOffset; var rh = pelvis + hipOffset;
            var chest = pelvis + cu * rig.SpineLength; var neck = pelvis + cu * (rig.SpineLength * 1.12);
            var head = neck + Unit(Lerp(from.Head - from.Neck, b.Head - b.Neck, weight)) * rig.NeckToHeadLength;
            var shoulderCentre = chest + cu * rig.ShoulderRise;
            var ls = shoulderCentre - shoulders * (rig.ShoulderWidth * .5); var rs = shoulderCentre + shoulders * (rig.ShoulderWidth * .5);
            var leftHand = BoundHand(ls, Lerp(from.LeftHand, b.LeftHand, weight), rig.UpperArmLength + rig.ForearmLength);
            var rightHand = BoundHand(rs, Lerp(from.RightHand, b.RightHand, weight), rig.UpperArmLength + rig.ForearmLength);
            var lu = pinLeft ? target.LeftSoleUp : Unit(Lerp(fromLeftUp, target.LeftSoleUp, weight));
            var ru = pinRight ? target.RightSoleUp : Unit(Lerp(fromRightUp, target.RightSoleUp, weight));
            var lt = pinLeft ? b.LeftToe : la + Unit(Project(Lerp(from.LeftToe - from.LeftAnkle, b.LeftToe - b.LeftAnkle, weight), lu)) * rig.FootLength;
            var rt = pinRight ? b.RightToe : ra + Unit(Project(Lerp(from.RightToe - from.RightAnkle, b.RightToe - b.RightAnkle, weight), ru)) * rig.FootLength;
            SkateVector lk, rk, le, re, lp, rp;
            if (!SkateCaptureFootSupport.TryBlendBendPlanes(from.LeftHip, from.LeftKnee, from.LeftAnkle,
                    b.LeftHip, b.LeftKnee, b.LeftAnkle, lh, la, weight, out lp) ||
                !SkateCaptureFootSupport.TryBlendBendPlanes(from.RightHip, from.RightKnee, from.RightAnkle,
                    b.RightHip, b.RightKnee, b.RightAnkle, rh, ra, weight, out rp))
            { error = "Capture transition has an underdetermined leg bend plane."; return false; }
            if (across.LengthSquared < .9 || pu.LengthSquared < .9 || cu.LengthSquared < .9 || shoulders.LengthSquared < .9 || lu.LengthSquared < .9 || ru.LengthSquared < .9 ||
                !SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, lp, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, rp, out rk, out error) ||
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
        private static bool TryFreeBoundary(SkateVector sourceAnkle, SkateVector sourceToe, SkateVector sourceUp,
            SkateBoardPose sourceBoard, SkateBoardPose liveBoard, double weight,
            ref SkateVector ankle, ref SkateVector toe, ref SkateVector soleUp)
        {
            if (weight <= 0) return true;
            var mappedAnkle = Point(sourceAnkle, sourceBoard, liveBoard);
            var mappedToe = Point(sourceToe, sourceBoard, liveBoard);
            var mappedUp = Rotate(sourceUp, sourceBoard, liveBoard);
            SkateBoardPose free, attached;
            if (!SkateCaptureFootSupport.TryFootFrame(ankle, toe, soleUp, out free) ||
                !SkateCaptureFootSupport.TryFootFrame(mappedAnkle, mappedToe, mappedUp, out attached)) return false;
            var localToe = Local(toe - ankle, free);
            var rotation = SkateCaptureRotation.Slerp(FrameRotation(free), FrameRotation(attached), weight);
            ankle = Lerp(ankle, mappedAnkle, weight);
            toe = ankle + rotation.Rotate(localToe);
            soleUp = rotation.Rotate(new SkateVector(0, 1, 0));
            return true;
        }
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
