      *>--- conformance-case
      *> id: pb5629_global_subordinate_names_told_apart_by_qualification
      *> rules: [GR-13.18.27.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5629 - the data-names subordinate to a GLOBAL record are global names (§2023:13.18.27.4 GR1, cite.py: OK), a
      *>   matter of scope; the item that possesses the global attribute is the record the clause is written on
      *>   (§2023:13.18.27.4 GR3: "it is only the subject of that REDEFINES clause that possesses the global attribute", cite.py:
      *>   OK), so the sentence of §2023:8.4.2.1 ("The same name shall not be used both as the name of an item possessing the
      *>   global attribute and as the name of any other data item", cite.py: OK) is about a GLOBAL record's own name.
      *>   Same-named subordinates are told apart by qualification (§2023:8.4.2.2.1: "Identical user-defined names may be
      *>   specified in a source unit"): X sits under A and under B of the GLOBAL record GLB, CODE-X also sits under the local
      *>   record LOC, and the contained program reads each by qualification (§1985:X.1.3.8.2 GR1, cite.py: OK, applies "the
      *>   normal rules for qualification" to a global name). conformance:negative/1985-SR-IV_4_3_8_1-4 and
      *>   pb5073-global-data-name-duplicated are the refused side (a GLOBAL record's own name).
      *> DERIVATION. The outer program displays X OF A OF GLB and X OF B OF GLB ("12"); the contained program displays the
      *>   same two and CODE-X OF GLB ("123"); CODE-X OF LOC ("L") is never displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5629P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GLB GLOBAL.
           02  A.
               03  X PIC X VALUE "1".
           02  B.
               03  X PIC X VALUE "2".
           02  CODE-X PIC X VALUE "3".
       01  LOC.
           02  CODE-X PIC X VALUE "L".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY X OF A OF GLB X OF B OF GLB
           CALL "PB5629Q"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5629Q.
       PROCEDURE DIVISION.
       Q-MAIN.
           DISPLAY X OF A OF GLB X OF B OF GLB CODE-X OF GLB
           EXIT PROGRAM.
       END PROGRAM PB5629Q.
       END PROGRAM PB5629P.
