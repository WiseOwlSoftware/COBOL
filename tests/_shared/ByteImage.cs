// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// THE ONE LOOP THAT PATCHES A COMPILED MODULE'S BYTES (kb/Work PB4470, PB4460). A fixture that damages a library to make a
/// refusal witness (a runtime version of another major, a renamed attribute argument, a redirected redefinition, a broken index
/// row) replaces every occurrence of one byte string by another of the SAME length, so every offset of the assembly stays and
/// the metadata still parses. <see cref="RuntimeVersionSkew.Patch"/> and the repository witnesses all call
/// <see cref="PatchFile"/>; a fourth copy of the loop is the shape this class exists to prevent.
/// </summary>
internal static class ByteImage
{
    /// <summary>Replaces every non-overlapping occurrence of <paramref name="from"/> in <paramref name="image"/> by
    /// <paramref name="to"/>, in place, scanning forward so a replacement is never searched again. Returns the number replaced;
    /// a caller asserts it is not zero, because zero means the module holds the text nowhere and the fixture damaged nothing.</summary>
    public static int ReplaceEvery(byte[] image, ReadOnlySpan<byte> from, ReadOnlySpan<byte> to)
    {
        if (from.Length == 0 || from.Length != to.Length)
            throw new ArgumentException($"a same-length patch needs equal, non-empty byte strings; got {from.Length} and {to.Length}");
        if (from.SequenceEqual(to))
            throw new ArgumentException("the patch changes nothing");
        int replaced = 0;
        for (int start = 0; start <= image.Length - from.Length;)
        {
            int at = image.AsSpan(start).IndexOf(from);
            if (at < 0) break;
            to.CopyTo(image.AsSpan(start + at));
            replaced++;
            start += at + from.Length;
        }
        return replaced;
    }

    /// <summary><see cref="ReplaceEvery"/> over the file at <paramref name="path"/>, written back only when something changed.
    /// Returns the number replaced.</summary>
    public static int PatchFile(string path, ReadOnlySpan<byte> from, ReadOnlySpan<byte> to)
    {
        byte[] image = File.ReadAllBytes(path);
        int replaced = ReplaceEvery(image, from, to);
        if (replaced > 0) File.WriteAllBytes(path, image);
        return replaced;
    }
}
