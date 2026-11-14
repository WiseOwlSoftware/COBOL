---
order: 24
---
> ⚖ **DETERMINATION — a SUM entry's category is judged against the INTEGER numeric row of Table 16 (§2023:13.18.54.3 SR2)**
> (2026-10-04; kb/Work PB1295). SR2 requires "the category of the subject of the entry" to be "valid as the category of a
> receiving operand in a MOVE statement for a sending operand of the category numeric", and Table 16 (§2023:14.9.25.3 SR10)
> splits the numeric sender into an Integer and a Noninteger row. The category of the entry's PICTURE does not say
> whether the sum has a decimal point, so the screen refuses only what the INTEGER row refuses — a numeric operand does
> not move to an alphabetic or a boolean item — and admits the alphanumeric and national categories the integer row
> allows; the noninteger row only adds "No" cells, and a compiler that rejected the alphanumeric entry would refuse a
> program some sums fill legally. Neither ISO nor GnuCOBOL (which checks no such category) offers a narrower reading.
> Pinned by `negative/pb1295-sum-entry-*`. In the same change §2023:8.4.2.3.3 SR8 ("neither a sum counter nor the LINE-COUNTER
> and PAGE-COUNTER identifiers may be used as a subscript") is screened once per written entry over every clause that
> carries a subscript (COBOL2751), and a report section item that is neither is the clause's own rule
> (§2023:13.18.54.3 SR6, §2023:13.18.53.3 SR4); a PRESENT WHEN condition is §2023:13.15.3 SR16's. Pinned by `negative/pb1474-*` and
> `85/pb1474_report_subscripts_legal`.
