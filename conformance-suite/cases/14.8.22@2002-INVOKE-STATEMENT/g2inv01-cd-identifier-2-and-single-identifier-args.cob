      *>--- conformance-case
      *> id: g2inv01-cd-identifier-2-and-single-identifier-args
      *> rules: [2002-SR-14.8.22.2-7, 2002-SR-14.8.22.2-8, 2002-GR-14.8.22.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> INVOKE operands that are record items under a 2002 CD entry.
      *> No 2002 rule restricts identifier-2's section; the
      *> identifier-3 arguments MNUM and MARG may be CD record items
      *> (§2002:14.8.22.2 SR9 names the communication section).
      *> §2002:14.8.22.2 SR7 - "Identifier-2 may be specified only
      *>   when identifier-1 is a universal object reference": U is
      *>   universal, so INVOKE U MNAME / INVOKE U MNATL are accepted
      *>   (lines 1=, 2=). A refusal of either fails the case.
      *> §2002:14.8.22.2 SR8 - "Identifier-2 shall reference an
      *>   alphanumeric or national data item": MNAME is PIC X(5),
      *>   MNATL is PIC N(5); §2002:14.8.22.3 GR2a makes their
      *>   content the method-name, so GREET runs and shows HI.
      *> §2002:14.8.22.3 GR4 - "An argument that consists merely of a
      *>   single identifier or literal is regarded as an identifier or
      *>   literal rather than an arithmetic or boolean expression":
      *>   MNUM and MARG (no BY phrase) are identifiers, so by
      *>   §2002:14.8.22.2 SR14 (typed O) and SR6 (universal U) they are
      *>   passed BY REFERENCE and the method's update is seen (3=0008,
      *>   4=XYZ, 5=0009). Were MNUM taken as an arithmetic expression
      *>   it would go BY CONTENT and 3= would stay 0007. The literal
      *>   "AB" is a literal: not valid BY REFERENCE, so BY CONTENT
      *>   (SR14a), and SHOW receives AB (6=AB).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV09.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV01C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE G2INV01C.
       01 U  USAGE OBJECT REFERENCE.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 MNAME PIC X(5).
          05 MNATL PIC N(5).
          05 MARG  PIC X(3).
          05 MNUM  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV01C "NEW" RETURNING O.
           SET U TO O.
           MOVE "GREET" TO MNAME.
           DISPLAY "1=" WITH NO ADVANCING.
           INVOKE U MNAME.
           MOVE N"GREET" TO MNATL.
           DISPLAY "2=" WITH NO ADVANCING.
           INVOKE U MNATL.
           MOVE 7 TO MNUM.
           INVOKE O "BUMP" USING MNUM.
           DISPLAY "3=" MNUM.
           MOVE "ABC" TO MARG.
           INVOKE O "SETX" USING MARG.
           DISPLAY "4=" MARG.
           INVOKE U "BUMP" USING MNUM.
           DISPLAY "5=" MNUM.
           DISPLAY "6=" WITH NO ADVANCING.
           INVOKE O "SHOW" USING "AB".
           STOP RUN.
       END PROGRAM G2INV09.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI".
       END METHOD GREET.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(4).
       PROCEDURE DIVISION USING LN.
       MAIN.
           ADD 1 TO LN.
       END METHOD BUMP.
       METHOD-ID. SETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(3).
       PROCEDURE DIVISION USING LX.
       MAIN.
           MOVE "XYZ" TO LX.
       END METHOD SETX.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(2).
       PROCEDURE DIVISION USING LA.
       MAIN.
           DISPLAY LA.
       END METHOD SHOW.
       END OBJECT.
       END CLASS G2INV01C.
