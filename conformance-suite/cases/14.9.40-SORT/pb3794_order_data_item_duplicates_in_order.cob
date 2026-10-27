      *>--- conformance-case
      *> id: pb3794_order_data_item_duplicates_in_order
      *> rules: [FMT-14.9.40.2, GR-14.9.40.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3794 - a COBOL-85 data item named ORDER and the SORT statement's DUPLICATES phrase written out in
      *> full. ISO 14.9.40.2 prints [ WITH DUPLICATES IN ORDER ] (cite.py: OK 14.9.40.2 "DUPLICATES"), and this
      *> compiler's COBOL-85 reserved-word table leaves ORDER free (reserved-words.json r85 false; reserved from
      *> 2002, docs/CONFORMANCE.md D-RW3), so the declaration is legal at 85. The leg that fails: the reservation
      *> gate retypes every ORDER of a program that declares one, and the phrase, spelling only the token, was
      *> refused with "unexpected 'ORDER'".
      *>
      *> EXPECTED VALUES, DERIVED: 14.9.40.4 GR3 b) - with the DUPLICATES phrase, records whose keys are all equal
      *> are returned "in the order in which these records are released by an input procedure". The input
      *> procedure releases B1, A2, B3 (key, then sequence); ascending on the key returns A2 first, then the two
      *> B records in release order, B1 before B3. ORDER keeps its VALUE 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794ORD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb3794-order.srt".
       DATA DIVISION.
       FILE SECTION.
       SD S.
       01 S-R.
          05 S-K PIC X.
          05 S-N PIC 9.
       WORKING-STORAGE SECTION.
       01 ORDER PIC 9 VALUE 7.
       01 WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P-MAIN.
           SORT S ON ASCENDING KEY S-K WITH DUPLICATES IN ORDER
               INPUT PROCEDURE IS P-IN OUTPUT PROCEDURE IS P-OUT.
           DISPLAY "ORDER=" ORDER.
           STOP RUN.
       P-IN.
           MOVE "B1" TO S-R. RELEASE S-R.
           MOVE "A2" TO S-R. RELEASE S-R.
           MOVE "B3" TO S-R. RELEASE S-R.
       P-OUT.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN S AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY S-R
               END-RETURN
           END-PERFORM.
