      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-3-redefines-unqualified
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SR3: "uniqueness must then be established
      *> through qualification for each user-defined name explicitly
      *> referenced, except in the case of redefinition"; and
      *> §1985:VI.5.10.3 SR7: "Data-name-2 must not be qualified even
      *> if it is not unique" (both cite.py --check OK). X is
      *> declared in REC-A and in REC-B, and the REDEFINES clause of
      *> Y names X unqualified, which the exception admits: the
      *> redefined item is the entry the clause's placement names.
      *> The program must translate and run at 85; Y OF REC-A shows
      *> the two characters of REC-A's X, AB, and X OF REC-B shows
      *> CD. A compiler that refuses the unqualified REDEFINES
      *> operand fails at the translation; one that takes REC-B's X
      *> shows CD twice. The refusal half of SR3 is
      *> 1985-SR-IV_4_3_8_1-3 (registrar re-read, 2026-11-15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85RDQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-A.
           05  X  PIC X(2) VALUE "AB".
           05  Y  REDEFINES X PIC X(2).
       01  REC-B.
           05  X  PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
       P1.
           DISPLAY Y OF REC-A.
           DISPLAY X OF REC-B.
           STOP RUN.
