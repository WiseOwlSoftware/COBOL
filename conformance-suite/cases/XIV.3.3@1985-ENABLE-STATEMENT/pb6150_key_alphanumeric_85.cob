      *>--- conformance-case
      *> id: pb6150_key_alphanumeric_85
      *> rules: [1985-SR-XIV.3.3.3-4, 1985-SR-XIV.3.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.3.3 SR4 (ENABLE) and XIV 3.2.3 SR4
      * (DISABLE): "Literal-1 or the content of the data item
      * referenced by identifier-1 must be defined as alphanumeric."
      * KEY-PLAIN is PIC X(8), category alphanumeric, so both
      * statements are accepted; the program fails if either is
      * refused. The alphanumeric edited counterpart is refused
      * (pb6150_enable_key_alphanumeric_edited). No MCS definitions
      * exist, so the statements only set their status keys.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6150A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KEY-PLAIN PIC X(8) VALUE "PASSWORD".
       COMMUNICATION SECTION.
       CD CD-OUT FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           STATUS KEY IS STAT-KEY
           SYMBOLIC DESTINATION IS DEST-NAME.
       PROCEDURE DIVISION.
       P1.
           ENABLE OUTPUT CD-OUT WITH KEY KEY-PLAIN.
           DISABLE OUTPUT CD-OUT WITH KEY KEY-PLAIN.
           STOP RUN.
