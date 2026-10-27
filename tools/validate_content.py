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
HERO_SPECIALS = ("blast", "volley")
# Hero states the engine reads (HeroDefinition.Required), the ones it fires from, and the optional ones.
HERO_STATES = ("idle", "run", "jump", "crouch", "aimUp", "jumpDown", "knife", "throw")
HERO_FIRING = ("idle", "run", "jump", "crouch", "crouchWalk", "crouchShoot", "aimUp", "aimDiagonal", "jumpDown")
BONUS_EFFECTS = ("weapon", "health", "turbo", "score", "grenades")
ENEMY_BEHAVIORS = ("melee", "thrower", "gunman")
BOSS_BEHAVIORS = ("flyer", "walker")
ATTACKS = ("gun", "bombs", "stomp")
RESERVED_MAP = ("terrain", "props", "hostages", "vehicles")
# Words that belong to one mission's content and must never be written in the engine's code.
PROPER_NOUNS = ("Paris", "Parisien", "Macron", "Bastille", "Seine", "Eiffel", "Haussmann", "Marseille", "Marseillaise",
                "Mistral", "Pastis", "Canebière", "Tokyo", "Shogun", "Sakura", "Shibuya", "Akihabara", "Chuck", "Norris",
                "Arnold", "Jean-Claude", "JCVD", "Sylvester", "macron-helicopter", "mecha-shogun", "news-helicopter", "route-0")


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


def validate_hero(data, resources):
    errors = Errors()
    label = f"hero {data.get('id')}"
    for key, limit in (("name", 24), ("shortName", 12), ("role", 24), ("specialName", 30)):
        errors.text(data, key, limit, label)
    errors.number(data, "order", 0, 999, label, 0, True)
    errors.number(data, "speed", 1, 30, label)
    errors.number(data, "fireRate", .02, 2, label)
    errors.number(data, "jump", 1, 30, label)
    errors.number(data, "recoilShake", 0, 1, label)
    errors.number(data, "strideRate", .1, 60, label)
    errors.number(data, "bob", 0, 1, label)
    errors.number(data, "damage", 1, 100, label, 0, True)
    special = obj(data, "special")
    kind = special.get("type")
    if kind not in HERO_SPECIALS:
        errors.append(f"{label}.special.type: one of {', '.join(HERO_SPECIALS)}")
    sl = label + ".special"
    errors.number(special, "cooldown", .5, 60, sl)
    errors.number(special, "cooldownPerDifficulty", 0, 20, sl)
    errors.number(special, "damage", 1, 100, sl, 0, True)
    if kind == "blast":
        errors.number(special, "radius", .5, 15, sl)
        errors.number(special, "bossReach", 0, 10, sl)
        errors.number(special, "invulnerable", 0, 5, sl)
        errors.number(special, "leap", 0, 30, sl)
        errors.number(special, "bossDamage", 0, 500, sl, 0, True)
    elif kind == "volley":
        errors.number(special, "speed", 1, 60, sl)
        errors.number(special, "spread", 0, 10, sl)
        errors.number(special, "shots", 1, 15, sl, 0, True)
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
        if None not in (fx, fy, size) and (fx + size > 1.0001 or fy + size > 1.0001):
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


def validate_rules(data, resources):
    errors = Errors()
    label = f"rules {data.get('id')}"
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
            errors.number(bonus, "weapon", 1, 3, bl, 0, True)
        errors.per_difficulty(bonus, "amount", 1, 100000, bl, True)
        errors.text(bonus, "announce", 30, bl)
        errors.number(bonus, "scale", .2, 5, bl, 1.25)
        voice = bonus.get("voice")
        if voice not in (None, ""):
            errors.resource(voice + "-a" if isinstance(voice, str) else voice, "Audio/", SOUNDS, resources, bl + ".voice")
        validate_cell(bonus.get("icon"), resources, bl + ".icon", errors)
    for key in ("supplies", "drops"):
        cycle = data.get(key)
        if not isinstance(cycle, list) or not 1 <= len(cycle) <= 64 or any(type(v) is not int or not 0 <= v < len(bonuses) for v in cycle):
            errors.append(f"{label}.{key}: one to 64 indices into bonuses")
    validate_cell(data.get("go"), resources, label + ".go", errors)
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
    if behavior in ("melee", "thrower"):
        melee = obj(data, "melee")
        errors.number(melee, "reach", .1, 5, label + ".melee")
        errors.number(melee, "height", 0, 5, label + ".melee")
        errors.number(melee, "cooldown", .05, 20, label + ".melee")
        errors.number(melee, "damage", 1, 10, label + ".melee", 0, True)
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


