      *>--- conformance-case
      *> id: g2-constant-cd-length-any-length
      *> rules: [2002-SR-13.9.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR10 - "Data-name-1 and data-name-2 shall not be
      *> described with the ANY LENGTH clause". In the nested program
      *> G2CON05I, K is a constant entry under a CD (§2002:13.1.1) whose
      *> LENGTH OF operand L is a linkage item described PIC X ANY
      *> LENGTH (§2002:13.16.2); the program is refused at the K entry
      *> (it fails if the CD constant takes some length for L and
      *> compiles).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON05.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON05I.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L PIC X ANY LENGTH.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  K CONSTANT AS LENGTH OF L.
       01  CDREC PIC X(10).
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM G2CON05I.
       END PROGRAM G2CON05.
