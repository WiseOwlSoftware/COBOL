**2026-11-15, lead from the PB5579 review (round 2, R2-1, low, not blocking).** Source: E:/COBOL-coord/scratch/reports/w20262043e-PB5579-report.md.

`conformance-suite/cases/14.9.51-WRITE/pb5579_write_file_exceptions.cob:68`: the zero-length-literal leg of `pb5579_write_file_exceptions` reads the record back into a
10-byte PIC X buffer, so its golden cannot tell a length-0 record from a 1-byte space record; the R1-1 fix (a zero-length literal WRITE FILE FROM sends length 0)
is witnessed only by the uncommitted byte dump. The fix gives the leg a witness the golden can see: read it back through a record whose length the program
displays (a variable-length record with `RECORD VARYING ... DEPENDING ON`, or the file's size after CLOSE through a LINE SEQUENTIAL / byte read), and add the
expected length to the `.out`.

**Sites:** `conformance-suite/cases/14.9.51-WRITE/pb5579_write_file_exceptions.cob` (the zero-length-literal leg and its `.out`; no code file).
