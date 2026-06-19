# 03-code-unlock-trigger – Freischaltungs-Trigger implementieren

**Status:** offen

## Ziel

Ein System implementieren, das die Freischaltung der Barriere ausloest, wenn eine definierte Bedingung erfuellt ist.

## Kurzbeschreibung

Erstellung einer Freischaltungs-Komponente, die:

- Ueber eine Referenz mit einer oder mehreren Barrier-Instanzen verbunden ist.
- Eine Freischaltbedingung ueberwacht (z. B. "alle Gegner im Level besiegt", "Schalter aktiviert", "Schluessel eingesammelt").
- Bei erfuellter Bedingung die verknuepften Barrieren freischaltet.
- Initiale Umsetzung mit der Bedingung "Alle Gegner besiegt" (Integration mit dem Gegner-Management-System des Projekts).

## Abhaengigkeiten

- **01-code-barrier-component** – Die Barrier-Komponente mit Freischalt-Funktion.
- **02-code-visual-feedback** – Visuelles Feedback sollte vorhanden sein, ist aber nicht zwingend.

## Akzeptanzkriterien

- [ ] Die Freischaltungs-Komponente existiert und kann im Editor konfiguriert werden.
- [ ] Die Komponente haelt eine Referenz auf eine oder mehrere Barrier-Instanzen.
- [ ] Die Bedingung "Alle Gegner besiegt" ist implementiert und loest die Freischaltung aus.
- [ ] Die Freischaltung wird nur einmal ausgeloest (wird nicht wiederholt).
- [ ] Das System arbeitet mit dem bestehenden Gegner-Management des Projekts zusammen.

## Definition of Done

- [ ] Die Komponente kompiliert fehlerfrei.
- [ ] Freischaltung durch "Alle Gegner besiegt" funktioniert im Play Mode.
- [ ] Mehrere Barrieren koennen gleichzeitig freigeschaltet werden.
- [ ] Feature wurde im Play Mode geprueft.
- [ ] Keine bekannten Regressionen (Gegner-KI, Gegner-Management unbeeintraechtigt).
