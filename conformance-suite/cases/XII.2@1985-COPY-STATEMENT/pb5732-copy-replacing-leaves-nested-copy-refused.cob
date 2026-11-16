      *>--- conformance-case
      *> id: pb5732-copy-replacing-leaves-nested-copy-refused
      *> rules: [1985-GR-XII.2.4-12]
      *> files: [PB5732A.cpy, PB5732Z.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR12: "The text produced as a result of the
      *> complete processing of a COPY statement must not contain a COPY
      *> statement." The twin of pb5732-copy-replacing-produces-comment-
      *> entry: the REPLACING phrase here matches nothing in PB5732A, so
      *> the produced text is PLACEHOLDER. COPY PB5732Z., where PLACEHOLDER
      *> is no comment-entry paragraph and the COPY is a COPY statement:
      *> the produced text holds one and the program is refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5732S.
       COPY PB5732A REPLACING ==OTHER== BY
       ==AUTHOR==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "X=" X.
           STOP RUN.
