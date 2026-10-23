## A MOVE into a RENAMES THROUGH alias no longer converts the numeric leaf it spans (PB3563)

**Defect.** `MOVE "XXXXXXX" TO R-AC` over `66 R-AC RENAMES R-A THROUGH R-C` with `R-A X(2)`, `R-B 9(3)`, `R-C X(2)` stored
`XX000XX`; the rule requires `XXXXXXX`. §13.18.45.4 2) makes the alias "an alphanumeric group item", and §14.9.25.4 4) makes a move
that is not elementary "an alphanumeric to alphanumeric elementary move, except that there is no conversion of data from one form
of internal representation to another" (both through `cite.py --check`). Four fuzz programs of run fuzz-20261022-163846
(fz03077, fz03775, fz04473, fz04474) found it.

**Re-probe.** Reproduced on origin/main 07bfc3606, and wider than the note: the shape where the program never names the numeric
leaf (`ren1`) was wrong too, so the leaf's being referenced by name was never the condition. The condition is the leaf's native
value carrier, which holds a decoded number and so turns `XXX` into `000`.

**Root.** `UsageCollectionPass.P` and `.Channel` asked the structural `DataItem.IsGroup`, false for a level-66 entry. Another
reader of the same question, `MoveClassifier.IsGroupPlace`, already answered `RenamesPlace => true` (PB430); the pass was the
reader that did not, and it is the one that decides which leaves store their character image.

**Fix.** `UsageCollectionPass.Visitor.AliasImage` adds each elementary item a `RenamesPlace` spans (`RenamesPlace.Leaves`) to
`CharacterChannelItems`, which `StorageFormPass` promotes through the one `IsImagePromotable` test, and `P` and `Channel` both
route an alias there. The `NumericImagePlace` the resolver wrapped around the leaf before the promotion renders as the identity over
an image carrier (`CobolNum.FormatImage(string)`, `StoreImage(string, ..., string)`), so the resolver is unchanged. A no-THROUGH alias
forwards to the renamed item's own place and stays elementary; an alias no statement names leaves its leaves native.

**Sibling sweep.** Every operand position the pass visits reaches `P` or `Channel` (MOVE both sides, DISPLAY, INSPECT, STRING,
UNSTRING, ACCEPT, a comparison, CALL BY REFERENCE). INITIALIZE refuses a RENAMES operand (§14.9.20.3 SR5). A reference-modified
alias slice already stored characters. The other group-answer readers (`PlaceRenderer.GroupImage`, `StorageOverlap`,
`CorrespondingBinder`, `StatementValidation`) already carry a `RenamesPlace` arm.

**Witnesses.** Golden `13.18.45-RENAMES/pb3563_renames_thru_numeric_leaf_characters` (nine legs: the alias, the class test of the
leaf, the plain-group control, an alias whose leaf is never named, digits flowing both ways, SPACES and INSPECT through the alias;
accept 85, no negative because RENAMES has no edition below it); unit `RenamesAliasImageTests` (positions, span bound, no-THROUGH
control, unreferenced-alias control; nine of its thirteen cases fail without the fix). Inventory witnesses added to
GR-13.18.45.4-2 and GR-14.9.25.4-4 (GAP 74 to 74). `docs/COBOLNET_DATA_MODEL_DESIGN.md` names the alias in the character-channel paragraph.
