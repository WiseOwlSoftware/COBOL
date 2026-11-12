      *>--- conformance-case
      *> id: 1985-SR-XIV_3_3_3-4
      *> rules: [1985-SR-XIV.3.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.3.3 SR4: "Literal-1 or the content of the
      * data item referenced by identifier-1 must be defined as
      * alphanumeric." NUM-KEY is a numeric data item, so the ENABLE
      * below is refused at 85 with COBOL6171 (kb/Work PB5082); it
      * compiled clean before. The accepted KEY forms are
      * pb5240_key_function_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENA85SR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NUM-KEY PIC 9(5) VALUE 12345.
       COMMUNICATION SECTION.
       CD CD-OUT FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           STATUS KEY IS STAT-KEY
           SYMBOLIC DESTINATION IS DEST-NAME.
       PROCEDURE DIVISION.
       P1.
           ENABLE OUTPUT CD-OUT WITH KEY NUM-KEY.
           STOP RUN.
