// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// A user-defined CLASS condition from SPECIAL-NAMES.
/// CLASS class-name IS literal [THRU literal] [, literal [THRU literal]]...
/// Each entry in ValidBytes is a set of byte values that the class accepts.
/// </summary>
public sealed class ClassDefinition
{
    public string Name { get; }

    /// <summary>
    /// Pre-computed set of valid byte values for this class.
    /// At runtime, a field IS class-name iff every byte is in this set.
    /// </summary>
    public byte[] ValidBytes { get; }

    public ClassDefinition(string name, byte[] validBytes)
    {
        Name = name;
        ValidBytes = validBytes;
    }
}

/// <summary>
/// An ALPHABET definition from SPECIAL-NAMES.
/// Maps alphabet-name to either a predefined alphabet or a custom collating sequence.
/// </summary>
public sealed class AlphabetDefinition
{
    public string Name { get; }

    /// <summary>
    /// The collating sequence: a 256-byte array where index = native character ordinal,
    /// value = collating weight. For STANDARD-1 / NATIVE this is identity.
    /// </summary>
    public byte[] CollatingSequence { get; }

    public AlphabetDefinition(string name, byte[] collatingSequence)
    {
        Name = name;
        CollatingSequence = collatingSequence;
    }

    /// <summary>Build the default (native/identity) collating sequence.</summary>
    public static byte[] NativeCollatingSequence()
    {
        var seq = new byte[256];
        for (int i = 0; i < 256; i++) seq[i] = (byte)i;
        return seq;
    }
}
