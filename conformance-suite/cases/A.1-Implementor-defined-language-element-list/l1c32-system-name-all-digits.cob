*>--- conformance-case
*> id: l1c32-system-name-all-digits
*> rules: [DOC-A.1-198]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:A.1 198) / §2023:8.3.2.3.1 — documented restriction: an all-digit system-name
      *>
      *> §2023:8.3.2.3.1: "The implementor may define rules for the formation of a system-name
      *> that add restrictions to the rules for formation of a COBOL word."
      *>   cite.py: OK  §2023:8.3.2.3.1   (General)
      *>   cite.py: OK  §2023:A.1 198)  (Implementor-defined language element list)
      *> docs/CONFORMANCE.md#DOC-A.1-198 documents ONE added restriction: "a system-name
      *> shall not consist of digits only, because such a character-string is always read
      *> as an integer literal (`OBJECT-COMPUTER. 12345.` is refused at the literal as the
      *> computer-name it cannot be, `COBOL7654`)".
      *> `12345` is otherwise a legal COBOL word (§2023:8.3.2.1: digits allowed, no hyphen at
      *> either end; the letter requirement of §2023:8.3.2.2 binds user-defined words only), so
      *> this documented restriction is the only reason to reject. The companion positive
      *> l1c32_system_name_formation shows `1-2` and `123ABC` accepted. The diagnostic is
      *> the refusal of a run written where the computer-name stands, which is exactly what the
      *> documentation names: since kb/Work PB3390 the error production of a list that no
      *> level-number follows opens at an unsigned integer, so the literal is the refused run,
      *> and since kb/Work PB5920 it is refused as the computer-name it is (COBOL7654), not as a
      *> clause of the paragraph (COBOL1970, which kb/Work PB3373 and PB3390 had made of it;
      *> before that it was COBOL0308, "a data-name is expected").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X-1.
       OBJECT-COMPUTER. 12345.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
