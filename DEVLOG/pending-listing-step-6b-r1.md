## PB5932: the source listing, step 6b remainder slice 1 - the procedure division's data-names and condition-names are identified by SymbolTable

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). The closing slice's first session
left the data-names' and condition-names' RESOLUTION by text (22 lookups) and the step 7b dispatch found the entry ids owed besides;
the remainder was ordered A (the procedure division's lookups), B (the data division's clause operands), C (the declarations) and D
(the entry ids). This entry lands part A, from `listing-step-6b-r1`: the baseline falls from 86 to 75, each change shown
behaviour-neutral by the architecture oracle (`=== ARCH-ORACLE: IDENTICAL — 22647 cases ===` against 8dd07d2eab, before and after the
read-once change).

**The typed identification of the two qualifiable classes.** `SymbolTable.IdentifyDataName(WrittenQualifiedName, report)` answers a
written data-name's §2023:8.4.2.2 candidate set and `IdentifyConditionName(WrittenQualifiedName, report)` the condition-names', beside
the index-name's `IdentifyIndexName`; the condition walks moved into the table (`SymbolTable.ConditionNames.cs`), and
`TryResolveCondition` and `NearestDeclaringConditions` are private. `NameResolution.DataItemNamed` is the procedure division's one
verdict over the set (§2023:8.4.2.2.1's ambiguity, the `--permissive` first-declared disposition through the one ambiguity verdict),
and eleven text lookups are deleted: `NameResolution.FindItem`, `ResolveQualified`, `ResolveUnqualified` and the string-taking
`FindReportedItem` and `DeclarationOf`, `ReferenceResolver.ResolveByName(string, ...)`, `ObjectPropertyResolver.ResolveObjectProperty`'s
name, `DataBinder.QualifiedConditionCandidates` and `ConditionTier`, and the table's two public condition lookups. Every caller hands
the written words: the reference resolver, the subscript reader and its paren-ownership callback, the condition binder, the SEARCH ALL
screens, the flag pass, table(ALL), SET ADDRESS OF, the object-property probes, a constant entry's LENGTH OF operand and the
storage-claim scan.

**Design corrections** (section 6.7.1, "Step 6b, remainder slice 1"). The brief asked `SymbolNamed` to gain Data and Condition arms;
that is the boxed `Identify` arm, whose cost on a qualifiable name the data-name lane had already measured over section 9's budget, so
each class has its typed method instead. `WrittenQualifiedName.Of(dataReference)` answers null for a LINAGE-COUNTER, LINE-COUNTER or
PAGE-COUNTER reference, whose `cobolWord` is its qualifier (§2023:8.4.2.2.2 Formats 7 and 8), where several callers had read that
qualifier, or the reference's glued text, as the name. And section 9 proved the first build wrong: it read the head word twice in the
identification step and a qualified reference's qualifier words twice, 0.05 to 0.32 MB over the parent's `None` rows;
`WrittenQualifiedName.OfRead` reads each word once, and every `None` row is then below the parent's three runs (IX113A 67.61 and 67.66
MB against 68.23 to 68.32; the benchmark README's step-6b table, remainder slice 1).

**What remains** (kb/Work PB5932's dated file `2026-11-19-step-6b-r1`): 75 entries, 1 table, 11 lookups and 63 comparison sites. Part
B is the data division's clause operands, which keep `(name, qualifiers)` text and so still reach `QualifiedCandidates` and its walks;
then C and D. A lead is filed there: `IdentifyDataName` keeps the two answers the procedure division gave (an unqualified reference is
not screened for a lapsed type-declaration member alone, and the sum-counter funnel asks the qualified set for an unqualified name),
for part B to decide with a differential.

**Review leads carried.** Every LEAD of the 21 `review-listing-step-*.jsonl` files and the two of
`kb/Work/PB5932/2026-11-19-review-leads-slices-2-and-3` was checked on origin/main 8dd07d2eab: all were already fixed except the
siblings of step 7b's R1-1 the step 9 sweep missed, the §2023:14.7.8 rules named "rule 1 / rule 2" beside the citation in three code
comments, `CONDITIONS_EXCEPTIONS_DESIGN.md`, `VERSION_CHANGE_REFERENCE.md`, the DOC-A.1-203 conformance entry and three conformance
cases' comments; they now name GR1 and GR2 (`cite.py --check "§2023:14.7.8 GR1"` and `"GR2"`: OK).

**Records.** The baseline and its ceiling (86 -> 75); `binder-member-readers.json`; five inventory rows re-sited with their
retired-witness marks (`audit_witness_loss.py --check`: 0 unexcused, 5 retired); the text drift tests that named the deleted members;
`cite-resolve-baseline.json` one entry smaller; the current-state docs and comments that named the deleted lookups.
