## Entry 3284 — 2026-11-07 17:28 PST — SocketTransport's accept loops end only when their listener closes, and the listeners stop before the links

kb/Work PB4920 (leads R1-3 and R1-6 of PB4742's review). Re-probed by reading the code at the sites on this build (the failures need an injected OS fault, so the probe is the new tests): `AcceptServerAsync` and `AcceptBellAsync` read every `SocketException` as "the listener closed", so a real accept failure ended the loop with the socket still listening and its file published; `Dispose` closed links before listeners, and an accept already past its await could register a link nothing disposed.

Fix: one `AcceptLoopAsync` replaces the two copies. It ends only when `Listener.Stop` is cancelled; any other accept failure waits 50 ms and accepts again; a socket accepted after the stop is closed at once. `NewLink` registers under a lock and refuses (closing the socket) once `Dispose` or `Abort` has taken the link registry, and both now close the listeners first. `AcceptSocketAsync` is a test seam, as `BetweenListenAndRename` is.

Tests (`SocketTransportTests`): a server and a bell each fail one accept (TooManyOpenSockets, ConnectionAborted) and still accept and ring; a connection delivered after `Dispose` is closed (the peer reads end-of-stream) and posts no `LinkAccepted`. Rows closed: none.

