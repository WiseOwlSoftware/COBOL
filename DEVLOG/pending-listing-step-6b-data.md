## PB5932: the source listing, step 6b data-name lane, first slice - the DEBUG-ITEM register names, the type-names, the CAPACITY register names and the index-names move into SymbolTable

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands the first slice of
the data-name lane, which ran beside the OO lane on `listing-step-6b-data`: four classes, each moved in its own commit with its
resolution and shown behaviour-neutral by the architecture oracle (`=== ARCH-ORACLE: IDENTICAL — 22647 cases ===` after every source
change), and the baseline lowered by 33 entries (merged with slice 7: 168).

**What moved.** The X3.23-1985 DEBUG-ITEM family's names (§1985:XV.3.2.4 GR10) are keyed where `SymbolTable.ActivateDebugRegisters`
makes the family. The type-names, the element's, its containers' GLOBAL ones and each method's, with §2023:8.4.2.2.1 SR4's lapsed
members, are `SymbolTable.Types.cs`; a type-name an entry carries as text is resolved from the entry (`TypeDeclarationOf`). The named
CAPACITY registers (§2023:13.18.38.3 SR30) are `SymbolTable.CapacityRegisters.cs`, their one lookup behind the `OccursResolved`
watermark. The index-names are `SymbolTable.IndexNames.cs` (`IndexNameRegistry` is deleted), and a subscript's qualified index-name
travels as `WrittenQualifiedName`, the name and its qualifier tokens, read by the one qualifier walk that replaced two copies.
A method's own tables are `SymbolTable.MethodNames`, whose `Declares` every method tier asks (§2023:11.7.4 GR5).

**What the code proved about the design** (section 6.7.1, "Step 6b, data-name lane, first slice"): the progress table's "one unit" of
data, condition, index, capacity, type and debug-register names is several classes (§2023:8.3.2.2), which moved one at a time because
the cross-class rankings of §2023:8.4.6.2.1 3) were already the table's; the data-names and condition-names, which rank each other both
ways, are the lane's next slice, and the progress note lists them with the work they need.

**Section 9.** The first build allocated 0.6 MB more on IX113A's front end and bind; the DEBUG-ITEM arm read every data reference's
suffix list before asking whether a register exists, and the qualified-name carrier built a list for every subscript name. Both are
fixed. IX113A then remained 0.14 to 0.26 MB above its parent, over the `None` budget, and review round 1 removed the rest: an
index-name ask answered an `IdentifiedName` around its boxed candidate set (now the typed set, `SymbolTable.IdentifyIndexName`), the
qualified-name carrier read its words off the tokens at each use (a token's text is a new string per read), and the CAPACITY register
arm read every data reference's word before asking the table. Every `None` row now lies at or below the merged main's, alternating on
one host (IX113A 68.46 and 68.40 MB against 69.09 and 69.06; the benchmark README's step-6b table).

**Also:** the inherited citation form `§2023:8.4.2.2.1 rule N`, which `cite.py` refuses, is `§2023:8.4.2.2.1 SRN` at 43 comment and
document sites; the one in a diagnostic's text is filed as a lead. Every review lead of listing steps 2 to 7 was checked on this tree:
each was fixed by slice 7 (#1888), which landed while this lane ran.
