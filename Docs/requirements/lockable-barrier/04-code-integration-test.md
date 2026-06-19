# 04-code-integration-test – Integration & End-to-End-Test

**Status:** offen

## Ziel

Das vollstaendige Feature "Gesperrter Ausgang" wird im Level integriert und einem End-to-End-Test unterzogen. Alle Akzeptanzkriterien des Features werden verifiziert.

## Kurzbeschreibung

- Platzierung der Barrier-Komponente an einem strategischen Punkt im Level (z. B. vor dem Ausgang).
- Konfiguration der Freischaltungs-Komponente mit der Bedingung "Alle Gegner besiegt".
- Test des vollstaendigen Spielflusses: Start → Barriere blockiert → Gegner besiegen → Barriere oeffnet sich → Durchgang nutzbar.
- Dokumentation des Testergebnisses.

## Abhaengigkeiten

- **01-code-barrier-component** – Basis-Komponente.
- **02-code-visual-feedback** – Visuelles Feedback.
- **03-code-unlock-trigger** – Freischaltungslogik.

## Akzeptanzkriterien

- [ ] Die Barriere ist an einer sinnvollen Stelle im Level platziert.
- [ ] Im initialen Zustand (`locked`) blockiert die Barriere den Spieler.
- [ ] Das visuelle Feedback zeigt klar den `locked`-Zustand an.
- [ ] Nach Besiegen aller Gegner schaltet die Barriere auf `unlocked` um.
- [ ] Das visuelle Feedback zeigt klar den `unlocked`-Zustand an.
- [ ] Der Spieler kann den Durchgang nach der Freischaltung passieren.
- [ ] Der `unlocked`-Zustand bleibt persistent (kein Rueckfall bei erneutem Betreten).
- [ ] Alle Feature-Akzeptanzkriterien aus der README sind erfuellt.

## Definition of Done

- [ ] Vollstaendiger Spielfluss mindestens einmal im Play Mode getestet.
- [ ] Alle Akzeptanzkriterien des Features sind erfuellt.
- [ ] Relevanter Diff wurde gelesen und enthaelt keine ungewollten Aenderungen.
- [ ] Keine bekannten Regressionen.
- [ ] Testprotokoll ist dokumentiert.
