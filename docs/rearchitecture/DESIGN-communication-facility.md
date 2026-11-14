# The communication facility (COBOL-85 and COBOL-2002)

Status: §1 to §5 IMPLEMENTED (kb/Work PB4677, owner decision 12 of PB4614, 2026-11-04). §6's message control system DESIGNED on the
runtime's paged store and a socket bell (owner decision of 2026-11-04 on the PB4700 transports); §8 (the engine entry it needs) is IMPLEMENTED as slice 1 (kb/Work PB4734; DESIGN-runtime-library.md 2.2.1.19), and
§9 orders the slices that build the rest (kb/Work PB4735-PB4738). WiseOwl COBOL implements the facility at COBOL-85 and 2002, so the per-edition suites can witness
it and NIST-85's CM module runs.

The authority is ISO/IEC 1989:2002 Annex A (Communications facility), with E.5 as its concepts section, read from the 2002 post-ballot text
(`E:/COBOL-coord/specs-drafts/t2002.txt`, general formats rendered from `x2002/cobolv200112.pdf`). The 2002 text is outside the tracked
tree, so citations of it are not `cite.py`-checkable and are written `ISO/IEC 1989:2002 A.n`, never with the `§` of the 2023 text. The
1985 text is `specs/ANSI_COBOL_1985.md` (§1985:XIV), and CCVS-85's CM programs witness the COBOL-85 KEY phrase and status 40.

The facility is not the 2023 asynchronous messaging facility (§2023:14.9.31, §2023:14.9.38), which shares the words RECEIVE and SEND, and the two
differ in kind. This facility stores and forwards: a message outlives the run unit that sent it, so the MCS keeps it in a durable store.
The 2023 facility is a rendezvous: "The statement suspends execution until the message is received by the server run unit" (§2023:14.9.38.4
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
statement is named even where no section declares its cd-name. §2023:8.9 already reserved the facility's words at 85 and 2002 only, so each
new lexer token is a nameSlot row of `cobol-words.json`, gated by the reservation table, and is an ordinary user word from 2014
(`pb4677_user_words_from_2014`).

## 2. Grammar

`Grammar/Core/CobolCommunication.g4` holds `communicationSection`, `communicationDescriptionEntry` and the statements. The printed page
decides the optional words: SYMBOLIC, IS, FOR, TIMES, BY, WITH, ADVANCING, the MESSAGE of MESSAGE COUNT and the LINE of REPLACING LINE
carry no underline.

Two statements need the union with the 2023 forms. RECEIVE parts by the word after its operand (`GIVING` against `MESSAGE` or `SEGMENT`).
`SEND x FROM y` is the shape of both A.2.6.1 Format 1 and §2023:14.9.38.2, and only x's declaration can tell them apart: the `cdName` rule's action
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
- A cd-name is a user word of its own type (`UserWordKind.CdName`) and no data-name. The `cdName` rule does not ride `dataName`, whose FILLER is a reserved word (A.1.2.1, 8.3.1.1.1): a word
  §2023:8.9 reserves draws COBOL0901 through `reservedGatedWord` and `IsProvableUserWordPosition`, and FILLER or PROCEDURE draws COBOL5506. It qualifies an item of its entry (the file-cd-report-qualifier of 8.4.1.1.1), through the
  one qualifier walk. A cd-name written where a data item is required is "declared as a cd-name, not as a data item" (COBOL1639, `NonDataDeclarationKind.CdName`, ranked with the file-name, depth 0 because 8.4.5 keeps a cd-name to its own source element) and never "not defined"; X3.23-1985 `USE FOR DEBUGGING ON cd-name` is a debug trigger target (`docs/CONTROL_FLOW_DESIGN.md` D20, `docs/CONFORMANCE.md` D-DEBUG-DATA; kb/Work PB4713, PB4759): its section runs after the ENABLE, DISABLE, SEND, RECEIVE and ACCEPT MESSAGE COUNT of the cd-name.

Asked at the entry: SR2 (INITIAL once, and not with USING), SR4 and SR8 (some way of describing the area), SR6, SR10 and SR15 (unique data-names), and Annex A's "only in a program definition". `LevelNumberPass` asks 13.16.31 SR2 (no level 77) with its own `CommunicationRecord` arm (the FD set, the CD sentence and the 2002 citation; PB4717), and `ResolveRedefinesPass` 13.16.42
SR4 (no REDEFINES at level 1). A residence row of `ClausePlacementRules` lists the sections its sentence lists, so EXTERNAL, GLOBAL (2002 13.16.25.2 SR1 b) and BASED refuse a CD's record as COBOL2404. Asked once the forest is complete (`CheckCommunicationRecordsPass`): SR5 and SR14 (exactly 87 or 33
positions: at --std 85 the standard data format characters of §1985:IV.4.3.4, `ItemLength.StandardDataFormatCharacters`, so a COMP item counts its digits; from 2002 the storage width) and the VALUE rule (a condition-name entry is a VALUE clause: kb/Work PB5290). All are COBOL6169 with the rule quoted, the COBOL1720 precedent.

