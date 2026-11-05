---
title: The communication facility
description: The COMMUNICATION SECTION of COBOL 1985 and 2002, the message control system WiseOwl COBOL provides, and the mcs.json file that defines its queues and destinations.
sidebar:
  order: 8
---

COBOL 1985 and COBOL 2002 define a communication facility: a `COMMUNICATION SECTION` with CD entries, and the statements
`ACCEPT cd-name MESSAGE COUNT`, `ENABLE`, `DISABLE`, `PURGE`, `RECEIVE` and `SEND`. WiseOwl COBOL compiles them at `--std 85` and
`--std 2002`, where each draws the obsolete-element warning COBOL0903. COBOL 2014 removed the facility, so from `--std 2014` each is
COBOL0902 (a warning under `--permissive`, and the program still runs). The `RECEIVE` and `SEND` of COBOL 2023 are a different facility,
which this compiler declines (`docs/CONFORMANCE.md`, section 4).

## The message control system

The standard's statements talk to a message control system (MCS). WiseOwl COBOL's runtime is one. It keeps its files in one directory,
the environment variable `WISEOWL_COBOL_MCS`, or the current directory when that is unset:

| File | What it is |
|---|---|
| `mcs.json` | Your definitions: the symbolic names of the queues, sources, destinations and terminals |
| `mcs-state.json` | The messages the MCS holds and which paths are enabled. Delete it to start again from your definitions |
| `mcs.lock` | Held for the length of one statement, so run units on one machine can share the directory |
| `<name>.out` | A destination's or terminal's device: each complete message, one per line |

With no `mcs.json`, every symbolic name is unknown: the statements run and their status keys say so (20 or 21).

```json
{
  "queues": [
    { "name": "ORDERS", "messages": [ "A SEEDED MESSAGE" ],
      "subQueues": [ { "name": "URGENT" } ] }
  ],
  "sources": [ { "name": "TERMINAL1" } ],
  "destinations": [
    { "name": "ECHO", "deliverTo": "ORDERS/URGENT", "source": "TERMINAL1" },
    { "name": "PRINTER" }
  ],
  "terminals": [ { "name": "DESK1" } ]
}
```

Every member but `name` is optional. A name is 1 to 12 characters. `enabled` (default `true`) is a path's starting state. `key` is the
password the COBOL 1985 `WITH KEY` phrase is checked against. `messages` enter once, when the MCS first answers, as a string or as
`{ "segments": [...], "group": true, "source": "...", "date": "YYMMDD", "time": "HHMMSSTT" }`. `capacity` bounds a destination's waiting
messages. `deliverTo` makes a destination feed a queue path or `terminal:<name>`, which is how one run unit sends to another; any other
destination writes to its `.out` file.

A process that starts a program on the MCS's behalf sets `WISEOWL_COBOL_MCS_SCHEDULED` to the queue's levels, separated by `/`, or the
terminal's name; a CD with the `INITIAL` clause then holds those names when the program starts.

## Status keys

Table A.1 of the COBOL 2002 standard gives the status key values. WiseOwl COBOL adds two of the `9x` values it leaves to the implementor: `91`, the
`mcs.json` file cannot be used, and `92`, the host refuses the MCS's files. Each takes no action, so a program can repeat the statement. At COBOL 1985, status `40` reports a wrong `KEY`.

`docs/CONFORMANCE.md`, section 3.3, records every determination, and `docs/rearchitecture/DESIGN-communication-facility.md` the design.
