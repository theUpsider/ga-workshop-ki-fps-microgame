# Requirements & ReqToCode-Traceability

> Sprach-/framework-agnostische Bauanleitung des Gesamtsystems (fuer die
> Uebertragung auf andere Stacks): [Docs/reqtocode-blueprint.md](../reqtocode-blueprint.md)

Dieser Ordner ist die **Source of Truth** fuer alle Software-Requirements (SWR) des Projekts.
Aus den Markdown-Dokumenten hier werden **compile-zeit-feste Traceables** generiert
([Assets/FPS/Scripts/Game/Requirements/SWR.g.cs](../../Assets/FPS/Scripts/Game/Requirements/SWR.g.cs)),
die der Implementierungscode per Attribut referenziert. Ein gebrochener Trace ist damit kein
verwaistes Kommentar, sondern ein Build-Fehler.

## Requirement-Dokumente

Jedes Requirement ist eine Markdown-Datei mit YAML-Frontmatter. Nur Dateien mit `req-id`
werden vom Generator erfasst; alle anderen (Analyseberichte, Testprotokolle, editor-tasks)
werden ignoriert.

```markdown
---
req-id: SWR-101
status: approved
trace: required
title: Barrier-Komponente mit locked/unlocked-Zustand und Kollisionslogik
---
```

| Feld | Werte | Bedeutung |
| --- | --- | --- |
| `req-id` | `SWR-<nummer>` (eindeutig) | Stabile ID; wird zum Enum-Member `SWR.SWR_<nummer>` |
| `status` | `draft` \| `approved` \| `deprecated` | Lebenszyklus (siehe unten) |
| `trace` | `required` (Default) \| `optional` | Ob der Code das Requirement referenzieren **muss** |
| `test` | `required` (Default) \| `optional` | Ob ein Test das Requirement abdecken **muss** |
| `title` | Freitext | Kurztitel; Fallback ist die erste Markdown-Ueberschrift |

Nummernschema: `SWR-x00` = Epic eines Features (trace optional, wird ueber die Subtasks
realisiert), `SWR-x01..x99` = einzelne Requirements des Features. Vergeben: `1xx`
lockable-barrier, `2xx` alarm-system, `3xx` interaction-system, `4xx` stateful-hazard.

## Traces im Code

Der implementierende Code referenziert sein Requirement mit dem `[Traces]`-Attribut
(Klasse, Methode, Property, Feld, ...):

```csharp
[Traces(SWR.SWR_101)]
public class Barrier : MonoBehaviour { ... }

[Traces(SWR.SWR_102)]
void ApplyState() { ... }
```

Das ist ein Compile-Zeit-Link, kein Kommentar. Ein Element kann mehrere Requirements
tracen (`[Traces(SWR.SWR_101, SWR.SWR_102)]`), ein Requirement kann an mehreren Stellen
getraced werden.

## Testabdeckung im Code

Tests deklarieren mit dem `[Verifies]`-Attribut, welches Requirement sie abdecken —
das Test-Gegenstueck zu `[Traces]`:

```csharp
[Test]
[Verifies(SWR.SWR_101)]
public void Barrier_StartsLocked_WithActiveCollision() { ... }
```

`[Verifies]` zaehlt nur in Test-Assemblies (Assemblies mit NUnit-Referenz) als
Abdeckung. Jedes approved Requirement mit `test: required` braucht mindestens eine
`[Verifies]`-Referenz, sonst schlaegt die Verifikation fehl (Console-Error,
Testfehler, Build-Abbruch, Pre-Commit-Hook). Implementierungs-Traces und
Test-Abdeckung werden getrennt gezaehlt — ein `[Traces]` in einem Test ersetzt
keine Abdeckung und umgekehrt.

## Lebenszyklus (graduated lifecycle)

| Statusaenderung | Wirkung |
| --- | --- |
| `draft` | Traceable existiert, Code darf referenzieren, nichts wird erzwungen |
| `approved` + `trace: required` | Mindestens eine `[Traces]`-Referenz muss existieren, sonst Verifikationsfehler (Console-Error nach jedem Compile, Testfehler, Build-Abbruch) |
| `approved` + `test: required` | Mindestens eine `[Verifies]`-Referenz in einer Test-Assembly muss existieren, sonst Verifikationsfehler |
| `deprecated` | Traceable erhaelt `[Obsolete]` → IDE-/Compiler-Warnung an **jeder** Referenzstelle (auch in Tests) |
| Datei/`req-id` entfernt | Enum-Member verschwindet → **Compile-Fehler** an jeder Referenzstelle |

