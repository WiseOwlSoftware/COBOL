// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ SLICE S0 OF THE MESSAGE-CONTROL FACILITY (kb/Work PB4739; docs/rearchitecture/DESIGN-message-control.md §7.2, §9):
/// the operating-system facts the peer AF_UNIX transport (§5) and the communication facility's bell
/// (DESIGN-communication-facility.md §6) rest on, which no document read states. One test per fact, run on Windows and
/// Linux CI (and macOS when a leg exists), so a runtime slice never builds on an assumption nobody observed.
/// <para>The first two witnesses, a killed child's connected stream and its unaccepted backlog, DECIDE THE TRANSPORT
/// (§9 S0): if a peer cannot see a killed process's sockets end on Windows, no 2023 runtime slice starts and the owner
/// re-decides with §8's liveness-byte design. The rest set constants and mappings; a failed rename witness only switches
/// that OS to <c>McsPublishByte</c> publishing (§2023:5.4.1).</para>
/// <para>A witness whose answer the design leaves OS-specific asserts the SET of answers the runtime's mapping handles
/// and writes the observed one to the test output, where a reader of the CI log finds it.</para>
/// <para>The child process is a file-based C# app (<c>dotnet run</c>), so the kills are real ones: no close, no flush,
/// nothing of the child's runtime runs. Every wait is a message or the process's own end, bounded only by the harness
/// budget (<see cref="ObservationBudget.UnitMs"/>), never a delay.</para>
/// </summary>
public sealed class PlatformAssumptionTests : IDisposable
{
    /// <summary>The child: binds and listens at args[0], prints LISTENING, optionally accepts one connection
    /// (args[1] == "accept") and prints ACCEPTED, then waits for the kill.</summary>
    private const string ChildSource = """
        using System.Net.Sockets;
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(args[0]));
        listener.Listen(8);
        Console.Out.WriteLine("LISTENING");
        Console.Out.Flush();
        Socket? accepted = null;
        if (args[1] == "accept")
        {
            accepted = listener.Accept();
            Console.Out.WriteLine("ACCEPTED");
            Console.Out.Flush();
        }
        Thread.Sleep(Timeout.Infinite);
        GC.KeepAlive(accepted);
        """;

    private readonly ITestOutputHelper _output;
    private readonly string _work = ScratchDirectory.Create("pb4739-");

    /// <summary>Where every socket of this class lives, a fresh directory of the socket directory's own shape, because that is the shape the product's budget covers. Never under
    /// <see cref="_work"/>: a scratch directory's depth depends on the host's user name and process id, and a CI runner's left eight bytes of the limit for a name (kb/Work PB4742).</summary>
    private readonly string _sockets = WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.Resolve(WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.NewRunId()).Path.TrimEnd(Path.DirectorySeparatorChar);

    public PlatformAssumptionTests(ITestOutputHelper output)
    {
        _output = output;
        Assert.True(Socket.OSSupportsUnixDomainSockets, "this host's .NET reports no AF_UNIX support");
    }

    public void Dispose()
    {
        ScratchDirectory.Remove(_sockets);
        ScratchDirectory.Remove(_work);
    }

    private string SocketPath(string name) => Path.Combine(_sockets, name);

    private static Socket NewSocket() => new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

    private static void Bind(Socket socket, string path) => socket.Bind(new UnixDomainSocketEndPoint(path));

    private static void Connect(Socket socket, string path) => socket.Connect(new UnixDomainSocketEndPoint(path));

    private static Socket Listening(string path, int backlog = 8)
    {
        var socket = NewSocket();
        Bind(socket, path);
        socket.Listen(backlog);
        return socket;
    }

    private static SocketException Refused(Action connect) => Assert.Throws<SocketException>(connect);

