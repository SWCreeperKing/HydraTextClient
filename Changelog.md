# v0.6

- Attached a custom logger to Godot (print out in Consoles -> Godot)

---
# v0.5.4

- Can now view the history of received items
- Fixed change hint priority not working for users with a nickname
- Fixed hint table not showing nickname for finding player
- Added a toggle for showing timestamps in the text client
- Made join/leave/tags changed messages more resistant to nicknames
- Moved the change priority popup from a window to a popup menu

---
# v0.5.3

- Updated Godot to `4.6 dev 1`
- Fixed sorting for hints
- Added an inventory tab in Utilities
  - You can click on an item to see how much of that item others have sent you
- Background color now applies properly to `Hints` and `Utilities` tabs
- Changed popups from being in-app to os native
- Updated some popups
- MultiWorlds will also be saved when you name them instead of __just__ when the app saves
- Added popups that inform you of items you have gained when not connected
- Added specially formatted messages for when someone:
  -  →  = Joins (color tied to progressive items)
  - ←   = Leaves (color tied to trap items)
  - ←→  = Changed Tags (color tied to normal items)
  - 💀  = Sent a DeathLink
  - 🪤  = Sent a TrapLink

---
# v.0.5.2

- Fixed bug preventing hydra from working correctly on:
  - Earlier windows than Windows 11 build 25324
  - Earlier linux than OpenSSL 1.1.1
  - ALL SAVED MULTIWORLDS WILL BREAK, i very much do apologize 

---
# v.0.5.1

- Global font size should now apply on startup
- Fixed Controller input resetting text in the text client

---
# v.0.5

- Some UI styling changes
- Added the option to save a multiworld by naming it
  - Naming it the same as another world will overwrite the previous (might change)
- Added the ability for hints to be cached
  - The cached hints will be saved to a file if the multiworld is named 
- Added the ability to organize hints
  - The multiworld needs to be named before organizing hints 
