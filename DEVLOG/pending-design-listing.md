## The source listing, designed: one model, three renderings, step 1 the 1985 minimum

PB5925 decided that WiseOwl COBOL produces a source listing, built to be the best of any COBOL compiler, and that its
design doc comes before any code (rule 2). This entry adds that doc, `docs/rearchitecture/DESIGN-listing.md`, and changes
no code.

**What it rests on.** The requirements survey of every edition (`listing-requirements.md`) and the competitor survey
(`survey.md`). Every citation in the doc was re-run through `scripts/spec/cite.py --check` this session. Two of my own
first quotations failed: "If no LISTING directive is specified" is not the text of §2023:7.3.18.3 GR2 (the rule reads
"The default LISTING directive is '>>LISTING ON'"), and "even if a LISTING OFF directive is in effect" is not in
§2023:7.3.12.4 GR2 (it reads "irrespective of whether it is suppressed by the LISTING directive"). They were paraphrases
written as quotations. Both now use the rules' own words and re-check OK.

**What the compiler already knows.** Reading the front end showed most of what a listing needs is already built: the
physical text of every file (`ReferenceFormatMap.PhysicalText`), the resultant-line map with column spans
(`SourceLineMap`, `LineOrigin`), omitted lines of false conditional branches (`LineOrigin.Omitted`), COPY depth and the
library text found (`CopyProcessor.ExpandCopiesOneLevel`, `ResolveOneCopy`), the store taxonomy of every bound statement
(`BoundStores`, whose `StoreKind.ReadWrite` is the cross-reference's "modified"), and the position of every statement
(`BoundStatement.At`). Two facts are missing. First, which COPY a resultant line came from: a copybook copied twice
gives two inclusions with the same file and line numbers. The design adds an inclusion trace that records each
inclusion's resultant line range. Second, the bind diagnostics are strings (`EditionContext.Diagnostics` is a
`List<string>`), so a listing could not anchor them. Step 3 makes them structured.

**The shape.** The model and its three renderers live in a new assembly, `WiseOwl.Cobol.Listing`, that references
nothing else in the solution. The compiler enforces that renderers read only the model, so no drift test has to.
Every fact the compiler has is positioned in resultant space, and one index maps it to a listing line. A new section
gets placed by construction. The JSON schema is generated from the model types and pinned by a preflight drift test,
so it cannot drift from the writer.

**One timestamp.** `CompilationInputs.ReadCompilationTime` captures the clock on its first call and returns the same
instant afterwards. WHEN-COMPILED, the heading and the DATE-COMPILED replacement then agree, as §1989:A.2.46.3 RV3 and
§2023:15.99.3 RV3 require.

**Step 1 closes PB5923.** It covers the 1985 minimum: source lines as written, '/' page ejection, DATE-COMPILED
replaced, program-names in the headings, and the listing on by default at `--std 85`. PB5923's site block now names
step 1's files and members.

**Architecture review, round 1 (four blocking findings, four leads).** The review read the design against the code and
found four things it got wrong. First, the Compiler cannot read `FixedFormLine`, which is internal, so the design now has
the normalizer keep the line kind it already computes, as a public `PhysicalLineKind` per line on `ReferenceFormatMap`.
Second, no step ran the benchmark, so each step that adds cost now measures it. Third, a `Place` has no position and
`StoreKindOf` answers per item, so the cross-reference could not be filled. Step 7 now records a per-occurrence
`ReferenceOccurrence` in `ReferenceResolver.ResolveImpl`, classified against the new `BoundStores.StorePositionsOf`.
Fourth, the 2002 and 2014 rules were missing. They are now in the table, every quotation checked, with the twelve GAP
2002 rows planned in step 4b, after 1985. The leads: the driver's listing hook no longer depends on its ten return
points; a failed parse lists the pass whose diagnostics it reports; the margin and the links are computed once, into
the model; step 1 now enters the new assembly in the architecture census and the R2 subsystem table.
**Round 2, and the owner's order.** The second review found that `ReferenceResolver.ResolveImpl` is one of many resolve
paths, not the one funnel. The ruling was to stop listing record sites. The design now records each occurrence at the
one step every path must take: identifying a written name. That step moves into `WrittenNames.Identify` and every
lookup becomes private to it, so a new resolve path has no way to reach a symbol without being recorded. A test that
enumerates the written references from the parse tree proves it, and one planted program per path backs the test up.
A Roslyn `SymbolFinder` sweep listed the paths, because the LSP server had not indexed this worktree. Rows L-1 and L-11
cite all three editions, which share the same text. On 2026-11-18 the owner ordered "build the full listing support",
so every step, 4b included, is built now, in order, each landed before the next branches.
**Round 3: no more lists.** The round-2 list of paths was incomplete again. It missed the file, report and CD tables
and the register arms of `Identify`. The ruling was to make both halves structural, and the design now does. First,
every table keyed by a COBOL name is private to `WrittenNames` and keyed by a `CobolName` type. A Roslyn drift test is
red on any name-keyed table outside `WrittenNames`, on any lookup by `CobolName`, and on any new string-taking member
that returns a symbol unless the member is classified. Second, the completeness oracle reads its name slots from ANTLR
labels on the grammar. A second drift test counts the ATN's transitions into name rules and checks each one is labelled.
A semantic sweep that applies the first test's rules to today's tree found 65 name-keyed tables (31 non-private),
including the three round 3 named. It also found 194 string-taking members that return binder types. The sweep is
committed as evidence, not as the guarantee.