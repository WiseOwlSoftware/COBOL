*>--- conformance-case
*> id: pb3225-set-capacity-register-second-index-up-by
*> rules: [SR-14.9.39.3-29]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.2 Format 14 - `SET data-name-2 { TO | UP BY | DOWN BY } ...` prints ONE receiving operand and no
      *> ellipsis, and §14.9.39.3 SR29 makes it a CAPACITY register:
      *>   "Data-name-2 shall reference a data item defined in the CAPACITY phrase of a dynamic-capacity-table format
      *>   OCCURS clause."   cite.py: OK  §14.9.39.3 29)  (Syntax rules)
      *> WS-CAP is a CAPACITY register but IX, written first, is an index-name that SR29 does not admit.
      *> No printed SET format admits the list, in EITHER order of its operands (kb/Work PB3225): the statement is
      *> refused with COBOLNET2112 (no-format-admits-receiver). It used to be refused only when the register was
      *> written first; written second it compiled with a not-implemented warning and aborted the run unit after
      *> displaying START. OCCURS DYNAMIC and Format 14 are COBOL-2014 introductions, so below 2014 the OCCURS
      *> clause itself takes the edition-band code and SR29 is not the subject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3225N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAB.
          05 WS-E PIC 9 OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       01 WS-TAB2.
          05 WS-E2 PIC 9 OCCURS DYNAMIC CAPACITY IN WS-CAP2 FROM 2 TO 10.
       01 WS-N PIC 9(4).
       01 WS-T3.
          05 WS-E3 PIC 9 OCCURS 5 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START"
           SET IX WS-CAP UP BY 1
           DISPLAY "END"
           STOP RUN.
       END PROGRAM PB3225N2.
