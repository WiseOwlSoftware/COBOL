      *>--- conformance-case
      *> id: g85_bf04_02
      *> rules: [1985-GR-VI.5.13.4-7, 1985-GR-VI.5.13.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13.4 GR7 - SYNC in and under an OCCURS entry
      *> §1985:VI.5.13.4 GR9 - no automatic alignment of DISPLAY items
      * GR7: T1 carries SYNC on an item subordinate to the OCCURS entry,
      * T3 in the entry with the OCCURS clause. "Each occurrence of the
      * data item is SYNCHRONIZED" (GR7a): every occurrence is its own
      * item and keeps its own value (T1(n), T3(n) lines; a layout that
      * overlapped occurrences or synchronized only the first breaks
      * them). GR7b generates any implicit FILLER "for each occurrence",
      * so every occurrence has one size and the table is 3 occurrences
      * of it: PEROCC=YES. The sizes themselves are the implementor's
      * (GR8b): WiseOwl COBOL documents no FILLER (DOC-A.1-195) and a
      * 2-byte S9(4) COMP (DOC-A.1-205/208): OCC=1+2=3, TAB=9,
      * T3LEN=1+6=7. GR9: automatic alignment may not touch "within a
      * record, data items whose usage is DISPLAY": D1 is exactly its
      * characters, D1LEN=6 and D1=A1234C, whatever the implementor
      * elects. For the binary item the option is the implementor's;
      * WiseOwl COBOL does not take it (DOC-A.1-195), so B1LEN=4 (an
      * elected alignment answers 5 or 6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0402.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      * T1: SYNCHRONIZED on an item subordinate to the OCCURS entry.
       01 T1.
           05 T1E OCCURS 3 TIMES.
               10 T1A PIC X.
               10 T1B PIC S9(4) COMP SYNC.
      * T3: SYNCHRONIZED in the entry that carries the OCCURS clause.
       01 T3.
           05 T3P PIC X VALUE "P".
           05 T3E PIC S9(4) COMP SYNC OCCURS 3 TIMES.
      * D1: a record of DISPLAY items only, no SYNCHRONIZED clause.
       01 D1.
           05 D1A PIC X VALUE "A".
           05 D1B PIC 9(4) VALUE 1234.
           05 D1C PIC X VALUE "C".
      * B1: a binary item in a record, no SYNCHRONIZED clause.
       01 B1.
           05 B1A PIC X.
           05 B1B PIC S9(4) COMP.
           05 B1C PIC X.
       01 IX PIC 9.
       01 W-NUM PIC S9(4) SIGN LEADING SEPARATE.
       01 W-LEN PIC 9(4).
       01 W-OCC PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3
               MOVE IX TO T1A (IX)
               COMPUTE T1B (IX) = IX * 1000 + 7
               COMPUTE T3E (IX) = IX * -11
           END-PERFORM.
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3
               MOVE T1B (IX) TO W-NUM
               DISPLAY "T1(" IX ")=" T1A (IX) " " W-NUM
           END-PERFORM.
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3
               MOVE T3E (IX) TO W-NUM
               DISPLAY "T3(" IX ")=" W-NUM
           END-PERFORM.
           DISPLAY "T3P=" T3P.
           COMPUTE W-OCC = FUNCTION LENGTH (T1E (2)).
           COMPUTE W-LEN = FUNCTION LENGTH (T1).
           DISPLAY "OCC=" W-OCC " TAB=" W-LEN.
           IF W-LEN = 3 * W-OCC
               DISPLAY "PEROCC=YES"
           ELSE
               DISPLAY "PEROCC=NO"
           END-IF.
           COMPUTE W-LEN = FUNCTION LENGTH (T3).
           DISPLAY "T3LEN=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (D1).
           DISPLAY "D1LEN=" W-LEN.
           DISPLAY "D1=" D1.
           COMPUTE W-LEN = FUNCTION LENGTH (B1).
           DISPLAY "B1LEN=" W-LEN.
           STOP RUN.
