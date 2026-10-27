using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shortcut.RustMod
{
    // Managed, runtime-neutral parser. External data is never executable code or downloaded here.
    public static class SkateCapturePack
    {
        public const string FileName = "skate_captures.skmc";
        public const int MaximumBytes = 32 * 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static bool TryRead(Stream stream, out SkateCaptureLibrary library, out string error)
        {
            library = null; error = null;
            try
            {
                if (stream == null || !stream.CanRead) throw new InvalidDataException("The capture stream is not readable.");
                var reader = new Cursor(ReadBounded(stream));
                const string magic = "SKMCAP01";
                for (int n = 0; n < magic.Length; n++) if (reader.Byte() != magic[n]) throw new InvalidDataException("The capture pack signature is invalid.");
                uint count = reader.UInt32();
                if (count < 1 || count > 256) throw new InvalidDataException("Capture clip count is outside 1..256.");
                var clips = new SkateCaptureClip[(int)count];
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long total = 0;
                for (int index = 0; index < clips.Length; index++)
                {
                    string id = reader.Text(64), label = reader.Text(192), url = reader.Text(2048), author = reader.Text(192), kind = reader.Text(64);
                    if (!ValidId(id) || !ids.Add(id)) throw new InvalidDataException("Capture IDs must be unique filename stems, including letter case aliases.");
                    if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(author))
                        throw new InvalidDataException("Capture label, movement kind and source provenance are required.");
                    byte reviewed = reader.Byte();
                    if (reviewed > 1) throw new InvalidDataException("Capture review status must be 0 or 1.");
                    double duration = reader.Double(); uint rows = reader.UInt32();
                    if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0 || duration > 10 || rows < 2 || rows > 2401)
                        throw new InvalidDataException("Capture duration or sample count is outside its bounds.");
                    total += rows;
                    if (total > 200000) throw new InvalidDataException("Capture pack total sample count exceeds 200000.");
                    int values = checked((int)rows * 83);
                    reader.Require(checked((long)values * 8 + 2)); // Check actual remaining bytes before allocation.
                    var data = new double[values]; for (int n = 0; n < data.Length; n++) data[n] = reader.Double();
                    int events = reader.UInt16();
                    if (events > 128) throw new InvalidDataException("Capture event count exceeds 128.");
                    reader.Require(events * 10L);
                    var observations = new SkateCaptureEvent[events]; double last = -1;
                    for (int n = 0; n < events; n++)
                    {
                        double time = reader.Double(); byte phase = reader.Byte(), contacts = reader.Byte();
                        if (double.IsNaN(time) || double.IsInfinity(time) || time < 0 || time > duration || time < last || phase > 6 || contacts > 15)
                            throw new InvalidDataException("Capture event time, phase or contact mask is invalid.");
                        observations[n] = new SkateCaptureEvent(time, (SkateCapturePhase)phase, (SkateCaptureContacts)contacts); last = time;
                    }
                    var clip = new SkateCaptureClip(id, label, url, author, kind, reviewed == 1, duration, (int)rows, data, observations);
                    string reason;
                    if (!SkateCaptureSampler.Validate(clip, out reason)) throw new InvalidDataException(reason);
                    clips[index] = clip;
                }
                if (!reader.End) throw new InvalidDataException("The capture pack contains trailing data.");
                library = new SkateCaptureLibrary(clips); return true;
            }
            catch (Exception e)
            {
                error = e is InvalidDataException || e is DecoderFallbackException || e is EndOfStreamException
                    ? "Capture pack rejected: " + e.Message : "Capture pack could not be read: " + e.GetType().Name;
                return false;
            }
        }
        private static byte[] ReadBounded(Stream stream)
        {
            if (stream.CanSeek)
            {
                long length = stream.Length - stream.Position;
                if (length < 12 || length > MaximumBytes) throw new InvalidDataException("Capture pack must be 12 bytes..32 MiB.");
                var data = new byte[(int)length]; int at = 0;
                while (at < data.Length) { int got = stream.Read(data, at, data.Length - at); if (got <= 0) throw new EndOfStreamException("Capture pack ended early."); at += got; }
                if (stream.ReadByte() != -1) throw new InvalidDataException("Capture stream grew beyond its declared length.");
                return data;
            }
            // A non-seekable caller cannot evade the file bound. Never grow the buffer above
            // MaximumBytes; at most one extra byte is read to distinguish EOF from oversized input.
            using (var memory = new MemoryStream())
            {
                var block = new byte[16384]; int total = 0;
                while (total < MaximumBytes)
                {
                    int got = stream.Read(block, 0, Math.Min(block.Length, MaximumBytes - total));
                    if (got <= 0) return memory.ToArray();
                    memory.Write(block, 0, got); total += got;
                }
                if (stream.ReadByte() != -1) throw new InvalidDataException("Capture pack exceeds 32 MiB.");
                return memory.ToArray();
            }
        }
        private static bool ValidId(string value)
        {
            if (value.Length == 0) return false;
            foreach (char c in value) if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-' || c == '_')) return false;
            if (value.Equals("CON", StringComparison.OrdinalIgnoreCase) || value.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("AUX", StringComparison.OrdinalIgnoreCase) || value.Equals("NUL", StringComparison.OrdinalIgnoreCase)) return false;
            return !(value.Length == 4 && (value.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || value.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && value[3] >= '1' && value[3] <= '9');
        }
        private sealed class Cursor
        {
            private readonly byte[] bytes; private int at;
            public Cursor(byte[] value) { bytes = value; }
            public bool End { get { return at == bytes.Length; } }
            public void Require(long count) { if (count < 0 || count > bytes.Length - at) throw new EndOfStreamException("Capture pack is truncated."); }
            public byte Byte() { Require(1); return bytes[at++]; }
            public ushort UInt16() { Require(2); int value = bytes[at] | bytes[at + 1] << 8; at += 2; return (ushort)value; }
            public uint UInt32() { Require(4); uint v = (uint)bytes[at] | (uint)bytes[at + 1] << 8 | (uint)bytes[at + 2] << 16 | (uint)bytes[at + 3] << 24; at += 4; return v; }
            public double Double()
            { Require(8); long bits = 0; for (int n = 0; n < 8; n++) bits |= (long)bytes[at + n] << (n * 8); at += 8; return BitConverter.Int64BitsToDouble(bits); }
            public string Text(int maximum)
            {
                int length = UInt16();
                if (length > maximum) throw new InvalidDataException("A capture metadata string exceeds its byte limit.");
                Require(length); string value = Utf8.GetString(bytes, at, length); at += length;
                foreach (char c in value) if (char.IsControl(c)) throw new InvalidDataException("Capture metadata contains control characters.");
                return value;
            }
        }
    }
}
