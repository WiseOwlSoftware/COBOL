## Two entries of one external indexed file that list the same alternate keys in another order agree (PB5567)

`FileEntryIdentity.Segments` joined the ALTERNATE RECORD KEY clauses in written order, so two describers of an external indexed file that named the same alternate keys, with the same descriptions and DUPLICATES phrases, in another order were refused with COBOL6847 at every edition. §1985:IX.2.3.4 GR1 asks for "the same number of alternate record keys, and the same DUPLICATES phrase", and §1985:IX.2.3.3 SR1 lets the clauses appear in any order; §2023:12.4.5.3 GR1 k) is silent on order too.

Re-probe: the note's program was refused before the change and prints `ALPHA` and `MAIN DONE` after it. The fix sorts the ALT segment like the COLLATING segment, and names an alternate key in the COLLATING segment by its name rather than by its index among the clauses (left as an index it would have kept the order in the identity). The sweep of the other segments found none that read a clause order the entry does not specify.

Goldens: `IX.2.3@1985-FILE-CONTROL-ENTRY/pb5567-external-alternate-keys-in-another-order` (accepted at 1985, runs) and `pb5567-external-alternate-keys-duplicates-differ` (refused with COBOL6847 at every edition). The row `1985-GR-IX.2.3.4-1` goes from PARTIAL to CONFORMS (1985 GAP 60 to 59).
