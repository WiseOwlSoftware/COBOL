## Five sibling GnuCOBOL records from the batch-1 reviews, vetted

The reviews of fuzz night 10's batch-1 drafts each turned up a defect outside the draft's own ticket. Each was vetted against the ISO text, reproduced on cobc 3.2.0 and 3.3-dev r5751, searched against the 1,827-ticket cache, and corrected by an independent refuter before filing. All five are `upstream_status: unreported`; WiseOwl is correct on each.

PB4594: a contained program ignores its container's PROGRAM COLLATING SEQUENCE. `setup_program` calls `setup_default_collation`, which overwrites the sequence that `cb_build_program` copied from the container, so `IF A1 > "B"` is false and a table SORT orders BCA as ABC in INNER where ISO (§12.3.6.4 rules 1, 9, 10, 11 and 13) requires true and CBA. Both versions are wrong and no ticket covers it; bugs-566 and bugs-435 are close but are other settings and code paths. Its fix must land with PB4542's.

PB4595: in sequential access a relative DELETE removes the slot the RELATIVE KEY names, not the record the last READ returned (§14.9.10.4 2)). READ gives R002, `MOVE 1 TO RK`, DELETE gives 00 and R001 is gone. Both versions are wrong, no ticket covers it, and the PB4539 patch does not change which record goes.

PB4596: in a relative file in dynamic access, a successful random DELETE, REWRITE or WRITE moves the READ NEXT position, because the descriptor offset is the file position indicator. READ 1, DELETE 3, READ NEXT returns R005 where §9.1.11 and §14.9.10.4 9) require R002. Both versions are wrong and no ticket covers it (bugs-589 is an indexed-file lookalike).

PB4597: a random or dynamic relative DELETE or REWRITE with relative key 0 returns status 24 where §14.9.35.4 21) and §14.9.10.4 4) give 23 (READ of the same key already gives 23). Both versions are wrong and no ticket covers it. Our side of the related 2023 WRITE status 34 is PB1192.

PB4598: for a relative file with variable-length records, REWRITE never writes the record-size word, so a record grown from 2 to 6 bytes reads back with LN=02 where §13.18.43.4 13) a) and 15) require 06. Both versions are wrong and no ticket covers it. Writing the size word alone would let REWRITE create a record in an empty slot, so the fix needs PB4539's empty-slot test first; the refuter built both together and checked five probes.

PB4530 (READ PREVIOUS skips records) gains a further witness from the PB4595 vetting: after READ NEXT three times and READ PREVIOUS, a sequential REWRITE returns 00 but overwrites slot 1's size word, leaving R002 unchanged (§14.9.35.4 5)).
