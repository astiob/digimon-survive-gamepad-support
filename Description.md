  * A Joy-Con pair is recognized as a Switch controller, and all controls work and have visible on-screen prompts with the official configuration.
  * Steam Deck, mobile Steam Link: all controls work and have visible on-screen prompts throughout the game with the official configuration, and the on-screen prompts more closely match each controller’s actual hardware.
  * PlayStation 3 controllers are recognized and work with the official configuration. On-screen prompts show the DualShock 4’s buttons.
  * A single Joy-Con is recognized and works with the official configuration, but because it lacks a ZL/ZR pair, it isn’t enough to beat the game. As it also lacks a D-pad, text skipping and auto-advance is unavailable. On-screen prompts mostly match the Joy-Con’s hardware layout (including plus or minus depending on which Joy-Con it is), but some inconsistencies remain and prompts for nonexistent controls are still displayed.
  * Other officially unsupported controller types and configurations should also have a better chance of working. Share your experience in the comments!
  * On-screen prompts never turn into white squares or persist after switching to another controller; all unrecognized inputs consistently show default prompts instead.
  * Standard buttons unbound via in-game Gamepad Settings can be rebound without resorting to resetting the whole settings page to defaults.
  * PlayStation controllers: the interaction prompt during exploration correctly shows “X” instead of “O” if you’re using the default “X” binding (or “O” instead of “X” if you genuinely have this action bound to “O”). Help pictures still show “O” though.
  * Switch controllers: tutorial instructions correctly show “A”, “B”, “X” and “Y” instead of “B”, “A”, “Y” and “X”, and on-screen prompts for stick clicks (if you’re using them) consistently show the sticks instead of SL/SR.
  * Steam Controller: all on-screen prompts for the right trackpad show “R” instead of “RS”.
  * Controllers are prioritized by type:
     1. mobile Steam Link (to override any controller attached to the computer);
     2. explicitly supported controllers: Xbox, PlayStation, Steam Controller, Switch Pro Controller, paired Joy-Con;
     3. generic/unrecognized controllers;
     4. Steam Deck (so that if a Deck has any external controller attached, it takes over);
     5. single Joy-Con (because it has too few controls).

Joy-Con support requires Steam client’s 19 Aug 2022 update.


Troubleshooting
---------------

The plugin records your gamepad configuration in the file BepInEx\LogOutput.log in the game’s folder.
If you are still having problems, send me the contents of this file. (See AUTHOR.txt for contact details.)



TODO:
  * Dedicated KeyTypes for Steam Controller/Deck trackpads that emulate other controls.
  * Display keyboard prompts if the action has no known origin, except for the L-stick on single Joy-Con.
  * Drop D-pad prompts for single Joy-Con.
  * Accept input from all attached controllers simultaneously.
  * Use Steam's PNGs for unrecognized origins?
  * If Joy-Con and Deck prompts can be combined unambiguously, move Joy-Con above Steam Deck in prompt priority.

Steam Controller = Xbox /. RS,R3 -> PS4
Steam Link = Xbox /. D-pad -> PS4, LRS,LR3 -> PS4
Steam Deck = Switch ABXY + PS4 LR12 + PS4 sticks + Xbox D-pad + Xbox SS
Joy-Con = Switch /. custom ABXY, custom D-pad