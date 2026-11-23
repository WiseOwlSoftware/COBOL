      *>--- conformance-case
      *> id: g2002-call-restricted-pointer-as-other-prototype
      *> rules: [2002-GR-14.8.4.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5714. §2002:14.8.4.3 GR7: "If the NESTED phrase is
      *> not specified, program-prototype-name-1, or the program
      *> prototype specified in the definition of identifier-1 if it is
      *> defined as a restricted program-pointer, is used to determine
      *> the characteristics of the called program". COBOL-2002 asks no
      *> agreement of the two prototypes: the rule that their
      *> signatures be the same is §2014:14.9.4.2 SR14, refused from
      *> 2014 (14.9.4-CALL/pb609-call-restricted-program-pointer-
      *> signature). PA is restricted to G2RPAA, whose formal L-N is
      *> required; the AS phrase names G2RPAB, whose formal is
      *> OPTIONAL, so the two signatures differ and the CALL compiles.
      *> WS-N PIC 9(4) is passed BY REFERENCE (assumed, §2002:14.8.4.3
      *> GR9a.1) to the program PA holds, G2RPAA, which adds 1: N=0008.
      *> The leg fails if the CALL is refused or WS-N is not shared.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPAA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-N.
       END PROGRAM G2RPAA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPAB IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING OPTIONAL L-N.
       END PROGRAM G2RPAB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPAM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2RPAA
           PROGRAM G2RPAB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA IS TYPEDEF USAGE PROGRAM-POINTER TO G2RPAA.
       01 PA TYPE TA.
       01 WS-N PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
           SET PA TO ADDRESS OF PROGRAM G2RPAA
           CALL PA AS G2RPAB USING WS-N
           DISPLAY "N=" WS-N
           STOP RUN.
       END PROGRAM G2RPAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPAA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-N.
           ADD 1 TO L-N
           GOBACK.
       END PROGRAM G2RPAA.
