      *>--- conformance-case
      *> id: g2dsp01_upon_device_mnemonic
      *> rules: [2002-SR-14.8.10.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.10.2 SR2 -- "Mnemonic-name-1 shall be associated
      *> with a hardware device in the SPECIAL-NAMES paragraph in the
      *> environment division." The accepted half: mnemonic-names
      *> associated by "device-name-1 IS mnemonic-name-3"
      *> (§2002:12.2.6.1) with each output device-name WiseOwl
      *> provides. The names are the implementor's (§2002:12.2.6.2
      *> SR8: "The implementor shall specify the names that are
      *> available for switch-name-1, feature-name-1, and
      *> device-name-1"); WiseOwl's are documented in
      *> docs/conformance DOC-A.1-189 and DOC-A.1-59: CONSOLE and
      *> SYSOUT are the standard output, SYSERR the standard error.
      *> §2002:14.8.10.3 GR1 -- "The DISPLAY statement causes the
      *> content of each operand to be transferred to the hardware
      *> device in the order listed":
      *>  U1 UPON OUT-DEV (SYSOUT)                  -> stdout U1 SYSOUT
      *>  U2 UPON CON-DEV (CONSOLE), two operands   -> stdout U2 CONSOLE
      *>  U3 UPON ERR-DEV (SYSERR), END-DISPLAY     -> stderr, not here
      *>  U4 without UPON, the standard device      -> stdout U4 END
      *> The refused half (a mnemonic associated with no device, or
      *> with a switch-name or feature-name) is g2dsp02..g2dsp04.
      *> Fails if a device mnemonic is refused, or the SYSERR line
      *> reaches the standard output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSP1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS CON-DEV
           SYSOUT IS OUT-DEV
           SYSERR IS ERR-DEV.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "U1 SYSOUT" UPON OUT-DEV
           DISPLAY "U2 " "CONSOLE" UPON CON-DEV
           DISPLAY "U3 SYSERR" UPON ERR-DEV END-DISPLAY
           DISPLAY "U4 END"
           STOP RUN.
