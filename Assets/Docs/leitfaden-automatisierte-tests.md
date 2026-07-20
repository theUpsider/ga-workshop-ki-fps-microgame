# Umsetzungsauftrag: Automatische Unity-Tests für Codex, Claude Code und GitHub Copilot

## 1. Ziel

Implementiere in der Unity-Codebase eine standardisierte Testinfrastruktur, über die folgende Coding-Agenten Tests selbstständig ausführen und auswerten können:

- OpenAI Codex
- Claude Code
- GitHub Copilot beziehungsweise Copilot Coding Agent und Copilot CLI

Die Agenten dürfen Unity nicht über individuell erzeugte Kommandozeilenaufrufe starten. Stattdessen müssen alle Agenten, Entwickler und CI-Systeme dieselbe versionierte Repository-Schnittstelle verwenden:

```text
python tools/unity_test.py <Argumente>
```

Das Skript ist der einzige offizielle Einstiegspunkt für automatisierte Unity-Tests.

Die Lösung muss folgende Eigenschaften besitzen:

- lokal und in CI ausführbar,
- reproduzierbar und ohne manuelle Eingaben,
- für Edit-Mode- und Play-Mode-Tests geeignet,
- für Coding-Agenten eindeutig dokumentiert,
- mit maschinenlesbaren Ergebnissen,
- mit zuverlässigen Exit-Codes,
- gegen parallele Unity-Prozesse abgesichert,
- unabhängig vom verwendeten KI-Anbieter.

Custom Instructions allein gelten nicht als Qualitätskontrolle. Sie unterstützen den Agenten bei der Arbeit; verbindlich sind das Testskript und die CI-Branch-Protection. GitHub weist darauf hin, dass Copilot benutzerdefinierte Anweisungen wegen des nichtdeterministischen Modellverhaltens nicht immer identisch befolgt.

---

## 2. Technische Grundentscheidung

### 2.1 Gemeinsamer Test-Entrypoint

Erstelle:

```text
tools/unity_test.py
```

Das Skript kapselt vollständig:

1. Auffinden der richtigen Unity-Version,
2. Erstellen der Ergebnisverzeichnisse,
3. Starten des Unity Editors,
4. Auswahl der Testplattform,
5. Setzen optionaler Filter,
6. Überwachung von Timeout und Prozessende,
7. Auswertung des NUnit-XML-Ergebnisses,
8. Auswertung des Unity-Logs,
9. Ausgabe einer kompakten Zusammenfassung,
10. Setzen eines eindeutigen Exit-Codes.

Unity unterstützt die automatisierte Testausführung über `-runTests`, `-testPlatform`, `-testResults`, Filter und Assembly-Namen. Der Batch-Modus ermöglicht die Ausführung ohne manuelle Eingaben.

### 2.2 Warum ein Repository-Skript notwendig ist

Die Agenten sollen nicht selbst entscheiden müssen:

- wo Unity installiert ist,
- wie die Kommandozeilenargumente geschrieben werden,
- wo Testergebnisse gespeichert werden,
- welche Exit-Codes als Fehler gelten,
- wie ein fehlendes XML-Ergebnis behandelt wird,
- wie lange ein Test laufen darf,
- welche Tests bei welcher Änderung erforderlich sind.

Diese Entscheidungen gehören in versionierten Code und dürfen nicht ausschließlich in Prompts stehen.

Codex kann Shell-Befehle und Test-Harnesses ausführen und lässt sich über `AGENTS.md` anweisen, bestimmte Testbefehle zu verwenden. Claude Code kann Tests über seine Terminalwerkzeuge ausführen und Befehle gezielt über erlaubte Tools freigeben. GitHub Copilot unterstützt Repository-Anweisungen sowie `AGENTS.md` und `CLAUDE.md` in seinen agentischen Oberflächen.

---

## 3. Vorgesehene Repository-Struktur

Implementiere mindestens folgende Struktur:

```text
RepositoryRoot/
├── AGENTS.md
├── CLAUDE.md
├── .github/
│   ├── copilot-instructions.md
│   └── workflows/
│       └── unity-tests.yml
├── .claude/
│   └── settings.json
├── docs/
│   └── ai/
│       └── testing.md
├── tools/
│   ├── unity_test.py
│   └── unity_test_config.json
├── artifacts/
│   └── unity-tests/
├── Assets/
│   └── Game/
│       ├── Runtime/
│       │   └── Game.Runtime.asmdef
│       └── Tests/
│           ├── EditMode/
│           │   └── Game.Tests.EditMode.asmdef
│           └── PlayMode/
│               └── Game.Tests.PlayMode.asmdef
└── ProjectSettings/
    └── ProjectVersion.txt
```

Folgende Verzeichnisse gehören in `.gitignore`:

```gitignore
artifacts/unity-tests/
Library/
Temp/
Logs/
obj/
```

