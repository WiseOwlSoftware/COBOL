## PB5928: a far pointer's slot-image entry is held strongly, so a garbage collection no longer turns its token foreign

A full Unit gate failed `SlotImageRoundTripTests.DataPointerOf_APointerDisplacedBeyondTheWindow_ReadsBackAsItself_NeverAsAnotherArea` with
"Expected CellPointer, Actual ForeignAddress"; the class passed alone. The suspected cause, process-wide pointer-image bases shared by parallel tests,
is the documented determination (DOC-A.1-216, §2023:13.18.60.4 GR23 leaves the representation to the implementor) and could only weaken the test's
aliasing check, not change the decoded type. The real cause was in the runtime: `SlotImageTable.Note` held every entry other than a `CellLife` or a
`NamedAddress` as a `WeakReference`, and PB4925's `FarDisplacement`, a record nothing else references, fell into that arm. Any garbage collection
between WRITE and READ dropped it, and a far pointer read back from a file as a foreign address. The parallel test that calls `GC.Collect` made the
gate hit it.

The fix is structural. Every entry the codec makes derives from `PointerImage.ImagedEntry`, held strongly and swept when its `Ended` says its storage
life has ended. The weak hold is its own method, `NoteIdentity`, for an addressless pointer's identity. A new test images a far pointer, collects, and
decodes: before the fix it failed alone with the gate's exact message, and after the fix it passes. The original test now reads B's displacement from
the two images instead of assuming B's base is A's next. The sibling sweep found no other weak hold and no other test that depends on a process-wide
counter's next value (kb/Work PB5928).
