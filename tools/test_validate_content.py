import copy
import json
import unittest
from pathlib import Path
from validate_content import validate_mission, ROOT

class MissionValidationTests(unittest.TestCase):
    def setUp(self):
        self.data = json.loads((ROOT / "Assets/Resources/Levels/panique-a-paname.json").read_text())
        self.resources = ROOT / "Assets/Resources"
    def test_shipped_mission_is_valid(self):
        self.assertEqual(validate_mission(self.data, self.resources), [])
    def test_missing_image_is_rejected(self):
        self.data["backgrounds"] = ["Art/does-not-exist"]
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_paths_cannot_escape_resources(self):
        self.data["backgrounds"] = ["Art/../../private"]
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_waves_must_precede_exit(self):
        self.data["waves"][0]["atX"] = self.data["exitX"]
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_unknown_enemy_is_rejected(self):
        self.data["waves"][0]["enemyPattern"] = [999]
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_invalid_numbers_are_rejected(self):
        for value in (float("nan"), float("inf"), True, -1):
            with self.subTest(value=value):
                data = copy.deepcopy(self.data)
                data["length"] = value
                self.assertTrue(validate_mission(data, self.resources))
    def test_empty_wave_list_is_rejected(self):
        self.data["waves"] = []
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_bad_schema_is_rejected(self):
        self.data["schemaVersion"] = 99
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_explicit_defaults_are_valid(self):
        self.data.update(ground="Art/paris", music="Audio/Music/marseillaise-arcade", bossMusic="Audio/Music/marseillaise-boss",
                         boss={"enabled": True, "name": "MACRON", "sprite": "Art/macron-helicopter", "health": 1})
        self.assertEqual(validate_mission(self.data, self.resources), [])
    def test_mission_without_boss_is_valid(self):
        self.data["boss"] = {"enabled": False}
        self.assertEqual(validate_mission(self.data, self.resources), [])
    def test_missing_ground_music_or_boss_art_is_rejected(self):
        for key, value in (("ground", "Art/does-not-exist"), ("music", "Audio/Music/does-not-exist"), ("bossMusic", "Art/paris"),
                           ("boss", {"sprite": "Art/../../private"})):
            with self.subTest(key=key):
                data = copy.deepcopy(self.data)
                data[key] = value
                self.assertTrue(validate_mission(data, self.resources))
    def test_bad_order_is_rejected(self):
        for order in (-1, 1000, 1.5, True, "2"):
            with self.subTest(order=order):
                data = copy.deepcopy(self.data)
                data["order"] = order
                self.assertTrue(validate_mission(data, self.resources))
    def test_bad_boss_settings_are_rejected(self):
        for boss in ({"enabled": "yes"}, {"health": 0}, {"health": True}, {"name": "X" * 31}, "MACRON"):
            with self.subTest(boss=boss):
                data = copy.deepcopy(self.data)
                data["boss"] = boss
                self.assertTrue(validate_mission(data, self.resources))
    def test_mission_code_format(self):
        self.data["code"] = "SAKURA"
        self.assertEqual(validate_mission(self.data, self.resources), [])
        for code in ("abc1", "ABC", "TOO-LONG-CODE", 1234):
            with self.subTest(code=code):
                data = copy.deepcopy(self.data)
                data["code"] = code
                self.assertTrue(validate_mission(data, self.resources))
    def test_enemy_roster(self):
        self.data["enemies"] = [{"role": "rusher", "row": 0}, {"role": "pistol", "row": 1, "muzzle": {"x": 1.2, "y": 1.3}}]
        for wave in self.data["waves"]:
            wave["enemyPattern"] = [0, 1]
        self.assertEqual(validate_mission(self.data, self.resources), [])
        for enemies in ([{"role": "ninja", "row": 0}], [{"role": "pistol", "row": 0}], [{"role": "rusher", "row": 3}], []):
            with self.subTest(enemies=enemies):
                data = copy.deepcopy(self.data)
                data["enemies"] = enemies
                for wave in data["waves"]:
                    wave["enemyPattern"] = [0]
                self.assertTrue(validate_mission(data, self.resources))
    def test_pattern_cannot_exceed_roster(self):
        self.data["enemies"] = [{"role": "rusher", "row": 0}]
        self.data["waves"][0]["enemyPattern"] = [1]
        self.assertTrue(validate_mission(self.data, self.resources))
    def test_boss_kind(self):
        self.data["boss"] = {"kind": "mecha"}
        self.assertEqual(validate_mission(self.data, self.resources), [])
        self.data["boss"] = {"kind": "dragon"}
        self.assertTrue(validate_mission(self.data, self.resources))

if __name__ == "__main__":
    unittest.main()
