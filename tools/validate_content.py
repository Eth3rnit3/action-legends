#!/usr/bin/env python3
"""Validate the content library and the missions without opening Unity or loading executable game code.

Mirrors Assets/Scripts/ContentLibrary.cs, BossDefinition.cs and LevelCatalog.cs: an absent field takes the
same default as in the game, every reference must exist, and the animation states a behaviour reads must be there."""
import copy
import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMAGES = (".png", ".jpg", ".jpeg")
SOUNDS = (".wav", ".ogg", ".mp3")
SCHEMA = 2
ID = re.compile(r"[a-z0-9]+(?:-[a-z0-9]+)*")
LOOKS = ("tracer", "molotov", "bullet")
# Generic specials (HeroSpecial): blast, volley, and since lot 3H an ally that charges (summon), the hero's own charge (dash)
# and shots in several directions (salvo).
HERO_SPECIALS = ("blast", "volley", "summon", "dash", "salvo")
ALLY_FROMS = ("edge", "behind")
MAX_HEROES = 99
# Hero states the engine reads (HeroDefinition.Required), the ones it fires from, and the optional ones.
HERO_STATES = ("idle", "run", "jump", "crouch", "aimUp", "jumpDown", "knife", "throw")
HERO_FIRING = ("idle", "run", "jump", "crouch", "crouchWalk", "crouchShoot", "aimUp", "aimDiagonal", "jumpDown")
BONUS_EFFECTS = ("weapon", "health", "turbo", "score", "grenades")
# Generic projectiles of heroes' weapons (Content/weapons), not the looks of enemy projectiles (LOOKS).
WEAPON_PROJECTILES = ("bullet", "flame", "rocket", "beam")
WEAPON_FLASHES = ("none", "light", "heavy")
LEGACY_HERO_WEAPON = ("fireRate", "damage", "recoilShake")
ENEMY_BEHAVIORS = ("melee", "thrower", "gunman")
BOSS_BEHAVIORS = ("flyer", "walker")
ATTACKS = ("gun", "bombs", "stomp", "charge", "troops")
# Lot 2b: kinds of wounds (deaths), enemy states read with their fallbacks, and the entries of encounters.
WOUNDS = ("bullet", "explosion", "fire", "knife")
ENTRIES = ("none", "walk", "door", "window", "parachute", "drop", "truck")
RESERVED_MAP = ()
# Lot 3b: how a hero gets into a vehicle (VehicleBoarding.board), its states and the anchors read on each of its images.
VEHICLE_BOARDS = ("touch", "jump")
VEHICLE_STATES = ("idle", "run", "jump")
# Lot 3a: kinds of relief (MapTerrain.Types) and who a prop stops (PropDefinition.Blocks).
TERRAIN_TYPES = ("platform", "block", "slope", "pit")
PROP_BLOCKS = ("none", "heroes", "all")
HOSTAGE_RELEASES = ("shot", "touch", "both")
HOSTAGE_STATES = ("tied", "thanks", "give", "run")
# Words that belong to one mission's content and must never be written in the engine's code.
PROPER_NOUNS = ("Paris", "Parisien", "Macron", "Bastille", "Seine", "Eiffel", "Haussmann", "Marseille", "Marseillaise",
                "Mistral", "Pastis", "Canebière", "Tokyo", "Shogun", "Sakura", "Shibuya", "Akihabara", "Chuck", "Norris",
                "Arnold", "Jean-Claude", "JCVD", "Sylvester", "macron-helicopter", "mecha-shogun", "news-helicopter", "route-0",
                "Saint-Rémy", "Saint-Remy", "Provence", "Provençal", "Nostradamus", "Arlésien", "Arlésienne", "arlesien", "arlesienne",
                "Farandole", "Alpilles", "Glanum", "Favier", "Camargue", "taureau", "prophetie-en-provence",
                "Fernandel", "Marguerite", "Bardot", "Brigitte", "Jeanne", "jeanne-d-arc", "brigitte-bardot")


class Errors(list):
    def number(self, obj, key, low, high, label, default=0, integer=False):
        value = obj.get(key, default) if isinstance(obj, dict) else default
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or not low <= value <= high \
                or integer and not float(value).is_integer():
            self.append(f"{label}.{key}: {'integer' if integer else 'number'} between {low} and {high}")
            return None
        return value

    def per_difficulty(self, obj, key, low, high, label, integer=False):
        values = obj.get(key) if isinstance(obj, dict) else None
        if not isinstance(values, list) or len(values) != 4 or any(
                isinstance(v, bool) or not isinstance(v, (int, float)) or not math.isfinite(v) or not low <= v <= high
                or integer and not float(v).is_integer() for v in values):
            self.append(f"{label}.{key}: four values between {low} and {high}, one per difficulty")

    def text(self, obj, key, limit, label, required=True):
        value = obj.get(key) if isinstance(obj, dict) else None
        if value is None and not required:
            return
        if not isinstance(value, str) or (required and not value.strip()) or len(value) > limit:
            self.append(f"{label}.{key}: {'non-empty ' if required else ''}string, maximum {limit} characters")

    def resource(self, value, prefix, extensions, resources, label):
        if not isinstance(value, str) or not value.startswith(prefix) or ".." in value or ":" in value or "\\" in value:
            self.append(f"{label}: invalid path (must start with {prefix})")
        elif not any((resources / (value + ext)).is_file() for ext in extensions):
            self.append(f"{label}: missing resource {value}")


def merge(parent, child):
    """JsonUtility.FromJsonOverwrite on the parent: nested objects merge field by field, lists and values are replaced."""
    result = copy.deepcopy(parent)
    for key, value in child.items():
        if isinstance(value, dict) and isinstance(result.get(key), dict):
            result[key] = merge(result[key], value)
        else:
            result[key] = copy.deepcopy(value)
    return result


def obj(data, key):
    value = data.get(key) if isinstance(data, dict) else None
    return value if isinstance(value, dict) else {}


def validate_animations(anim, resources, label, required, errors):
    if not isinstance(anim, dict):
        errors.append(f"{label}.animations: object required")
        return None
    label += ".animations"
    sheet = obj(anim, "sheet")
    errors.resource(sheet.get("texture"), "Art/", IMAGES, resources, label + ".sheet.texture")
    columns = errors.number(sheet, "columns", 1, 32, label + ".sheet", 1, True)
    rows = errors.number(sheet, "rows", 1, 32, label + ".sheet", 1, True)
    crops = [errors.number(sheet, k, 0, .9, label + ".sheet") for k in ("cropLeft", "cropRight", "cropTop", "cropBottom")]
    if None not in crops and (crops[0] + crops[1] >= .9 or crops[2] + crops[3] >= .9):
        errors.append(f"{label}.sheet: crops leave too little of a cell")
    pivot = obj(sheet, "pivot") if "pivot" in sheet else {"x": .5, "y": .5}
    errors.number(pivot, "x", 0, 1, label + ".sheet.pivot", .5)
    errors.number(pivot, "y", 0, 1, label + ".sheet.pivot", .5)
    height, width = sheet.get("cellHeight", 0), sheet.get("cellWidth", 0)
    if height:
        errors.number(sheet, "cellHeight", .0001, 50, label + ".sheet")
    else:
        errors.number(sheet, "cellWidth", .0001, 50, label + ".sheet")
    cells = columns * rows if columns and rows else None
    states = anim.get("states")
    names = []
    if not isinstance(states, list) or not 1 <= len(states) <= 64:
        errors.append(f"{label}.states: one to 64 animation states")
        states = []
    for i, state in enumerate(states):
        if not isinstance(state, dict) or not isinstance(state.get("name"), str) or not state["name"].strip() or len(state["name"]) > 30:
            errors.append(f"{label}.states[{i}]: name required")
            continue
        name = state["name"]
        if name in names:
            errors.append(f"{label}: state {name} defined twice")
        names.append(name)
        frames = state.get("cells")
        if not isinstance(frames, list) or not 1 <= len(frames) <= 64 or any(
                type(c) is not int or c < 0 or cells is not None and c >= cells for c in frames):
            errors.append(f"{label}.states.{name}.cells: one to 64 cells of the sheet (0 to {cells - 1 if cells else '?'})")
        errors.number(state, "fps", 0, 60, f"{label}.states.{name}")
        errors.number(state, "perStride", 0, 10, f"{label}.states.{name}")
        if not isinstance(state.get("loop", True), bool):
            errors.append(f"{label}.states.{name}.loop: true or false")
    for name in required:
        if name not in names:
            errors.append(f"{label}: state {name} is required")
    anchors = anim.get("anchors", [])
    if not isinstance(anchors, list):
        errors.append(f"{label}.anchors: list required")
        anchors = []
    for i, anchor in enumerate(anchors):
        if not isinstance(anchor, dict) or not isinstance(anchor.get("name"), str) or not anchor["name"].strip():
            errors.append(f"{label}.anchors[{i}]: name required")
            continue
        errors.number(anchor, "cell", -1, (cells or 1024) - 1, f"{label}.anchors[{i}]", -1, True)
        errors.number(anchor, "x", -20, 20, f"{label}.anchors[{i}]")
        errors.number(anchor, "y", -20, 20, f"{label}.anchors[{i}]")
    errors.number(anim, "layer", -100, 100, label, 30, True)
    return {"states": {s["name"]: s for s in states if isinstance(s, dict) and isinstance(s.get("name"), str)},
            "anchors": [a for a in anchors if isinstance(a, dict)], "cells": cells}


def has_anchor(info, name, cell):
    return info is not None and any(a.get("name") == name and a.get("cell", -1) in (-1, cell) for a in info["anchors"])


def first_cell(info, state):
    cells = info["states"].get(state, {}).get("cells") if info else None
    return cells[0] if isinstance(cells, list) and cells and type(cells[0]) is int else None


def weapon_library(resources):
    """The resolved weapons of Content/weapons, for validators called on their own."""
    library, _ = load_library(resources, kinds=("weapons",))
    return library["weapons"]


def unlimited(weapon):
    ammo = weapon.get("ammo") if isinstance(weapon, dict) else None
    return not ammo or all(v == 0 for v in ammo)


def png_size(path):
    """Width and height of a PNG from its header (no imaging library needed)."""
    with open(path, "rb") as f:
        head = f.read(24)
    return int.from_bytes(head[16:20], "big"), int.from_bytes(head[20:24], "big")


def cell_aspect(anim, resources):
    sheet = obj(anim, "sheet")
    path = resources / (str(sheet.get("texture")) + ".png")
    columns, rows = sheet.get("columns", 1), sheet.get("rows", 1)
    if not path.is_file() or not isinstance(columns, int) or not isinstance(rows, int) or columns < 1 or rows < 1:
        return 1
    w, h = png_size(path)
    return (w / columns) / (h / rows)