Einzelne Platzhalterdateien dürfen aufgenommen werden, falls das Ergebnisverzeichnis im Repository sichtbar bleiben soll.

---

## 4. Unity-Testarchitektur

### 4.1 Assembly-Trennung

Produktionscode, Edit-Mode-Tests und Play-Mode-Tests müssen in getrennten Assembly Definitions liegen.

Empfohlene Richtung:

```text
Game.Tests.EditMode  ──> Game.Runtime
Game.Tests.PlayMode  ──> Game.Runtime
```

Nicht erlaubt:

```text
Game.Runtime ──> Game.Tests.EditMode
Game.Runtime ──> Game.Tests.PlayMode
```

Unity-Testcode muss in einer Test Assembly liegen. Die Test Assembly muss die getestete Runtime Assembly ausdrücklich referenzieren. Automatisch erzeugter Code in `Assembly-CSharp.dll` ist dafür ungeeignet; testbarer Produktionscode soll deshalb ebenfalls in eigenen `.asmdef`-Assemblies liegen.

### 4.2 Edit Mode als Standard

Verwende Edit-Mode-Tests für:

- reine C#-Logik,
- Berechnungen,
- Validierungen,
- Zustandsautomaten,
- Inventar- und Kampflogik,
- Serialisierung ohne Player-Laufzeit,
- Fehler- und Randfälle,
- Code mit abstrahierten externen Abhängigkeiten.

Verwende Play-Mode-Tests nur, wenn das Verhalten tatsächlich von Unity-Laufzeitfunktionen abhängt:

- `MonoBehaviour`-Lebenszyklus,
- Frames und Coroutines,
- `Update`, `FixedUpdate` oder `LateUpdate`,
- `Time.deltaTime`,
- Physik,
- Szenen,
- Prefabs im Laufzeitkontext,
- Input-System,
- Animationen,
- Unity Player Loop.

Die vorhandene Testdokumentation empfiehlt, Spiellogik möglichst als reine C#-Logik mit Interfaces zu strukturieren. Dadurch kann der überwiegende Teil schnell im Edit Mode getestet werden; Frames, Physik, Szenen und `MonoBehaviour`-Lebenszyklen verbleiben im Play Mode.

### 4.3 Testaufbau

Tests müssen nach Arrange–Act–Assert aufgebaut sein:

```text
Arrange: Voraussetzungen und Testobjekte erzeugen.
Act: Genau die zu prüfende Aktion ausführen.
Assert: Beobachtbares Ergebnis prüfen.
```

Unity verwendet das Unity Test Framework auf Basis von NUnit. Normale Tests werden mit `[Test]` beziehungsweise `[TestCase]` geschrieben; `[UnityTest]` ist für frameübergreifendes Laufzeitverhalten vorgesehen.
Testnamen verwenden das Schema:

```text
MethodOrBehavior_Condition_ExpectedResult
```

Beispiele:

```csharp
CalculateDamage_WithArmor_ReturnsReducedDamage
TryExecute_DuringCooldown_ReturnsFalse
LoadInventory_WithInvalidData_ReturnsEmptyInventory
```

### 4.4 Testbarkeit des Produktionscodes

Abhängigkeiten, die Tests langsam oder nichtdeterministisch machen, müssen über Interfaces abstrahiert werden:

- Zeit,
- Zufallswerte,
- Netzwerk,
- Dateisystem,
- Datenbanken,
- Plattformdienste,
- Eingabesystem,
- Audio,
- externe SDKs.

Beispiel:

```csharp
public interface ITimeProvider
{
    float CurrentTime { get; }
}
```

Der Runtime-Code erhält die echte Unity-Implementierung. Tests erhalten eine kontrollierbare Fake-Implementierung.

Private Methoden werden nicht direkt getestet. Geprüft wird das beobachtbare Verhalten über öffentliche oder gezielt freigegebene interne Schnittstellen.

### 4.5 TDD-Regel für Agenten

Bei Bugfixes und klar spezifizierter Geschäftslogik gilt:

1. Einen Test schreiben, der den Fehler reproduziert und fehlschlägt.
2. Nur den notwendigen Produktionscode ändern.
3. Den Test erfolgreich ausführen.
4. Refactoring durchführen.
5. Relevante Regressionstests erneut ausführen.

Dies entspricht dem Red-Green-Refactor-Zyklus.

Bei Legacy-Code ohne vorhandene Tests soll vor einer Verhaltensänderung zunächst ein Characterization Test das bestehende Verhalten dokumentieren.

---

## 5. Schnittstelle des Testskripts

### 5.1 Erforderliche Kommandos

Das Skript muss mindestens folgende Aufrufe unterstützen:

