      *>--- conformance-case
      *> id: pb5795_computer_name_spelled_as_device_name
      *> rules: [FMT-12.3.5.2, FMT-12.3.6.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5795, the later editions. §2023:8.3.2.3.1: "Within
      *> an implementation, a given system-name shall not belong to more
      *> than one of the following types of system-names: device-name,
      *> feature-name, and switch-name." (§2002:8.3.1.1.2 alike).
      *> Computer-name is not among them, so a computer-name spelled as
      *> this implementation's device-name CONSOLE (DOC-A.1-198) is
      *> legal, beside SPECIAL-NAMES using CONSOLE as the device-name it
      *> is. At COBOL-85 the three types of system-name are disjoint and
      *> the same program is refused
      *> (pb5795-computer-name-is-implementor-name-85).
      *> A leg fails if either paragraph is refused, or if the DISPLAY
      *> UPON the mnemonic-name does not print RAN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5795A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. CONSOLE.
       OBJECT-COMPUTER. CONSOLE.
       SPECIAL-NAMES. CONSOLE IS MYCON.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN" UPON MYCON.
           STOP RUN.
