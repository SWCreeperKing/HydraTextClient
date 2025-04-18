# v.0.3.1

---
- Decreased connection cooldown from 15 to 7, cooldown increases the more slots you have connected: + (3 * connected)
- Font changer for text client, hints table, and players table
- Fixed a (probably) non-noticeable bug with disconnecting and `Items & Locations`

# v.0.3

---
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

# v.0.2

---
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

# v.0.1

---
- Initial Release