def validate_special(special, resources, sl, errors):
    """HeroSpecial.Validate: the numbers each kind of special reads."""
    kind = special.get("type")
    if kind not in HERO_SPECIALS:
        errors.append(f"{sl}.type: one of {', '.join(HERO_SPECIALS)}")
    errors.number(special, "cooldown", .5, 60, sl)
    errors.number(special, "cooldownPerDifficulty", 0, 20, sl)
    errors.number(special, "pose", 0, 5, sl)
    errors.number(special, "damage", 1, 100, sl, 0, True)
    errors.number(special, "bossDamage", 0, 500, sl, 0, True)
    if special.get("sound") not in (None, ""):
        errors.resource(special.get("sound"), "Audio/", SOUNDS, resources, sl + ".sound")
    voice = special.get("voice")
    if voice not in (None, ""):
        if not isinstance(voice, str) or not any((resources / (voice + s + ext)).is_file() for s in ("", "-a") for ext in SOUNDS):
            errors.resource(voice + "-a" if isinstance(voice, str) else voice, "Audio/", SOUNDS, resources, sl + ".voice")
    if special.get("wound") not in (None, "") and special.get("wound") not in WOUNDS:
        errors.append(f"{sl}.wound: one of {', '.join(WOUNDS)}")
    if kind == "blast":
        errors.number(special, "radius", .5, 15, sl)
        errors.number(special, "bossReach", 0, 10, sl)
        errors.number(special, "invulnerable", 0, 5, sl)
        errors.number(special, "leap", 0, 30, sl)
    elif kind == "volley":
        errors.number(special, "speed", 1, 60, sl)
        errors.number(special, "spread", 0, 10, sl)
        errors.number(special, "shots", 1, 15, sl, 0, True)
    elif kind == "summon":
        errors.number(special, "delay", 0, 3, sl)
        ally, al = obj(special, "ally"), sl + ".ally"
        if ally.get("from", "edge") not in ALLY_FROMS:
            errors.append(f"{al}.from: one of {', '.join(ALLY_FROMS)}")
        errors.number(ally, "speed", 1, 60, al)
        errors.number(ally, "reach", .2, 6, al)
        errors.number(ally, "height", .2, 6, al)
        errors.number(ally, "y", 0, 10, al)
        errors.number(ally, "life", .5, 12, al, 6)
        errors.number(ally, "margin", 0, 10, al, 2)
        if ally.get("sound") not in (None, ""):
            errors.resource(ally.get("sound"), "Audio/", SOUNDS, resources, al + ".sound")
        validate_animations(ally.get("animations"), resources, al, ("run",), errors)
    elif kind == "dash":
        errors.number(special, "speed", 1, 60, sl)
        errors.number(special, "time", .05, 3, sl)
        errors.number(special, "radius", .2, 6, sl)
        errors.number(special, "invulnerable", 0, 5, sl)
    elif kind == "salvo":
        errors.number(special, "speed", 1, 60, sl)
        errors.number(special, "spread", 0, 45, sl)
        errors.number(special, "shots", 1, 15, sl, 0, True)
        bursts = errors.number(special, "bursts", 1, 20, sl, 1, True)
        interval = errors.number(special, "interval", 0, 2, sl)
        if bursts and bursts > 1 and interval is not None and interval < .02:
            errors.append(f"{sl}.interval: at least 0.02 s between two bursts")
        directions = special.get("directions")
        if not isinstance(directions, list) or not 1 <= len(directions) <= 8:
            errors.append(f"{sl}.directions: one to eight directions")
        else:
            for i, d in enumerate(directions):
                if not isinstance(d, dict):
                    errors.append(f"{sl}.directions[{i}]: object required")
                    continue
                errors.number(d, "angle", -360, 360, f"{sl}.directions[{i}]")
                if d.get("anchor") is not None and not isinstance(d.get("anchor"), str):
                    errors.append(f"{sl}.directions[{i}].anchor: anchor name")
    if kind in ("summon", "dash"):
        errors.number(special, "trailEvery", .02, 2, sl, .12)
        validate_boss_sprite(special.get("trail"), resources, sl + ".trail", errors)
        validate_boss_sprite(special.get("impact"), resources, sl + ".impact", errors)


def validate_hero(data, resources, weapons=None):
    errors = Errors()
    label = f"hero {data.get('id')}"
    weapons = weapon_library(resources) if weapons is None else weapons
    for key, limit in (("name", 24), ("shortName", 12), ("role", 24), ("specialName", 30)):
        errors.text(data, key, limit, label)
    errors.number(data, "order", 0, 999, label, 0, True)
    errors.number(data, "speed", 1, 30, label)
    errors.number(data, "jump", 1, 30, label)
    errors.number(data, "strideRate", .1, 60, label)
    errors.number(data, "bob", 0, 1, label)
    for key in LEGACY_HERO_WEAPON:
        if data.get(key, 0) != 0:
            errors.append(f"{label}.{key}: set in the hero's weapon (weapon, Content/weapons) since lot 1b")
    weapon = data.get("weapon")
    if weapon not in weapons:
        errors.append(f"{label}.weapon: unknown weapon {weapon} (Content/weapons/<id>.json)")
    elif not unlimited(weapons[weapon]):
        errors.append(f"{label}.weapon: a hero's own weapon must not run out (ammo 0)")
    validate_special(obj(data, "special"), resources, label + ".special", errors)
    hitbox = obj(data, "hitbox")
    errors.number(hitbox, "y", .1, 5, label + ".hitbox", 1)
    errors.number(hitbox, "radius", .1, 3, label + ".hitbox", .53)
    crouch = obj(data, "crouch")
    errors.number(crouch, "speed", 0, 30, label + ".crouch")
    errors.number(crouch, "y", .05, 5, label + ".crouch")
    errors.number(crouch, "radius", .1, 3, label + ".crouch")
    knife = obj(data, "knife")
    errors.number(knife, "reach", .1, 5, label + ".knife")
    errors.number(knife, "height", 0, 5, label + ".knife")
    errors.number(knife, "cooldown", .02, 3, label + ".knife")
    errors.number(knife, "pose", 0, 2, label + ".knife")
    errors.number(knife, "damage", 1, 500, label + ".knife", 0, True)
    errors.resource(knife.get("sound"), "Audio/", SOUNDS, resources, label + ".knife.sound")
    info = validate_animations(data.get("animations"), resources, label, HERO_STATES, errors)
    if info and info["cells"] is not None:
        errors.number(data, "portraitCell", 0, info["cells"] - 1, label, 0, True)
        frame = obj(data, "portraitFrame") if "portraitFrame" in data else {"size": 1}
        fx, fy = errors.number(frame, "x", 0, 1, label + ".portraitFrame"), errors.number(frame, "y", 0, 1, label + ".portraitFrame")
        size = errors.number(frame, "size", .05, 1, label + ".portraitFrame", 1)
        # size is a fraction of the cell's width; the square is as many pixels tall (a cell wider than tall keeps it square).
        aspect = cell_aspect(data.get("animations"), resources)
        if None not in (fx, fy, size) and (fx + size > 1.0001 or fy + size * aspect > 1.0001):
            errors.append(f"{label}.portraitFrame: must stay inside the cell")
        for state in HERO_FIRING:
            for cell in info["states"].get(state, {}).get("cells", []) or []:
                if not has_anchor(info, "muzzle", cell):
                    errors.append(f"{label}: muzzle anchor missing for cell {cell} ({state})")
        thrown = info["states"].get("throw", {}).get("cells")
        if isinstance(thrown, list) and thrown and not has_anchor(info, "hand", thrown[-1]):
            errors.append(f"{label}: hand anchor required on the last cell of throw")
    return errors


def validate_cell(cell, resources, label, errors):
    if not isinstance(cell, dict):
        errors.append(f"{label}: object required")
        return
    errors.resource(cell.get("texture"), "Art/", IMAGES, resources, label + ".texture")
    columns = errors.number(cell, "columns", 1, 32, label, 1, True)
    rows = errors.number(cell, "rows", 1, 32, label, 1, True)
    errors.number(cell, "cell", 0, (columns or 1) * (rows or 1) - 1, label, 0, True)


def validate_weapon(data, resources, library=None):
    errors = Errors()
    label = f"weapon {data.get('id')}"
    errors.text(data, "name", 24, label)
    ammo = data.get("ammo", [0, 0, 0, 0])
    errors.per_difficulty({"ammo": ammo}, "ammo", 0, 9999, label, True)
    limited = isinstance(ammo, list) and len(ammo) == 4 and all(isinstance(v, int) and not isinstance(v, bool) for v in ammo) and any(ammo)
    if limited and not all(v > 0 for v in ammo):
        errors.append(f"{label}.ammo: 0 everywhere (unlimited) or at least 1 everywhere")
    letter = data.get("letter", "")
    if letter not in (None, "") and not (isinstance(letter, str) and re.fullmatch(r"[A-Z]", letter)):
        errors.append(f"{label}.letter: one capital letter")
    if limited:
        if not letter:
            errors.append(f"{label}.letter: required for a weapon with ammunition")
        errors.text(data, "announce", 30, label)
        validate_cell(data.get("crate"), resources, label + ".crate", errors)
    else:
        errors.text(data, "announce", 30, label, required=False)
        if "crate" in data:
            validate_cell(data["crate"], resources, label + ".crate", errors)
    voice = data.get("voice")
    if voice not in (None, ""):
        errors.resource(voice + "-a" if isinstance(voice, str) else voice, "Audio/", SOUNDS, resources, label + ".voice")
    if "icon" in data:
        validate_cell(data["icon"], resources, label + ".icon", errors)
    errors.number(data, "fireRate", .02, 3, label)
    errors.number(data, "damage", 1, 200, label, 0, True)
    errors.resource(data.get("sound"), "Audio/", SOUNDS, resources, label + ".sound")
    if data.get("flash", "light") not in WEAPON_FLASHES:
        errors.append(f"{label}.flash: one of {', '.join(WEAPON_FLASHES)}")
    if not isinstance(data.get("casings", True), bool):
        errors.append(f"{label}.casings: true or false")
    shot, pl = obj(data, "projectile"), label + ".projectile"
    kind = shot.get("type", "bullet")
    if kind not in WEAPON_PROJECTILES:
        errors.append(f"{pl}.type: one of {', '.join(WEAPON_PROJECTILES)}")
    errors.number(shot, "pellets", 1, 9, pl, 1, True)
    errors.number(shot, "pierce", -1, 20, pl, 0, True)
    for key, low, high, default in (("life", .016, 5, 1.25), ("spread", 0, 90, 0), ("jitter", 0, 30, 0), ("radius", 0, 3, 0), ("size", .05, 5, 1), ("growth", 0, 10, 0)):
        errors.number(shot, key, low, high, pl, default)
    if kind == "beam":
        errors.number(shot, "range", 1, 40, pl)
    else:
        errors.number(shot, "speed", 1, 80, pl, 36)
        errors.number(shot, "acceleration", -200, 200, pl)
        errors.number(shot, "homing", 0, 1440, pl)
    sprite = shot.get("sprite")
    if isinstance(sprite, dict) and sprite.get("texture"):
        validate_cell(sprite, resources, pl + ".sprite", errors)
        frames = errors.number(shot, "frames", 1, 16, pl, 1, True)
        cells = sprite.get("columns", 1) * sprite.get("rows", 1) if all(isinstance(sprite.get(k, 1), int) for k in ("columns", "rows")) else 0
        if frames is not None and isinstance(sprite.get("cell", 0), int) and sprite.get("cell", 0) + frames > cells:
            errors.append(f"{pl}.frames: the frames must stay inside the grid, from cell on")
    elif kind in ("beam", "flame"):
        errors.append(f"{pl}.sprite: a beam or a flame is drawn from a sprite")
    color = shot.get("color")
    if color not in (None, "") and not (isinstance(color, str) and re.fullmatch(r"[0-9A-Fa-f]{6}", color)):
        errors.append(f"{pl}.color: six hexadecimal digits (RRGGBB)")
    blast, bl = obj(data, "explosion"), label + ".explosion"
    errors.number(blast, "radius", 0, 10, bl)
    errors.number(blast, "damage", 0, 500, bl, 0, True)
    errors.number(blast, "bossDamage", 0, 1000, bl, 0, True)
    recoil = obj(data, "recoil")
    errors.number(recoil, "shake", 0, 1, label + ".recoil")
    errors.number(recoil, "push", 0, 20, label + ".recoil")
    return errors


