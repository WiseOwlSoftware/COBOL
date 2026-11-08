// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
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
/// for the limit). On Windows it is <see cref="Path.GetTempPath"/> followed by <c>wiseowl-mcs\&lt;h&gt;\</c>, inside the user's own temp directory, when the longest
/// entry fits there, else <c>%ProgramData%\wiseowl-mcs-&lt;u&gt;\&lt;h&gt;\</c> with a user level only the user may use (<see cref="WindowsDirectory"/>, kb/Work PB4922).
/// On Unix every directory created is mode 0700 and one that already exists with any group or other permission is refused
/// (<see cref="McsSocketDirectoryRefusedException"/>): a directory this user can create entries in whose mode grants nothing to anyone
/// else is this user's, so no owner test is needed, and a directory another user made first denies access and is refused the same way.
/// No setting names another socket directory, so two users never share an MCS's sockets.</para>
/// </summary>
internal sealed class McsSocketDirectory
{
    /// <summary>The 2023 facility's longest entry under <c>&lt;S&gt;</c>: <c>p/</c> (2), <c>&lt;N&gt;/</c> (at most 15) and <c>&lt;r&gt;</c> (16) (§2023:5.1).</summary>
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
            var (windows, userLevel) = WindowsDirectory(System.IO.Path.GetTempPath(), Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                WindowsUserKey(), identity);
            if (userLevel is not null) SecureWindowsUserLevel(userLevel);
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

    /// <summary>⛔ THE WINDOWS SOCKET DIRECTORY, THE TWIN OF <see cref="UnixDirectory"/> (kb/Work PB4922; DESIGN-message-control.md §5.1, §6): under
    /// <paramref name="tempPath"/> (<see cref="System.IO.Path.GetTempPath"/>, which ends in its separator, a directory the user's own profile already restricts) when the
    /// longest entry of either facility still fits under it, else under <paramref name="commonData"/> (<c>%ProgramData%</c>, short and the same for every user) in
    /// <c>wiseowl-mcs-&lt;u&gt;</c>, <c>&lt;u&gt;</c> the user's key (<see cref="WindowsUserKey"/>). The second result is that user level, which the caller restricts to the
    /// user (<see cref="SecureWindowsUserLevel"/>), as the Unix user level is mode 0700; null under the temp directory. A user name longer than 16 characters makes the
    /// default temp path too long (91 bytes plus the name against 107), and that user's messaging was EC-MCS-IMP.</summary>
    internal static (string Path, string? UserLevel) WindowsDirectory(string tempPath, string commonData, string userKey, string identity)
    {
        string underTemp = tempPath + WindowsFolder + "\\" + identity + "\\";
        if (FitsUnder(underTemp, LongestEntryBytes) || commonData.Length == 0) return (underTemp, null);
        string userLevel = commonData.TrimEnd('\\') + "\\" + UnixFolder + userKey;
        return (userLevel + "\\" + identity + "\\", userLevel);
    }

    /// <summary>The Windows user's key <c>&lt;u&gt;</c>: the first 16 lower-case hex digits of the SHA-256 of the user's SID string (a SID is up to 184 characters, so it
    /// is hashed to a fixed width as the MCS identity <c>&lt;h&gt;</c> is). Windows only.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static string WindowsUserKey() =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(WindowsIdentity.GetCurrent().User!.Value)))[..16];

    /// <summary>⛔ THE WINDOWS USER LEVEL IS THE USER'S ALONE (kb/Work PB4922), as the Unix one is mode 0700: <c>%ProgramData%</c> lets every user create a directory, so
    /// the level is created owned by the user with a PROTECTED access list that grants the user alone full control (nothing inherited from <c>%ProgramData%</c>), and
    /// one that already exists is refused (<see cref="McsSocketDirectoryRefusedException"/>) unless the user owns it, its list is protected, and it allows no one else
    /// anything. Another user can therefore deny a user the fallback by making the directory first, but never read or plant a socket in it: the Unix posture under
    /// <c>/tmp</c>. The sockets and directories beneath inherit the list.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static void SecureWindowsUserLevel(string userLevel)
    {
        var user = WindowsIdentity.GetCurrent().User!;
        var info = new DirectoryInfo(userLevel);
        try
        {
            if (!info.Exists)
            {
                var security = new DirectorySecurity();
                security.SetOwner(user);
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                info.Create(security);
            }
            var acl = info.GetAccessControl();
            if (acl.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner || owner != user)
                throw new McsSocketDirectoryRefusedException($"the socket directory '{userLevel}' is not owned by this user");
            if (!acl.AreAccessRulesProtected)
                throw new McsSocketDirectoryRefusedException($"the socket directory '{userLevel}' inherits access from its parent");
            foreach (FileSystemAccessRule rule in acl.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && rule.IdentityReference != user)
                    throw new McsSocketDirectoryRefusedException($"the socket directory '{userLevel}' grants access to {rule.IdentityReference}, beyond its user");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException && e is not McsSocketDirectoryRefusedException)
        {
            throw new McsSocketDirectoryRefusedException($"the socket directory '{userLevel}' cannot be used: {e.Message}", e);
        }
    }

    /// <summary>The socket directory at <paramref name="path"/> as it stands: nothing is created or checked. For a caller that has already formed a path by
    /// <see cref="UnixDirectory"/> or <see cref="WindowsDirectory"/> and asks the budget of it; no setting reaches this, so a user still cannot name another directory.</summary>
    internal static McsSocketDirectory Under(string path) => new(path);

    /// <summary>⛔ THE ONE PLACE AN AF_UNIX PATH BECOMES AN ENDPOINT (DESIGN-message-control.md §6 "The budget"; <c>AfUnixEndPointDriftTests</c> holds the runtime to it).
    /// .NET's constructor answers a path it cannot hold with an <see cref="ArgumentOutOfRangeException"/>; a path comes from the host's temp directory or from
    /// <c>WISEOWL_COBOL_MCS_NOTIFY</c>, so that is a failure of the run unit's environment, never of the program, and it is an <see cref="IOException"/> naming the path and the limit.
    /// Ask before making the socket, so a path refused leaves nothing to dispose. A NUL is refused too: a leading one names a Linux abstract socket, which is no file of the socket directory.</summary>
    internal static UnixDomainSocketEndPoint EndPointOf(string path)
    {
        int bytes = Encoding.UTF8.GetByteCount(path);
        if (bytes == 0 || bytes > MaxPathBytes || path.Contains('\0'))
            throw new IOException($"'{path}' cannot be an AF_UNIX path: it is {bytes} bytes, and such a path holds 1 to {MaxPathBytes} bytes and no NUL");
        return new UnixDomainSocketEndPoint(path);
    }

    private static bool FitsUnder(string directory, int tailBytes) => Encoding.UTF8.GetByteCount(directory) + tailBytes <= MaxPathBytes;

    /// <summary>Creates the two levels below the base (each 0700 on Unix) and checks the mode of each, whether this call or another made it.</summary>
    internal static string Create(string directory)
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
