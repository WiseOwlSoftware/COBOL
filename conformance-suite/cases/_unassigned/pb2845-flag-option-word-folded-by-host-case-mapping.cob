*>--- conformance-case
*> id: pb2845-flag-option-word-folded-by-host-case-mapping
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2845, PB4240 - the option words of >>FLAG-14 are
      *> keywords, matched on the basic letters alone (ISO 8.1.3.2 3) a)),
      *> not by the host's upper-casing: U+017F LATIN SMALL LETTER LONG S is
      *> no basic letter, so "I-O-ſTATUS-07" is NOT the option
      *> I-O-STATUS-07 (7.3.15 format: the option is one of the words the
      *> directive lists), though .NET's ToUpperInvariant raises it to S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2845N.
       PROCEDURE DIVISION.
       >>FLAG-14 I-O-ſTATUS-07 ON
           STOP RUN.
