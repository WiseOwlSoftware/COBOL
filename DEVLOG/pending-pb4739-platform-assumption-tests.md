## Messaging slice S0: PlatformAssumptionTests, the 2023 transport's Windows gate (PB4739)

DESIGN-message-control.md §7.2 and §9 make slice S0 the gate of the peer AF_UNIX transport: if a killed process's sockets do not
end for its peer on Windows, no 2023 runtime slice starts. `tests/WiseOwl.Cobol.Tests.Unit/PlatformAssumptionTests.cs` is the
whole class, 12 witnesses: kill of a child holding an accepted connection and of one holding an unaccepted backlog entry (a real
`dotnet run` file-based child, killed through `LiveProcess`), a dead listener's file, bind over an existing file, a connect between
Bind and Listen, rename of a listening socket's file, `MaxPathBytes` and §5.1's worst-case entry, `Socket.Select`, `Socket.Poll`,
and the non-blocking connect cases of the communication facility's bell.

Windows result (this host): both kills reach the peer as `ConnectionReset`, so the transport stands and the liveness-byte design
stays a rejected alternative. The rename witness passes, so no `McsPublishByte`. `MaxPathBytes` is 107. Windows answers a
non-blocking connect `WouldBlock` even to a listener with room, so the test also waits for it to finish and checks the byte
arrives (a `WouldBlock` ring is not a lost ring). The design doc's §7.2 records the observed answers and its §5.1 table's
Windows limit. Waits are messages or the harness budget; no literal time limit.

The Linux answers come from CI's merge group; the class writes each observed answer to its output. The note named no source
file, so nothing outside the test and the design doc changed.

Review round 1 (R1-1): the full-backlog witness stopped at the first WouldBlock, which Windows also gives a listener with room. It now counts a WouldBlock only when the connect then fails to finish, and accepts NoBufferSpaceAvailable, the real Windows full-backlog answer (6th connect on Listen(1)); both design docs state it. R1-2: the Windows branch of the bind test now also binds again after the live listener is gone. R1-3: the child runs with UseSharedCompilation=false so the tree kill cannot reach VBCSCompiler.
