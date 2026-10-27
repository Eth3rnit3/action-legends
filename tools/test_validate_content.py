import copy
import json
import tempfile
import unittest
from pathlib import Path
from validate_content import (ROOT, find_proper_nouns, load_library, merge, validate_boss, validate_enemy, validate_hero,
                              validate_mission)

RESOURCES = ROOT / "Assets/Resources"
LIBRARY, LIBRARY_ERRORS = load_library(RESOURCES)


def content(kind, entry):
    return json.loads((RESOURCES / "Content" / kind / f"{entry}.json").read_text(encoding="utf-8"))


class MissionValidationTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads((RESOURCES / "Levels/panique-a-paname.json").read_text(encoding="utf-8"))

    def errors(self, data=None):
        return validate_mission(self.data if data is None else data, RESOURCES, LIBRARY)

    def rejected(self, change):
        data = copy.deepcopy(self.data)
        change(data)
        return bool(self.errors(data))

    def test_shipped_missions_are_valid(self):
        for file in sorted((RESOURCES / "Levels").glob("*.json")):
            with self.subTest(file=file.name):
                self.assertEqual(self.errors(json.loads(file.read_text(encoding="utf-8"))), [])

    def test_missing_image_is_rejected(self):
        self.assertTrue(self.rejected(lambda d: d["map"].update(backgrounds=["Art/does-not-exist"])))

    def test_paths_cannot_escape_resources(self):
        self.assertTrue(self.rejected(lambda d: d["map"].update(backgrounds=["Art/../../private"])))

    def test_waves_must_precede_exit(self):
        self.assertTrue(self.rejected(lambda d: d["waves"][0].update(atX=d["exitX"])))

    def test_unknown_enemy_is_rejected(self):
        self.assertTrue(self.rejected(lambda d: d["waves"][0].update(enemyPattern=[999])))
        self.assertTrue(self.rejected(lambda d: d.update(enemies=["looter-rusher", "ninja"])))

    def test_invalid_numbers_are_rejected(self):
        for value in (float("nan"), float("inf"), True, -1):
            with self.subTest(value=value):
                self.assertTrue(self.rejected(lambda d: d.update(length=value)))

    def test_wave_lock_must_be_boolean(self):
        for value in (True, False):
            self.data["waves"][0]["lock"] = value
            self.assertEqual(self.errors(), [])
        for value in (1, "false", None):
            with self.subTest(value=value):
                self.assertTrue(self.rejected(lambda d: d["waves"][0].update(lock=value)))

    def test_empty_wave_list_is_rejected(self):
        self.assertTrue(self.rejected(lambda d: d.update(waves=[])))

    def test_bad_schema_is_rejected(self):
        for version in (1, 3, None):
            with self.subTest(version=version):
                self.assertTrue(self.rejected(lambda d: d.update(schemaVersion=version)))

    def test_mission_without_boss_is_valid(self):
        self.data["boss"] = {"enabled": False}
        del self.data["bossMusic"]
        self.assertEqual(self.errors(), [])

    def test_missing_ground_music_or_boss_music_is_rejected(self):
        for change in (lambda d: d["map"].update(ground="Art/does-not-exist"), lambda d: d["map"].pop("ground"),
                       lambda d: d.update(music="Audio/Music/does-not-exist"), lambda d: d.pop("music"),
                       lambda d: d.update(bossMusic="Art/paris"), lambda d: d.pop("bossMusic")):
            with self.subTest(change=change):
                self.assertTrue(self.rejected(change))

    def test_bad_order_is_rejected(self):
        for order in (-1, 1000, 1.5, True, "2"):
            with self.subTest(order=order):
                self.assertTrue(self.rejected(lambda d: d.update(order=order)))

    def test_boss_reference_and_settings(self):
        self.data["boss"] = {"id": "mecha-shogun", "health": 2, "name": "AUTRE NOM"}
        self.assertEqual(self.errors(), [])
        for boss in ({"enabled": "yes", "id": "macron-helicopter"}, {"id": "dragon"}, {}, {"id": "le-direct", "health": 0},
                     {"id": "le-direct", "health": True}, {"id": "le-direct", "name": "X" * 31}, "MACRON"):
            with self.subTest(boss=boss):
                self.assertTrue(self.rejected(lambda d: d.update(boss=boss)))

    def test_mission_code_format(self):
        self.data["code"] = "SAKURA"
        self.assertEqual(self.errors(), [])
        for code in ("abc1", "ABC", "TOO-LONG-CODE", 1234):
            with self.subTest(code=code):
                self.assertTrue(self.rejected(lambda d: d.update(code=code)))

    def test_roster_reuses_enemies_of_other_missions(self):
        self.data["enemies"] = ["looter-rusher", "gunman-pistol"]
        for wave in self.data["waves"]:
            wave["enemyPattern"] = [0, 1]
        self.assertEqual(self.errors(), [])
        for enemies in ([], ["looter-rusher"] * 9, "looter-rusher", [3]):
            with self.subTest(enemies=enemies):
                self.assertTrue(self.rejected(lambda d: d.update(enemies=enemies)))

    def test_pattern_cannot_exceed_roster(self):
        self.data["enemies"] = ["looter-rusher"]
        self.data["extraEnemy"] = {"enemy": -1}
        self.data["waves"][0]["enemyPattern"] = [1]
        self.assertTrue(self.errors())

    def test_extra_enemy(self):
        for extra in ({"enemy": -1}, {"enemy": 2, "every": 3, "fromDifficulty": 0}, {}):
            with self.subTest(extra=extra):
                self.data["extraEnemy"] = extra
                self.assertEqual(self.errors(), [])
        for extra in ({"enemy": 3}, {"enemy": 1, "every": 0}, {"enemy": 1, "fromDifficulty": 4}, {"enemy": 1.5}, []):
            with self.subTest(extra=extra):
                self.assertTrue(self.rejected(lambda d: d.update(extraEnemy=extra)))

    def test_reserved_slots_must_stay_empty(self):
        for key in ("terrain", "props", "hostages", "vehicles"):
            with self.subTest(key=key):
                self.assertTrue(self.rejected(lambda d: d["map"].update({key: [{"id": "x", "x": 10}]})))
                self.assertFalse(self.rejected(lambda d: d["map"].pop(key)))
        self.assertTrue(self.rejected(lambda d: d.update(encounters=[{"atX": 20}])))
        self.assertFalse(self.rejected(lambda d: d.pop("encounters")))