```bash
python tools/unity_test.py --suite fast
python tools/unity_test.py --suite full
python tools/unity_test.py --platform EditMode
python tools/unity_test.py --platform PlayMode
python tools/unity_test.py --platform EditMode --filter Game.Tests.EditMode.DamageCalculatorTests
python tools/unity_test.py --platform PlayMode --category Smoke
python tools/unity_test.py --platform EditMode --assembly Game.Tests.EditMode
```

Semantik:

| Aufruf         | Verhalten                                                       |
| -------------- | --------------------------------------------------------------- |
| `--suite fast` | Alle Edit-Mode-Tests                                            |
| `--suite full` | Alle Edit-Mode-Tests, danach alle Play-Mode-Tests               |
| `--platform`   | Nur angegebene Testplattform                                    |
| `--filter`     | Vollqualifizierter NUnit- beziehungsweise Unity-Testfilter      |
| `--category`   | Eine oder mehrere Testkategorien                                |
| `--assembly`   | Eine oder mehrere Test-Assemblies                               |
| `--ci`         | CI-Ausgabe, keine interaktiven Prompts                          |
| `--timeout`    | Optionales Überschreiben des konfigurierten Timeouts            |
| `--keep-going` | Bei einer Suite nach einem Fehler weitere Plattformen ausführen |

Filterargumente dürfen nur kontrolliert an Unity weitergereicht werden. Das Skript darf keine Shell-Strings zusammensetzen, sondern muss Prozesse mit einer Argumentliste starten.

### 5.2 Konfigurationsdatei

Erstelle:

```json
{
  "projectPath": ".",
  "resultsDirectory": "artifacts/unity-tests",
  "unityEditorEnvironmentVariable": "UNITY_EDITOR",
  "failOnNoTests": true,
  "failOnInconclusive": true,
  "timeoutsSeconds": {
    "EditMode": 900,
    "PlayMode": 1800
  },
  "suites": {
    "fast": [
      {
        "platform": "EditMode"
      }
    ],
    "full": [
      {
        "platform": "EditMode"
      },
      {
        "platform": "PlayMode"
      }
    ]
  }
}
```

Konfigurationswerte dürfen projektbezogen angepasst werden. Der Agent darf sie nicht stillschweigend verändern, um fehlschlagende Tests zu umgehen.

### 5.3 Auflösung des Unity Editors

Reihenfolge:

1. Verwende `UNITY_EDITOR`, falls gesetzt.

2. Lies die erwartete Version aus:

   ```text
   ProjectSettings/ProjectVersion.txt
   ```

3. Suche optional in bekannten Unity-Hub-Verzeichnissen.

4. Brich mit einem Infrastrukturfehler ab, wenn:
   - Unity nicht gefunden wird,
   - die gefundene Version nicht zur Projektversion passt,
   - die ausführbare Datei nicht gestartet werden kann.

Beispielpfade:

```text
Windows:
C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe

macOS:
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity

Linux:
$HOME/Unity/Hub/Editor/<version>/Editor/Unity
```

In CI muss `UNITY_EDITOR` explizit gesetzt sein. CI darf sich nicht auf eine heuristische Pfadsuche verlassen.

### 5.4 Unity-Aufruf

Edit Mode:

```text
<UnityEditor>
-batchmode
-nographics
-quit
-projectPath <absoluter Repository-Pfad>
-runTests
-testPlatform EditMode
-testResults <absoluter XML-Pfad>
-logFile <absoluter Log-Pfad>
```

Play Mode:

```text
<UnityEditor>
-batchmode
-nographics
-quit
-projectPath <absoluter Repository-Pfad>
-runTests
-testPlatform PlayMode
-testResults <absoluter XML-Pfad>
-logFile <absoluter Log-Pfad>
```

Bei Play-Mode-Tests, die eine GPU oder ein sichtbares Grafiksystem benötigen, muss `-nographics` über eine explizite Konfiguration deaktivierbar sein. Solche Tests sind auf einem geeigneten Self-Hosted Runner auszuführen.

### 5.5 Ergebnisverzeichnis

Jeder Lauf erhält ein eigenes Verzeichnis:

```text
artifacts/unity-tests/
└── 2026-07-20T143512Z-editmode/
    ├── results.xml
    ├── unity.log
    ├── summary.json
    └── command.txt
```

`command.txt` darf keine Secrets enthalten.

`summary.json` soll mindestens enthalten:

```json
{
  "status": "failed",
  "platform": "EditMode",
  "total": 42,
  "passed": 40,
  "failed": 2,
  "skipped": 0,
  "inconclusive": 0,
  "durationSeconds": 12.48,
  "failedTests": [
    {
      "name": "Game.Tests.EditMode.InventoryTests.AddItem_WhenFull_ReturnsFalse",
      "message": "Expected: False, But was: True"
    }
  ],
  "resultFile": "results.xml",
  "logFile": "unity.log"
}
```