## 4. The statements

`CommunicationBinder` builds one `BoundCommunicationStatement` per statement, carrying `Cd.Area`'s place: they are one operation over one area, and the optional members are the operands
each format prints. COBOL6170 is a name that is no cd-name, or a cd-name of a format the statement's rule does not admit. COBOL6171 is an operand of the wrong class or category.

The operand rules by edition (kb/Work PB5278, PB5289, PB5082, PB5240, PB5384, PB5342):

| Operand | 85 | 2002 |
|---|---|---|
| RECEIVE identifier-1 | any data item (§1985:XIV.3.5.3 has SR1 alone) | category alphanumeric (A.2.5.2 SR2), asked of the reference: a reference-modified numeric item is alphanumeric (§2023:8.4.3.3.4 GR6) |
| SEND identifier-1 | any data item; a function-identifier names an alphanumeric function (§1985:XIV.3.6.3 SR6) | class alphanumeric (A.2.6.2 SR2, SR7) |
| SEND identifier-2 | a one-character unsigned integer: `PicInfo.IsIntegerDescription` with one digit position (P is not counted, §1985:VI.5.9.4 GR8); a LINAGE-COUNTER by its implicit description, one digit only for a one-digit page size (§1985:VII.1.3.7); never a reference-modified item (§1985:IV.4.3.8.4.4 GR5) | the same (SR3) |
| ADVANCING identifier-3 | an integer data item (`IntrinsicResultType.IsIntegerOperand`, the one integer classifier): never an index data item, an index-name, a reference-modified item, a function, a constant-name or a symbolic-character; a counter register by its implicit description | the same (SR4) |
| ADVANCING integer-1 | an integer, or the figurative ZERO with or without ALL | an integer or ZERO; ALL ZERO is COBOL0902 (§2002:8.3.1.2.5.2 SR1a, `FigurativeAssociation.GateAllZeroAsNumeric`, VCR Table 7 row 7.28) |
| ENABLE and DISABLE KEY | `sendingOperand`, so an alphanumeric function-identifier too (§1985:IV.4.3.8.3.3 SR3), "defined as alphanumeric" (§1985:XIV.3.2.3 SR4, §1985:XIV.3.3.3 SR4), asked of the reference as RECEIVE's is | removed (COBOL0902) |

`CommunicationBinder.Prints2002` is the binder's one edition read: it decides the 2002 class rules and which edition's rule every COBOL6170 and COBOL6171
message quotes. The two editions number and word the same rules differently (2002 inserted SEND's SR2, so SEND's 85 SR2 to SR6 are its SR3 to SR7, and 85
writes "must" where 2002 writes "shall"), so each rule is a `SyntaxRule` carrying both, and a message at `--std 85` quotes §1985:XIV.3, never 2002's A.2
(kb/Work PB5343; `CommunicationRuleCitationTests`).

The ADVANCING phrase's operands are screened and kept nowhere. The devices of this MCS are oriented to variable-length messages (A.2.6.3 GR2), so vertical positioning "is not
applicable" and "the MCS will ignore" it (GR12). REPLACING LINE follows.

The facility raises no exception condition: "exception handling [is] intentionally omitted from the obsolete communications facility" (Annex A NOTE; 14.5.12.1 NOTE 2).

## 5. Code generation

`CommunicationEmitter` renders one `MessageControl` call per statement. The area goes over through `OperandText.RecordAreaImage`, the one record-area channel, and comes back through
`SequentialIoEmitter.EmitImageInto` verbatim (other descriptions read the same storage). "The message control system (MCS) always references the record according to the data description
defined in" GR2, GR16 and GR24, so the runtime reads positions and a user's redefinitions never reach it. RECEIVE returns a `CommunicationReceipt`: the area, the receiving item's image, and
which phrase runs. A CD with INITIAL sets its names once per instance, before the first procedure division statement (GR6, GR28): the scheduling names go only to the program that establishes the run unit (`ModuleStack.RunningIsMain`), and a CALLed program's INITIAL CD is spaces (§1985:XIV.2.2.4 GR28 "In all other cases"; kb/Work PB5282). RECEIVE evaluates identifier-1's subscripts and reference modifiers once, before the MCS call, through `PlaceIdentification.Freeze` (§1985:IV.4.3.8; kb/Work PB5279).

RECEIVE hands the MCS the receiving item as `OperandText.RecordAreaImage(into, use: AreaUse.Receiving)`: a group that holds its own OCCURS DEPENDING object is a receiving item of its MAXIMUM length (§1985:VI.5.8.4 GR3b; GR3a, a DEPENDING item outside the group, keeps the current count), so a message or segment shorter than the group is left aligned with every later position of the group unchanged (§1985:XIV.3.5.4 GR8b, GR9b), the same sizing `PlaceRenderer.WriteGroupImage` applies to the store back (kb/Work PB5377).

