## Entry 4082 — 2026-11-27 19:50 PST — The bell reader adopts its stream inside the try and closes the socket on every path

PB6218. Re-probe: the bell half of `AnAcceptedSocketThatCannotBeAdoptedIsClosedAndLeavesTheServerAcceptingAndTheBellRinging` passed without witnessing the fix, because `AcceptBellAsync` hands the socket to `Task.Run(ReadRingAsync)`, so the `AcceptLoopAsync` catch is never reached on the bell arm. `ReadRingAsync` built its `NetworkStream` before the `try`, so an unadoptable socket faulted the task unobserved and stayed open.

Fix: `new NetworkStream` moves inside the try, and a `finally` disposes the accepted socket. The test captures the unadoptable bell socket and asserts its `SafeHandle` is closed; with the old `ReadRingAsync` it fails ("the unadoptable bell socket was never closed"), with the fix it passes. The server arm already closes through the `AcceptLoopAsync` catch (PB6134). No inventory rows, no diagnostics codes used.