def validate_table(table, weapons, label, required, errors):
    """A weapons table: crates that name no weapon give these, weight times per sum of weights."""
    if table in (None, []):
        if required:
            errors.append(f"{label}: at least one weapon, since a weapon crate names none")
        return
    if not isinstance(table, list) or len(table) > 16:
        errors.append(f"{label}: a list of at most sixteen weapons")
        return
    for i, line in enumerate(table):
        if not isinstance(line, dict):
            errors.append(f"{label}[{i}]: object required")
            continue
        errors.number(line, "weight", 1, 100, f"{label}[{i}]", 1, True)
        weapon = line.get("weapon")
        if weapon not in weapons:
            errors.append(f"{label}[{i}].weapon: unknown weapon {weapon}")
        elif unlimited(weapons[weapon]):
            errors.append(f"{label}[{i}].weapon: {weapon} has no ammunition to give")


def validate_rules(data, resources, weapons=None):
    errors = Errors()
    label = f"rules {data.get('id')}"
    weapons = weapon_library(resources) if weapons is None else weapons
    stock = obj(data, "grenades")
    errors.per_difficulty(stock, "start", 0, 99, label + ".grenades", True)
    errors.per_difficulty(stock, "max", 0, 99, label + ".grenades", True)
    if not isinstance(stock.get("refillOnRespawn", True), bool):
        errors.append(f"{label}.grenades.refillOnRespawn: true or false")
    if isinstance(stock.get("start"), list) and isinstance(stock.get("max"), list) and len(stock["start"]) == len(stock["max"]) == 4 \
            and all(isinstance(v, (int, float)) for v in stock["start"] + stock["max"]) and any(a > b for a, b in zip(stock["start"], stock["max"])):
        errors.append(f"{label}.grenades: start cannot exceed max")
    grenade, gl = obj(data, "grenade"), label + ".grenade"
    for key, low, high in (("throwX", 0, 30), ("throwY", -10, 30), ("gravity", 1, 100), ("bounce", 0, 1), ("friction", 0, 1), ("fuse", .1, 10),
                           ("radius", .2, 10), ("size", .05, 2), ("cooldown", 0, 5), ("pose", 0, 2)):
        errors.number(grenade, key, low, high, gl)
    errors.number(grenade, "bounces", 0, 5, gl, 0, True)
    errors.number(grenade, "damage", 1, 500, gl, 0, True)
    errors.number(grenade, "bossDamage", 0, 1000, gl, 0, True)
    if not isinstance(grenade.get("destroysBombs", False), bool):
        errors.append(f"{gl}.destroysBombs: true or false")
    errors.resource(grenade.get("throwSound"), "Audio/", SOUNDS, resources, gl + ".throwSound")
    validate_cell(grenade.get("sprite"), resources, gl + ".sprite", errors)
    validate_cell(grenade.get("icon"), resources, gl + ".icon", errors)
    bonuses = data.get("bonuses")
    if not isinstance(bonuses, list) or not 1 <= len(bonuses) <= 16:
        errors.append(f"{label}.bonuses: one to sixteen bonuses")
        bonuses = []
    for i, bonus in enumerate(bonuses):
        bl = f"{label}.bonuses[{i}]"
        if not isinstance(bonus, dict) or not isinstance(bonus.get("id"), str) or not bonus["id"].strip():
            errors.append(f"{bl}: id required")
            continue
        if bonus.get("effect") not in BONUS_EFFECTS:
            errors.append(f"{bl}.effect: one of {', '.join(BONUS_EFFECTS)}")
        if bonus.get("effect") == "weapon":
            # The weapon brings its ammunition, crate, announce and voice; empty draws one from the weapons table.
            weapon = bonus.get("weapon", "")
            if weapon not in ("", None) and weapon not in weapons:
                errors.append(f"{bl}.weapon: unknown weapon {weapon} (Content/weapons/<id>.json)")
            elif weapon not in ("", None) and unlimited(weapons[weapon]):
                errors.append(f"{bl}.weapon: {weapon} has no ammunition to give")
            if bonus.get("amount") not in (None, []):
                errors.append(f"{bl}.amount: not for a weapon, whose ammunition is in the weapon")
            errors.text(bonus, "announce", 30, bl, required=False)
            if "icon" in bonus:
                validate_cell(bonus["icon"], resources, bl + ".icon", errors)
        else:
            if bonus.get("weapon") not in (None, ""):
                errors.append(f"{bl}.weapon: only for effect weapon")
            errors.per_difficulty(bonus, "amount", 1, 100000, bl, True)
            errors.text(bonus, "announce", 30, bl)
            validate_cell(bonus.get("icon"), resources, bl + ".icon", errors)
        errors.number(bonus, "scale", .2, 5, bl, 1.25)
        voice = bonus.get("voice")
        if voice not in (None, ""):
            errors.resource(voice + "-a" if isinstance(voice, str) else voice, "Audio/", SOUNDS, resources, bl + ".voice")
    for key in ("supplies", "drops"):
        cycle = data.get(key)
        if not isinstance(cycle, list) or not 1 <= len(cycle) <= 64 or any(type(v) is not int or not 0 <= v < len(bonuses) for v in cycle):
            errors.append(f"{label}.{key}: one to 64 indices into bonuses")
    errors.per_difficulty(data, "firstSupply", 5, 500, label)
    errors.per_difficulty(data, "supplySpacing", 10, 500, label)
    errors.per_difficulty(data, "killsPerDrop", 1, 500, label, True)
    draws = any(isinstance(b, dict) and b.get("effect") == "weapon" and not b.get("weapon") for b in bonuses)
    validate_table(data.get("weapons"), weapons, label + ".weapons", draws, errors)
    validate_cell(data.get("go"), resources, label + ".go", errors)
    tally = obj(data, "tally")
    errors.per_difficulty(tally, "hostage", 0, 100000, label + ".tally", True)
    errors.per_difficulty(tally, "perSecond", 0, 10000, label + ".tally", True)
    validate_cell(tally.get("icon"), resources, label + ".tally.icon", errors)
    deaths = data.get("deaths", [])
    seen = set()
    if not isinstance(deaths, list):
        errors.append(f"{label}.deaths: list required")
        deaths = []
    for i, death in enumerate(deaths):
        dl = f"{label}.deaths[{i}]"
        if not isinstance(death, dict) or death.get("damage") not in WOUNDS:
            errors.append(f"{dl}.damage: one of {', '.join(WOUNDS)}")
            continue
        if death["damage"] in seen:
            errors.append(f"{dl}: {death['damage']} twice")
        seen.add(death["damage"])
        launch = obj(death, "launch")
        errors.number(launch, "x", -20, 20, dl + ".launch")
        errors.number(launch, "y", -5, 30, dl + ".launch")
        errors.number(death, "spin", -1440, 1440, dl)
        errors.number(death, "time", .1, 5, dl, 1)
        errors.number(death, "gravity", 0, 100, dl, 18)
        for key in ("lie", "burn"):
            if not isinstance(death.get(key, False), bool):
                errors.append(f"{dl}.{key}: true or false")
    # Lot 3a: the sign over a wave's objective.
    if isinstance(data.get("objective"), dict) and data["objective"].get("texture"):
        validate_cell(data["objective"], resources, label + ".objective", errors)
    errors.number(data, "objectiveSize", .2, 5, label, 1.1)
    stage, sl = obj(data, "stage"), label + ".stage"
    if "alert" in stage:
        validate_cell(stage["alert"], resources, sl + ".alert", errors)
    errors.number(stage, "alertSize", .1, 5, sl, .9)
    if stage.get("alertSound"):
        errors.resource(stage["alertSound"], "Audio/", SOUNDS, resources, sl + ".alertSound")
    for key in ("alertVoice", "panicVoice"):
        if stage.get(key):
            errors.resource(stage[key] + "-a" if isinstance(stage[key], str) else stage[key], "Audio/", SOUNDS, resources, f"{sl}.{key}")
    chute = obj(stage, "parachute")
    for key in ("open", "folding", "landed"):
        if key in chute:
            validate_cell(chute[key], resources, f"{sl}.parachute.{key}", errors)
    for key, low, high, default in (("size", .2, 10, 2.4), ("speed", .3, 20, 2.4), ("sway", 0, 2, .3), ("height", 0, 6, 2.1)):
        errors.number(chute, key, low, high, sl + ".parachute", default)
    openings = stage.get("openings", [])
    if not isinstance(openings, list) or len(openings) > 16:
        errors.append(f"{sl}.openings: at most sixteen")
        openings = []
    ids = []
    for i, opening in enumerate(openings):
        ol = f"{sl}.openings[{i}]"
        if not isinstance(opening, dict) or not isinstance(opening.get("id"), str) or not opening["id"].strip() or opening["id"] in ids:
            errors.append(f"{ol}: unique id required")
            continue
        ids.append(opening["id"])
        validate_cell(opening.get("sprite"), resources, ol + ".sprite", errors)
        errors.number(opening, "height", .2, 10, ol, 2.6)
        errors.number(opening, "time", 0, 5, ol, .7)
    return errors


def validate_carrier(data, resources):
    """CarrierDefinition.Validate: a vehicle that brings an encounter's enemies (states drive and open, a door anchor on open)."""
    errors = Errors()
    label = f"carrier {data.get('id')}"
    for key, low, high, default in (("speed", 1, 40, 9), ("brake", 0, 20, 4), ("interval", .05, 5, .45), ("wait", 0, 10, .8), ("length", .5, 20, 4)):
        errors.number(data, key, low, high, label, default)
    if data.get("sound"):
        errors.resource(data["sound"], "Audio/", SOUNDS, resources, label + ".sound")
    info = validate_animations(data.get("animations"), resources, label, ("drive", "open"), errors)
    cell = first_cell(info, "open")
    if cell is not None and not has_anchor(info, "door", cell):
        errors.append(f"{label}: a door anchor is required on the open cell")
    return errors


def check_reward(bonus, weapon, weapons, label, errors):
    """HostageDefinition.CheckReward: a bonus id, or a weapon with ammunition (a crate holding it)."""
    if not bonus and not weapon:
        errors.append(f"{label}: bonus or weapon required")
    elif weapon:
        if weapon not in weapons:
            errors.append(f"{label}.weapon: unknown weapon {weapon} (Content/weapons/<id>.json)")
        elif unlimited(weapons[weapon]):
            errors.append(f"{label}.weapon: {weapon} has no ammunition to give")


def reward_bonus(rules, bonus, weapon):
    """HostageDefinition.BonusIndex: the rules' bonus a reward names, or their first weapon bonus for a weapon; None if absent."""
    for i, b in enumerate(rules.get("bonuses") or []):
        if not isinstance(b, dict):
            continue
        if (b.get("id") == bonus) if bonus else b.get("effect") == "weapon":
            return None if weapon and b.get("effect") != "weapon" else i
    return None


