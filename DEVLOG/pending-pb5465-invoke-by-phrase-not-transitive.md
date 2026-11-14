## PB5465 retired: an INVOKE BY phrase is not transitive, so the reported wrong answer is the conforming answer

PB5465 reported that `INVOKE C "M" USING BY CONTENT G N` passes N BY REFERENCE, so the method's store into its formal
changes the caller's N, and called it a silent wrong answer by analogy with CALL's carry-over rule. The re-probe on this
branch's build reproduces the behaviour (the note's `o2.cob` ends with `N=-038`, its `o3.cob`, with a BY CONTENT on each
argument, with `N=-039`).

The standard requires that behaviour. The printed INVOKE figure (§2023:14.9.23.2, PDF page 681 rendered) repeats
`[BY phrase] operand` as one unit, with no inner ellipsis under a phrase. CALL's carry-over, §2023:14.9.4.4 GR5, is a
Format 1 rule, and INVOKE has no counterpart in 2002, 2014 or 2023. A keyword-less INVOKE argument takes its mode from
its formal under §2023:14.9.23.3 SR14 and §2023:14.9.23.4 GR6, the twin of CALL Format 2's GR9. N meets SR9 and SR10
and its formal is BY REFERENCE, so GR6 a) 1. passes it BY REFERENCE. Carrying the phrase over would also refuse
conforming source: `USING BY REFERENCE G 7` would make the literal an identifier-3, and `USING BY CONTENT G V` against a
BY VALUE formal would break SR5 a).

No compiler code changed. The note is retired unlanded (`closes_rows: []` with its reason, and an addendum carrying the
evidence). A doc comment on `InvocationArg.OfInvokeArgument` and a sentence in `docs/OO_DESIGN.md` state the rule so the
next reader does not thread a carried mode through the reader. The golden
`cases/14.9.23-INVOKE/pb5465_invoke_by_phrase_not_transitive` (accept 2002) pins five legs: a BY CONTENT followed by a
keyword-less identifier (by reference), an explicit BY CONTENT on each, a keyword-less identifier before a BY CONTENT,
a keyword-less literal after BY REFERENCE (by content), and a keyword-less identifier after BY CONTENT into a BY VALUE
formal (by value). Its expected output was derived from GR6 and matched the run at 2002, 2014 and 2023. A
`record_verdicts` batch added it as a witness of GR-14.9.23.4-6, SR-14.9.23.3-14 and FMT-14.9.23.2 (CONFORMS before and
after; GAP 0 to 0).

The sibling sweep found every INVOKE argument reader already per-argument (`OfInvokeArgument`, `OoBindInvoke`'s
pre-screen, `StorageClaimScan`, `VersionConformancePass.VisitInvokeArgument`), and CALL already splits GR5 (Format 1)
from GR9 (Format 2).
