## Entry 3962 — 2026-11-22 23:59 PST — PB6075: an index data item as a report SOURCE at 85, and the report rules 2014 added, swept at 2002

Two leads from PB5775. The first asked whether PB5188's refusal of an index data item as a report SOURCE at COBOL-85 rests on the Procedure-Division-only
§1985:VI.5.14.3 SR5 that PB5774 showed does not reach a Report Section clause. The re-probe (`SOURCE WS-IX`, `USAGE INDEX`) is refused COBOL2770 at every edition, and
at 85 the refusal is right on a different rule: §1985:XIII.3.18.3 SR2 makes identifier-1 conform "to the rules for sending items in the MOVE statements", and
§1985:VI.6.19.3 SR4 bars an index data item from any MOVE. No legal 1985 source is refused. What was wrong was the message: at 85 it also cited SR5, and at 2002 and
2014 it cited COBOL-2023's rules (`§2023:14.9.25.3 SR1`, `§2023:13.18.60.3 SR10`, the SOURCE rules). `MoveTable16.SenderClassRefusal` now takes whether the operand is a
Procedure Division statement's (the SOURCE caller says no, and SR5 is left out at 85), and the MOVE and USAGE rules and the SOURCE rules are chosen by edition
through `EditionInfo.Cite`: `§2002:14.8.24.2 SR1`, `§2002:13.16.58.2 SR9`, `§2002:13.16.51.2 SR2` and `§2002:13.16.51.3 GR1` at 2002, the `§2014:` pair at 2014.
Three message pins: `cases/13.18.53-SOURCE/pb6075-source-index-cites-{1985,2002,2014}` with their `wiseowl/diagnostics` files.

The second lead, 2014-added report rules enforced at 2002, was walked as a per-clause diff of §2002:13.16 against §2023:13.18 over every report clause and statement. The
rules the later text adds are each gated or unreachable at 2002 (SUM ROUNDED needs COLUMN, CONTROL no OCCURS, variable-length group in CONTROL and SOURCE,
FUNCTION SUM in a report description entry, exception-checking PERFORM over INITIATE and TERMINATE, the zero-length literal in VALUE); TYPE, LINE, COLUMN, NEXT
GROUP and PAGE carry word-identical rules. Found on the way and left registered in the landing addendum: a zero-length literal or a refused DYNAMIC LENGTH item at 2002
draws a second, cascading diagnostic (COBOL2247, COBOL1561) worded from COBOL-2023, and the pointer, message-tag and object arms of the MOVE class refusal still word 2023 at
2002 and 2014. No inventory row closes: `closes_rows_reason` says so.