def validate_hostage(data, resources, weapons=None):
    errors = Errors()
    label = f"hostage {data.get('id')}"
    weapons = weapon_library(resources) if weapons is None else weapons
    errors.text(data, "name", 24, label)
    if data.get("release", "both") not in HOSTAGE_RELEASES:
        errors.append(f"{label}.release: one of {', '.join(HOSTAGE_RELEASES)}")
    errors.number(data, "hits", 1, 50, label, 1, True)
    errors.number(data, "touchRadius", .2, 5, label, .9)
    errors.number(data, "thanks", 0, 10, label, 1.2)
    give = errors.number(data, "give", 0, 10, label, .8)
    errors.number(data, "giveAt", 0, give if give is not None else 10, label, .45)
    hitbox = obj(data, "hitbox")
    errors.number(hitbox, "y", 0, 5, label + ".hitbox", .7)
    errors.number(hitbox, "radius", .1, 3, label + ".hitbox", .6)
    run = obj(data, "run")
    errors.number(run, "speed", .5, 30, label + ".run", 7)
    errors.number(run, "direction", -1, 1, label + ".run", -1, True)
    rewards = data.get("rewards")
    if not isinstance(rewards, list) or not 1 <= len(rewards) <= 16:
        errors.append(f"{label}.rewards: one to sixteen rewards")
        rewards = []
    for i, reward in enumerate(rewards):
        rl = f"{label}.rewards[{i}]"
        if not isinstance(reward, dict):
            errors.append(f"{rl}: object required")
            continue
        errors.number(reward, "weight", 1, 100, rl, 1, True)
        check_reward(reward.get("bonus"), reward.get("weapon"), weapons, rl, errors)
    if data.get("sound") not in (None, ""):
        errors.resource(data.get("sound"), "Audio/", SOUNDS, resources, label + ".sound")
    voice = data.get("voice")
    if voice not in (None, ""):
        errors.resource(voice + "-a" if isinstance(voice, str) else voice, "Audio/", SOUNDS, resources, label + ".voice")
    validate_animations(data.get("animations"), resources, label, HOSTAGE_STATES, errors)
    return errors


def validate_prop(data, resources, weapons=None):
    """PropDefinition.Validate: destructible scenery, its box, what it stops, ledge, explosion, debris, rewards and images."""
    errors = Errors()
    label = f"prop {data.get('id')}"
    weapons = weapon_library(resources) if weapons is None else weapons
    errors.text(data, "name", 24, label)
    blocks = data.get("blocks", "heroes")
    if blocks not in PROP_BLOCKS:
        errors.append(f"{label}.blocks: one of {', '.join(PROP_BLOCKS)}")
    errors.per_difficulty(data, "health", 1, 5000, label, True)
    errors.number(data, "damagedAt", 0, 1, label, .5)
    box = obj(data, "box")
    errors.number(box, "width", .3, 12, label + ".box", 1)
    height = errors.number(box, "height", .2, 6, label + ".box", 1)
    platform = errors.number(data, "platform", 0, 4, label, 0)
    errors.number(data, "platformWidth", 0, 12, label, 0)
    wall = errors.number(data, "wall", 0, 8, label, 0)
    if wall and (blocks == "none" or height is not None and wall < height):
        errors.append(f"{label}.wall: the obstacle's height of a prop that blocks, at least box.height")
    if wall and height is not None:
        height = wall
    if blocks in ("heroes", "all") and platform and height is not None and platform < height:
        errors.append(f"{label}: platform above the top of the box (box.height) of a prop that blocks")
    errors.number(data, "score", 0, 10000, label, 0, True)
    for key in ("cover", "crush"):
        if not isinstance(data.get(key, False), bool):
            errors.append(f"{label}.{key}: true or false")
    explosion = obj(data, "explosion")
    if errors.number(explosion, "radius", 0, 8, label + ".explosion", 0):
        errors.number(explosion, "damage", 0, 500, label + ".explosion", 0, True)
        errors.number(explosion, "heroDamage", 0, 10, label + ".explosion", 0, True)
    debris = obj(data, "debris")
    if debris.get("texture") or debris.get("count", 0):
        dl = label + ".debris"
        errors.resource(debris.get("texture"), "Art/", IMAGES, resources, dl + ".texture")
        columns = errors.number(debris, "columns", 1, 32, dl, 1, True)
        rows = errors.number(debris, "rows", 1, 32, dl, 1, True)
        errors.number(debris, "count", 1, 24, dl, 0, True)
        cells = debris.get("cells", [])
        if not isinstance(cells, list) or any(type(c) is not int or not 0 <= c < (columns or 1) * (rows or 1) for c in cells):
            errors.append(f"{dl}.cells: cells of the grid")
        errors.number(debris, "size", .05, 3, dl, .45)
        errors.number(debris, "speed", 0, 30, dl, 5)
    rewards = data.get("rewards", [])
    if not isinstance(rewards, list) or len(rewards) > 16:
        errors.append(f"{label}.rewards: at most sixteen rewards")
        rewards = []
    for i, reward in enumerate(rewards):
        rl = f"{label}.rewards[{i}]"
        if not isinstance(reward, dict):
            errors.append(f"{rl}: object required")
            continue
        errors.number(reward, "weight", 1, 100, rl, 1, True)
        check_reward(reward.get("bonus"), reward.get("weapon"), weapons, rl, errors)
    for key in ("sound", "breakSound"):
        if data.get(key) not in (None, ""):
            errors.resource(data.get(key), "Audio/", SOUNDS, resources, f"{label}.{key}")
    validate_animations(data.get("animations"), resources, label, ("intact",), errors)
    return errors


def validate_vehicle(data, resources, weapons=None):
    """VehicleDefinition.Validate: a vehicle the heroes drive: health, moves, box, gun, cannon and shells, seat, boarding,
    ram, wreck, debris, HUD icons, sounds and images (seat, turret and cannon anchors on every image it drives with)."""
    errors = Errors()
    label = f"vehicle {data.get('id')}"
    weapons = weapon_library(resources) if weapons is None else weapons
    errors.text(data, "name", 24, label)
    errors.per_difficulty(data, "health", 1, 2000, label, True)
    for key, low, high, default in (("damagedAt", 0, 1, .4), ("invulnerable", 0, 2, .12), ("speed", 1, 20, 6), ("jump", 0, 30, 11),
                                    ("gravity", 5, 80, 29), ("strideRate", .05, 20, 1)):
        errors.number(data, key, low, high, label, default)
    box = obj(data, "box")
    errors.number(box, "width", .8, 8, label + ".box", 1)
    errors.number(box, "height", .6, 6, label + ".box", 1)

    def weapon(wid, wl):
        w = weapons.get(wid) if isinstance(wid, str) else None
        if w is None:
            errors.append(f"{wl}: unknown weapon {wid} (Content/weapons/<id>.json)")
        elif obj(w, "projectile").get("type", "bullet") == "beam":
            errors.append(f"{wl}: a vehicle's weapon fires no beam")
        return w
    gun = weapon(data.get("gun"), label + ".gun")
    if gun is not None and not unlimited(gun):
        errors.append(f"{label}.gun: a weapon that never runs out (ammo 0)")
    cannon = obj(data, "cannon")
    weapon(cannon.get("weapon"), label + ".cannon.weapon")
    errors.per_difficulty(cannon, "shells", 0, 99, label + ".cannon", True)
    errors.number(cannon, "angle", -90, 90, label + ".cannon", 0)
    errors.number(cannon, "pose", 0, 2, label + ".cannon", .25)
    if not isinstance(cannon.get("aimed", False), bool):
        errors.append(f"{label}.cannon.aimed: true or false")
    turret = obj(data, "turret")
    validate_cell(turret.get("sprite"), resources, label + ".turret.sprite", errors)
    if obj(turret, "firing").get("texture"):
        validate_cell(turret.get("firing"), resources, label + ".turret.firing", errors)
    for key, low, high, default in (("size", .1, 4, .5), ("pivot", 0, 1, .3), ("length", 0, 6, 1.4), ("turnRate", 30, 3600, 360),
                                    ("up", -90, 90, 90), ("diagonal", -90, 90, 45), ("down", -90, 90, -25), ("downAir", -90, 90, -90)):
        errors.number(turret, key, low, high, label + ".turret", default)
    pilot = obj(data, "pilot")
    errors.number(pilot, "scale", .2, 2, label + ".pilot", 1)
    boarding = obj(data, "boarding")
    if boarding.get("board", "touch") not in VEHICLE_BOARDS:
        errors.append(f"{label}.boarding.board: one of {', '.join(VEHICLE_BOARDS)}")
    for key, high, default in (("exitJump", 30, 10), ("ejectJump", 30, 15), ("exitInvulnerable", 5, .6), ("ejectInvulnerable", 5, 2)):
        errors.number(boarding, key, 0, high, label + ".boarding", default)
    ram = obj(data, "ram")
    for key in ("damage", "propDamage", "stomp"):
        errors.number(ram, key, 0, 500, label + ".ram", 0, True)
    errors.number(ram, "interval", .05, 5, label + ".ram", .3)
    if ram.get("wound", "explosion") not in WOUNDS:
        errors.append(f"{label}.ram.wound: one of {', '.join(WOUNDS)}")
    wreck = obj(data, "wreck")
    errors.number(wreck, "time", 0, 6, label + ".wreck", 1.4)
    errors.number(wreck, "radius", 0, 10, label + ".wreck", 3)
    errors.number(wreck, "damage", 0, 500, label + ".wreck", 60, True)
    errors.number(wreck, "bossDamage", 0, 1000, label + ".wreck", 0, True)
    errors.number(wreck, "heroDamage", 0, 10, label + ".wreck", 0, True)
    debris = obj(data, "debris")
    if debris.get("texture") or debris.get("count", 0):
        dl = label + ".debris"
        errors.resource(debris.get("texture"), "Art/", IMAGES, resources, dl + ".texture")
        errors.number(debris, "count", 1, 24, dl, 0, True)
    for key in ("icon", "shellIcon"):
        if obj(data, key).get("texture"):
            validate_cell(data.get(key), resources, f"{label}.{key}", errors)
    for key in ("sound", "boardSound", "exitSound", "ejectSound", "breakSound"):
        if data.get(key) not in (None, ""):
            errors.resource(data.get(key), "Audio/", SOUNDS, resources, f"{label}.{key}")
    if data.get("voice") not in (None, "") and not str(data.get("voice")).startswith("Audio/"):
        errors.append(f"{label}.voice: invalid path (must start with Audio/)")
    errors.text(data, "announce", 30, label, required=False)
    info = validate_animations(data.get("animations"), resources, label, VEHICLE_STATES, errors)
    if info:
        names = [n for n in ("idle", "run", "jump", "fire", "damaged", "damagedRun") if n in info["states"]]
        anchors = (pilot.get("anchor", "seat"), turret.get("anchor", "turret"), cannon.get("anchor", "cannon"))
        for name in names:
            for c in info["states"][name].get("cells") or []:
                for anchor in anchors:
                    if not has_anchor(info, anchor, c):
                        errors.append(f"{label}: anchor {anchor} missing on image {c}")
    return errors


