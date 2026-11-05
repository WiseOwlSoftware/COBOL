# The communication facility (COBOL-85 and COBOL-2002)

Status: §1 to §5 IMPLEMENTED (kb/Work PB4677, owner decision 12 of PB4614, 2026-11-04). §6's message control system DESIGNED on the
runtime's paged store and a socket bell (owner decision of 2026-11-04 on the PB4700 transports); §8 designs the engine entry it needs and
§9 orders the slices that build both (kb/Work PB4734-PB4738). WiseOwl COBOL implements the facility at COBOL-85 and 2002, so the per-edition suites can witness
it and NIST-85's CM module runs.

The authority is ISO/IEC 1989:2002 Annex A (Communications facility), with E.5 as its concepts section, read from the 2002 post-ballot text
(`E:/COBOL-coord/specs-drafts/t2002.txt`, general formats rendered from `x2002/cobolv200112.pdf`). The 2002 text is outside the tracked
tree, so citations of it are not `cite.py`-checkable and are written `ISO/IEC 1989:2002 A.n`, never with the `§` of the 2023 text. The
1985 text is not held: CCVS-85's CM programs are the witness of the COBOL-85 KEY phrase and status 40.

The facility is not the 2023 asynchronous messaging facility (§14.9.31, §14.9.38), which shares the words RECEIVE and SEND, and the two
differ in kind. This facility stores and forwards: a message outlives the run unit that sent it, so the MCS keeps it in a durable store.
The 2023 facility is a rendezvous: "The statement suspends execution until the message is received by the server run unit" (§14.9.38.4
GR4), so no 2023 message outlives the parties that hold it. The 2023 facility therefore has its own transport, AF_UNIX stream sockets
between run units (docs/rearchitecture/DESIGN-message-control.md), and shares only three things with this one: the resolution of the MCS
directory (`McsDirectory`), the resolution of its socket directory (`McsSocketDirectory`) and one frame codec (`McsFrame`), so the product
has one socket primitive (§6, "Shared with the 2023 facility").

## 1. Editions

| Edition | The facility | How |
|---|---|---|
| 85 | Accepted, with the KEY phrase of ENABLE and DISABLE | no diagnostic |
| 2002 | Accepted, obsolete, no KEY phrase | COBOL0903 at each construct; COBOL0902 for the KEY phrase |
| 2014, 2023 | Removed | COBOL0902 at each construct; a warning under `--permissive`, and the program runs |

The rows are `communication-facility-removed-2014` (obsolete 2002) and `communication-key-phrase-removed-2002`. The 2002 Annex A says the
facility "is an obsolete element and is to be deleted from the next revision of standard COBOL", and the 2014 FCD has no such section.
Everything parses at every edition (the union grammar); the parse arm of `VersionConformancePass` gates each rule on recognition, so a
statement is named even where no section declares its cd-name. §8.9 already reserved the facility's words at 85 and 2002 only, so each
new lexer token is a nameSlot row of `cobol-words.json`, gated by the reservation table, and is an ordinary user word from 2014
(`pb4677_user_words_from_2014`).

## 2. Grammar

`Grammar/Core/CobolCommunication.g4` holds `communicationSection`, `communicationDescriptionEntry` and the statements. The printed page
decides the optional words: SYMBOLIC, IS, FOR, TIMES, BY, WITH, ADVANCING, the MESSAGE of MESSAGE COUNT and the LINE of REPLACING LINE
carry no underline.

Two statements need the union with the 2023 forms. RECEIVE parts by the word after its operand (`GIVING` against `MESSAGE` or `SEGMENT`).
`SEND x FROM y` is the shape of both A.2.6.1 Format 1 and §14.9.38.2, and only x's declaration can tell them apart: the `cdName` rule's action
records each cd-name, and the `statement` arm of the 2023 SEND is predicated on `communicationNameFollows()`. A data division precedes its
procedure division and an action never runs during prediction, so the set is complete when a statement asks. A SEND that writes an indicator
fails the 2023 arm and reaches `communicationSendStatement` whatever x is, so an undeclared name there is COBOL6170.

The three formats of the entry are one rule. `CommunicationFormats` (the binder) is the one table of which clause each format prints, so a clause of
another format is refused by name.

## 3. The record area

A CD is a redefinition class, the FD's implicit record area applied to a new section (`CommunicationSectionBinder`):

- `CommunicationDescription.Area` is the record A.1.2.3 prints as the "IMPLICIT DESCRIPTION", built from `CommunicationFormats`: an unnamed level 1 group of
  elementary items, each named by its clause or by its place in the series, unnamed otherwise (`FILLER`). The output table is flat without a DESTINATION TABLE
  OCCURS clause (A.1.2.2 SR11) and an `OCCURS` group with it (SR12, with its INDEXED BY names).
- Every record written after the CD joins the class by `RedefinitionKind.ImplicitCommunicationRecord`. The class's anchor is the FIRST written record when there is
  one, because "only the first redefinition may contain VALUE clauses" (SR5, SR9, SR14) and a class's seed is its anchor's.
- The items are static like working-storage's (ISO/IEC 1989:2002 8.6.3), so they join `WorkingStorageRoots`, under `EntrySection.Communication`.
- A cd-name is a user word of its own type (`UserWordKind.CdName`) and no data-name. The `cdName` rule does not ride `dataName`, whose FILLER and PROCEDURE are reserved words (A.1.2.1, 8.3.1.1.1): a word
  §8.9 reserves draws COBOL0901 through `reservedGatedWord` and `IsProvableUserWordPosition`, and FILLER or PROCEDURE draws COBOL5506. It qualifies an item of its entry (the file-cd-report-qualifier of 8.4.1.1.1), through the
  one qualifier walk.

Asked at the entry: SR2 (INITIAL once, and not with USING), SR4 and SR8 (some way of describing the area), SR6, SR10 and SR15 (unique data-names), and Annex A's "only in a program definition". `LevelNumberPass` asks 13.16.31 SR2 (no level 77) with the FD's arm, and `ResolveRedefinesPass` 13.16.42
SR4 (no REDEFINES at level 1). A residence row of `ClausePlacementRules` lists the sections its sentence lists, so EXTERNAL, GLOBAL (2002 13.16.25.2 SR1 b) and BASED refuse a CD's record as COBOL2404. Asked once the forest is complete (`CheckCommunicationRecordsPass`): SR5 and SR14 (exactly 87 or 33
positions) and the VALUE rule. All are COBOL6169 with the rule quoted, the COBOL1720 precedent.

