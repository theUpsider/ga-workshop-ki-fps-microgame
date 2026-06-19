# Gesperrter Ausgang / Lockable Barrier

## Feature-Beschreibung

Im Unity FPS Microgame soll ein gesperrter Ausgang (Lockable Barrier) implementiert werden. Eine Barriere, Tuer, ein Kraftfeld oder eine Bruecke blockiert zu Beginn den Fortschritt des Spielers und verhindert den Abschluss des Levels. Erst durch eine definierte Bedingung (z. B. alle Gegner besiegt, ein Schalter betaetigt oder ein Schluessel eingesammelt) wird die Barriere freigeschaltet. Nach der Freischaltung soll der Spieler den Bereich verlaesslich erreichen und den Durchgang nutzen koennen.

Die Loesung soll mit den vorhandenen Systemen des Levels arbeiten und keine unnoetige doppelte Logik erzeugen.

## Akzeptanzkriterien

- [ ] Die Barriere besitzt mindestens die Zustaende `locked` und `unlocked`.
- [ ] Im `locked`-Zustand verhindert die Barriere den Fortschritt nachvollziehbar (z. B. durch Kollision, visuelles Feedback).
- [ ] Ein anderes System kann die Freischaltung ausloesen (z. B. Gegner-Management, Trigger-Zone, Game-Manager).
- [ ] Die Freischaltung wird dem Spieler klar kommuniziert (visuell und/oder auditiv).
- [ ] Nach der Freischaltung ist der Ausgang oder Durchgang verlaesslich nutzbar (Kollision deaktiviert, Passage frei).
- [ ] Der `unlocked`-Zustand ist persistent und faellt nicht zurueck.
- [ ] Die Barriere ist im Editor konfigurierbar (Zustand, Freischaltbedingung, visuelle Assets).

## Subtasks

| #   | Task                                                | Beschreibung                                                    |
| --- | --------------------------------------------------- | --------------------------------------------------------------- |
| 01  | [Barrier-Komponente](01-code-barrier-component.md)  | Neue Komponente mit locked/unlocked-Zustand und Kollisionslogik |
| 02  | [Visuelles Feedback](02-code-visual-feedback.md)    | Visuelle Darstellung der Zustaende locked/unlocked              |
| 03  | [Freischaltungs-Trigger](03-code-unlock-trigger.md) | System zum Ausloesen der Freischaltung durch Bedingungen        |
| 04  | [Integration & Test](04-code-integration-test.md)   | End-to-End-Verifikation im Level                                |
