// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WiseOwl.Cobol.Runtime.Mcs;

/// <summary>A socket directory the resolution refuses (a mode that grants group or others anything, or one it cannot create). Each facility
/// decides what that means: EC-MCS-IMP for 2023, "No bell" for the communication facility.</summary>
internal sealed class McsSocketDirectoryRefusedException(string message, Exception? inner = null) : IOException(message, inner);

/// <summary>
/// ⛔ THE SOCKET DIRECTORY <c>&lt;S&gt;</c>, resolved once for both messaging facilities (docs/rearchitecture/DESIGN-message-control.md §6).
/// Sockets stay out of the MCS directory because an AF_UNIX path is short and an MCS directory can be arbitrarily deep.
/// <para>On Unix <c>&lt;S&gt;</c> is <c>&lt;base&gt;/wiseowl-mcs-&lt;uid&gt;/&lt;h&gt;/</c>: <c>&lt;uid&gt;</c> the effective user id in decimal, <c>&lt;base&gt;</c>
/// <c>$XDG_RUNTIME_DIR</c> when it is set, absolute, exists and leaves room for the longest entry of either facility
/// (<see cref="LongestEntryBytes"/>), else <c>/tmp</c> (never <see cref="Path.GetTempPath"/>, which on macOS is a per-user directory too long
/// for the limit). On Windows it is <see cref="Path.GetTempPath"/> followed by <c>wiseowl-mcs\&lt;h&gt;\</c>, inside the user's own temp directory.
/// On Unix every directory created is mode 0700 and one that already exists with any group or other permission is refused
/// (<see cref="McsSocketDirectoryRefusedException"/>): a directory this user can create entries in whose mode grants nothing to anyone
/// else is this user's, so no owner test is needed, and a directory another user made first denies access and is refused the same way.
/// No setting names another socket directory, so two users never share an MCS's sockets.</para>
/// </summary>
internal sealed class McsSocketDirectory
{
    /// <summary>The 2023 facility's longest entry under <c>&lt;S&gt;</c>: <c>p/</c> (2), <c>&lt;N&gt;/</c> (at most 15) and <c>&lt;r&gt;</c> (16) (§5.1).</summary>
    internal const int LongestEntryBytes = 33;

    /// <summary>The freedesktop variable that names a Unix user's runtime directory, the preferred base of <c>&lt;S&gt;</c>.</summary>
    internal const string RuntimeDirectoryVariable = "XDG_RUNTIME_DIR";

    private const string UnixFolder = "wiseowl-mcs-", WindowsFolder = "wiseowl-mcs";

    private McsSocketDirectory(string path) => Path = path;

    /// <summary>The longest AF_UNIX path <c>Bind</c> accepts, without its NUL: Linux's <c>sun_path</c> "is 108 bytes in size" (unix(7)) and macOS's is
    /// <c>char sun_path[104]</c> (xnu <c>bsd/sys/un.h</c>), each counting the NUL; Windows' 107 is what <c>PlatformAssumptionTests</c> (slice S0, PB4739)
    /// measures, and that class asserts this constant against its measurement on every host that runs it.</summary>
    internal static int MaxPathBytes { get; } = OperatingSystem.IsMacOS() ? 103 : 107;

    /// <summary>The socket directory, ending in the separator.</summary>
    internal string Path { get; }

    /// <summary>Whether <c>&lt;S&gt;</c> plus an entry of <paramref name="tailBytes"/> bytes is within <see cref="MaxPathBytes"/> (UTF-8 bytes).</summary>
    internal bool Fits(int tailBytes) => FitsUnder(Path, tailBytes);

    /// <summary>The run id <c>&lt;r&gt;</c>: 16 random lower-case hex digits. Each facility draws its own.</summary>
    internal static string NewRunId() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));

    /// <summary>The socket directory of the MCS whose identity is <paramref name="identity"/> (<see cref="McsDirectory.Identity"/>), created if it is absent.</summary>
    /// <exception cref="McsSocketDirectoryRefusedException">A directory exists with a mode open to group or others, or cannot be created.</exception>
    internal static McsSocketDirectory Resolve(string identity)
    {
        if (OperatingSystem.IsWindows())
        {
            string windows = System.IO.Path.GetTempPath() + WindowsFolder + "\\" + identity + "\\";
            return new McsSocketDirectory(Create(windows));
        }
        string? runtime = Environment.GetEnvironmentVariable(RuntimeDirectoryVariable);
        uint uid = EffectiveUserId();
        string path = UnixDirectory(runtime is not null && Directory.Exists(runtime) ? runtime : null, uid, identity);
        return new McsSocketDirectory(Create(path));
    }

    /// <summary>The Unix socket directory: under <paramref name="runtimeDirectory"/> (<c>$XDG_RUNTIME_DIR</c>) when it is absolute and the longest entry
    /// still fits under it, else under <c>/tmp</c>.</summary>
    internal static string UnixDirectory(string? runtimeDirectory, uint uid, string identity)
    {
        string tail = UnixFolder + uid.ToString(CultureInfo.InvariantCulture) + "/" + identity + "/";
        if (runtimeDirectory is { Length: > 0 } && runtimeDirectory[0] == '/')
        {
            string underRuntime = runtimeDirectory.TrimEnd('/') + "/" + tail;
            if (FitsUnder(underRuntime, LongestEntryBytes)) return underRuntime;
        }
        return "/tmp/" + tail;
    }

    private static bool FitsUnder(string directory, int tailBytes) => Encoding.UTF8.GetByteCount(directory) + tailBytes <= MaxPathBytes;

    /// <summary>Creates the two levels below the base (each 0700 on Unix) and checks the mode of each, whether this call or another made it.</summary>
    private static string Create(string directory)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(directory);
                return directory;
            }
            string trimmed = directory.TrimEnd('/');
            string identityLevel = trimmed;
            string userLevel = trimmed[..trimmed.LastIndexOf('/')];
            foreach (string level in new[] { userLevel, identityLevel })
            {
                Directory.CreateDirectory(level, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                UnixFileMode mode = File.GetUnixFileMode(level);
                if ((mode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                             | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
                    throw new McsSocketDirectoryRefusedException($"the socket directory '{level}' has mode {Convert.ToString((int)mode, 8)}, which grants access beyond its user");
            }
            return directory;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException && e is not McsSocketDirectoryRefusedException)
        {
            throw new McsSocketDirectoryRefusedException($"the socket directory '{directory}' cannot be used: {e.Message}", e);
        }
    }

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint NativeGeteuid();

    /// <summary>The effective user id; Unix only.</summary>
    private static uint EffectiveUserId() => NativeGeteuid();
}
