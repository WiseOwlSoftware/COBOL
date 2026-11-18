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