## Ablauf bei Requirement-Aenderungen

1. Requirement-Datei aendern (Status, Text, neue Datei, Loeschung) → Commit.
2. Der Generator erkennt die Drift automatisch (Hash im Header von `SWR.g.cs`) und
   regeneriert beim naechsten Script-Reload; manuell: Menu **Tools ▸ ReqToCode ▸
   Regenerate Traceables**. Auch der Agent-Testlauf (`Tools/agent-tests/Invoke-AgentTests.ps1`)
   regeneriert vor dem Testen.
3. Compiler/IDE zeigen die Konsequenzen an den betroffenen Codestellen (Warnung bei
   deprecated, Fehler bei entfernt).
4. Verifikation: Menu **Tools ▸ ReqToCode ▸ Verify Traceability**, die EditMode-Tests
   (`Unity.FPS.Tests.ReqToCodeTests`) oder ein Player-Build (bricht bei Verstoessen ab).
5. Fuer den agentischen Change-Durchzug (Diff lesen → Referenzen finden → Code und
   Tests ueberarbeiten → committen) existiert ein Playbook als Agent-Skill:
   [.claude/skills/reqtocode-propagate/SKILL.md](../../.claude/skills/reqtocode-propagate/SKILL.md).

## Pre-Commit-Hook

Der versionierte Hook [Tools/git-hooks/pre-commit](../../Tools/git-hooks/pre-commit)
blockiert Commits, wenn Requirement-Quellen fehlerhaft sind, `SWR.g.cs` nicht zu den
Quellen passt oder ein approved Requirement mit `trace: required` nirgends referenziert
wird. Er laeuft ohne Unity-Editor (PowerShell-Spiegel der Generator-Logik in
[Tools/reqtocode/Check-ReqToCode.ps1](../../Tools/reqtocode/Check-ReqToCode.ps1)).

Einmalig pro Clone aktivieren:

```
git config core.hooksPath Tools/git-hooks
```

Bei veralteter `SWR.g.cs` ohne offenen Editor:

```
powershell -NoProfile -File Tools/reqtocode/Check-ReqToCode.ps1 -Fix
```

Der EditMode-Test `GeneratedTraceables_AreUpToDate` stellt sicher, dass Skript und
C#-Generator dasselbe Ergebnis erzeugen — der C#-Generator im Editor bleibt massgeblich.

## Beteiligte Komponenten

- [ReqToCodeGenerator.cs](../../Assets/FPS/Scripts/Game/Editor/ReqToCode/ReqToCodeGenerator.cs) — Frontmatter-Parser + Codegenerator + Auto-Regeneration
- [ReqToCodeVerifier.cs](../../Assets/FPS/Scripts/Game/Editor/ReqToCode/ReqToCodeVerifier.cs) — Traceability-Pruefung (Reflection ueber alle Spiel-Assemblies), Console-Errors nach jedem Reload
- [ReqToCodeBuildCheck.cs](../../Assets/FPS/Scripts/Game/Editor/ReqToCode/ReqToCodeBuildCheck.cs) — bricht Player-Builds bei Verstoessen ab
- [Traceability.cs](../../Assets/FPS/Scripts/Game/Requirements/Traceability.cs) — `TracesAttribute`, `RequirementAttribute`, `RequirementStatus`
- [SWR.g.cs](../../Assets/FPS/Scripts/Game/Requirements/SWR.g.cs) — generiert, nicht manuell editieren
- [ReqToCodeTests.cs](../../Assets/Tests/EditMode/ReqToCodeTests.cs) — EditMode-Tests der gesamten Kette
- [BarrierFeatureTests.cs](../../Assets/Tests/EditMode/BarrierFeatureTests.cs) — Testabdeckung SWR-101..104
- [AlarmSystemTests.cs](../../Assets/Tests/EditMode/AlarmSystemTests.cs) — Testabdeckung SWR-202..206
