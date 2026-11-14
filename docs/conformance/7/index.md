---
heading: "## 7. Annex A.1 — implementor-defined language element register (§2023:4.2.5 / §2023:4.2.16)"
columns: ["A.1 item", "Element", "Our determination", "Pinned by"]
---
> **STATUS: every documentation obligation is discharged (2026-09-25, doc-rows-2) — but a documented determination is
> not always what the compiler does yet: a row whose ⚠ names a kb/Work note states the INTENDED behaviour, and its
> traceability-inventory verdict (DIVERGES / PARTIAL / NOT-IMPLEMENTED) is held open by that note.**
> Annex A.1 lists **222** implementor-defined language elements: **164 required · 30 optional · 27
> conditionally required · 1 whose class the standard states in prose rather than the usual sentence
> (item 176, the SET NaN payload)**, of which **199 carry the obligation "This item shall be documented in the
> implementor's user documentation"** and 23 explicitly do not. §2023:4.2.5 requires the implementor to *specify*
> every element identified as required and to *document* every element identified as requiring documentation;
> D13 makes those 199 part of the definition of done.
> **MEASURED 2026-09-25 — this register discharges 182 of the 182 in scope, and 0 obligations remain**;
> 205 items carry a determination, twenty-three of them (**35**, **60**, **61**, **63**, **69**, **81**, **90**, **92**,
> **104**, **113**, **118**, **119**, **123**, **125**, **138**, **144**, **169**, **170**, **186**, **194**, **200**,
> **201**, **220**) documented voluntarily where A.1 does not require it. ⚙ **The numbers and the item list are `python scripts/spec/audit_annex_a1.py`'s
> output, not a hand count — run it rather than trusting this sentence.** An enumeration of the discharged items
> used to live in this paragraph and it rotted twice: stale by three when item 50 landed, and by three again by
> 2026-09-02. The script prints the list (`--json`'s `filed`), the gate audit ANNEX A.1 REGISTER (`audit_annex_a1.py --check`) runs it
> every build, and a list here could only ever be a second answer to a question one artifact already owns.
> The rows below are the register itself, and they are what the script reads. Notable among them are the
> **storage-representation items 205–209, 211 and 215** that V59 pinned (BINARY · the BINARY-CHAR family ·
> COMPUTATIONAL · INDEX · PACKED-DECIMAL: the byte width, the radix, the byte order and a worked example for
> each, since a COBOL developer reading this needs to know what lands on disk).
> ⛔ **SEVENTEEN OF THE 199 CANNOT ARISE, so the denominator is 182** — items **84**,
> **85** (FORMAT clause), **173** (SELECT WHEN clause) and **86** (Format validation, which cites only PICTURE GR15
> and USAGE GR3 *during a VALIDATE statement*), and — by owner decision R42 (2026-09-24) — the eleven items of the
> declined screen-handling and commit/rollback modules: **3**, **4** (ACCEPT screen format), **11**
> (BACKGROUND-COLOR), **27** (color number), **41** (CRT status), **45** (cursor), **83** (FOREGROUND-COLOR), **91**
> (function keys), **172** (SECURE), **199** (terminal screen) and **28** (commit and rollback) — and, by owner
> decision R43 (2026-09-24), the two items of declined PROCESSOR-DEPENDENT facilities: **47** (default encoding for the
> standard decimal floating-point usages, Annex A.3 items 13 and 19). (Item **212**, USAGE MESSAGE-TAG, was withdrawn while the
> asynchronous messaging facility of A.3 item 4 was declined and is documented again since kb/Work PB4740: it is §7's item 212.) Annex A.1's own preamble is the licence, not an inference from the Annex A.4 modules' §2023:4.2.7 non-claim: *"Required: The
> element shall be provided by the implementor. **When the element is part of a feature that is optional or
> processor-dependent, the item is not required if the optional or processor-dependent feature is not
> implemented.**"* A.4.2, A.4.8 and A.4.14 are Not claimed (§5), and A.3 items 4, 13 and 19 are Not claimed
> (§2), so these obligations cannot be incurred;
> §7 carries no row for them, and they carry DOCUMENTED-NON-SUPPORT in the traceability inventory, each on the
> negative golden that diagnoses its declined construct. ⚠ The exclusion is **derived, not listed**:
> `audit_annex_a1.py` reads the same `derived-verdicts` selectors the inventory batch and the C# drift test read,
> so the next declined module is handled with no edit here — and, more to the point, this counter and the
> inventory can never give two answers about one item, which is what they did before 2026-09-02.
> ⚠ The MODULE-NAME row was filed under item **213** (which is USAGE NATIONAL) from 2026-08-05 until
> 2026-08-08 — the audit's number/element cross-check caught it; its true item is **135** (§2023:15.65.4 r4).
> ⚠ **The two item-92 rows are VOLUNTARY**, and each has its OWN key: the row keyed by the item's bare id is the
> returned value's characteristics, representation and text image, and the row keyed with the suffix `b` is the evaluation
> of an exact-family function whose argument is floating-point. A row key is unique (`audit_annex_a1.py` refuses a repeat, and a lettered key whose item
> has no row of its own); a second determination of any item takes the item's id plus a letter, so a code comment that
> cites a determination names one row (kb/Work PB3840). A.1-92 is one of the 23 elements the standard says need *not* be
> documented. They are kept because the determination is load-bearing for users, but they discharge no
> obligation. (Their DISPLAY-conversion half was subsumed when A.1-56 was completed — kb/Work PB148.)
> Driving this register's rows to CONFORMS is **PHASE-14 Step 0** work — the four-edition traceability inventory
> enumerates every A.1 row and drives it to zero-GAP. A row documents the INTENDED determination and never the
> wrong behaviour (kb/Work PB1535): where today's compiler differs, the row's ⚠ says how and names the kb/Work
> note that owns the fix — an undocumented determination and a wrongly-documented one are both non-conformance,
> and the second is worse.
> ⛔ **That last sentence earned itself on 2026-08-03**: the §2023:15.3.3.2 fractional-seconds determination had been
> filed under **item 87**, which is *FORMATTED-CURRENT-DATE (accuracy of returned time)* — a different, still
> undocumented obligation. It belongs to **item 202**. The number was inherited and never re-derived, exactly
> the failure mode CLAUDE.md rule 1 names. `audit_annex_a1.py --check` now re-derives every number here against
> the catalog parsed straight out of Annex A.1, so it cannot recur silently.
>
> ⚙ **THE ITEM KEY IS THE INVENTORY RULE-ID, AND THAT IS WHAT MAKES A ROW ADDRESSABLE.** Each row's first cell is
> `DOC-A.1-<n>` — the `rule-id` of the P14 traceability-inventory row for A.1 item *n* — because that row's
> `code-location` is **computed** as `docs/CONFORMANCE.md#DOC-A.1-<n>` from its own rule-id
> (`tests/version-matrix/inventory-schema.json` → `kinds.DOC.anchor-template`). A determination therefore cannot
> be filed under a neighbouring item and still resolve: the anchor is derived, never typed. The bare number would
> not do — `#7` matched the digit 7 anywhere in this document, and five live inventory rows were closed on it.
> Prose elsewhere still cites items as *A.1 item N* / *A.1-N*, which is deliberate: the `DOC-A.1-<n>` token
> appears **only as a REGISTER ROW KEY** — here, or as the `DRV-`-prefixed key of the §8 derivation row that
> closes the same inventory row — and `audit_annex_a1.py` fails if it appears anywhere else in the file, so a
> passing mention can never masquerade as a filed determination.
>
> ⚙ **`Pinned by` names the SPEC-DERIVED test(s) that pin the determination, machine-readably** — in the
> inventory's own `test-ref` vocabulary (`conformance:<edition>/<case>`, `unit:<Class>.<Method>`), `—` when
> nothing pins it yet. `audit_annex_a1.py --check` asserts each cell AGREES with the inventory row's own
> spec-derived `test-ref`, so the determination and its evidence are one artifact rather than two that drift. A
> NIST golden or a `*_MatchesLegacy` differential is corroboration and never appears here: it cannot close a row
> (CLAUDE.md rule 1). ⚠ A `—` is an honest state, not a defect — the determination is written and correct, and
> the row stays a GAP until a golden pins it.
>
> ⚙ **A determination may cite the document that governs it, but it shall also STATE THE VALUE** — owner
> decision, `kb/Work/PB280` Q3, 2026-09-02. §2023:4.2.16 permits an implementor to meet a documentation requirement
> "by reference to other documents", and A.1's own preamble likewise allows a row to "reference other
> documentation that fulfills this requirement"; this register takes the narrower path deliberately, because a
> by-reference row states no value, so **no test can pin it** and the gate can only check that a row exists —
> the same unfalsifiable shape Q2 refused. Where the answer is the host's (the .NET case-mapping table, the
> Unicode version, an OS limit), name the document *and its version* **and** write down the resulting value; the
> value is what the `Pinned by` golden asserts.
>
> ⚙ **An element the standard marks OPTIONAL that we do not provide is recorded here as `Not provided.` and
> carries DOCUMENTED-NON-SUPPORT in the traceability inventory** — owner decision, `kb/Work/PB280` Q1,
> 2026-09-02. A.1's preamble conditions the duty on provision ("Documentation required: **If the element is
> provided by the implementor**, the implementor's user documentation shall document the element…") read with §2023:4.2.5
> ¶1, the clause that governs the A.1 list (*"The provisions of this clause apply to required normative elements … and
> to optional language elements for which an implementor claims support"*), so stating the non-provision IS the
> determination (§2023:4.2.7 is not this arm's ground: its first sentence scopes it to the Annex A.4 list, kb/Work PB593) —
> which is why the row stays here rather than being withdrawn the way the four items above are. The verdict is
> applied by a predicate, not row by row: `inventory-schema.json` → `derived-verdicts.a1-optional-not-provided`
> selects on the requirement class plus this cell's opening words, so writing `Not provided.` into an optional
> item's row turns the drift gate red until the batch is re-run. Items **7**, **64**, **106**, **127**, **143**, **150**, **193** and **196** today. Item **206** LEFT the set on
> 2026-09-09, which is this paragraph's own promise running forwards: owner decision Q30 (kb/Work PB592) replaced its
> cell with a `Provided` determination, the selector stopped selecting it, and the row was re-verdicted CONFORMS by
> a batch.
>
> ⚙ **An element the standard marks CONDITIONALLY REQUIRED whose condition does not exist in WiseOwl COBOL is recorded
> here as `Condition absent.` and carries DOCUMENTED-NON-SUPPORT in the traceability inventory** — owner decision
> `kb/Work/R43` item 3 (2026-09-24), answering `kb/Work/PB1536` Q1. A.1's preamble makes such an item owed only
> while its condition holds ("Conditionally required: If the associated feature or language element is
> implemented then this element is also required." — `cite.py --check A.1` → OK), so the row states WHICH
> condition is absent and WHY, names the code that makes it absent, and is pinned by a witness that observes the
> absence. ⛔ The disposition is **contingent**: if the capability is ever added, the item becomes owed and the
> row is re-adjudicated. The verdict is applied by a predicate, not row by row: `inventory-schema.json` →
> `derived-verdicts.a1-condition-absent` selects on the requirement class `conditionally required` plus this
> cell's opening words, the same AND-gate as the optional selector above. Items **13**, **15**, **30**, **34**,
> **37**, **88**, **96**, **98**, **142**, **149**, **152** and **200** today.
