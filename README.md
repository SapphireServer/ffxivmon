# FFXIVMon Reborn          [![Build status](https://ci.appveyor.com/api/projects/status/hvqfvrj5puf96f0b?svg=true)](https://ci.appveyor.com/project/SapphireMordred/ffxivmon)


A FINAL FANTASY XIV Network analysis tool.

![pic](https://i.imgur.com/HiEQnip.png)

Includes automated packet parsing from the main Sapphire repo, scripting features, linking packet information to EXD tables, etc.

Depends on a fork of [Machina](https://github.com/goaaats/machina) by Ravahn. 


# Features
## Packet filtering
- Enter filters for displayed packets, in the GUI see `Filters > Show Help`
## pcap Parsing
- Support for parsing pcap captures. pcap captures must be versioned via the `Database` functionality after import and saved as XML to retain any changes made to them
## Packet struct version control
- Syncs with [Sapphire](http://github.com/SapphireServer/Sapphire) packet structs (or another repo). See `Options > Set Repository`. Changing this value will erase local definitions. For safety of local data, this must be manually resynced via this menu or `Database > Redownload Definitions` whenever you wish to update definitions
- Allows versioning of a capture via the `Database` menu. To temporarily preview packets with specific, right-click the packet in the viewer and `Apply specific struct to packet`
- Attempts to find the closest [Sapphire](http://github.com/SapphireServer/Sapphire) structs to apply to packets in the capture. For this to work for `pcap` the parent directory must be a FFXIV patch number (numbers only) e.g. `captures/4.30/test.pcap`
## Packet exporting
- Allows exporting packets to the FFXIV packet payload structure as raw binary DAT files via right-click on packet(s) and `Export to DAT` and `Export set`
## Live Captures
- Captures packets while the game is running and uses [Machina](https://github.com/ravahn/machina) to process packets in realtime
## Scripting
- Run C# scripts on captures. Make use of game files via the [Lumina](https://github.com/NotAdam/Lumina) library to further process captures
- Scripts can be run per capture or per packet via right-click on the packet list entry
## Packet Diff
- Compare the currently opened capture with another saved capture to try find opcode changes (not very accurate)

To apply changes to a capture it must be saved again, preferably to a file with a new name to preserve packet data in case of incorrect filtering.