## 4. The statements

`CommunicationBinder` builds one `BoundCommunicationStatement` per statement, carrying `Cd.Area`'s place: they are one operation over one area, and the optional members are the operands
each format prints. COBOL6170 is a name that is no cd-name, or a cd-name of a format the statement's rule does not admit. COBOL6171 is an operand of the wrong class or category.

The ADVANCING phrase's operands are screened and kept nowhere. The devices of this MCS are oriented to variable-length messages (A.2.6.3 GR2), so vertical positioning "is not
applicable" and "the MCS will ignore" it (GR12). REPLACING LINE follows.

The facility raises no exception condition: "exception handling [is] intentionally omitted from the obsolete communications facility" (Annex A NOTE; 14.5.12.1 NOTE 2).

## 5. Code generation

`CommunicationEmitter` renders one `MessageControl` call per statement. The area goes over through `OperandText.RecordAreaImage`, the one record-area channel, and comes back through
`SequentialIoEmitter.EmitImageInto` verbatim (other descriptions read the same storage). "The message control system (MCS) always references the record according to the data description
defined in" GR2, GR16 and GR24, so the runtime reads positions and a user's redefinitions never reach it. RECEIVE returns a `CommunicationReceipt`: the area, the receiving item's image, and
which phrase runs. A CD with INITIAL sets its names once per instance, before the first procedure division statement (GR6, GR28).

## 6. The message control system

E.5.1 requires "that a message control system (MCS) be present in the run unit's operating environment", and everything about it is the implementor's. No .NET host has one, so the runtime is
one (`Runtime/Communication`): `MessageControl` the entry points, `CommunicationArea` the positions, `MessageControlSession` the run unit's half, `MessageStore` the MCS's statement
boundary, `McsStore` its paged store, and `MessageBell` the wake-up of a RECEIVE that waits.

Two rules hold the design together. **The store is the only truth:** every outcome of every statement is decided inside one statement of the
runtime's paged store engine (DESIGN-runtime-library.md §2.2.1), under the store's cross-run-unit mutex. **The bell is only a hint:** a ring
says "ask again", and a lost, late, duplicated or spurious ring costs latency and never changes an answer.

**The MCS directory.** `McsDirectory.Resolve(null)` names it (§6 "Shared with the 2023 facility"). It holds:

| File | Written by | Content |
|---|---|---|
| `mcs.json` | the user | the definitions ("defined by the user to the message control system at some time prior to the execution of the run unit", E.5.1) |
| `mcs.store` | the MCS | the paged store of §6 "The store" |
| `<device>.out` | the MCS | a device's output, appended (§6 "Devices") |
| `.wolk-<16 hex digits>/` | the region-lock host | only where `HostRegionLocks.HostOfThisProcess` chooses the lock-file host, `SidecarRegionLocks` (macOS, any other Unix, and Linux when `OfdRegionLocks.Available` is false): the lock directory `SidecarLockFiles.LockDirectoryOf` names beside `mcs.store` |

`SidecarLockFiles.LockDirectoryOf` names the lock directory inside the file's own parent directory, from the file's name alone, so every
spelling of the MCS directory reaches one lock directory and one store mutex. With no `mcs.json`, every symbolic name is unknown and no
store is created. The 2023 facility adds only `servers/`, the logs of the servers it starts (DESIGN-message-control.md,
server activation). The bells live in the socket directory (§6 "The RECEIVE wait"), outside the MCS directory, because an AF_UNIX path has
a length limit and a user's MCS directory can be arbitrarily deep.

```json
{
  "queues": [
    { "name": "Q1", "key": "PASSWORD", "enabled": true,
      "messages": [ "ONE SEGMENT", { "segments": ["A", "B"], "group": true, "source": "T1", "date": "260102", "time": "03040506" } ],
      "subQueues": [ { "name": "S1" } ] }
  ],
  "sources": [ { "name": "T1", "key": "PASSWORD", "enabled": true } ],
  "destinations": [ { "name": "D1", "key": "PASSWORD", "enabled": true, "capacity": 100, "deliverTo": "Q1/S1", "source": "T1" } ],
  "terminals": [ { "name": "T2", "key": "PASSWORD", "enabled": true, "messages": [] } ]
}
```

