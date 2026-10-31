# War after Death — Setup-Anleitung für die neuen Systeme

Alle Systeme sind **datengetrieben**: Du arbeitest im Inspector und mit
ScriptableObject-Assets. Für Abstimmung (Werte, Sounds, Animationen, Loot)
musst du **keinen Code schreiben**.

Erstelle Assets jeweils über das Rechtsklick-Menü:
`Create > WAD > ...`

---

## Übersicht der neuen Ordner

| Ordner | Inhalt |
|---|---|
| `Assets/Scripts/WAD/Vehicles/` | Jet: Fliegen, Reparatur, Waffen, Spawner, Cutscene-Hooks |
| `Assets/Scripts/WAD/CameraFx/` | Kamera-Animation + "Found Footage"-Optik |
| `Assets/Scripts/WAD/Cutscenes/` | Cutscene-Abläufe (Intro/Outro) + Untertitel |
| `Assets/Scripts/WAD/Feedback/` | Aktions-Feedback (Sound/Animation/VFX pro Aktion) |
| `Assets/Scripts/WAD/Movement/FirstPersonArmsRig.cs` | Hände/Arme in erster Person |
| `Assets/Data/Magazines/MagazineCatalog.asset` | Katalog aller Magazin-Typen |

---

## 1) Jet — reparierbar, zufällig platziert, frei fliegbar

### Aufbau des Jet-Prefabs

```
Jet (Root)
├─ ActionFeedbackPlayer      ← alle Sounds/Animationen (Wichtig!)
├─ JetController             ← Fliegen + Ein-/Aussteigen
├─ JetRepairSystem           ← Reparatur mit zufälligen Teilen
├─ JetWeaponSystem           ← MG + Raketen
├─ JetCutsceneTrigger        ← optional: Cutscene beim ersten Flug
├─ Collider                  ← für die Blick-Interaktion (F)
├─ VisualRoot (Kind)         ← das sichtbare Modell (wird beim Rollen gekippt)
│   ├─ CockpitCameraPoint    ← wo die Kamera sitzt
│   ├─ ThrusterPoint         ← Düse (für VFX)
│   └─ MuzzlePoints…         ← Mündungspunkte der Waffen
```

**Wichtig:** `ActionFeedbackPlayer` auf dasselbe Objekt legen. Dort trägst du
die Geräusche ein (Triebwerk, MG, Rakete, Kollision).

### Reparatur mit zufälligen Teilen

Auf `JetRepairSystem`:

| Feld | Bedeutung |
|---|---|
| **Possible Parts** | Pool aller möglichen Ersatzteile (Item + Menge) |
| **Distinct Parts Range** | Wie viele *verschiedene* Teile dieser Jet verlangt (z. B. 2–3) |
| **Quantity Per Part Range** | Mengenbereich pro Teil (z. B. 1–3) |
| **Random Seed** | 0 = bei jedem Start neu. Der `JetSpawner` setzt pro Jet einen eigenen Seed, damit sich die Jets unterscheiden. |

Einfach mehrere `ItemSO`/`EntitySO` in **Possible Parts** ziehen — jeder Jet
würfelt daraus seine eigene Einkaufsliste.

### Zufälliges Platzieren

Ein leeres Objekt in die Level-Szene legen und `JetSpawner` hinzufügen:

| Feld | Bedeutung |
|---|---|
| **Jet Prefabs** | Ein oder mehrere Jet-Prefabs (zufällige Auswahl) |
| **Spawn Count Range** | Wie viele Jets erscheinen (z. B. 1–2) |
| **Spawn Area Center / Radius** | Suchbereich in der Welt |
| **Min Distance From Player** | Jets sollen gesucht werden (z. B. 80 m) |
| **Min Distance Between Jets** | Abstand der Jets zueinander |
| **Max Ground Slope** | Steilheit, auf der ein Jet noch stehen darf |
| **Ground Layers** | Auf welchen Layern der Boden gesucht wird |

### Steuerung (im Inspector änderbar)

