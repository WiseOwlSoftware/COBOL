- **D-RM1 — a reference-modified argument passed BY REFERENCE to a method, a prototyped program, a program the NESTED phrase activates or a function**
  (kb/Work PB2077; decision PB3098, 2026-10-12). §2023:8.4.3.3.4 GR5/GR6 define the unique data item with class, category,
  usage and no JUSTIFIED clause — and no PICTURE clause; §2023:9.3.6 match rule 3 e) and §2023:14.8.2.3.2 rule 2 require "the
  same … PICTURE … clauses". So BY REFERENCE the slice matches and conforms only to an alphanumeric group formal of no
  more bytes (3 c), §2023:14.8.2.2 rule 1), on the typed lane (a compile-time refusal, COBOL0828) and the universal lane
  (EC-OO-METHOD) alike; an elementary formal takes it BY CONTENT (§2023:9.3.6 4 d)). Two readings the text leaves open:
  **(a)** a slice whose length is a run-time value is measured for the compile-time rule 1 by its MAXIMUM extent — the
  model §2023:14.8.2.2 gives an occurs-depending group ("the maximum length is used"): identifier-1's positions from a
  static leftmost position on, or all of them, where identifier-1's positions are at their bound when its own length
  is a run-time value — a DYNAMIC LENGTH item's §2023:8.5.1.10.1 maximum size (so a slice of a `LIMIT 3` item never fits a
  4-byte group formal), and for an ANY LENGTH item, whose description fixes no bound (§2023:13.18.2.4 GR1 b): its length
  is the argument's), the largest character string the run time holds, so every group formal is admitted (kb/Work
  PB3128) — and by its evaluated length at the boundary: the universal lane raises
  EC-OO-UNIVERSAL when it is shorter than the group formal; the typed lane's formal holds the slice padded to the
  formal's width and writes back through the slice, truncated to it. Such a slice states its storage area WITH its
  evaluated length (`CellPointer.Extent`), so a formal is laid over a slice at least as long as itself (§2023:14.2.3 GR8: a
  second argument over the same positions sees its stores, on the CALL lane too) and never reads or writes past a
  shorter one; **(b)** the ANY LENGTH item of match rule 3 e) is read through §2023:14.8.2.3.2
  "Additionally" d)/e) (a formal's ANY LENGTH matches any argument length, every other clause still compared; an ANY
  LENGTH argument needs an ANY LENGTH formal), because the literal reading empties §2023:13.18.2 for methods. One relation
  serves both lanes, so through a universal receiver a `PIC X(4)` argument matches a `PIC X ANY LENGTH` formal and the
  bound method is then refused by §2023:14.9.23.4 GR7 c) (EC-OO-UNIVERSAL, not EC-OO-METHOD); a slice matches no ANY LENGTH
  formal (it has no PICTURE clause). GnuCOBOL 3.2 gives no answer: its prototype conformance check reads identifier-1's
  description and ignores the modifier (measured). Witnesses: `conformance:cases/_unassigned/pb480_universal_refmod_evaluated_length`,
  `conformance:cases/_unassigned/pb2077_refmod_argument_both_lanes`, `conformance:cases/_unassigned/pb480_universal_any_length_omitted`,
  `conformance:cases/_unassigned/pb3128_refmod_any_length_argument`, `conformance:cases/_unassigned/pb3128_refmod_dynamic_length_argument`; the CALL and function lanes ask the same relation at bind time (kb/Work PB3101): `conformance:cases/_unassigned/pb3101_activation_lanes_one_relation` and the negatives `pb3101-prototype-call-refmod-by-reference`, `pb3101-function-refmod-by-reference`, `pb3101-nested-refmod-into-any-length`.
