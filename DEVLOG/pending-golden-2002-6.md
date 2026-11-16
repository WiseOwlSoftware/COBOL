## The 2002 golden lane, sixth landing: 129 witnesses at --std 2002 for 14 wave-u files, 94 more 2002 rows closed (GAP 1,805 to 1,711)

The gl2002 lane (kb/Work PB5597) gives each 2002 row a witness of its own at `--std 2002`. This landing integrates the
first 14 "wave u" files: rows a 2002 read left needs-own-witness, undecided or uninterpretable. They cover floating
indicators, COPY, REPLACE, the constant conditional expression, the EVALUATE, SOURCE FORMAT and TURN directives,
alphanumeric literals, qualification, identifiers, concatenation expressions, relation conditions, the compilation group
and the FACTORY paragraph.

**The shape.** 102 rows by work kind: 82 needs-own-witness, 19 undecided and 1 uninterpretable. The writers reported 82
new-golden, 13 existing-golden and 7 not-closable, and deferred none. The refuters overturned 29 verdicts: convention
10, expected-value 8, citation 5, does-not-exercise-rule 3 and evidence-kind 3. The fixers applied all 29 (one in part:
the Format 5 screen leg of 2002-FMT-8.4.1.1.1 stays with kb/Work PB5581).

**What landed.** 129 cases, each validated on this branch's build, since no validate stage ran for these files:
- 57 new cases are accepted at [2002]. run.py at 2002 on a manifest of exactly these 57 gave PASS 57, FAIL 0.
- 72 refusals have `refuse-at: ["2002"]`, and each was refused at `--std 2002` with its expected code.
- Three existing cases (g2rct01, g2_class_truth_values, g2_sign_condition) gained 2002 ids in their `rules` header.
- pb5600-flag-native-arithmetic-onoff no longer lists 2002-GR-7.2.12.3-4 and -5: it specifies ARITHMETIC IS STANDARD
  and its last directive implies ON, so neither the native-in-effect nor the no-directive branch is reached.
- 16 PROGRAM-IDs collided with earlier cases (two of them with golden-2002-7's, after the merge) and were renumbered; no expectation changed. The drafts arrived CRLF and
  were written LF.
- `docs/conformance/3/D-ARITHMETIC-MODE.md` said the 2002 compile-time mode used 34-digit intermediates rounded
  NEAREST-AWAY-FROM-ZERO. The 2002 text (§2002:8.8.1.3.1.1, §2002:8.8.1.3.2 to .5) gives 32 digits, each operation
  truncated, which is what the compiler does; g2cta01, g2cce10 and g2def01 now pin it at 2002.

**Records.** `record_verdicts.py --edition 2002` wrote 95 store files: CONFORMS 94 and PARTIAL 1. The 2002 GAP went from
1,899 to 1,805 on the branch's base, and from 1,805 to 1,711 on main with golden-2002-7 merged in. The PARTIAL row, 2002-SR-11.3.2-2, is witnessed for single inheritance; its multiple-inheritance branch
waits on kb/Work PB5580. Every test-ref was checked to run at 2002 on this tree, and a refusal-only witness was
admitted only for an SR or FMT rule that grants no form (9 records).

**Defects** (each re-run on this branch's build at `--std 2002`):
- PB5751 (new): with EC-SIZE checking on, an ADD with ON SIZE ERROR still sets EC-SIZE-TRUNCATION as the last
  exception status (`EcEmitter.EmitSizeHandling`), against §2002:14.6.4. Also at 2023.
- PB5752 (new): COBOL1519 for a COMMUNICATION SECTION in a method, factory or instance definition says SCREEN SECTION
  (`OoDefinitionRules` has no Communication arm).
- PB5753 (new): COBOL2239, a repeated parameter-name in INTERFACE-ID or CLASS-ID USING, is applied at 2002 on the
  2023-only rules §2023:11.6.3 SR7 and §2023:11.3.3 SR9.
- PB5580 (finding): multiple inheritance refused at 2002 (COBOL0849) for a factory that implements an interface through
  an inherited method.
- PB5738 (finding): a method declared `METHOD-ID m AS literal` is reachable only by the literal (COBOL0825), seen from
  2002-GR-11.6.3-1.

**Not closed** (7 rows):
- 2002-GR-7.1.2.3-4 and -5: library text listing and the LISTING directive after a COPY. No listing is produced
  (§2002:7.2.15.2 GR1), so they close only by a documented determination like the 2023 twins'.
- 2002-GR-7.2.11.3-5 (FLAG-85 default off) and 2002-GR-7.2.12.3-3, -4 and -5 (FLAG-NATIVE-ARITHMETIC OFF, native in
  effect, default off): the only observable is the absence of a warning, which a conformance case cannot fail on.
- 2002-SR-11.2.2-4: sentence 2 (a parameterized class that expands class-name-1) reads two ways and needs an
  adjudication before a witness can be written.
