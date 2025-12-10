<!--<p align="center"><img src="https://github.com/LocalPrimate/AppSummoner/raw/main/AppSummoner/Resources/AppSummonerLogo.png" /></p>-->

<p align="center"><img src="https://github.com/LocalPrimate/AppSummoner/blob/main/AppSummoner/Resources/AppSummonerIcon-2-2x.png?raw=true" /></p>

---

<h3 align="center">App Summoner</h3>

<p align="center"><i>An application that launches, focuses, and minimizes other applications really quickly.</i></p>

---

Switching between applications should be lightning fast.

[Snap](https://apps.apple.com/us/app/snap/id418073146?mt=12) for MacOS does just that, and has been an indispensable tool for my work productivity. Sadly a similar tool for Windows that is as simple and flexible doesn't seem to exist! That's where App Summoner comes in.

App Summoner aims to remove road bumps by making Windows applications accessible at the press of a button. 

If you're like me, sometimes those mild, little inconveniences (cycling through alt-tab windows; searching for an application in your taskbar with your mouse) can become much larger blockers while you're deep in a flow state. That small amount of time spent finding the tool before using it can be enough to vanish a crucial thought or cause a detrimental context switch.

We're working on computers, here. Apps should be available at the press of a button. 

# 

### So what does it do?

Features:
- Assign hotkeys to launch applications and toggle window visibility
- Configure hotkey behaviour with any combination of:
  - Launching the application
  - Bringing it into focus
  - Hiding or minimizing it
- Enable/disable hotkeys
- Use pretty much any key you want (though you should probably include a modifier key in the majority of cases)
- Flexibility:
  - Maybe you only want to use App Summoner to launch applications, and nothing else. You can totally do that.

# 

### Building

`dotnet publish -c Release -o <output_dir>`

# 

### Roadmap
- [ ] Interface with specific windows for a given application
  - For example: If you have many browser windows open, you should be able to specify which window you want to focus or hide, dynamically (it shouldn't be in a config)
- [ ] Add option to affect all windows for a given application
  - This doesn't play nice with some apps, such as Audacity, but would be great for browsers, potentially.
- [ ] Bring an application into focus if it's currently minimized to the tray
- [ ] Eliminate the short mouse freeze during minimize and maximize animations when invoking a hotkey
- [ ] Separate the focus-steal-prevention feature into its own project
- [ ] Remove the whole "rune" terminology, it was a bad idea.

# 

### Play online games? Use at your own risk.

App Summoner uses system-wide keyboard hooks. 

While other legitimate tools, such as Logitech G Hub, are whitelisted by anti-cheat systems, there's no guarantee that this application won't be flagged for any reason.

Systems like Vanguard are extra sensitive. Maybe they'll whitelist us one day, who knows?

---

<p align="center"><img src="https://github.com/LocalPrimate/AppSummoner/blob/main/AppSummoner/Resources/AppSummonerIcon2x.png?raw=true" /></p>
