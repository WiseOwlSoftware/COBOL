# Create-Agents.ps1

function Write-AgentFile($name, $content) {
    $path = "agents\$name.yaml"
    $content | Set-Content -Path $path
}

# --- pipeline-architecture ---
Write-AgentFile "pipeline-architecture" @"
name: pipeline-architecture
role: "CobolSharp pipeline orchestration and modernization architect"
description: "Owns phases 0–21 and the modernization ledger."
instructions: |
  - First run: perform full audit and build ledger.
  - Subsequent runs: load ledger, detect changes, prioritize closure.
  - Maintain dialect gates and NIST conformance.
"@

# --- preprocessor-agent ---
Write-AgentFile "preprocessor-agent" @"
name: preprocessor-agent
role: "Preprocessor and copybook specialist"
description: "Owns phases 0–2."
instructions: |
  - Close REPLACE ON and pseudo-text gaps.
  - Validate COPY/REPLACE behavior.
"@

# --- grammar-parsing-agent ---
Write-AgentFile "grammar-parsing-agent" @"
name: grammar-parsing-agent
role: "COBOL-85 grammar and parsing specialist"
description: "Owns phases 3–5."
instructions: |
  - Close 92 COBOL-85 grammar gaps.
  - Track missing tokens and statements.
  - Gate Report Writer and COBOL-2002+ features.
"@

# --- semantics-agent ---
Write-AgentFile "semantics-agent" @"
name: semantics-agent
role: "Semantic analysis specialist"
description: "Owns phases 7–14."
instructions: |
  - Close qualified name resolution.
  - Improve ODO layout.
  - Fix GLOBAL propagation.
"@

# --- flow-bound-agent ---
Write-AgentFile "flow-bound-agent" @"
name: flow-bound-agent
role: "Bound tree and flow analysis specialist"
description: "Owns phases 15–18."
instructions: |
  - Add branch-sensitive file state analysis.
"@

# --- ir-lowering-agent ---
Write-AgentFile "ir-lowering-agent" @"
name: ir-lowering-agent
role: "IR lowering specialist"
description: "Owns phases 19.1–19.6."
instructions: |
  - Maintain complete lowering coverage.
"@

# --- cil-emission-runtime-agent ---
Write-AgentFile "cil-emission-runtime-agent" @"
name: cil-emission-runtime-agent
role: "CIL emission and runtime specialist"
description: "Owns phases 20–21."
instructions: |
  - Close ENTRY USING and nested CALL StopRun TODOs.
"@

# --- nist-harness-agent ---
Write-AgentFile "nist-harness-agent" @"
name: nist-harness-agent
role: "NIST suite specialist"
description: "Tracks NIST coverage and regression baselines."
instructions: |
  - Expand beyond NC suite.
  - Plan full COBOL-85 NIST enablement.
"@

# --- docs-dx ---
Write-AgentFile "docs-dx" @"
name: docs-dx
role: "Documentation and DX specialist"
description: "Produces architecture docs and grammar audits."
instructions: |
  - Keep docs aligned with ledger and pipeline.
"@

# --- legacy-compat-agent ---
Write-AgentFile "legacy-compat-agent" @"
name: legacy-compat-agent
role: "Legacy behavior and compatibility specialist"
description: "Tracks COBOL-85 behavior and NIST expectations."
instructions: |
  - Maintain compatibility matrix.
"@

Write-Host "Agent files created."
