      *>--- conformance-case
      *> id: pb5924-fifty-qualifiers-85
      *> rules: [1985-SR-VI.1.3.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.1.3.2 (cite.py: OK):
      *> "50 qualifiers are permitted in level 2"
      *> C is unique only through 49 data-name qualifiers and a file-name:
      *> 50 qualifiers, at level 2 (docs/conformance 3.9: --std 85 is level 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QUAL50.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5924F".
           SELECT G ASSIGN TO "PB5924G".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 L01.
             02 L02.
              03 L03.
               04 L04.
                05 L05.
                 06 L06.
                  07 L07.
                   08 L08.
                    09 L09.
                     10 L10.
                      11 L11.
                       12 L12.
                        13 L13.
                         14 L14.
                          15 L15.
                           16 L16.
                            17 L17.
                             18 L18.
                              19 L19.
                               20 L20.
                                21 L21.
                                 22 L22.
                                  23 L23.
                                   24 L24.
                                    25 L25.
                                     26 L26.
                                      27 L27.
                                       28 L28.
                                        29 L29.
                                         30 L30.
                                          31 L31.
                                           32 L32.
                                            33 L33.
                                             34 L34.
                                              35 L35.
                                               36 L36.
                                                37 L37.
                                                 38 L38.
                                                  39 L39.
           40 L40.
            41 L41.
             42 L42.
              43 L43.
               44 L44.
                45 L45.
                 46 L46.
                  47 L47.
                   48 L48.
           49 L49 PIC X.
           88 C VALUE "Y".
       FD G.
       01 L01.
             02 L02.
              03 L03.
               04 L04.
                05 L05.
                 06 L06.
                  07 L07.
                   08 L08.
                    09 L09.
                     10 L10.
                      11 L11.
                       12 L12.
                        13 L13.
                         14 L14.
                          15 L15.
                           16 L16.
                            17 L17.
                             18 L18.
                              19 L19.
                               20 L20.
                                21 L21.
                                 22 L22.
                                  23 L23.
                                   24 L24.
                                    25 L25.
                                     26 L26.
                                      27 L27.
                                       28 L28.
                                        29 L29.
                                         30 L30.
                                          31 L31.
                                           32 L32.
                                            33 L33.
                                             34 L34.
                                              35 L35.
                                               36 L36.
                                                37 L37.
                                                 38 L38.
                                                  39 L39.
           40 L40.
            41 L41.
             42 L42.
              43 L43.
               44 L44.
                45 L45.
                 46 L46.
                  47 L47.
                   48 L48.
           49 L49 PIC X.
           88 C VALUE "Y".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F G.
           MOVE "Y" TO L49 OF L48 OF L47 OF L46 OF L45 OF L44 OF L43
           OF L42 OF L41 OF L40 OF L39 OF L38 OF L37 OF L36 OF L35
               OF L34 OF L33 OF L32 OF L31 OF L30 OF L29 OF L28 OF L27
               OF L26 OF L25 OF L24 OF L23 OF L22 OF L21 OF L20 OF L19
               OF L18 OF L17 OF L16 OF L15 OF L14 OF L13 OF L12 OF L11
               OF L10 OF L09 OF L08 OF L07 OF L06 OF L05 OF L04 OF L03
               OF L02 OF L01 OF F.
           MOVE "N" TO L49 OF L48 OF L47 OF L46 OF L45 OF L44 OF L43
           OF L42 OF L41 OF L40 OF L39 OF L38 OF L37 OF L36 OF L35
               OF L34 OF L33 OF L32 OF L31 OF L30 OF L29 OF L28 OF L27
               OF L26 OF L25 OF L24 OF L23 OF L22 OF L21 OF L20 OF L19
               OF L18 OF L17 OF L16 OF L15 OF L14 OF L13 OF L12 OF L11
               OF L10 OF L09 OF L08 OF L07 OF L06 OF L05 OF L04 OF L03
               OF L02 OF L01 OF G.
           IF C OF L49 OF L48 OF L47 OF L46 OF L45 OF L44 OF L43 OF L42
               OF L41 OF L40 OF L39 OF L38 OF L37 OF L36 OF L35 OF L34
               OF L33 OF L32 OF L31 OF L30 OF L29 OF L28 OF L27 OF L26
               OF L25 OF L24 OF L23 OF L22 OF L21 OF L20 OF L19 OF L18
               OF L17 OF L16 OF L15 OF L14 OF L13 OF L12 OF L11 OF L10
               OF L09 OF L08 OF L07 OF L06 OF L05 OF L04 OF L03 OF L02
               OF L01 OF F
           DISPLAY "F-C TRUE"
           ELSE
           DISPLAY "F-C FALSE".
           IF C OF L49 OF L48 OF L47 OF L46 OF L45 OF L44 OF L43 OF L42
               OF L41 OF L40 OF L39 OF L38 OF L37 OF L36 OF L35 OF L34
               OF L33 OF L32 OF L31 OF L30 OF L29 OF L28 OF L27 OF L26
               OF L25 OF L24 OF L23 OF L22 OF L21 OF L20 OF L19 OF L18
               OF L17 OF L16 OF L15 OF L14 OF L13 OF L12 OF L11 OF L10
               OF L09 OF L08 OF L07 OF L06 OF L05 OF L04 OF L03 OF L02
               OF L01 OF G
           DISPLAY "G-C TRUE"
           ELSE
           DISPLAY "G-C FALSE".
           CLOSE F G.
           STOP RUN.
