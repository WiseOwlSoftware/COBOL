// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE ONE PLACE A TEST MAKES A RUNTIME VERSION OF ANOTHER MAJOR. A run unit refuses a module whose recorded runtime
/// MAJOR differs from its own, in either direction (<see cref="RuntimeAbi.Skew"/>), so a skew witness needs a version
/// text whose major is not the running runtime's, for EVERY runtime major the project will ship.
/// </summary>
/// <remarks>
/// <para>The skewed text keeps the length of the recorded one: a witness may patch it over the text a compiled module
/// recorded in its user-string heap, whose entries are length-prefixed and so cannot grow or shrink. The next major
/// keeps the digit count except at an all-nines major, where the previous one does (9 → 8, 99 → 98).</para>
/// <para>The earlier shape had each witness build <c>Major + 1</c> itself. When the kb/Work PB3339 branch raised the
/// runtime to major 9, the patched text "10.0.0.0" came out longer than the recorded "9.0.0.0" and the byte-patch
/// witness failed in its own setup at the landing's Linux gate; the witnesses that compile a fresh string never
/// noticed.</para>
/// </remarks>
internal static class RuntimeVersionSkew
{
    /// <summary>The running runtime's version text, as a compiled module records it.</summary>
    public static string Current { get; } = RuntimeAbi.Version.ToString();

    /// <summary><see cref="Current"/> with its major replaced by another of the same digit count.</summary>
    public static string OtherMajor { get; } = OtherMajorOf(Current);

    /// <summary><paramref name="versionText"/> with its major replaced by the next major, or by the previous one when
    /// the next would gain a digit; every other component is kept, so the text keeps its length.</summary>
    public static string OtherMajorOf(string versionText)
    {
        int dot = versionText.IndexOf('.');
        string majorText = dot < 0 ? versionText : versionText[..dot];
        int major = int.Parse(majorText, NumberStyles.None, CultureInfo.InvariantCulture);
        string next = (major + 1).ToString(CultureInfo.InvariantCulture);
        string other = next.Length == majorText.Length ? next : (major - 1).ToString(CultureInfo.InvariantCulture);
        return other + versionText[majorText.Length..];
    }
}