def validate_enemy(data, resources):
    errors = Errors()
    label = f"enemy {data.get('id')}"
    behavior = data.get("behavior")
    if behavior not in ENEMY_BEHAVIORS:
        errors.append(f"{label}.behavior: one of {', '.join(ENEMY_BEHAVIORS)}")
    for key, low, high in (("health", 1, 500), ("speed", 0, 30), ("keepDistance", 0, 20), ("hitRadius", .1, 3), ("knockback", 0, 20),
                           ("firstAttack", 0, 10), ("attackPose", 0, 5), ("strideRate", .1, 60)):
        errors.number(data, key, low, high, label)
    errors.number(data, "bonusScore", 0, 10000, label, 0, True)
    if not isinstance(data.get("armored", False), bool):
        errors.append(f"{label}.armored: true or false")
    info = validate_animations(data.get("animations"), resources, label, ("idle", "run", "attack"), errors)
    pose = first_cell(info, "attack")
    # Lot 2b, all optional: awareness, panic, a lobbed grenade and cries.
    for key, low, high in (("sight", 0, 30), ("hearing", 0, 30), ("surprise", 0, 5), ("alarm", 0, 30)):
        errors.number(obj(data, "awareness"), key, low, high, label + ".awareness", {"sight": 8, "hearing": 2.5, "surprise": .6, "alarm": 5}[key])
    panic = obj(data, "panic")
    for key, low, high, default in (("health", 0, 1, 0), ("radius", 0, 30, 6), ("time", .2, 10, 2), ("speed", .2, 4, 1.3)):
        errors.number(panic, key, low, high, label + ".panic", default)
    errors.number(panic, "allies", 0, 20, label + ".panic", 0, True)
    if not isinstance(panic.get("returns", True), bool):
        errors.append(f"{label}.panic.returns: true or false")
    grenade = obj(data, "grenade")
    if grenade.get("range", 0):
        gl = label + ".grenade"
        reach = errors.number(grenade, "range", 1, 30, gl)
        errors.number(grenade, "minRange", 0, reach or 30, gl, 3)
        for key, low, high, default in (("cooldown", .3, 30, 4), ("first", 0, 30, 1.5), ("throwY", 1, 30, 8), ("maxX", 1, 30, 9), ("radius", .3, 6, 1.6),
                                        ("fuse", .2, 10, 2.5), ("pose", 0, 2, .4)):
            errors.number(grenade, key, low, high, gl, default)
        errors.number(grenade, "damage", 1, 10, gl, 1, True)
        errors.number(grenade, "bounces", 0, 3, gl, 0, True)
    terrain = obj(data, "terrain")
    errors.number(terrain, "climb", 0, 4, label + ".terrain", 1.7)
    errors.number(terrain, "leap", 1, 30, label + ".terrain", 10)
    if not isinstance(terrain.get("follow", False), bool):
        errors.append(f"{label}.terrain.follow: true or false")
    cries = data.get("cries", [])
    if not isinstance(cries, list) or len(cries) > 8:
        errors.append(f"{label}.cries: a list of at most eight sounds")
    else:
        for cry in cries:
            if not isinstance(cry, str) or not cry.startswith("Audio/") or not any((resources / (cry + suffix + ext)).is_file() for suffix in ("", "-a") for ext in SOUNDS):
                errors.append(f"{label}.cries: missing sound {cry}")
    if behavior in ("melee", "thrower"):
        melee = obj(data, "melee")
        errors.number(melee, "reach", .1, 5, label + ".melee")
        errors.number(melee, "height", 0, 5, label + ".melee")
        errors.number(melee, "cooldown", .05, 20, label + ".melee")
        errors.number(melee, "damage", 1, 10, label + ".melee", 0, True)
        if melee.get("sound"):
            errors.resource(melee["sound"], "Audio/", SOUNDS, resources, label + ".melee.sound")
    if behavior == "thrower":
        throw, tl = obj(data, "throw"), label + ".throw"
        errors.number(throw, "range", .5, 30, tl)
        errors.number(throw, "cooldown", .1, 20, tl)
        errors.number(throw, "aimHeight", 0, 5, tl)
        errors.per_difficulty(throw, "speed", 1, 60, tl)
        errors.number(throw, "damage", 1, 10, tl, 0, True)
        if throw.get("projectile") not in LOOKS:
            errors.append(f"{tl}.projectile: one of {', '.join(LOOKS)}")
        if pose is not None and not has_anchor(info, "hand", pose):
            errors.append(f"{label}: a hand anchor is required on the attack cell")
    if behavior == "gunman":
        gun, gl = obj(data, "gun"), label + ".gun"
        for key, low, high in (("range", .5, 30), ("aimTime", 0, 5), ("cooldown", .1, 20), ("burstInterval", .02, 2), ("spread", -45, 45),
                               ("spreadStep", -45, 45), ("life", .05, 5), ("pose", 0, 2), ("shake", 0, 1)):
            errors.number(gun, key, low, high, gl)
        errors.number(gun, "burst", 1, 12, gl, 0, True)
        errors.number(gun, "pellets", 1, 9, gl, 0, True)
        errors.number(gun, "damage", 1, 10, gl, 0, True)
        errors.per_difficulty(gun, "speed", 1, 60, gl)
        if gun.get("projectile") not in LOOKS:
            errors.append(f"{gl}.projectile: one of {', '.join(LOOKS)}")
        errors.resource(gun.get("sound"), "Audio/", SOUNDS, resources, gl + ".sound")
        if not isinstance(gun.get("heavyFlash", False), bool):
            errors.append(f"{gl}.heavyFlash: true or false")
        if pose is not None and not has_anchor(info, "muzzle", pose):
            errors.append(f"{label}: a muzzle anchor is required on the attack cell")
    return errors


def validate_boss_sprite(art, resources, label, errors):
    """BossSprite.Validate: frames cells of a sheet from sprite.cell on; absent (or without a texture) means not drawn."""
    if art is None:
        return False
    if not isinstance(art, dict):
        errors.append(f"{label}: object required")
        return False
    cell = art.get("sprite")
    if not isinstance(cell, dict) or not cell.get("texture"):
        return False
    validate_cell(cell, resources, label + ".sprite", errors)
    frames = errors.number(art, "frames", 1, 16, label, 1, True)
    columns, rows, first = cell.get("columns", 1), cell.get("rows", 1), cell.get("cell", 0)
    if frames and all(isinstance(v, int) for v in (columns, rows, first)) and first + frames > columns * rows:
        errors.append(f"{label}.frames: the frames must stay inside the grid")
    errors.number(art, "fps", .5, 60, label, 12)
    errors.number(art, "size", .1, 10, label, 1)
    errors.number(art, "y", -3, 10, label, 0)
    return True


def validate_lane(entry, library, label, errors):
    """MissionBoss.lane: a flyer kept between left and right of the arena centre, on the line y; absent or right <= left means none."""
    lane = entry.get("lane")
    if lane is None:
        return
    if not isinstance(lane, dict):
        errors.append(f"{label}.lane: object required")
        return
    left, right, y = (errors.number(lane, key, low, high, label + ".lane") for key, low, high in (("left", -10, 10), ("right", -10, 10), ("y", 0, 12)))
    if left is None or right is None or right <= left:
        return
    if right - left < 1:
        errors.append(f"{label}.lane: right at least 1 unit beyond left")
    boss = library["bosses"].get(entry.get("id"))
    if isinstance(boss, dict) and boss.get("behavior") == "walker":
        errors.append(f"{label}.lane: only for a flyer")


def validate_boss(data, resources, enemies=None):
    errors = Errors()
    label = f"boss {data.get('id')}"
    behavior = data.get("behavior")
    if behavior not in BOSS_BEHAVIORS:
        errors.append(f"{label}.behavior: one of {', '.join(BOSS_BEHAVIORS)}")
    for key, limit in (("name", 30), ("intro", 80), ("rage", 80), ("defeat", 80)):
        errors.text(data, key, limit, label)
    errors.per_difficulty(data, "health", 10, 100000, label)
    errors.number(data, "score", 0, 100000, label, 10000, True)
    for key, low, high in (("healthPerExtraPlayer", 0, 5), ("ragePace", .1, 2), ("rageDelay", 0, 10), ("firstAttack", 0, 10), ("smokeSize", 0, 5)):
        errors.number(data, key, low, high, label)
    if not isinstance(data.get("flash", False), bool):
        errors.append(f"{label}.flash: true or false")
    shadow = obj(data, "shadow")
    errors.number(shadow, "x", 0, 20, label + ".shadow")
    errors.number(shadow, "y", 0, 5, label + ".shadow")
    walker = behavior == "walker"
    info = validate_animations(data.get("animations"), resources, label, ("idle", "walk") if walker else ("idle",), errors)
    names = {a.get("name") for a in info["anchors"]} if info else set()
    states = info["states"] if info else {}

    def anchor(name, where):
        if not isinstance(name, str) or name not in names:
            errors.append(f"{where}: anchor {name} is not defined in the animations")
    anchor("smoke", label)
    if walker:
        walk, wl = obj(data, "walk"), label + ".walk"
        errors.per_difficulty(walk, "speed", .1, 30, wl)
        for key, low, high in (("stopDistance", 0, 20), ("margin", 0, 10), ("stepLength", .1, 10), ("stepShake", 0, 1), ("entry", -8, 10)):
            errors.number(walk, key, low, high, wl)
        anchor(walk.get("foot"), wl)
    else:
        flight, fl = obj(data, "flight"), label + ".flight"
        errors.per_difficulty(flight, "speed", .05, 20, fl)
        for key, low, high in (("rageCatchUp", 0, 5), ("sweep", 0, 20), ("sweepMargin", 0, 20), ("altitude", 0, 12), ("bob", 0, 5), ("bobRate", 0, 20),
                               ("roll", 0, 20), ("rollRate", 0, 20)):
            errors.number(flight, key, low, high, fl)
        validate_boss_sprite(flight.get("wake"), resources, fl + ".wake", errors)
        rotor = obj(data, "rotor")
        errors.number(rotor, "width", 0, 20, label + ".rotor")
        errors.number(rotor, "height", 0, 2, label + ".rotor")
        errors.number(rotor, "speed", 0, 500, label + ".rotor")
    hitbox, hl = obj(data, "hitbox"), label + ".hitbox"
    errors.number(hitbox, "count", 1, 12, hl, 0, True)
    errors.number(hitbox, "radius", .1, 10, hl)
    for key, limit in (("x", 20), ("y", 20), ("stepX", 10), ("stepY", 10)):
        errors.number(hitbox, key, -limit, limit, hl)
    death, dl = obj(data, "death"), label + ".death"
    for key, low, high in (("fall", 0, 10), ("spin", -360, 360), ("spreadX", 0, 10), ("spreadY", 0, 10)):
        errors.number(death, key, low, high, dl)
    attacks = data.get("attacks")
    if not isinstance(attacks, list) or not 1 <= len(attacks) <= 12:
        errors.append(f"{label}.attacks: one to twelve attacks")
        attacks = []
    bombs = False
    for i, attack in enumerate(attacks):
        al = f"{label}.attacks[{i}]"
        if not isinstance(attack, dict) or attack.get("type") not in ATTACKS:
            errors.append(f"{al}.type: one of {', '.join(ATTACKS)}")
            continue
        kind = attack["type"]
        errors.text(attack, "announce", 80, al)
        errors.number(attack, "cooldown", 0, 20, al)
        if attack.get("rageCooldown", -1) != -1:
            errors.number(attack, "rageCooldown", 0, 20, al)
        for key in ("state", "aimState", "recoverState"):
            if attack.get(key) and attack[key] not in states:
                errors.append(f"{al}.{key}: state {attack[key]} is not in the animations")
        for key in ("sound", "startSound", "impactSound"):
            if attack.get(key):
                errors.resource(attack[key], "Audio/", SOUNDS, resources, f"{al}.{key}")
        drawn = {key: validate_boss_sprite(attack.get(key), resources, f"{al}.{key}", errors) for key in ("projectile", "warning", "impact")}
        if kind != "gun" and drawn["projectile"]:
            errors.append(f"{al}.projectile: only for a gun attack")
        if kind != "bombs" and (drawn["warning"] or drawn["impact"] or attack.get("impactSound")):
            errors.append(f"{al}: warning, impact and impactSound are only for a bombs attack")
        if kind == "gun":
            errors.per_difficulty(attack, "aimTime", 0, 10, al)
            errors.per_difficulty(attack, "interval", .02, 5, al)
            errors.per_difficulty(attack, "speed", 1, 60, al)
            for key, low, high in (("duration", .05, 20), ("sweep", 0, 20), ("sweepRate", 0, 50), ("aimHeight", 0, 5), ("sightWidth", 0, 20)):
                errors.number(attack, key, low, high, al)
            muzzles = attack.get("muzzles")
            if not isinstance(muzzles, list) or not 1 <= len(muzzles) <= 8:
                errors.append(f"{al}.muzzles: one to eight anchors")
            else:
                for name in muzzles:
                    anchor(name, al)
            errors.number(attack, "damage", 1, 10, al, 0, True)
        elif kind == "bombs":
            bombs = True
            errors.per_difficulty(attack, "count", 1, 20, al, True)
            errors.per_difficulty(attack, "delay", 0, 10, al)
            errors.per_difficulty(attack, "flight", .05, 10, al)
            errors.number(attack, "rageExtra", 0, 10, al, 0, True)
            for key, low, high in (("duration", 0, 20), ("launch", 0, 20), ("spacing", 0, 10), ("stagger", 0, 5), ("arc", -20, 20),
                                   ("blastRadius", .1, 10), ("blastHeight", 0, 10), ("marker", 0, 20)):
                errors.number(attack, key, low, high, al)
            anchor(attack.get("rack"), al)
            errors.number(attack, "damage", 1, 10, al, 0, True)
        elif kind == "charge":
            if not walker:
                errors.append(f"{al}: charge is a move of a boss on the ground (walker)")
            for key, low, high in (("windup", 0, 10), ("recover", 0, 10), ("clearance", 0, 6), ("shake", 0, 1)):
                errors.number(attack, key, low, high, al)
            errors.number(attack, "reach", .2, 8, al, 1.5)
            errors.per_difficulty(attack, "speed", 1, 60, al)
            errors.number(attack, "damage", 1, 10, al, 0, True)
        elif kind == "troops":
            troops = attack.get("enemies")
            if not isinstance(troops, list) or not 1 <= len(troops) <= 6:
                errors.append(f"{al}.enemies: one to six library enemy ids")
            elif enemies is not None:
                for enemy in troops:
                    if enemy not in enemies:
                        errors.append(f"{al}.enemies: unknown enemy {enemy}")
            errors.per_difficulty(attack, "count", 1, 8, al, True)
            errors.per_difficulty(attack, "interval", .05, 5, al)
            errors.number(attack, "duration", 0, 20, al)
            errors.number(attack, "spread", 0, 10, al, 3)
            errors.number(attack, "maxAlive", 1, 12, al, 4, True)
            anchor(attack.get("door"), al)
        else:
            for key, low, high in (("windup", 0, 10), ("repeatWindup", 0, 10), ("recover", 0, 10), ("waveReach", .05, 5), ("waveHeight", 0, 5), ("shake", 0, 1)):
                errors.number(attack, key, low, high, al)
            errors.per_difficulty(attack, "speed", 1, 60, al)
            anchor(attack.get("foot"), al)
            errors.number(attack, "repeat", 1, 6, al, 0, True)
            errors.number(attack, "rageRepeat", 1, 6, al, 0, True)
            errors.number(attack, "damage", 1, 10, al, 0, True)
    if bombs:
        missile, ml = obj(data, "missile"), label + ".missile"
        errors.resource(missile.get("texture"), "Art/", IMAGES, resources, ml + ".texture")
        values = [errors.number(missile, k, 0, 1, ml) for k in ("x", "y", "width", "height")]
        if None not in values and (values[2] <= 0 or values[3] <= 0 or values[0] + values[2] > 1.0001 or values[1] + values[3] > 1.0001):
            errors.append(f"{ml}: x, y, width and height are fractions of the image")
        errors.number(missile, "size", .1, 10, ml)
        errors.number(missile, "hitRadius", .05, 3, ml)
        errors.number(missile, "tipRadius", 0, 3, ml)
        errors.number(missile, "hitLength", 0, 3, ml)
        outline = missile.get("outline", [])
        if not isinstance(outline, list) or outline and not 3 <= len(outline) <= 64:
            errors.append(f"{ml}.outline: three to 64 points")
        else:
            for j, point in enumerate(outline):
                errors.number(point if isinstance(point, dict) else {}, "x", 0, 1, f"{ml}.outline[{j}]")
                errors.number(point if isinstance(point, dict) else {}, "y", 0, 1, f"{ml}.outline[{j}]")
    return errors


