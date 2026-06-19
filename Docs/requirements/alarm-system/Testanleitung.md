# Testanleitung: Alarm-System

**Datum:** 2026-06-19
**Feature:** Alarm-System für Gegner
**Zweck:** Manuelle Play-Test-Anleitung zur End-to-End-Verifikation

## Vorbereitung

### 1. Gegner-Platzierung im Level

Öffne die FPS-Szene im Unity-Editor und platziere mindestens 4-6 Gegner wie folgt:

- **Cluster A (3 Gegner):** Drei `EnemyMobile`-Instanzen innerhalb von 10-15m Abstand zueinander platzieren
- **Isolierter Gegner B:** EIN `EnemyMobile` mindestens 50m vom Cluster A entfernt platzieren
- **Optional:** 1-2 weitere Gegner für Tests mit variabler Reichweite

### 2. AlarmModule-Komponente hinzufügen

- Wähle JEDEN Gegner im Level aus
- Füge per "Add Component" → `AlarmModule` hinzu
- Der `Alarm Radius`-Default (30f) ist für die ersten Tests ausreichend

### 3. Konsole öffnen

- Window → General → Console
- Auf "Collapse" deaktivieren (damit jedes Log einzeln sichtbar ist)
- Filter auf "AlarmModule" setzen (optional)

---

## Testszenarien

### Szenario 1: Sicht-Erkennung im Cluster

**Vorbereitung:** Alle Gegner haben AlarmModule, AlarmRadius = 30. Cluster A ist nah beieinander.

**Durchführung:**
1. Starte Play-Modus
2. Bewege den Spieler in das Sichtfeld EINES Gegners aus Cluster A
3. Beobachte die Konsole

**Erwartetes Ergebnis:**
- `[AlarmModule] Alarm triggered on GegnerA1. Alarm radius: 30`
- `[AlarmModule] Propagated alarm to GegnerA2`
- `[AlarmModule] Propagated alarm to GegnerA3`
- Gegner A2 und A3 beginnen, den Spieler zu verfolgen
- Isolierter Gegner B zeigt KEIN Alarm-Log

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 2: Schadens-Alarm

**Vorbereitung:** Alle Gegner haben AlarmModule, AlarmRadius = 30. Cluster A ist nah beieinander.

**Durchführung:**
1. Starte Play-Modus
2. Schieße AUF EINEN Gegner im Cluster A (ohne dass er dich vorher sieht)
3. Beobachte die Konsole

**Erwartetes Ergebnis:**
- `[AlarmModule] Alarm triggered on GegnerA1. Alarm radius: 30` (vom getroffenen Gegner)
- `[AlarmModule] Propagated alarm to GegnerA2`
- `[AlarmModule] Propagated alarm to GegnerA3`
- Alle drei Gegner verfolgen den Spieler
- Isolierter Gegner B zeigt KEIN Alarm-Log

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 3: Distanz-Ausschluss (isolierter Gegner)

**Vorbereitung:** Cluster A (3 Gegner nah), Gegner B isoliert (>50m entfernt). Alle haben AlarmRadius = 30.

**Durchführung:**
1. Starte Play-Modus
2. Lass dich von einem Gegner aus Cluster A entdecken
3. Beobachte Gegner B

**Erwartetes Ergebnis:**
- In der Konsole erscheinen KEINE "Propagated alarm to GegnerB"-Logs
- Gegner B patrouilliert weiterhin normal (wurde NICHT alarmiert)

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 4: Selbst-Ausschluss

**Vorbereitung:** Mindestens 2 Gegner mit AlarmModule.

**Durchführung:**
1. Starte Play-Modus
2. Lass dich von Gegner A entdecken
3. Prüfe die "Propagated alarm"-Logs

**Erwartetes Ergebnis:**
- Kein "Propagated alarm to GegnerA" (Selbst-Alarmierung ausgeschlossen)
- Nur andere Gegner werden alarmiert

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 5: Toten-Ausschluss

**Vorbereitung:** 3 Gegner im Cluster, alle mit AlarmModule.

**Durchführung:**
1. Starte Play-Modus
2. Töte EINEN Gegner im Cluster
3. Lass dich von einem ÜBERLEBENDEN Gegner entdecken
4. Prüfe die "Propagated alarm"-Logs

**Erwartetes Ergebnis:**
- Kein "Propagated alarm to [getöteter Gegner]"-Log
- Der überlebende dritte Gegner wird alarmiert
- Nur LEBENDE Gegner werden alarmiert

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 6: Radius-Variation im Inspector

**Vorbereitung:** 2 Gegner (A und B) in ca. 15m Abstand. AlarmModule auf beiden.

**Durchführung:**
1. Setze AlarmRadius auf **5m** bei Gegner A
2. Starte Play-Modus, lass dich von Gegner A entdecken
3. Beobachte: Gegner B wird **NICHT** alarmiert (15m > 5m)
4. Stoppe Play-Modus
5. Setze AlarmRadius auf **100m** bei Gegner A
6. Starte Play-Modus, lass dich von Gegner A entdecken
7. Beobachte: Gegner B wird alarmiert (15m < 100m)

**Erwartetes Ergebnis:**
- Bei 5m: Nur Gegner A alarmiert
- Bei 100m: Gegner A + B alarmiert

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 7: Regression – Normale KI unbeeinträchtigt

**Vorbereitung:** Isolierter Gegner mit AlarmModule, aber außerhalb jedes Alarm-Radius.

**Durchführung:**
1. Starte Play-Modus
2. Nähere dich dem isolierten Gegner (ohne in sein Sichtfeld zu kommen)
3. Beobachte sein Patrouillen-Verhalten
4. Tritt in sein Sichtfeld
5. Beobachte: Verfolgung beginnt normal
6. Töte den Gegner

**Erwartetes Ergebnis:**
- Gegner patrouilliert normal (kein vorzeitiger Alarm)
- Verfolgung und Angriff funktionieren wie gewohnt
- Tod und Loot-Drop funktionieren normal
- Der Gegner hat KEIN Alarm-System-Fehlerverhalten

**Bestanden:** [ ] Ja / [ ] Nein

---

### Szenario 8: Konsole sauber

**Vorbereitung:** Alle oben genannten Szenarien durchlaufen.

**Durchführung:**
1. Konsolen-Filter zurücksetzen (kein Filter)
2. Alle Log-Einträge während der Play-Tests prüfen

**Erwartetes Ergebnis:**
- Keine Errors (rote Meldungen)
- Keine Warnings (gelbe Meldungen) die vom Alarm-System stammen
- Keine NullReferenceExceptions
- Keine MissingReferenceExceptions

**Bestanden:** [ ] Ja / [ ] Nein

---

## Zusammenfassung

| Szenario | Beschreibung | Bestanden? |
|----------|-------------|------------|
| 1 | Sicht-Erkennung im Cluster | [ ] |
| 2 | Schadens-Alarm | [ ] |
| 3 | Distanz-Ausschluss | [ ] |
| 4 | Selbst-Ausschluss | [ ] |
| 5 | Toten-Ausschluss | [ ] |
| 6 | Radius-Variation | [ ] |
| 7 | Regression | [ ] |
| 8 | Konsole sauber | [ ] |

**Gesamtergebnis:** Alle 8 Szenarien müssen BESTANDEN sein für Feature-Release.
