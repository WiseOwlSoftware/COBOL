## PB6134: the socket transport's accept loop survives a connection it cannot adopt, and a non-observation reports both processes

PB6134 filed a messaging requestor whose SEND never returned in a full gate (MessagingProcessTests, EC-MCS-MESSAGE-LENGTH case, 120 s, no SENT line). Not reproduced: 8 concurrent
runs of the class on the wave's build, 8 x 25/25 green.

Reading the two-process path against the note's derivation (a SEND that never returns holds a live link in Waiting while the server never answers) found one mechanism that fits:
`SocketTransport.AcceptLoopAsync` caught failures of the accept (PB4920) but not of the step after it. `PeerLink`'s constructor builds a `NetworkStream`, an `IOException` for a socket that is
no longer connected; the exception left the loop, the listener stayed published and listening, and every later connect was queued by the OS and never read. Fix: a connection that cannot be
adopted is closed and the loop goes on, on both arms (server and bell). Witness `AnAcceptedSocketThatCannotBeAdoptedIsClosedAndLeavesTheServerAcceptingAndTheBellRinging` failed before
("no event arrived") and passes after.

The note's other ask: `LiveProcess` pumps stdout into a queue, and a `HarnessNonObservationException` carries the state, the unread stdout and the stderr of every process the test named as a
companion (`MessagingProcessTests.Start`). `LiveProcessTests` witnesses the queue. The gate's failure is not proven to be the accept-loop mechanism; a recurrence now says what both processes printed.

No inventory row closes (`closes_rows: []`). No diagnostic codes used.
