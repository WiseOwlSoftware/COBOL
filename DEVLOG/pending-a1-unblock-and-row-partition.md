## PB2556 and PB1253 are released from the SORT and file I/O hold; PB4053 files the planner's row-key partition

The owner took the Annex A.1 documentation rows of the GAP campaign (PB2911) into the Mythos lane on 2026-10-29, then
released PB2556 from the PB3420 hold ("Release it"). PB2556 is one probe: does a refused wide print line on a LINE
SEQUENTIAL report file give `'71'`, as DOC-A.1-159 says, or `'91'`, as the code suggests? It then makes the code and the
document agree. PB1253's remaining GAP row, GR-13.4.5.4-4, documents the same determination, so PB1253 now waits only
on PB2556. PB3420's tier 3 had waited on the weekly quota resets, which have passed. The rest of tier 3 keeps its order.

The owner also asked how to stop `docs/CONFORMANCE.md` from being a single point of sync. `plan_wave.py` chains every
note with a site in that file, so the A.1 notes run one link per wave: PB438, PB464, PB1092 with PB1397, then PB1369.
The root fix is PB3738, already designed and decided, which splits the file into one source per determination plus
keyed register tables. PB4053 files what PB3738 leaves: the planner collides a keyed-table file by path. With row-key
partitioning the A.1 notes would run as one parallel wave, and `enqueue.py`'s existing hold on driver-merged files
would serialize only their landings.
