## PB5928: every slot-image entry is held until what it names ends, so a garbage collection no longer turns a token foreign

A full Unit gate failed `SlotImageRoundTripTests.DataPointerOf_APointerDisplacedBeyondTheWindow_ReadsBackAsItself_NeverAsAnotherArea` with
"Expected CellPointer, Actual ForeignAddress", though the class passed alone. The suspected cause was the process-wide pointer-image bases shared by
parallel tests. Those bases are the documented determination: DOC-A.1-216 records it, and §2023:13.18.60.4 GR23 leaves the representation to the
implementor. They could only weaken the test's aliasing check; they could not change the decoded type.

The real cause was in the runtime. `SlotImageTable.Note` held every entry other than a `CellLife` or a `NamedAddress` as a `WeakReference`.
PB4925's `FarDisplacement` is a record that nothing else references, so it fell into that arm. A garbage collection between WRITE and READ dropped it,
and the far pointer read back as a foreign address.

Review round 1 found the same mechanism in the arm the first fix kept weak. That arm held an addressless identity, such as a saved-locale handle. The
reviewer's probe runs `SET P TO LOCALE LC_ALL`, `WRITE`, `SET P TO NULL`, then a collection, then `READ` and `SET LOCALE LC_ALL TO P`, and it ended the
run fatally with EC-LOCALE-INVALID-PTR. A token on a file means the program is still holding the reference. So the table now has no weak path:
- Every entry is a `PointerImage.ImagedEntry`.
- An addressless value is an `Identity` that lives as long as its run unit.
- A cell life or far displacement is swept when its life ends.

The same round bounded the process-wide per-cell base memo, which kept one base per life of a cell and scanned that list linearly. It is now hashed,
and it drops ended lives the first time the cell is imaged in its current life.

Five new tests cover the two collection cases (far pointer, saved-locale handle, addressless carrier) and the bounded growth (100000 lives hold one
base). Each failed against the code it replaces and passes now. The far-pointer test now reads B's displacement from the two images instead of
assuming adjacent bases (kb/Work PB5928).
