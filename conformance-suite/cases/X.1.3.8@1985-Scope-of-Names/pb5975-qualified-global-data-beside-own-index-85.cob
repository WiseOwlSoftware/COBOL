      *>--- conformance-case
      *> id: pb5975-qualified-global-data-beside-own-index-85
      *> rules: [1985-GR-X.1.3.8.2-1, 1985-GR-X.1.3.8.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5975 review R1-1. §1985:X.1.3.8.2 GR1 (cite.py: OK):
      *> over the set of program B's names and its containers' global
      *> names "the normal rules for qualification and any other rules
      *> for uniqueness of reference are applied until one or more
      *> objects is identified"; GR2 (cite.py: OK): "If only one object
      *> is identified, it is the referenced object." The container
      *> declares 01 G GLOBAL with 02 X; the contained program P75QI
      *> declares its own index-name X (INDEXED BY X). X OF G there
      *> identifies only the container's X: the qualifier G reaches no
      *> index-name, and an index-name takes no qualifier at 85. So
      *> MOVE X OF G moves 7, while the unqualified X in SET and the
      *> subscript stays P75QI's own index (GR3a), selecting E (2) = B.
      *> A compiler that lets the own index-name claim the qualified
      *> reference refuses X OF G at translation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P75QO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          02 X PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P-1.
           CALL "P75QI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P75QI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABC".
          02 E PIC X OCCURS 3 INDEXED BY X.
       01 Y PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       I-1.
           MOVE X OF G TO Y
           DISPLAY "Y=" Y
           SET X TO 2
           DISPLAY "E=" E (X)
           EXIT PROGRAM.
       END PROGRAM P75QI.
       END PROGRAM P75QO.
