## Entry 2771 — 2026-10-29 21:29 PDT — PB4089: STRING into a numeric DISPLAY receiver moves characters instead of storing a decoded number

`STRING "AB" DELIMITED BY SIZE INTO N` with `N PIC 9(6) VALUE 123456` left `003456`. ISO/IEC 1989:2023 §14.9.43.3 SR1 admits a
numeric item of usage display or national as identifier-3, §14.9.43.4 GR6 has the characters moved "one at a time" into its
character positions, and GR7 changes only the positions written: the item holds `AB3456`. GnuCOBOL 3.2 agrees. The two
compilers differed, and the PB1908 implementer carried the lead.

**Re-probe (build of 20fecec14).** `N=003456`; `SS PIC S9(6) SIGN TRAILING SEPARATE` gave `003456+` where `AB3456+` is
right; a group receiver was already right; and a Tier-A REDEFINES pair (`A1 PIC 9(4)`, `A2 REDEFINES A1 PIC 9(4)`) lost the
letters as well (`A1=0034` after `STRING "AB" ... INTO A1`).

**Root cause.** The emitter was not the defect. `StringEmitter.ReadImage` reads the receiver's character image and
`WriteImage` stored it back, but a plain numeric DISPLAY item has a native `long` carrier that cannot hold `AB`, so
`WriteImage` carried an arm that decoded the whole image with `NumParseDisplay` and stored the number. The item needed a
character-image storage form, and INSPECT already has the mechanism that gives one (PB1128): the statement is a CHARACTER
CHANNEL of its elementary identifier-1, `UsageCollectionPass` records the item, and `StorageFormPass` promotes it to
`CharImage(Numeric)`, forwarding a Tier-A REDEFINES alias to its canonical's form (PB3563).

**Fix.** `UsageCollectionPass.Visit(BoundStringStmt)` now records an elementary identifier-3 as a character channel
(`Channel(n.Into)`). `WriteImage` lost the decode arm, because no receiver can reach it: a group takes the group writer, a
reference-modified receiver the splice, and every other legal receiver assigns its image. I first tried the MOVE binder's
`MarkImageForced` route; it missed the Tier-A alias (the canonical stayed native), which is what showed the channel
mechanism is the one place the rule belongs, so that attempt was reverted instead of being added as a second copy of the
eligibility predicate. `docs/STRING_OPS_DESIGN.md` records the rule beside INSPECT's.

**Sibling sweep.** Verified on the fix: signed overpunch, SIGN TRAILING SEPARATE, POINTER into the middle and onto the sign
position, a table occurrence, Tier-A and Tier-B REDEFINES, EXTERNAL, BASED and LINKAGE receivers, a national numeric
receiver, and a numeric use afterwards (`ADD 1 TO N` over a digits-only result). The first round of this entry said
Tier-A was verified; only the CANONICAL as receiver had been, which review round 1 caught (below). UNSTRING into a numeric receiver stores by
MOVE (§14.9.48.4 GR11, PB979) and is unchanged. The other character writers into a numeric item (MOVE of a figurative,
a reference-modified MOVE, INSPECT) already take the image form.

**Golden.** `conformance-suite/cases/14.9.43-STRING/pb4089_string_into_numeric_display` (accept 2014 and 2023: Annex E lists
no STRING change, so the rule is the 2014 text; whether COBOL-85 and 2002 admitted a numeric identifier-3 cannot be derived
from this repository, so no edition refusal is added). Seven legs: the report's case, a POINTER variant, a signed
separate-sign receiver written at the sign position, digits that stay a valid number, a table occurrence and a REDEFINES.
No inventory row changes: SR-14.9.43.3-1 and GR-14.9.43.4-3, -6 and -7 were already CONFORMS; the test is a new witness.

**Review round 1 (R1-1, blocking).** `STRING "XY" ... INTO G2` with `G2 REDEFINES G1`, both `PIC 9(4)`, failed the backend
compile (`CS0029`, string to long at `G1 = __strInto0;`). The channel recorded the ALIAS item, `Classify` forwards a Tier-A
alias to its CANONICAL's form, and `ComputePromotedSet` promoted only the recorded item, so the canonical kept its native
`long` carrier. The same gap already broke the two older channels through an alias: `INSPECT G2 REPLACING ...` crashed the
compiler (`InspectEmitter.EmitStore`, "UsageCollectionPass must have promoted it") and `MOVE GR TO G2` read back as zeros
instead of the group's characters (§14.9.25.4 GR4). Fix at the root: `StorageFormPass.ComputePromotedSet` ends with arm (6),
the Tier-A alias closure, which promotes the canonical of every promoted alias (§13.18.44.1: "the same computer storage
area"), over all arms at once, so no present or future channel reopens it. Goldens: leg H in the STRING case and
`14.9.25-MOVE/pb4089_character_channel_into_redefines_alias` (the MOVE and INSPECT siblings). The lead R1-2 (two comments
still naming the STRING receiver as a reader of `PicInfo.IsCharacterFormNumeric`) is fixed in `PicInfo.cs` and
`DisplayUsageUnionDriftTests.cs`.

