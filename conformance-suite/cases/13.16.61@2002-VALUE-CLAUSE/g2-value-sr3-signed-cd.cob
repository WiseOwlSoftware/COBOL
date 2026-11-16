      *>--- conformance-case
      *> id: g2-value-sr3-signed-cd
      *> rules: [2002-SR-13.16.61.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR3 - "If a signed numeric literal is
      *> specified, the subject of the entry shall be a signed numeric
      *> data item."  The accepted leg: a signed literal on a SIGNED
      *> item of a record under a CD entry (§2002:A.1.1.2: the CD area
      *> "may be implicitly redefined by user-specified record
      *> description entries"); §2002:13.16.61 "The VALUE clause
      *> specifies the initial value of communication section" items,
      *> and §2002:13.16.61.3 GR4 "VALUE clauses in the working-storage,
      *> local-storage, and communication sections take effect" when the
      *> program is placed in its initial state.
      *> CS is S99 SIGN LEADING SEPARATE: §2002:13.16.50.3 GR6b, the
      *> sign is '+' or '-' in its own leading position, so VALUE -7
      *> displays "-07" and VALUE +12 displays "+12".
      *> Fails if a signed literal on a signed CD item is refused, or
      *> if the CD record's initial values are not stored.
      *> The refusal leg is g2-value-sr3-unsigned-cd.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VS301.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CS PIC S99 SIGN IS LEADING SEPARATE VALUE -7.
           05 CP PIC S99 SIGN IS LEADING SEPARATE VALUE +12.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "CS=" CS " CP=" CP.
           STOP RUN.
       END PROGRAM G2VS301.