| Taste | Standard | Aktion |
|---|---|---|
| W / S | Schub erhöhen / verringern | |
| ↑ / ↓ | Nicken | |
| A / D | Gieren | |
| Q / E | Rollen | |
| Leertaste / Strg | Steigen / Sinken (VTOL) | |
| Shift | Nachbrenner | |
| Maus | Nicken + Gieren (Alternative) | |
| F | Ein-/Aussteigen | |
| LMB | MG | |
| RMB | Rakete | |
| R | Nachladen | |

### Flugverhalten

Alles im Asset `JetFlightProfile` (Create > WAD > Jet Flight Profile)
einstellbar: Schub, Höchstgeschwindigkeit, Gier-/Nick-/Rollrate, Trägheit,
Auftrieb, VTOL, Startsequenz-Dauer, Kamera-Offset, Treibstoffverbrauch.

Treibstoff kommt aus `RunStateManager.jetFuel` — Belohnung dafür gibt es über
Taskmaster-Aufgaben (`fuelReward`) oder `RunStateManager.AddFuel()`.

### Waffensystem

Auf `JetWeaponSystem` gibt es eine Liste **Weapon Slots**. Pro Slot:

* `Is Rocket` — MG oder Rakete
* `Ammo Type` — liefert Schaden, Tracer, Einschlag-VFX
* `Muzzle Points` / `Muzzle Container` — Mündungen (mehrere = abwechselnd)
* `Fire Action` / `Reload Action` / `Empty Action` — welche
  `ActionFeedbackPlayer`-Aktion beim Feuern/Nachladen/leerem Magazin läuft
* `Rocket Fire Action` — eigene Aktion für Raketen (Standard `JetRocketFire`)
* `Rocket Prefab` bei Raketen (`JetRocket` wird automatisch ergänzt)

### Jet-Sounds und -Animationen (Fertige Slots)

Der Jet nutzt jetzt eigene Aktionen im `ActionFeedbackPlayer`. Einfach im
Inspector belegen — kein Code:

| Aktion | Wann |
|---|---|
| `JetEngineStart` | Triebwerke fahren hoch |
| `JetEngineLoop` | **Dauerschleife** des Triebwerks (Drehzahl steuert Lautstärke + Tonhöhe automatisch) |
| `JetEngineShutdown` | Triebwerke fahren herunter |
| `JetAfterburner` | Nachbrenner zündet |
| `JetTakeoff` | Abheben |
| `JetLanding` | Aufsetzen |
| `JetCrash` | Aufprall |
| `JetRocketFire` | Raketenabschuss |
| `JetRepairProgress` | Ersatzteil abgeliefert |
| `JetRepairComplete` | Reparatur fertig |
| `JetRepairDenied` | Teile fehlen |
| `JetGearDeploy` / `JetGearRetract` | Fahrwerk (für spätere Animation) |

**Triebwerksschleife einrichten:** Bei `JetEngineLoop` einen Clip eintragen.
Der `ActionFeedbackPlayer` legt dafür automatisch eine zweite AudioSource an
(`Loop Source`). Beim Start wird sie eingeblendet, die Drehzahl regelt
Lautstärke und Tonhöhe. Über `Loop Fade Seconds` einstellbar.

Für VFX (Triebwerksflamme) bei denselben Aktionen einfach
`Effect Prefab` belegen — er wird an `Thruster Point` erzeugt.

---

## 2) Taskmaster — Cutscene, Dialogfenster, Aufgabenliste

### Aufbau

Auf dem NPC: `TaskmasterController` (+ Collider).
Im Canvas: `TaskmasterUI` (+ `QuestListEntryUI` als Zeilen-Prefab).

### Neue Felder auf `TaskmasterController`

| Feld | Bedeutung |
|---|---|
| **Available Quests** | Alle Aufgaben dieses Taskmasters |
| **First Meeting Cutscene** | Wird beim **ersten** Ansprechen abgespielt |
| **Dialogue Delay After Cutscene** | Pause, bevor das Fenster aufgeht |
| **Show Locked Quests** | Gesperrte Aufgaben ausgegraut anzeigen |

### `TaskmasterUI` verdrahten

