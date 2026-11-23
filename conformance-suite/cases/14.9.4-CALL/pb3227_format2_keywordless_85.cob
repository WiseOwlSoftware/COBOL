*>--- conformance-case
*> id: pb3227_format2_keywordless_85
*> rules: [GR-14.9.4.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3227, the edition below pb3227_format2_keywordless_shares_storage. ISO 14.9.4.4 GR9 decides a
      *> keyword-less argument of a Format-2 CALL from its corresponding formal; COBOL-85 has one CALL format, with
      *> neither the AS phrase nor BY VALUE, so the program that leg 1 of that test writes is refused at 85
      *> by the construct it lacks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227M85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) BINARY VALUE 7.
       01 NUM PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
           CALL "PB3227V85" AS NESTED USING BY VALUE N NUM NUM
           DISPLAY "AFTER 1: " NUM
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227V85.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE V BY REFERENCE A B.
           MOVE "5678" TO AX
           DISPLAY "B 1: " B
           GOBACK.
       END PROGRAM PB3227V85.
       END PROGRAM PB3227M85.
