      *>--- conformance-case
      *> id: pb5276-sort-giving-same-record-area-85
      *> rules: [SR-14.9.40.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5276. §1985:XI.4.4.3 SR9: "File-names associated with
      *>   the GIVING phrase may not be specified in the same SAME
      *>   clause", and SAME RECORD AREA is a SAME clause of the 1985
      *>   format (§1985:VII.2.13.2). OUT1 and OUT2 are the GIVING files
      *>   of the SORT and are named in one SAME RECORD AREA clause.
      *>   Expected: COBOL1757 at 85. Fails if only SAME AREA is asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5276R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "P5276I1".
           SELECT IN2 ASSIGN TO "P5276I2".
           SELECT OUT1 ASSIGN TO "P5276O1".
           SELECT OUT2 ASSIGN TO "P5276O2".
           SELECT SW ASSIGN TO "P5276SW".
       I-O-CONTROL.
           SAME RECORD AREA FOR OUT1 OUT2.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(4).
       FD  IN2.
       01  IN2-REC PIC X(4).
       FD  OUT1.
       01  OUT1-REC PIC X(4).
       FD  OUT2.
       01  OUT2-REC PIC X(4).
       SD  SW.
       01  SW-REC.
           05  SK PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF1 PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IN1 IN2.
           MOVE "CCCC" TO IN1-REC. WRITE IN1-REC.
           MOVE "AAAA" TO IN1-REC. WRITE IN1-REC.
           MOVE "BBBB" TO IN2-REC. WRITE IN2-REC.
           CLOSE IN1 IN2.
           SORT SW ON ASCENDING KEY SK USING IN1 IN2 GIVING OUT1 OUT2.
           OPEN INPUT OUT2.
           PERFORM UNTIL EOF1 = "Y"
              READ OUT2 AT END MOVE "Y" TO EOF1
                 NOT AT END DISPLAY OUT2-REC END-READ
           END-PERFORM.
           CLOSE OUT2.
           STOP RUN.