def validate_difficulty(data, resources):
    """DifficultyDefinition.Validate: a lobby difficulty, its column of the content's per-difficulty lists and its own rules."""
    errors = Errors()
    label = f"difficulty {data.get('id')}"
    errors.text(data, "name", 14, label)
    errors.text(data, "tagline", 70, label, required=False)
    errors.number(data, "order", 0, 99, label, 0, True)
    errors.number(data, "column", 0, 3, label, 0, True)
    errors.number(data, "health", 1, 20, label, 5, True)
    errors.number(data, "lives", 0, 20, label, 2, True)
    errors.number(data, "damageTaken", 1, 10, label, 1, True)
    if not isinstance(data.get("livesPerPlayer", False), bool):
        errors.append(f"{label}.livesPerPlayer: true or false")
    for key in ("enemyHealth", "enemyCount", "enemySpeed", "enemyCadence", "gunmanAim"):
        errors.number(data, key, .1, 10, label, 1)
    errors.number(data, "hurtInvulnerable", 0, 10, label, .75)
    reward = obj(data, "waveReward")
    errors.number(reward, "every", 0, 20, label + ".waveReward", 0, True)
    errors.number(reward, "lives", 0, 10, label + ".waveReward", 0, True)
    errors.number(reward, "health", 0, 20, label + ".waveReward", 0, True)
    respawn = obj(data, "respawn")
    errors.number(respawn, "delay", .1, 10, label + ".respawn", 2.5)
    errors.number(respawn, "invulnerable", 0, 10, label + ".respawn", 3)
    if not isinstance(respawn.get("inPlace", False), bool):
        errors.append(f"{label}.respawn.inPlace: true or false")
    cont = obj(data, "continues")
    if not isinstance(cont.get("enabled", False), bool):
        errors.append(f"{label}.continues.enabled: true or false")
    elif cont.get("enabled", False):
        seconds = errors.number(cont, "seconds", 1, 60, label + ".continues", 10)
        errors.number(cont, "grace", 0, seconds if seconds is not None else 60, label + ".continues", 1)
        errors.number(cont, "lives", 0, 20, label + ".continues", 0, True)
    return errors


# Weapons first: heroes and rules name them.
VALIDATORS = {"weapons": validate_weapon, "heroes": validate_hero, "enemies": validate_enemy, "bosses": validate_boss, "rules": validate_rules,
              "difficulties": validate_difficulty, "hostages": validate_hostage, "carriers": validate_carrier, "props": validate_prop,
              "vehicles": validate_vehicle}
NEEDS_WEAPONS = ("heroes", "rules", "hostages", "props", "vehicles")


def load_library(resources, extra=None, kinds=None):
    """Read Content/<kind>/*.json, resolve "extends" and validate. Returns (library, errors);
    extra maps a kind to additional raw entries (dicts), as ContentLibrary.AddEnemy/AddBoss do."""
    library, errors = {kind: {} for kind in VALIDATORS}, []
    for kind, validate in VALIDATORS.items():
        if kinds is not None and kind not in kinds:
            continue
        sources = {}
        for file in sorted((resources / "Content" / kind).glob("*.json")):
            try:
                data = json.loads(file.read_text(encoding="utf-8"))
            except ValueError as exc:
                errors.append(f"Content/{kind}/{file.name}: {exc}")
                continue
            if not isinstance(data, dict) or data.get("id") != file.stem:
                errors.append(f"Content/{kind}/{file.name}: id must be the file name")
                continue
            if not ID.fullmatch(file.stem):
                errors.append(f"Content/{kind}/{file.name}: id must be lowercase words separated by hyphens")
                continue
            sources[file.stem] = data
        for data in (extra or {}).get(kind, []):
            sources[data.get("id")] = data

        def resolve(entry_id, depth=0):
            data = sources[entry_id]
            parent = data.get("extends")
            if not parent:
                return data
            if depth > 8 or parent == entry_id or parent not in sources:
                raise ValueError(f"extends {parent}: unknown entry or loop")
            return merge(resolve(parent, depth + 1), data)
        for entry_id in sources:
            try:
                data = resolve(entry_id)
            except ValueError as exc:
                errors.append(f"Content/{kind}/{entry_id}: {exc}")
                continue
            found = validate(data, resources, library["weapons"]) if kind in NEEDS_WEAPONS else \
                validate(data, resources, library["enemies"]) if kind == "bosses" else validate(data, resources)
            errors.extend(f"Content/{kind}/{entry_id}: {e}" for e in found)
            library[kind][entry_id] = data
    if kinds is not None:
        return library, errors
    # As many heroes as there are files: the menu has one card per player, each scrolls through all of them.
    if not 1 <= len(library["heroes"]) <= MAX_HEROES:
        errors.append(f"Content/heroes: one to {MAX_HEROES} heroes are required")
    if "default" not in library["rules"]:
        errors.append("Content/rules/default.json is required")
    if not 1 <= len(library["difficulties"]) <= 6:
        errors.append("Content/difficulties: one to six difficulties are required")
    return library, errors


