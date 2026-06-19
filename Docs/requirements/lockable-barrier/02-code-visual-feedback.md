# 02-code-visual-feedback – Visuelles Feedback fuer Zustaende

**Status:** erledigt

## Ziel

Die Barriere soll dem Spieler ihren aktuellen Zustand (`locked`/`unlocked`) klar kommunizieren – sowohl statisch als auch beim Zustandswechsel.

## Kurzbeschreibung

Erweiterung der Barrier-Komponente um visuelles Feedback:

- Unterschiedliche Materialien/Farben fuer `locked` (z. B. rot/undurchsichtig) und `unlocked` (z. B. gruen/transparent oder deaktiviert).
- Ein Event-Hook fuer den Zustandswechsel, sodass Designer eigene Effekte (Partikel, Sound, Animation) im Inspector verdrahten koennen.
- Optional: Eine UI-Text-Anzeige ("Ausgang gesperrt" → "Ausgang offen") in der Naehe der Barriere.

## Abhaengigkeiten

- **01-code-barrier-component** – Die Basis-Komponente mit Zustandslogik muss vorhanden sein.

## Akzeptanzkriterien

- [x] Die Barriere zeigt im `locked`-Zustand ein klar erkennbares visuelles Signal (z. B. rotes Material).
- [x] Die Barriere zeigt im `unlocked`-Zustand ein klar anderes visuelles Signal (z. B. gruenes Material oder deaktiviertes Mesh).
- [x] Der Zustandswechsel loest ein im Inspector konfigurierbares Event aus.
- [x] Visuelle Assets (Materialien, Farben) sind im Inspector austauschbar.
- [x] Die Aenderungen sind im Play Mode sofort sichtbar.

## Definition of Done

- [x] Visuelles Feedback fuer beide Zustaende funktioniert.
- [x] Event-Hook ist vorhanden und im Inspector verdrahtbar.
- [x] Feature wurde im Play Mode geprueft (Zustandswechsel sichtbar).
- [x] Keine bekannten Regressionen.
