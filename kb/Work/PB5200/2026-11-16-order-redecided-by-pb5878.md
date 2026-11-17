**2026-11-16, the both-modes order re-decided by kb/Work PB5878.** This note's golden
`conformance-suite/cases/14.9.49-USE/pb5200_use_mode_of_refused_open_41` derived that an OPEN refused '41' runs the procedure
of the mode being opened and "never the one of the mode the file is open in". `§2023:14.9.49.4 GR6` b)-e) (and
`§1985:VII.4.6.4 GR5`) apply a mode's procedure to a file "open in the <mode> mode or in the process of being opened in" it,
so with both procedures declared both qualify and the text orders neither. PB5878's ruling settles the order by CLAUDE.md
rule 1 (GnuCOBOL's): the procedure of the mode the file IS open in (`FileConnector.UseModeView`; docs/conformance
determination `D-USE-OPEN-MODE-BOTH`). The golden's three refused OPENs of the file open INPUT now each run `USE INPUT`; its
derivation comment says why. The rest of this note's mechanism (the being-opened mode is the OPEN statement's own state, so
no later statement finds it) is unchanged and still holds.
