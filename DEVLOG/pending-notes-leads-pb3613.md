## Unfiled review leads of seven landed branches become kb/Work notes PB3613 to PB3616

Seven branch reports carried leads that no note held. This change files them, one note per mechanism. PB3613: a numeric
FUNCTION value sent to a group keeps its sign and decimal point (source: the PB3570 report's new defect). PB3614:
SequentialJournal holds journal sizes in 32-bit int, so a near-int.MaxValue length word wraps the torn-tail bound and a journal
over 2 GiB throws OverflowException (source: the PB670 report, leads R1-2 and R1-3). PB3615: INITIALIZE of a USAGE BIT table is
quadratic in the occurrence count, in the WITH FILLER pad loop and in a Tier-B bit table (source: the PB3597 report, leads
R1-1 and R1-2). PB3616: a stale EmitLeafMethods summary, the wrong counts in DEVLOG entry 2422 and a PB3586 test that cites
the NOTE of 14.8.2.1 for a question it does not answer (sources: the PB3597 report R1-3 and R1-4, the PB3586 report R2-1).
The PB3608 report's R2-1 is added to PB3609's body. The PB3597 report's three new defects already live in PB3598 (the owner
question and shape 2 as its open items, the ODO INITIALIZE residue as item 3), so they add nothing. The PB3584 report's
R1-1 and R1-2, the PB3608 report's R1-1 and R1-2 and the PB3586 report's R1-2 and R1-3 were answered as fixed and are not filed.