SEND's text is measured and transferred in one unit at --std 85: "the size of an elementary data item or a group item is the number of characters in standard data format of the item" (§1985:IV.4.3.4), so text length is counted against `ItemLength.StandardDataFormatCharacters` for an elementary item and a group alike. An elementary binary or packed item is its digits, a signed one with the sign fused onto the last digit (`CobolStandardData.Characters`, `CobolNum.FormatStandardData`; the S is not counted, §1985:VI.5.9.4 GR8d), and a group holding such an item is its storage image with each of them replaced by its digits: `ItemLength.StandardLayout` states where they sit, `RuntimeApi.StandardDataImage` carries the layout, and `CobolStandardData.Image` composes the characters (kb/Work PB5378). From 2002 the CD's records are counted in storage positions (`CommunicationSectionBinder.CheckRecords`) and a group is its storage image; the 2002 text is not among the specs this repository carries, so the elementary arm there is unchanged.

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

Every member but `name` is optional. A name is 1 to 12 characters and a file-name component on every host, since a queue path and a device file are built from names: it holds no character `Path.GetInvalidFileNameChars` gives on either OS (the Windows set, so the colon that opens an NTFS alternate stream is refused on Linux too) and is no Windows device name (`CON`, `NUL`, `COM1`, `LPT¹` and the rest, with or without an extension, in any case), a screen that does not depend on the host (`MessageDefinitions.NameOf`); names are compared as system-names are, a lowercase letter equal to its uppercase (§1985:XIV.2.2.4 GR4, §1985:p.A-9), through the one comparer `McsNames`, so a CD's `q` designates the queue `Q` and a definition holding both is refused as a duplicate (kb/Work PB5284); queues nest to the four levels of E.5.5.4. `key` is the COBOL-85 password (a KEY phrase's value against it, status 40; an endpoint with
none admits any). `messages` enter a queue or a terminal once, when the store first holds that endpoint. `capacity` bounds a destination's waiting messages (status 65, error key 6) and is not negative (a negative one is refused, status 91). `deliverTo` makes a destination feed a queue path or
`terminal:<name>`, so one run unit talks to another; a destination without it transmits to its device, the file `<name>.out`, and a terminal's SEND does too. Those device-bearing names (the destinations without `deliverTo` and the terminals) are distinct without regard to case, on every host, since a case-folding file system would make `D1` and `d1` one file: a definition file that breaks this is refused (status 91, `MessageDefinitions.RefuseSharedDeviceFiles`).
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
| `L` | `Paths` | queue path, 0x00, source name | whether the logical path between the source and the queue is enabled, as a statement last set it |
| `M` | `QueueMessages` | queue path, 0x00, serial | `StoredMessage` in its binary codec: segments, end of group, date, time, source |
| `I` | `TerminalMessages` | terminal, 0x00, serial | `StoredMessage`: what a terminal has sent, for an input-output CD's RECEIVE |
| `N` | `QueueCounts` | queue path prefix | the complete messages under the prefix |
| `O` | `Outbound` | destination, 0x00, serial | `StoredMessage` waiting while the destination's path is closed |
| `P` | `OutboundCounts` | destination | the messages `Outbound` holds for it, for the capacity check |
| `D` | `DeviceEnds` | device | the committed byte length of `<device>.out` |
| `R` | `PartialReceives` | owner run unit id (8 bytes), serial (8 bytes) | the queue level (path) a message was taken from by a RECEIVE that returned only its first portion; the message stays counted by ACCEPT MESSAGE COUNT until its last portion is returned (§1985:XIV.3.1.4 GR1). The owner's id is the byte `HostRegionLocks.McsRunUnitLivenessByte`, held through its handle while its session lives, so a row whose owner is dead is dropped by the statement that finds it |
| `W` | `Waits` | `Q` and a queue path, or `T` and a terminal; 0x00; the waiter's run id `<r>` | the waiter's bell path, as the waiter bound it, and the wait's stamp, a serial drawn from `Meta`'s next serial when the row is written |

The key layout makes the rules of E.5.5.3 and E.5.5.4 the tree's own order. A node's own messages (path, 0x00, serial) sort before its
children's (path, a name, 0x01, ...), children sort by name, and serials sort first in, first out. So the next message of a designated
structure is the first `M` key at or after the structure's prefix: one seek. `QueueCounts` holds one row per prefix of every path that
holds a message, changed in the same statement as the message, so ACCEPT MESSAGE COUNT reads one row.

