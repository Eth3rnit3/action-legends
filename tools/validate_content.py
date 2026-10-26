#!/usr/bin/env python3
"""Validate community missions without opening Unity or loading executable game code."""
import json
import math
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMAGES = (".png", ".jpg", ".jpeg")
SOUNDS = (".wav", ".ogg", ".mp3")
ROLES = ("rusher", "thrower", "shield", "pistol", "smg", "shotgun")
ARMED = ("pistol", "smg", "shotgun")
BOSS_KINDS = ("helicopter", "mecha")


def sheet_rows(resources, path):
    """Rows of four square frames in an enemy sheet, read from the PNG header; None if unknown."""
    file = resources / (path + ".png")
    try:
        header = file.read_bytes()[:24]
    except OSError:
        return None
    if header[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    width, height = struct.unpack(">II", header[16:24])
    return max(1, round(height / (width / 4)))


def validate_mission(data, resources):
    errors = []
    if not isinstance(data, dict):
        return ["The mission must be a JSON object"]
    if data.get("schemaVersion") != 1:
        errors.append("schemaVersion must be 1")
    order = data.get("order", 1)
    if type(order) is not int or not 0 <= order <= 999:
        errors.append("order must be an integer between 0 and 999")
    for key, limit in {"id": 60, "title": 45, "location": 45, "intro": 80, "exitLabel": 40, "winTitle": 80}.items():
        value = data.get(key)
        if not isinstance(value, str) or not value.strip() or len(value) > limit:
            errors.append(f"{key}: non-empty string, maximum {limit} characters")
    if not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", str(data.get("id", ""))):
        errors.append("id must be lowercase words separated by hyphens")
    def number(key, lower, upper):
        value = data.get(key)
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not lower <= value <= upper:
            errors.append(f"{key}: number between {lower} and {upper}")
            return None
        return value
    length = number("length", 30, 500)
    exit_x = number("exitX", 10.001, 500)
    low = number("cameraMin", 0, 500)
    high = number("cameraMax", 0, 500)
    if length is not None and exit_x is not None and exit_x > length:
        errors.append("exitX must be inside the level")
    if low is not None and high is not None and (high <= low or length is not None and high > length):
        errors.append("cameraMin < cameraMax <= length is required")
    backgrounds = data.get("backgrounds")
    if not isinstance(backgrounds, list) or not 1 <= len(backgrounds) <= 8:
        errors.append("backgrounds must contain one to eight resource paths")
    else:
        for path in backgrounds:
            if not isinstance(path, str) or not path.startswith("Art/") or ".." in path or ":" in path or "\\" in path:
                errors.append("Invalid background path")
            elif not any((resources / (path + ext)).is_file() for ext in IMAGES):
                errors.append(f"Missing background: {path}")
    def resource(label, value, prefix, extensions):
        # Optional fields: an absent or empty value falls back to the Paris default.
        if value is None or isinstance(value, str) and not value.strip():
            return
        if not isinstance(value, str) or not value.startswith(prefix) or ".." in value or ":" in value or "\\" in value:
            errors.append(f"Invalid {label} path")
        elif not any((resources / (value + ext)).is_file() for ext in extensions):
            errors.append(f"Missing {label}: {value}")
    resource("ground", data.get("ground"), "Art/", IMAGES)
    resource("music", data.get("music"), "Audio/", SOUNDS)
    resource("bossMusic", data.get("bossMusic"), "Audio/", SOUNDS)
    code = data.get("code")
    if code is not None and (not isinstance(code, str) or not re.fullmatch(r"[A-Z0-9]{4,10}", code)):
        errors.append("code must be 4 to 10 uppercase letters or digits")
    sheet = data.get("enemySheet")
    resource("enemy sheet", sheet, "Art/", IMAGES)
    rows = sheet_rows(resources, sheet) if isinstance(sheet, str) and sheet.strip() else 3
    enemies = data.get("enemies")
    roster = 3
    if enemies is not None:
        if not isinstance(enemies, list) or not 1 <= len(enemies) <= 6:
            errors.append("enemies must contain one to six enemy types")
        else:
            roster = len(enemies)
            for i, enemy in enumerate(enemies):
                if not isinstance(enemy, dict) or enemy.get("role") not in ROLES:
                    errors.append(f"enemy {i}: role must be one of {', '.join(ROLES)}")
                    continue
                row = enemy.get("row", 0)
                if type(row) is not int or row < 0 or rows is not None and row >= rows:
                    errors.append(f"enemy {i}: row must exist in the enemy sheet")
                if enemy["role"] in ARMED:
                    muzzle = enemy.get("muzzle")
                    if not isinstance(muzzle, dict) or not all(
                            not isinstance(muzzle.get(k), bool) and isinstance(muzzle.get(k), (int, float)) and 0 < muzzle.get(k) < 3 for k in "xy"):
                        errors.append(f"enemy {i}: armed enemies need a muzzle with x and y between 0 and 3")
    boss = data.get("boss", {})
    if not isinstance(boss, dict):
        errors.append("boss must be an object")
    else:
        if not isinstance(boss.get("enabled", True), bool):
            errors.append("boss.enabled must be true or false")
        if boss.get("kind", "helicopter") not in BOSS_KINDS:
            errors.append(f"boss.kind must be one of {', '.join(BOSS_KINDS)}")
        for key, limit in {"name": 30, "intro": 80, "rage": 80, "defeat": 80}.items():
            value = boss.get(key)
            if value is not None and (not isinstance(value, str) or len(value) > limit):
                errors.append(f"boss.{key}: string, maximum {limit} characters")
        health = boss.get("health", 1)
        if isinstance(health, bool) or not isinstance(health, (int, float)) or not math.isfinite(health) or not .25 <= health <= 4:
            errors.append("boss.health must be between .25 and 4")
        resource("boss sprite", boss.get("sprite"), "Art/", IMAGES)
    waves = data.get("waves")
    if not isinstance(waves, list) or not 1 <= len(waves) <= 20:
        errors.append("waves must contain one to twenty waves")
        return errors
    previous = 0
    for i, wave in enumerate(waves):
        if not isinstance(wave, dict):
            errors.append(f"wave {i}: object required")
            continue
        x = wave.get("atX")
        if isinstance(x, bool) or not isinstance(x, (int, float)) or not math.isfinite(x) or x <= previous or exit_x is not None and x >= exit_x:
            errors.append(f"wave {i}: atX must increase and precede exitX")
        else:
            previous = x
        for key, lower, upper in (("baseEnemies", 1, 100), ("enemiesPerPlayer", 0, 20), ("maxAlive", 1, 30)):
            value = wave.get(key)
            if type(value) is not int or not lower <= value <= upper:
                errors.append(f"wave {i}: {key} must be an integer between {lower} and {upper}")
        interval = wave.get("spawnInterval")
        if isinstance(interval, bool) or not isinstance(interval, (int, float)) or not math.isfinite(interval) or not .1 <= interval <= 10:
            errors.append(f"wave {i}: spawnInterval must be between .1 and 10")
        pattern = wave.get("enemyPattern")
        if not isinstance(pattern, list) or not 1 <= len(pattern) <= 30 or any(type(v) is not int or not 0 <= v < roster for v in pattern):
            errors.append(f"wave {i}: enemyPattern must use enemy types 0 to {roster - 1}")
    return errors


def main():
    resources = ROOT / "Assets/Resources"
    files = sorted((resources / "Levels").glob("*.json"))
    failures = []
    ids = set()
    codes = set()
    if not files:
        failures.append("No missions found")
    for file in files:
        try:
            data = json.loads(file.read_text(encoding="utf-8"))
            errors = validate_mission(data, resources)
            if not errors:
                if data["id"] in ids:
                    errors.append("Duplicate mission id")
                ids.add(data["id"])
                if data.get("code") in codes:
                    errors.append("Duplicate mission code")
                if data.get("code"):
                    codes.add(data["code"])
                if file.stem != data["id"]:
                    errors.append("Filename must match mission id")
            failures.extend(f"{file.name}: {error}" for error in errors)
        except (ValueError, OSError) as exc:
            failures.append(f"{file.name}: {exc}")
    if failures:
        print("\n".join(failures), file=sys.stderr)
        return 1
    print(f"Validated {len(files)} mission(s)")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
