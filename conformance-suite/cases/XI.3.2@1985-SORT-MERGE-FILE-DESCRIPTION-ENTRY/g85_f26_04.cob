      *>--- conformance-case
      *> id: g85_f26_04
      *> rules: [1985-SR-XI.3.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.3 SR3 - "One or more record description entries
      *>   must follow the sort-merge file description entry; however,
      *>   no input-output statements may be executed for this sort or
      *>   merge file."
      *>   cite.py: OK  §1985:XI.3.2.3 SR3
      *> The permitted form: SD SRT followed by TWO record descriptions
      *> (SR-A and SR-B), and the file reached only by SORT, RELEASE
      *> and RETURN - never by OPEN, READ, WRITE or CLOSE. A compiler
      *> that rejects a second record under an SD fails to translate.
      *> Supporting rules for the output (not witnessed rows):
      *>   §1985:XI.4.3.4 GR1 "these records automatically share the
      *>   same storage area" (cite.py: OK) - a record built in SR-A is
      *>   seen through SR-B's key SR-K;
      *>   §1985:XI.4.3.4 GR2 "as determined by the keys listed in the
      *>   SORT or MERGE statement" (cite.py: OK).
      *> Derivation, by line: released CC03 (via SR-A), AA01 (via
      *> SR-B), BB02 (via SR-A); ascending on SR-K gives AA01, BB02,
      *> CC03; then DONE 3. If the two records did not share the area,
      *> the SR-A releases would sort on an unset key and the order
      *> would differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2604.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT ASSIGN TO "G85F2604.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SRT.
       01  SR-A PIC X(4).
       01  SR-B.
           05  SR-K PIC X(2).
           05  SR-D PIC X(2).
       WORKING-STORAGE SECTION.
       01  WS-R PIC X(4).
       01  WS-EOF PIC 9 VALUE 0.
       01  WS-CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           SORT SRT ON ASCENDING KEY SR-K
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "DONE " WS-CNT.
           STOP RUN.
       IN-P SECTION.
       I-1.
           MOVE "CC03" TO SR-A.
           RELEASE SR-A.
           MOVE "AA" TO SR-K.
           MOVE "01" TO SR-D.
           RELEASE SR-B.
           MOVE "BB02" TO SR-A.
           RELEASE SR-A.
       OUT-P SECTION.
       O-1.
           PERFORM UNTIL WS-EOF = 1
               RETURN SRT INTO WS-R
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       ADD 1 TO WS-CNT
                       DISPLAY WS-R
               END-RETURN
           END-PERFORM.
