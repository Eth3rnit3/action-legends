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

if __name__ == "__main__":
    unittest.main()