    /// <summary>Start the child at <paramref name="path"/> and return it once it listens (and, for
    /// <paramref name="accept"/>, once it holds <paramref name="accept"/> connection: the caller connects first).</summary>
    private LiveProcess StartChild(string path, bool accept)
    {
        string dir = Path.Combine(_work, "child");
        Directory.CreateDirectory(dir);
        string source = Path.Combine(dir, "child.cs");
        File.WriteAllText(source, ChildSource, new UTF8Encoding(false));
        var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = dir };
        // The tree kill below must not reach the host-wide Roslyn server (VBCSCompiler) that other builds on this host share:
        // with shared compilation off, `dotnet run` compiles in-process and starts no server of its own.
        psi.Environment["UseSharedCompilation"] = "false";
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add(source);
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add(path);
        psi.ArgumentList.Add(accept ? "accept" : "listen");
        var child = LiveProcess.Start(psi);
        // `dotnet run` writes its own build's warnings to the same stdout (a restore's NU1903, an SDK target's), first, so the child's protocol starts at its first line of its own.
        while (true)
        {
            string line = child.ExpectLine();
            if (line == "LISTENING") return child;
            _output.WriteLine($"dotnet run: {line}");
        }
    }

    /// <summary>What <paramref name="socket"/>'s blocking receive does once its peer is gone: zero bytes (end of stream) or
    /// the exception's error code. A receive that never returns is a failed witness, named, not a hang.</summary>
    private string ReceiveAfterPeerKilled(Socket socket)
    {
        var receive = Task.Run(() =>
        {
            try { return $"end-of-stream:{socket.Receive(new byte[16])}"; }
            catch (SocketException e) { return $"SocketException:{e.SocketErrorCode}"; }
        });
        if (!receive.Wait(ObservationBudget.UnitMs))
            throw new Xunit.Sdk.XunitException(
                "the receive on a socket whose peer process was killed returned neither zero nor an exception "
                + $"within {ObservationBudget.UnitMs} ms: the OS did not end the link (DESIGN-message-control.md §5.5, §9 S0)");
        return receive.Result;
    }

    [Fact]
    public void KilledChildEndsAConnectedStream()
    {
        string path = SocketPath("k1");
        using var child = StartChild(path, accept: true);
        using var client = NewSocket();
        Connect(client, path);
        Assert.Equal("ACCEPTED", child.ExpectLine());
        child.Kill();
        string outcome = ReceiveAfterPeerKilled(client);
        _output.WriteLine($"receive after the peer's kill, connection accepted: {outcome}");
        Assert.True(outcome == "end-of-stream:0" || outcome.StartsWith("SocketException:", StringComparison.Ordinal), outcome);
    }

    [Fact]
    public void KilledChildEndsAConnectionStillInItsListenBacklog()
    {
        string path = SocketPath("k2");
        using var child = StartChild(path, accept: false);
        using var client = NewSocket();
        Connect(client, path);
        child.Kill();
        string outcome = ReceiveAfterPeerKilled(client);
        _output.WriteLine($"receive after the peer's kill, connection never accepted: {outcome}");
        Assert.True(outcome == "end-of-stream:0" || outcome.StartsWith("SocketException:", StringComparison.Ordinal), outcome);
    }

    [Fact]
    public void ConnectToTheKilledListenersSocketFileFails()
    {
        string path = SocketPath("k3");
        using var child = StartChild(path, accept: false);
        child.Kill();
        bool fileSurvives = File.Exists(path);
        using var client = NewSocket();
        var error = Refused(() => Connect(client, path));
        _output.WriteLine($"dead listener: file survives the kill = {fileSurvives}; Connect answers {error.SocketErrorCode} "
                          + $"(native {error.NativeErrorCode})");
        if (fileSurvives)
        {
            // §2023:5.4.1's refusal rule rests on this: a file under s/ or p/ that refuses a connect belonged to an ended owner.
            Assert.Equal(SocketError.ConnectionRefused, error.SocketErrorCode);
        }
    }

    [Fact]
    public void BindOverAnExistingSocketFileFailsAndSucceedsOnceItIsRemoved()
    {
        string path = SocketPath("b1");
        using (var first = Listening(path)) { /* closed below by the using; the file's fate is the witness */ }
        bool survives = File.Exists(path);
        _output.WriteLine($"socket file after its listener's Dispose: exists = {survives}");
        if (!survives)
        {
            // This OS removes the file with the socket; "bind over an existing file" needs one, so plant it live.
            // (A killed listener's leftover file, the stale case, is witnessed by ConnectToTheKilledListenersSocketFileFails.)
            using (var live = Listening(path))
            using (var second = NewSocket())
            {
                var inUse = Assert.Throws<SocketException>(() => Bind(second, path));
                _output.WriteLine($"Bind over a live listener's file: {inUse.SocketErrorCode}");
                Assert.Equal(SocketError.AddressAlreadyInUse, inUse.SocketErrorCode);
            }
            // The listener is gone and its file with it: a bind at the same path succeeds.
            using var rebound = Listening(path);
            Assert.True(File.Exists(path));
            return;
        }
        using (var second = NewSocket())
        {
            var inUse = Assert.Throws<SocketException>(() => Bind(second, path));
            _output.WriteLine($"Bind over the existing socket file: {inUse.SocketErrorCode}");
            Assert.Equal(SocketError.AddressAlreadyInUse, inUse.SocketErrorCode);
        }
        File.Delete(path);
        using var third = NewSocket();
        Bind(third, path);
        third.Listen(1);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ConnectBetweenBindAndListenIsRefused()
    {
        string path = SocketPath("w1");
        using var server = NewSocket();
        Bind(server, path);
        using (var early = NewSocket())
        {
            var error = Refused(() => Connect(early, path));
            _output.WriteLine($"Connect after Bind and before Listen: {error.SocketErrorCode} (native {error.NativeErrorCode})");
        }
        server.Listen(1);
        using var late = NewSocket();
        Connect(late, path);
        using var accepted = server.Accept();
        Assert.True(accepted.Connected);
    }

    [Fact]
    public void RenamedListenerIsReachedAtTheNewNameAndNotTheOld()
    {
        string oldPath = SocketPath("r1");
        string newPath = SocketPath("r2");
        using var server = Listening(oldPath);
        File.Move(oldPath, newPath);
        Assert.False(File.Exists(oldPath));
        using (var viaOld = NewSocket())
        {
            var missing = Refused(() => Connect(viaOld, oldPath));
            _output.WriteLine($"Connect at the old name after the rename: {missing.SocketErrorCode} (native {missing.NativeErrorCode})");
        }
        using var viaNew = NewSocket();
        Connect(viaNew, newPath);
        using var accepted = server.Accept();
        viaNew.Send([42]);
        var buffer = new byte[1];
        Assert.Equal(1, accepted.Receive(buffer));
        Assert.Equal(42, buffer[0]);
    }

    /// <summary>The longest path <c>Bind</c> accepts, found by growing a file name until Bind refuses, and the one byte
    /// more that it refuses: the constant <c>McsSocketDirectory.Fits</c> will be written against (§6).</summary>
    private int MeasureMaxPathBytes()
    {
        int baseBytes = Encoding.UTF8.GetByteCount(_sockets) + 1;
        int longest = 0;
        for (int name = 1; baseBytes + name <= 240; name++)
        {
            string path = SocketPath(new string('p', name));
            using var socket = NewSocket();
            try { Bind(socket, path); }
            catch (Exception e) when (e is SocketException or ArgumentOutOfRangeException)
            {
                // Everything longer must be refused too; the loop's next iteration would find a longer accepted path.
                return longest;
            }
            File.Delete(path);
            longest = baseBytes + name;
        }
        return longest;
    }

    [Fact]
    public void MaxPathBytesIsTheLongestPathBindAcceptsAndOneMoreFails()
    {
        int max = MeasureMaxPathBytes();
        _output.WriteLine($"MaxPathBytes on {Environment.OSVersion.Platform}: {max}");
        Assert.True(max > 0 && max < 230, $"Bind accepted a path of {max} bytes: no limit was found");
        Assert.Equal(WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.MaxPathBytes, max);   // the constant McsSocketDirectory.Fits is written against
        if (OperatingSystem.IsLinux()) Assert.Equal(107, max);          // unix(7): sun_path is 108 bytes, NUL included
        else if (OperatingSystem.IsMacOS()) Assert.Equal(103, max);     // sys/un.h: char sun_path[104]

        // The path at the limit binds, listens and is connectable; one byte more is refused.
        int baseBytes = Encoding.UTF8.GetByteCount(_sockets) + 1;
        string atLimit = SocketPath(new string('q', max - baseBytes));
        using (var server = Listening(atLimit))
        using (var client = NewSocket())
        {
            Connect(client, atLimit);
            using var accepted = server.Accept();
            Assert.True(accepted.Connected);
        }
        File.Delete(atLimit);
        using var tooLong = NewSocket();
        Assert.ThrowsAny<Exception>(() => Bind(tooLong, SocketPath(new string('q', max - baseBytes + 1))));
    }

    [Fact]
    public void TheWorstCaseEntryOfTheSocketDirectoryFitsMaxPathBytes()
    {
        int max = MeasureMaxPathBytes();
        const int Entry = WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.LongestEntryBytes;   // DESIGN-message-control.md §5.1: p/ (2) + <N>/ (at most 15) + <r> (16)
        string hash = new('h', 16);     // <h>: 16 hex digits
        var cases = new List<(string Label, string Directory)>();
        if (OperatingSystem.IsWindows())
        {
            // The directory this host resolves (its temp path, or the fallback when that is too long), and the fallback itself (kb/Work PB4922): a temp path of any length
            // leaves the fallback, so the worst-case entry must fit under %ProgramData%\wiseowl-mcs-<u>\<h>\ on every host.
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), user = new('u', 16);
            cases.Add(("this host", WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.WindowsDirectory(Path.GetTempPath(), programData, user, hash).Path));
            cases.Add(("%ProgramData%", WiseOwl.Cobol.Runtime.Mcs.McsSocketDirectory.WindowsDirectory(new string('t', 200) + "\\", programData, user, hash).Path));
        }
        else
        {
            string uid = uint.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture); // 10 digits
            cases.Add(("/tmp", $"/tmp/wiseowl-mcs-{uid}/{hash}/"));
            if (OperatingSystem.IsLinux())
                cases.Add(("$XDG_RUNTIME_DIR", $"/run/user/{uid}/wiseowl-mcs-{uid}/{hash}/"));
        }
        foreach (var (label, dir) in cases)
        {
            int worst = Encoding.UTF8.GetByteCount(dir) + Entry;
            _output.WriteLine($"{label}: <S> = {dir.Length} bytes, worst entry = {worst}, MaxPathBytes = {max}");
            Assert.True(worst <= max, $"{label}: the worst-case entry is {worst} bytes, over MaxPathBytes {max}");
        }
    }

    [Fact]
    public void SocketSelectOverAfUnixReportsAPendingConnectionAndAnArrivedByte()
    {
        string path = SocketPath("s1");
        using var server = Listening(path);
        var none = new List<Socket> { server };
        Socket.Select(none, null, null, 0);
        Assert.Empty(none);                                   // nothing pending: not readable

        using var client = NewSocket();
        Connect(client, path);
        var pending = new List<Socket> { server };
        Socket.Select(pending, null, null, ObservationBudget.UnitMs * 1000);
        Assert.Single(pending);                               // a pending connection makes the listener readable

        using var accepted = server.Accept();
        client.Send([7]);
        var arrived = new List<Socket> { accepted };
        Socket.Select(arrived, null, null, ObservationBudget.UnitMs * 1000);
        Assert.Single(arrived);
        _output.WriteLine("Socket.Select over AF_UNIX: listener readable at a pending connection, stream readable at a byte");
    }

    [Fact]
    public void PollOnAListeningSocketReturnsAtAPendingConnection()
    {
        string path = SocketPath("p1");
        using var server = Listening(path);
        Assert.False(server.Poll(0, SelectMode.SelectRead));
        using var client = NewSocket();
        Connect(client, path);
        Assert.True(server.Poll(ObservationBudget.UnitMs * 1000, SelectMode.SelectRead));
    }

    [Fact]
    public void NonBlockingConnectToAListenerWithRoomCompletesOrWouldBlockNeverRefused()
    {
        string path = SocketPath("n1");
        using var server = Listening(path, backlog: 4);
        using var client = NewSocket();
        client.Blocking = false;
        try
        {
            Connect(client, path);
            _output.WriteLine("non-blocking connect to a listener with room: completed at once");
        }
        catch (SocketException e)
        {
            // Where WouldBlock is the in-progress answer (Windows gives it whatever the backlog holds), the connect still
            // completes: a ring that answers WouldBlock is not a lost ring, so the wait below must end and the byte arrive.
            _output.WriteLine($"non-blocking connect to a listener with room: {e.SocketErrorCode}");
            Assert.Equal(SocketError.WouldBlock, e.SocketErrorCode);
            Assert.True(client.Poll(ObservationBudget.UnitMs * 1000, SelectMode.SelectWrite));
        }
        client.Send([9]);
        Assert.True(server.Poll(ObservationBudget.UnitMs * 1000, SelectMode.SelectRead));
        using var accepted = server.Accept();
        var buffer = new byte[1];
        Assert.Equal(1, accepted.Receive(buffer));
        Assert.Equal(9, buffer[0]);
    }

    [Fact]
    public void NonBlockingConnectToAListenerWithAFullBacklogAnswersAnErrorNeverCompletes()
    {
        // WouldBlock is the in-progress answer on Windows even for a listener with room, so it counts as the full-backlog
        // answer only when the connect then fails to finish (the socket still cannot send after the poll). Windows reports
        // a full backlog as NoBufferSpaceAvailable (WSAENOBUFS); Linux connect(2) as WouldBlock (EAGAIN).
        string path = SocketPath("n2");
        using var server = Listening(path, backlog: 1);
        var held = new List<Socket>();
        try
        {
            SocketError? answer = null;
            for (int i = 0; i < 4096 && answer is null; i++)
            {
                var client = NewSocket();
                client.Blocking = false;
                held.Add(client);
                try { Connect(client, path); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    if (client.Poll(ObservationBudget.UnitMs * 1000, SelectMode.SelectWrite) && Finished(client)) continue;
                    answer = SocketError.WouldBlock;
                }
                catch (SocketException e) { answer = e.SocketErrorCode; }
            }
            _output.WriteLine($"non-blocking connect to a listener with a full backlog: after {held.Count} connects, "
                              + (answer is { } a ? a.ToString() : "every connect completed"));
            Assert.NotNull(answer);
            Assert.True(answer is SocketError.WouldBlock or SocketError.ConnectionRefused or SocketError.NoBufferSpaceAvailable,
                        answer.ToString());
        }
        finally
        {
            foreach (var socket in held) socket.Dispose();
        }
    }

    /// <summary>True when the in-progress connect finished: a connected stream socket accepts a byte, an unconnected one does not.</summary>
    private static bool Finished(Socket client)
    {
        try { client.Send([0]); return true; }
        catch (SocketException) { return false; }
    }
}
