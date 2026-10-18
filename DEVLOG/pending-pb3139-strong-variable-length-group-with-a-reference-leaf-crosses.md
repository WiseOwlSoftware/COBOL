## PB3139 and PB3342 — a strongly-typed variable-length group with a reference leaf crosses as its area and its leaf vector; one C# name per method slot

### PB3139 — the COBOL boundary and the MOVE (landed; the host entry is PB3273's)

**Re-probe** (fresh Debug build of main 0b8557338, repros in `E:/COBOL-coord/scratch/w1106c/`): a strong type holding
`PIC X DYNAMIC LENGTH`, `USAGE POINTER` and `PIC 9(2)` passed to a program-prototype CALL drew COBOLNET1688 on the CALL
AND COBOLNET1513 on the prototype against its own definition; the activated program's formal staged the Tier-C loud at its
copy-out; a group MOVE of two such groups aborted with GR9's "cannot compose" loud. The fixed-length twin of the same
type crossed correctly, so the one cause was `OoClassTable.LeafCarried` excluding every variable-length strong group,
while `CurrentExtentImageCapable` excludes any group with a reference leaf: the shape had no crossing form at all.

**Rule** (each `cite.py --check` OK): §14.8.2.2 rule 2 ("If either the formal parameter or the corresponding argument is a
strongly-typed group item, both shall be of the same type"), §13.18.60.3 SR14 (a POINTER leaf "subordinate to a type
declaration that includes the STRONG phrase"), §8.5.1.12.1 (nothing bars a strong group from being variable-length),
§14.2.3 GR8 / GR9, §14.6.5, §14.9.25.3 SR2, §14.9.25.4 GR9 a), §14.6.9.2 ("The operation recreates or overwrites the
receiving table with a copy of the sending table").

**Fix.** `LeafCarried` admits a variable-length strong group whose current extent does not compose; every consumer the
gate now reaches was completed rather than staged: `StorageCell.CopyArea` / `StoreArea` carry the area's components from
its `DynBase` (a dynamic-capacity table deep-copied, `CobolDynTable.CopyOf`), so BY CONTENT (`ContentRecord`, which now
keeps the area's `Shape`) and RETURNING (`StoreReturnArea`) deliver them; a slot-riding group's BY CONTENT carrier is a
read-only view instead of an eager image read; the leaf vector carries a dynamic-capacity table as its current
occurrences, `RecordStructEmitter` emitting an instance `WithLeaves` (replacing the static `OfLeaves`) that recreates the
receiver's own table (`CobolDynTable.Recreated`) and `PlaceRenderer.CellLeaves` / `CellLeavesStore` doing the same for a
cell-backed group (`ViewCursor.AtElementCell`); `MoveEmitter` asks the leaf vector before GR9's composer (keeping main's
PB3261 sender-first `Ordered` form). Docs: COBOLNET_OO_DESIGN (the leaf vector and the program ABI's area), DOC-A.1-141 in
CONFORMANCE.md and DESIGN-external-repository §4.3 (the host restriction's reason).

**Carried onto main 4016a67cb (w1111b).** w1106c's three commits were cherry-picked; main's PB3177 had moved
`tests/conformance/` into `conformance-suite/`, so the goldens were re-filed with their metadata blocks:
`cases/14.8.2.2-Group-items/pb3139_strong_vlg_pointer_call` (CALL BY REFERENCE / BY CONTENT / RETURNING, a user-defined
function's result, MOVE between a cell-backed formal and a record struct; a pointer in a dynamic-capacity table's
element) and `pb3139_strong_vlg_objref_invoke` (INVOKE BY REFERENCE / BY CONTENT / RETURNING; an object reference in a
table element), both `rules: [SR-14.8.2.2-2, GR-14.9.25.4-9]`, accepted at 2014 and 2023 (re-probed: MATCH at both); the
portability lint's numeric DISPLAYs now go through a `PIC Z` / `PIC X` holder (the capacity counter is binary, and
§14.9.25.3 SR8 admits only a numeric receiver for it). The negative `cases/13.18.19-DYNAMIC-LENGTH/pb3139-strong-vlg-pointer-at-2002`
refuses at 85 and 2002 with COBOLNET0900 (`wiseowl/diagnostics.json`). Main had already raised the runtime major to 9
(PB1099 to 8, PB3339 to 9), and this branch changes shipped members (`CopyArea`, `StoreArea`, `ContentRecord`, `StoreReturnArea`, and
PB3342's `BASE.__FactoryObject`), so the major is 10 and `RuntimeAbiPinDriftTests` re-pins the measured surface hash.

**Carried onto main a0a2ff24f (w1114a).** The seven commits were cherry-picked again (never merged); main's PB3339 had
taken the runtime major to 9, so the major is 10 here and the surface hash re-pins to `7B86AAFDF5884757`. Re-probed:
both goldens MATCH. **Rows:** none claimed by the note (`closes_rows: []`; `SR-14.8.2.2-2` and `GR-14.9.25.4-9`, which
the goldens witness, were already CONFORMS). PB3139 lands with its own mechanism, the COBOL boundary. The .NET host entry
is a separate mechanism with its own note, PB3273, now unblocked: the published record composes no area holding
components beside slots, so `PublishedRecordEmitter.Renders` still withholds a unit with such a formal, and PB3139's
w1111b Progress section names the design constraint (the area must state its `Shape`, and a host-built
dynamic-capacity table needs its description's element seed). Measured on the way and reported for the register:
DISPLAY of such a group whole compiles and then aborts in the Tier-C whole-group image loud, where DOC-A.1-214 makes a
reference's positions the placeholder image; a relation of two such groups and FUNCTION LENGTH of one run correctly.

### PB3342 — one C# name per method slot (COBOLNET4213 retired)

**Re-probe** (this branch's build): a class `M3342` overriding `K3342B`'s method `M3342` drew COBOLNET4213, the
"implementation restriction" inherited from C#'s CS0542. **Sibling sweep:** a class `N3342` implementing an interface
prototype `N3342` compiled through the binder and failed in Roslyn (CS0535: the class's own method had been renamed
alone, kb/Work PB975, while the prototype kept the plain name); a class `FACTORYOBJECT` overriding BASE's FactoryObject
(§16.2 does not declare it FINAL) drew 4213 against the runtime member `FACTORYOBJECT()`; an interface `Q3342` with a
prototype `Q3342`, and a factory method spelled like its class, were measured too.

**Rule** (each `cite.py --check` OK): §8.3.2.2 "Within a source element" (one word as two types is barred only there),
§10.5 "A source element is a source unit excluding any contained source units", §8.4.6.1 "independent of the use of these
user-defined words by other source elements", §11.7.3 rule 3), §9.3.6 ("it is the class of the actual object referenced at
runtime that is used in resolving a method invocation to a particular method implementation"). The source is legal.

**Fix (one rule, one place).** `OoClassTable.NameMethodSlots`, at the end of `ResolveOverrides` and before any INVOKE
binds: every method C# requires to share one member name is one SLOT (an override chain joined with every interface
prototype a member implements, found by the roster key `ValidateImplements` uses), named once from its first
non-overriding method's word (a standard method's runtime name), and renamed to `NamingConvention.ClassNamedMethodName`
when any type declaring a member of it (instance class, factory class, interface) spells that name. `AddMethods`' own-name
rename is deleted (it was the one-member case). `BASE.FACTORYOBJECT()` is `BASE.__FactoryObject()`, as `New` is `__New`.
COBOLNET4213 `OverrideNamedLikeItsClass` is deleted with a retirement comment; COBOLNET_OO_DESIGN item 5 describes slots
(its stale "0820 restriction" sentence replaced).

**Goldens:** `cases/9.3.6-Method-invocation/pb3342_slot_named_like_its_class` (`rules: [GR-9.3.6-1, SR-11.7.3-3]`, MATCH at
2002, 2014 and 2023) and the negative `pb3342-slot-named-like-its-class-at-85` (COBOLNET0900). **Rows:** none
(`closes_rows: []`, reason recorded; SR-11.7.3-3 and GR-9.3.6-1 were already CONFORMS).

**Self-review:** architecture — one naming rule for every slot, in the class table that owns the slots; code — the slot
union reads the same roster key the conformance check pairs by; duplication — the AddMethods special case is gone;
performance — one pass over the rosters per compilation group. Codes: none of COBOLNET4244-4246 used.

**Gate** (w1114a, at 8b57d399d, run `20261018T034820Z-37ceab`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515):
GREEN ===`; the four goldens and both refusals ran in leg 1 and passed. Codes: none of COBOLNET4274-4276 used.

**Branch review round 1** (kb/Work PB3027): one finding, R1-1 — `OoClassTable.LeafCarried`'s summary still named the
deleted static `OfLeaves`; it now names the sender's `AsLeaves` and the receiver's instance `WithLeaves` and why the
store is an instance one (§14.6.9.2). `git grep OfLeaves` over src, tests, docs and scripts finds no other stale use.
