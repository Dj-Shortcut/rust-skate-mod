using System;
using System.IO;

// Runtime-neutral WAV reader. It does not download, synthesize, repair or license audio.
// ElevenLabs downloads/raw files remain private ignored artifacts, not CC0 content.
public static class SkateSoundPack
{
    public const int Count = 18, Variations = 1, Rate = 44100;
    private const int MinimumSamples = Rate / 50, MaximumSamples = Rate * 6;
    private const int MaximumFileBytes = MaximumSamples * 2 + 65536 + 44;
    private const int MaximumChunks = 128, BlockSamples = 32768;
    private static readonly string[] stems = {
        "skate_rolling_smooth", "skate_rolling_rough", "skate_rolling_wood",
        "skate_grit", "skate_whirr", "skate_push", "skate_ollie",
        "skate_landing", "skate_drop", "skate_seam", "skate_grind_in",
        "skate_grind", "skate_grind_out", "skate_slide_in", "skate_deckslide",
        "skate_scrape", "skate_slam", "skate_bail"
    };
    private static void Index(int index)
    {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index");
    }

    public static string Stem(int index) { Index(index); return stems[index]; }

    public static bool IsLoop(int index)
    {
        Index(index);
        return index < 5 || index == 11 || index == 14;
    }

    // Compatibility hooks retain the prior caller API. One selected recording is
    // intentional: no artificial variations/copies or nonrepeat claim are made.
    public static void ResetVariants(uint seed) { }

    public static int NextVariant(int index)
    {
        Index(index);
        return 1;
    }

    public static bool TryOpen(string directory, int index, int variation,
        out Reader reader, out string error)
    {
        reader = null; error = null;
        if (index < 0 || index >= Count) { error = "Invalid sound index"; return false; }
        if (variation != 1) { error = "Only the selected _01 sound is supported"; return false; }
        if (string.IsNullOrWhiteSpace(directory)) { error = "Sound directory is empty"; return false; }
        var leaf = stems[index] + "_0" + variation + ".wav";
        FileStream stream = null;
        try
        {
            // Only catalogue-derived leaf names are ever combined with the supplied root.
            stream = new FileStream(Path.Combine(directory, leaf), FileMode.Open,
                FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            var length = stream.Length;
            if (length < 44 || length > MaximumFileBytes) throw Invalid("WAV file length is outside its bound");
            if (U32(stream) != 0x46464952u) throw Invalid("Expected little-endian RIFF");
            if ((long)U32(stream) + 8 != length) throw Invalid("RIFF length does not match the file");
            if (U32(stream) != 0x45564157u) throw Invalid("Expected WAVE form");
            var formatFound = false; var dataFound = false; var chunks = 0;
            long dataAt = 0; uint dataBytes = 0;
            while (stream.Position < length)
            {
                if (++chunks > MaximumChunks) throw Invalid("Too many RIFF chunks");
                if (length - stream.Position < 8) throw Invalid("Truncated RIFF chunk header");
                var tag = U32(stream); var size = U32(stream); var start = stream.Position;
                if ((long)size > length - start) throw Invalid("RIFF chunk extends past the file");
                var next = start + size + (size & 1u);
                if (next > length) throw Invalid("Missing odd-chunk padding");
                if (tag == 0x20746d66u)
                {
                    if (formatFound) throw Invalid("Duplicate fmt chunk");
                    formatFound = true;
                    if (size != 16 && size != 18) throw Invalid("Unsupported PCM fmt chunk size");
                    var format = U16(stream); var channels = U16(stream);
                    var rate = U32(stream); var byteRate = U32(stream);
                    var alignment = U16(stream); var bits = U16(stream);
                    if (format != 1 || channels != 1 || rate != Rate ||
                        byteRate != Rate * 2 || alignment != 2 || bits != 16)
                        throw Invalid("Expected PCM16 mono at 44100 Hz");
                    if (size == 18 && U16(stream) != 0) throw Invalid("Unexpected PCM format extension");
                }
                else if (tag == 0x61746164u)
                {
                    if (dataFound) throw Invalid("Duplicate data chunk");
                    dataFound = true; dataAt = start; dataBytes = size;
                    if ((size & 1u) != 0) throw Invalid("PCM16 data length is not sample-aligned");
                }
                stream.Position = next;
            }
            if (!formatFound || !dataFound) throw Invalid("Missing fmt or data chunk");
            var sampleCount = (long)dataBytes / 2;
            var maximum = Rate * (IsLoop(index) ? 4 : 6);
            if (sampleCount < MinimumSamples || sampleCount > maximum)
                throw Invalid("PCM duration is outside the sound bound");
            stream.Position = dataAt;
            reader = new Reader(stream, (int)sampleCount, length);
            stream = null;
            return true;
        }
        catch (Exception e)
        {
            if (!(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException ||
                e is ArgumentException || e is NotSupportedException ||
                e is System.Security.SecurityException)) throw;
            error = leaf + ": " + (e is InvalidDataException ? e.Message : e.GetType().Name);
            return false;
        }
        finally { if (stream != null) stream.Dispose(); }
    }

    private static InvalidDataException Invalid(string message) { return new InvalidDataException(message); }

    private static int Byte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0) throw Invalid("Truncated WAV header");
        return value;
    }
    private static ushort U16(Stream stream) { return (ushort)(Byte(stream) | Byte(stream) << 8); }
    private static uint U32(Stream stream) { return (uint)(U16(stream) | (uint)U16(stream) << 16); }

    public sealed class Reader : IDisposable
    {
        private FileStream stream;
        private byte[] bytes;
        private readonly long fileLength;
        private int remaining;
        private bool disposed;
        private string failure;
        public int SampleCount { get; private set; }

        internal Reader(FileStream source, int sampleCount, long length)
        {
            stream = source; SampleCount = remaining = sampleCount; fileLength = length;
            bytes = new byte[BlockSamples * 2];
        }

        // true/count==0 means EOF. Only [0,count) is written; the final tail is untouched.
        // The caller must upload exactly count samples, not the full destination array.
        public bool Read(float[] destination, out int count, out string error)
        {
            count = 0; error = null;
            if (disposed) { error = "Sound reader is disposed"; return false; }
            if (failure != null) { error = failure; return false; }
            if (destination == null || destination.Length == 0)
            { error = "A nonempty PCM destination is required"; return false; }
            if (remaining == 0) return true;
            try
            {
                if (stream.Length != fileLength) throw Invalid("WAV file changed during reading");
                var samples = Math.Min(Math.Min(destination.Length, BlockSamples), remaining);
                var wanted = samples * 2; var read = 0;
                while (read < wanted)
                {
                    var got = stream.Read(bytes, read, wanted - read);
                    if (got <= 0) throw Invalid("Truncated PCM data");
                    read += got;
                }
                for (var i = 0; i < samples; i++)
                    destination[i] = unchecked((short)(bytes[i * 2] | bytes[i * 2 + 1] << 8)) / 32768f;
                remaining -= samples; count = samples;
                if (remaining == 0) Close();
                return true;
            }
            catch (Exception e)
            {
                if (!(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is ObjectDisposedException)) throw;
                failure = e is InvalidDataException ? e.Message : e.GetType().Name;
                count = 0; error = failure; Close(); return false;
            }
        }

        private void Close()
        {
            var owned = stream; stream = null; bytes = null;
            if (owned != null) owned.Dispose();
        }

        public void Dispose() { disposed = true; Close(); }
    }
}
