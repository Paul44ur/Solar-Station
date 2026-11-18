roles-antag-bloodsucker-name = Bloodsucker
roles-antag-bloodsucker-objective = Feed on the crew, grow in rank and bind vassals, all without being found out.
role-subtype-bloodsucker = Bloodsucker
bloodsucker-roundend-name = bloodsucker
objective-issuer-bloodsucker = [color=darkred]Bloodsucker[/color]

bloodsucker-gamemode-title = Bloodsuckers
bloodsucker-gamemode-description = Something on the station is draining the crew dry.

bloodsucker-role-greeting =
    You are a Bloodsucker, an undead predator hiding among the crew.
    Use Feed on a victim to drink their blood. Feeding from a hard grab bites the neck and puts them to sleep. Being seen feeding breaks the Masquerade.
    Claim a coffin as your lair. When Sol rises, the sun burns you unless you rest inside it.
    You gain a rank for each Sol you sleep through in your coffin and for every 300u of blood you drink. Each rank unlocks new powers.
    Bullets pass through you, lasers hurt you less, disablers barely tire you, and ordinary weapons barely hurt you. Holy water and weapons blessed by the Chaplain burn you badly.
    Humanity: drinking someone to the brink, being seen feeding and torturing vassals cost humanity; letting victims go and resting through Sol restore it. High humanity (70+) unlocks gentle feeding on sleepers, blood packs, Beguile, Forget and double ranks from Sol. Low humanity makes you stronger but below 30 you can't Masquerade or Veil.
    Burns only heal near your coffin. Masquerade lets you pass health scans and examination, but drops your undead resistances, and taking 30 damage breaks it.

bloodsucker-role-briefing = You are a Bloodsucker. Feed in secret, hide in your coffin during Sol, and rank up to unlock new powers.

bloodsucker-rank-up-sol = You rested through Sol.
bloodsucker-rank-up-blood = The blood you drank makes you stronger.
bloodsucker-rank-up = {$reason} You are now rank {$rank}.
bloodsucker-rank-up-powers = {$reason} You are now rank {$rank}. New powers: {$powers}.

objective-condition-bloodsucker-blood-title = Drink {$count}u of blood
objective-condition-bloodsucker-blood-description = Drink at least {$count}u of blood from your victims.
objective-condition-bloodsucker-rank-title = Reach rank {$count}
objective-condition-bloodsucker-rank-description = Grow in power until you reach rank {$count}.
objective-condition-bloodsucker-vassal-title = Make {$count} {$count ->
    [one] vassal
    *[other] vassals
}
objective-condition-bloodsucker-vassal-description = Bind {$count} {$count ->
    [one] crew member
    *[other] crew members
} to your will on a persuasion rack.

bloodsucker-melee-resisted = {CAPITALIZE(THE($target))} seems to take barely any damage from this.
bloodsucker-holywater-burns-skin = The holy water sears your skin!
bloodsucker-holywater-burns-inside = Holy water burns you from the inside!
reagent-comp-condition-bloodsucker = bloodsucker

blessed-weapon-blessed = You bless {THE($weapon)}. It glows with holy light.
blessed-weapon-faded = The holy glow of {THE($weapon)} fades.
blessed-weapon-examine = [color=gold]It glows with holy light.[/color]
objective-condition-bloodsucker-vassal-serve-title = Help your master {$targetName} take over the station.

bloodsucker-roundend-stats = [color=darkred]{$name}[/color] reached rank {$rank}, drank {$blood}u of blood and made {$made} {$made ->
    [one] vassal
    *[other] vassals
}, {$vassals} still serving at the end. Humanity: {$humanity}.
bloodsucker-masquerade-broken-user = The pain tears your Masquerade apart!
bloodsucker-masquerade-broken-others = {CAPITALIZE(THE($user))}'s skin turns corpse-pale and their eyes flash red!

alert-bloodsucker-masquerade-name = Masquerade
alert-bloodsucker-masquerade-desc = You pass as human. Your undead resistances are gone.
alert-bloodsucker-humanity-name = Humanity
alert-bloodsucker-humanity-desc = How much of your mortal self remains. High humanity unlocks the Masquerade path, low humanity makes you a stronger but obvious Monster.
bloodsucker-humanity-too-low = You are too far gone to pass as human.
bloodsucker-examined-eyes = {CAPITALIZE(POSS-ADJ($target))} eyes have a faint red glint.
bloodsucker-examined-monster = {CAPITALIZE(POSS-ADJ($target))} eyes glow red, and sharp fangs show when {SUBJECT($target)} {CONJUGATE-BASIC($target, "smile", "smiles")}.

bloodsucker-feed-gentle-user = You drink from {THE($target)} while {SUBJECT($target)} {CONJUGATE-BE($target)} asleep. They will never know.
bloodsucker-feed-gentle-victim = You feel strangely tired.
bloodsucker-bloodpack-refuse = Cold, stale blood disgusts the beast in you.
bloodsucker-bloodpack-drink = You drain the blood pack. It's no fresh kill, but it will do.

bloodsucker-rank-up-patient = Your patience is rewarded.

bloodsucker-forget-victim = Your mind goes blank. You can't remember anything that happened in the last minute.
bloodsucker-forget-unknown-name = Unknown

bloodsucker-beguile-low-humanity = You need more humanity to charm the living.
bloodsucker-beguile-fail = {CAPITALIZE(THE($target))} can't accept the dark gift.
bloodsucker-beguile-fail-protected = {CAPITALIZE(THE($target))}'s mind is shielded against you.
bloodsucker-beguile-offered = You offer {THE($target)} the dark gift. They have 30 seconds to accept.
bloodsucker-beguile-offer = {CAPITALIZE(THE($user))} offers you a dark gift: eternal service to the night. To accept, right-click {OBJECT($user)} within 30 seconds and choose "Accept the dark gift". You would become {POSS-ADJ($user)} vassal.
bloodsucker-beguile-whisper = Serve me, and you will never die.
bloodsucker-beguile-offer-popup = {CAPITALIZE(THE($user))} offers you a dark gift. Right-click {OBJECT($user)} to accept.
bloodsucker-beguile-accept-verb = Accept the dark gift
bloodsucker-beguile-accepted-vassal = You accept the dark gift. You now serve {THE($user)}.
bloodsucker-beguile-accepted = {CAPITALIZE(THE($target))} accepts your gift and becomes your vassal.
