# PB3286 - three goldens retired by PB5150 (2026-11-14)

The goldens pb3286-contained-address-of-global-beside-local, pb3286-contained-address-of-global-first and pb3286-contained-by-reference-global-beside-local each declared a local X in R1 beside the GLOBAL R2 whose subordinate is X, in one source element. That is refused by §2023:8.4.2.1 with §2023:13.18.27.4 GR1 (kb/Work PB5150), so the three were deleted. The addressing and by-reference mechanisms they witnessed remain covered by ContainedGlobalStorageClaimTests, whose container-local record no longer shares a name with the global item.
