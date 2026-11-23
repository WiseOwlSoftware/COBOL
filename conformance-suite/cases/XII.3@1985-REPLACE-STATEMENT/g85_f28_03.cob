      *>--- conformance-case
      *> id: g85_f28_03
      *> rules: [1985-GR-XII.3.4-4]
      *> accept: [85]
      *> files: [G85F28CA.cpy]
      *> source-format: fixed
      *>---
      *> §1985:XII.3.4 GR4 - REPLACE statements are processed after the
      *> COPY statements. cite.py --check "§1985:XII.3.4 GR4" "Any
      *> REPLACE statements contained in a source program are processed
      *> after the processing of any COPY statements" -> OK.
      *> Library text G85F28CA is one line: DISPLAY CPWORD.
      *> DERIVED: COPY first (§1985:XII.2.4 GR3 -> OK) gives
      *>   COPY G85F28CA.            -> DISPLAY CPWORD.
      *>   COPY G85F28CA REPLACING CPWORD BY MIDWORD -> DISPLAY MIDWORD.
      *> and the REPLACE then acts on that text (§1985:XII.3.4 GR1):
      *>   CPWORD  -> "COPY-TEXT-REPLACED"
      *>   MIDWORD -> "AFTER-COPY-REPLACING"
      *> Lines: COPY-TEXT-REPLACED then AFTER-COPY-REPLACING. REPLACE
      *> applied first would rewrite the second COPY's operand CPWORD, so
      *> its REPLACING would match nothing and line 2 would repeat line 1;
      *> REPLACE not reaching library text leaves CPWORD undefined.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2803.
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==CPWORD== BY =="COPY-TEXT-REPLACED"==
                   ==MIDWORD== BY =="AFTER-COPY-REPLACING"==.
           COPY G85F28CA.
           COPY G85F28CA REPLACING ==CPWORD== BY ==MIDWORD==.
           STOP RUN.
