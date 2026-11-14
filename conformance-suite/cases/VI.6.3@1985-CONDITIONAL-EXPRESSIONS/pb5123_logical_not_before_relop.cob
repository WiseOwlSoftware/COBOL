      *>--- conformance-case
      *> id: pb5123_logical_not_before_relop
      *> rules: [1985-GR-VI.6.3.3-L2.1, 1985-GR-VI.6.3.3-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5123. §1985:VI.6.3.3 reads the NOT of an
      *> abbreviated combined relation condition by the word after it.
      *> GRL2.1: "If the word immediately following NOT is GREATER, >,
      *> LESS, <, EQUAL, =, then the NOT participates as part of the
      *> relational operator; otherwise," GRL2.2: "The NOT is
      *> interpreted as a logical operator and, therefore, the implied
      *> insertion of subject or relational operator results in a
      *> negated relation condition." So before >=, <= or IS the NOT
      *> negates the abbreviated relation, and the operator it states
      *> is the last stated relational operator, the one inserted into
      *> a later bare object (the insertion rule of the same clause):
      *>   A > B AND NOT >= C OR D  is
      *>   ((A > B) AND (NOT (A >= C))) OR (A >= D).
      *> T1-T7 are the ordinary readings (NOT part of the operator,
      *> or NOT before a bare object). T8-T10 fail if NOT >= is
      *> refused or if the carried operator is the negated one (T8:
      *> A >= D is true, A < D false). T11-T12 are the same for <=.
      *> T13 fails if NOT IS > is refused or carries NOT > (A > D is
      *> true, A NOT > D false). ISO 2002 prohibited NOT before >=
      *> and <= (§2002:F.1 item 22) and read NOT IS > with the NOT
      *> part of the operator (§2002:8.8.4.2.4.3 GR2b, T13 FALSE
      *> there), so the program is 85's alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5123A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       01  B PIC 9.
       01  C PIC 9.
       01  D PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO B. MOVE 5 TO C.
           IF NOT A = B OR C DISPLAY "T1 TRUE"
           ELSE DISPLAY "T1 FALSE".
           MOVE 3 TO B. MOVE 5 TO C.
           IF NOT A = B OR C DISPLAY "T2 TRUE"
           ELSE DISPLAY "T2 FALSE".
           MOVE 5 TO B. MOVE 3 TO C.
           IF NOT A = B OR C DISPLAY "T3 TRUE"
           ELSE DISPLAY "T3 FALSE".
           MOVE 7 TO B. MOVE 8 TO C. MOVE 3 TO D.
           IF NOT (A NOT > B AND C AND NOT D) DISPLAY "T4 TRUE"
           ELSE DISPLAY "T4 FALSE".
           MOVE 9 TO D.
           IF NOT (A NOT > B AND C AND NOT D) DISPLAY "T5 TRUE"
           ELSE DISPLAY "T5 FALSE".
           MOVE 3 TO B. MOVE 5 TO C.
           IF A = B OR NOT C DISPLAY "T6 TRUE"
           ELSE DISPLAY "T6 FALSE".
           MOVE 4 TO C.
           IF A = B OR NOT C DISPLAY "T7 TRUE"
           ELSE DISPLAY "T7 FALSE".
           MOVE 9 TO B. MOVE 1 TO C. MOVE 3 TO D.
           IF A > B AND NOT >= C OR D DISPLAY "T8 TRUE"
           ELSE DISPLAY "T8 FALSE".
           MOVE 1 TO B. MOVE 6 TO C. MOVE 7 TO D.
           IF A > B AND NOT >= C OR D DISPLAY "T9 TRUE"
           ELSE DISPLAY "T9 FALSE".
           MOVE 1 TO B. MOVE 5 TO C. MOVE 7 TO D.
           IF A > B AND NOT >= C OR D DISPLAY "T10 TRUE"
           ELSE DISPLAY "T10 FALSE".
           MOVE 1 TO B. MOVE 4 TO C.
           IF A > B AND NOT <= C DISPLAY "T11 TRUE"
           ELSE DISPLAY "T11 FALSE".
           MOVE 5 TO C.
           IF A > B AND NOT <= C DISPLAY "T12 TRUE"
           ELSE DISPLAY "T12 FALSE".
           MOVE 9 TO B. MOVE 6 TO C. MOVE 3 TO D.
           IF A > B AND NOT IS > C OR D DISPLAY "T13 TRUE"
           ELSE DISPLAY "T13 FALSE".
           STOP RUN.
