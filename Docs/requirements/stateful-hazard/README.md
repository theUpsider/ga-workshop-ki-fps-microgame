---
req-id: SWR-400
status: draft
trace: optional
test: optional
title: Epic: Zustandsbasierte Falle oder Gefahrenbereich
---

# Epic: Zustandsbasierte Falle oder Gefahrenbereich

**GitHub-Epic:** [#14 – Feature 3: Zustandsbasierte Falle oder Gefahrenbereich](https://github.com/theUpsider/ga-workshop-ki-fps-microgame/issues/14)

## Ziel

Eine Gefahr erzeugt spielerischen Druck, hat klare Zustände und kann durch Aufmerksamkeit, Bewegung oder Timing bewältigt werden.

## Feature-Beschreibung

Im Level existiert mindestens eine Falle oder ein Gefahrenbereich. Die Gefahr verursacht nicht einfach permanent einen Effekt, sondern besitzt einen nachvollziehbaren Ablauf mit klar unterscheidbaren Zuständen. Ein geeigneter Ablauf besteht beispielsweise aus einer inaktiven Phase, einer Warnphase, einer aktiven Phase und einem Cooldown.

Vor oder während der Aktivierung erhält der Spieler sichtbares oder hörbares Feedback. In der aktiven Phase erzeugt die Gefahr einen konkreten Gameplay-Effekt, beispielsweise Schaden, Pushback, Teleport, Slow oder Zeitdruck. Dauer, Reichweite und Zustandswechsel sind so abgestimmt, dass der Spieler die Gefahr erkennen und durch Aufmerksamkeit, Bewegung oder Timing realistisch vermeiden oder bewältigen kann.

Die Lösung soll mit den vorhandenen Gameplay-, Schadens- und Feedbacksystemen arbeiten und für weitere Fallen- oder Gefahrentypen wiederverwendbar sein, soweit dies ohne unnötige Abstraktion möglich ist.

## Umfang

Dieses Epic umfasst:

- mindestens eine im Level platzierte Falle oder einen Gefahrenbereich,
- ein nachvollziehbares Zustandsmodell mit mindestens zwei klar unterscheidbaren Zuständen,
- zeit- oder ereignisgesteuerte Zustandsübergänge,
- sichtbares oder hörbares Warn- und Aktivfeedback,
- mindestens einen konkreten Gameplay-Effekt,
- eine faire Reaktions- oder Ausweichmöglichkeit,
- ein definiertes Verhalten nach der aktiven Phase, beispielsweise Cooldown, Reset oder dauerhafte Deaktivierung.

Nicht Bestandteil dieses Epics sind ein allgemeiner visueller Fallen-Editor, prozedurale Gefahrengenerierung sowie eine persistente Speicherung des Fallenzustands über Level- oder Spielstarts hinweg.

## Funktionale Anforderungen

### Zustandsmodell

- Die Gefahr besitzt mindestens zwei klar unterscheidbare Zustände, darunter eine nicht aktive und eine aktive Phase.
- Für eine zeitgesteuerte Gefahr ist eine Warnphase vor der aktiven Phase vorgesehen, sofern der Spieler die Aktivierung nicht bereits eindeutig durch ein eigenes Ereignis verursacht.
- Jeder Zustandswechsel erfolgt deterministisch anhand eines definierten Timers oder Ereignisses.
- Nach der aktiven Phase wechselt die Gefahr in einen definierten Folgezustand, beispielsweise Cooldown, inaktiv oder dauerhaft deaktiviert.
- Ein wiederholbarer Ablauf setzt Timer und temporäre Effekte korrekt zurück.

### Feedback und Lesbarkeit

- Die Warnphase wird durch sichtbares oder hörbares Feedback kommuniziert.
- Die aktive Phase ist eindeutig von der Warnphase und der inaktiven Phase unterscheidbar.
- Feedback endet oder ändert sich passend zum aktuellen Zustand.
- Der aktive Wirkungsbereich ist für den Spieler ausreichend verständlich oder durch Levelgestaltung, Effekt oder Platzierung nachvollziehbar.

### Gameplay-Effekt

- Während der aktiven Phase erzeugt die Gefahr mindestens einen konkreten Gameplay-Effekt: Schaden, Pushback, Teleport, Slow oder Zeitdruck.
- Außerhalb der aktiven Phase wird der Gameplay-Effekt nicht angewendet.
- Der Effekt betrifft nur gültige Ziele im vorgesehenen Wirkungsbereich.
- Wiederholte Effekte, beispielsweise periodischer Schaden, verwenden ein definiertes und konfigurierbares Intervall statt unkontrolliert pro Frame ausgelöst zu werden.
- Temporäre Zustände wie Slow werden nach Ablauf oder Verlassen des Wirkungsbereichs zuverlässig aufgehoben.

### Fairness und Bewältigung

- Der Spieler erhält vor dem unvermeidbaren Eintritt des Effekts eine realistische Reaktionsmöglichkeit.
- Warnzeit, Aktivdauer, Cooldown und Effektstärke sind im Unity Inspector konfigurierbar, soweit sie für den gewählten Gefahrentyp relevant sind.
- Die Gefahr kann durch Aufmerksamkeit, Bewegung oder Timing vermieden oder bewältigt werden.
- Die Platzierung im Level erzeugt keine unvermeidbare Schadens- oder Kontrollverlustschleife.

### Robustheit

- Das Deaktivieren oder Zerstören der Gefahr beendet laufende temporäre Effekte und Feedbackzustände kontrolliert.
- Fehlende optionale Audio- oder Effektreferenzen führen nicht zu unbehandelten Exceptions.
- Mehrere Ziel-Collider desselben Spielers verursachen keine unbeabsichtigte Vervielfachung eines einzelnen Effekts.

## Nicht-funktionale Anforderungen

- Die Implementierung integriert sich in bestehende Schadens-, Bewegungs- und Feedbacksysteme und vermeidet unnötige doppelte Logik.
- Zustände und zentrale Balancing-Werte sind im Inspector verständlich benannt und dokumentiert.
- Die zustandsabhängige Kernlogik ist unabhängig vom konkreten Level testbar, soweit technisch sinnvoll.
- Die Gefahr erzeugt keine unnötigen Allokationen oder teuren globalen Suchen in jedem Frame.

## Akzeptanzkriterien

- [ ] Die Falle oder Gefahr hat mindestens zwei klar unterscheidbare Zustände.
- [ ] Vor oder beim Auslösen gibt es sichtbares oder hörbares Feedback.
- [ ] Die aktive Phase ist eindeutig von der Warn- und der inaktiven Phase unterscheidbar.
- [ ] Die Gefahr hat einen konkreten Gameplay-Effekt, beispielsweise Schaden, Pushback, Teleport, Slow oder Zeitdruck.
- [ ] Der Gameplay-Effekt wird nur während der vorgesehenen aktiven Phase angewendet.
- [ ] Der Spieler kann die Gefahr durch Aufmerksamkeit, Bewegung oder Timing erkennen und vermeiden oder bewältigen.
- [ ] Nach der aktiven Phase erreicht die Gefahr zuverlässig ihren definierten Folge- oder Ausgangszustand.
- [ ] Konfigurierbare Zeiten und Effektwerte können im Inspector angepasst werden.

## Verifikation

- Automatisierte Edit-Mode-Tests prüfen Zustände, erlaubte Übergänge, Timer und die Begrenzung des Gameplay-Effekts auf die aktive Phase, soweit technisch sinnvoll.
- Ein manueller Play-Mode-Test prüft Warnung, Aktivierung, Gameplay-Effekt, Ausweichmöglichkeit und Reset oder Cooldown im vollständigen Spielfluss.
- Der Test umfasst mindestens einen Fall, in dem der Spieler rechtzeitig ausweicht, und einen Fall, in dem der aktive Effekt ausgelöst wird.
- Bei wiederholbaren Gefahren wird mindestens ein vollständiger zweiter Zyklus geprüft.
- Das Testergebnis und verbleibende manuelle Prüfungen werden nachvollziehbar dokumentiert.

## Definition of Done

- [ ] Alle Akzeptanzkriterien sind erfüllt oder verbleibende Einschränkungen sind ausdrücklich dokumentiert.
- [ ] Das Feature wurde im relevanten Level oder in einer geeigneten Testszene getestet.
- [ ] Relevante automatisierte Tests wurden ausgeführt und bestanden.
- [ ] Balancing und realistische Reaktionszeit wurden im Play Mode geprüft.
- [ ] Der finale Diff wurde auf unbeabsichtigte Änderungen und Regressionen geprüft.
- [ ] Erforderliche Unity-Editor-, Inspector- und Level-Setup-Schritte sind dokumentiert.
- [ ] Die Umsetzung wurde in einem nachvollziehbaren Git-Commit festgehalten.

> Hinweis: Das letzte Akzeptanzkriterium der ursprünglichen Vorlage war unvollständig ("Der Spieler kann die Gefahr durch …"). Es wurde passend zu Ziel und Beschreibung als Erkennen und Vermeiden oder Bewältigen durch Aufmerksamkeit, Bewegung oder Timing vervollständigt.
