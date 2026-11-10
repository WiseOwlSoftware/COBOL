      *>--- conformance-case
      *> id: pb5072_value_under_external_record_2002
      *> rules: [GR-13.18.63.4-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5072 - ISO/IEC 1989:2002 Annex F.2 item 145: the
      *>   data-item format of the VALUE clause "may be specified ... for
      *>   items described with the EXTERNAL clause"; an entry
      *>   subordinate to the external record is one. The MOVE then sets
      *>   the field and the flag. Expected: "WXYZ" then "ON". It fails
      *>   if the VALUE on the subordinate entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5072VE.
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
