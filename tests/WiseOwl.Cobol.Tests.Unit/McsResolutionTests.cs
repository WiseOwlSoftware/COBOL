// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Runtime.Mcs;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SHARED RESOLUTIONS OF THE TWO MESSAGING FACILITIES (kb/Work PB4735; docs/rearchitecture/DESIGN-message-control.md §6, §7.2 "Unit"):
/// <see cref="McsDirectory"/> and its identity <c>&lt;h&gt;</c>, <see cref="McsSocketDirectory"/> with the run id and the path budget, and
/// <see cref="McsFrame"/>. The Unix-only witnesses (mode 0700, a directory open to group or others refused) run on the Linux leg of CI;
/// the pure string rules (the fallback to <c>/tmp</c>, the budget) run on every host.
/// </summary>
public sealed class McsResolutionTests : IDisposable
{
    private readonly string _work = ScratchDirectory.Create("pb4735-");

    public void Dispose() => ScratchDirectory.Remove(_work);

    private string Make(params string[] parts)
    {
        string path = Path.Combine([_work, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Link(string link, string target) => Directory.CreateSymbolicLink(link, target);

    // ---- McsDirectory ----

    [Fact]
    public void AConfiguredDirectoryIsMadeAbsolute()
    {
        var resolved = McsDirectory.Resolve(Path.Combine("relative", "mcs"));
        Assert.Equal(Path.GetFullPath(Path.Combine("relative", "mcs")), resolved.Path);
        Assert.True(Path.IsPathRooted(resolved.Path));
    }

    [Fact]
    public void TheEnvironmentVariableIsTheSecondChoiceAndTheCurrentDirectoryTheThird()
    {
        string configured = Make("configured"), fromEnvironment = Make("fromEnvironment"), current = Make("current");
        Assert.Equal(Path.GetFullPath(configured), McsDirectory.Resolve(configured, fromEnvironment, current).Path);
        Assert.Equal(Path.GetFullPath(fromEnvironment), McsDirectory.Resolve(null, fromEnvironment, current).Path);
        Assert.Equal(Path.GetFullPath(fromEnvironment), McsDirectory.Resolve("", fromEnvironment, current).Path);
        Assert.Equal(Path.GetFullPath(current), McsDirectory.Resolve(null, null, current).Path);
        Assert.Equal(Path.GetFullPath(current), McsDirectory.Resolve(null, "", current).Path);
    }

    [Fact]
    public void AnEmptyConfiguredNameNamesNothingAndTheDefaultIsAbsolute()
    {
        Assert.True(Path.IsPathRooted(McsDirectory.Resolve(null).Path));
        Assert.Equal(McsDirectory.Resolve(null).Path, McsDirectory.Resolve("").Path);
    }

    [Fact]
    public void TheIdentityIsTheFirstSixteenHexDigitsOfTheSha256OfTheCanonicalFormsUtf8Bytes()
    {
        // FIPS 180-2 appendix B.1: SHA-256("abc") = ba7816bf 8f01cfea 414140de 5dae2223 ...
        Assert.Equal("ba7816bf8f01cfea", McsDirectory.IdentityOf("abc"));
        string identity = McsDirectory.Resolve(_work).Identity;
        Assert.Matches("^[0-9a-f]{16}$", identity);
        Assert.Equal(McsDirectory.IdentityOf(McsDirectory.Resolve(_work).Canonical), identity);
    }

    [Fact]
    public void ACaseVariantNamesOneMcsOnWindowsAndTwoWhereNamesAreCaseSensitive()
    {
        string upper = Path.Combine(_work, "CASEDIR"), lower = Path.Combine(_work, "casedir");
        Assert.Equal(upper.ToUpperInvariant(), McsDirectory.CanonicalOf(lower, upperCase: true));
        Assert.Equal(McsDirectory.CanonicalOf(upper, upperCase: true), McsDirectory.CanonicalOf(lower, upperCase: true));
        Assert.NotEqual(McsDirectory.CanonicalOf(upper, upperCase: false), McsDirectory.CanonicalOf(lower, upperCase: false));
        // The resolution itself folds exactly where the host does (§6): Windows only.
        Assert.Equal(OperatingSystem.IsWindows(),
            McsDirectory.Resolve(upper).Identity == McsDirectory.Resolve(lower).Identity);
    }

    [Fact]
    public void ALinkInAMiddleComponentAndOneInTheFinalComponentNameTheirTargetsMcs()
    {
        string real = Make("real", "inner");
        string middle = Path.Combine(_work, "viaMiddle");
        Link(middle, Path.Combine(_work, "real"));
        string finalLink = Path.Combine(_work, "viaFinal");
        Link(finalLink, real);

        string expected = McsDirectory.Resolve(real).Identity;
        Assert.Equal(expected, McsDirectory.Resolve(Path.Combine(middle, "inner")).Identity);
        Assert.Equal(expected, McsDirectory.Resolve(finalLink).Identity);
        Assert.Equal(McsDirectory.Resolve(real).Canonical, McsDirectory.Resolve(Path.Combine(middle, "inner")).Canonical);
        // A name that does not exist yet is kept as written below the resolved part.
        Assert.Equal(McsDirectory.Resolve(Path.Combine(real, "later")).Canonical, McsDirectory.Resolve(Path.Combine(middle, "inner", "later")).Canonical);
    }

    [Fact]
    public void AChainOfLinksAndALinkWhoseTargetPathHoldsALinkAreFollowedAndALoopIsRefused()
    {
        string real = Make("chainReal");
        string first = Path.Combine(_work, "first");
        string second = Path.Combine(_work, "second");
        Link(first, real);
        Link(second, first);                                        // second -> first -> real
        Assert.Equal(McsDirectory.Resolve(real).Canonical, McsDirectory.Resolve(second).Canonical);

        string leaf = Make("chainReal", "leaf");
        string deep = Path.Combine(_work, "deep");
        Link(deep, Path.Combine(first, "leaf"));                     // the target path itself passes through the link "first"
        Assert.Equal(McsDirectory.Resolve(leaf).Canonical, McsDirectory.Resolve(deep).Canonical);

        string a = Path.Combine(_work, "loopA"), b = Path.Combine(_work, "loopB");
        Link(a, b);
        Link(b, a);
        Assert.ThrowsAny<IOException>(() => McsDirectory.Resolve(a).Canonical);
    }

    // ---- McsSocketDirectory ----

    [Fact]
    public void TheRunIdIsSixteenRandomLowerCaseHexDigits()
    {
        string one = McsSocketDirectory.NewRunId(), two = McsSocketDirectory.NewRunId();
        Assert.Matches("^[0-9a-f]{16}$", one);
        Assert.Matches("^[0-9a-f]{16}$", two);
        Assert.NotEqual(one, two);
    }

    [Fact]
    public void MaxPathBytesIsTheLimitWithoutItsNulOfEachOs()
    {
        Assert.Equal(OperatingSystem.IsMacOS() ? 103 : 107, McsSocketDirectory.MaxPathBytes);
    }

    [Fact]
    public void TheUnixDirectoryIsUnderXdgRuntimeDirWhenTheLongestEntryFitsElseUnderTmp()
    {
        const string h = "0123456789abcdef";
        Assert.Equal("/run/user/1000/wiseowl-mcs-1000/0123456789abcdef/", McsSocketDirectory.UnixDirectory("/run/user/1000", 1000, h));
        Assert.Equal("/run/user/1000/wiseowl-mcs-1000/0123456789abcdef/", McsSocketDirectory.UnixDirectory("/run/user/1000/", 1000, h));
        Assert.Equal("/tmp/wiseowl-mcs-1000/0123456789abcdef/", McsSocketDirectory.UnixDirectory(null, 1000, h));
        Assert.Equal("/tmp/wiseowl-mcs-1000/0123456789abcdef/", McsSocketDirectory.UnixDirectory("", 1000, h));
        Assert.Equal("/tmp/wiseowl-mcs-1000/0123456789abcdef/", McsSocketDirectory.UnixDirectory("relative/run", 1000, h));

        // The base that leaves exactly the room the longest entry needs is kept; one byte more falls back to /tmp.
        string tail = "/wiseowl-mcs-1000/" + h + "/";
        string exact = "/" + new string('x', McsSocketDirectory.MaxPathBytes - McsSocketDirectory.LongestEntryBytes - tail.Length - 1);
        Assert.Equal(exact + tail, McsSocketDirectory.UnixDirectory(exact, 1000, h));
        Assert.Equal("/tmp" + tail, McsSocketDirectory.UnixDirectory(exact + "x", 1000, h));
    }

    [Fact]
    public void TheWorstCaseOfEachFacilitysEntryFitsUnderEachOsDirectoryWithATenDigitUid()
    {
        const string h = "0123456789abcdef";
        const uint uid = uint.MaxValue;                              // 10 digits
        string tmp = McsSocketDirectory.UnixDirectory(null, uid, h);
        string run = McsSocketDirectory.UnixDirectory("/run/user/" + uid, uid, h);
        Assert.StartsWith("/run/user/", run);                        // 61 bytes + 33 = 94 (DESIGN-message-control.md §5.1): no fallback
        foreach (string directory in new[] { tmp, run })
        {
            int s = Encoding.UTF8.GetByteCount(directory);
            Assert.True(s + McsSocketDirectory.LongestEntryBytes <= McsSocketDirectory.MaxPathBytes, directory);
            Assert.True(s + 21 <= McsSocketDirectory.MaxPathBytes, directory);       // the communication facility's bell/<r>
        }
        Assert.True(Encoding.UTF8.GetByteCount(tmp) + 21 <= 103);   // macOS's limit under /tmp (DESIGN-communication-facility.md §6)
    }

    [Fact]
    public void TheWindowsDirectoryStaysUnderATempPathWithRoom_AndFallsBackToTheUsersProgramDataLevelOnOneTooLong()
    {
        // kb/Work PB4922: a temp path one byte too long for the longest entry falls back to %ProgramData%\wiseowl-mcs-<u>\<h>\ (with its user level, which the caller
        // restricts to the user), as Unix falls back from $XDG_RUNTIME_DIR to /tmp; a host with no %ProgramData% keeps the temp directory and its start then fails.
        string h = "0123456789abcdef", u = "fedcba9876543210";
        const string Tail = "wiseowl-mcs\\0123456789abcdef\\";
        string room = new('t', McsSocketDirectory.MaxPathBytes - McsSocketDirectory.LongestEntryBytes - Tail.Length - 1);
        Assert.Equal((room + "\\" + Tail, (string?)null), McsSocketDirectory.WindowsDirectory(room + "\\", @"C:\ProgramData", u, h));
        Assert.True(McsSocketDirectory.Under(McsSocketDirectory.WindowsDirectory(room + "\\", @"C:\ProgramData", u, h).Path).Fits(McsSocketDirectory.LongestEntryBytes));
        Assert.Equal((@"C:\ProgramData\wiseowl-mcs-fedcba9876543210\0123456789abcdef\", @"C:\ProgramData\wiseowl-mcs-fedcba9876543210"),
            McsSocketDirectory.WindowsDirectory(room + "t\\", @"C:\ProgramData\", u, h));
        Assert.True(McsSocketDirectory.Under(McsSocketDirectory.WindowsDirectory(room + "t\\", @"C:\ProgramData", u, h).Path).Fits(McsSocketDirectory.LongestEntryBytes));
        Assert.Equal((room + "t\\" + Tail, (string?)null), McsSocketDirectory.WindowsDirectory(room + "t\\", "", u, h));
        Assert.False(McsSocketDirectory.Under(McsSocketDirectory.WindowsDirectory(room + "t\\", "", u, h).Path).Fits(McsSocketDirectory.LongestEntryBytes));
    }

    [Fact]
    public void TheWindowsUserLevelIsTheUsersAlone_AndOneThatInheritsOrGrantsAnotherIsRefused()
    {
        if (OperatingSystem.IsWindows()) WindowsUserLevelWitness();   // the access-list witness is Windows' (the Unix mode witness is the test below)
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void WindowsUserLevelWitness()
    {
        string level = Path.Combine(_work, "wiseowl-mcs-fedcba9876543210");
        McsSocketDirectory.SecureWindowsUserLevel(level);
        McsSocketDirectory.SecureWindowsUserLevel(level);             // an existing level of the user's is accepted again
        var acl = new DirectoryInfo(level).GetAccessControl();
        var user = System.Security.Principal.WindowsIdentity.GetCurrent().User!;
        Assert.True(acl.AreAccessRulesProtected);
        Assert.Equal(user, acl.GetOwner(typeof(System.Security.Principal.SecurityIdentifier)));
        var rules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)).Cast<System.Security.AccessControl.FileSystemAccessRule>().ToList();
        Assert.Equal(user, Assert.Single(rules).IdentityReference);
        string inheriting = Path.Combine(_work, "wiseowl-mcs-inherits");
        Directory.CreateDirectory(inheriting);                         // made first by someone else's ordinary create: it inherits its parent's list
        Assert.Contains("inherits", Assert.Throws<McsSocketDirectoryRefusedException>(() => McsSocketDirectory.SecureWindowsUserLevel(inheriting)).Message);
        var open = new DirectoryInfo(level).GetAccessControl();
        open.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(
            System.Security.Principal.WellKnownSidType.WorldSid, null), System.Security.AccessControl.FileSystemRights.Read, System.Security.AccessControl.AccessControlType.Allow));
        new DirectoryInfo(level).SetAccessControl(open);
        Assert.Contains("beyond its user", Assert.Throws<McsSocketDirectoryRefusedException>(() => McsSocketDirectory.SecureWindowsUserLevel(level)).Message);
    }

    [Fact]
    public void AnEndPointIsFormedUpToTheLimitInUtf8BytesAndAnythingElseIsAnIoFailure()
    {
        string exact = new('x', McsSocketDirectory.MaxPathBytes);
        Assert.Equal(exact, McsSocketDirectory.EndPointOf(exact).ToString());
        foreach (string refused in new[] { exact + "x", "", "a\0b", "\0abstract", new('\u00e9', McsSocketDirectory.MaxPathBytes / 2 + 1) })   // the last is under the limit in characters, over it in bytes
            Assert.Contains("AF_UNIX", Assert.Throws<IOException>(() => McsSocketDirectory.EndPointOf(refused)).Message);
    }

    [Fact]
    public void ResolveCreatesTheDirectoryAndFitsAsksTheBudget()
    {
        string h = McsSocketDirectory.NewRunId();
        McsSocketDirectory resolved = McsSocketDirectory.Resolve(h);
        try
        {
            Assert.True(Directory.Exists(resolved.Path));
            Assert.EndsWith(h + (OperatingSystem.IsWindows() ? "\\" : "/"), resolved.Path);
            int s = Encoding.UTF8.GetByteCount(resolved.Path);
            Assert.True(resolved.Fits(McsSocketDirectory.MaxPathBytes - s));
            Assert.False(resolved.Fits(McsSocketDirectory.MaxPathBytes - s + 1));
            if (OperatingSystem.IsWindows())
                Assert.StartsWith(Path.GetTempPath() + "wiseowl-mcs\\", resolved.Path);
            else
            {
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(resolved.Path));
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
                    File.GetUnixFileMode(Path.GetDirectoryName(resolved.Path.TrimEnd('/'))!));
            }
        }
        finally { Directory.Delete(resolved.Path); }
    }

    [Fact]
    public void AnExistingDirectoryOpenToGroupOrOthersIsRefused()
    {
        if (OperatingSystem.IsWindows()) return;                     // Windows has no mode to refuse; the Linux leg of CI runs this witness
        string h = McsSocketDirectory.NewRunId();
        McsSocketDirectory resolved = McsSocketDirectory.Resolve(h);
        try
        {
            foreach (UnixFileMode open in new[] { UnixFileMode.GroupRead, UnixFileMode.GroupExecute, UnixFileMode.OtherRead, UnixFileMode.OtherWrite })
            {
                File.SetUnixFileMode(resolved.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | open);
                Assert.Throws<McsSocketDirectoryRefusedException>(() => McsSocketDirectory.Resolve(h));
            }
            File.SetUnixFileMode(resolved.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Assert.Equal(resolved.Path, McsSocketDirectory.Resolve(h).Path);
        }
        finally
        {
            File.SetUnixFileMode(resolved.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Directory.Delete(resolved.Path);
        }
    }

    [Fact]
    public void AnExistingUserLevelOpenToGroupOrOthersIsRefusedWhateverTheIdentityLevelIs()
    {
        if (OperatingSystem.IsWindows()) return;                     // Windows has no mode to refuse; the Linux leg of CI runs this witness
        const UnixFileMode Private = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        string userLevel = Path.Combine(_work, "wiseowl-mcs-1000");
        string directory = userLevel + "/0123456789abcdef/";
        Assert.Equal(directory, McsSocketDirectory.Create(directory));
        foreach (UnixFileMode open in new[] { UnixFileMode.GroupRead, UnixFileMode.GroupExecute, UnixFileMode.OtherRead, UnixFileMode.OtherWrite })
        {
            File.SetUnixFileMode(userLevel, Private | open);
            Assert.Throws<McsSocketDirectoryRefusedException>(() => McsSocketDirectory.Create(directory));
        }
        File.SetUnixFileMode(userLevel, Private);
        Assert.Equal(directory, McsSocketDirectory.Create(directory));
    }

    // ---- McsFrame ----

    [Fact]
    public void ARingFrameIsLengthKindAndNoPayloadLittleEndian()
    {
        Assert.Equal(new byte[] { 0, 0, 0, 0, 1 }, McsFrame.Encode(McsFrameKind.Ring, []));
        using var stream = new MemoryStream();
        McsFrame.Write(stream, McsFrameKind.Ring, []);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 1 }, stream.ToArray());
        stream.Position = 0;
        var frame = McsFrame.Read(stream);
        Assert.Equal(McsFrameKind.Ring, frame!.Value.Kind);
        Assert.Empty(frame.Value.Payload);
        Assert.Null(McsFrame.Read(stream));                          // the peer closed between frames
    }

    [Fact]
    public void AReaderRefusesWhatIsNotAWholeFrameOfAKnownKind()
    {
        static void Refused(byte[] bytes) => Assert.Throws<InvalidDataException>(() => McsFrame.Read(new MemoryStream(bytes)));
        Refused([0, 0, 0]);                                          // ends inside the header
        Refused([0, 0, 0, 0, 9]);                                    // a kind no build knows
        Refused([0, 0, 0, 0, 0]);                                    // kind 0 is none
        Refused([1, 0, 0, 0, 1, 7]);                                 // a Ring carries no payload
        Refused([0, 0, 0, 0x80, 1]);                                 // 2^31 bytes: the guard
        Refused([0xFF, 0xFF, 0xFF, 0xFF, 1]);                        // 2^32 - 1: the guard
        Assert.Throws<InvalidDataException>(() => McsFrame.Encode(McsFrameKind.Ring, new byte[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => McsFrame.Encode((McsFrameKind)200, []));
    }
}
