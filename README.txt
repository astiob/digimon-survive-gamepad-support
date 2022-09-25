Gamepad support plugin for Digimon Survive (PC/Steam)
=====================================================

This plugin improves the game's support for gamepads/controllers of all brands.

Unsupported controllers now work.
Missing prompts are added.
Wrong prompts are corrected.
Prompts better match hardware.
You can't screw up in-game gamepad settings.
The game's choice of controller (if you have multiple) is improved.
The mouse pointer is hidden when unused.

Specifically:

  * The mouse pointer disappears when using a gamepad or keyboard
    and reappears when the mouse is used again.

  * A Joy-Con pair is recognized as a Switch controller, and all controls
    work and have visible on-screen prompts with the official configuration.

  * Steam Deck's built-in controls and mobile Steam Link's on-screen touch
    controls work fully and have visible on-screen prompts throughout the
    game with the official configuration, and the on-screen prompts more
    closely match the actual hardware.

  * PlayStation 3 controllers are recognized and work with the official
    configuration. On-screen prompts show the DualShock 4's buttons.

  * MFi controllers on Apple devices running Steam Link are recognized and
    work. On-screen prompts are styled as for the Xbox One (because that's
    the default style).

  * A single Joy-Con is recognized and works with the official configuration,
    but because it lacks a ZL/ZR pair, it isn't enough to beat the game.
    As it also lacks a D-pad, text skipping and auto-advance is unavailable.
    On-screen prompts mostly match the Joy-Con's hardware layout
    (including plus or minus depending on which Joy-Con it is),
    but some inconsistencies remain and prompts for nonexistent
    controls are still displayed.

  * Other officially unsupported controller types and configurations
    should also have a better chance of working.

  * On-screen prompts never turn into white squares or persist after switching
    to another controller; all unrecognized inputs consistently show
    closest-approximation or default prompts instead.

  * Standard buttons unbound via in-game Gamepad Settings can be rebound
    without resorting to resetting the whole settings page to defaults.

  * PlayStation controllers: the interaction prompt during exploration
    correctly shows "X" instead of "O" if you're using the default "X"
    binding (or "O" instead of "X" if you genuinely have this action bound
    to "O"). Help pictures still show "O" though.

  * Switch controllers: tutorial instructions correctly show "A", "B", "X"
    and "Y" instead of "B", "A", "Y" and "X", and on-screen prompts for stick
    clicks (if you're using them) consistently show the sticks instead of SL/SR.

  * Steam Controller: all on-screen prompts for
    the right trackpad show "R" instead of "RS".

  * Controllers connected via Remote Play (including Steam Link) take priority
    over controllers attached to the host computer.

  * Controllers are prioritized by type:
     1. explicitly supported controllers: Xbox, PlayStation,
        Steam Controller, Switch Pro Controller, paired Joy-Con;
     2. generic/unrecognized controllers;
     3. built-in controls on the Steam Deck or mobile Steam Link
        (so that if an external controller attached, it takes over);
     4. single Joy-Con (because it has too few controls).

Joy-Con support requires Steam client's 19 Aug 2022 update.
Some local controllers may not be fully supported by Steam on Windows 7.


Installing
----------

 1. In your Steam Library, right-click Digimon Survive (or click the gear
    button for Digimon Survive) and select Manage -> Browse Local Files
    or Properties -> Local Files -> Browse. This will open the game's
    file folder.

 2. Download BepInEx 5 x64 from https://github.com/BepInEx/BepInEx/releases

 3. Copy/extract BepInEx into the game's folder,
    so that the folder looks like this:

        BepInEx
        DigimonSurvive_Data
        MonoBleedingEdge
        Support
        changelog.txt
        DigimonSurvive.exe
        doorstop_config.ini
        UnityCrashHandler64.exe
        UnityPlayer.dll
        winhttp.dll

 4. Create a "plugins" folder inside the BepInEx folder.

 5. Copy/extract GamepadSupportPlugin.dll into the plugins folder.

 6. If you're using Linux/Proton (including Steam Deck), open Digimon Survive
    in your Steam Library, press the gear button, select Properties, and put
    this in Launch Options:

        WINEDLLOVERRIDES="winhttp=n,b" %command%

That's it. The mod will be active whenever you launch the game.


Combining with other mods
-------------------------

This can be combined with other mods, including other BepInEx plugins
such as the Japanese text mod or the video cutscene fix plugin:
just put all your BepInEx plugins in the same plugins folder.


Troubleshooting
---------------

The plugin records your gamepad configuration in the file
BepInEx\LogOutput.log in the game's folder. If you are still having problems,
send me the contents of this file. (See AUTHOR.txt for contact details.)