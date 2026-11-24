      *>--- conformance-case
      *> id: pb6150_disable_key_alphanumeric_edited
      *> rules: [1985-SR-XIV.3.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.2.3 SR4: "Literal-1 or the content of the
      * data item referenced by identifier-1 must be defined as
      * alphanumeric." KEY-EDT is PIC XXBXX, category alphanumeric
      * edited. IV 4.3.3 names alphanumeric and alphanumeric edited
      * as two of the five categories, and INSPECT treats an
      * "alphanumeric edited, numeric edited, or unsigned numeric"
      * item as alphanumeric only "as though it had been redefined"
      * (VI 6.18.4 GR3b), so an alphanumeric edited item is not
      * defined as alphanumeric. The DISABLE below is refused at 85
      * with COBOL6171 (kb/Work PB6150, PB5722); a plain PIC X(8) key
      * is accepted (pb6150_key_alphanumeric_85). The KEY phrase is
      * absent from 2002 on (pb4677-key-phrase-removed-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6150D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KEY-EDT PIC XXBXX VALUE "AB CD".
       COMMUNICATION SECTION.
       CD CD-OUT FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           STATUS KEY IS STAT-KEY
           SYMBOLIC DESTINATION IS DEST-NAME.
       PROCEDURE DIVISION.
       P1.
           DISABLE OUTPUT CD-OUT WITH KEY KEY-EDT.
           STOP RUN.
