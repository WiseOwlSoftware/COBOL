      *>--- conformance-case
      *> id: pb5924-end-program-column-9-85
      *> rules: [1985-SR-IV.7.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.6 (cite.py: OK):
      *> "The end program header must start in area A"
      *> END PROGRAM beginning in column 9 (area A) compiles and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDP.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ENDP".
           STOP RUN.
        END PROGRAM ENDP.