### 5.6 Exit-Codes

Verwende verbindlich:

| Exit-Code | Bedeutung                                          |
| --------: | -------------------------------------------------- |
|       `0` | Alle geforderten Tests bestanden                   |
|       `1` | Mindestens ein Test fehlgeschlagen                 |
|       `2` | Unity-, Konfigurations- oder Kompilierungsfehler   |
|       `3` | Timeout                                            |
|       `4` | Keine Tests gefunden, obwohl Tests erwartet wurden |
|       `5` | Ungültige Skriptargumente                          |

Das Skript darf nicht ausschließlich den Unity-Prozesscode verwenden. Es muss zusätzlich das XML-Ergebnis prüfen.

Fehlt das XML-Ergebnis, ist der Lauf niemals erfolgreich. In diesem Fall muss das Unity-Log nach typischen Kompilierungs-, Lizenz-, Paket- oder Startfehlern ausgewertet und Exit-Code `2` zurückgegeben werden.

### 5.7 Konsolenausgabe

Bei Erfolg:

```text
UNITY TESTS PASSED
Platform: EditMode
Total: 42
Passed: 42
Duration: 12.48 s
Results: artifacts/unity-tests/.../results.xml
```

Bei Fehler:

```text
UNITY TESTS FAILED
Platform: EditMode
Total: 42
Passed: 40
Failed: 2

1. Game.Tests.EditMode.InventoryTests.AddItem_WhenFull_ReturnsFalse
   Expected: False
   But was: True

2. Game.Tests.EditMode.InventoryTests.RemoveItem_WhenMissing_ReturnsFalse
   Expected: False
   But was: True

Full log: artifacts/unity-tests/.../unity.log
```

Die Konsole soll nur die wichtigsten Fehler enthalten. Vollständige Stacktraces verbleiben im Ergebnis und Log.

### 5.8 Timeout und Prozessbereinigung

Bei Überschreitung des Timeouts muss das Skript:

1. den Unity-Prozess beenden,
2. zugehörige Kindprozesse beenden,
3. ein partielles `summary.json` schreiben,
4. Exit-Code `3` zurückgeben,
5. den Logpfad ausgeben.

Ein hängender Unity-Prozess darf den Agentenlauf oder CI-Runner nicht dauerhaft blockieren.

### 5.9 Parallele Ausführung

Zwei Unity-Prozesse dürfen nicht gleichzeitig dieselbe Arbeitskopie verwenden.

Implementiere eine Lock-Datei, beispielsweise:

```text
artifacts/unity-tests/.unity-test.lock
```

Alternativ kann ein betriebssystemweites File Lock verwendet werden.

Regeln:

- Pro Worktree beziehungsweise Clone maximal ein aktiver Unity-Testprozess.
- Parallele Agenten müssen getrennte Worktrees oder Clones verwenden.
- `Library`, `Temp` und Ergebnisverzeichnisse dürfen nicht zwischen gleichzeitig laufenden Worktrees geteilt werden.
- Ein veralteter Lock muss nur dann entfernt werden, wenn nachweislich kein zugehöriger Prozess mehr läuft.

---

## 6. Verbindliche Testauswahl für Coding-Agenten

Der Agent muss nach jeder relevanten Änderung Tests ausführen.

| Änderung                                    | Erforderliche Tests                                                        |
| ------------------------------------------- | -------------------------------------------------------------------------- |
| Reine C#-Logik                              | Betroffene Edit-Mode-Tests, danach gesamte Edit-Mode-Suite                 |
| Bugfix                                      | Zuerst fehlschlagender Regressionstest, danach gesamte Edit-Mode-Suite     |
| `MonoBehaviour` oder Player Loop            | Betroffene Play-Mode-Tests und gesamte Edit-Mode-Suite                     |
| Physik, Szene, Prefab, Input oder Animation | Betroffene Play-Mode-Tests und Play-Mode-Smoke-Suite                       |
| `.asmdef`-Änderung                          | Vollständige Suite                                                         |
| `Packages/manifest.json` oder Package-Lock  | Vollständige Suite                                                         |
| `ProjectSettings`                           | Vollständige Suite                                                         |
| Gemeinsame Testutilities                    | Vollständige Suite                                                         |
| Ausschließlich Dokumentation                | Keine Unity-Tests erforderlich                                             |
| Unklare Auswirkung                          | Vollständige Edit-Mode-Suite; bei Unity-Laufzeitbezug zusätzlich Play Mode |

Der Agent darf Tests nur überspringen, wenn:

- ausschließlich Dokumentation geändert wurde, oder
- die benötigte Unity-Infrastruktur nachweislich nicht verfügbar ist.

Im zweiten Fall muss der Agent den Zustand ausdrücklich melden und darf die Aufgabe nicht als vollständig validiert bezeichnen.

---

