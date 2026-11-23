## EXIT PROGRAM in a main program raises no EC-FLOW-GLOBAL-EXIT

Review finding R1-1 of wave 20262150 group E: `CallEmitter.EmitExitProgram` raised EC-FLOW-GLOBAL-EXIT even in a program not under a calling runtime element, ending the run unit. §2002:14.8.13.3 GR5 (§2014:14.9.13.3 GR5) treats that EXIT PROGRAM as CONTINUE and raises no exception condition, so the raise is now `__asCalled`-gated like GOBACK's. Golden `pb5715_exit_program_main_global_declarative_is_continue` (2002) runs on with `AFTER-EXIT`, `BACK-IN-DECL`, `AFTER-READ 10`; D-GLOBAL-EXIT records the gate.
