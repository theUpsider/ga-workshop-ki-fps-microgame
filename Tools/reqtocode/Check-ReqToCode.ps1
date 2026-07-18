<#
.SYNOPSIS
    ReqToCode gate for use outside the Unity Editor (pre-commit hook, CI).

.DESCRIPTION
    Mirrors the checks of Unity.FPS.Game.Editor.ReqToCodeGenerator/-Verifier without
    needing a running Editor:

      1. All requirement documents under Docs/requirements parse (valid req-id,
         status, trace; unique IDs).
      2. The generated traceables (Assets/FPS/Scripts/Game/Requirements/SWR.g.cs)
         match the requirement sources (no drift).
      3. Every approved requirement with trace: required is referenced in a
         [Traces(...)] attribute and every approved requirement with test: required
         is referenced in a [Verifies(...)] attribute somewhere in Assets/**/*.cs
         (text-scan approximation of the reflection checks; the authoritative check
         runs in the Editor/EditMode tests).

    The C# generator in the Editor is the source of truth for the generated format;
    this script replicates it byte-for-byte and the EditMode test
    Unity.FPS.Tests.ReqToCodeTests.GeneratedTraceables_AreUpToDate guards against
    the two implementations drifting apart.

.PARAMETER Fix
    Regenerate SWR.g.cs instead of failing when it is stale.

.OUTPUTS
    Exit code 0 = all checks passed, 1 = violations found, 2 = internal error.
#>
param(
    [switch]$Fix
)

$ErrorActionPreference = "Stop"