def validate_mission(data, resources, library=None):
    if library is None:
        library, _ = load_library(resources)
    errors = Errors()
    if not isinstance(data, dict):
        return ["The mission must be a JSON object"]
    if data.get("schemaVersion") != SCHEMA:
        errors.append(f"schemaVersion must be {SCHEMA}")
    errors.number(data, "order", 0, 999, "mission", 1, True)
    for key, limit in {"id": 60, "title": 45, "location": 45, "intro": 80, "exitLabel": 40, "winTitle": 80}.items():
        value = data.get(key)
        if not isinstance(value, str) or not value.strip() or len(value) > limit:
            errors.append(f"{key}: non-empty string, maximum {limit} characters")
    if not ID.fullmatch(str(data.get("id", ""))):
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
    code = data.get("code")
    if code is not None and (not isinstance(code, str) or not re.fullmatch(r"[A-Z0-9]{4,10}", code)):
        errors.append("code must be 4 to 10 uppercase letters or digits")
    errors.resource(data.get("music"), "Audio/", SOUNDS, resources, "music")
    if data.get("logo") is not None:
        errors.resource(data.get("logo"), "Art/", IMAGES, resources, "logo")
    game_map = data.get("map")
    if not isinstance(game_map, dict):
        errors.append("map must be an object")
        game_map = {}
    backgrounds = game_map.get("backgrounds")
    if not isinstance(backgrounds, list) or not 1 <= len(backgrounds) <= 8:
        errors.append("map.backgrounds must contain one to eight resource paths")
    else:
        for path in backgrounds:
            errors.resource(path, "Art/", IMAGES, resources, "map.backgrounds")
    errors.resource(game_map.get("ground"), "Art/", IMAGES, resources, "map.ground")
    # Framing (drawing only): walking line in the panoramas, band of the ground image in front of the actors.
    errors.number(game_map, "horizon", 0, 0.6, "map", default=0.2)
    errors.number(game_map, "groundBand", 0.02, 1, "map", default=0.2)
    errors.number(game_map, "groundTop", -6, 3, "map", default=0)
    errors.number(game_map, "groundFade", 0, 1, "map", default=0.22)
    for key in RESERVED_MAP:
        if game_map.get(key) not in (None, []):
            errors.append(f"map.{key} is reserved for a later batch: leave the list empty")
    validate_decor(game_map.get("decor", []), data.get("length", 80), resources, errors)
    terrain = validate_terrain(game_map.get("terrain", []), data, resources, errors)
    rules = data.get("rules", "default")
    if rules not in library["rules"]:
        errors.append(f"rules: unknown rules {rules} (Content/rules/{rules}.json)")
    validate_table(data.get("weapons"), library["weapons"], "weapons", False, errors)
    errors.number(data, "parTime", 0, 3600, "mission", 0)
    enemies = data.get("enemies")
    roster = 0
    if not isinstance(enemies, list) or not 1 <= len(enemies) <= 8:
        errors.append("enemies must list one to eight library enemy ids")
    else:
        roster = len(enemies)
        for enemy in enemies:
            if enemy not in library["enemies"]:
                errors.append(f"enemies: unknown enemy {enemy} (Content/enemies/{enemy}.json)")
    extra = data.get("extraEnemy", {})
    if not isinstance(extra, dict):
        errors.append("extraEnemy must be an object")
    else:
        errors.number(extra, "enemy", -1, max(roster - 1, -1), "extraEnemy", -1, True)
        errors.number(extra, "every", 1, 30, "extraEnemy", 4, True)
        errors.number(extra, "fromDifficulty", 0, 3, "extraEnemy", 1, True)
    boss = data.get("boss", {})
    if not isinstance(boss, dict):
        errors.append("boss must be an object")
    else:
        if not isinstance(boss.get("enabled", True), bool):
            errors.append("boss.enabled must be true or false")
        if boss.get("enabled", True):
            if boss.get("id") not in library["bosses"]:
                errors.append(f"boss.id: unknown boss {boss.get('id')} (Content/bosses/<id>.json)")
            for key, limit in {"name": 30, "intro": 80, "rage": 80, "defeat": 80}.items():
                errors.text(boss, key, limit, "boss", required=False)
            health = boss.get("health", 1)
            if isinstance(health, bool) or not isinstance(health, (int, float)) or not math.isfinite(health) or not .25 <= health <= 4:
                errors.append("boss.health must be between .25 and 4")
            validate_lane(boss, library, "boss", errors)
            errors.resource(data.get("bossMusic"), "Audio/", SOUNDS, resources, "bossMusic")
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
        for key, lower, upper in (("baseEnemies", 0, 100), ("enemiesPerPlayer", 0, 20), ("maxAlive", 1, 30)):
            value = wave.get(key)
            if type(value) is not int or not lower <= value <= upper:
                errors.append(f"wave {i}: {key} must be an integer between {lower} and {upper}")
        interval = wave.get("spawnInterval")
        if isinstance(interval, bool) or not isinstance(interval, (int, float)) or not math.isfinite(interval) or not .1 <= interval <= 10:
            errors.append(f"wave {i}: spawnInterval must be between .1 and 10")
        pattern = wave.get("enemyPattern")
        if not isinstance(pattern, list) or not 1 <= len(pattern) <= 30 or any(type(v) is not int or not 0 <= v < roster for v in pattern):
            errors.append(f"wave {i}: enemyPattern must use enemy types 0 to {roster - 1}")
        if not isinstance(wave.get("lock", True), bool):
            errors.append(f"wave {i}: lock must be true or false")
    validate_map_hostages(game_map.get("hostages"), waves, length, library.get("rules", {}).get(rules), library, errors, terrain)
    validate_map_props(game_map.get("props", []), waves, data, terrain, library.get("rules", {}).get(rules), library, errors)
    validate_encounters(data.get("encounters"), waves, data, length, exit_x, library.get("rules", {}).get(rules), library, errors)
    validate_map_vehicles(game_map.get("vehicles", []), data, terrain, library, errors)
    return errors


def validate_map_vehicles(vehicles, data, terrain, library, errors):
    """MissionDefinition.ValidateVehicles: at most eight vehicles of the library parked in the mission, outside the boss
    arenas and off the pits."""
    if vehicles in (None, []):
        return
    if not isinstance(vehicles, list) or len(vehicles) > 8:
        errors.append("map.vehicles: a list of at most 8 vehicles")
        return
    length = data.get("length", 80) if isinstance(data.get("length", 80), (int, float)) else 80
    for i, place in enumerate(vehicles):
        label = f"map.vehicles[{i}]"
        if not isinstance(place, dict):
            errors.append(f"{label}: object required")
            continue
        kind = library.get("vehicles", {}).get(place.get("id"))
        if kind is None:
            errors.append(f"{label}.id: unknown vehicle {place.get('id')} (Content/vehicles/<id>.json)")
            continue
        half = obj(kind, "box").get("width", 1) * .5
        x = errors.number(place, "x", 1 + half, length - 1 - half, label)
        if not isinstance(place.get("flip", False), bool):
            errors.append(f"{label}.flip: true or false")
        if x is None:
            continue
        where = arena(data, x - half, x + half)
        if where:
            errors.append(f"{label}: no vehicle parked in {where}")
        for piece in terrain or ():
            if isinstance(piece, dict) and piece.get("type") == "pit" and x + half > piece.get("x", 0) and x - half < piece.get("x", 0) + piece.get("width", 0):
                errors.append(f"{label}: not over a pit")


def validate_decor(decor, length, resources, errors):
    """MissionDefinition.Validate: street scenery, drawn only, the bottom centre of a sheet's cell at (x, y)."""
    if not isinstance(decor, list) or len(decor) > 120:
        errors.append("map.decor: a list of at most 120 items")
        return
    span = length if isinstance(length, (int, float)) and not isinstance(length, bool) else 80
    for i, item in enumerate(decor):
        label = f"map.decor[{i}]"
        if not isinstance(item, dict):
            errors.append(f"{label}: object required")
            continue
        validate_cell(item.get("sprite"), resources, label + ".sprite", errors)
        errors.number(item, "x", -10, span + 10, label)
        errors.number(item, "y", -3, 8, label, 0)
        errors.number(item, "height", .2, 12, label, 2)
        for key in ("flip", "front"):
            if not isinstance(item.get(key, False), bool):
                errors.append(f"{label}.{key}: true or false")


def validate_encounters(encounters, waves, data, length, exit_x, rules, library, errors):
    """MissionDefinition.ValidateEncounters: triggers, entries seen from the team's screen, trucks, mid-mission bosses."""
    if encounters in (None, []):
        return
    if not isinstance(encounters, list) or len(encounters) > 30:
        errors.append("encounters: a list of at most 30 encounters")
        return
    length, exit_x = length or 500, exit_x or 500
    openings = [o.get("id") for o in (obj(rules or {}, "stage").get("openings") or []) if isinstance(o, dict)]
    bosses = 0
    for i, c in enumerate(encounters):
        label = f"encounters[{i}]"
        if not isinstance(c, dict):
            errors.append(f"{label}: object required")
            continue
        wave = errors.number(c, "wave", -1, len(waves) - 1, label, -1, True)
        if c.get("on", "start") not in ("start", "clear"):
            errors.append(f"{label}.on: start or clear")
        errors.text(c, "announce", 40, label, required=False)
        if not isinstance(c.get("lock", False), bool):
            errors.append(f"{label}.lock: true or false")
        if wave is None:
            continue
        if wave >= 0 and isinstance(waves[wave], dict):
            at = waves[wave].get("atX", 0)
        else:
            at = errors.number(c, "atX", 1, exit_x - 1.0001, label)
            if at is None:
                continue
        spawns = c.get("spawns", [])
        truck = obj(c, "truck")
        boss = obj(c, "boss")
        if boss.get("id"):
            if wave >= 0 or spawns or c.get("lock") or truck.get("id"):
                errors.append(f"{label}: a mid-mission boss starts on its own at atX (no wave, spawns, lock or truck)")
            if at > exit_x - 30:
                errors.append(f"{label}: a mid-mission boss stands at least 30 units before the exit")
            bosses += 1
            if boss["id"] not in library["bosses"]:
                errors.append(f"{label}.boss.id: unknown boss {boss['id']}")
            for key, limit in {"name": 30, "intro": 80, "rage": 80, "defeat": 80}.items():
                errors.text(boss, key, limit, label + ".boss", required=False)
            errors.number(boss, "health", .25, 4, label + ".boss", 1)
            validate_lane(boss, library, label + ".boss", errors)
            for w in waves:
                if isinstance(w, dict) and isinstance(w.get("atX"), (int, float)) and w["atX"] - 10 < at < w["atX"] + 5:
                    errors.append(f"{label}: a mid-mission boss stands outside a wave's screen (atX - 10 to atX + 5, {w['atX']})")
            continue
        if not isinstance(spawns, list) or not 1 <= len(spawns) <= 16:
            errors.append(f"{label}.spawns: one to sixteen")
            continue
        low, high = (at - 15, at + 2.5) if wave >= 0 else (at - 7, at + 12)
        if truck.get("id"):
            if truck["id"] not in library.get("carriers", {}):
                errors.append(f"{label}.truck.id: unknown carrier {truck['id']} (Content/carriers/<id>.json)")
            if truck.get("side", "right") not in ("left", "right"):
                errors.append(f"{label}.truck.side: left or right")
            x = errors.number(truck, "x", 0, length, label + ".truck")
            if x is not None and not low <= x <= high:
                errors.append(f"{label}.truck.x: inside the encounter's screen ({low} to {high})")
        for j, s in enumerate(spawns):
            sl = f"{label}.spawns[{j}]"
            if not isinstance(s, dict):
                errors.append(f"{sl}: object required")
                continue
            kind = library["enemies"].get(s.get("enemy"))
            if kind is None:
                errors.append(f"{sl}.enemy: unknown enemy {s.get('enemy')}")
            entry = s.get("entry", "walk")
            if entry not in ENTRIES:
                errors.append(f"{sl}.entry: one of {', '.join(ENTRIES)}")
                continue
            x = errors.number(s, "x", 1, length - 1, sl)
            y = errors.number(s, "y", 0, 8, sl)
            errors.number(s, "delay", 0, 30, sl)
            errors.number(s, "facing", -1, 1, sl, 0, True)
            if not isinstance(s.get("unaware", False), bool):
                errors.append(f"{sl}.unaware: true or false")
            if s.get("side") not in (None, "", "left", "right"):
                errors.append(f"{sl}.side: left or right")
            if y and kind is not None and kind.get("behavior") == "melee" and not obj(kind, "grenade").get("range"):
                errors.append(f"{sl}: a close-combat enemy stays in the street (y 0)")
            if x is None:
                continue
            if entry == "none":
                if wave >= 0 or c.get("lock") or not at + 14 <= x <= at + 40:
                    errors.append(f"{sl}: entry none only serves an encounter started at atX, without lock, between atX + 14 and atX + 40 (off screen)")
            elif not low <= x <= high:
                errors.append(f"{sl}: inside the encounter's screen ({low} to {high})")
            if entry in ("walk", "truck") and y:
                errors.append(f"{sl}: walk and truck arrive in the street (y 0)")
            if entry == "truck" and not truck.get("id"):
                errors.append(f"{sl}: entry truck needs the encounter's truck")
            if entry in ("door", "window"):
                fx = errors.number(s, "fromX", 0, length, sl)
                fy = errors.number(s, "fromY", 0, 8, sl)
                if fx is not None and not low <= fx <= high:
                    errors.append(f"{sl}.fromX: inside the encounter's screen ({low} to {high})")
                if y and fy is not None and abs(y - fy) > .01:
                    errors.append(f"{sl}: from a door or a window it comes down to the street (y 0) or stays (y = fromY)")
                if s.get("opening") and s["opening"] != "none" and s["opening"] not in openings:
                    errors.append(f"{sl}.opening: {s['opening']} is not in the rules' stage.openings")
            elif s.get("opening"):
                errors.append(f"{sl}.opening: only for the door and window entries")
    if bosses > 3:
        errors.append("encounters: three mid-mission bosses at most")


