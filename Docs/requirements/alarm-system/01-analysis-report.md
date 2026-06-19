# Analysebericht: Gegner-KI

**Datum:** 2026-06-19
**Branch:** agents
**Zweck:** Grundlage für Alarm-System-Implementierung (Task 02)

## 1. Übersicht der relevanten Skripte

| Datei                 | Pfad                                | Verantwortlichkeit                                                                                                       | Schlüsselmethoden                                                                                                                                                                                                                                                                                                                                                                                                                          |
| --------------------- | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| EnemyController.cs    | `Assets/FPS/Scripts/AI/`            | Zentraler Hub: vereint Detection, Navigation, Weapons, Health-Events. Hält UnityActions für State-Machine-Kommunikation. | `Start()` (Z.118): Registriert bei EnemyManager, bindet Health.OnDie/OnDamaged, initialisiert DetectionModule + Weapons. `Update()` (Z.180): Ruft DetectionModule.HandleTargetDetection() auf. `OnDamaged()` (Z.272): Reagiert auf Spieler-Schaden, leitet an DetectionModule.OnDamaged() weiter. `TryAtack()` (Z.305): Feuert Waffe, orientiert sie zum Ziel. `OnDie()` (Z.294): Spawnt VFX, unregistriert bei EnemyManager, droppt Loot. |
| DetectionModule.cs    | `Assets/FPS/Scripts/AI/`            | Sichtfeld + Erkennungslogik: Raycast-basierte Sichtprüfung mit Team-Filter über ActorsManager.                           | `HandleTargetDetection()` (Z.48): Iteriert alle Actors, filtert nach Affiliation, prüft Sicht per RaycastAll, setzt KnownDetectedTarget. `OnDetect()` (Z.107): Feuert onDetectedTarget-Event. `OnLostTarget()` (Z.105): Feuert onLostTarget-Event. `OnDamaged()` (Z.109): Überschreibt KnownDetectedTarget mit damageSource (kein Raycast nötig). `OnAttack()` (Z.118): Triggert Attack-Animation.                                         |
| EnemyMobile.cs        | `Assets/FPS/Scripts/AI/`            | State Machine für mobile Gegner: Patrol → Follow → Attack.                                                               | `Start()` (Z.42): Bindet EnemyController-Events, startet in Patrol. `Update()` (Z.61): `UpdateAiStateTransitions()` + `UpdateCurrentAiState()`. `UpdateAiStateTransitions()` (Z.73): Folgt/Attack-Transitionen. `UpdateCurrentAiState()` (Z.93): Verhalten pro State. `OnDetectedTarget()` (Z.126): Patrol → Follow. `OnLostTarget()` (Z.141): Zurück zu Patrol.                                                                           |
| EnemyTurret.cs        | `Assets/FPS/Scripts/AI/`            | State Machine für Geschütztürme: Idle → Attack. Mit Turret-Rotation und Fire-Delay.                                      | `Start()` (Z.48): Bindet Events, startet in Idle. `Update()` (Z.67): `UpdateCurrentAiState()`. `UpdateCurrentAiState()` (Z.76): Attack-Logik mit Fire-Delay. `UpdateTurretAiming()` (Z.101): Slerp-Rotation des Turrets. `OnDetectedTarget()` (Z.122): Idle → Attack, setzt m_TimeStartedDetection. `OnLostTarget()` (Z.140): Attack → Idle.                                                                                               |
| EnemyManager.cs       | `Assets/FPS/Scripts/AI/`            | Lifecycle-Verwaltung: Registrierung, Deregistrierung, EnemyKillEvent-Broadcast.                                          | `Awake()` (Z.13): Initialisiert Enemies-Liste. `RegisterEnemy()` (Z.17): Fügt EnemyController hinzu, erhöht NumberOfEnemiesTotal. `UnregisterEnemy()` (Z.24): Broadcastet EnemyKillEvent, entfernt aus Liste.                                                                                                                                                                                                                              |
| PatrolPath.cs         | `Assets/FPS/Scripts/AI/`            | Wegpunkt-System: Liste von Transform-Nodes, Zuweisung an EnemyController.                                                | `Start()` (Z.15): Weist sich allen EnemiesToAssign zu. `GetDistanceToNode()` (Z.22): Distanz Vector3→Node. `GetPositionOfPathNode()` (Z.32): Node-Position per Index.                                                                                                                                                                                                                                                                      |
| NavigationModule.cs   | `Assets/FPS/Scripts/AI/`            | Überschreibt NavMeshAgent-Werte (MoveSpeed, AngularSpeed, Acceleration) beim Start.                                      | Keine Methoden – reine Datenkomponente. Properties: `MoveSpeed`, `AngularSpeed`, `Acceleration`.                                                                                                                                                                                                                                                                                                                                           |
| Health.cs             | `Assets/FPS/Scripts/Game/Shared/`   | Lebenspunkte + Schadens-/Heilungs-/Todes-Events.                                                                         | `TakeDamage()` (Z.44): Reduziert HP, feuert OnDamaged(damage, damageSource), prüft Tod. `HandleDeath()` (Z.68): Feuert OnDie wenn HP ≤ 0. `Heal()` (Z.31): Erhöht HP.                                                                                                                                                                                                                                                                      |
| Damageable.cs         | `Assets/FPS/Scripts/Game/Shared/`   | Proxy zwischen Projektil und Health: wendet DamageMultiplier an, ruft Health.TakeDamage().                               | `Awake()` (Z.16): Findet Health-Komponente (GetComponent / GetComponentInParent). `InflictDamage()` (Z.24): Multipliziert Schaden, ruft Health.TakeDamage(totalDamage, damageSource).                                                                                                                                                                                                                                                      |
| Actor.cs              | `Assets/FPS/Scripts/Game/`          | Team-Zugehörigkeit (Affiliation int) + AimPoint. Registriert sich selbst bei ActorsManager.                              | `Start()` (Z.16): Fügt sich zu ActorsManager.Actors hinzu. `OnDestroy()` (Z.24): Entfernt sich aus ActorsManager.Actors.                                                                                                                                                                                                                                                                                                                   |
| ActorsManager.cs      | `Assets/FPS/Scripts/Game/Managers/` | Zentrales Register aller Actor-Instanzen. Hält Player-Referenz.                                                          | `Awake()` (Z.13): Initialisiert Actors-Liste. `SetPlayer()` (Z.10): Setzt Player-Referenz.                                                                                                                                                                                                                                                                                                                                                 |
| WeaponController.cs   | `Assets/FPS/Scripts/Game/Shared/`   | Waffensteuerung: Schießen, Munition, Nachladen. Instanziiert ProjectileBase.                                             | `HandleShootInputs()` (Z.282): Input-Processing (Manual/Automatic/Charge). `TryShoot()` (Z.316): Munitions-Check + Delay-Check. `HandleShoot()` (Z.345): Instanziiert Projectile, setzt Owner = controller.Owner.                                                                                                                                                                                                                          |
| ProjectileStandard.cs | `Assets/FPS/Scripts/Gameplay/`      | Projektil-Logik: Physik, Kollision per SphereCast, Schadensanwendung.                                                    | `OnShoot()` (Z.75): Initialisiert Velocity, ignoriert Owner-Collider. `Update()` (Z.107): Bewegung + SphereCast-Kollisionserkennung. `OnHit()` (Z.185): Ruft Damageable.InflictDamage() auf, spawnt VFX/SFX.                                                                                                                                                                                                                               |
| ProjectileBase.cs     | `Assets/FPS/Scripts/Game/Shared/`   | Abstrakte Projektil-Basis. Hält Owner (GameObject) und Muzzle-Properties.                                                | `Shoot()` (Z.14): Setzt Owner = controller.Owner, speichert InitialPosition/Direction/Velocity.                                                                                                                                                                                                                                                                                                                                            |