try {
    $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $reqDir = Join-Path $root "Docs\requirements"
    $generatedPath = Join-Path $root "Assets\FPS\Scripts\Game\Requirements\SWR.g.cs"
    $generatedRel = "Assets/FPS/Scripts/Game/Requirements/SWR.g.cs"

    $sha = [System.Security.Cryptography.SHA256]::Create()
    function Get-ShortHash([string]$s) {
        $bytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($s))
        (($bytes[0..7] | ForEach-Object { $_.ToString("x2") }) -join "")
    }
    function Normalize([string]$s) { $s.Replace("`r`n", "`n").Replace("`r", "`n") }

    $errors = @()
    $reqs = @()

    if (-not (Test-Path $reqDir)) {
        Write-Host "[ReqToCode] Requirements folder not found: Docs/requirements" -ForegroundColor Red
        exit 1
    }

    # --- 1. Parse requirement documents (mirrors ReqToCodeGenerator.ParseRequirements) ---
    $files = Get-ChildItem $reqDir -Recurse -Filter *.md | Sort-Object FullName
    foreach ($f in $files) {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        $rel = $f.FullName.Substring($root.Length + 1).Replace("\", "/")
        $lines = (Normalize $text).Split("`n")

        $idx = 0
        while ($idx -lt $lines.Length -and $lines[$idx].Trim().Length -eq 0) { $idx++ }
        if ($idx -ge $lines.Length -or $lines[$idx].Trim() -ne "---") { continue }

        $fm = @{}
        $closed = $false
        for ($i = $idx + 1; $i -lt $lines.Length; $i++) {
            if ($lines[$i].Trim() -eq "---") { $closed = $true; break }
            $colon = $lines[$i].IndexOf(":")
            if ($colon -gt 0) { $fm[$lines[$i].Substring(0, $colon).Trim().ToLowerInvariant()] = $lines[$i].Substring($colon + 1).Trim() }
        }
        if (-not $closed -or -not $fm.ContainsKey("req-id")) { continue }

        $id = $fm["req-id"]
        if ($id -notmatch "^SWR-(\d+)$") {
            $errors += "[ReqToCode] ${rel}: invalid req-id '$id' (expected format SWR-<number>)."
            continue
        }
        $num = [int]$Matches[1]

        $status = "Draft"
        if ($fm.ContainsKey("status")) {
            switch ($fm["status"].ToLowerInvariant()) {
                "draft" { $status = "Draft" }
                "approved" { $status = "Approved" }
                "deprecated" { $status = "Deprecated" }
                default { $errors += "[ReqToCode] ${rel}: invalid status '$($fm["status"])' (draft | approved | deprecated)."; continue }
            }
        }

        $traceRequired = $true
        if ($fm.ContainsKey("trace")) {
            switch ($fm["trace"].ToLowerInvariant()) {
                "required" { $traceRequired = $true }
                "optional" { $traceRequired = $false }
                default { $errors += "[ReqToCode] ${rel}: invalid trace '$($fm["trace"])' (required | optional)."; continue }
            }
        }

        $testRequired = $true
        if ($fm.ContainsKey("test")) {
            switch ($fm["test"].ToLowerInvariant()) {
                "required" { $testRequired = $true }
                "optional" { $testRequired = $false }
                default { $errors += "[ReqToCode] ${rel}: invalid test '$($fm["test"])' (required | optional)."; continue }
            }
        }

        $title = $null
        if ($fm.ContainsKey("title") -and $fm["title"].Trim().Length -gt 0) {
            $title = $fm["title"].Trim()
        }
        else {
            foreach ($line in $lines) {
                if ($line.TrimStart().StartsWith("# ")) { $title = $line.TrimStart().Substring(2).Trim(); break }
            }
            if (-not $title) { $title = $id }
        }

        $reqs += [pscustomobject]@{
            Id = $id; Number = $num; Status = $status; Title = $title
            SourcePath = $rel; TraceRequired = $traceRequired; TestRequired = $testRequired
            ContentHash = (Get-ShortHash (Normalize $text))
        }
    }

    $reqs | Group-Object Number | Where-Object Count -gt 1 | ForEach-Object {
        $errors += "[ReqToCode] Duplicate req-id SWR-$($_.Name) in: $(($_.Group | ForEach-Object SourcePath) -join ', ')"
    }

    $reqs = $reqs | Sort-Object Number

    # --- 2. Generated traceables must match the sources (mirrors GenerateSource) ---
    if ($errors.Count -eq 0) {
        $globalHash = Get-ShortHash (($reqs | ForEach-Object { "$($_.Id)|$($_.Status)|$($_.TraceRequired)|$($_.TestRequired)|$($_.Title)|$($_.SourcePath)|$($_.ContentHash)" }) -join "`n")

        $sb = New-Object System.Text.StringBuilder
        $null = $sb.Append("// <auto-generated>`n")
        $null = $sb.Append("//     ReqToCode traceables, generated from Docs/requirements. DO NOT EDIT MANUALLY.`n")
        $null = $sb.Append("//     Source of truth: the markdown file referenced on each member.`n")
        $null = $sb.Append("//     Regenerate: menu `"Tools/ReqToCode/Regenerate Traceables`" (also runs automatically on script reload).`n")
        $null = $sb.Append("//     requirements-hash: $globalHash`n")
        $null = $sb.Append("// </auto-generated>`n")
        $null = $sb.Append("using System;`n`n")
        $null = $sb.Append("namespace Unity.FPS.Game`n{`n")
        $null = $sb.Append("    /// <summary>`n")
        $null = $sb.Append("    /// Software requirements (SWR) as compile-time traceables (ReqToCode).`n")
        $null = $sb.Append("    /// Removing a requirement removes its member, so every [Traces] reference breaks the build.`n")
        $null = $sb.Append("    /// Deprecating a requirement raises obsolete-warnings at every reference site.`n")
        $null = $sb.Append("    /// </summary>`n")
        $null = $sb.Append("    public enum SWR`n    {`n")
        $first = $true
        foreach ($r in $reqs) {
            if (-not $first) { $null = $sb.Append("`n") }
            $first = $false
            $statusLabel = $r.Status.ToLowerInvariant()
            $traceLit = if ($r.TraceRequired) { "true" } else { "false" }
            $testLit = if ($r.TestRequired) { "true" } else { "false" }
            $xmlTitle = $r.Title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            $csTitle = $r.Title.Replace("\", "\\").Replace('"', '\"')
            $null = $sb.Append("        /// <summary>[$statusLabel] $xmlTitle ($($r.SourcePath))</summary>`n")
            if ($r.Status -eq "Deprecated") {
                $null = $sb.Append("        [Obsolete(`"$($r.Id) is deprecated: $csTitle (see $($r.SourcePath))`")]`n")
            }
            $null = $sb.Append("        [Requirement(`"$($r.Id)`", RequirementStatus.$($r.Status), `"$csTitle`", `"$($r.SourcePath)`", $traceLit, `"$($r.ContentHash)`", $testLit)]`n")
            $null = $sb.Append("        $($r.Id.Replace('-', '_')) = $($r.Number),`n")
        }
        $null = $sb.Append("    }`n}`n")
        $expected = $sb.ToString()

        $stale = $false
        if (-not (Test-Path $generatedPath)) {
            $stale = $true
        }
        else {
            $actual = [System.IO.File]::ReadAllText($generatedPath)
            $stale = ((Normalize $actual) -ne (Normalize $expected))
        }

        if ($stale) {
            if ($Fix) {
                New-Item -ItemType Directory -Force (Split-Path -Parent $generatedPath) | Out-Null
                [System.IO.File]::WriteAllText($generatedPath, $expected, (New-Object System.Text.UTF8Encoding($false)))
                Write-Host "[ReqToCode] Regenerated $generatedRel." -ForegroundColor Yellow
            }
            else {
                $errors += "[ReqToCode] $generatedRel does not match the requirement sources. Regenerate (Unity menu Tools/ReqToCode, or run this script with -Fix) and stage the result."
            }
        }
    }

    # --- 3. Approved requirements: required traces ([Traces]) and test coverage ([Verifies]) ---
    $tracedNumbers = @{}
    $verifiedNumbers = @{}
    Get-ChildItem (Join-Path $root "Assets") -Recurse -Filter *.cs |
        Where-Object { $_.Name -notlike "*.g.cs" } |
        Select-String -Pattern "(Traces|Verifies)\s*\(([^)]*)" -AllMatches |
        ForEach-Object { $_.Matches } |
        ForEach-Object {
            $kind = $_.Groups[1].Value
            foreach ($m in [regex]::Matches($_.Groups[2].Value, "SWR_(\d+)")) {
                if ($kind -eq "Traces") { $tracedNumbers[[int]$m.Groups[1].Value] = $true }
                else { $verifiedNumbers[[int]$m.Groups[1].Value] = $true }
            }
        }

    foreach ($r in $reqs) {
        $member = "SWR.$($r.Id.Replace('-', '_'))"
        if ($r.Status -eq "Approved" -and $r.TraceRequired -and -not $tracedNumbers.ContainsKey($r.Number)) {
            $errors += "[ReqToCode] $($r.Id) `"$($r.Title)`" is approved but not traced in code. Add [Traces($member)] to the implementing code element (source: $($r.SourcePath))."
        }
        if ($r.Status -eq "Approved" -and $r.TestRequired -and -not $verifiedNumbers.ContainsKey($r.Number)) {
            $errors += "[ReqToCode] $($r.Id) `"$($r.Title)`" is approved but has no test coverage. Add [Verifies($member)] to a test (source: $($r.SourcePath))."
        }
    }

    # --- Result ---
    if ($errors.Count -gt 0) {
        foreach ($e in $errors) { Write-Host $e -ForegroundColor Red }
        Write-Host ""
        Write-Host "[ReqToCode] $($errors.Count) violation(s). Commit aborted." -ForegroundColor Red
        exit 1
    }

    Write-Host "[ReqToCode] OK: $($reqs.Count) requirement(s), traceables current, all required traces present."
    exit 0
}
catch {
    Write-Host "[ReqToCode] Internal error: $($_.Exception.Message)" -ForegroundColor Red
    exit 2
}
