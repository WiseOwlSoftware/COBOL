      *>--- conformance-case
      *> id: g85_f08_08
      *> rules: [1985-GR-VI.6.27.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.27.4 GR10 - "Literal-1 or the content of the data
      *> item referenced by identifier-2 can contain any character in
      *> the computer's character set."  The delimiters here are an item
      *> holding LOW-VALUE (a character of the set, not printable), the
      *> literal x"y (lower case letters and an embedded quotation
      *> mark), and the literal "; " (semicolon and space).
      *> Expected (derived): SRC = "AB" LOW-VALUE "CD" 'x"y' "EF" "; "
      *> "GH" padded with spaces to 20.  §1985:VI.6.27.4 GR12 and GR13
      *> split it at each delimiter: D1 "AB" (delimiter LOW-VALUE), D2
      *> "CD" (delimiter 'x"y' into DL2), D3 "EF" (COUNT 2), D4 = the
      *> rest "GH" + spaces, truncated to 4 = "GH  ".  §1985:VI.6.27.4
      *> GR16: TALLYING counts 4 fields from 0.
      *> A leg fails if a delimiter holding LOW-VALUE, a quotation mark,
      *> a lower case letter, ';' or a space is not matched as written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0808.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC                PIC X(20).
       01  DLM                PIC X VALUE LOW-VALUE.
       01  D1                 PIC X(4).
       01  D2                 PIC X(4).
       01  D3                 PIC X(4).
       01  D4                 PIC X(4).
       01  DL1                PIC X.
       01  DL2                PIC X(3).
       01  C3                 PIC 99 VALUE 0.
       01  TL                 PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE "AB?CDx""yEF; GH" TO SRC.
           MOVE LOW-VALUE TO SRC (3:1).
           UNSTRING SRC DELIMITED BY DLM OR "x""y" OR "; "
               INTO D1 DELIMITER IN DL1
                    D2 DELIMITER IN DL2
                    D3 COUNT IN C3
                    D4
               TALLYING IN TL.
           DISPLAY "D1=[" D1 "] D2=[" D2 "] D3=[" D3 "] D4=[" D4 "]".
           IF DL1 = LOW-VALUE
               DISPLAY "DL1=LOW-VALUE"
           ELSE
               DISPLAY "DL1 WRONG".
           DISPLAY "DL2=[" DL2 "] C3=" C3 " TL=" TL.
           STOP RUN.