## 2. Spieler-Erkennung (Detection)

- **Datei(en):** `DetectionModule.cs` + `EnemyController.cs`
- **Methode/Code-Pfad:** `DetectionModule.HandleTargetDetection(Actor actor, Collider[] selfColliders)` – aufgerufen in `EnemyController.Update()` jede Frame
- **Funktionsweise:**
  1. **Distance-Filter:** Für jeden Actor in `ActorsManager.Actors` wird die quadrierte Distanz zwischen `DetectionSourcePoint.position` und `otherActor.transform.position` berechnet. Nur Actor innerhalb `DetectionRange` (Default 20f) werden berücksichtigt.

  2. **Team/Affiliation-Filter:** Nur Actors mit `otherActor.Affiliation != actor.Affiliation` werden als potentielle Ziele betrachtet. Spieler und Gegner haben unterschiedliche Affiliation-Werte.

  3. **Line-of-Sight (Raycast):** Von `DetectionSourcePoint.position` wird ein `Physics.RaycastAll` in Richtung `otherActor.AimPoint.position` geschossen (Distanz = DetectionRange, `QueryTriggerInteraction.Ignore`). Alle Hits werden iteriert, eigene Collider (`selfColliders`) werden ignoriert. Der nächste gültige Hit wird geprüft: `closestValidHit.collider.GetComponentInParent<Actor>()` muss == `otherActor` sein. Nur wenn der Raycast den anderen Actor tatsächlich trifft (kein Wall-Blocking), gilt er als gesehen.

  4. **Erkennung:** Bei erfolgreichem Line-of-Sight: `IsSeeingTarget = true`, `TimeLastSeenTarget = Time.time`, `KnownDetectedTarget = otherActor.AimPoint.gameObject`.

  5. **Events:** `OnDetect()` feuert `onDetectedTarget` (UnityAction) → EnemyMobile.OnDetectedTarget() / EnemyTurret.OnDetectedTarget() reagieren mit State-Wechsel (Patrol→Follow bzw. Idle→Attack).

  6. **Timeout:** Wenn `KnownDetectedTarget` existiert, aber `!IsSeeingTarget` UND `(Time.time - TimeLastSeenTarget) > KnownTargetTimeout` (Default 4s), wird `KnownDetectedTarget = null` gesetzt. Dann feuert `OnLostTarget()`.

