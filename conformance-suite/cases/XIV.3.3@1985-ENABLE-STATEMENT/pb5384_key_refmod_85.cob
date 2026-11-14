      *>--- conformance-case
      *> id: pb5384_key_refmod_85
      *> rules: [1985-SR-XIV.3.3.3-4, 1985-SR-XIV.3.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.3.3 SR4 (ENABLE) and XIV 3.2.3 SR4 (DISABLE):
      * "Literal-1 or the content of the data item referenced by
      * identifier-1 must be defined as alphanumeric." A reference
      * modification of a numeric or numeric edited item is: "the
      * categories numeric, numeric edited, and alphanumeric edited
      * are considered class and category alphanumeric"
      * (IV 4.3.8.4.4 GR5). Each KEY below was refused because the
      * screen read the base item through the reference modification
      * (kb/Work PB5384); the program fails if any is refused. No MCS
      * definitions exist, so the statements only set their status
      * keys; the program displays nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-NUM PIC 9(4) VALUE 1234.
       01 K-EDT PIC ZZ9.99 VALUE " 12.50".
       COMMUNICATION SECTION.
       CD CD-IN FOR INPUT
            SYMBOLIC QUEUE IS IQ
            STATUS KEY IS IST.
       CD CD-OUT FOR OUTPUT
            DESTINATION COUNT IS ODC
            STATUS KEY IS OST
            SYMBOLIC DESTINATION IS ODS.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "QUEUEA" TO IQ.
           ENABLE INPUT CD-IN WITH KEY K-NUM (1:2).
           DISABLE INPUT CD-IN KEY K-NUM (3:).
           ENABLE OUTPUT CD-OUT KEY K-EDT (1:3).
           DISABLE OUTPUT CD-OUT WITH KEY K-EDT (2:2).
           STOP RUN.
