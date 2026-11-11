      *>--- conformance-case
      *> id: 1985-SR-XIV_3_2_3-4
      *> rules: [1985-SR-XIV.3.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.2.3 SR4: "Literal-1 or the content of the
      * data item referenced by identifier-1 must be defined as
      * alphanumeric." The numeric literal 123 is not, so the DISABLE
      * below is refused at 85 with COBOL6171 (kb/Work PB5082); it
      * compiled clean before. The accepted KEY forms are
      * pb5240_key_function_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIS85SR4.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CD-OUT FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           TEXT LENGTH IS TXT-LEN
           STATUS KEY IS STAT-KEY
           ERROR KEY IS ERR-KEY
           SYMBOLIC DESTINATION IS DEST-NAME.
       PROCEDURE DIVISION.
       P1.
           DISABLE OUTPUT CD-OUT WITH KEY 123.
           STOP RUN.
