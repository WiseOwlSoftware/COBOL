      *>--- conformance-case
      *> id: g2pad01_padding_operands_and_null_facility
      *> rules: [2002-SR-12.3.4.10.2-1, 2002-SR-12.3.4.10.2-2, 2002-SR-12.3.4.10.2-3, 2002-GR-12.3.4.10.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.10 - "The PADDING CHARACTER clause is an obsolete
      *>   element in this International Standard" (cite.py OK): the
      *>   obsolete warning COBOL0903 does not fail the case.
      *> §2002:12.3.4.10.2 SR1 - "Literal-1 shall be a one-character
      *>   alphanumeric literal" (cite.py OK). Leg F1 writes "*"; leg F2
      *>   writes SPACE, which §2002:8.3.1.2.5.2 SR1 grants: "A figurative
      *>   constant may be used whenever 'literal' appears in a format"
      *>   (cite.py OK). Fails if either legal operand is refused.
      *> §2002:12.3.4.10.2 SR2 - "Data-name-1 may be qualified" (cite.py
      *>   OK). Leg F3 names PAD-CHAR OF G1 where G2 also holds a
      *>   PAD-CHAR, so only the qualified form resolves. Fails if the
      *>   qualified form is refused.
      *> §2002:12.3.4.10.2 SR3 - data-name-1 "defined in the
      *>   working-storage, local-storage, or linkage section" (cite.py
      *>   OK). F3 names a working-storage item, F4 a local-storage item,
      *>   F5 (in the contained program G2PAD1L) a linkage item. Fails if
      *>   any of the three sections is refused.
      *> §2002:12.3.4.10.3 GR2 - "If the PADDING CHARACTER clause is not
      *>   applicable to the device type to which the file is assigned,
      *>   the creation or recognition of padding characters shall not
      *>   occur" (cite.py OK); §2002:B.3 lists the clause as
      *>   processor-dependent and §2002:3.1.5 leaves the claim to the
      *>   implementor (both cite.py OK). WiseOwl COBOL's determination
      *>   (docs/conformance/3.6, the PADDING CHARACTER row, 85 and 2002):
      *>   no blocking model, padding not applicable to any file.
      *> Context for the GR2 leg (not a witness of GR1): §2002:12.3.4.10.3
      *>   GR1 - "a logical record that consists solely of padding
      *>   characters shall be ignored" (cite.py OK) - is the arm where
      *>   padding applies, unreachable on this processor. On the GR2 arm
      *>   no record is recognized as padding: each leg writes a record
      *>   made solely of its padding character, which must be read back,
      *>   not ignored, and no fill record may appear before EOF.
      *> F3 moves "#" into PAD-CHAR OF G1 before OPEN OUTPUT and "*"
      *>   after it; both all-"#" and all-"*" records must be read back
      *>   (GR2: no recognition of either value). Reopening INPUT under a
      *>   different padding value is not a conflicting-attribute failure:
      *>   §2002:14.8.26.3 GR8 "The implementor defines which of the
      *>   fixed-file attributes are validated" (cite.py OK), and the
      *>   docs/conformance/3.6 determination validates no padding. The
      *>   leg does not witness GR3 (which value is selected is not
      *>   observable when padding never occurs).
      *> DERIVATION: every leg reads back exactly the records it wrote,
      *> in order, then EOF; the brackets show the record of spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PAD01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2PAD01A.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*".
           SELECT F2 ASSIGN TO "G2PAD01B.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS SPACE.
           SELECT F3 ASSIGN TO "G2PAD01C.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS PAD-CHAR OF G1.
           SELECT F4 ASSIGN TO "G2PAD01D.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS LPAD.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       FD  F4.
       01  R4 PIC X(4).
       WORKING-STORAGE SECTION.
       01  G1.
           05 PAD-CHAR PIC X VALUE "*".
       01  G2.
           05 PAD-CHAR PIC X VALUE "#".
       01  EOF-SW PIC X.
       01  LK-ARG PIC X VALUE "#".
       LOCAL-STORAGE SECTION.
       01  LPAD PIC X VALUE "*".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "AB**" TO R1 WRITE R1
           MOVE "****" TO R1 WRITE R1
           MOVE "CD  " TO R1 WRITE R1
           CLOSE F1
           OPEN INPUT F1
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
               READ F1 AT END MOVE "Y" TO EOF-SW
                       DISPLAY "F1 EOF"
                   NOT AT END DISPLAY "F1 [" R1 "]"
               END-READ
           END-PERFORM
           CLOSE F1
           OPEN OUTPUT F2
           MOVE "EF  " TO R2 WRITE R2
           MOVE SPACES TO R2 WRITE R2
           MOVE "GH  " TO R2 WRITE R2
           CLOSE F2
           OPEN INPUT F2
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
               READ F2 AT END MOVE "Y" TO EOF-SW
                       DISPLAY "F2 EOF"
                   NOT AT END DISPLAY "F2 [" R2 "]"
               END-READ
           END-PERFORM
           CLOSE F2
           MOVE "#" TO PAD-CHAR OF G1
           OPEN OUTPUT F3
           MOVE "*" TO PAD-CHAR OF G1
           MOVE "####" TO R3 WRITE R3
           MOVE "****" TO R3 WRITE R3
           MOVE "IJ##" TO R3 WRITE R3
           CLOSE F3
           OPEN INPUT F3
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
               READ F3 AT END MOVE "Y" TO EOF-SW
                       DISPLAY "F3 EOF"
                   NOT AT END DISPLAY "F3 [" R3 "]"
               END-READ
           END-PERFORM
           CLOSE F3
           OPEN OUTPUT F4
           MOVE "****" TO R4 WRITE R4
           MOVE "KL**" TO R4 WRITE R4
           CLOSE F4
           OPEN INPUT F4
           MOVE "N" TO EOF-SW
           PERFORM UNTIL EOF-SW = "Y"
               READ F4 AT END MOVE "Y" TO EOF-SW
                       DISPLAY "F4 EOF"
                   NOT AT END DISPLAY "F4 [" R4 "]"
               END-READ
           END-PERFORM
           CLOSE F4
           CALL "G2PAD1L" USING LK-ARG
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PAD1L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F5 ASSIGN TO "G2PAD01E.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS LK-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  F5.
       01  R5 PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF5 PIC X.
       LINKAGE SECTION.
       01  LK-PAD PIC X.
       PROCEDURE DIVISION USING LK-PAD.
       L-PARA.
           OPEN OUTPUT F5
           MOVE "MN##" TO R5 WRITE R5
           MOVE "####" TO R5 WRITE R5
           CLOSE F5
           OPEN INPUT F5
           MOVE "N" TO EOF5
           PERFORM UNTIL EOF5 = "Y"
               READ F5 AT END MOVE "Y" TO EOF5
                       DISPLAY "F5 EOF"
                   NOT AT END DISPLAY "F5 [" R5 "]"
               END-READ
           END-PERFORM
           CLOSE F5
           GOBACK.
       END PROGRAM G2PAD1L.
       END PROGRAM G2PAD01.