## 7. Inhalt der Agentenanweisungen

### 7.1 Zentrale Datei `docs/ai/testing.md`

Lege alle ausführlichen Regeln in dieser Datei ab.

Mindestinhalt:

```markdown
# Unity testing contract

## Required command

Run Unity tests only through:

python tools/unity_test.py ...

Do not invoke the Unity executable directly.

## Before changing code

- Identify whether the affected behavior belongs in Edit Mode or Play Mode.
- For bug fixes, add or identify a regression test that fails before the fix.
- Do not weaken, skip, delete, ignore or quarantine tests merely to obtain a green result.

## After changing code

- Run the narrowest relevant test first.
- Then run the required regression suite from the test selection matrix.
- Treat every non-zero exit code as a failed validation.
- Read summary.json and the Unity log when a run fails.
- Do not report success unless all required tests passed.

## Test design

- Use Arrange-Act-Assert.
- Prefer normal NUnit tests for pure C# behavior.
- Use UnityTest only for frame-dependent Unity behavior.
- Keep tests deterministic and independent.
- Abstract time, randomness, network, filesystem and platform services.
- Test observable behavior rather than private methods.

## Failure handling

- Fix production code when a valid test exposes a regression.
- Fix test code only when the test is demonstrably incorrect.
- Do not change expected values merely to match current behavior.
- Report baseline failures separately from failures introduced by the change.

## Required final report

State:

- which test commands were executed,
- whether each command passed,
- number of executed and failed tests,
- paths to the result XML and Unity log,
- any validation that could not be performed.
```

Tests müssen korrekt, wiederholbar, automatisierbar, unabhängig, nachvollziehbar, wartbar und aussagekräftig sein. Fehlgeschlagene Tests sollen die Ursache klar erkennen lassen.

### 7.2 `AGENTS.md`

`AGENTS.md` soll kurz bleiben und als Einstieg beziehungsweise Inhaltsverzeichnis dienen:

```markdown
# Repository agent instructions

Before modifying Unity C# code, scenes, prefabs, packages, assembly definitions
or project settings, read:

- docs/ai/testing.md

Run all Unity tests through:

python tools/unity_test.py ...

Never invoke the Unity editor directly.
Never report a task as validated when required tests were not executed or failed.
```

Codex verwendet `AGENTS.md` für Repository-Konventionen und Testbefehle. OpenAI empfiehlt, die Datei als kompakte Orientierung zu verwenden und ausführliche Informationen in strukturierte Dokumentation auszulagern.

### 7.3 `CLAUDE.md`

```markdown
@docs/ai/testing.md

# Repository entrypoint

Use only the repository test command:

python tools/unity_test.py ...

Do not invoke Unity directly.
```

Claude Code lädt `CLAUDE.md` als projektspezifische Anweisung und unterstützt Importe über `@pfad`. Anthropic empfiehlt, dort unter anderem Build-, Test- und Lint-Befehle zu dokumentieren.

### 7.4 `.github/copilot-instructions.md`

```markdown
@docs/ai/testing.md

For every Unity code change, run the required tests through:

python tools/unity_test.py ...

Do not invoke the Unity executable directly.
Do not state that a change is complete while required tests are failing.
Include the executed commands and results in the final response or pull request.
```

GitHub empfiehlt Repository-Anweisungen ausdrücklich dafür, Copilot mitzuteilen, wie ein Projekt gebaut, getestet und validiert wird.

---

## 8. Claude-Code-Berechtigungen

Erstelle `.claude/settings.json` mit möglichst enger Freigabe.

Beispiel:

```json
{
  "permissions": {
    "allow": [
      "Bash(python tools/unity_test.py:*)",
      "Bash(python3 tools/unity_test.py:*)",
      "Bash(git status:*)",
      "Bash(git diff:*)"
    ],
    "deny": ["Bash(*Unity.exe:*)", "Bash(*Unity.app*:*)"]
  }
}
```

Die endgültige Syntax ist gegen die im Projekt eingesetzte Claude-Code-Version zu prüfen.

Grundsätze:

- Erlaube das Wrapper-Skript.
- Erlaube nicht pauschal sämtliche Bash-Befehle.
- Verwende nicht dauerhaft `bypassPermissions`.
- Erlaube keinen direkten Unity-Aufruf.
- Erlaube keine Befehle zum Lesen oder Ausgeben von Secrets.

Claude Code unterstützt kommando- beziehungsweise präfixbezogene Berechtigungsregeln wie `Bash(npm run test:*)`; Deny-Regeln haben Vorrang.

---

## 9. CI-Integration

### 9.1 Runner

Bevorzugte Ausführung:

```text
GitHub Actions
└── Self-Hosted Runner
    ├── exakt benötigte Unity-Version
    ├── gültige Unity-Lizenz
    ├── Python
    └── Zugriff auf das Repository
```