- **Code-Auszug (sinngemäß):**
  ```
  HandleTargetDetection(Actor actor, Collider[] selfColliders):
      if KnownDetectedTarget && !IsSeeingTarget && timedOut:
          KnownDetectedTarget = null

      for each otherActor in ActorsManager.Actors:
          if otherActor.Affiliation != actor.Affiliation:
              sqrDistance = (otherActor.pos - DetectionSourcePoint.pos).sqrMagnitude
              if sqrDistance < sqrDetectionRange:
                  raycastHits = Physics.RaycastAll(source → aimPoint, DetectionRange)
                  find closestHit not in selfColliders
                  if closestHit.collider.GetComponentInParent<Actor>() == otherActor:
                      IsSeeingTarget = true
                      KnownDetectedTarget = otherActor.AimPoint.gameObject

      IsTargetInAttackRange = KnownDetectedTarget != null
          && distance <= AttackRange

      if !HadKnownTarget && KnownDetectedTarget: OnDetect()
      if HadKnownTarget && !KnownDetectedTarget: OnLostTarget()
  ```

## 3. Schadenserhalt (Damage Pipeline)

- **Pipeline:** `ProjectileStandard.OnHit()` → `Damageable.InflictDamage()` → `Health.TakeDamage()` → `EnemyController.OnDamaged()` → `DetectionModule.OnDamaged()`

- **Details pro Schritt:**
  1. **ProjectileStandard.OnHit(Vector3 point, Vector3 normal, Collider collider)** (Z.185):
     - Holt `Damageable` via `collider.GetComponent<Damageable>()`
     - Ruft `damageable.InflictDamage(Damage, false, m_ProjectileBase.Owner)` — der `m_ProjectileBase.Owner` ist das `GameObject`, das die Waffe hält (beim Spieler: das Player-GameObject)
     - Bei `AreaOfDamage` stattdessen: `AreaOfDamage.InflictDamageInArea(Damage, point, ...)`

  2. **Damageable.InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)** (Z.24):
     - `totalDamage = damage * DamageMultiplier` (wenn nicht Explosion)
     - Selbstschaden-Reduktion: `totalDamage *= SensibilityToSelfdamage` wenn `Health.gameObject == damageSource`
     - `Health.TakeDamage(totalDamage, damageSource)`

  3. **Health.TakeDamage(float damage, GameObject damageSource)** (Z.44):
     - Wenn `Invincible` → return
     - `CurrentHealth -= damage` (geclampt auf [0, MaxHealth])
     - `OnDamaged?.Invoke(trueDamageAmount, damageSource)` — der damageSource ist das Player-GameObject
     - `HandleDeath()` → wenn `CurrentHealth <= 0`: `OnDie?.Invoke()`

  4. **EnemyController.OnDamaged(float damage, GameObject damageSource)** (Z.272):
     - **Wichtiger Check:** `if (damageSource && !damageSource.GetComponent<EnemyController>())` — nur Schaden von NICHT-Gegnern (d.h. vom Spieler) wird verarbeitet
     - `DetectionModule.OnDamaged(damageSource)` — **setzt KnownDetectedTarget SOFORT auf damageSource (den Spieler), ohne Raycast!**
     - `onDamaged?.Invoke()` → EnemyMobile.OnDamaged() / EnemyTurret.OnDamaged() spielen Hit-Effekte
     - `m_LastTimeDamaged = Time.time` → steuert Flash-on-Hit-Visual
     - Spielt `DamageTick` Sound (nur 1x pro Frame via `m_WasDamagedThisFrame`)

  5. **DetectionModule.OnDamaged(GameObject damageSource)** (Z.109):
     - `TimeLastSeenTarget = Time.time`
     - `KnownDetectedTarget = damageSource` — **der Gegner kennt den Spieler sofort nach Schadenserhalt, auch ohne Sichtlinie!**
     - Triggert `OnDamaged`-Animation

  6. **EnemyController.OnDie()** (Z.294):
     - Spawnt `DeathVfx`, zerstört nach 5s
     - `m_EnemyManager.UnregisterEnemy(this)` → entfernt aus Liste, broadcastet `EnemyKillEvent`
     - Droppt `LootPrefab` (mit `DropRate`-Wahrscheinlichkeit)
     - `Destroy(gameObject, DeathDuration)`

