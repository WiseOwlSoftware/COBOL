// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// The <c>USAGE NATIONAL</c> storage-image codec: a national item's characters as the bytes its storage holds, and back
/// (kb/Work PB2646: these members were in <see cref="CobolBits"/>, the USAGE BIT codec). WiseOwl COBOL represents a
/// national character position as one UTF-16 code unit in <see cref="BytesPerNational"/> bytes, high-order byte first
/// (determinations D-N1/D-N3; ISO §2023:13.18.60.4 GR8 leaves the size and representation to the implementor). A byte
/// image is a string of one character per byte under the storage-byte law (<see cref="StorageByte"/>), the convention
/// every byte-form codec of the runtime shares.
/// </summary>
public static class NationalImage
{
    /// <summary>The UTF-16BE byte serialization of a NATIONAL string — one char per BYTE, two bytes (high-order
    /// first) per national position (D-N1: national is UTF-16, one code unit per character position). ⛔ THE ONE
    /// national→bytes reduction: <c>CobolIntrinsics.Convert</c>'s NAT source arm and the compiler-emitted CONVERT
    /// ANY raw-storage channel (a national leaf's storage bits, §2023:15.19.3 AR7) both ride it — never a second
    /// serializer (fix-queue PB59 family 5b). The char==byte string convention is <see cref="CobolBits.Pack"/>'s, and every byte
    /// becomes a character through <see cref="StorageByte"/>'s law.</summary>
    public static string Bytes(string s)
    {
        var image = new char[s.Length * BytesPerNational];
        for (int i = 0; i < s.Length; i++) Put(image, BytesPerNational * i, s[i]);
        return new string(image);
    }

    /// <summary>⛔ THE ONE national byte-order law, write side: the high-order byte of a national character
    /// position occupies the LOWER-addressed byte (UTF-16 <b>BE</b>, D-N1). Shared by <see cref="Bytes"/> (into
    /// a fresh image) and <see cref="WriteWindow"/> (into a copy of an existing one), exactly as
    /// the USAGE BIT codec's one bit-order law is shared by <see cref="CobolBits.Pack"/> and <see cref="CobolBits.WriteWindow"/>, so the two cannot
    /// disagree about which byte comes first.</summary>
    private static void Put(char[] buf, int at, char c)
    {
        buf[at] = StorageByte.ToChar((byte)(c >> 8));
        buf[at + 1] = StorageByte.ToChar((byte)c);
    }

    /// <summary>Bytes per national character position — ISO §2023:13.18.60.4 GR8 leaves the size to the implementor
    /// ("National characters shall be represented in the storage of the computer as characters of a uniform size
    /// equal to or a multiple of the size of characters in the computer's alphanumeric character set. Each
    /// implementor shall specify the size and representation of characters stored for usage NATIONAL") and
    /// WiseOwl COBOL pins <b>2</b> — determination D-N1/D-N3, and the answer <c>FUNCTION BYTE-LENGTH</c>, CONVERT's
    /// raw-storage channel and CONFORMANCE.md items 33/188 already give.</summary>
    public const int BytesPerNational = 2;

    /// <summary>⛔ THE INVERSE OF <see cref="Bytes"/>: decode <paramref name="positions"/> national character
    /// positions from the UTF-16BE byte pairs at <paramref name="startByte"/> of a byte image (char==byte). A
    /// pair that runs past the end of the image decodes as the national SPACE — the same benign short-window
    /// decode <see cref="CobolBits.ReadWindow"/> gives a bit run, never a throw (a shared storage area may legitimately be
    /// shorter than a view over it, §2023:13.18.44.4 GR1's larger-of-the-two allocation notwithstanding, and a BASED
    /// window over a hand-sized ALLOCATE is the reachable case).
    /// <para>⛔ A position holding a character with no byte image (an alphanumeric item may hold any UTF-16 code unit,
    /// DOC-A.1-31) reads through <see cref="StorageByte.ImageByte"/>: the byte is <c>?</c> and EC-DATA-CONVERSION is
    /// set (§2023:8.1.2 1: every character of the alphanumeric coded character set occupies the same number of bytes, so
    /// the storage image of one that cannot is decided once, DOC-A.1-33) — never the code unit's truncated low-order
    /// byte, which turned U+20AC into U+00AC through a national REDEFINES view (kb/Work PB547).</para></summary>
    public static string ReadWindow(string image, int startByte, int positions)
    {
        var chars = new char[positions];
        for (int i = 0; i < positions; i++)
        {
            int hi = startByte + BytesPerNational * i;
            chars[i] = hi >= 0 && hi + 1 < image.Length
                ? (char)((StorageByte.ImageByte(image[hi]) << 8) | StorageByte.ImageByte(image[hi + 1]))
                : ' ';
        }
        return new string(chars);
    }

    /// <summary>The receiving twin of <see cref="ReadWindow"/>: splice <paramref name="value"/>, fitted to
    /// exactly <paramref name="positions"/> national positions, into <paramref name="image"/> as UTF-16BE byte
    /// pairs starting at <paramref name="startByte"/>, leaving every other byte of the image untouched (ISO
    /// §2023:13.18.44.4 GR1 — the shared area is one storage area and a write through one view must not disturb the
    /// bytes of another). Fitting happens in POSITIONS, before serialization: padding the BYTES with 0x20 would
    /// manufacture U+2020 national characters instead of national spaces.</summary>
    public static string WriteWindow(string image, int startByte, int positions, string value)
    {
        var buf = image.ToCharArray();
        for (int i = 0; i < positions; i++)
        {
            int hi = startByte + BytesPerNational * i;
            if (hi < 0) continue;
            if (hi + 1 >= buf.Length) break;
            Put(buf, hi, i < value.Length ? value[i] : ' ');
        }
        return new string(buf);
    }

    /// <summary>The national CONTENT a whole storage image becomes in a receiver whose length is the content's — a
    /// dynamic-length national item (ISO §2023:8.5.1.10.4: "The new length of the dynamic-length elementary item is
    /// determined by the length of new content"). The image's UTF-16BE pairs decode as <see cref="ReadWindow"/>
    /// decodes them; an ODD byte count cannot be whole national content, so its last position is completed with the
    /// alphanumeric space, the fill §2023:14.9.25.4 GR4's elementary-move rules supply and the fixed national receiver
    /// already takes (design D-N8, kb/Work PB546): the three bytes "ABC" become two positions, U+4142 and U+4320.</summary>
    public static string FromStorage(string image)
    {
        var chars = new char[(image.Length + BytesPerNational - 1) / BytesPerNational];
        for (int i = 0; i < chars.Length; i++)
        {
            int hi = BytesPerNational * i;
            byte lo = hi + 1 < image.Length ? StorageByte.ImageByte(image[hi + 1]) : StorageByte.ToByte(' ');
            chars[i] = (char)((StorageByte.ImageByte(image[hi]) << 8) | lo);
        }
        return new string(chars);
    }
}