| Feld | Bedeutung |
|---|---|
| **Panel Root** | Das Dialogfenster |
| **Close Button** | **Neu:** Button, der das Fenster schließt |
| **Close Key** | Alternativ Escape |
| **Quest List Content** | Parent *innerhalb* der ScrollView (mit `VerticalLayoutGroup` + `ContentSizeFitter`) |
| **Quest Entry Prefab** | Prefab mit `QuestListEntryUI` |
| **Turn In Button / Label** | Abgeben |
| **Dialogue Text** | Zeile, in der der Taskmaster spricht |

**Scrollfenster einrichten:** `Scroll View` ins Panel → deren `Content`-Objekt
in **Quest List Content** ziehen → `VerticalLayoutGroup` + `ContentSizeFitter`
(Vertical = Preferred Size) auf das Content-Objekt.

### Aufgaben-Assets

`TaskmasterQuestSO` hat jetzt zusätzlich:

* **Quest Id** — **Pflicht und eindeutig!** Ohne ID verwechseln sich Aufgaben.
  (Beim Start wird das im Log gemeldet.)
* **Dialogzeilen** — `offerLine`, `inProgressLine`, `completionLine`
* **Zusatz-Anforderungen** — `requiredKills` (nutzt den neuen Kill-Zähler),
  `requiredFlag`
* **Voraussetzungen** — `prerequisiteQuests`, `requiredLevelIndex`
* **repeatable** — mehrfach abgebbar

> Die alte `Quest_DeathResistance` hat eine **leere questId** — bitte nachtragen.

---

## 3) Kamera-Animationen + Hände

### Kamera-Animation

1. Asset anlegen: `Create > WAD > Camera Animation Profile`
2. Auf der Kamera (Main Camera) Objekt `CameraAnimationController` hinzufügen
3. Profil zuweisen

Schichten, jeweils mit Amplitude, Frequenz, Ein-/Ausblendzeit:

| Schicht | Wirkung |
|---|---|
| **Idle Breathing** | Atmen im Stand |
| **Turn Drag** | Kamera läuft der Mausdrehung nach |
| **Walk Bob** | Wippen beim Gehen |
| **Sprint Shake** | Rütteln beim Sprinten |
| **Crouch Sway** | ruhigeres Schwanken beim Ducken |
| **Reload Cradle** | Neigen beim Nachladen |
| **Landing Impact** | Stoß bei der Landung |
| **ADS Breath Hold** | Mikro-Zittern beim Zielen |
| **Low Health Stagger** | Taumeln bei wenig Leben |

`Use Noise` an = organisches Zittern (Perlin-Rauschen), aus = sauberer Sinus.
`Global Intensity` skaliert alles auf einmal — zum schnellen Abschwächen.

Das System ist **additiv**: Maussteuerung bleibt unverändert, es legt nur einen
kleinen Versatz darüber. Im Jet und in Cutscenes schaltet es sich automatisch ab.

### Hände ins Spiel bringen (zwei Wege)

**Weg A — Hände am Waffenmodell (empfohlen, einfachster Weg)**

1. Arme/Hände als Kinder des **Waffen-Prefabs** modellieren/importieren.
2. Im Waffen-Prefab zwei leere Objekte anlegen: `HandSlot_Right` (am Griff)
   und `HandSlot_Left` (am Vorderschaft).
3. Arme als Kinder dieser Slots hängen — dann sitzen sie korrekt und bewegen
   sich bei ADS, Nachladen und Rückstoß automatisch mit.

**Weg B — separates Arm-Prefab**

1. `FirstPersonArmsRig` auf das Spieler-Objekt legen (oder auf die Kamera).
2. **Arms Prefab** zuweisen (das Arm-Modell).
3. Optional `Hand Slot Right/Left` aus dem Waffen-Prefab zuweisen.
4. Mit `Right/Left Hand Local Position/Euler` feinjustieren.

**Wippen/Schwanken** (gilt für beide Wege) auf `FirstPersonArmsRig`:
`Walk Bob`, `Sway`, `Idle Breath`, `Sprint`, `ADS Dampening`,
`Lower While Reloading`. Alles im Inspector, ohne Code.

`SetArmsVisible(false)` blendet die Arme aus (z. B. in Cutscenes).

---

## 4) Magazine — mehrere Typen pro Kaliber

### Wie es jetzt zusammenspielt

