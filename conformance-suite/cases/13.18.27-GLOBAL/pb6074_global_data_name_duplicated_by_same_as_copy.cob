      *>--- conformance-case
      *> id: pb6074_global_data_name_duplicated_by_same_as_copy
      *> rules: [SR-13.18.27.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6074 item 4 - the SAME AS arm of the copy clash that pb5073-global-data-name-duplicated-by-type-copy pins for TYPE.
      *>   §2023:13.18.49.4 GR1 (cite.py: OK): the effect of SAME AS is as though the data description of data-name-1 "had been coded in
      *>   place of the SAME AS clause", so V holds a data item named G, and §2023:8.4.2.1 (cite.py: OK) forbids G beside the GLOBAL
      *>   item G. Two refusals follow: SRC's own G (a data item named G beside the GLOBAL G) and
      *>   the copy under V; the second is reported at V, the entry whose SAME AS clause brought it in (kb/Work PB5629), and its message
      *>   says so, which is the text this case's diagnostics file expects. Nothing references G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6074S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC.
           05  G PIC X.
       01  G PIC X GLOBAL.
       01  V SAME AS SRC.
       PROCEDURE DIVISION.
       P-MAIN.
           STOP RUN.
