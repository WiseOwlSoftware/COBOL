      *>--- conformance-case
      *> id: pb5276_sort_giving_same_record_area_2023
      *> rules: [SR-14.9.40.3-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5276. §2023:14.9.40.3 SR10 bars the GIVING files of
      *>   one SORT only from the same SAME AREA clause ("File-names
      *>   associated with the GIVING phrase shall not be specified in
      *>   the same SAME AREA clause"), so two of them may share a SAME
      *>   RECORD AREA clause. Each GIVING file receives the whole sorted
      *>   result; OUT2 is read back: AAAA BBBB CCCC. The same program
      *>   is refused at 85 (pb5276-sort-giving-same-record-area-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5276X.
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
