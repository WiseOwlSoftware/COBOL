## The LINAGE data-name of an EXTERNAL file must be external at --std 85 (PB5982)

PB5982: at `--std 85`, `FD PFILE IS EXTERNAL LINAGE IS LN LINES` with LN a plain WORKING-STORAGE item compiled, and so did two programs
naming different plain items, though §1985:VII.3.7.4 GR11c requires "The same corresponding external data items referenced by
data-name-1, data-name-2, data-name-3, and data-name-4" in every description of the connector. The PADDING CHARACTER twin was already
refused at 85 (§1985:VII.2.7.4 GR6). `ExternalFileConnectorRules.Check` gated the LINAGE screen on 2023 only, on the premise that the rule
was new there.

Re-probe: both shapes exited 0 at 85 on this branch's build before the change. Fix: the screen runs at 85 as an error citing GR11c
(the PADDING screen's severity), through the same `NotExternal` arm; at 2023 it is unchanged. After: both shapes exit 65 with COBOL1624
naming GR11c; the PB5638 control (`pb5638-external-linage-agree`, external item) still exits 0.

Goldens: `pb5982-external-linage-not-external` and `pb5982-external-linage-two-programs-not-external`, each refused COBOL1624 at 85. The
2023 witness's comment, which said the rule did not exist below 2023, now says it does.

The 2002 and 2014 texts carry the rule too (§2002:13.3.4.3 GR2c2, §2014:13.4.4.3 GR2c2); neither is screened (2002 frozen, 2014 waits),
recorded in the PB5982 addendum. Sibling sweep: the externality arms of `Check` are RELATIVE KEY (every edition), PADDING (85 and 2002)
and FILE STATUS (2023, a clause 1985 does not carry); LINAGE was the one arm missing at 85. No inventory row was claimed.
