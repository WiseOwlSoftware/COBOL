## The 1985 name table: one resolver over GR1's set, a report verb that names the hider, and 1985 clauses in its messages

Wave 20262112 group A, kb/Work PB5975, PB5976 and PB5978 (the 1985 half), one root: the symbol table's cross-program name
resolution (`src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/`).

**PB5975, one resolver.** PB5973 had made every probed outcome of §1985:X.1.3.8.2 right by teaching each class's lookup to ask some
other classes' "declared nearer" predicates. The re-probe confirmed the PB5973 goldens and then tried a pair none of those paths
asked: a contained program's own `INDEXED BY XN` against its container's `01 XN GLOBAL`, used as an `OCCURS ... DEPENDING ON`
object. It compiled and ran on the container's item (`AB`), at `--std 85` and `--std 2023`, because the data-name narrowing never
asked the index-names. §1985:X.1.3.8 says a reference "even when it is a different type of user-defined word, is to the object
which that program describes". The fix is the shape PB5973's note had decided: `SymbolTable.NearestInNameSet` answers the nearest
declaration of a spelling over GR1's whole set (ten classes, `NameSetClass`), qualification first, as a `DeclarationFact` with its
kind, depth and line, and every narrowing asks it with the classes other than its own. Eleven per-class predicates are deleted. A
drift test holds `NameSetClass` to `DeclarationKind` and the resolver to an arm per class, so the next class is ranked against
every other at once. Golden: `pb5975-own-index-name-hides-global-data-85` (now refused COBOL0851, "is declared as an index-name").
Rows GR-8.4.6.2.1-1 and GR-8.4.6.2.1-3 were re-sited from the deleted predicates to the resolver by a retirement batch; no GAP
changed (4 → 4 at 2023, 0 → 0 at 1985).

**PB5976, the report verb names the hider.** `INITIATE R1` where R1 is the program's own data item and a container's GLOBAL report
was refused COBOL1757 as if the RD did not exist. INITIATE, TERMINATE and GENERATE now add what R1 names here, with its line, and
the GR3a rule, through the same resolver (`Declarations.ReportNameHiddenBy`, `DeclarationFact.Placed`). The GR3a citation is one
function now (`NameResolution.NearerDeclarationRule`), shared with the condition-only refusal, and its 2023 half cites
`§2023:8.4.6.2.1 GR3a` (cite.py OK) instead of the non-designator `3) a)`. Goldens: the two PB5973 refusal files expect the new
wording; `pb5976-own-condition-name-hides-global-report-85` is the TERMINATE sibling.

**PB5978, 1985 clauses.** `DeclarationFact.ClassPhrase` cited §2023 clauses at `--std 85` ("a file control entry, ISO §2023:12.4.5").
It and `DeclaredAs` now take the edition and cite §1985:VI.5.8, VII.2.3, XIV.2.2, XIII.3.5, XIII.3.9 and XIII.3.19 at 85; the same
sentences' qualification and ODO arms cite §1985:IV.4.3.8.1 SR3 and §1985:VI.5.8. Golden:
`pb5978-file-name-as-data-cites-1985-85`. The note stays open for its 2002 and 2014 halves (COBOL2692's citation), which wait for
those editions as the note decided.

**Self-review** (architecture, code, performance, duplication): one mechanism for cross-class rank; the capacity-register arm reads
its table only in `SymbolTable.CapacityRegisters.cs` (`CapacityRegisterReferenceDriftTests`); the resolver runs only for a tier at
depth > 0, so a program with no containers pays nothing; `CapacityRegisterClassPhrase` folded into `ClassPhrase`.

**Review round 1, R1-1 (PB5975).** The index-name lookup still claimed a qualified reference when the written qualifiers reached
no index-name: a contained program's own `INDEXED BY X` refused `X OF G`, which only the container's GLOBAL `G` satisfies
(COBOL0900, COBOL1639, COBOL1637 at `--std 85`). `SymbolTable.IndexCandidates` now asks the resolver whatever its set's count, so
qualification is applied over GR1's whole set before the index-name class claims the reference; an `X OF Q` that reaches nothing
keeps the index-name refusal. The method arm keeps its own set (§2023:11.7.4 GR5 replaces, it does not rank). Golden:
`pb5975-qualified-global-data-beside-own-index-85` (`Y=7`, `E=B`).

### The four rows read again, and what the readers saw beside them

The PB5972 and PB5973 fixes landed after the 1985 code reads that found them, so `code_reads.py plan --edition 1985` owed
those four rows a re-read (1985-SR-IV.4.3.8.1-L2.1 and 1985-GR-X.1.3.8.2-1, -2, -3). They were read again on a compiler
built at this branch's head (5be31a9ac5), one reader and one refuter, about 80 probe runs between them: all four CONFORM and the
refuter upheld each. The reader found `SymbolTable.NearestInNameSet` to be the one edition-free resolver PB5975 asked for.
The 1985 store now reads 1,355 confirmed, 0 contradicted, 0 owed a read, 0 owed a re-read.

The review's one lead (the GENERATE arm's hider wording had no golden) has one now:
pb5976-own-data-name-hides-global-report-generate-85. Three misleading messages the agents met are filed, not fixed here:
PB5979 (a qualified operand printed run together, `XOFQ`), PB5980 (the file verbs do not name the declaration that hides a
GLOBAL file, PB5976's sibling) and an addendum to PB5978 (COBOL2326 cites §1985:VI.6.23.3 SR2, which the program satisfies,
where §1985:VI.6.23.4 GR3c is the rule it breaks).
