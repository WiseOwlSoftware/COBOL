## Fuzz night 7: file seven GnuCOBOL upstream records and the night's witnesses

The independent refuters (Workflow `fuzz-triage`, slices R1 and R2) re-ran all eight GnuCOBOL-side records of fuzz night 7
(`fuzz-20261027-215606`) under cobc 3.2.0 and upheld every one, with small corrections to citations and wording.
`fuzz_triage.py file --write` filed seven of them as `kind: upstream` notes, PB3998 to PB4004. All are wrong-output at
edition 2014:

- national comparison pads with 0x20 bytes;
- FUNCTION LENGTH of a reference-modified item returns the whole item;
- an alphanumeric-to-national comparison compares raw bytes;
- TEST-NUMVAL and its siblings return 1 for a valid national argument;
- a zero-length literal is read as one space at `-std=cobol2014`;
- TEST-NUMVAL accepts 32 to 38 digits;
- UNSTRING of a national item counts bytes.

It also added the night's program ids as witnesses to 35 existing upstream notes. PB3879 gains fz06094 by hand: the
verdict schema carries one note per line, so slice B3 named it only in its report.

The tool HELD the eighth record (INVOKE of a universal object reference). The refutation's corrected citation, §14.9.23.1,
is not named in the record's Expected result, so filing it would give the note `spec_refs` its body contradicts. It
stays in the triage's `filing.md`, as do three families the adjudicators did not file. Slice B3 found two CONFORMANCE rows
that share the id DOC-A.1-92; that is PB3840, already open.
