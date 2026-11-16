      *>--- conformance-case
      *> id: pb5732-copy-replacing-produces-comment-entry
      *> rules: [1985-GR-XII.2.4-12]
      *> files: [PB5732A.cpy, PB5732Z.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR12: "The text produced as a result of the
      *> complete processing of a COPY statement must not contain a COPY
      *> statement." §1985:XII.2.3 SR10: "If the word COPY appears in a
      *> comment-entry or in the place where a comment-entry may appear,
      *> it is considered part of the comment-entry."
      *> PB5732A holds the one line PLACEHOLDER. COPY PB5732Z. The
      *> REPLACING phrase turns PLACEHOLDER into AUTHOR, so the text the
      *> COPY statement produces is the AUTHOR paragraph AUTHOR. COPY
      *> PB5732Z. whose comment-entry holds the word COPY: the produced
      *> text holds no COPY statement, PB5732Z (a second DATA DIVISION
      *> header, which would break the program) is not copied, and the
      *> program runs and displays X=3. (pseudo-text-2 begins on a line
      *> of its own so that AUTHOR stands in area A, where a paragraph
      *> name begins.) Judged on the library text as written the COPY
      *> would be a nested COPY statement, refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5732R.
       COPY PB5732A REPLACING ==PLACEHOLDER== BY
       ==AUTHOR==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "X=" X.
           STOP RUN.