Every member but `name` is optional. A name is 1 to 12 characters with no `/` or `\`, since a queue path and a device file are built from names; queues nest to the four levels of E.5.5.4. `key` is the COBOL-85 password (a KEY phrase's value against it, status 40; an endpoint with
none admits any). `messages` enter a queue or a terminal once, when the store first holds that endpoint. `capacity` bounds a destination's waiting messages (status 65, error key 6). `deliverTo` makes a destination feed a queue path or
`terminal:<name>`, so one run unit talks to another; a destination without it transmits to its device, the file `<name>.out`, and a terminal's SEND does too.
A `servers` member defines the 2023 facility's servers (DESIGN-message-control.md §0 Q3); this facility reads its four members by
name and ignores it.

**The store.** `McsStore` is one paged store, `mcs.store`, on the engine of DESIGN-runtime-library.md §2.2.1: shadow paging, stamped
pointers, a generation ledger, and O(log n) pages per statement. Its meta page records the store kind `M` (§8), so a COBOL OPEN of
`mcs.store` through a RELATIVE or INDEXED description answers '39' and never misreads it. The store holds one B+ tree under the ordinal
key order (`KeyOrders.Ordinal`); the first key byte is a tag, and each tag is one logical table. Keys are big-endian, names are UTF-8, a
queue path is its level names each followed by the byte 0x01, and a serial is the 8-byte big-endian value of `Meta`'s next serial.

| Tag | Table | Key | Value |
|---|---|---|---|
| `C` | `Meta` | one fixed key | the store's identity (16 random bytes, minted at creation), the next serial, the SHA-256 of the `mcs.json` it was seeded from, and the three bell counters `BackstopFires`, `BackstopFinds` and `BellUnavailable` (§6 "The RECEIVE wait") |
| `E` | `Endpoints` | kind (queue, source, destination, terminal), name or queue path | enabled, key, capacity, `deliverTo`, source |
| `M` | `QueueMessages` | queue path, 0x00, serial | `StoredMessage` in its binary codec: segments, end of group, date, time, source |
| `I` | `TerminalMessages` | terminal, 0x00, serial | `StoredMessage`: what a terminal has sent, for an input-output CD's RECEIVE |
| `N` | `QueueCounts` | queue path prefix | the complete messages under the prefix |
| `O` | `Outbound` | destination, 0x00, serial | `StoredMessage` waiting while the destination's path is closed |
| `P` | `OutboundCounts` | destination | the messages `Outbound` holds for it, for the capacity check |
| `D` | `DeviceEnds` | device | the committed byte length of `<device>.out` |
| `W` | `Waits` | `Q` and a queue path, or `T` and a terminal; 0x00; the waiter's run id `<r>` | the waiter's bell path, as the waiter bound it, and the wait's stamp, a serial drawn from `Meta`'s next serial when the row is written |

The key layout makes the rules of E.5.5.3 and E.5.5.4 the tree's own order. A node's own messages (path, 0x00, serial) sort before its
children's (path, a name, 0x01, ...), children sort by name, and serials sort first in, first out. So the next message of a designated
structure is the first `M` key at or after the structure's prefix: one seek. `QueueCounts` holds one row per prefix of every path that
holds a message, changed in the same statement as the message, so ACCEPT MESSAGE COUNT reads one row.

**Seeding.** `MessageStore.Open` reads `mcs.json` once per run unit and refuses unusable definitions with 91 (`MessageStore.OpenAt`,
`IsUnusable`). The first statement of a run unit compares the definitions' hash with `Meta`'s. A store that is new, or whose hash differs,
is seeded in that statement: every defined endpoint gets its `Endpoints` row; an endpoint the store already held keeps its enabled state
and its messages, and takes its definition's key, capacity, `deliverTo` and source; an endpoint new to the store takes its definition's
enabled state and its `messages`; an endpoint the definitions no longer name is deleted with its messages and counts. Deleting a queue or a
terminal also deletes the `Waits` rows on it (for a queue, on its path and every path below it), and the seeding statement collects
those rows and rings each of their waiters after it releases the mutex, as any ringing statement does (§6 "The RECEIVE wait"). A waiter
blocked on a deleted endpoint therefore asks again and answers the unknown-name status (20, "One or more queues or subqueues unknown",
ISO/IEC 1989:2002 Table A.1), and none waits on an endpoint that no longer exists. The definitions are small and are held in memory as the endpoint objects of `MessageStoreState`, each a view over its `Endpoints` row.

**The transaction.** Each statement of §4 is one call of `MessageStore.Run`, and that call is one statement of the store engine:

1. Hold the store mutex: `HostRegionLocks.StoreMutexByte`, exclusively, through the run unit's own endpoint on its own `mcs.store`
   handle, **waiting** for another run unit's statement. `HostRegionLocks.HostOfThisProcess` chooses the host once per process. On
   Windows, and on Linux when `OfdRegionLocks.Available` holds, the host is `HandleRegionLocks` and the wait blocks in the kernel
   (`LockFileEx` without `LOCKFILE_FAIL_IMMEDIATELY` in `WindowsByteRangeLocks.Lock`; `F_OFD_SETLKW` in `OfdRegionLocks.Hold`).
   Everywhere else (macOS, any other Unix, and Linux without the ABI `OfdRegionLocks` was written for) the host is the lock-file host,
   `SidecarRegionLocks`, and the wait is its jittered retry. There is no retry count and no time limit: a statement waits for the one
   before it, as a keyed file's statement does.
2. Enter the store coherently (`PagedFile.Enter`, 2.2.1.7): one read of page 2, and the page cache dropped when another run unit changed
   it. The run unit's first statement opens the store under the mutex instead, creating it when `mcs.store` holds no bytes and
   otherwise running the adoption rule (2.2.1.6 R6).
3. Act: the statement reads the rows it needs and edits the tree in memory.
4. Transmit to devices (§6 "Devices").
5. Complete the statement through the engine's one statement core (§8): its reservation, its pages, page 2 last.
6. Release the mutex, then ring the bells the statement collected (§6 "The RECEIVE wait").

A statement that changes nothing (ACCEPT MESSAGE COUNT, a RECEIVE that finds no data and has a NO DATA phrase, a refused statement)
completes no page statement. The cost is O(log n) pages per row a statement touches, whatever the store holds.

**A host whose locks answer `Unavailable` is refused with status 92.** `RegionLocks.Hold` answers `Unavailable` when the host cannot
carry the lock on that file: `OfdRegionLocks.Classify` maps `EINVAL`, `EOPNOTSUPP`, `ENOSYS`, `EBADF` and a lasting `ENOLCK` to it,
`WindowsByteRangeLocks.Lock` maps every failure but `ERROR_LOCK_VIOLATION` to it, and `SidecarRegionLocks.Hold` answers it when no lock
directory can be named beside the file, when a waiting hold meets a lock directory it cannot observe, when `SidecarLockFiles.Publish`
answers `Unavailable` (the file's own directory is missing, the lock directory may not be written, or the probe finds that the host
takes no lock on the holder's file), and when two publication rounds in a row lose with no holder in the way. A keyed file on such a host keeps only run-unit-local protection (docs/CONFORMANCE.md DOC-A.1-75). The MCS cannot:
two run units inside one statement at once can take one message twice or lose one. So every statement whose mutex hold answers
`Unavailable` answers 92 and takes no action, and the MCS never runs unlocked. The MCS never relies on a share-none open either. On Unix,
.NET expresses `FileShare.None` as an advisory `flock`, and `SafeFileHandle.Unix.cs` (`Init`, dotnet/runtime main, read 2026-11-04)
throws only on `EWOULDBLOCK`: "Other errors, such as ENOTSUP (locking isn't supported) or EACCES (the file system doesn't allow us to
lock), will only hamper FileStream's usage without providing value, given again that this is only advisory / best-effort". The same file's
`DisableFileLocking` switch (`DOTNET_SYSTEM_IO_DISABLEFILELOCKING`) skips the `flock` altogether. A share-none lock file therefore admits
two run units at once on such a file system, silently; the region-lock host reports the same file system as `Unavailable`.

**Status 92 takes no action, by construction.** `MessageControlSession.Statement` is the one path of every statement: the action works
on a copy of the CD's area and a copy of the run unit's half, and only a statement that landed in the store publishes them, so a refused
statement answers the area it was handed with its status key changed. An `Unavailable` lock host answers 92 before the statement reads
anything. For a refusal the engine reports (a refused read, write or sync, a torn page, the boundary), the session decides whether the
statement landed before it answers:

1. The session discards its `PagedFile`: after a refusal the engine's state for it is not one to build on (a refused statement drops
   its cache and leaves the connector in permanent error, and after a failed sync "the medium's state is not known", which "R3 and R6
   decide ... at the next read or OPEN", DESIGN-runtime-library.md 2.2.1.6).
2. Still inside the same mutex hold, it opens a new `PagedFile` over the same handle, so the mutex the handle carries stays held, and
   runs the adoption rule (2.2.1.6 R6). No other run unit's statement can run between the refusal and this decision.
3. If the adopted page 2 carries the generation the refused statement stamped, the statement landed (the engine never issues a
   generation twice, 2.2.1.6): it publishes its outcome and answers as a completed statement. Otherwise page 2 names the previous statement's trees (2.2.1.6 R1), nothing landed, and it answers 92.
4. If the reopen is itself refused, the session holds no store and the statement answers 93: its outcome is not known. The session's
   next statement opens the store under the mutex as a run unit's first statement does (§6 "The transaction", step 2).

So 92 always means no action, and a program that retries on 92 delivers exactly once. A program that retries on 93 can deliver twice,
because the refused statement may have landed. Both promises hold only up to the engine's durability contract (2.2.1.2): a later power
loss can still return the store to its last promotion. The devices' half is §6 "Devices".

**Devices.** A device is the file `<device>.out` in the MCS directory, and `DeviceEnds` records the length of each device's committed
output. A statement that transmits to a device:

1. writes each message positionally at the device's committed end, `DeviceEnds[device]`, and sets the file's length to the end of what
   it wrote, so any tail a dead or refused statement left past the committed end is overwritten or cut;
2. flushes the device to the medium (`RandomAccess.FlushToDisk`, the call `HandlePageMedium.Sync` makes);
3. sets `DeviceEnds[device]` to the new length in the same store statement;
4. completes the store statement.

A process death or a refusal before step 4 leaves the store naming the old end, and the next transmission to that device starts there.
The first statement of each run unit also cuts every device longer than its committed end, so a reader of a device file sees a dead
statement's tail only until then. Step 2 orders the device before page 2, so a power loss can never leave a committed end past the
device's durable bytes. The engine's durability contract (2.2.1.2) still holds: a power loss can return the store to its last promotion;
the devices are then cut to match on the next open, and the messages the lost statements took or transmitted are in the store again. A
device file shorter than its committed end was changed outside the MCS: the next transmission writes at the file's actual end and records
that. A transmission is O(1) in the device's size.

**The RECEIVE wait.** A RECEIVE without a NO DATA phrase that finds no data waits until data is made available (ISO/IEC 1989:2002 A.2.5.3
GR5 b). It waits on its bell, never on a timer.

- **The bell.** One listening AF_UNIX stream socket per run unit that has waited at least once (`MessageBell`), bound at
  `<S>bell/<r>` (§6 "Shared with the 2023 facility"), where `<r>` is the run id the session draws when it opens. It is bound before the run unit's first `Waits` row is written and stays bound until the run unit
  ends.
- **Waiting.** The RECEIVE's statement that finds nothing writes `Waits(key, me)` in that same statement, with the bell's full path and
  a fresh stamp, whether or not a row was there, and completes it: every wait is one page statement. The run unit then blocks in
  `Socket.Poll` on the bell, for the backstop interval or with no limit when it is infinite; the documentation says that for
  `SelectRead` it "returns true if Listen() has been called and a connection is pending" (Socket.Poll, learn.microsoft.com, read
  2026-11-04). That this holds for AF_UNIX on Windows is unverified, and `PlatformAssumptionTests` witnesses it. On a wake it accepts
  every pending connection without blocking (`Blocking = false`, until `Accept` answers `SocketError.WouldBlock`) and closes each
  without reading it, so a ringer that died between its connect and its write cannot hold the waiter. It then asks again in a new
  statement. A statement that finds the data deletes the run unit's `Waits` row in the same statement.
- **Ringing.** A statement that adds a message to a queue path collects the `Waits` rows of every prefix of that path (at most four range
  reads); one that adds a message to a terminal's `TerminalMessages` collects that terminal's rows; seeding collects the rows of the
  endpoints it deletes. After the statement releases the mutex it rings each collected run unit once, at the bell path its row stores:
  the path the waiter bound, never one the ringer derives from its own spelling of the MCS directory. A ring is a socket with
  `Blocking = false` that connects, writes one `McsFrame` of kind `Ring`, and closes. The ringer never blocks on a waiter. On Linux,
  connect(2) answers `EAGAIN` "For nonblocking UNIX domain sockets, the socket is nonblocking, and the connection cannot be completed
  immediately" (man7.org, read 2026-11-04), which a full listen backlog causes; .NET reports it as `SocketError.WouldBlock`. A connect
  that answers `WouldBlock`, and a write that would block, end the ring as delivered and keep the row: a waiter whose backlog is full
  already has connections pending and will wake. What a non-blocking AF_UNIX connect answers on Windows, for a full backlog and for a
  listener with room, is unverified, and `PlatformAssumptionTests` witnesses both.
- **Why no wake is lost.** The bell listens before the row exists; the row is committed before the waiter blocks; and any statement that
  adds data after that commit runs later under the mutex, reads the row and rings after its own commit. A ring that arrives before the
  waiter blocks waits in the listen backlog, so the next `Poll` returns at once. A statement that added data before the waiter's commit is
  one the waiter's own statement already saw.
- **A ring that cannot be delivered.** A ring whose connect answers `SocketError.ConnectionRefused`, or whose bell path names no file,
  is undeliverable. unix(7) gives `ECONNREFUSED` when "The remote address specified by connect(2) was not a listening socket"
  (man7.org, read 2026-11-04), which is what a dead run unit's leftover socket file answers on Linux. Whether a full backlog on Windows
  also answers `ConnectionRefused`, as TCP does, is unverified, so the death rule does not depend on the answer. The ringer deletes the
  waiter's row in its next statement, or in the session's closing statement when it makes no other, and only when the row still carries
  the stamp the ringer collected. An unchanged stamp means the waiter has not asked again since the ringer read the row: either it is
  dead and the row goes with it, or its backlog was full, so it has connections pending, will wake, ask again and write its row afresh.
  A changed stamp means it asked again since, and the row stays. Either way no wake is lost. A refusal from a live listener with room in
  its backlog would lose that one wake to the backstop, and `PlatformAssumptionTests` asserts that it never happens. A ringer never
  unlinks another run unit's socket file, since it cannot tell a dead listener from a full one everywhere; a dead run unit's file
  stays in its user's socket directory until `cobol mcs reset`. Any other failure keeps the row.
- **The backstop.** Each wait also ends after the backstop interval, `WISEOWL_COBOL_MCS_BACKSTOP` (seconds; 30 by default; `infinite`
  disables it for a run unit with a bell), and the RECEIVE then asks again. A backstop never decides an outcome; it is one more ask.
  Each backstop ask increments `Meta`'s `BackstopFires`, and one that finds data increments `BackstopFinds` too: a find is a lost or
  undelivered ring, so a broken bell shows in `cobol mcs status` instead of only as latency. The test suites run with the backstop
  infinite, and a lost wake there is a red, never a hang: the `IMessageBell` fake fails at once a wait that no ring it was scripted
  with can end, with no clock; and the process tests read every child's progress through `LiveProcess.ReadLine`
  (`tests/_shared/LiveProcess.cs`), which raises `HarnessNonObservationException` when no line comes within the one harness budget
  (`ObservationBudget.UnitMs`, `tests/_shared/ObservationBudget.cs`).
- **No bell.** A run unit that cannot bind its bell (`Socket.OSSupportsUnixDomainSockets` false, a bell path that
  `McsSocketDirectory.Fits(21)` refuses, a socket directory it may not write or one the resolution refuses) writes no `Waits` row, so no
  ringer ever tries it, and increments `Meta`'s `BellUnavailable` once. It waits on the backstop alone, at the configured interval, or
  30 seconds when the setting is `infinite`, because nothing else can end its wait. It answers correctly, later.
- **Test seam.** `MessageControlSession` takes an `IMessageBell` (bind, wait, ring), and the unit tests inject a fake that delivers
  rings in any order, duplicates them, drops them, refuses them, drives the backstop from a virtual clock, and fails a wait that nothing
  scripted can end.

**The run unit's half.** A portion released and not ended, the remainder of a message begun, and each input-output CD's first terminal live in `MessageControlSession`, on the `RunUnit`. ISO/IEC 1989:2002 14.5.10 rule 2 follows by
construction: "remaining portions of any partially-received messages are eliminated from the queue. Any portions of messages transferred from the run unit by a SEND statement, and not terminated by an EMI or EGI, are purged
from the system." A RECEIVE takes a message out of `QueueMessages` when it begins it, so a run unit that dies with a remainder loses the remainder, which that rule prescribes.
The session holds the run unit's `mcs.store` handle, its region-lock endpoint and its bell from its first statement until `RunUnit.Terminate`,
which ends the session after `Files.CloseAll`: the session's closing statement deletes its own `Waits` rows, the store closes under the mutex (the engine's promotion, `PagedFile.Close`), the endpoint
gives back every byte it holds, the bell closes and its socket file is unlinked. The handle is opened `FileShare.ReadWrite`; it is the
handle the engine's pages and the region locks travel through, never a lock in itself. The endpoint also holds `HostRegionLocks.McsPresenceByte`
shared for the session's life, so `cobol mcs reset` can tell whether any run unit has the MCS open.

**Shared with the 2023 facility.** `McsDirectory`, `McsSocketDirectory` (with the run id `<r>` and the path budget) and `McsFrame`, in
`src/WiseOwl.Cobol.Runtime/Mcs/`, are the whole of what the two facilities share, and DESIGN-message-control.md §6 is their one
definition: the canonical MCS directory and its identity `<h>`, the socket directory `<S>` with its modes and refusals, `NewRunId()`,
`Fits(tailBytes)` against `MaxPathBytes`, and the frame `u32 length | u8 kind | payload` with its kind `Ring`. This facility adds only
its own entries and what a refusal means here:

- **Its entries.** A bell is `<S>bell/<r>`, where `<r>` is the run id the session draws with `McsSocketDirectory.NewRunId()` when it
  opens. The entry adds 21 bytes to `<S>` (`bell/` and 16 hex digits), and the session asks `McsSocketDirectory.Fits(21)` before it
  binds. Under that `<S>` a bell's path is 70 bytes under `/run/user/1000`, 82 with a 10-digit uid, and 59 under
  macOS's `/tmp` with uid 501, within the per-OS limits DESIGN-message-control.md §6 gives (107 bytes on Linux, 103 on
  macOS, Windows pinned by `PlatformAssumptionTests` as `MaxPathBytes`).
- **A refusal.** A socket directory the resolution refuses, or a bell that does not fit, is "No bell" (§6 "The RECEIVE wait"): the run
  unit answers correctly, later, and never fails a statement for it.
- **What does not depend on the identity.** The store mutex is named from the store's own directory (§6 "The MCS directory"), and a
  ringer connects to the bell path its waiter stored in `Waits` (§6 "The RECEIVE wait"), so no delivery of this facility depends on two
  run units computing the same `<h>`.

**Operator commands.** `cobol mcs status [--mcs <directory>]` and `cobol mcs reset [--mcs <directory>]`, a third verb tree beside the
compile and `check-batch` (`Program.Parse` chooses the tree by the first word). The CLI calls a public entry of the runtime,
`McsOperator`, on the `RepositoryIndex` precedent, because the runtime grants the CLI no internals.

- `status` reads the store under the mutex and prints its identity, the definitions' hash and whether `mcs.json` still matches it, every
  endpoint's enabled state, every queue path's count, each destination's `Outbound` count, each device's committed end beside the file's
  length, every `Waits` row with its run id, stamp, bell path and whether that file exists, the three bell counters, and the lock
  host and whether it answers `Unavailable`. It rings no bell. `McsOperator.Status` returns these as a `McsStatus` record, which the
  CLI prints and the tests read.
- `reset` holds the store mutex and then `McsPresenceByte` exclusively, both without waiting, and refuses with a nonzero exit code when
  either is held: a run unit has the MCS open or is inside a statement. Otherwise it recreates `mcs.store` empty through the engine's
  creation (`PagedFile.Create`, the one truncation) and unlinks the leftover bells: every bell path the old store's `Waits` rows name,
  read before the recreation, and every file in its own socket directory's `bell/`. The next statement seeds the store
  from `mcs.json`. Device files are output already delivered and are left as they are.

**Scheduling.** A process that starts the run unit on the MCS's behalf sets `WISEOWL_COBOL_MCS_SCHEDULED`, the levels of the queue separated by `/` or the terminal's name; the INITIAL CD then holds those names (GR6, GR28) and an
input-output CD's terminal is fixed from the start (GR30). Unset, the names are spaces.

**Choices the rules leave.** A level not named at RECEIVE is chosen alphabetically, a level's messages first in, first out (E.5.5.3, E.5.5.4). Data is "not made available" when the designated structure holds no complete message (B.1 item 116).
A disabled queue still answers RECEIVE: DISABLE deactivates "the logical paths between the queue ... and all the associated enabled sources" (A.2.2.3 GR3), and CCVS-85 CM105M reads after disabling. A complete message sent to a destination
goes, in the SEND's own statement, to the device, to the terminal, or to the queue the destination feeds; while the destination, the input queue it feeds, or its source's path is disabled it waits in `Outbound`, and it moves on in the
statement that opens the path (E.5.5.1). The endpoints whose paths an ENABLE opens are found from the definitions in memory, and only their `Outbound` rows are read. An error key is written '0' for a destination with no error (Table A.2, and the CM102M
report's `00/0`). Status 80 appears only for destinations, the one place one statement names several. Status 9x (B.1 item 28): 91 an unusable `mcs.json` and 92 a store the host refuses or a host whose locks answer `Unavailable`, each with no action; 93 a refused statement whose outcome the store could not decide (§6 "Status 92 takes no action"). With 9x a RECEIVE passes control as if data had been made
available. No alphabetic error key is defined (B.1 item 27). ADVANCING is §4 above.

## 7. Witnesses

- `conformance:cases/_unassigned/pb4677_cd_area_layout`, `pb4677_initial_cd_spaces`, `pb4677_user_words_from_2014`: what any MCS answers.
- `conformance:wiseowl/cases/_unassigned/pb4677_mcs_receive`, `pb4677_mcs_send`, `pb4677_mcs_paths`, `pb4677_mcs_key_85`, `pb4677_mcs_transaction`, over the shared `mcs.json` beside them.
- The refusals `wiseowl/diagnostics/pb4677-*`: each rule above, one program each.
- `unit:MessageControlTests`: statuses 91, 92 and 93, no action on each statement a store refuses, a refused device, a message across run units, a RECEIVE waiting on another run unit, GR30 and 14.5.10 rule 2, and:
  - **the region-lock refusal:** under `HostRegionLocks.Forcing` with a host whose every operation answers `Unavailable`, every statement kind answers 92, takes no action, and leaves `mcs.store` and every device byte for byte as it was;
  - **seeding:** a changed `mcs.json` keeps a kept endpoint's state and messages, seeds a new one once, and deletes a removed one with its counts and its `Waits` rows; a run unit blocked on the removed queue is rung, asks again, and answers 20;
  - **the wake protocol** over the `IMessageBell` fake. With the backstop infinite: rings delivered before the block, after it, twice, and out of order all complete the RECEIVE; a ring refused for a dead waiter deletes its `Waits` row; a ring refused while the waiter's row carries a newer stamp keeps it; a ring refused for a live waiter whose backlog is full deletes the row, and the waiter's next ask writes it again and a later SEND completes the RECEIVE; a wait nothing scripted can end fails the test at once. With a finite backstop on the virtual clock: a dropped ring completes the RECEIVE at the backstop and increments `BackstopFinds`; a run unit with no bell writes no `Waits` row, waits on a 30-second backstop although the setting is `infinite`, and increments `BellUnavailable` once.
- `unit:McsStoreCrashMatrixTests`: every statement kind (ENABLE and DISABLE of each path kind, SEND to a queue, a terminal, a device, `Outbound` and several destinations at once, the RECEIVE take, an ENABLE that moves `Outbound`, seeding, reset) on `DyingPageMedium`, with the run unit killed after every page write it hands over, and the device killed before its write, after it, and after its flush. A fresh run unit then opens the store and the test asserts: no message lost and none duplicated, every `QueueCounts` and `OutboundCounts` row equal to a recount, every device equal to its committed content, and the `Waits` rows consistent. The same matrix runs with the medium's refusal at every write and every sync: each statement answers 92 with no action, or answers as completed when the in-hold reopen finds that it landed, or answers 93 when the reopen is refused too; it never answers 92 for a statement that landed. It runs once more with the medium's power loss (the store at its last promotion, the devices cut to match). The medium enters through the engine's one medium seam (§8).
- `unit:PlatformAssumptionTests`, on both CI operating systems, landed whole by PB4700's slice S0 (DESIGN-message-control.md §7.2 lists the class). This facility relies on these cases: a stale AF_UNIX socket file answers `ConnectionRefused`; a bind over an existing file fails; `MaxPathBytes` is the longest path that binds, and one byte more fails; `Socket.Poll` on a listening AF_UNIX socket returns at a pending connection; a non-blocking connect to a listener with room completes or answers `WouldBlock` and never `ConnectionRefused`; a non-blocking connect to a listener whose backlog is full answers `WouldBlock` (as connect(2) gives for Linux) or `ConnectionRefused`, and the test records which, since §6 "A ring that cannot be delivered" holds for both. The 2023 transport's death witnesses live in the same class.
- `unit:McsResolutionTests`: the shared resolutions and frame, as DESIGN-message-control.md §6 defines them and its §7.2 lists the cases; this facility adds that `McsSocketDirectory.Fits(21)` holds for a bell under the `<S>` of each OS, and, on every host, that a bell's path under the `<S>` macOS resolves (`/tmp`, a 10-digit uid) is within 103 bytes.
- `conformance:CommunicationBellProcessTests`: two COBOL programs, A and B, run as `LiveProcess` children over one MCS directory with the backstop infinite, and the test orders every step by a message or by an observed `Waits` row, never by a delay. A RECEIVEs on queue QA without NO DATA. The test polls `McsOperator.Status` until A's `Waits` row on QA appears, then sends B a line; B SENDs to QA through `deliverTo`, and A's RECEIVE completes and DISPLAYs the message: the bell from B to A. B then RECEIVEs on QB without NO DATA. The test polls until B's `Waits` row on QB appears, then sends A a line; A SENDs to QB, and B's RECEIVE completes and DISPLAYs it: the bell from A to B. Each RECEIVE began waiting before its message was sent, so each completion is a ring delivered across processes. A third case kills a waiting receiver after its row appears, sends to its queue, and asserts that `McsOperator.Status` then shows no `Waits` row for it. Every DISPLAY is read through `LiveProcess.ReadLine` and every poll is bounded by `ObservationBudget.UnitMs`, so a lost wake is a `HarnessNonObservationException`.
- `unit:McsOperatorTests`: `status` over a known store; `reset` refused while another run unit holds `McsPresenceByte`, and an empty store reseeded after it with the old store's bells unlinked.

- `conformance:CommunicationCcvsTests`: all nine CM programs compile at COBOL-85; CM102M, CM103M, CM105M and CM201M run against `tests/nist/data/mcs.json` with no CCVS failure. CM101M, CM104M and CM202M wait out
  wall-clock delays and an operator, and CM303M and CM401M test the federal flagging layer, outside the ISO suite (PB4614 decision 11).

The programs sit in `_unassigned` because the traceability inventory derives from the 2023 text, which has no rows for this facility. Placing them under a clause waits for the inventory to hold pre-2023 rules (PB4614's design revision).

## 8. The store engine's statement entry without a connector

DESIGN-runtime-library.md §2.2.1 gains this as a new subsection after 2.2.1.18. The engine today has one client: `KeyedStore`, whose
`RelativeStore` and `IndexedStore` views run inside a file connector. The MCS is a second client with no connector, no record frames, no
FD and no transactions. What it needs from the engine, and the shape that gives it without a second path:

| Need | Where the engine stands | The change |
|---|---|---|
| A statement that edits trees in memory and lands them as one page statement | `KeyedStore.Change` does it, interleaved with the UNDO and HOLDINGS work of commit control, and is `private protected` to `KeyedStore` | Move the transaction-free core into `Store/StoreStatement.cs`: `TreeChange` and `Edit` move with it, and its `Complete(PagedFile file, IReadOnlyList<TreeChange> trees, ...)` sizes the statement (`StatementBound.Of`), begins it (`PagedFile.Begin`), flushes every tree, sets every root, completes it with page 2 last, and discards every pending edit on any refusal. `KeyedStore.Change` keeps its transaction steps (the draw, `Holdings`, the give-back) and calls the core for the rest. One sequence for both clients. |
| A store that is not a file of §9.1.6's organizations | `MetaPage` encodes the organization byte as `R` or `I`, decodes nothing else, and `PagedFile.Create` takes `FixedFileAttributes`, whose `Organization` carries exactly §9.1.6's three values | The meta page's organization byte becomes the store's KIND: `R`, `I` and `M`, the message store. `PagedFile.Create` and `Open` take a `StoreKind` with the attributes it has: a file kind carries its `FixedFileAttributes`, the message store none. `MetaPage` carries the `StoreKind` in place of its `FixedFileAttributes`. An `M` page writes, at the offsets a file's page uses: the organization byte (offset 40) `M`, the record-type byte (41) `F`, the minimum (42) and maximum (46) record sizes 0, and the key count (50) 0, so it has no key descriptors and no alternate roots; every other field is written as for a file. `MetaPage.Decode` gains one branch: organization `M` with record type `F`, both sizes 0 and key count 0 decodes as the message store, and `M` with any other value in those four fields is a slot that does not validate (null), as an unknown organization byte is today. `MetaPage.Describes` answers `StoreFormat.Foreign` for a committed `M` slot, so a COBOL OPEN answers '39' (§14.9.27.4 GR10's conflict), never a misread. `PagedFile.Open` for the message store refuses a committed `R` or `I` slot (a keyed file someone put at `mcs.store`), and the MCS answers it with 92 until `cobol mcs reset`. `FixedFileAttributes` keeps its three organizations. No format version change: a build before this one decodes an `M` slot as no slot, finds the magic, and answers '30' (`StoreFormat.Damaged`), and nothing reads an older build's files differently. |
| One medium seam for both clients' fault-injecting tests | `KeyedStore.MediumObserver`, an `AsyncLocal` on `KeyedStore` | Move it to `Store/PageMediumObserver`, read by `KeyedStore`'s constructor and by `McsStore`'s, so `DyingPageMedium` and the refusing medium reach both through one seam. Every reference moves with it, the test fixtures' included (§8 "Size"); no alias of the old name stays. |
| A new `PagedFile` over a handle whose last one was refused | `PagedFile` is opened once per connector OPEN; after a refusal the connector stays in permanent error until CLOSE | Nothing new: the MCS's in-hold reopen (§6 "Status 92 takes no action") is `PagedFile.Open` over the same `HandlePageMedium`, which runs the adoption rule (2.2.1.6 R6) as an OPEN does. The session compares the adopted page 2's generation with the one the refused statement stamped; a new engine test asserts that they are equal exactly when the statement landed, at every refusal point of 2.2.1.16. |
| The mutex's answer, not only whether it is held | `HostRegionLocks.HoldStoreMutex` returns the endpoint or null, folding `Unavailable` into "no protection" | It returns the `RegionLockResult`. `KeyedConnector` and `KeyedConnector.DeadOwners` keep reading anything but `Ok` as "no protection" (DOC-A.1-75); `InStoreMutex` keeps its behaviour; the MCS reads `Unavailable` as 92. Still the one way the mutex is taken. |
| A byte that says "a run unit has this store open" | the region map has `StoreMutexByte`, `LockPresenceByte` and `EntryMutexByte` | Add `McsPresenceByte = RegionBase + 0x103`, held shared by every open MCS session and tested exclusively by `cobol mcs reset`. |

What the MCS does NOT need, and does not get: `RunUnitPageMedium` (its one handle is a `HandlePageMedium`), record frames, release
ordinals (it passes none), alternate trees, the TRANSACTIONS and HOLDINGS trees, and the dead-owner step. Its holdings order is
`KeyOrders.Ordinal` and no holdings tree is ever created. Promotion stays the engine's: the ledger checkpoint and the generation ceiling
(2.2.1.6 R2, R5) inside `PagedFile.Begin`, and `PagedFile.Close` at the session's end.

**Size.** The estimate, from the code read: about 200 lines change in `IO/Store`, `IO/KeyedStore.cs` and `IO/Sharing/HostRegionLocks.cs`.
About 90 of them move out of `KeyedStore.Change` into `StoreStatement`; `MetaPage` and `PagedFile` gain about 50 for the kind; the
mutex's result and the presence byte touch about 15 lines at three call sites (`HostRegionLocks.cs`, `KeyedConnector.cs`,
`KeyedConnector.DeadOwners.cs`). The seam move is larger than either: on origin/main (485c15326) `MediumObserver` appears on 152 lines
in 19 files: 3 lines in 2 `src` files (`KeyedStore.cs`, `RunUnitFileLock.cs`) and 149 in 17 test files, `CommitRollbackRuntimeTests` (38),
`IndexedEngineTests` (29), `RelativeEngineTests` (27), `StoreCoherenceTests` (13) and `FileBoundaryTests` (8) the largest, and the
fault-injecting media `DyingPageMedium`, `RefusingPageMedium` and `PageArrayMedium` among them. Every one changes to
`PageMediumObserver` in the same slice, a mechanical rename best done with a Roslyn rewrite, so the slice changes the test fixtures as
well as the engine: about 350 lines in all. The page format, the six rules of 2.2.1.6 and
every crash point of 2.2.1.16 are unchanged, so `PagedFileCrashMatrixTests`, `TransactionFaultMatrixTests` and the keyed stores'
drift tests must stay green untouched, and they are the slice's behaviour-neutrality oracle. New engine tests: an `M` store is `Foreign`
to `Describes`, and the core completes, refuses and discards for a client with no transaction. `McsStore` itself, on top, is about 350
lines: the codecs, the tag tables, open and seed, the statement, the devices.

## 9. Slices

Ordered; each lands alone with its doc changes, goldens and tests, and is one kb/Work note.

1. (kb/Work PB4734) **The engine's statement entry (§8).** `StoreStatement`, the store kind, `PageMediumObserver`, `HoldStoreMutex`'s result,
   `McsPresenceByte`, and the subsection of DESIGN-runtime-library.md §2.2.1, with the seam's rename through the 17 test files that
   use it. Behaviour-neutral for keyed files. About 350 lines, 150 of them the rename.
2. (kb/Work PB4735) **The shared resolutions and frame.** `McsDirectory`, `McsSocketDirectory` and `McsFrame` as DESIGN-message-control.md §6 defines them,
   the frame with the kind `Ring` only (S2a adds the 2023 kinds); `MessageStore.Open` resolves its directory through `McsDirectory.Resolve(null)`;
   `unit:McsResolutionTests`. It needs PB4700's slice S0, which lands `PlatformAssumptionTests` and pins `MaxPathBytes`, and not
   slice 1. The 2023 transport's slice S2a depends on this one. About 150 lines.
3. (kb/Work PB4736) **The paged MCS store.** `McsStore`; `MessageStore.Run` as one engine statement under the mutex, with `Unavailable` refused as 92;
   seeding; `MessageStoreState`'s finders, takes and deliveries over the tag tables, and `StoredMessage`'s binary codec; devices
   positioned at `DeviceEnds`; the session's handle, endpoint and presence byte, ended by `RunUnit.Terminate`. It deletes `LockWait`,
   `AcquireLock`, `mcs.lock`, `mcs-state.json`, `Commit`'s state file, `TakeBack`, and `MessageStoreState`'s `ToJson`, `Overlay`,
   `Touch`, `Dirty` and `Empty`, with every caller. Witnesses: the region-lock refusal, seeding and `McsStoreCrashMatrixTests`; the CCVS
   and `pb4677_mcs_*` goldens unchanged. The in-hold reopen after a refusal and status 93. docs/CONFORMANCE.md's 9x determination gains the `Unavailable`
   host and 93, and so does the user guide's status paragraph (`docs/manual/user-guide/communication.md`; 2002 B.1 item 28 requires
   a provided 9x value in the user documentation), whose "a program can repeat the statement" then holds for 91 and 92 only. Slice 3
   needs slices 1 and 2. About 600 lines in and 450 out.
4. (kb/Work PB4737) **The bell.** `MessageBell`, `IMessageBell`, `Waits` with its bell path and stamp, the non-blocking ring after release (seeding's
   included), the stamped death rule, the backstop and its counters, and the no-bell path. It deletes
   `MessageControlSession`'s `awaitData` parameter, `WaitInterval` and the `Thread.Sleep` branch. Witnesses: the wake-protocol unit
   cases and `CommunicationBellProcessTests`. About 250 lines.
5. (kb/Work PB4738) **The operator commands.** `McsOperator`, the `mcs` verb tree, `unit:McsOperatorTests`, and the CLI's user documentation. About 250
   lines.
