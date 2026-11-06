## SetBinder: SET Format 17 below 2023 binds to a no-op, so the COBOL0900 gate is the only diagnostic (PB4771)

PB4771 (wave 20261879, group H). PB4740 gave `BindMessageTagSet` real SR35 class refusals and dropped its below-2023
`BoundNop`. Re-probe on this build at `--std 2014`: `message-tag-refused-set-numeric-receiver.cob` and
`...-sender.cob` each drew COBOL0869 beside the entry's COBOL0900, against DESIGN-message-control.md section 7.1 ("no
second one may appear").

Fix: `BindMessageTagSet` returns `BoundNop` when `ConstructRegistry.IsAvailable(edition, Constructs.UsageMessageTag2023)`
is false (the pre-PB4740 shape); the operand's gate fails the compile. Witness: `MessageTagIntroductionGateTests.
SetFormat17Refusals_AreTheGateAloneBelowTheIntroduction` (ten rows: numeric receiver and sender, two receivers, a literal
sender, at 85, 2002, 2014; at 2023 each draws COBOL0869 and no gate). No inventory row is claimed or closed.

Not fixed (outside the group, reported as a lead): the other PB4740 class refusals (MOVE COBOL0809, DISPLAY COBOL1694 and
the rest of the `message-tag-refused-*` programs) draw the same second diagnostic at 2014.
