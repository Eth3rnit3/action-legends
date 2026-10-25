# Version 0.5.0 — difficulty, sound and helicopter boss

Four selectable difficulties change enemy health, density, aggression, player health,
reserves, bonuses and boss durability. Arcade is the default; Détente retains the
earlier enemy health and generous lives. The final helicopter arena blocks victory
until the boss is defeated. Bombs have ground warnings and can be shot down.

## Music and voices

Superseded: the soundtrack, voices and effects were replaced afterwards, see [AUDIO.md](AUDIO.md).

`Assets/Resources/Audio/cerises-arcade.wav` and `cerises-boss.wav` are newly synthesized
arrangements of Antoine Renard's nineteenth-century melody **Le Temps des cerises**.
Only the public-domain melody is used. The accompaniment, percussion, instrument
synthesis and recordings were created for this project. No existing recording,
modern accompaniment or lyrics are included.

Historical source: [BnF, original 1868 edition](https://catalogue.bnf.fr/ark:/12148/cb486936941).
Score catalogue: [IMSLP, original editions listed as public domain](https://imslp.org/wiki/Le_temps_des_cerises_(Renard,_Antoine)).

The eight French announcements use the generic macOS Jacques synthetic voice,
processed with high/low pass filters and compression. They do not imitate a hero
or political figure. The checked-in WAV files work offline in the static browser build.

`tools/generate_arcade_audio.py` regenerates both tracks and announcements on macOS
with Python + NumPy, `say` and `ffmpeg`. The game itself needs none of these tools.

## Generated boss artwork

Built-in OpenAI ImageGen was used, with true transparent background.
Saved game asset: `Assets/Resources/Art/macron-helicopter.png`.
The game animates the rotor and attacks separately from the illustration.

Final prompt:

+> Use case: stylized-concept
> Asset type: single transparent side-view boss sprite for a premium hand-painted pixel-art 2D run-and-gun arcade game.
> Primary request: satirical caricature of Emmanuel Macron piloting an absurdly overarmed small French presidential helicopter, clearly a fictional comic boss. The helicopter faces LEFT, cockpit at LEFT, tail to RIGHT, strictly readable side view. A recognizably exaggerated Macron face is prominently visible through the open cockpit: high forehead, neatly combed short brown hair, long nose, confident smirk, navy suit and narrow tie, one hand on the controls. Large stylized head, humorous political cartoon, no photorealism. The aircraft is steel blue and dark navy with brass/gold trim and a small tricolor stripe. Two chunky forward/downward-pointing gun barrels below the nose, visible bomb racks under belly, landing skids, tail rotor. Highly polished 1990s arcade hand-pixelled craftsmanship, dark navy contours, warm sunset edge highlights, teal shadow metal, fine rivets and readable machine detail. Main rotor blades are omitted except rotor mast, since they will be animated separately by the game. Full complete helicopter including tail and skids, centred, generous transparent padding, wide 3:1 composition, no scene or ground, no effects, no bombs in flight, no lettering, no logo, no watermark, true transparent alpha around the entire silhouette.
