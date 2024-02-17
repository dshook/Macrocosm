# Macrocosm

This is the game Macrocosm that I worked on for about 4 years.  For all the details, check out https://dillonshook.com/what-it-takes-to-make-a-game-by-yourself/


There are some things scraped from the repo to not publish assets I paid for or baked in credentials.  Unfortunately this means I can't post the full git history since it's such a pain in the butt to scrape them from the history.

Hope you have fun exploring and might find something useful!


## Some General File Structure

`Assets/Editor` Custom Unity Editor tools and inspectors.  They were especially helpful for the Stage 6 & 7 map generation.  Also includes the [Unity Save Game Editor](https://dillonshook.com/unity-save-game-editor/) which was also invaluable.

`Assets/Plugins` All the 3rd party code, most from the Unity Store but some just downloaded.

`Assets/Resources` All art/sounds/prefabs/materials.  Kept them all in Resources folder for ease of loading in game.  See [ResourceLoaderService](./Assets/Scripts/Services/ResourceLoaderService.cs)

`Assets/Scripts` All my code.  The numbered folders correspond to the stages of course.

`Assets/Scripts/Models` The whole game state data (with GameDataModel.cs being the root), serialized into the save game.

`Assets/Scripts/Services` Some fun general game services.  Check out GameSaverService, InputService, and StageRules.

`Assets/Scripts/Shaders` Like many game developers, shaders are one of the banes of my time developing the game.  Luckily I didn't have to go too shader heavy on this game but countless hours were still spent in this folder.

`Assets/Scripts/StrangeControls` Root of [StrangeIOC](https://strangeioc.github.io/strangeioc/) which is the dependency injection library I used that worked out very well.  The [GameSignalsContext.cs](./Assets/Scripts/StrangeControls/GameSignalsContext.cs) is the main starting point for setting up the whole DI container with all the services and injectible things.

`Assets/Scripts/UI` All the Unity UI control code. `nuf said

`Assets/Scripts/Util` Everyone has to have a util folder right?  This is probably one of the better points to mine code for other games.

## Some Fun Files to Look at

[Snake Movement](./Assets/Scripts/2/Snake.cs#L474) took unexpectedly long and lots of iterations to get to feel right

[Beat Manager](./Assets/Scripts/3/BeatManager.cs#L312) handling spawing cells in time with the song templates was also surprisingly challenging to get the audio synced properly since audio samples at a much higher rate than the game loop.

[Creature Modifier](./Assets/Scripts/4/CreatureModifier.cs) Fun list of all the creature modifiers and their requirements / exclusions / upgrades.

[TDGrid](./Assets/Scripts/5/TdGrid.cs) Who doesn't love dynamic (based on the players buildings) pathfinding from multiple sources to multiple destinations?

[City Building](./Assets/Scripts/6/CityBuilding.cs) All the possible city buildings based off the creature modifiers except even more complicated

[Hex Map Generator](./Assets/Scripts/6/HexMapGenerator.cs) Map generator for the civ like map. Based off of [CatLikeCoding's Tutorial](https://catlikecoding.com/unity/tutorials/hex-map/part-26/) with lots of modifications.

[HexRiverOrRoad path](./Assets/Scripts/6/HexRiverOrRoad.cs#L89) Crazy logic that took like a week to get right setting all the spline points for rivers and roads along a path to get a nice smooth windy curve that also has to take into account merging rivers or roads and how they overlap to look right.

[Galaxy Generator](./Assets/Scripts/7/GalaxyGenerator.cs) Even crazier version of the Hex Map Generator, generating random stars in a galaxy based on some real science estimations of distributions of stars in their life cycle, then random planets around each star based on the system archetype each with resources for gameplay.  Loosely based off the descriptions in the [GURPS Space book](https://www.sjgames.com/gurps/books/space/)

[Star Name Generator](./Assets/Scripts/7/StarName.cs) The name generator for all the stars. As mentioned in the file, based off [this awesome blog / code](https://martindevans.me/game-development/2016/01/14/Procedural-Generation-For-Dummies-Galaxies/)

[Galaxy Ship](./Assets/Scripts/7/GalaxyShip.cs#L391) Fun relativistic calculations for how fast ships move

[Galaxy Transport Ships Manager](./Assets/Scripts/7/GalaxyTransportShipsManager.cs) Really insane logic for graph pathfinding (traveling salesman essentially) for the ships transporting resources over the connected star routes that has to meet a lot of gameplay constraints.  This alone took weeks to nail down.