- Renamed Items & Locations tab to Utilities
- Added a Location Checklist to the Utilities tab
  - Location Checklist has a very simple custom scripting language
  - [here is the documentation for it](https://github.com/SWCreeperKing/HydraTextClient/blob/master/ChecklistDocs.md)

---
# v.0.4.8

- Fixed bug with clients not disconnecting when loosing connection
- Fixed bug with copy style was not image the image directory was appearing
- Improved the changing of viewing previously sent messages
- Fixed a bug when trying to sign in to a slot that had a space in the name

---
# v.0.4.7

- Added countermeasures for if 2 games share the same location id
- A lot of tiny UI changes, here are some: 
  - Added more UI saving i.e. window size and window position
  - Redid player slots UI
  - Changed popup UI a bit
- Added a drop-down in text client to change the item log copy button style  
- Fixed a bug with connection limit being 6 and not 7
- Added `Always on top` setting 
- Fixed `Player (Not Connected To)` not applying to text client 
- Updated Godot to `4.5 Beta 1`
- Universal Alias functionality
  - there are now text client settings along with this
- Added a typo easter egg (1 in 100)

---
# v.0.4.6

- Hints from `!hint_location` will try to be prioritized to being shown in the text client
- Actually fixed the up arrow in the text client allowing you to get previously sent messages
- Bundled all UI font size into a font size setting
- Enabled transparent window and per pixel transparency (UI background can be transparent)
- Added background color options for items
- Added a player not connected color option
- Login restrictions will no longer apply if you are local hosting
- Convinced myself to lower the login cooldown again
- Fixed a bug with locations and connecting with duplicate/reconnecting with the same game
- Enabled Screen Space Anti-Aliasing (SSAA)

---
# v.0.4.5-hf1

- Added a button that will open save directory in the settings
- Fixed a bug on linux that would fail to load color data from the save
  - As a consequence the save file has been optimized
    - Should be backwards compatible 
- Fixed a bug with font size changers acting funny

---
# v.0.4.5

- Fixed some text not being 'cleaned'
- Locations in `Items & Locations` are now all missing locations and now removes checked locations
- Fixed a problem with save data from an old save not adapting correctly
- Fixed an incorrect parent-child with a container in the Text Client messing with the ScrollContainers
- Added a `Line Separation` setting for the Text Client
- RichTextLabel allows you to select text? that's on now for the Text Client 

---
# v.0.4.4hf

- Added more scroll containers
- Fixed `Copy` not appearing in the hints tab
- Fixed not actually being able to change the `Show Related to you only` setting

---
# v.0.4.3

- Updated from `Godot 4.5 (dev 2)` to `Godot 4.5 (dev 3)` 
- Limited max fps from vsync (monitor) to 20fps
- Used the new `FoldableContainer` in `Godot 4.5 (dev 3)` for `Items & Locations`
- `FoldableContainer` (:D) to hide text client filters
- Changed renderer from `Forward+` to `Compatibility`
- Added icon made by Raphael2512
- Added item log filter: `Show Related to you only`
- Changed `[Copy]` in the item log to an image and added a tooltip

---
# v.0.4.2hf

- Fixed funny problems with color settings
- Added more scroll containers
- Fixed disconnecting breaking everything
- Added Licenses

---
# v.0.4.1

- Fixed rich presence not updating correctly when switching clients
- Rich presence now disposes when program is closed
- Fixed the error text disappearing when trying to connect
- Client cooldown is canceled when a connection error happens
- Added manual retry rich presence connection button, rich presence connected notifier & version number
- Mostly proper disconnection behavior

---
# v.0.4

- Fixed a few small bugs
- Added copy text to hints and item logs in the text client
- ~~stole~~ implemented serpent.ai's rich presence
- Small UI coloring, can change app background color
- Added filtering for specific items
  - click on an item in the item log in the text client or the hint table in the hints tab to add them to the filter in the settings 

---
# v.0.3.1

- Font size changer for text client, hints table, and players table added to the settings
- Fixed a (probably) non-noticeable bug with disconnecting and `Items & Locations`
- Decreased connection cooldown from 15 to 5, cooldown increases the more slots you have connected: + (1 * connected)
  - total connection time with the max 7 slots from 1min 45s to 56s
  - just remember that this does 2 things:
    - reduces spam to server
    - disincentivizes having too many slots open
    - it is possible that i might slightly decrease it further in the future, but until ap allows for something that allows you to connect with multiple slots at the cost of having only 1 connected i am very hesitant to decrease it further

---
# v.0.3

- Up arrow now correctly goes to the last msg you sent instead of the first one
- `Items` renamed to `Items & Locations`, the hints now have a friend again :D its hint_locations
- Made the headers in `Items & Locations` buttons so it's far easier to show/hide their contents
- As per qwint's suggestion I limited non inuse clients
- Added a 7 slot connection limit (as to not overload the server with too many connections at once)
- Added a universal 15s cooldown to connecting/changing slots as to not spam the chat
- Removed the need to sync text clients :seekerHappy:
- Decreased font size of chat messages from 24 to 20
- Redid how hints are handled from other active slots when someone does !hint 
- You can click on messages in the text client to copy them (not including hints, go to the hints tab to copy a hint)
- Import/Export colors in settings

---
# v.0.2

- Reduced Players Tab's text size from 32 to 24
- Made player names be aliases in the text client
- Added a tooltip in the text client on player names to show the non nickname and game
- Fixed (hopefully) a bug where the wrong item name is used
- Fixed (hopefully) a bug where items were of the wrong kind
- Added a new color setting (Tooltip Background Color)
- Moved Hint status changing from Hint Manager to the Hints table
- Renamed Hint Manager to Items
- Removed old sorting order layout with the new one that involves clicking on the table's headers
- Very Slightly Improved the message syncing
- Made the Text Client only send out a message from the input every 300ms to reduce spamming the server 

---
# v.0.1

- Initial Release