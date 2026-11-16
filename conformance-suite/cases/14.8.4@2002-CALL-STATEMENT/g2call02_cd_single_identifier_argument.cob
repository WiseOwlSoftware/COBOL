      *>--- conformance-case
      *> id: g2call02_cd_single_identifier_argument
      *> rules: [2002-GR-14.8.4.3-8, 2002-GR-14.8.4.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.3 GR8 -- "An argument that consists merely of a
      *> single identifier or literal is regarded as an identifier or
      *> literal rather than an arithmetic or boolean expression."
      *> §2002:14.8.4.3 GR9a -- with no BY keyword and a BY REFERENCE
      *> formal, "if the argument meets the requirements of syntax rule
      *> 3, BY REFERENCE is assumed"; otherwise "BY CONTENT is assumed".
      *> §2002:14.8.4.2 SR3 admits "a data item defined in the file,
      *> working-storage, local-storage, linkage, or communication
      *> section", so a CD record item written alone is an identifier
      *> (GR8) that meets SR3 and goes BY REFERENCE (GR9a1): the
      *> callee's store is seen. The same item in parentheses is not
      *> "merely a single identifier"; it is an arithmetic expression,
      *> which does not meet SR3, so it goes BY CONTENT (GR9a2) and the
      *> callee's store is not seen. Working-storage WA is the control.
      *> Derived: QA=XYZ MC=0042 WA=XYZ, then MC-EXPR=0005.
      *> Fails if: a CD item is passed by content (QA=ABC or MC=0005 on
      *> line 1), or a parenthesized operand is passed by reference
      *> (MC-EXPR=0042). COBOL0903 (obsolete communication facility,
      *> §2002:G.2) is a warning and does not fail the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C2M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC X(3) VALUE "AAA".
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT.
       01 CDREC.
          02 QA PIC X(3).
          02 MC PIC 9(4).
          02 FILLER PIC X(80).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO QA
           MOVE 5 TO MC
           CALL "G2C2S" AS NESTED USING QA MC WA
           DISPLAY "QA=" QA " MC=" MC " WA=" WA
           MOVE 5 TO MC
           CALL "G2C2N" AS NESTED USING (MC)
           DISPLAY "MC-EXPR=" MC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C2S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(3).
       01 LN PIC 9(4).
       01 LW PIC X(3).
       PROCEDURE DIVISION USING LA LN LW.
       S1.
           MOVE "XYZ" TO LA
           MOVE 42 TO LN
           MOVE "XYZ" TO LW
           GOBACK.
       END PROGRAM G2C2S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C2N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(4).
       PROCEDURE DIVISION USING LN.
       N1.
           MOVE 42 TO LN
           GOBACK.
       END PROGRAM G2C2N.
       END PROGRAM G2C2M.
