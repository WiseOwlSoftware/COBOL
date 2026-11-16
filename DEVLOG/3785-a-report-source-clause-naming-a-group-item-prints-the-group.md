## Entry 3785 — 2026-11-16 12:08 PST — A report SOURCE clause naming a group item prints the group's characters, as the MOVE of the same group stores them (PB5807)

**Defect.** `SOURCE WS-G` over a `PIC 999` printable item, WS-G a group holding `4X7`, printed `047` at every edition where `MOVE WS-G TO N3` gives `4X7`. Re-probed on this build before the change: the report file held `047` at `--std 85`.

**Cause.** SOURCE identifier-1 is the sender of an implicit MOVE (`§1985:XIII.3.18.3 SR2`; `§2023:13.18.53.4 GR1`), and a move with a group sender is an alphanumeric-to-alphanumeric move with no conversion (`§1985:VI.6.19.4 GR5`; `§2023:14.9.25.4 GR4`). `ReportWriterEmitter.TransferImage` sent every identifier SOURCE to `MoveEmitter.ConvertSource`, the elementary-sender conversion, and never asked `MoveClassifier.IsGroupSender`, the one test the MOVE statement dispatches on.

**Fix.** `TransferImage` asks `MoveClassifier.IsGroupSender` and takes the new `MoveEmitter.GroupSenderImage`: the sending group's storage image (`OperandText.NonElementaryMoveSender`) fitted to the printable item's area and decoded by `ReceivingStore.StorageCharacters`, the same two steps `EmitGroupToElementaryMove` runs before its store. A printable item has no storage, so the characters are the printed field. Sibling sweep: the other callers of `ConvertSource` are the SUM counter and computed-expression arms of `TransferImage` (elementary senders by construction) and the OO formal-argument store in `OoEmitter` (a CALL/INVOKE argument, outside this change; filed as a lead in the report).

**Golden.** `conformance-suite/cases/13.18.53-SOURCE/pb5807_report_source_group_sender` (accepted at 85, 2002, 2014, 2023; no negative, the rule exists at 1985): a numeric, a numeric-edited, a wider and a narrower printable item over `4X7`, and a two-leaf group; expected line `4X7 4X7 4X7   4X 12AB`. With the fix disabled the same program prints `047  47 00047 4X 0012`.

**Rows.** `GR-13.18.53.4-1` PARTIAL to CONFORMS (GAP 20 to 19). The 1985 row `1985-SR-XIII.3.18.3-2` keeps its stored code read; its row follows the 2023 row it is KEPT to.