Gründe:

- Die Unity-Version ist kontrolliert.
- Lizenzierung wird zentral verwaltet.
- Play-Mode-Tests können auf geeigneter Hardware laufen.
- Agenten müssen Unity nicht bei jeder Aufgabe neu installieren.
- Lokale Agenten und CI verwenden dasselbe Testskript.

Cloud-Coding-Agenten dürfen nicht automatisch als Unity-fähig betrachtet werden. Ist in ihrer Sandbox kein passender Unity Editor verfügbar, führen sie lokal nur statische Änderungen durch; die verbindliche Validierung übernimmt der Unity-CI-Runner. Für Codex kann eine passend konfigurierte Entwicklungsumgebung beziehungsweise ein Setup-Skript die Fehlerquote reduzieren.

### 9.2 Workflow

Erstelle `.github/workflows/unity-tests.yml`:

```yaml
name: Unity tests

on:
  pull_request:
  push:
    branches:
      - main

jobs:
  editmode:
    name: Unity Edit Mode
    runs-on: [self-hosted, windows, unity]
    timeout-minutes: 25

    steps:
      - name: Checkout
        uses: actions/checkout@<approved-version-or-pinned-sha>

      - name: Run Edit Mode tests
        shell: pwsh
        run: python tools/unity_test.py --platform EditMode --ci

      - name: Upload Edit Mode results
        if: always()
        uses: actions/upload-artifact@<approved-version-or-pinned-sha>
        with:
          name: unity-editmode-results
          path: artifacts/unity-tests/

  playmode:
    name: Unity Play Mode
    needs: editmode
    runs-on: [self-hosted, windows, unity]
    timeout-minutes: 40

    steps:
      - name: Checkout
        uses: actions/checkout@<approved-version-or-pinned-sha>

      - name: Run Play Mode tests
        shell: pwsh
        run: python tools/unity_test.py --platform PlayMode --ci

      - name: Upload Play Mode results
        if: always()
        uses: actions/upload-artifact@<approved-version-or-pinned-sha>
        with:
          name: unity-playmode-results
          path: artifacts/unity-tests/
```

Die Organisation soll GitHub Actions nach ihrer eigenen Supply-Chain-Richtlinie auf freigegebene Versionen oder vollständige Commit-SHAs pinnen.

### 9.3 Branch Protection

Konfiguriere folgende Checks als erforderlich:

```text
Unity Edit Mode
Unity Play Mode
```

Ein Pull Request darf nicht gemergt werden, wenn:

- ein Test fehlschlägt,
- Unity nicht startet,
- das Ergebnis-XML fehlt,
- keine Tests gefunden wurden,
- ein Timeout auftritt.

### 9.4 Optionales Stufenmodell

Falls die vollständige Play-Mode-Suite zu langsam ist:

```text
Agentenlauf:
- gezielte Tests
- vollständiger Edit Mode

Pull Request:
- vollständiger Edit Mode
- Play-Mode-Smoke-Tests

Nightly:
- vollständiger Play Mode
- Player-Tests
- Plattformtests
```

Tests werden dabei über NUnit-Kategorien markiert:

```csharp
[Category("Smoke")]
[Category("Integration")]
[Category("Slow")]
```

Eine Kategorie wie `Quarantined` darf nur für dokumentierte, zeitlich befristete Ausnahmefälle verwendet werden. Quarantänisierung benötigt:

- verknüpftes Issue,
- Begründung,
- verantwortliche Person,
- Ablaufdatum,
- sichtbaren CI-Report.

---

## 10. Verhalten der Coding-Agenten

Jeder Agent muss folgenden Ablauf einhalten:

### Phase 1: Analyse

1. Relevante Produktions- und Test-Assemblies identifizieren.
2. Betroffene Tests suchen.
3. Entscheiden, ob Edit Mode oder Play Mode benötigt wird.
4. Bei einem Bugfix einen reproduzierenden Test bestimmen oder erstellen.

### Phase 2: Baseline

Wenn die Ausführungskosten angemessen sind, vor der Änderung den relevanten Test ausführen:

```bash
python tools/unity_test.py --platform EditMode --filter <betroffener Test>
```

Schlägt bereits die Baseline fehl:

- Fehler dokumentieren,
- nicht als durch die eigene Änderung verursacht darstellen,
- keine fremden Fehler ohne Auftrag umfassend beheben,
- nach der Änderung sicherstellen, dass keine neuen Fehler entstanden sind.

### Phase 3: Implementierung

- Kleine, nachvollziehbare Änderungen durchführen.
- Keine Tests löschen, deaktivieren oder abschwächen.
- Keine erwarteten Werte an das fehlerhafte Ist-Verhalten anpassen.
- Keine künstlichen Wartezeiten verwenden, um Race Conditions zu verdecken.
- Keine Netzwerkabhängigkeiten in schnelle Tests einbauen.