- **Code-Auszug (sinngemäß):**

  ```
  ProjectileStandard.OnHit(collider):
      damageable = collider.GetComponent<Damageable>()
      damageable.InflictDamage(Damage, false, Owner)  // Owner = Player-GameObject

  Damageable.InflictDamage(damage, isExplosion, damageSource):
      totalDamage = damage * DamageMultiplier
      Health.TakeDamage(totalDamage, damageSource)

  Health.TakeDamage(damage, damageSource):
      CurrentHealth -= damage
      OnDamaged?.Invoke(damage, damageSource)
      if CurrentHealth <= 0: OnDie?.Invoke()

  EnemyController.OnDamaged(damage, damageSource):
      if damageSource && !damageSource is EnemyController:
          DetectionModule.OnDamaged(damageSource)  // → KnownDetectedTarget = damageSource!
          onDamaged?.Invoke()
  ```

## 4. Zustandslogik (State Machines)

### 4.1 EnemyMobile

- **States:** `Patrol`, `Follow`, `Attack`

- **Transitionen:**
  - `Patrol → Follow`: `OnDetectedTarget()` wird aufgerufen (wenn DetectionModule ein Ziel erkennt)
  - `Follow → Attack`: `IsSeeingTarget && IsTargetInAttackRange` (Sichtlinie vorhanden UND Distanz ≤ AttackRange)
  - `Attack → Follow`: `!IsTargetInAttackRange` (Ziel außerhalb AttackRange)
  - `Follow → Patrol`: `OnLostTarget()` (Ziel verloren / Timeout)
  - `Attack → Patrol`: `OnLostTarget()` (Ziel verloren / Timeout)

- **Verhalten pro State:**
  - **Patrol:** `UpdatePathDestination()` (inkrementiert Path-Node-Index), `SetNavDestination(GetDestinationOnPath())` (NavMeshAgent folgt Wegpunkten)
  - **Follow:** `SetNavDestination(KnownDetectedTarget.transform.position)` (verfolgt Ziel), `OrientTowards()` + `OrientWeaponsTowards()` (dreht sich zum Ziel)
  - **Attack:** Bewegt sich nur auf Ziel zu, wenn Distanz ≥ `AttackStopDistanceRatio * AttackRange` (Default: 50% der AttackRange), sonst stehenbleiben. `OrientTowards()` + `TryAtack()` (feuert Waffe)

### 4.2 EnemyTurret

- **States:** `Idle`, `Attack`

- **Transitionen:**
  - `Idle → Attack`: `OnDetectedTarget()` (Ziel erkannt). Setzt `m_TimeStartedDetection = Time.time`
  - `Attack → Idle`: `OnLostTarget()` (Ziel verloren). Setzt `m_TimeLostDetection = Time.time`

- **Verhalten pro State:**
  - **Idle:** Keine aktive Logik. Turret rotiert per Animation.
  - **Attack:** Berechnet `directionToTarget` von `TurretAimPoint` zu `KnownDetectedTarget`. `m_PivotAimingRotation` per Slerp (mit `LookAtRotationSharpness`). Sobald `Time.time > m_TimeStartedDetection + DetectionFireDelay`: schärfere Rotation (`AimRotationSharpness`), feuert via `TryAtack()`. `LateUpdate()`: `UpdateTurretAiming()` rotiert `TurretPivot` zur Zielrotation.

## 5. Instanziierung & Lifecycle

- **EnemyManager:**
  - `Awake()`: Initialisiert `Enemies = new List<EnemyController>()`
  - `RegisterEnemy(EnemyController enemy)`: `Enemies.Add(enemy)`, `NumberOfEnemiesTotal++`
  - `UnregisterEnemy(EnemyController enemyKilled)`: Erstellt `EnemyKillEvent` mit `Enemy = enemyKilled.gameObject` und `RemainingEnemyCount = NumberOfEnemiesRemaining - 1`. `EventManager.Broadcast(evt)`. `Enemies.Remove(enemyKilled)`.

