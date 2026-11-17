# Rulings on the 13 rows of the newly printed 1985 folios XIII-30, XIII-31, XIII-54, XIII-55 (batch 22)

Written 2026-11-17 by the Mythos lane session (Claude Opus 5.5, main thread). The folios come from a public GovInfo scan
of X3.23-1985 (owner approval 2026-11-17; kb/Work PB5789, PB4952). Proposals:
`E:/COBOL-coord/scratch/my-20261115/w20262099/f/proposals.json`. Refuters: run wf_8d8420f2-1eb, records under
`E:/COBOL-coord/scratch/my-20261115/w20262099/f/refute-page-heading/`, `refute-sum/` and `refute-type/`. Every
citation below answered `python scripts/spec/cite.py --check` OK in the refuters' or the proposer's runs.

## Decided as proposed (refuter UPHELD)
`1985-GR-XIII.3.10.7-2`, `-3` and `-4`; `1985-GR-XIII.3.19.4-10`, `-11` and `-12`;
`1985-SR-XIII.3.20.3-1` (its record stands; only its reason changes); `-2` and `-3`. Each is recorded with the
proposal's verdict, basis and test-refs. SR2 and SR4 get their corrected 2023 targets: SR2 to SR-13.18.57.3-13 and
-14, and SR4 to SR-13.18.57.3-10 and -14.

## 1985-GR-XIII.3.10.7-1 (kb/Work PB5789's row): CONFORMS on its own witness; the proposed PARTIAL is withdrawn
The refuter's DOUBTFUL is right. GR1 sets the first line on which a page heading CAN be presented. A program whose
absolute page-heading line lies above that limit violates a general rule, and `§1985:I.1.6` makes "the results of
violating the formats or rules" undefined. The compiler's placement on that line (by GR3a) is therefore not a
defect. For every program that respects GR1, the compiler presents the page heading where GR1 and GR3 put it. The case
`pb5789_page_heading_relative_after_rh_85` (a report heading with NEXT GROUP, then a relative page heading, at
`--std 85`) prints the page heading on exactly GR1's first line. That case is the row's own-edition witness: the row is
CONFORMS on a test, not a derivation. The old KEPT link to `GR-13.18.57.4-7` becomes CHANGED, as the proposal says:
the editions' limits differ when the report heading has NEXT GROUP. kb/Work PB5789 is closed by this.
Lead (not a defect): the compiler refuses the lower-limit violation at compile time (COBOL2199) but not the
upper-limit one. A diagnostic for the upper limit would be consistent. Also, COBOL2199 cites `§2023:13.18.35.3 SR6c`
at `--std 85` (a citation sibling of the slice-244 to 248 leads).

## 1985-FMT-XIII.3.20.2 and 1985-SR-XIII.3.20.3-4: CONFORMS, with the refuter's corrections (OVERTURNED in basis only)
- FMT: drop the test-ref `1985-FMT-XIII_3_9_2` (at 85 it fails on a missing PICTURE, which has nothing to do with TYPE),
  and re-cite the qualified-operand basis to `§1985:IV.4.3.8.1` ("uniqueness must then be established through
  qualification for each user-defined name explicitly referenced") instead of the CONTROL clause's rule.
- SR4: drop the test-ref `pb205-type-control-operand-other-slice` (at 85 it is refused for 2002 syntax, so it does not
  witness SR4). The new cases `pb5789-type-operand-not-a-control-85` and `-ch-twice-85` remain.

## 1985-GR-XIII.3.19.4-9: PARTIAL; two defects, kb/Work PB5894 and PB5895 (refuter UPHELD both)
`§1985:XIII.3.19.4 GR9a` (printed folio XIII-54): "Crossfooting is performed according to the sequence in which sum
counters are defined within the control footing report group", and "When one of the addends is the sum counter defined
by the data description entry in which that SUM clause appears, the initial value of that sum counter at the time of
summation is used in the summing operation". `§1985:XIII.3.19.3 SR1` allows the addend in the same report group, and
no 1985 syntax rule forbids a forward or a self addend.
- kb/Work PB5894 (wrong answer, 1985 only unless another edition's own text says the same; every edition's rule to be
  read): the compiler crossfoots in dependency order, not definition order. `SA SUM SB SC`, `SB SUM SC`,
  `SC SUM WS-A` prints `CF +006 +003 +003`, where definition order gives SA = SB (0) + SC (3) = 3. Programs:
  `E:/COBOL-coord/scratch/my-20261115/w20262099/f/probes/sumxo.cob` and
  `.../f/refute-sum/probes/rford.cob`.
- kb/Work PB5895 (rejects legal source at 1985): a sum counter that is its own addend (`S5 SUM WS-A S5`) is refused
  COBOL2730 under `§2023:13.18.54.3 SR4e`, a 2023 rule. Programs: `.../f/probes/sumxf.cob` and
  `.../f/refute-sum/probes/rfself.cob`.
1985 reads 1 GAP (this row) until the fix lands.
