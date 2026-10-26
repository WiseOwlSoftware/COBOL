*>--- conformance-case
*> id: pb3682-typedef-usage-binary-over-alphanumeric
*> rules: [SR-13.18.60.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.60.3 SR3: an elementary data item "subordinate to a group item whose declaration contains, a USAGE
      *> clause specifying BINARY, COMPUTATIONAL, or PACKED-DECIMAL shall be specified only with a picture character-string
      *> that describes a numeric item" (cite.py: OK  13.18.60.3 3)). The usage reaches the member of a TYPEDEF group by
      *> 13.18.60.4 GR1 (cite.py: OK  13.18.60.4 1)), so the PICTURE X member is refused in the declaration.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE BINARY.
          05 A PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