**Seeding.** `MessageStore.Open` reads `mcs.json` once per run unit and refuses unusable definitions with 91 (`MessageStore.OpenAt`,
`IsUnusable`). The first statement of a run unit compares the definitions' hash with `Meta`'s. A store that is new, or whose hash differs,
is seeded, landing as a store statement of its own before the first statement's action (§6 "Devices"): every defined endpoint gets its `Endpoints` row; an endpoint the store already held keeps its enabled state
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
   otherwise running the adoption rule (2.2.1.6 R6). A creation the host refuses part-way (`McsStore.EmptyUncreatedStore`) sets the file back to no bytes, under the same hold, so the
   pages it wrote, which name no committed meta, never refuse every later statement (best effort; a death mid-creation is the file system's).
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
`Unavailable` answers 92 and takes no action, and the MCS never runs unlocked. The session's run unit id is the same rule's other half: the session takes its liveness byte (`HostRegionLocks.McsRunUnitLivenessByte`, the owner of its `PartialReceives` rows) when it opens, drawing a random 60-bit id and redrawing a `Held` one, a collision, at most four times (`McsStore.DrawRunUnit`, kb/Work PB5364). A second `Held` in a row is no collision but a host that cannot tell a free byte from a held one: the lock-file host answers every non-waiting hold `Held` over a lock directory this user may enter but not list. A session whose draw ends without a byte (`Unavailable`, or `Held` every time) holds no id and answers 92 to every statement before it reads anything, since every other run unit would read the owner of a message it partly received as dead; the draw never retries without bound. The MCS never relies on a share-none open either. On Unix,
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
2. Still inside the same mutex hold, it opens a new READ-ONLY `PagedFile` (`writable: false`) over the same handle, so the mutex the
   handle carries stays held, and runs the adoption rule (2.2.1.6 R6) in memory, writing nothing. It must not be a writable open: that
   promotes at a fence above the stamp and retires, so its generation is never the stamp, even for a statement that landed. No other run unit's statement can run between the refusal and this decision.
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
The first statement of each run unit also cuts every device with a `DeviceEnds` row that is longer than its committed end, so a reader of a device file sees a dead
statement's tail only until then; the cut is best effort and opens a file only when it is longer, so a device the host refuses to open for write refuses no statement that does not transmit to it. Step 2 orders the device before page 2, so a power loss can never leave a committed end past the
device's durable bytes. The engine's durability contract (2.2.1.2) still holds: a power loss can return the store to its last promotion;
the devices are then cut to match on the next open, and the messages the lost statements took or transmitted are in the store again. A
device file shorter than its committed end was changed outside the MCS: the next transmission writes at the file's actual end and records
that. A transmission is O(1) in the device's size. Seeding lands as a store statement of its own, before the action and before any device is written, and gives every device the definitions name that has no `DeviceEnds`
row the length its file has as its committed end (device files are output already delivered, and a new store, `cobol mcs reset` or a new definition must not destroy it, §6 "Operator commands");
because that row is durable before the statement writes, a dead statement's tail is never taken for delivered output, and the first statement of the next run unit cuts it. Durable means a POWER LOSS keeps it: the landed seeding is PROMOTED (`PagedFile.Promote`, kb/Work PB4862), whose two syncs put it in the committed slot, so the store a power loss returns to either has the row or has seen no device written. A promotion the host refuses answers 92 with the seeding landed (it has no action to undo), and the next statement's writable open fences it durable before it writes a device. Seeding also RETIRES the `DeviceEnds` row of every device the definitions no longer name (kb/Work PB4863): the store claims exactly the files of the devices it defines, so the cut never touches a file that is no longer its own, and a device renamed only by case, which on a case-insensitive host is the same file, takes a new row at the file's length instead of keeping a stale end that would cut its delivered output. A device is cut to its committed end before its row retires, and its file is then left as it is. A run unit that began before the seeding still holds the old definitions and may transmit to a device that has no row: the file's length is its committed end, as in seeding, so what was delivered is appended to and never overwritten, and its send creates no row: on a case-insensitive host a spelling that differs only by case from a stored row is that row's file, so the send advances the stored row (`McsStore.CommittedRow`) and no second row claims the file. A statement that answers 92 also puts the devices it wrote to back to their committed ends, and deletes one
it created, best effort (`McsStore.CutBack`): the positional protocol above does not depend on it, and a host that refuses it leaves the tail only until the
next transmission or the next run unit's first statement. A statement that answers 93 or as completed leaves the devices as written.

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
  that answers `WouldBlock` or `NoBufferSpaceAvailable`, and a write that would block, end the ring as delivered and keep the row: a
  waiter whose backlog is full already has connections pending and will wake. On Windows a non-blocking AF_UNIX connect answers
  `WouldBlock` then completes for a listener with room, and `NoBufferSpaceAvailable` (WSAENOBUFS) for a full backlog
  (`PlatformAssumptionTests`, PB4739); a `WouldBlock` that completes is not a lost ring.
- **Why no wake is lost.** The bell listens before the row exists; the row is committed before the waiter blocks; and any statement that
  adds data after that commit runs later under the mutex, reads the row and rings after its own commit. A ring that arrives before the
  waiter blocks waits in the listen backlog, so the next `Poll` returns at once. A statement that added data before the waiter's commit is
  one the waiter's own statement already saw.
- **A ring that cannot be delivered.** A ring whose connect answers `SocketError.ConnectionRefused`, or whose bell path names no file,
  is undeliverable. unix(7) gives `ECONNREFUSED` when "The remote address specified by connect(2) was not a listening socket"
  (man7.org, read 2026-11-04), which is what a dead run unit's leftover socket file answers on Linux. A full backlog on Windows answers
  `NoBufferSpaceAvailable`, not `ConnectionRefused`, and the death rule does not depend on the answer. The ringer deletes the
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
from the system." A RECEIVE takes a message out of `QueueMessages` when it begins it, so a run unit that dies with a remainder loses the remainder, which that rule prescribes. The message still exists in the queue structure until its last portion is returned (§1985:XIV.3.1.4 GR1 counts the complete messages that exist in the queue structure; §1985:VI.6.24.4 GR3), so a RECEIVE that returns only its first portion writes a `PartialReceives` row (owner run unit, serial, queue level) in the statement that took the message, and the RECEIVE that returns the last portion deletes it. ACCEPT MESSAGE COUNT adds the rows under the queue structure the CD designates (`MessageStoreState.PartlyReceivedFrom`) for every run unit, the owner's and any other's. A run unit's id is random over 60 bits, and the session holds `HostRegionLocks.McsRunUnitLivenessByte(id)` exclusively through its handle for its life, so "the owner is dead" is one free byte (the dead-owner probe of a keyed store's transaction, DESIGN-runtime-library.md 2.2.1.10); a row whose owner is dead is dropped by the statement that finds it, never counted, which is STOP RUN's and 14.5.10 rule 2's removal of the remainder (kb/Work PB5339). A portion released and not ended is kept per occurrence of the DESTINATION TABLE
(the destination's name and which occurrence of that name), so a destination named twice is sent to twice, each message with a segment and message control of its own (kb/Work PB5288). The first statement of an input-output CD fixes its terminal
before any status is decided (GR31, kb/Work PB5283), and the content it fixes is the program instance's own: the generated program holds one `CdActivation` per CD and passes it to every statement, so a CANCELled program, each CALL of a program with the INITIAL attribute and two programs that name a CD alike start with no lock (§1985:X.5.3.4 GR2; kb/Work PB5381). The comparison is of content, exact, with no case folding; a refused statement (9x) lifts the lock it set (`Dialogue.UndoLocks`). A received message's symbolic source is written only when it names a source or a terminal of the definitions (GR10, `MessageStoreState.KnowsCommunicationTerminal`), else spaces.
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
**Logical paths.** ENABLE and DISABLE act on paths, not endpoints (§1985:XIV.3.2.4 GR3, GR4; §1985:XIV.3.3.4 GR2, GR3). A path is one (source, queue) pair of the definitions. The path is open when its `Paths` row says so, and with no row when the queue's and the source's own enabled states are both true; a destination whose source name is no defined source has no path, and its queue's own state decides. DISABLE and ENABLE INPUT write the queue named and every queue below it (a queue given by fewer levels than the CD has is a structure) and the row of each of them with every defined source; DISABLE and ENABLE INPUT TERMINAL write the source and the row of it with every queue of the definitions. A statement is answered 15 when every state it wrote was already the one asked for. The password is the named queue's or source's own. (`MessageStoreState.SetQueuePaths`, `SetSourcePaths`, `PathOpen`; kb/Work PB5286.) Seeding deletes a queue's or a source's rows with the endpoint. DISABLE and ENABLE I-O TERMINAL write the terminal's own enabled flag, which closes the terminal's link to the MCS and not the program's path to what the MCS already holds (§1985:XIV.3.2.4 GR1, GR5; §1985:XIV.3.3.4 GR4; kb/Work PB5107, PB5315): a SEND on it takes no action (status 10), a message bound for it stays in its destination's output queue until ENABLE (`MessageStoreState.Deliver`), and a RECEIVE still takes a message the terminal already holds (`TakeFromTerminal` asks nothing of the flag).
A disabled queue still answers RECEIVE: DISABLE deactivates "the logical paths between the queue ... and all the associated enabled sources" (A.2.2.3 GR3), and CCVS-85 CM105M reads after disabling. A complete message sent to a destination
goes, in the SEND's own statement, to the device, to the terminal, or to the queue the destination feeds; while the destination, or the path between its source and the input queue it feeds, is disabled it waits in `Outbound`, and it moves on in the
statement that opens the path (E.5.5.1). The endpoints whose paths an ENABLE opens are found from the definitions in memory, and only their `Outbound` rows are read. An error key is written '0' for a destination with no error (Table A.2, and the CM102M
report's `00/0`). Status 80 (§1985:B.2.2: conditions 15 and 20 both apply) appears for destinations, where one statement names several, and for ENABLE and DISABLE INPUT, where the queue levels the MCS knows are already in the requested state and a level below them is unknown (`MessageDefinitions.FindQueuePrefix`, `MessageStoreState.QueuePathsAre`; kb/Work PB5393); neither answer takes action. DISABLE and ENABLE OUTPUT judge each occurrence of a destination by the state it had when the statement began, so a destination named twice is not already disconnected by its second occurrence (kb/Work PB5376). Status 9x (B.1 item 28): 91 an unusable `mcs.json` and 92 a store the host refuses or a host whose locks answer `Unavailable`, each with no action; 93 a refused statement whose outcome the store could not decide (§6 "Status 92 takes no action"). With 9x a RECEIVE passes control as if data had been made
available. No alphabetic error key is defined (B.1 item 27). A statement refused before its destinations (status 40 of ENABLE and DISABLE OUTPUT, 50 and 60 of SEND) still writes each occurrence 1 to the count (§1985:XIV.2.2.4 GR22): 1 for an unknown destination, else Table 2's value for the destination as it is, else 0 (`SetErrorKeys`). ADVANCING is §4 above.

## 7. Witnesses

- `conformance:cases/_unassigned/pb4677_cd_area_layout`, `pb4677_initial_cd_spaces`, `pb4677_user_words_from_2014`: what any MCS answers.
- `conformance:wiseowl/cases/_unassigned/pb4677_mcs_receive`, `pb4677_mcs_send`, `pb4677_mcs_paths`, `pb4677_mcs_key_85`, `pb4677_mcs_transaction`, over the shared `mcs.json` beside them.
- The refusals `wiseowl/diagnostics/pb4677-*`: each rule above, one program each.
- `unit:MessageControlTests`: statuses 91, 92 and 93, no action on each statement a store refuses, a refused device, a message across run units, a RECEIVE waiting on another run unit, GR30 and 14.5.10 rule 2, and:
  - **the region-lock refusal:** under `HostRegionLocks.Forcing` with a host whose every operation answers `Unavailable`, every statement kind answers 92, takes no action, and leaves `mcs.store` and every device byte for byte as it was;
  - **the undrawable run unit id:** under `HostRegionLocks.Forcing` with a host that answers every non-waiting hold `Held` (an unlistable lock directory), the session's draw ends within its bound, every statement answers 92 with no action, and the files are left as they were;
  - **seeding:** a changed `mcs.json` keeps a kept endpoint's state and messages, seeds a new one once, and deletes a removed one with its counts and its `Waits` rows; a run unit blocked on the removed queue is rung, asks again, and answers 20;
  - **the wake protocol** over the `IMessageBell` fake. With the backstop infinite: rings delivered before the block, after it, twice, and out of order all complete the RECEIVE; a ring refused for a dead waiter deletes its `Waits` row; a ring refused while the waiter's row carries a newer stamp keeps it; a ring refused for a live waiter whose backlog is full deletes the row, and the waiter's next ask writes it again and a later SEND completes the RECEIVE; a wait nothing scripted can end fails the test at once. With a finite backstop on the virtual clock: a dropped ring completes the RECEIVE at the backstop and increments `BackstopFinds`; a run unit with no bell writes no `Waits` row, waits on a 30-second backstop although the setting is `infinite`, and increments `BellUnavailable` once.
- `unit:McsStoreCrashMatrixTests`: every statement kind (ENABLE and DISABLE of each path kind, SEND to a queue, a terminal, a device, `Outbound` and several destinations at once, the RECEIVE take, an ENABLE that moves `Outbound`, seeding, reset) on `DyingPageMedium`, with the run unit killed after every page write it hands over, and the device killed before its write, after it, and after its flush. A fresh run unit then opens the store and the test asserts: no message lost and none duplicated, every `QueueCounts` and `OutboundCounts` row equal to a recount, every device equal to its committed content, and the `Waits` rows consistent. The same matrix runs with the medium's refusal at every write and every sync: each statement answers 92 with no action, or answers as completed when the in-hold reopen finds that it landed, or answers 93 when the reopen is refused too; it never answers 92 for a statement that landed. It runs once more with the medium's power loss (the store at its last promotion, the devices cut to match). A first run unit on a store that does not exist runs the same matrix: killed after every page write of its creation, seeding and first transmissions, and with the medium's syncs alone refused from the nth on (the creation, the first seeding, the close); the next run unit always finds a usable store, true counts, and a device holding whole lines. The same refusal over an existing store holding messages (the fence of the writable open, every promotion, the close) keeps every message whole. The medium enters through the engine's one medium seam (§8).
- `unit:PlatformAssumptionTests`, on both CI operating systems, landed whole by PB4700's slice S0 (DESIGN-message-control.md §7.2 lists the class). This facility relies on these cases: a stale AF_UNIX socket file answers `ConnectionRefused`; a bind over an existing file fails; `MaxPathBytes` is the longest path that binds, and one byte more fails; `Socket.Poll` on a listening AF_UNIX socket returns at a pending connection; a non-blocking connect to a listener with room completes or answers `WouldBlock` and never `ConnectionRefused`; a non-blocking connect to a listener whose backlog is full answers `WouldBlock` (as connect(2) gives for Linux; counted only when the connect then fails to finish), `NoBufferSpaceAvailable` (Windows) or `ConnectionRefused`, and the test records which, since §6 "A ring that cannot be delivered" holds for both. The 2023 transport's death witnesses live in the same class.
- `unit:McsResolutionTests`: the shared resolutions and frame, as DESIGN-message-control.md §6 defines them and its §2023:7.2 lists the cases; this facility adds that `McsSocketDirectory.Fits(21)` holds for a bell under the `<S>` of each OS, and, on every host, that a bell's path under the `<S>` macOS resolves (`/tmp`, a 10-digit uid) is within 103 bytes.
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
| A statement that edits trees in memory and lands them as one page statement | `KeyedStore.Change` does it, interleaved with the UNDO and HOLDINGS work of commit control, and is `private protected` to `KeyedStore` | Move the transaction-free core into `Store/StatementLanding.cs` (the name is not `StoreStatement`, the connector's mutex hold): `TreeChange` and `Edit` move with it, and its `Complete(PagedFile file, IReadOnlyList<TreeChange> trees, ...)` sizes the statement (`StatementBound.Of`), begins it (`PagedFile.Begin`), flushes every tree, sets every root, completes it with page 2 last, and discards every pending edit on any refusal. `KeyedStore.Change` keeps its transaction steps (the draw, `Holdings`, the give-back) and calls the core for the rest. One sequence for both clients. |
| A store that is not a file of §2023:9.1.6's organizations | `MetaPage` encodes the organization byte as `R` or `I`, decodes nothing else, and `PagedFile.Create` takes `FixedFileAttributes`, whose `Organization` carries exactly §2023:9.1.6's three values | The meta page's organization byte becomes the store's KIND: `R`, `I` and `M`, the message store. `PagedFile.Create` takes a `StoreKind` with the attributes it has: a file kind carries its `FixedFileAttributes`, the message store none; `PagedFile.Open` takes `bool messageStore` and no kind, since the adopted meta names the kind and `Open` only checks the family (2.2.1.19). `MetaPage` carries the `StoreKind` in place of its `FixedFileAttributes`. An `M` page writes, at the offsets a file's page uses: the organization byte (offset 40) `M`, the record-type byte (41) `F`, the minimum (42) and maximum (46) record sizes 0, and the key count (50) 0, so it has no key descriptors and no alternate roots; every other field is written as for a file. `MetaPage.Decode` gains one branch: organization `M` with record type `F`, both sizes 0 and key count 0 decodes as the message store, and `M` with any other value in those four fields is a slot that does not validate (null), as an unknown organization byte is today. `MetaPage.Describes` answers `StoreFormat.Message` for a committed `M` slot (a keyed OPEN reads it as a conflict, and a sequential I-O or EXTEND OPEN as a paged store, as for `R` and `I`), so a COBOL OPEN answers '39' (§2023:14.9.27.4 GR10's conflict), never a misread. `PagedFile.Open` for the message store refuses a committed `R` or `I` slot (a keyed file someone put at `mcs.store`), and the MCS answers it with 92 until `cobol mcs reset`. `FixedFileAttributes` keeps its three organizations. No format version change: a build before this one decodes an `M` slot as no slot, finds the magic, and answers '30' (`StoreFormat.Damaged`), and nothing reads an older build's files differently. |
| One medium seam for both clients' fault-injecting tests | `KeyedStore.MediumObserver`, an `AsyncLocal` on `KeyedStore` | Move it to `Store/PageMediumObserver`, read by `KeyedStore`'s constructor and by `McsStore`'s, so `DyingPageMedium` and the refusing medium reach both through one seam. Every reference moves with it, the test fixtures' included (§8 "Size"); no alias of the old name stays. |
| A new `PagedFile` over a handle whose last one was refused | `PagedFile` is opened once per connector OPEN; after a refusal the connector stays in permanent error until CLOSE | Nothing new: the MCS's in-hold reopen (§6 "Status 92 takes no action") is `PagedFile.Open` over the same `HandlePageMedium`, which runs the adoption rule (2.2.1.6 R6) as an OPEN does. The session compares the adopted page 2's generation with the one the refused statement stamped; a new engine test asserts that they are equal exactly when the statement landed, at every refusal point of 2.2.1.16, and that a writable reopen never equals the stamp. The in-hold reopen that decides is read-only (`writable: false`); a session that goes on opens a second, writable `PagedFile` after the decision. |
| The mutex's answer, not only whether it is held | `HostRegionLocks.HoldStoreMutex` returns the endpoint or null, folding `Unavailable` into "no protection" | It returns the `RegionLockResult`. `KeyedConnector` and `KeyedConnector.DeadOwners` keep reading anything but `Ok` as "no protection" (DOC-A.1-75); `InStoreMutex` keeps its behaviour; the MCS reads `Unavailable` as 92. Still the one way the mutex is taken. |
| A byte that says "a run unit has this store open" | the region map has `StoreMutexByte`, `LockPresenceByte` and `EntryMutexByte` | Add `McsPresenceByte = RegionBase + 0x103`, held shared by every open MCS session and tested exclusively by `cobol mcs reset`. |

What the MCS does NOT need, and does not get: `RunUnitPageMedium` (its one handle is a `HandlePageMedium`), record frames, release
ordinals (it passes none), alternate trees, the TRANSACTIONS and HOLDINGS trees, and the dead-owner step. Its holdings order is
`KeyOrders.Ordinal` and no holdings tree is ever created. Promotion stays the engine's: the ledger checkpoint and the generation ceiling
(2.2.1.6 R2, R5) inside `PagedFile.Begin`, and `PagedFile.Close` at the session's end.

**Size.** The estimate, from the code read: about 200 lines change in `IO/Store`, `IO/KeyedStore.cs` and `IO/Sharing/HostRegionLocks.cs`.
About 90 of them move out of `KeyedStore.Change` into `StatementLanding`; `MetaPage` and `PagedFile` gain about 50 for the kind; the
mutex's result and the presence byte touch about 15 lines at three call sites (`HostRegionLocks.cs`, `KeyedConnector.cs`,
`KeyedConnector.DeadOwners.cs`). The seam move is larger than either: on origin/main (7ccb2822b) `MediumObserver` appears on 152 lines
in 19 files: 3 lines in 2 `src` files (`KeyedStore.cs`, `RunUnitFileLock.cs`) and 149 in 17 test files, `CommitRollbackRuntimeTests` (38),
`IndexedEngineTests` (29), `RelativeEngineTests` (27), `StoreCoherenceTests` (13) and `FileBoundaryTests` (8) the largest, and the
fault-injecting media `DyingPageMedium`, `RefusingPageMedium` and `PageArrayMedium` among them. Every one changes to
`PageMediumObserver` in the same slice, a mechanical rename best done with a Roslyn rewrite, so the slice changes the test fixtures as
well as the engine: about 350 lines in all. The page format, the six rules of 2.2.1.6 and
every crash point of 2.2.1.16 are unchanged, so `PagedFileCrashMatrixTests`, `TransactionFaultMatrixTests` and the keyed stores'
drift tests must stay green untouched, and they are the slice's behaviour-neutrality oracle. New engine tests: an `M` store is `Message`
to `Describes`, and the core completes, refuses and discards for a client with no transaction. `McsStore` itself, on top, is about 350
lines: the codecs, the tag tables, open and seed, the statement, the devices.

## 9. Slices

Ordered; each lands alone with its doc changes, goldens and tests, and is one kb/Work note.

1. (kb/Work PB4734) **The engine's statement entry (§8).** `StatementLanding`, the store kind, `PageMediumObserver`, `HoldStoreMutex`'s result,
   `McsPresenceByte`, and the subsection of DESIGN-runtime-library.md §2.2.1, with the seam's rename through the 17 test files that
   use it. Behaviour-neutral for keyed files. About 350 lines, 150 of them the rename.
2. (kb/Work PB4735) **The shared resolutions and frame.** `McsDirectory`, `McsSocketDirectory` and `McsFrame` as DESIGN-message-control.md §6 defines them,
   the frame with the kind `Ring` only (S2a adds the 2023 kinds); `MessageStore.Open` resolves its directory through `McsDirectory.Resolve(null)`;
   `unit:McsResolutionTests`. It needs PB4700's slice S0, which lands `PlatformAssumptionTests` and pins `MaxPathBytes`, and not
   slice 1. The 2023 transport's slice S2a depends on this one. About 150 lines.
3. (kb/Work PB4736) **The paged MCS store.** `McsStore`; `MessageStore.Run` as one engine statement under the mutex, with `Unavailable` refused as 92;
   seeding; `MessageStoreState`'s finders, takes and deliveries over the tag tables, and `StoredMessage`'s binary codec; devices
   positioned at `DeviceEnds`; the session's handle, endpoint and presence byte, ended by `RunUnit.Terminate`. It deletes `LockWait`,
   `AcquireLock`, `mcs.lock`, `mcs-state.json`, `Commit`'s state file, `TakeBack` (`McsStore.CutBack` is its best-effort successor, §6 "Devices"), and `MessageStoreState`'s `ToJson`, `Overlay`,
   `Touch`, `Dirty` and `Empty`, with every caller (`MessageStoreState.Undefined`, the state of an MCS nobody defined, reaches no table, and stands where `Empty` stood). Witnesses: the region-lock refusal, seeding and `McsStoreCrashMatrixTests`; the CCVS
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