* Ein **Magazin-Typ** = ein `MagazineTypeSO`-Asset (Kaliber, Kapazität, Gewicht,
  Magazin-Klasse, Nachlade-Multiplikator).
* Eine Waffe akzeptiert ein Magazin, wenn es in ihrer Liste steht **oder
  dasselbe Kaliber (`AmmoTypeSO`) hat**. → Neue Magazine funktionieren
  automatisch, ohne sie bei jeder Waffe einzutragen.
* Magazine erscheinen an drei Stellen:
  1. **Als Weltloot** — `WorldMagazinePickup` mit `magazineType` belegen
  2. **Als Reserve im Inventar** — erscheint bei `V` einzeln mit Füllstand
  3. **Als Attachment** an der `MagPoint`-Rail (Kategorie `Magazine`)

### Neuen Magazin-Typ hinzufügen

1. `Create > WAD > Magazine Type`
2. Kaliber (`Ammo Type`), `Base Capacity`, `Empty Weight Kg` setzen
3. `Magazine Class` wählen (Standard/Short/Extended/Drum/Box/Belt/…)
4. Optional `Reload Time Multiplier` (Trommel = langsamer) und
   `Spread Multiplier`
5. Asset in **`Assets/Data/Magazines/MagazineCatalog.asset`** eintragen
   → damit ist es im Katalog und für Loot/UI bekannt

### Als Attachment verwendbar machen

1. `Create > WAD > Attachment`
2. `Category` = **Magazine**
3. `Magazine Type Override` = dein Magazin-Typ
4. Optional `Visual Prefab`

Beim Montieren wechselt `WeaponController` automatisch den geladenen
Magazin-Typ und begrenzt die Patronenzahl auf die neue Kapazität.

### Mitgelieferte neue Magazine

| Magazin | Kaliber | Kapazität | Klasse |
|---|---|---|---|
| AKM Trommelmagazin 75 | 7.62×39 | 75 | Drum (langsameres Nachladen) |
| AKM Kurzmagazin 10 | 7.62×39 | 10 | Short (schneller) |
| STANAG 30 Rounds | 5.56×45 | 30 | Standard |
| Glock 33 Rounds | 9×19 | 33 | Extended |
| M1911 Extended 10 | .45 ACP | 10 | Extended |

Zusätzlich als Attachments angelegt:
`Attachment_AKM_Drum_75`, `Attachment_AKM_Short_10`, `Attachment_Glock_33`,
`Attachment_M1911_Extended_10`.

### Inspect-Fenster (`V`)

Auf `WeaponInspectUI` neu:

* **Reserve List Content** — Parent in der ScrollView → zeigt jedes
  Reserve-Magazin einzeln (Name, Füllstand, Gewicht)
* **Reserve Entry Prefab** — optional; ohne Prefab werden Textzeilen erzeugt
* **Magazine Catalog** — `MagazineCatalog.asset` zuweisen → zeigt alle
  Magazin-Typen (grau = nicht im Inventar)
* **Catalog List Content** — Parent der Katalog-ScrollView
* **Filter Catalog To Weapon** — nur passende Kaliber zeigen

### Magazine als Level-Loot platzieren

Zwei Wege — beide ohne Code:

**A) `MagazineWorldSpawner`** (frei im Level verteilt)

Ein leeres Objekt in die Level-Szene legen und `MagazineWorldSpawner` hinzufügen:

| Feld | Bedeutung |
|---|---|
| **Magazine Catalog** | `MagazineCatalog.asset` (leer = letzter geladener Katalog) |
| **Spawn Count Range** | Wie viele Magazine erscheinen (z. B. 4–8) |
| **Spawn Area Center / Radius** | Suchbereich in der Welt |
| **Min Distance From Player** | Nicht direkt vor die Nase spawnen |
| **Min Distance Between Magazines** | Abstand zueinander |
| **Ground Layers / Max Ground Slope** | Bodensuche |
| **Allowed Calibers / Allowed Classes** | Optionaler Filter (z. B. nur Trommelmagazine) |
| **Random Rounds / Min Fill Fraction** | Teils gefüllte Magazine |
| **Random Seed** | 0 = jedes Mal anders |

> Magazine ohne `Ground Model Prefab` werden übersprungen (sie sind sonst
> unsichtbar). Diese Modelle sind bereits für alle Kaliber gesetzt.

