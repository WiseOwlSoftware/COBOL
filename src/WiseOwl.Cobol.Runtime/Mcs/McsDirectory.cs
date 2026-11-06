// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;

namespace WiseOwl.Cobol.Runtime.Mcs;

/// <summary>
/// ⛔ THE MCS DIRECTORY, resolved once for both messaging facilities (docs/rearchitecture/DESIGN-message-control.md §6). <see cref="Resolve"/>
/// names it: the configured directory, else the environment variable <c>WISEOWL_COBOL_MCS</c>, else the current directory, made absolute
/// with <see cref="System.IO.Path.GetFullPath(string)"/>; files open at <see cref="Path"/>. <see cref="Canonical"/> resolves every symbolic
/// link on the path (walking from the root, each existing component through <see cref="FileSystemInfo.ResolveLinkTarget"/>) and, on Windows,
/// upper-cases the result with <see cref="string.ToUpperInvariant"/> because Windows file systems compare names without regard to case by
/// default, so two spellings of one directory name one MCS. <see cref="Identity"/> (<c>&lt;h&gt;</c>) is the first 16 lower-case hex digits of the SHA-256 of
/// the canonical form's UTF-8 bytes. The walk is lexical for <c>..</c> (<see cref="System.IO.Path.GetFullPath(string)"/> removes it first), and the canonical
/// form and the identity are computed on first use, so a caller that only opens files (the communication facility) pays no walk.
/// </summary>
internal sealed class McsDirectory
{
    /// <summary>The environment variable that names the MCS directory.</summary>
    internal const string Variable = "WISEOWL_COBOL_MCS";

    /// <summary>The most links one path may chain through before it is refused as a loop (Linux's own limit is 40).</summary>
    private const int LinkLimit = 40;

    private readonly Lazy<string> _canonical;
    private readonly Lazy<string> _identity;

    private McsDirectory(string path)
    {
        Path = path;
        _canonical = new Lazy<string>(() => CanonicalOf(path, OperatingSystem.IsWindows()));
        _identity = new Lazy<string>(() => IdentityOf(_canonical.Value));
    }

    /// <summary>The absolute directory the MCS's files open in.</summary>
    internal string Path { get; }

    /// <summary>The path with every symbolic link resolved (and, on Windows, upper-cased): the text the identity hashes.</summary>
    internal string Canonical => _canonical.Value;

    /// <summary>The MCS identity <c>&lt;h&gt;</c>: 16 lower-case hex digits.</summary>
    internal string Identity => _identity.Value;

    /// <summary>The MCS directory: <paramref name="configured"/>, else <c>WISEOWL_COBOL_MCS</c>, else the current directory. An empty
    /// string names nothing, as an unset variable does.</summary>
    internal static McsDirectory Resolve(string? configured)
    {
        string named = configured is { Length: > 0 } ? configured
            : Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } fromEnvironment ? fromEnvironment
            : Directory.GetCurrentDirectory();
        return new McsDirectory(System.IO.Path.GetFullPath(named));
    }

    /// <summary>The first 16 lower-case hex digits of the SHA-256 of <paramref name="canonical"/>'s UTF-8 bytes.</summary>
    internal static string IdentityOf(string canonical) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)).AsSpan(0, 8));

    /// <summary>The canonical form of an absolute path: components that exist are resolved through their links (a link's target is itself walked,
    /// so a link in the middle of the target resolves too); components that do not exist are kept as written.</summary>
    /// <exception cref="IOException">More than <see cref="LinkLimit"/> links chain: a loop.</exception>
    internal static string CanonicalOf(string absolute, bool upperCase)
    {
        string resolved = Walk(absolute, links: 0);
        return upperCase ? resolved.ToUpperInvariant() : resolved;
    }

    private static string Walk(string absolute, int links)
    {
        string root = System.IO.Path.GetPathRoot(absolute) ?? "";
        string current = root;
        string[] parts = absolute[root.Length..].Split(
            [System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string next = System.IO.Path.Combine(current, part);
            FileSystemInfo info = Directory.Exists(next) ? new DirectoryInfo(next) : new FileInfo(next);
            if (info.Exists && info.LinkTarget is not null)
            {
                if (++links > LinkLimit) throw new IOException($"more than {LinkLimit} symbolic links in '{absolute}'");
                string target = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                    ?? throw new IOException($"the link '{next}' has no target");
                next = Walk(System.IO.Path.GetFullPath(target), links);
            }
            current = next;
        }
        return current;
    }
}
