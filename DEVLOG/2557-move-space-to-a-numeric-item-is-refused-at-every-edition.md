## Entry 2557 — 2026-10-25 12:46 PDT — MOVE SPACE to a numeric item is refused at every edition; PERFORM VARYING without BY is refused at 85 (PB3605, PB2282)

Wave 20261500 group E, two notes on one root, `VersionConformancePass`.

**PB3605 — the SPACE arm of SR5 was modelled as a 2023 removal.** Re-probed on the worktree build: `MOVE SPACE TO N`
(PIC 9(3)) and `MOVE SPACES TO E` (PIC ZZ9) compiled clean at 85, 2002 and 2014 and were COBOLNET0902 ("removed in
COBOL-2023") only at 2023. The archived J4 texts settle the 2002 and 2014 edges: syntax rule 5 of clause 14.8.24.2 of the
2001-12-04 text reads "The figurative constant SPACE shall not be moved to a numeric or numeric-edited item", and the 2009
committee draft repeats it. The 1985 edge is derived (new VCR row 7.41): the 2002 Annex F.1 list of changes that may affect
existing programs has no SPACE item, its Annex G.1 item 2 names "any figurative constant except SPACE" as the archaic
move, and GnuCOBOL 3.2 refuses the move at `-std=cobol85` as at 2002, 2014, default and ibm (only `mf` warns). So the arm is
not an edition row at all.

The fix moved it to where the edition-invariant figurative rules live: `MoveValidity.FigurativeRefusal`, the one copy of
SR6/SR7 that the compiler's MOVE chain and the run-time activation relations both ask, gained SR5's SPACE arm, and its two
booleans became one `FigurativeSender` value read by one reader, `MoveTable16.FigurativeSenderOf`. Every MOVE-validity
asker gets the refusal without an edit of its own: the written MOVE and implicit FROM/INTO moves (COBOLNET0819),
INITIALIZE REPLACING (COBOLNET2031), and a BY CONTENT argument at bind and at run time (COBOLNET1688), through a new
`ActivationCategory.FigurativeSpace` that one table maps to and from the sender. `VersionConformancePass.Sr5FigurativeText`
no longer lists SPACE, so the 2023 removal row (`move-alphanumeric-figurative-removed-2023`, VCR row 1) covers HIGH-VALUE,
LOW-VALUE, QUOTE and the non-digit ALL literal; its description, display text and matrix source were corrected. GR2's
zero-length literal stays legal: SR5 names written figuratives only.

Goldens: `85/pb3605_move_space_nonnumeric` (alphanumeric, reference-modified and group receivers stay legal), negatives
`pb3605-move-space-numeric` (85 through 2023), `pb3605-initialize-replacing-numeric-by-space`, and
`pb3605-content-space-into-numeric-edited`. The sweep found every witness that wrote MOVE SPACE into a numeric item to show
the pre-2023 fill, and moved each to a shape that has one: `MoveEditionDifferentialTests` (non-digit ALL literal, HIGH-VALUE),
`pb880_initialize_replacing_figurative_numeric` (BY ALL "X"), `pb879-initialize-replacing-high-value-numeric-2023`
(renamed), `BoundArmDiagnosticPositionTests`; and `pb2553_sum_counter_character_cell`, `pb425_zero_length_literal_move` and
`ZeroLengthLiteralMoveTests` now move SPACE through the item's full-width reference-modified view. `docs/CONFORMANCE.md`
A.4.11's sum-counter sentence was corrected the same way. Measured: all 459 NIST programs compile at `--nist` with no SR5
refusal; the conformance suite's cases at their accept editions hit it only in the tests listed.

**PB2282 — PERFORM VARYING with no BY phrase compiled at 85.** The STRING half was already closed by PB1567. The PERFORM half
reproduced: a VARYING phrase and an AFTER phrase without BY compiled and ran at `--std 85`. The 2002 edge is now quoted:
general rule 9 of clause 14.8.27.3 of the 2001-12-04 J4 text is the 2023 sentence, "For any BY phrase that is omitted, the
augment value is 1" (§14.9.28.4 GR12). The 1985 edge is derived (VCR row 7.42, row 7.31's method): no NIST program omits BY
(all 459 compile at `--nist` with the gate on), and GnuCOBOL refuses the omission at `-std=cobol85` only. The row records
the evidence against it too: the 2002 Annex F.2 does not name the change. The new construct `perform-varying-by-omitted-2002`
(COBOLNET0900) is a ParseArm recognition gate, `VisitPerformVarying` and `VisitPerformVaryingAfter`, reported at the phrase
that omits BY. The grammar comments that called BY "optional per COBOL-85 spec" now cite GR12 and the row.
`PerformVaryingDifferentialTests.Varying_ThreeLevels_OmittedBy` now runs at 2002. Goldens: `2002/pb2282_perform_varying_by_omitted`
(output derived from GR12 and GR13 d)/e)), negative `pb2282-perform-varying-by-omitted-below-2002` (the AFTER site).

Rows: SR-14.9.25.3-5, GR-14.9.25.4-2 and GR-14.9.28.4-12 re-verdicted CONFORMS with their witnesses replaced or added
(three retired witnesses named), GAP 41 → 41. Diagnostic codes allocated COBOLNET5071-5073: none used.

