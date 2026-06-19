# Editor-Anleitung: Freischaltungs-Trigger konfigurieren

**Feature:** Lockable Barrier (Gesperrter Ausgang)
**Task:** `Docs/requirements/lockable-barrier/03-code-unlock-trigger.md`

## Übersicht

- Die Skript-Datei `UnlockTriggerBarrier.cs` wurde unter `Assets/FPS/Scripts/Gameplay/` erstellt
- Die Komponente überwacht die Bedingung "Alle Gegner besiegt" via `EnemyKillEvent`
- Sobald `RemainingEnemyCount == 0` ist, werden alle verknüpften Barrieren entsperrt
- Kann beliebig viele Barrier-Instanzen gleichzeitig freischalten (über `List<Barrier>`)
- Löst nur **einmal** aus (`m_HasTriggered`-Guard)

## Schritt-für-Schritt

### Schritt 1: UnlockTriggerBarrier-Komponente hinzufügen

1. Wähle ein GameObject im Level aus, das die Freischaltung steuern soll
   - **Empfehlung:** Erstelle ein eigenes Empty-GameObject
   - **Rechtsklick** im Hierarchy → **Create Empty**
   - Nenne es `UnlockManager` (oder `BarrierTrigger`)
2. Wähle `UnlockManager` aus
3. **Add Component** → Suche nach `Unlock Trigger Barrier`

### Schritt 2: Bedingung konfigurieren

1. Im Inspector → `Unlock Trigger Barrier`-Komponente
2. Das Feld `Condition` ist standardmäßig auf `All Enemies Killed` gesetzt
   - Bei Bedarf kann hier später eine andere Bedingung ausgewählt werden (aktuell nur dieser Wert)

### Schritt 3: Barrier-Referenzen verdrahten

1. Im Inspector → `Barriers` (Liste)
2. Setze die **Größe (Size)** auf die Anzahl der Barrieren im Level
   - Beispiel: `Size = 1` für eine einzelne Barriere
3. Ziehe das Barriere-GameObject (z. B. `ExitBarrier`) per Drag & Drop in jedes Feld `Element 0`, `Element 1`, ...

**Mehrere Barrieren gleichzeitig entsperren:**

| Element   | GameObject      | Effekt                          |
| --------- | --------------- | ------------------------------- |
| Element 0 | `ExitBarrier`   | Wird bei Freischaltung geöffnet |
| Element 1 | `SecondBarrier` | Wird gleichzeitig geöffnet      |
| ...       | ...             | ...                             |

### Schritt 4: Zusammenspiel testen

1. Stelle sicher, dass im Level Gegner vorhanden sind (z. B. `Enemy`-Prefabs)
2. Drücke **Play**
3. **Beobachte:**
   - Barriere ist geschlossen (locked / rot)
   - Notification "Ausgang gesperrt" wird angezeigt
4. Besiege **alle** Gegner im Level
5. **Beobachte:**
   - Barriere öffnet sich (Material wechselt zu Unlocked-Material)
   - Notification "Ausgang offen" wird angezeigt
   - Console-Log: `[UnlockTriggerBarrier] Unlocking barrier 'ExitBarrier'.`
   - Console-Log: `[Barrier] ExitBarrier unlocked.`
6. Gehe durch die Barriere – der Weg ist frei

### Schritt 5: Persistenz prüfen

1. Nach der Freischaltung: Verlasse den Bereich und komme zurück
2. Die Barriere muss weiterhin geöffnet sein (kein Zurückfallen in `Locked`)
3. Erneutes Besiegen von Gegnern löst keine erneute Freischaltung aus

## Prüfliste (Checkliste für den Entwickler)

- [ ] `UnlockManager` (o.ä.) existiert im Hierarchy
- [ ] `Unlock Trigger Barrier`-Komponente ist hinzugefügt
- [ ] `Condition` ist auf `All Enemies Killed` gesetzt
- [ ] `Barriers`-Liste ist befüllt: Größe ≥ 1, Referenzen zeigen auf Barriere-GameObjects
- [ ] Im Play Mode: Gegner besiegen → Barriere schaltet frei
- [ ] Console zeigt `[UnlockTriggerBarrier]` und `[Barrier] unlocked` Logs
- [ ] Kein Zurückfallen in `Locked` nach Freischaltung
- [ ] Nur einmalige Auslösung (zweites Durchlaufen löst nichts aus)
- [ ] Keine roten Fehler in der Console