def arena(data, left, right):
    """MissionDefinition.InArena: the boss arenas stay flat (the end boss from exitX - 30, mid-mission bosses atX - 12.5 to atX + 15)."""
    boss = data.get("boss", {}) if isinstance(data.get("boss", {}), dict) else {}
    exit_x = data.get("exitX", 77)
    if boss.get("enabled", True) and isinstance(exit_x, (int, float)) and right > exit_x - 30:
        return "the boss arena (from exitX - 30)"
    for c in data.get("encounters") or []:
        if isinstance(c, dict) and isinstance(c.get("boss"), dict) and c["boss"].get("id"):
            at = c.get("atX", 0)
            if isinstance(at, (int, float)) and right > at - 12.5 and left < at + 15:
                return f"the mid-mission boss arena at {at} (atX - 12.5 to atX + 15)"
    return None


def validate_terrain(terrain, data, resources, errors):
    """MissionDefinition.ValidateTerrain: platforms, blocks, slopes and pits inside the mission, drawn, outside the boss arenas,
    solid pieces side by side. Returns the valid pieces, for what stands on them."""
    if terrain in (None, []):
        return []
    if not isinstance(terrain, list) or len(terrain) > 60:
        errors.append("map.terrain: a list of at most 60 pieces")
        return []
    length = data.get("length", 80) if isinstance(data.get("length", 80), (int, float)) else 80
    pieces = []
    for i, t in enumerate(terrain):
        label = f"map.terrain[{i}]"
        if not isinstance(t, dict):
            errors.append(f"{label}: object required")
            continue
        kind = t.get("type")
        if kind not in TERRAIN_TYPES:
            errors.append(f"{label}.type: one of {', '.join(TERRAIN_TYPES)}")
            continue
        x = errors.number(t, "x", 1, length - 1, label)
        width = errors.number(t, "width", .5, 60, label)
        if x is None or width is None:
            continue
        if x + width > length - 1:
            errors.append(f"{label}: x + width inside the mission")
        height = t.get("height", 0)
        to = t.get("toHeight", 0)
        if kind == "platform":
            height = errors.number(t, "height", .4, 6, label)
        elif kind == "block":
            height = errors.number(t, "height", .2, 4, label)
        elif kind == "slope":
            height = errors.number(t, "height", 0, 4, label)
            to = errors.number(t, "toHeight", 0, 4, label)
            if height is not None and to is not None and abs(to - height) > width:
                errors.append(f"{label}: a slope rises at most one unit per unit")
        else:
            height = errors.number(t, "height", .3, 8, label)
            errors.number(t, "width", .5, 8, label)
            errors.number(t, "damage", 0, 10, label, 1, True)
        if height is None:
            continue
        for key in ("lethal", "flip", "front"):
            if not isinstance(t.get(key, False), bool):
                errors.append(f"{label}.{key}: true or false")
        if t.get("lethal") and kind != "pit":
            errors.append(f"{label}.lethal: only for pits")
        where = arena(data, x, x + width)
        if where:
            errors.append(f"{label}: no relief in {where}")
        sprite = t.get("sprite")
        drawn = isinstance(sprite, dict) and bool(sprite.get("texture"))
        if kind != "pit" and not drawn:
            errors.append(f"{label}: a piece of relief is drawn (sprite), or nobody sees it")
        if drawn:
            validate_cell(sprite, resources, label + ".sprite", errors)
            errors.number(t, "drawY", -8, 6, label, 0)
            errors.number(t, "drawX", -10, 10, label, 0)
            errors.number(t, "drawWidth", 0, 60, label, 0)
            errors.number(t, "drawHeight", 0, 12, label, 0)
        if kind != "platform":
            for j, other in pieces:
                if other["type"] != "platform" and x < other["x"] + other["width"] - .01 and other["x"] < x + width - .01:
                    errors.append(f"{label}: overlaps map.terrain[{j}] (only platforms overlap)")
        pieces.append((i, {"type": kind, "x": x, "width": width, "height": height, "toHeight": to if kind == "slope" else height}))
    return [p for _, p in pieces]


def top_at(piece, x):
    if piece["type"] == "slope":
        return piece["height"] + (piece["toHeight"] - piece["height"]) * (x - piece["x"]) / piece["width"]
    return piece["height"]


def on_surface(terrain, x, y, ledges=True):
    """MissionDefinition.OnSurface: the street where no solid piece stands, or the top of a piece (a ledge too for hostages)."""
    if abs(y) < .01:
        return not any(t["type"] != "platform" and t["x"] < x < t["x"] + t["width"] for t in terrain)
    return any(t["type"] != "pit" and (ledges or t["type"] != "platform") and t["x"] <= x <= t["x"] + t["width"] and abs(top_at(t, x) - y) < .05
               for t in terrain)


def validate_map_props(props, waves, data, terrain, rules, library, errors):
    """MissionDefinition.ValidateProps: props of the library on the street or on solid relief, outside the boss arenas; an
    objective inside the screen its locked wave holds; rewards the mission's rules know."""
    if props in (None, []):
        return
    if not isinstance(props, list) or len(props) > 60:
        errors.append("map.props: a list of at most 60 props")
        return
    length = data.get("length", 80) if isinstance(data.get("length", 80), (int, float)) else 80
    for i, place in enumerate(props):
        label = f"map.props[{i}]"
        if not isinstance(place, dict):
            errors.append(f"{label}: object required")
            continue
        kind = library["props"].get(place.get("id"))
        if kind is None:
            errors.append(f"{label}.id: unknown prop {place.get('id')} (Content/props/<id>.json)")
            continue
        half = obj(kind, "box").get("width", 1) * .5
        x = errors.number(place, "x", 1 + half, length - 1 - half, label)
        y = errors.number(place, "y", 0, 6, label, 0)
        if x is None or y is None:
            continue
        if not on_surface(terrain, x, y, False):
            errors.append(f"{label}: y on the street (0) or on top of a block or slope there")
        where = arena(data, x - half, x + half)
        if where:
            errors.append(f"{label}: no prop in {where}")
        if not isinstance(place.get("flip", False), bool):
            errors.append(f"{label}.flip: true or false")
        wave = errors.number(place, "wave", -1, len(waves) - 1, label, -1, True)
        if wave is not None and wave >= 0 and isinstance(waves[wave], dict):
            at = waves[wave].get("atX", 0)
            if not waves[wave].get("lock", True) or isinstance(at, (int, float)) and not at - 14 <= x <= at + 2:
                errors.append(f"{label}: a locked wave's objective stands between atX - 14 and atX + 2 ({at})")
        if rules is None:
            continue
        for reward in kind.get("rewards") or []:
            if isinstance(reward, dict) and reward_bonus(rules, reward.get("bonus"), reward.get("weapon")) is None:
                errors.append(f"{label}: reward {reward.get('bonus') or reward.get('weapon')} of {place.get('id')} is not a bonus of rules {rules.get('id')}")
        bonus, weapon = place.get("reward"), place.get("weapon")
        if bonus or weapon:
            check_reward(bonus, weapon, library["weapons"], label, errors)
            if reward_bonus(rules, bonus, weapon) is None:
                errors.append(f"{label}: reward {bonus or weapon} is not a bonus of rules {rules.get('id')}")


def validate_map_hostages(hostages, waves, length, rules, library, errors, terrain=()):
    """MissionDefinition.ValidateHostages: placements, their wave's locked screen and rewards the mission's rules know."""
    if hostages in (None, []):
        return
    if not isinstance(hostages, list) or len(hostages) > 40:
        errors.append("map.hostages: a list of at most 40 hostages")
        return
    for i, place in enumerate(hostages):
        label = f"map.hostages[{i}]"
        if not isinstance(place, dict):
            errors.append(f"{label}: object required")
            continue
        kind = library["hostages"].get(place.get("id"))
        if kind is None:
            errors.append(f"{label}.id: unknown hostage {place.get('id')} (Content/hostages/<id>.json)")
        x = errors.number(place, "x", 1, (length or 500) - 1, label)
        y = errors.number(place, "y", 0, 10, label, 0)
        if x is not None and y is not None and not on_surface(terrain, x, y):
            errors.append(f"{label}: y on the street (0) or on top of a piece of relief there")
        wave = errors.number(place, "wave", -1, len(waves) - 1, label, -1, True)
        if wave is not None and wave >= 0 and x is not None and isinstance(waves[wave], dict):
            at = waves[wave].get("atX", 0)
            if isinstance(at, (int, float)) and not at - 14 <= x <= at + 2:
                errors.append(f"{label}: a wave's hostage stands between atX - 14 and atX + 2 ({at})")
        if rules is None:
            continue
        for j, reward in enumerate((kind or {}).get("rewards") or []):
            if isinstance(reward, dict) and reward_bonus(rules, reward.get("bonus"), reward.get("weapon")) is None:
                errors.append(f"{label}: reward {reward.get('bonus') or reward.get('weapon')} of {place.get('id')} is not a bonus of rules {rules.get('id')}")
        bonus, weapon = place.get("reward"), place.get("weapon")
        if bonus or weapon:
            check_reward(bonus, weapon, library["weapons"], label, errors)
            if reward_bonus(rules, bonus, weapon) is None:
                errors.append(f"{label}: reward {bonus or weapon} is not a bonus of rules {rules.get('id')}")


def find_proper_nouns(root=ROOT):
    """Mission names written in the engine's code: content must stay in data."""
    words = re.compile(r"(?<![A-Za-z0-9-])(" + "|".join(re.escape(w) for w in PROPER_NOUNS) + r")(?![A-Za-z])", re.IGNORECASE)
    hits = []
    for file in sorted((root / "Assets/Scripts").rglob("*.cs")):
        for number, line in enumerate(file.read_text(encoding="utf-8").splitlines(), 1):
            for match in words.finditer(line):
                hits.append(f"{file.relative_to(root)}:{number}: {match.group(0)}")
    return hits


def find_unused_art(root=ROOT):
    """Images under Resources/Art that no mission, content file or engine script names: Unity ships every file under
    Resources whether it is loaded or not, so sources, intermediate sheets and unused variants belong in Assets/ArtSources."""
    resources = root / "Assets/Resources"
    texts = [f.read_text(encoding="utf-8") for folder in ("Levels", "Content") for f in (resources / folder).rglob("*.json")]
    texts += [f.read_text(encoding="utf-8") for f in (root / "Assets/Scripts").rglob("*.cs")]
    names = set(re.findall(r'"(Art/[^"]+)"', "\n".join(texts)))
    unused = []
    for image in sorted((resources / "Art").rglob("*")):
        if image.suffix.lower() in IMAGES:
            name = image.relative_to(resources).with_suffix("").as_posix()
            if name not in names:
                unused.append(name)
    return unused


def main():
    resources = ROOT / "Assets/Resources"
    library, failures = load_library(resources)
    files = sorted((resources / "Levels").glob("*.json"))
    ids = set()
    codes = set()
    if not files:
        failures.append("No missions found")
    for file in files:
        try:
            data = json.loads(file.read_text(encoding="utf-8"))
            errors = validate_mission(data, resources, library)
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
    failures.extend(f"Mission name in engine code: {hit}" for hit in find_proper_nouns())
    failures.extend(f"Image never loaded, move it to Assets/ArtSources: {name}" for name in find_unused_art())
    if failures:
        print("\n".join(failures), file=sys.stderr)
        return 1
    counts = ", ".join(f"{len(library[k])} {k}" for k in VALIDATORS)
    print(f"Validated {len(files)} mission(s) and the library ({counts})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
