      *>--- conformance-case
      *> id: pb5072-value-under-external-record-at-85
      *> rules: [GR-13.18.63.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5072 - the edition half of
      *>   pb5072_value_under_external_record_2002. 1985 VI.5.15.3 SR4:
      *>   "The VALUE clause must not be specified in any entry which is
      *>   part of the description or redefinition of an external data
      *>   record." EXT-FLD is part of EXT-REC; the level-88 VALUE is
      *>   the condition-name entry the rule admits. Expected: COBOL0900
      *>   at 85, at the VALUE of EXT-FLD only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5072NVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-REC EXTERNAL.
           05  EXT-FLD PIC X(4) VALUE "ABCD".
           05  EXT-FLG PIC 9.
               88  EXT-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "WXYZ" TO EXT-FLD
           MOVE 1 TO EXT-FLG
           DISPLAY EXT-FLD
           IF EXT-ON DISPLAY "ON" END-IF
           STOP RUN.
