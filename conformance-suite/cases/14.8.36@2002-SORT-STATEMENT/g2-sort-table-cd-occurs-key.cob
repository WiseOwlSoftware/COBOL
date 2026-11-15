      *>--- conformance-case
      *> id: g2-sort-table-cd-occurs-key
      *> rules: [2002-SR-14.8.36.2-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.36.2 SR15 - "The KEY phrase may be omitted only if
      *> the description of the table referenced by data-name-2
      *> contains a KEY phrase." The granted form: SORT data-name-2
      *> with no KEY phrase, the tables lying in a record under an
      *> output CD (§2002:A.1.2.2 SR9), a reach the 2023 text has no
      *> CD for. The CD facility is obsolete (COBOL0903 warning).
      *> §2002:14.8.36.3 GR20 "If the KEY phrase is not specified, the
      *> sequence is determined by the KEY phrase in the data
      *> description entry of the table referenced by data-name-2."
      *>   CTAB ASCENDING KEY CK: CK 3,1,2 / CV C,A,B -> SORTED=ABC
      *>   CT2 DESCENDING KEY DK: DK 1,3,2 / DV P,R,Q -> SORTED=RQP
      *>   (GR18b: the higher key value has the lower occurrence)
      *> Fails if the KEY-less SORT of a CD table is refused, or if the
      *> sequence ignores the OCCURS KEY phrase or its direction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRT01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CTAB OCCURS 3 TIMES ASCENDING KEY CK.
             10 CK PIC 9.
             10 CV PIC X.
          05 CT2 OCCURS 3 TIMES DESCENDING KEY DK.
             10 DK PIC 9.
             10 DV PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO CK(1) MOVE 1 TO CK(2) MOVE 2 TO CK(3)
           MOVE "C" TO CV(1) MOVE "A" TO CV(2) MOVE "B" TO CV(3)
           MOVE 1 TO DK(1) MOVE 3 TO DK(2) MOVE 2 TO DK(3)
           MOVE "P" TO DV(1) MOVE "R" TO DV(2) MOVE "Q" TO DV(3)
           SORT CTAB
           DISPLAY "SORTED=" CV(1) CV(2) CV(3)
           SORT CT2
           DISPLAY "SORTED=" DV(1) DV(2) DV(3)
           STOP RUN.