**B) `MagazineSpawner`** (an Chunk-Markierungen) — der bestehende Weg über
`MagazineSpawnPoint`-Objekte in den Chunks, mit `MagazineTableSO`.

---

## 5) "Found Footage"-Optik

1. Volume im Level anlegen (`GameObject > Volume > Global Volume`)
2. `FoundFootageController` darauf legen
3. Asset `Create > WAD > Found Footage Profile` zuweisen

Einstellbar: **Filmkorn**, **Vignette**, **Kontrast/Sättigung/Farbstich**,
**chromatische Aberration**, **Linsenverzerrung**, **Bloom**, **Bewegungsunschärfe**,
**Handheld Intensity**.

`Blend Duration` regelt weiches Umschalten — z. B. für Cutscenes, die einen
eigenen Look bekommen (`PlayProfile(...)` / `BlendBack()`).

> Der Zugriff auf die URP-Overrides läuft über Reflexion, damit ein
> URP-Update das Skript nicht bricht.

---

## 6) Cutscenes am Levelanfang und pro Exit

### Cutscene bauen

1. `Create > WAD > Cutscene Sequence`
2. **Steps** füllen. Pro Schritt:
   * `Duration`, `Hard Cut` / `Blend`
   * `Camera Mode`:
     * `HoldPlayerView` — nichts bewegen (nur Text/Sound)
     * `DollyBetweenPoints` — Kamera fährt von A nach B
     * `PanAtPoint` — an einem Punkt schwenken
     * `FollowTarget` — Objekt verfolgen (z. B. Helikopter)
   * `Point A` / `Point B` / `Follow Target` — leere Objekte in der Szene
   * `Subtitle Text`
   * `Sound`
   * `Field Of View`
   * `Handheld Shake` — Found-Footage-Wackeln
   * `Footage Override` — eigenes Optik-Profil für diesen Schritt
   * `Time Scale` — Slow-Motion
   * `On Step End` — UnityEvent

### Abspielen

* **Levelanfang:** `CutscenePlayer` in die Szene legen und **Intro Cutscene**
  zuweisen. (Alternativ auf `LevelExitController` → `Intro Cutscene`.)
* **Pro Exit:** im jeweiligen `ExitCondition`-Asset (z. B.
  `Exit_Distance_1500`) das Feld **Outro Cutscene** zuweisen.
  Der Ablauf ist: Cutscene → Run-State → Szenenwechsel.
* **Szenenwechsel:** `LevelExitController` hat jetzt **Next Scene Name** und
  **Hideout Scene Name**. Leer = kein Wechsel (nur Run-State). Der Name muss
  exakt in den Build Settings stehen, sonst gibt es eine klare Log-Meldung.

### Untertitel

`CutsceneSubtitleUI` auf ein Canvas-Objekt legen:
`Panel Root`, `TMP Text` **oder** `Legacy Text`, `Typewriter Speed`,
`Fade Duration`. Im `CutscenePlayer` zuweisen.

### Jet-Cutscene

`JetCutsceneTrigger` auf das Jet-Objekt: `First Boarding Cutscene`,
`Subsequent Boarding Cutscene`, `Repaired Cutscene`.

---

## 7) Sound- und Animations-Slots für jede Aktion

Das ist das **`ActionFeedbackPlayer`**-System.

1. `ActionFeedbackPlayer` auf ein Objekt legen (Waffe, Spieler, Gegner, Jet)
2. Im Inspector Rechtsklick auf die Komponente →
   **"Alle Aktionen als Eintraege anlegen"**
   → erzeugt einen leeren Eintrag pro Aktion
3. Pro Eintrag befüllen:
   * **Sounds** — Liste, ein Clip wird zufällig gewählt
   * **Volume**, **Pitch Range**
   * **Animator Trigger** / **Animator Bool**
   * **Effect Prefab** + **Effect Lifetime** (VFX)
   * **Delay**

