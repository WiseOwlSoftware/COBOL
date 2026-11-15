## PB5623: a CALL's RETURNING item that is an omitted formal raises EC-PROGRAM-ARG-OMITTED at the CALL; PB5621 retired

**PB5623.** `CALL "Q25G" RETURNING RX`, with RX a formal whose argument was omitted, raised nothing at `--std 2002` (the
repro ran Q25G and N stayed 02, where CALL identifier-1 raised). The formal's root carries its `OmittedFormalGuard`, but the
RETURNING carrier is a lazy `OverField` over that guarded path, read and written only by the activated program's delivery, in a
checking scope that is not the CALL's. §2002:14.8.4.3 GR3 a) identifies identifier-3 "at the beginning of the execution of the CALL
statement", and that identification is a reference GR12 raises on. `CallEmitter.EmitCall` now asks `PlaceRenderer.RootGuard` of the
RETURNING place (a path-root test, so a subordinate of a group formal and a redefinition view answer as the formal does) and emits
`PlaceRenderer.OmittedReferenceRaise` after the identification hoist; the same repro prints N=03. Sweep: INVOKE RETURNING of an
omitted method formal already raised (its store runs in the INVOKE, through the guarded path); the other receiving verbs write
through `PlaceRenderer.Write`. `docs/INTERPROGRAM_DESIGN.md` carries the rule. Golden `14.8.4@2002-CALL-STATEMENT/pb5623-returning-omitted-formal-2002`
(whole formal and group subordinate as RETURNING, the formal as an argument, all arguments supplied) and its 85 refusal; row
`2002-GR-14.8.4.3-12` CONFORMS.

**PB5621, retired.** The note expected an ANY LENGTH formal bound to an OCCURS DEPENDING group to take the argument's current length. For
a BY REFERENCE argument §2002:14.7.1.1 (§2023:14.8.2.2 SR2) says "the maximum length is used", which is what the compiler does; BY
CONTENT the length is the sending item's current one, and the compiler does that too (LEN 0 at N=0, 3 at N=3, UNSTRING of the zero-length
formal ends at once, a zero-length delimiter is ignored). No code change; golden `14.8.44@2002-UNSTRING-STATEMENT/pb5621-unstring-any-length-variable-occurrence-2002`
pins the three legs and rows `2002-GR-14.8.44.3-2`, `2002-GR-14.8.44.3-9` CONFORMS (2002 GAP 2511 to 2508 with the PB5623 row).

**Self-review.** Architecture: the guard test and the raise text live in `PlaceRenderer` beside `GuardedRoot`, not in the emitter. Code: the raise is
the one `OmittedFormal.Raise` the path guard uses; zero scaffolding without a >>TURN (the guard is null). Performance: one inline test per CALL with
a guarded RETURNING, none otherwise. Duplication: none, the guard is read from the access path, not recomputed from the item.

Review round 1 (R1-1): `PlaceRenderer.RootGuard` had no arm for `RenamesPlace`, so `CALL ... RETURNING` a level-66 RENAMES alias of an omitted group formal still ran the activated program unraised. The arm now answers with the first guarded leaf; the golden gains a Q5623R leg (alias RETURNING, N=03, G2=12 on the supplied call). Sibling sweep: the other Place kinds (register, report counter, debug, exception-object places) are never formal storage.
