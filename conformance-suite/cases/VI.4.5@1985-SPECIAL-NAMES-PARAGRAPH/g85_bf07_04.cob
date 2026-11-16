      *>--- conformance-case
      *> id: g85_bf07_04
      *> rules: [1985-SR-VI.4.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR1 - "If implementor-name-1 references an
      *> external switch, the associated mnemonic-name may be specified
      *> only in the SET statement." The rule grants the form: the
      *> mnemonic-name of a switch IS legal as the operand of a SET
      *> statement (format 3, §1985:VI.4.5.4 GR3: "the status of that
      *> switch may be altered by execution of a format 3 SET
      *> statement"). SWITCH-3 and SWITCH-4 are switches WiseOwl's SET
      *> may reference (§1985:VI.6.23.3 SR5 leaves which to the
      *> implementor; docs/CONFORMANCE.md DOC-A.1-191: every one of
      *> SWITCH-0..36 may be SET). Their status is read through the
      *> condition-names (§1985:VI.4.5.4 GR2), never through the
      *> mnemonic-name, and only after a SET, since the initial status
      *> comes from outside the program.
      *> L1 - SET SW3 TO ON: SW3-ON true, SW3-OFF false.
      *> L2 - SET SW3 TO OFF: SW3-OFF true.
      *> L3 - one SET of two mnemonic-names, two TO phrases (the
      *>   format's repeated braces): SW3 ON and SW4 OFF.
      *> L4 - SET SW3 SW4 TO ON: both ON.
      *> A compiler that refused the mnemonic in SET rejects legal
      *> source; one that ignored the SET prints a wrong status.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0704.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-3 IS SW3 ON STATUS IS SW3-ON OFF STATUS IS SW3-OFF
           SWITCH-4 IS SW4 ON STATUS IS SW4-ON OFF STATUS IS SW4-OFF.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET SW3 TO ON.
           IF SW3-ON
               DISPLAY "L1 SW3=ON"
           ELSE
               DISPLAY "L1 SW3=NOT-ON".
           IF SW3-OFF
               DISPLAY "L1 SW3-OFF=TRUE"
           ELSE
               DISPLAY "L1 SW3-OFF=FALSE".
           SET SW3 TO OFF.
           IF SW3-OFF
               DISPLAY "L2 SW3=OFF"
           ELSE
               DISPLAY "L2 SW3=NOT-OFF".
           SET SW3 TO ON SW4 TO OFF.
           IF SW3-ON AND SW4-OFF
               DISPLAY "L3 SW3=ON SW4=OFF"
           ELSE
               DISPLAY "L3 WRONG".
           SET SW3 SW4 TO ON.
           IF SW3-ON AND SW4-ON
               DISPLAY "L4 SW3=ON SW4=ON"
           ELSE
               DISPLAY "L4 WRONG".
           STOP RUN.
