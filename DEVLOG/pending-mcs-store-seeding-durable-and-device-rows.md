## MCS store: the seeding is promoted (PB4862) and a device the definitions drop loses its DeviceEnds row (PB4863)

Wave 20261887 group C, one root: `McsStore.Prepare`, the run unit's first statement.

**PB4862.** The seeded `DeviceEnds` rows landed as a plain store statement, so a power loss returned the store to its last
promotion, without them, while `McsStore` and design section 6 said they were durable before any device was written. The
re-probe (`Seeding_IsPromoted_SoAPowerLossKeepsItsDeviceEndsRows`) read pages 0 to 2 and found the committed slot behind the
work slot. `Prepare` now calls `PagedFile.Promote` after the seeding lands. A refused promotion answers 92 with the seeding
landed (it has no action to undo; the next statement's writable open fences it), so the seeding row of
`HostRefusingEveryWriteFromTheNth_...` accepts either state on 92. Design section 6 "Devices" states it.

**PB4863.** Seeding never retired a `DeviceEnds` row and `known` compares Ordinal, so `dev` renamed `DEV` left a stale `dev`
row naming the same file on Windows and macOS; the next run unit's cut loop cut `DEV.out` to the old end. Seeding now retires
every row whose device the definitions do not name (`McsTables.RetireDeviceEnd`); the new name takes the file's length. The
file of a removed device is left alone. Witness: `DeviceRenamedOnlyByCase_LeavesNoStaleRow_AndItsDeliveredOutputIsNotCut`.

Both notes close no inventory row. Self-review: the McsStore, McsTables and McsStoreCrashMatrixTests unit population (141 tests) green.
