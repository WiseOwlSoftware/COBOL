## Entry 2552 — 2026-10-25 12:07 PDT — PB3686 - the CALL RETURNING pair of two program-pointers answers "the same restriction" as the SET and BY REFERENCE lanes do

**Re-probe.** On the branch's own build (`--std 2014`, checking on), `CALL U3686 RETURNING PA` and `CALL "U3686" RETURNING PA`
raised EC-PROGRAM-ARG-MISMATCH when the callee's returning item was a program-pointer restricted to one prototype
(X3686B) and the caller's receiving item one restricted to another (X3686A), both REPOSITORY prototypes with no definition
in the compilation group. PB3162 (landed) had made the SET lane, the BY REFERENCE pointer arm and `MaySet` admit exactly
that pair; the RETURNING lane was the one comparison left on strict string equality.

**Root.** `ActivationRelations.ReturningConformance` ended in `SameElementaryClauses`, which compares `Clauses` ordinally.
For a class pointer `Clauses` IS the TO-phrase restriction, which PB3162 reads through `SamePointerRestriction` (equal, or
both restricted and either one restricted to a prototype with no compile-time signature, §12.3.8.4 GR10 c)).

**Fix.** Two class pointers of one category now take `SamePointerRestriction` in `ReturningConformance`; every other
pair keeps `SameElementaryClauses`. §14.8.3.3 requires "the same ... USAGE clauses", the pointer's USAGE clause carries
the TO phrase, and its type is the prototype's signature (§13.18.60.4 GR25 / GR26), so the lanes are one question. An
unrestricted pointer opposite a restricted one is still refused. `SignatureDifference`, the other `SameElementaryClauses`
caller (§9.3.8.2.3's symmetric same-description relation), keeps exact identity: an equivalence must stay transitive.
`docs/COBOLNET_OO_DESIGN.md` names the third lane.

**Tests.** Unit `PointerRestrictionLanesAgreeTests` asserts one verdict across SET, BY REFERENCE and RETURNING for every
restricted program- and function-pointer pair, plus the unrestricted, data-pointer and category controls. Golden
`pb3686_returning_external_prototype_pointer` (prototype CALL and dynamic CALL conform; the unrestricted receiver is refused).

**Rows.** None closed (the elementary-USAGE paragraph of 14.8.3.3 has no catalogued rule id).

**Self-review.** The citations were run through `cite.py --check`. A restricted pointer opposite a different restriction
at INVOKE method matching (`MatchViolation`, §9.3.6 match rule 3 e) still compares strictly; that lane is a method-resolution
signature and was left alone (registered as a lead in the report).

