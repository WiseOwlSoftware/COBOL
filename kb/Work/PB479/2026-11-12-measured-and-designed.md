## 2026-11-12 — measured (step 1) and designed (step 2 decided: kb/Work PB5448); steps 3 and 4 are now PB5449 to PB5456

**Where everything is.** The design: "Phase A, continued" of section 3 of
`docs/rearchitecture/DESIGN-spec-conformance-review.md` (plus a bullet in section 10.1 and a paragraph in section 10.4).
The decision: kb/Work PB5448. The steps, cluster PB479, in order: PB5449 (the mechanism, row-neutral), PB5450 (2023
dispositions), PB5451 (2023 mint, one landing), PB5454 (the fourteen held 1985 records), PB5455 (the open 2023 rows),
PB5452 (1985 levels, dispositions and mint), PB5453 (1985 change records of the new rows), PB5456 (2002). The measuring
scripts and their outputs are outside the repository, in `E:/COBOL-coord/scratch/pb479/` (`measure.py`, `sample.py`,
`analyse.py`, `pdf_levels.py`, `firstpass.py`; `out/` holds every sentence, the samples and the hand judgments).

**The criterion.** A sentence is deontic when, outside a NOTE, an EXAMPLE, a general format, a quotation and a code
example, it carries 'shall', 'may' or 'is/are (not) permitted / allowed / required' (2023's verbal forms are defined
in its Introduction; 1985 defines none and its syntax rules say "must", §1985:IV.3.1, so 'must' is added for 1985). A
HIT is a deontic sentence in the normative region (2023 clauses 6 to 16 and Annexes A to C; 1985 Sections IV, VI to
XVI and A) whose words lie in no catalog row's text of its clause.

**The totals** (`python E:/COBOL-coord/scratch/pb479/measure.py`, `analyse.py`):

- 2023: 432 hit sentences in 344 paragraphs and 223 clauses; 2,533 indicative sentences in no row; 1,774 paragraphs in
  no row in 737 clauses (263 of them Annex B and C character data). Plus 121 margin paragraphs (47 deontic) glued into
  57 numbered rows, measured on the PDF at the block's margin (`pdf_levels.py`); 46 tables (721 lines) outside rule
  blocks. Per clause: `E:/COBOL-coord/scratch/pb479/out/2023-summary.txt`.
- 1985: 238 hit sentences in 186 paragraphs and 135 clauses; 1,296 indicative sentences in no row; 871 paragraphs in
  no row in 502 clauses. Per clause: `out/1985-summary.txt`.

**The sample** (30 hits and 30 non-hits per edition, seed 479, judged by hand, `out/sample-judgments.json`): precision
22/30 (2023) and 23/30 (1985); obligations among the non-hits 15/30 (2023: 8 character-data lines, 7 general-rule
semantics) and 9/30 (1985). So the verb criterion finds about 20 % (2023) and 32 % (1985) of the unnumbered
obligations, and the design rests rows on adjudicated dispositions of every margin paragraph, with the verbs only as a
refusal of a deontic `not-rule`.

**Already implemented elsewhere (PB204's case).** 143 of the 223 2023 hit clauses are cited in `src/` and a test tree;
293 of the 1,774 2023 paragraphs are quoted by a CONFORMS record with a test (`firstpass.py`); every one of the 121 glued
paragraphs sits in a row decided against text that held it. For 1985, 18 clauses and 78 paragraphs.

**What the owner is told.** The 2023 GAP (0 of 4,214 today, `inventory_view.py --summary`) rises when PB5451 mints the
rows: about 950 to 1,330 new rows, about 300 to 400 closed in the same landing, roughly 550 to 1,000 GAP after. The 1985
GAP (318 today) rises by about 350 or more until PB5453's change records. PB5450 replaces the estimates with the count.

**This note's own first instances,** checked on this tree: §2023:14.8.2.2's variable-length sentence is not missing but
GLUED into `SR-14.8.2.2-2` (whose record verdicts it by name, so PB204's work was recorded there); §2023:14.8.3.2 has no
row at all; §2023:8.3.2.1's hyphen sentence is a dropped lead-in. All three become rows in PB5451.