class LibraryValidationTests(unittest.TestCase):
    def test_shipped_library_is_valid(self):
        self.assertEqual(LIBRARY_ERRORS, [])
        self.assertEqual(sorted(LIBRARY["heroes"]), ["arnold", "chuck-norris", "jean-claude", "sylvester"])
        self.assertEqual(len(LIBRARY["enemies"]), 6)
        self.assertEqual(sorted(LIBRARY["bosses"]), ["le-direct", "macron-helicopter", "mecha-shogun"])

    def test_extends_merges_objects_and_replaces_lists(self):
        direct = LIBRARY["bosses"]["le-direct"]
        self.assertEqual(direct["animations"]["sheet"]["texture"], "Art/marseille/news-helicopter")
        self.assertEqual(direct["animations"]["sheet"]["cellWidth"], 8.8)
        self.assertEqual(direct["attacks"], LIBRARY["bosses"]["macron-helicopter"]["attacks"])
        self.assertEqual(merge({"a": {"b": 1, "c": 2}, "l": [1, 2]}, {"a": {"b": 3}, "l": [4]}), {"a": {"b": 3, "c": 2}, "l": [4]})

    def test_extends_needs_a_known_parent(self):
        for entry in ({"id": "orphan", "extends": "nobody"}, {"id": "loop", "extends": "loop"}):
            with self.subTest(entry=entry):
                library, errors = load_library(RESOURCES, {"enemies": [entry]})
                self.assertTrue(any(entry["id"] in e for e in errors))

    def test_new_enemy_from_data_only(self):
        enemy = {"id": "test-brute", "extends": "looter-shield", "health": 12, "speed": 2,
                 "animations": {"states": [{"name": "idle", "cells": [8, 11], "fps": 4}, {"name": "run", "cells": [9, 10, 9], "perStride": 0.5},
                                           {"name": "attack", "cells": [11]}, {"name": "hurt", "cells": [8]}, {"name": "death", "cells": [11, 8], "fps": 6, "loop": False}]}}
        library, errors = load_library(RESOURCES, {"enemies": [enemy]})
        self.assertEqual(errors, [])
        self.assertEqual(library["enemies"]["test-brute"]["animations"]["sheet"]["texture"], "Art/enemies")

    def test_enemy_animation_and_anchor_rules(self):
        base = content("enemies", "gunman-smg")
        self.assertEqual(validate_enemy(base, RESOURCES), [])
        changes = (
            lambda d: d["animations"].update(states=[s for s in d["animations"]["states"] if s["name"] != "attack"]),
            lambda d: d["animations"]["states"][0].update(cells=[12]),
            lambda d: d["animations"].update(anchors=[]),
            lambda d: d["animations"]["anchors"][0].update(cell=0),
            lambda d: d["animations"]["sheet"].update(texture="Art/none"),
            lambda d: d["animations"]["sheet"].update(columns=0),
            lambda d: d["animations"]["states"][1].update(fps=-1),
            lambda d: d["animations"]["states"].append(dict(d["animations"]["states"][0])),
            lambda d: d.update(behavior="ninja"),
            lambda d: d["gun"].update(speed=[12, 13]),
            lambda d: d["gun"].update(sound="Audio/Sfx/none"),
            lambda d: d["gun"].update(projectile="laser"),
            lambda d: d["gun"].update(burst=0),
            lambda d: d.update(health=0),
        )
        for change in changes:
            with self.subTest(change=change):
                data = copy.deepcopy(base)
                change(data)
                self.assertTrue(validate_enemy(data, RESOURCES))
        thrower = content("enemies", "looter-thrower")
        self.assertEqual(validate_enemy(thrower, RESOURCES), [])
        thrower["animations"]["anchors"] = []
        self.assertTrue(validate_enemy(thrower, RESOURCES))

    def test_hero_rules(self):
        base = content("heroes", "arnold")
        self.assertEqual(validate_hero(base, RESOURCES), [])
        changes = (
            lambda d: d["animations"].update(anchors=d["animations"]["anchors"][:-1]),
            lambda d: d["animations"]["states"].pop(),
            lambda d: d["special"].update(type="teleport"),
            lambda d: d["special"].update(shots=0),
            lambda d: d.update(portraitCell=16),
            lambda d: d.update(name=""),
            lambda d: d.update(speed=0),
        )
        for change in changes:
            with self.subTest(change=change):
                data = copy.deepcopy(base)
                change(data)
                self.assertTrue(validate_hero(data, RESOURCES))

    def test_boss_rules(self):
        for boss_id in ("macron-helicopter", "mecha-shogun"):
            base = content("bosses", boss_id)
            self.assertEqual(validate_boss(base, RESOURCES), [])
            changes = (
                lambda d: d["attacks"][0].update(type="laser"),
                lambda d: d["attacks"][0].update(muzzles=["nowhere"]),
                lambda d: d["attacks"][-1].update(rack="nowhere"),
                lambda d: d["attacks"][0].update(state="dance"),
                lambda d: d.update(attacks=[]),
                lambda d: d.update(health=[300, 560]),
                lambda d: d["missile"].update(x=0.9),
                lambda d: d["hitbox"].update(count=0),
                lambda d: d.update(behavior="swimmer"),
                lambda d: d["animations"].update(anchors=[a for a in d["animations"]["anchors"] if a["name"] != "smoke"]),
            )
            for change in changes:
                with self.subTest(boss=boss_id, change=change):
                    data = copy.deepcopy(base)
                    change(data)
                    self.assertTrue(validate_boss(data, RESOURCES))
        walker = content("bosses", "mecha-shogun")
        walker["animations"]["states"] = [s for s in walker["animations"]["states"] if s["name"] != "walk"]
        self.assertTrue(validate_boss(walker, RESOURCES))


class EngineCodeTests(unittest.TestCase):
    def test_no_mission_name_in_engine_code(self):
        self.assertEqual(find_proper_nouns(), [])

    def test_mission_names_are_detected(self):
        with tempfile.TemporaryDirectory() as folder:
            scripts = Path(folder) / "Assets/Scripts"
            scripts.mkdir(parents=True)
            (scripts / "Bad.cs").write_text('namespace Paname{class Bad{string a="Art/macron-helicopter";} // MARSEILLE\n}\n', encoding="utf-8")
            hits = find_proper_nouns(Path(folder))
            self.assertEqual(len(hits), 2)
            self.assertFalse(any("Paname" in h.split(": ")[-1] for h in hits))


if __name__ == "__main__":
    unittest.main()