### Phase 4: Gezielter Test

Zuerst den kleinsten relevanten Test ausführen.

### Phase 5: Regression

Danach die Testauswahl aus Abschnitt 6 ausführen.

### Phase 6: Fehlerbehebung

Bei Fehlern:

1. `summary.json` lesen.
2. Fehlgeschlagenen Test und Fehlermeldung analysieren.
3. Bei Infrastrukturfehlern `unity.log` lesen.
4. Ursache beheben.
5. Gezielten Test wiederholen.
6. Regressionstests wiederholen.

Der Agent darf eine begrenzte Fehlerbehebungsschleife ausführen. Wiederholt sich derselbe Infrastrukturfehler ohne neue Erkenntnis, muss er abbrechen und den konkreten Blocker melden.

### Phase 7: Abschlussbericht

Der Agent muss angeben:

```text
Tests executed:
- python tools/unity_test.py --platform EditMode --filter ...
  Result: passed, 3 tests

- python tools/unity_test.py --platform EditMode
  Result: passed, 126 tests

Artifacts:
- artifacts/unity-tests/.../results.xml
- artifacts/unity-tests/.../unity.log
```

Nicht zulässig:

```text
The tests should pass.
The change appears correct.
I could not run Unity, but the implementation is complete.
```

Zulässig:

```text
Implementation completed. Unity validation was not possible because the required
Unity Editor version was unavailable. The change is not fully validated.
```

---

## 11. Sicherheits- und Betriebsregeln

### 11.1 Secrets

Folgende Werte dürfen niemals im Repository, in Agentenanweisungen, Logs oder Kommandodateien stehen:

- Unity-Lizenzdaten,
- API-Schlüssel,
- GitHub-Tokens,
- Zugangsdaten externer Dienste,
- private Zertifikate.

Secrets werden ausschließlich über den Secret Store des CI-Systems oder die Runner-Konfiguration bereitgestellt.

### 11.2 Netzwerk

Edit-Mode- und normale Play-Mode-Tests sollen standardmäßig ohne externes Netzwerk funktionieren.

Tests gegen echte externe Dienste müssen:

- separat kategorisiert sein,
- explizit gestartet werden,
- kontrollierte Testkonten verwenden,
- niemals Teil der schnellen Agentenschleife sein.

### 11.3 Dateisystem

Tests dürfen nicht unkontrolliert in Benutzerverzeichnisse oder globale Systempfade schreiben.

Temporäre Testdaten gehören in:

```text
artifacts/unity-tests/<run-id>/temp/
```

oder in ein vom Betriebssystem bereitgestelltes Temp-Verzeichnis, das nach dem Test bereinigt wird.

### 11.4 Keine automatische Testmanipulation

Das Testskript und die Agentenanweisungen müssen ausdrücklich verbieten:

- `[Ignore]` hinzuzufügen, nur um einen Lauf grün zu machen,
- Testdateien zu löschen,
- Assertions zu entfernen,
- Timeouts ohne Begründung stark zu erhöhen,
- Fehler als `Inconclusive` umzudeklarieren,
- Testfilter so zu verändern, dass relevante Tests nicht mehr laufen,
- `failOnNoTests` zu deaktivieren,
- CI-Schritte mit `continue-on-error` zu versehen.

---

## 12. Implementierungsreihenfolge

Der Auftrag ist in folgender Reihenfolge umzusetzen.

### Schritt 1: Bestand analysieren

Dokumentiere:

- Unity-Version,
- vorhandene Test-Framework-Version,
- bestehende `.asmdef`-Dateien,
- vorhandene Edit-Mode-Tests,
- vorhandene Play-Mode-Tests,
- bestehende CI,
- Betriebssystem der Entwickler und Runner,
- bekannte nichtdeterministische Tests.

### Schritt 2: Assembly-Struktur herstellen

- Runtime-Code in testbare Assemblies überführen.
- Edit-Mode-Testassembly erstellen.
- Play-Mode-Testassembly erstellen.
- Referenzen korrekt setzen.
- Sicherstellen, dass Produktionscode nicht von Testcode abhängt.

### Schritt 3: Testskript implementieren

- CLI-Argumente,
- Unity-Auflösung,
- Prozessstart,
- Timeout,
- Lock,
- XML-Auswertung,
- Logauswertung,
- Exit-Codes,
- JSON-Zusammenfassung.

### Schritt 4: Testskript selbst testen

Für `unity_test.py` sind eigene automatisierte Tests ohne Unity zu erstellen. Unity-Prozess und Dateisystemzugriffe sollen mockbar sein.

Mindestens testen:

