# Devil art needed

Every sprite in this folder is a **placeholder** copied from existing art.
Replace the PNG files here with new art, keeping the same file names, and update the
`copyright` (and `license` if needed) in each `meta.json`. No code changes are needed.

## Action icons: `actions.rsi` (32x32, one direction)

| File | Used for | Description |
|---|---|---|
| `voice-of-hell.png` | Voice of Hell (lesser devil) | Mutes a target for 10 seconds with a fraction of hell's might. |
| `hells-seven-rings.png` | Hell's Seven Rings (Archdevil) | Toggles the Archdevil's burning aura on and off. |
| `soul-harvest.png` | Soul Harvest (Archdevil) | Tears out a soul the Archdevil owns. The victim dies for good. |
| `summon-imps.png` | Summon Imps (lesser devil) | Calls two imps up from below. |
| `mass-contract.png` | Mass Contract (Archdevil) | Offers one contract to everyone nearby at once. |

## Imp mob: `imp.rsi` (32x64, 4 directions)

| File | Used for | Description |
|---|---|---|
| `imp.png` | The imp (`MobDevilImp`) | A small, nasty demon summoned by the lesser devil. |

The placeholder imp is the full-size true devil, shrunk and tinted red in
`Resources/Prototypes/_Goobstation/Devil/mobs.yml` (`scale: 0.6, 0.6` and `color: "#b84040"`).
When real imp art arrives, remove those two lines so it shows at its real size and color.
If the new art is 32x32, also change `size` in `imp.rsi/meta.json`.
