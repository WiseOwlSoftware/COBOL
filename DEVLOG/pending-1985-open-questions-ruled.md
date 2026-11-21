## Nine open 1985 questions ruled: five defects, four not, two determinations written

A sweep of the work register for open notes about `--std 85` found nine questions nobody had ruled: analyses and adjudications
filed by the 1985 code-read passes. Three adjudicators took three each against main at 8244f1aa4b, wrote one ruling per note,
and checked every citation. I read the text behind every ruling, re-ran a sample of the probes, and sent the one ruling that
reversed a landed note to an independent refuter.

- **Defects.** PB5853 is a wrong answer: in a report with no PAGE clause, a first group beginning `LINE PLUS 0` presents on
  line 0 by §1985:XIII.3.10.8 rule 4d, and the report writer clamps it to line 1, so LINE-COUNTER reads one too high.
  PB5901: `PROCEDURE DIVISION USING BY REFERENCE` compiles at 85, where the 1985 header has no BY phrase. PB5902: `WRITE ...
  ADVANCING ZERO LINES` compiles at 85, where a figurative constant may stand only where the format says 'literal' and
  integer must be a numeric literal; the refuter upheld it, and it reverses the 85 half of PB5289. PB5933: the refusal of
  DELETE on a sequential file is right but cites a 2023 rule. PB5982, new: a LINAGE data-name of an EXTERNAL file need not be
  external at 85, though §1985:VII.3.7.4 GR11c says it must, and the check ran only at 2023.
- **Not defects.** PB5897 (an out-of-domain literal argument is legal source; the result is undefined only when evaluated),
  PB5896 (the page heading's upper limit is a general rule with undefined results), and PB5164 (both recorded owner
  determinations cover their 1985 rows; D-MEDIUM-85 now names the second row).
- **Two determinations.** PB5817 asked whether an index-name may stand in another dimension's subscript position. The
  adjudicator ruled it a defect on a positional reading of SR3. I overruled it: SR3 is satisfied as written, and §1985:IV.4.3.8.2.4
  GR2 makes what an index's value means the implementor's ("This correspondence is defined by the implementor"), so CLAUDE.md
  rule 1's precedence applies and GnuCOBOL accepts it: D-85-INDEX-IN-ANOTHER-DIMENSION. PB5912 asked whether 85 must compare the
  descriptions of one external file across separately compiled programs. It need not: the rules state no consequence, so a
  violation is undefined, and D-85-EXTERNAL-FD-AGREEMENT records the limit. PB5983 keeps the one question that limit must not
  hide, whether mismatched record lengths can crash the run time.
- PB5981, new: the report heading's lower limit at 85 is borrowed from 2023 and cited to a 1985 rule that states another.
