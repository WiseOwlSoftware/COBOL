      *>--- conformance-case
      *> id: pb1047-two-globals-one-element-ambiguous
      *> rules: [GR-8.4.6.2.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1047 / PB5629: two global X items in ONE source element, each the subordinate of its own GLOBAL record.
      *> §2023:8.4.6.2.1 3) b) 1. narrows a name duplicated ACROSS nested source elements to the nearest element that declares
      *> it (positive twin: 85/pb1047_nearest_declaring_element); inside ONE element, qualification alone tells the two apart
      *> (§2023:8.4.2.2.1), and the unqualified `X` of PB1047B names neither uniquely (§2023:8.4.2.2.3 SR1: "uniqueness shall be
      *> established through a sequence of qualifiers that precludes any ambiguity of reference", cite.py: OK): the
      *> reference-time COBOL1639. The declarations themselves are legal (kb/Work PB5629: the subordinates are global names, and only a GLOBAL
      *> record possesses the global attribute, §2023:13.18.27.4 GR1 and GR3; positive twin:
      *> pb5629_global_subordinate_names_told_apart_by_qualification).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 GLOBAL.
          05 X PIC X(3) VALUE "G1X".
       01 G2 GLOBAL.
          05 X PIC X(3) VALUE "G2X".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047B"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047B.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "X=" X
           EXIT PROGRAM.
       END PROGRAM PB1047B.
       END PROGRAM PB1047A.