- **Spawn-Prozess:**
  - Gegner werden als Prefabs im Unity-Editor in der Szene platziert (kein dynamisches Spawn-Script gefunden)
  - Beim `Start()` jedes `EnemyController`: `m_EnemyManager.RegisterEnemy(this)` — der Gegner registriert sich selbst
  - `PatrolPath.Start()` weist `EnemiesToAssign`-Liste den PatrolPath zu: `enemy.PatrolPath = this`

- **Actor-Registrierung:**
  - `Actor.Start()`: `m_ActorsManager.Actors.Add(this)` — jeder Actor registriert sich beim ActorsManager
  - `Actor.OnDestroy()`: `m_ActorsManager.Actors.Remove(this)` — deregistriert sich

- **Events:**
  - `EnemyKillEvent`: Gefeuert von `EnemyManager.UnregisterEnemy()`. Enthält `GameObject Enemy` und `int RemainingEnemyCount`. Wird von `ObjectiveKillEnemies` abgehört.
  - `onDetectedTarget` (UnityAction): Gefeuert von `DetectionModule.OnDetect()`. Abonniert von `EnemyMobile`, `EnemyTurret`, `CompassMarker`.
  - `onLostTarget` (UnityAction): Gefeuert von `DetectionModule.OnLostTarget()`. Abonniert von `EnemyMobile`, `EnemyTurret`, `CompassMarker`.
  - `onDamaged` (UnityAction): Gefeuert von `EnemyController.OnDamaged()`. Abonniert von `EnemyMobile`.
  - `onAttack` (UnityAction): Gefeuert von `EnemyController.TryAtack()`. Abonniert von `DetectionModule.OnAttack()` (Animation-Trigger) und `EnemyMobile.OnAttack()`.

## 6. Fazit & Hook-Points für Task 02

- **Wo kann ein Alarm-System eingreifen?**
  - **Spieler-Erkennung:** `DetectionModule.OnDetect()` (Z.107) feuert `onDetectedTarget`. Die konkrete Erkennung passiert in `DetectionModule.HandleTargetDetection()` (Z.48), wo `OnDetect()` aufgerufen wird, sobald `!HadKnownTarget && KnownDetectedTarget != null`. Ein Alarm-System kann sich auf `EnemyController.onDetectedTarget` einklinken.

  - **Schadenserhalt:** `EnemyController.OnDamaged()` (Z.272) feuert `onDamaged`. Zusätzlich ruft es `DetectionModule.OnDamaged(damageSource)` (Z.109) auf, was das Ziel sofort bekannt macht. Ein Alarm-System kann sich auf `EnemyController.onDamaged` einklinken.

  - **State-Change:** `EnemyMobile.UpdateAiStateTransitions()` (Z.73) wechselt States. `EnemyMobile.OnDetectedTarget()` (Z.126) macht Patrol→Follow. Ein Alarm-System könnte am State-Wechsel ansetzen.

  - **Tod eines Gegners:** `EnemyManager.UnregisterEnemy()` (Z.24) broadcastet `EnemyKillEvent`. Ein Alarm-System könnte `EventManager.AddListener<EnemyKillEvent>()` nutzen.

- **Empfohlene Integrationspunkte:**
  1. **`EnemyController.onDetectedTarget` (UnityAction)** — Wird gefeuert, wenn EIN Gegner den Spieler das ERSTE MAL sieht (nicht bei jedem Frame). Idealer Hook für "Spieler wurde entdeckt"-Alarm.
  2. **`DetectionModule.OnDetect()` / `HandleTargetDetection()`** — Falls feinere Kontrolle nötig (z.B. Alarm erst nach X Sekunden Sichtkontakt oder ab bestimmter Distanz).
  3. **`DetectionModule.OnDamaged(damageSource)`** — Wird aufgerufen, wenn der Gegner Schaden vom Spieler erhält. Setzt KnownDetectedTarget = damageSource OHNE Raycast. Bedeutet: Ein beschossener Gegner "weiß" sofort, wo der Spieler ist — selbst durch Wände. Hook für "Spieler hat angegriffen"-Alarm.
  4. **`EnemyManager.UnregisterEnemy()` / `EnemyKillEvent`** — Hook für "Gegner wurde eliminiert"-Ereignisse im Alarm-Kontext.
