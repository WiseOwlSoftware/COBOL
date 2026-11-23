      *>--- conformance-case
      *> id: pb5975-own-index-name-hides-global-data-85
      *> rules: [1985-GR-X.1.3.8.2-3, GR-8.4.6.2.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5975. A refusal at 85. §1985:X.1.3.8 (cite.py: OK):
      *> "a program's reference to such a name, even when it is a
      *> different type of user-defined word, is to the object which
      *> that program describes rather than to the object, possessing
      *> the same name, described in another program"; §1985:X.1.3.8.2
      *> GR3a (cite.py: OK): "If the name is declared in program B,
      *> the object in program B is the referenced object." The
      *> container declares 01 XN GLOBAL; the contained program
      *> declares its own index-name XN (INDEXED BY XN). There XN
      *> names that index-name, so OCCURS ... DEPENDING ON XN names no
      *> data item and is refused (COBOL0851, "declared as an
      *> index-name"). A compiler whose data-name lookup ranks only
      *> some classes against the container's global data item takes
      *> the container's XN (value 2) as the object, compiles, and
      *> prints AB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P75XO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XN PIC 9 VALUE 2 GLOBAL.
       PROCEDURE DIVISION.
       P-1.
           CALL "P75XI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P75XI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 3 INDEXED BY XN.
       01 V.
          02 F PIC X OCCURS 1 TO 3 DEPENDING ON XN.
       PROCEDURE DIVISION.
       I-1.
           MOVE "ABC" TO V
           DISPLAY V
           EXIT PROGRAM.
       END PROGRAM P75XI.
       END PROGRAM P75XO.
