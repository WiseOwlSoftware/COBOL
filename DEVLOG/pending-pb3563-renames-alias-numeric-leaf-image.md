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

**Witnesses.** Golden `13.18.45-RENAMES/pb3563_renames_thru_numeric_leaf_characters` (eleven legs: the alias, the class test of the
leaf, the plain-group control, an alias whose leaf is never named, digits flowing both ways, SPACES and INSPECT through the alias;
accept 85, no negative because RENAMES has no edition below it); unit `RenamesAliasImageTests` (positions, span bound, no-THROUGH
control, unreferenced-alias control; nine of its thirteen cases fail without the fix). Inventory witnesses added to
GR-13.18.45.4-2 and GR-14.9.25.4-4 (GAP 74 to 74). `docs/COBOLNET_DATA_MODEL_DESIGN.md` names the alias in the character-channel paragraph.

**Review round 1 (one blocking finding).** The reviewer showed the fix incomplete for a numeric leaf that is the canonical member of
a REDEFINES class (`05 R-B PIC 999.` with `05 R-B2 REDEFINES R-B PIC 9V99.` inside the span): `MOVE "XXXXXXX" TO R-AC` still stored
`XX000XX`, while the same group moved as a whole stored `XXXXXXX`. Root: arm (4) of `StorageFormPass.ComputePromotedSet` (the
elementary character channel) filtered `item.Class is null`, which arm (3) (the whole group) never did, so the two arms of one
promotion disagreed on a class member. The filter's stated reason (a class member keeps its tier's classification) is already honored
downstream, where `Classify` arm (2) reads the one promoted union: a Tier-B view becomes `CharImage(Numeric)` and a Tier-A alias
forwards to its canonical's promoted form. The filter is deleted (arm (5) is addressed in round 2). The same root was pre-existing for an elementary receiver: `MOVE SG TO R-B` (SG a 3-character group, R-B in a
REDEFINES class) stored `000` for `ABC` and now stores `ABC` (§14.9.25.4 GR4). Witnesses: golden legs 10 and 11 added to the same
case (eleven legs), unit cases `ThroughAlias_OverARedefinedNumericLeaf_PromotesIt` and `GroupMoveIntoARedefinedNumericReceiver_PromotesIt`.
The lead (design doc paragraph claiming a NATIONAL numeric leaf is not promoted) was stale against `IsImagePromotable` and is corrected
in the same paragraph.

**Review round 2 (one blocking finding).** Round 1 dismissed arm (5) of `StorageFormPass.ComputePromotedSet` (the OPTIONS INITIALIZE
background) as "a different rule", and it was not. `AddBackgroundFilled` carried the same `item.Class is null` exclusion, while
`InitialStateBackground.Seed` seeds a class member's IMAGE (`item.Class is not null && ... HasImageByteForm`). So
`OPTIONS. INITIALIZE WORKING-STORAGE TO X"5A".` over `05 RE-B PIC 9(3). 05 RE-B2 REDEFINES RE-B PIC 9V99.` left `RE-B` a native
`long` and emitted `RE = new _T_0 { RE_B = new string('Z', 3) }`: backend error CS0029 on legal source (the same program without the
REDEFINES line printed `ZZZ`). ISO §14.6.2.3.2 GR1 sets the allocated storage to the fill character with no exemption for a redefined
item, and the owner decision R53 extends it to numeric items. Root: the promotion decision and the seed answered "does a class member
take the image?" differently, the same split as round 1's arm (3)/(4). The filter is deleted from arm (5) too, so all three arms that
promote a leaf (whole group, character channel, background) read `IsImagePromotable` alone; a Tier-A alias member forwards to its
canonical's promoted form and a Tier-B view becomes `CharImage(Numeric)`, in `Classify`. Sweep: no other `Class is null` filter remains
in `StorageFormPass`. Witnesses: golden `14.6.2.3.2-Initial-state/pb3563_options_initialize_redefined_numeric_leaf` (DISPLAY leaf `ZZZ`
and NOT NUMERIC, COMP-5 pair `23130 23130`, unredefined control `ZZZ`; accept 2023, and the OPTIONS paragraph's existing
below-2023 refusal golden is its negative) and unit `OptionsInitializeBackground_PromotesTheNumericLeaf_WhetherOrNotItIsRedefined`
(Tier-A DISPLAY, Tier-A COMP-5, Tier-B numeric view, unredefined control).
