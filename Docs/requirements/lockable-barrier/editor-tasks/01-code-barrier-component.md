# Editor-Anleitung: Barrier-Komponente erstellen

**Feature:** Lockable Barrier (Gesperrter Ausgang)
**Task:** `Docs/requirements/lockable-barrier/01-code-barrier-component.md`

## Übersicht

- Die Skript-Datei `Barrier.cs` wurde unter `Assets/FPS/Scripts/Gameplay/` erstellt
- Die Komponente benötigt einen `Collider` (wird automatisch via `[RequireComponent]` erzwungen)
- Sie verwaltet die Zustände `Locked`/`Unlocked` und steuert die Kollision
- Öffentliche Methode `TryUnlock()` schaltet von `Locked` → `Unlocked` (irreversibel)

## Schritt-für-Schritt

### Schritt 1: Barriere-GameObject erstellen

1. Erstelle ein neues GameObject in der Hierarchie:
   - **Rechtsklick** im Hierarchy → **Create Empty**
   - Nenne es z. B. `ExitBarrier`
2. Positioniere es an der gewünschten Stelle im Level (z. B. vor dem Ausgang)

### Schritt 2: Collider hinzufügen

1. Wähle `ExitBarrier` im Hierarchy aus
2. **Add Component** → Suche nach `Box Collider` (oder einem anderen Collider-Typ)
3. Passe die Größe des Colliders an, sodass er den Durchgang vollständig blockiert
   - `Is Trigger` muss **deaktiviert** bleiben (die Barriere soll physikalisch blockieren)

### Schritt 3: MeshRenderer / visuelles Mesh hinzufügen

1. **Add Component** → `Mesh Filter` (optional) + `Mesh Renderer`
2. Weise ein beliebiges Mesh zu (z. B. `Cube` als Platzhalter)
3. Passe Skalierung und Rotation an, damit die Barriere sichtbar ist

### Schritt 4: Barrier-Komponente hinzufügen

1. **Add Component** → Suche nach `Barrier`
2. Die Komponente wird automatisch hinzugefügt
3. Im Inspector erscheinen folgende Felder:

| Feld                | Beschreibung                                | Empfohlener Wert       |
| ------------------- | ------------------------------------------- | ---------------------- |
| `Initial State`     | Startzustand der Barriere                   | `Locked` (Default)     |
| `Barrier Renderer`  | Referenz auf den MeshRenderer (Drag & Drop) | `ExitBarrier`-Renderer |
| `Locked Material`   | Material im gesperrten Zustand              | _(später zuweisen)_    |
| `Unlocked Material` | Material im offenen Zustand                 | _(später zuweisen)_    |

4. **Wichtig:** Ziehe den `Mesh Renderer` von `ExitBarrier` per Drag & Drop in das Feld `Barrier Renderer`

### Schritt 5: Initialen Zustand konfigurieren

1. Setze `Initial State` im Inspector auf `Locked` (Default)
2. Lasse `Locked Material` und `Unlocked Material` vorerst leer
   - Die Barriere funktioniert auch ohne Materialien – es erscheint eine Warning in der Console

### Schritt 6: Testen (Grundfunktion)

1. Drücke **Play**
2. Die Barriere blockiert den Durchgang (Collider aktiv)
3. Öffne die Console und prüfe: Keine Fehler außer ggf. der Warning wegen fehlender Materialien
4. Beende den Play Mode

## Prüfliste (Checkliste für den Entwickler)

- [ ] `ExitBarrier` (oder ähnlich) existiert im Hierarchy
- [ ] Collider ist vorhanden und **kein Trigger**
- [ ] Collider blockiert den Durchgang vollständig
- [ ] `Barrier`-Komponente ist hinzugefügt
- [ ] `Barrier Renderer` ist im Inspector zugewiesen
- [ ] `Initial State` steht auf `Locked`
- [ ] Im Play Mode blockiert die Barriere den Spieler
- [ ] Keine roten Fehler in der Console
