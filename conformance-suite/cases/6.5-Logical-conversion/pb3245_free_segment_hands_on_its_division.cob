      *>--- conformance-case
      *> id: pb3245_free_segment_hands_on_its_division
      *> rules: [GR-6.5-1, GR-6.5-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>SOURCE FORMAT FREE
*> kb/Work PB3245 (review finding R1-1 sibling) - 6.5 5): "If the line
*>   is a fixed-form line, contains a source indicator, and is not a
*>   continuation line, the program text area of that line is copied to
*>   the resultant compilation group." The fixed-form REMARKS line below
*>   stands in the PROCEDURE DIVISION, which a FREE segment entered, so
*>   it is a procedure paragraph (ISO 8.9 reserves REMARKS at no edition)
*>   and no comment-entry. The free segment used to keep the division it
*>   read to itself, so the fixed segment read REMARKS as a comment-entry
*>   paragraph, dropped its body, and the program was refused
*>   (COBOL0001 at the period of REMARKS).
*>   cite.py --check 6.5 "If the line is a fixed-form line, contains a
*>     source indicator, and is not a continuation line" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3245SG.
PROCEDURE DIVISION.
    PERFORM REMARKS.
    STOP RUN.
>>SOURCE FORMAT FIXED
       REMARKS.
           DISPLAY "IN-REMARKS".
