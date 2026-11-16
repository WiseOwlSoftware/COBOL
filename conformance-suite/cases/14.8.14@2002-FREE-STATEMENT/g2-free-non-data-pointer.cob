      *>--- conformance-case
      *> id: g2-free-non-data-pointer
      *> rules: [2002-SR-14.8.14.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.14.2 SR1 - "The data item referenced by data-name-1
      *>   shall be of category data-pointer." A prohibiting syntax rule
      *>   that grants no form. PP is USAGE PROGRAM-POINTER (category
      *>   program-pointer) and TL is the TEXT LENGTH data-name of an
      *>   output CD (a numeric item), so each FREE below violates SR1
      *>   and the program is refused. The communication facility is
      *>   obsolete at 2002 (COBOL0903 warning), which does not affect
      *>   the refusal. The accepted form (FREE of data-pointers) is
      *>   g2-free-resume-next-implicit in this folder.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FRE02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       PROCEDURE DIVISION.
       MAIN-P.
           FREE PP.
           FREE TL.
           STOP RUN.