def validate_boss(data, resources):
    errors = Errors()
    label = f"boss {data.get('id')}"
    behavior = data.get("behavior")
    if behavior not in BOSS_BEHAVIORS:
        errors.append(f"{label}.behavior: one of {', '.join(BOSS_BEHAVIORS)}")
    for key, limit in (("name", 30), ("intro", 80), ("rage", 80), ("defeat", 80)):
        errors.text(data, key, limit, label)
    errors.per_difficulty(data, "health", 10, 100000, label)
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
        for key, low, high in (("stopDistance", 0, 20), ("margin", 0, 10), ("stepLength", .1, 10), ("stepShake", 0, 1), ("entry", 0, 10)):
            errors.number(walk, key, low, high, wl)
        anchor(walk.get("foot"), wl)
    else:
        flight, fl = obj(data, "flight"), label + ".flight"
        errors.per_difficulty(flight, "speed", .05, 20, fl)
        for key, low, high in (("rageCatchUp", 0, 5), ("sweep", 0, 20), ("sweepMargin", 0, 20), ("altitude", 0, 12), ("bob", 0, 5), ("bobRate", 0, 20)):
            errors.number(flight, key, low, high, fl)
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
        if attack.get("sound"):
            errors.resource(attack["sound"], "Audio/", SOUNDS, resources, al + ".sound")
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


VALIDATORS = {"heroes": validate_hero, "enemies": validate_enemy, "bosses": validate_boss, "rules": validate_rules}


def load_library(resources, extra=None):
    """Read Content/<kind>/*.json, resolve "extends" and validate. Returns (library, errors);
    extra maps a kind to additional raw entries (dicts), as ContentLibrary.AddEnemy/AddBoss do."""
    library, errors = {kind: {} for kind in VALIDATORS}, []
    for kind, validate in VALIDATORS.items():
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
            errors.extend(f"Content/{kind}/{entry_id}: {e}" for e in validate(data, resources))
            library[kind][entry_id] = data
    if not 1 <= len(library["heroes"]) <= 4:
        errors.append("Content/heroes: one to four heroes are required")
    if "default" not in library["rules"]:
        errors.append("Content/rules/default.json is required")
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
    for key in RESERVED_MAP:
        if game_map.get(key) not in (None, []):
            errors.append(f"map.{key} is reserved for a later batch: leave the list empty")
    if data.get("encounters") not in (None, []):
        errors.append("encounters is reserved for a later batch: leave the list empty")
    rules = data.get("rules", "default")
    if rules not in library["rules"]:
        errors.append(f"rules: unknown rules {rules} (Content/rules/{rules}.json)")
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
        if not isinstance(wave.get("lock", True), bool):
            errors.append(f"wave {i}: lock must be true or false")
    return errors


def find_proper_nouns(root=ROOT):
    """Mission names written in the engine's code: content must stay in data."""
    words = re.compile(r"(?<![A-Za-z0-9-])(" + "|".join(re.escape(w) for w in PROPER_NOUNS) + r")(?![A-Za-z])", re.IGNORECASE)
    hits = []
    for file in sorted((root / "Assets/Scripts").rglob("*.cs")):
        for number, line in enumerate(file.read_text(encoding="utf-8").splitlines(), 1):
            for match in words.finditer(line):
                hits.append(f"{file.relative_to(root)}:{number}: {match.group(0)}")
    return hits


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
    if failures:
        print("\n".join(failures), file=sys.stderr)
        return 1
    counts = ", ".join(f"{len(library[k])} {k}" for k in VALIDATORS)
    print(f"Validated {len(files)} mission(s) and the library ({counts})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
