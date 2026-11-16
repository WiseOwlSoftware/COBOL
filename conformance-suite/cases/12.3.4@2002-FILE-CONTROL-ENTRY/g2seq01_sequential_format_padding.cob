      *>--- conformance-case
      *> id: g2seq01_sequential_format_padding
      *> rules: [2002-GR-12.3.4.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.3 GR8 - "The sequential format defines a file
      *> connector for a sequential file."
      *>   cite.py --check "§2002:12.3.4.3 GR8" "The sequential format
      *>   defines a file connector for a sequential file" -> OK
      *> The 2002 sequential format (Format 3) carries the PADDING
      *> CHARACTER clause, with data-name-8 or literal-2; 2023's Format 3
      *> has none. This case writes Format 3 entries WITH that clause:
      *> F1 with literal-2 and ORGANIZATION IS SEQUENTIAL, F2 with
      *> data-name-8 and the ORGANIZATION clause omitted (§2002:12.3.4.9.2
      *> GR5: "When the ORGANIZATION clause is not specified, sequential
      *> organization is implied" -> cite.py OK).
      *> §2002:12.3.4.10: "The PADDING CHARACTER clause is an obsolete
      *> element in this International Standard" -> cite.py OK; so the
      *> compiler may warn (COBOL0903), which does not fail the case.
      *> §2002:12.3.4.10.2 SR3: data-name-1 "shall be a one-character
      *> data item of category alphanumeric, defined in the
      *> working-storage, local-storage, or linkage section" -> cite.py
      *> OK; PADC is PIC X in working-storage.
      *> §2002:12.3.4.10.3 GR1: on input "a logical record that consists
      *> solely of padding characters shall be ignored" -> cite.py OK;
      *> no record written here consists solely of '*' or '#', so no
      *> record may be dropped.
      *> DERIVATION OF THE EXPECTED OUTPUT: each connector is a
      *> sequential file connector, so (§2002:12.3.4.9.2 GR2) a record is
      *> identified by "a predecessor-successor relationship established
      *> when the record is placed into the file" and (§2002:12.3.4.4.3
      *> GR2a) sequential READs return the records in WRITE order,
      *> including a record added by OPEN EXTEND; past the last record
      *> the READ takes AT END with status 10 (§2002:9.1.12.3).
      *>   F1: OUTPUT HELLO, WORLD; EXTEND AGAIN
      *>       => F1:00 / F1:HELLO / F1:WORLD / F1:AGAIN / F1:END 10
      *>   F2: OUTPUT ONE, TWO; EXTEND THREE
      *>       => F2:00 / F2:ONE / F2:TWO / F2:THREE / F2:END 10
      *> A leg fails if the entry is refused at 2002, if the connector
      *> is not sequential (no EXTEND, or another order), or if a record
      *> is lost to padding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SEQ01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2SEQ1.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "G2SEQ2.DAT"
               ACCESS MODE IS SEQUENTIAL
               PADDING CHARACTER IS PADC
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1          PIC X(10).
       FD  F2.
       01  R2          PIC X(10).
       WORKING-STORAGE SECTION.
       01  PADC        PIC X VALUE "#".
       01  ST1         PIC XX.
       01  ST2         PIC XX.
       01  WS-EOF      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           DISPLAY "F1:" ST1.
           MOVE "HELLO" TO R1. WRITE R1.
           MOVE "WORLD" TO R1. WRITE R1.
           CLOSE F1.
           OPEN EXTEND F1.
           MOVE "AGAIN" TO R1. WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ F1
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "F1:END " ST1
                   NOT AT END DISPLAY "F1:" R1
               END-READ
           END-PERFORM.
           CLOSE F1.
           OPEN OUTPUT F2.
           DISPLAY "F2:" ST2.
           MOVE "ONE" TO R2. WRITE R2.
           MOVE "TWO" TO R2. WRITE R2.
           CLOSE F2.
           OPEN EXTEND F2.
           MOVE "THREE" TO R2. WRITE R2.
           CLOSE F2.
           OPEN INPUT F2.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ F2
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "F2:END " ST2
                   NOT AT END DISPLAY "F2:" R2
               END-READ
           END-PERFORM.
           CLOSE F2.
           STOP RUN.