Verfügbare Aktionen (Auszug): Waffe (`FireHip`, `FireADS`, `DryFire`,
`ReloadStartEmpty`, `MagazineOut`, `MagazineIn`, `BoltCycle`, `InspectStart` …),
Bewegung (`FootstepWalk`, `FootstepSprint`, `FootstepCrouch`, `Jump`, `Land`,
`LandHard`, `CrouchDown`, `LeanStart`, `StaminaExhausted` …), Spieler
(`PlayerHurtLight`, `PlayerHurtHeavy`, `PlayerDeath`, `HealStart` …),
Interaktion (`PickupItem`, `InteractStart`, `DoorOpen` …),
Gegner (`EnemyIdle`, `EnemySpottedPlayer`, `EnemyHurt`, `EnemyDeath`,
`EnemyAttackRanged`, `EnemyReload`, `EnemySuppressed`, `EnemyFootstep` …).

**Wichtig:** Wo noch nichts eingetragen ist, benutzen `PlayerAudio` und
`EnemyAudio` weiterhin ihre alten Clip-Listen. Du kannst also schrittweise
umstellen, ohne dass etwas verstummt.

---

## 8) Zieloptik auf Riser — behoben

### Was kaputt war

Die Optik-Prefabs (z. B. T-1 mit Riser) sind selbst Prefab-Instanzen. Dadurch
liegt das eigentliche Optik-Modell eine Ebene **unter** dem Visual-Root, und
`ADS_AimPoint` hängt erst in der zweiten Ebene.

`Transform.Find("ADS_AimPoint")` sucht aber nur **direkte Kinder** → fand
nichts → die Waffe fiel auf `Default Iron Sight Aim Point` zurück →
**man zielte durch Kimme und Korn statt durch die Optik.**

### Behoben durch

`WeaponAttachmentManager.GetOpticAimPoint()` sucht jetzt **rekursiv**. Wird
kein `ADS_AimPoint` gefunden, gibt es eine eindeutige Warnung im Log.

### Was du tun musst

In jedem Optik-Prefab ein Kind namens exakt **`ADS_AimPoint`** anlegen — an der
Stelle, durch die der Spieler schauen soll (Mitte des Rohrs/Glases).

### Rotpunkt im Sichtbild

`RedDotReticle` (auf dem Punkt-Objekt im Optik-Prefab) ist jetzt konfigurierbar:

| Feld | Bedeutung |
|---|---|
| **Only Visible While Aiming** | Punkt nur bei ADS sichtbar |
| **Fade Duration** | weiches Ein-/Ausblenden |
| **Toggle Game Object** | ganzes Objekt schalten (spart Draw-Calls) |
| **Behaviour** | `Fixed` (starr) oder `Collimated` (folgt der Zielachse) |
| **Max Wander Radius** | wie weit der Punkt in der Optik wandern darf |
| **Follow Strength** | Stärke des Kollimator-Effekts |
| **Breath Amplitude / Speed** | leichtes Atmen des Punktes |
| **Emission Property** | z. B. `_EmissionColor` |
| **Aim Color / Intensity** | Helligkeit im Zielmodus |

**Empfehlung für realistischen Look:** `Behaviour = Collimated`,
`Max Wander Radius` ≈ 0,004, `Follow Strength` ≈ 0,6,
`Emission Property = _EmissionColor`, `Aim Intensity` ≈ 2.
Das Material braucht **Emission aktiviert**, sonst zeigt die Intensität keine
Wirkung.

---

## Bekannte Punkte, die noch Assets brauchen

Das sind **Inhalte**, keine Code-Fehler — die Systeme stehen bereit:

* **Jet-Modell** und **Jet-Geräusche** (Triebwerk, MG, Rakete) — es gibt noch
  kein Jet-Mesh im Projekt, nur einen Platzhalter.
* **`ADS_AimPoint`** in jedem Optik-Prefab (siehe 8).
* **Waffen ohne AudioSource** (`M1911`, `XM607`): `WeaponController` ergänzt
  beim Start automatisch eine — die Clips kommen aus dem `WeaponSO`.
* **`Quest_DeathResistance`**: `questId` nachtragen.
* **`Exit_*`-Assets**: `outroCutscene` und die Ziel-Level-Indizes setzen.
* **Magazin-Modelle**: `groundModelPrefab` / `attachmentVisualPrefab` sind noch
  leer — ohne Modell funktioniert das System, aber man sieht nichts.