- Argumentvalidierung,
- Unity-Versionserkennung,
- korrekte Argumentliste,
- erfolgreiches NUnit-XML,
- fehlgeschlagenes NUnit-XML,
- fehlendes XML,
- keine Tests,
- Kompilierungsfehler im Log,
- Timeout,
- bestehender Lock,
- Erzeugung von `summary.json`,
- korrekte Exit-Codes.

### Schritt 5: Agentenanweisungen erstellen

- `docs/ai/testing.md`
- `AGENTS.md`
- `CLAUDE.md`
- `.github/copilot-instructions.md`
- `.claude/settings.json`

### Schritt 6: CI einrichten

- Self-Hosted Runner vorbereiten.
- Exakte Unity-Version installieren.
- Lizenz sicher konfigurieren.
- Workflow hinzufügen.
- Testergebnisse als Artefakte sichern.
- Branch Protection aktivieren.

### Schritt 7: Mit allen Agenten verifizieren

Jeweils eine kleine, isolierte Änderung mit folgenden Werkzeugen durchführen:

1. Codex,
2. Claude Code,
3. GitHub Copilot.

Jeder Agent muss:

- die Dokumentation finden,
- das Wrapper-Skript verwenden,
- einen absichtlich fehlschlagenden Test erkennen,
- nach der Korrektur den erfolgreichen Lauf melden,
- Ergebnis- und Logpfad ausgeben.

---

## 13. Abnahmekriterien

Die Umsetzung ist abgeschlossen, wenn sämtliche Kriterien erfüllt sind.

### Test Runner

- [ ] `python tools/unity_test.py --suite fast` funktioniert lokal.
- [ ] `python tools/unity_test.py --suite full` funktioniert lokal.
- [ ] Edit-Mode-Tests können gefiltert werden.
- [ ] Play-Mode-Tests können gefiltert werden.
- [ ] Assembly- und Kategoriefilter funktionieren.
- [ ] Ein fehlschlagender Test erzeugt Exit-Code `1`.
- [ ] Ein Kompilierungs- oder Unity-Startfehler erzeugt Exit-Code `2`.
- [ ] Ein Timeout erzeugt Exit-Code `3`.
- [ ] Null gefundene Tests erzeugen Exit-Code `4`.
- [ ] Jeder Lauf erzeugt XML, Log und JSON-Zusammenfassung.
- [ ] Fehlerausgaben enthalten Testname und konkrete Fehlermeldung.
- [ ] Hängende Unity-Prozesse werden beendet.
- [ ] Parallele Läufe in derselben Arbeitskopie werden verhindert.

### Unity-Struktur

- [ ] Produktionscode liegt in mindestens einer Runtime Assembly.
- [ ] Edit-Mode-Tests liegen in einer Test Assembly.
- [ ] Play-Mode-Tests liegen in einer Test Assembly.
- [ ] Test Assemblies referenzieren Runtime Assemblies.
- [ ] Runtime Assemblies referenzieren keine Test Assemblies.
- [ ] Reine C#-Logik kann überwiegend im Edit Mode getestet werden.

### Agenten

- [ ] Codex findet den Testbefehl über `AGENTS.md`.
- [ ] Claude Code findet den Testbefehl über `CLAUDE.md`.
- [ ] Copilot findet den Testbefehl über die GitHub-Anweisungen.
- [ ] Alle drei verwenden denselben Test-Entrypoint.
- [ ] Kein Agent muss den Installationspfad von Unity selbst erraten.
- [ ] Kein Agent meldet Erfolg bei fehlgeschlagenen Tests.
- [ ] Fehlende Testinfrastruktur wird als unvollständige Validierung gemeldet.

### CI

- [ ] Edit Mode läuft bei jedem Pull Request.
- [ ] Play Mode läuft entsprechend der definierten CI-Stufe.
- [ ] Testfehler blockieren den Merge.
- [ ] Infrastrukturfehler blockieren den Merge.
- [ ] Testartefakte sind nach fehlgeschlagenen Läufen verfügbar.
- [ ] Unity-Lizenzdaten erscheinen nicht in Logs.
- [ ] CI und lokale Agenten verwenden dasselbe Skript.

---

## 14. Definition of Done für spätere Agentenänderungen

Eine durch einen Coding-Agenten implementierte Unity-Änderung gilt nur dann als abgeschlossen, wenn:

1. der Produktionscode implementiert wurde,
2. relevante Tests vorhanden oder ergänzt sind,
3. die vorgeschriebene Testsuite ausgeführt wurde,
4. alle geforderten Tests bestanden haben,
5. keine Tests zur Erreichung eines grünen Ergebnisses abgeschwächt wurden,
6. Testbefehle und Ergebnisse im Abschlussbericht genannt werden,
7. die CI-Prüfungen erfolgreich sind.

Die Agentenanweisung darf niemals ein grünes Ergebnis simulieren. Die technische Wahrheit stammt aus dem Testprozess, dem NUnit-XML und der CI.
