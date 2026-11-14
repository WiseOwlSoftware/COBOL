---
cells: ["`set-program-pointer-to-entry`", "`SET program-pointer TO ENTRY literal-or-identifier` (COBOL-2002 and later)", "IBM Enterprise COBOL, Micro Focus", "flagged", "`SET program-pointer TO ADDRESS OF PROGRAM program-name` (§2023:14.9.39.2 Format 9, §2023:8.4.3.13)"]
---
**An accepted extension stays inside the content rules of the items it loads** (kb/Work PB935). `SET program-pointer TO
ENTRY` names a program by value, so its sender is an unrestricted program-address; §2023:13.18.60.4 GR25 ("A restricted
program-pointer shall contain only the predefined address NULL or the address of a program with the same signature as
that identified by the specified program-prototype-name") binds the receiving item whatever statement loads it, so the
vendor spelling is refused (COBOL1959) for a restricted program-pointer exactly as the ISO `ADDRESS OF PROGRAM`
literal-1 and identifier-1 arms are (§2023:14.9.39.3 SR22); the conforming load is `ADDRESS OF PROGRAM program-prototype-name-1`
(§2023:8.4.3.13.4 GR3). An unrestricted program-pointer takes the ENTRY form as before.
