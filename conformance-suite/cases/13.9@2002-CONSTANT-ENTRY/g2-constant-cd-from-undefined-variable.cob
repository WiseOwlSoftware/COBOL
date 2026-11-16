      *>--- conformance-case
      *> id: g2-constant-cd-from-undefined-variable
      *> rules: [2002-SR-13.9.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR8 - "Compilation-variable-name-1 shall be a
      *> compilation-variable-name for which the defined condition is
      *> currently true". CV is defined and then set OFF by the DEFINE
      *> directive (§2002:7.2.9.3 GR2), so its defined condition is
      *> false where KC, a constant entry under a CD (§2002:13.1.1),
      *> names it; the
      *> program is refused at the KC entry (it fails if the OFF
      *> variable's old text is used or the entry compiles).
       >>DEFINE CV AS "ABC"
       >>DEFINE CV OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNP03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KC CONSTANT FROM CV.
       01  CDREC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
