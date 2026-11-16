      *>--- conformance-case
      *> id: pb5731_sort_sd_format3_odo_sized_at_maximum
      *> rules: [1985-SR-XI.4.4.3-10, 1985-SR-XI.4.4.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5731. §1985:VII.3.8.4 GR15 sizes a format 3 record at the
      *> MAXIMUM of each variable length item (cite.py --check OK: "the sum
      *> of the maximum number of characters in any variable length item
      *> subordinate to the record"). SW describes SR = 1 + 5 = 6 characters
      *> and SR2 = 7, so its records are 6 and 7 characters. The GIVING file
      *> G1 allows 3 to 8, so §1985:XI.4.4.3 SR10 ("must not be less than the
      *> smallest record nor larger than the largest record described for
      *> file-name-3") is met; and the key SK of SR2, at positions 3 to 4,
      *> lies within the minimum record size x = 6 (SR4f: "where x equals
      *> the minimum record size specified for the file referenced by
      *> file-name-1").
      *> DERIVATION: the records are 5ABCDE and 3XYZ. Sorted on SN (the first
      *> character) ascending: 3XYZ before 5ABCDE. Sorted on SK (positions 3
      *> to 4: BC and YZ) ascending: 5ABCDE before 3XYZ.
      *> Fails if: the program is refused with COBOL1757 (SW sized at 2 to 7)
      *> or COBOL0874 (SK past a minimum of 2), or the order differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5731S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB5731S.TMP".
           SELECT U1 ASSIGN TO "PB5731S1.DAT".
           SELECT G1 ASSIGN TO "PB5731S2.DAT".
       DATA DIVISION.
       FILE SECTION.
       SD SW RECORD CONTAINS 6 TO 7 CHARACTERS.
       01 SR.
          05 SN PIC 9.
          05 ST PIC X OCCURS 1 TO 5 TIMES DEPENDING ON SN.
       01 SR2.
          05 SA PIC XX.
          05 SK PIC XX.
          05 SC PIC XXX.
       FD U1.
       01 U1R PIC X(6).
       FD G1 RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS.
       01 G1R PIC X(8).
       WORKING-STORAGE SECTION.
       01 EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT U1.
           MOVE "5ABCDE" TO U1R. WRITE U1R.
           MOVE "3XYZ  " TO U1R. WRITE U1R.
           CLOSE U1.
           SORT SW ASCENDING KEY SN
               USING U1 GIVING G1.
           PERFORM SHOW.
           SORT SW ASCENDING KEY SK
               USING U1 GIVING G1.
           PERFORM SHOW.
           STOP RUN.
       SHOW.
           MOVE "N" TO EOF-SW.
           OPEN INPUT G1.
           PERFORM UNTIL EOF-SW = "Y"
               READ G1 AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY "[" G1R (1:6) "]"
               END-READ
           END-PERFORM.
           CLOSE G1.
