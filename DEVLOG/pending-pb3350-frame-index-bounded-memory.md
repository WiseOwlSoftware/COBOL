## PB3350: a RECORD VARYING file's frame index holds a checkpoint per 4096 records, not one offset per record

**The defect.** `RecordFraming.FrameStarts` added the start of every frame to a `List<long>`. `SequentialConnector` kept the
list for the life of the OPEN for a backward READ (`_varyingStarts`, §14.9.30.4 GR21 c)), which needs one offset at a time,
and `CountRecords` built the whole list only to take its `Count`. A hundred-million-record file held about 800 MB (twice
that transiently). Behaviour was right; the memory scaled with the file.

**Re-probe.** The note's three sites reproduced by reading the base tree (`FrameStarts` at RecordFraming.cs:207,
`_varyingStarts ??=` at SequentialConnector.cs:1689, `FrameStarts(...).Count` at :512). The new allocation witness
(`FrameIndexTests.AllocationAndMemory_DoNotGrowWithTheFile`) is the measure: over 2^20 frames the count allocates under 16 KB
and the index walk under 64 KB.

**The fix, from DESIGN-runtime-library.md 2.2.3.** `FrameStarts` is deleted (no forwarder). The count is
`RecordFraming.CountFrames`, the same `ReadStreamFrame` loop returning a number. The backward READ asks the new
`IO/FrameIndex`: a checkpoint per 4096 frames plus a window of at most 4096 exact starts, rebuilt by one forward walk of at
most 4096 frame heads when the position leaves its span. The forward walk feeds the checkpoints for free (`Observe` in
`NextFrame`); a target beyond the walked frames extends them by one counting walk (`Extend`), which is also the OPEN REVERSED
count, so REVERSED pays one pass where the count plus the list paid two. Memory is O(records / 4096 + 4096).

**Design correction (2.2.3 updated).** The design keyed the free checkpoints on the connector's `_readOrdinal`. START FIRST/LAST
walks frames without advancing it, so `Observe` tests byte-offset contiguity instead (a frame is new when it starts where
the walked frames end); a re-read after a backward step is then ignored for free.

**Siblings.** The fixed-width record-sequential arm computes offsets arithmetically and LINE SEQUENTIAL forbids PREVIOUS
(§14.9.30.3 SR7); neither holds a per-record collection, and `IO/` declares no other `List<long>`.

**Witnesses.** `FrameIndexTests` (Unit): the allocation bound over 2^20 frames, ⌈frames / stride⌉ checkpoints and a window of
at most the stride, every ordinal's start against the file at strides 1 to 64 asked forward, backward and scattered, a step
inside a span costing no frame-head read and a step into another costing one window build (a stream counting the payload seeks),
frames a sibling appended after the walk found when a target reaches them, and a structural scan for a per-record offset list.
Two goldens cross the span boundary with 4100 varying records: `14.9.30-READ/pb3350_read_previous_varying_span` (GR21 c and e;
the forward walk's checkpoints) and `_unassigned/pb3350_open_reversed_varying_span_85` (the counting walk's). The existing
PREVIOUS and REVERSED cases are the byte-identical net. Inventory rows: none claimed, none changed.

**Review round 1 (R1-1, fixed).** `FrameIndex.BuildWindow` overwrote the window's slots in place and assigned the span and count
only after the walk, so an `IOException` mid-walk (status 30) left the OLD span claimed over the NEW span's starts: the next
backward READ into the old span answered another record's start with status 00. It now invalidates the window before it writes
a slot. Pinned by `FrameIndexTests.AWindowBuildThatThrows_LeavesNoTornWindowBehind` (red without the fix, measured) and the
sibling arm `AnExtendThatThrows_ResumesFromTheLastWholeFrame`. The structural guard now asserts it found its three files and
scans the `List<long>` AND `long[]` members, allowing only the index's `_checkpoints` and `_window` (R1-2).
