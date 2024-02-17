-------------------------------------------------------------------------------------------------
                                         Color Palette
                                         Version 1.1.8
                                       PygmyMonkey Tools
                                     tools@pygmymonkey.com
                   	     http://pygmymonkey.com/tools/color-palette/
-------------------------------------------------------------------------------------------------

Thank you for buying Color Palette!

If you have questions, suggestions, comments or feature requests, please send us an email
at tools@pygmymonkey.com



-------------------------------------------------------------------------------------------------
                            Support, Documentation, Examples and FAQ
-------------------------------------------------------------------------------------------------

You can find everything at http://pygmymonkey.com/tools/color-palette/



-------------------------------------------------------------------------------------------------
                                  How to update ColorPalette
-------------------------------------------------------------------------------------------------
1. Close the Color Palette window
2. Delete everything under the 'ColorPalette' folder from the Project View, EXCEPT the folder
"/ColorPalette/Resources/"
3. Import the latest version from the Asset Store


-------------------------------------------------------------------------------------------------
                                           Get Started
-------------------------------------------------------------------------------------------------

Color Palette allows you to manage all your color palettes directly inside Unity.
Instead of manually setting RGB color from the color picker, you can just pick the color you want
from the Color Palette Window. You can even apply an entire palette on all the objects in your
scene with just one click (in the editor and at runtime too!).
You can launch the interface by going to "Window/PygmyMonkey/Color Palette" in Unity.


---------------------------------------- The header menu ----------------------------------------
On the very top of the Window, is the Color Palette menu which allows you to:
- Clear all the palettes (except the first one),
- Restore the default palettes that comes with Color Palette,
- Update the current scene, see "Update color of objects in your scene" below,
- Click on the help button to open the Color Palette website


----------------------------------------- Palette List ------------------------------------------
At the top of the window, you'll find all of the color palettes you have created, you can see:
- the name of the palette, and even click on it if you want to hide it.
- all the colors of the palette (with alpha at the bottom)
- on the right, you have some buttons to do :
-- button +: Using this will clone the current palette. Use that to create a new palette
-- button ↑: You can use this to move the palette up in the list (to reorder them)
-- button ↓: You can use this to move the palette down in the list (to reorder them)
-- button -: To delete a palette
- on the left, the 'Set current' button will set the palette as the current palette (more on
this below). The current palette appears green in the window.
- you then have the 'Show details' button, that allows you to modify your palette.


---------------------------------------- Edit a palette -----------------------------------------
Once you've clicked on 'Show details', you'll be able to edit the palette information, such as
the name and the different colors of your palette.
To modify a color, simply click on the color and chose the new one with the color picker. As for
palettes, you can add (clone), move up, move down and remove a color from the list.

If you prefer to see your palette colors as a vertical list instead of horizontal, you can click
the 'Show details' button on every palette.


---------------------------------------- Create palettes ----------------------------------------
Video tutorial: https://www.youtube.com/watch?v=TQ2jiE5HtjM

At the bottom of the window, you have the 'Create palette' section. Here you just chose the
palette algorithm you want, to create a random palette.
You can also specify the number of colors to be generated.

--- Random Palette ---
Using this algorithm will create a completely random palette with no logic at all.

--- Random Pastel ---
Same thing than 'Random Palette', except the colors are pastel.

--- Random Vivid ---
Same thing than 'Random Vivid', except the colors are bright.

--- Random From Color ---
Will create a random palette with some variations from a reference color you specify.
You can also change the offset slider to increase/decrease the variations of color.

--- Random Golden Ratio ---
Will create a random palette using the golden ratio as a variable. This create a palette with
colors being really differents from one another.
You can also change the saturation slider to have colors that are more pastel or vivid.

-- Gradient --
Will create a palette with a gradient from a color to another one. You specify the starting
and ending color, and it will generate the colors in between.


--------------------------------------- Import palettes -----------------------------------------
Video tutorial: https://www.youtube.com/watch?v=0LQdHCx1wNU

Then you have the 'Import palette' section, that allows you to import external palettes.
The first part of the section, will open the file explorer and ask you to select the location
of the file you want to import. If you want, you can just drag & drop a file in the "Drop a
palette here to import it" box (to do that, the file must be inside your Unity project).

If you want to use a file type that is not listed, please contact us. Here are the formats we
currently support:
--- .ase ---
This is the Adobe Swatch Exchange file format. It is used in popular software and websites
such as Adobe Kuler (Adobe Colors), Adobe Illustrator, Adobe Photoshop...

--- .aco ---
Another Adobe file format, the Adobe Color file, used in Adobe Photoshop.

--- .gpl ---
The GIMP file format for palettes.

--- .svg ---
You can import SVG color palettes, that you can find on websites such as www.colourlovers.com

--- Color presets ---
Unity save color palettes as Color Presets objects. You can find some examples in the folder
"PygmyMonkey/ColorPalette/Example/Editor/Palettes". If you want to save yours, you can just open
the color picker, at the bottom, there is 'Presets' and a small menu on the right. Clicking on
it and selecting "Create new library..." will allow you to save it as a Color Presets object.


-------------------------------- Import palettes from websites ----------------------------------
You can also directly import palettes from some websites. You just need to copy the URL, and
click on the 'Download from URL in clipboard' button to download it to your palettes.
This is really powerful and fast, as you don't have to find where is the download button on
the website, download the file, the import it manually going through your folders.

Current compatible sites are:
- colourLovers.com
- dribbble.com
- colrd.com
If you know a website that we can add, please contact us!


-------------------------------------------------------------------------------------------------
----------------------------- Update color of objects in your scene -----------------------------
-------------------------------------------------------------------------------------------------
With Color Palette, you can assign a script to objects in your scene you want to control via palettes.
You can see an example of that in the Demo scene, in "PygmyMonkey/ColorPalette/Example/Scenes".
You just assign the 'Color Palette Object' script on all the gameObjects you want to control.

In the inspector, you'll see some info you can play with on the 'Color Palette Object' script :

--- React ---
You can select some values in this list to tell the script how to react :
- NONE: Will do absolutely nothing, just as the script was not attached on the object.
- CURRENT_PALETTE: With this, you're telling Color Palette that you want to use the colors that
are inside the current palette you selected in the ColorPalette Window with the 'Set current'
button.
- CUSTOM_PALETTE: Here, you can say that you don't want to use the current palette, but another
one of the palettes.

--- Palette ---
Here you'll see the name of the palette that is used by the 'Color Palette Object' script.
If you've selected CURRENT_PALETTE, you'll see the name of the current palette, but if you've
selected CUSTOM_PALETTE, you'll see a list of all the palettes you created, and will be able
to select the one you want to use for this gameObject.

--- Percentage ---
You can then use the percentage slider to define which color will be used according to the
palette you selected in the previous step.
It's a percentage slider, meaning that if you only have 2 colors on your palette, you'll have:
- From 0 to 50%: The first color
- From 50 to 100%: The second color
If you have 4 colors in your palette, you'll have:
- From 0 to 25%: The first color
- From 25 to 50%: The second color
- From 50 to 75%: The third color
- From 75 to 100%: The fourth color
etc....

Now that you know that, you'll see that you can attach the 'Color Palette Object' script on a
gameObject that has a color, and you can set 'React' to CURRENT_PALETTE. You'll see that your
object color will change depending on the pourcentage you set. And you can even go to the 'Color
Palette' Window and change the current palette to another one, and see that the color of your
object will be updated.

Clicking on the 'Set current' button will update the color of all the gameObjects in the current
opened scene that have the 'Color Palette Object' script on them. If you just modify a color in
a palette, it will not directly update the current scene. If you want to manually update all the
gameObjects in your scene, in the menu header of the 'Color Palette' Window there is the button
'Update scene' that will do just that.

The 'Color Palette Object' also works at runtime. That means that you don't have to click the
button 'Update scene'. You can just enter play mode, and every object that has the 'Color palette
Object' script will update the color of the script in the 'Awake' method.
Of course, you can open every scene in your project and press the button to update the gameObjects
of all your scenes if you want the changes to be directly set in the scene and not at runtime.


-------------------------------------------------------------------------------------------------
----------------------------------- Using Color Palette at runtime ------------------------------
-------------------------------------------------------------------------------------------------
You can even change the color palettes you're using at runtime. You'll see an example of that
in the demo scene, but here is how it works.

You can for example, let the user chose a color for the UI or for his character in a menu at
runtime. When the user press your 'Red' button, you will call a method to set the current palette
to the red palette using 'ColorPaletteData.Singleton.setCurrentPalette("YOUR_PALETTE_NAME");'. It
will then update every object in the scene that has the 'Color Palette Object' script and with the
'react' field set to CURRENT_PALETTE.
It's really great to change the entire color of your UI to another palette in an instant.

If you have some gameObjects that use the CUSTOM_PALETTE 'react' field. You can call the method
'setCustomPalette' of the 'Color Palette Object' script to change the custom palette used by
these gameObjects.



-------------------------------------------------------------------------------------------------
                                            Examples
-------------------------------------------------------------------------------------------------

------------------------------------------ Demo scene 1 -----------------------------------------
You can find the demo scene in "PygmyMonkey/ColorPalette/Example/Scenes/".
In this scene you'll see 3 different type of objects that all use the 'Color Palette Object'
script to manage their color.

--- 2D Sprites ---
At the top, you see a Smiley and a House sprite.
They are in fact, composed of multiple sprites. Each sprite is a part of the image and is full
white on a transparent image.
You can find the sprites in "PygmyMonkey/ColorPalette/Example/Textures/".
For example, Smiley is composed of 3 different sprites, one for each color. We then assign the 
color of the sprite directly in the inspector. Same thing for the House.
You can see that each sprite, have the 'Color Palette Object' script attached and each have a
custom palette selected.
For Smiley, the custom palette named 'Smiley' is selected, that is composed of 3 colors. You can
select the 3 sprites in the Hierarchy, and change the palette to another palette, and see that
the Smiley colors will change directly.
As they are using the 'react' CUSTOM_PALETTE, they are not affected by the current palette you
have chosen.

--- 3D Objects ---
You can even use the 'Color Palette Object' on 3D objects. In this example, each cube is using
the current palette and have a different percentage so they all have a different color.
You can change the current palette in the 'Color Palette' Window and see that the cubes will
change color depending on the palette you've selected.

--- User Interface ---
The last group of objects is UI and is located under the Canvas gameObject.
We added a 'Color Palette Object' script on each UI component and select the color we wanted
using the percentage slider. They are all dependant on the current color palette. So selecting a
new palette in the 'Color Palette' Window will update their color.
This is really powerful when you want to change in just one click the entire color of all your UI.

--- Play Mode ---
You can then enter in play mode and see that you can change the current palette at runtime using
the button in the gameView. You can find the code that do that in the 'ColorPaletteDemo' script
located in "PygmyMonkey/ColorPalette/Example/Scripts/".

------------------------------------------ Demo scene 2 -----------------------------------------
In this scene, you'll see an example on how you can retrieve a color palette at runtime from the
web and apply it to whatever object you want. In this example, to 3D cubes.

------------------------------------------ Demo scene 3 -----------------------------------------
You'll have an example on how to use the ColorPaletteObject script to attribute a different
palette and a different color to your gameObjects. And how to change them all at once in a single
click of a button :)
You'll also find an example on how to retrieve a palette info at runtime, and how to get a color
from a palette, using the color name.


-------------------------------------------------------------------------------------------------
                                          Release Notes
-------------------------------------------------------------------------------------------------
1.1.8
- NEW: Added support for Unity 2017 & 2018
- NEW: Improved Demo2 scene with 3 buttons (each one downloading a palette from a supported website)
- REMOVED: Removed ColorCombos color palette parser

1.1.7
- FIX: Issue where the current palette was not saved
- FIX: Errors with WebPlayer and SamsungTV

1.1.6
- FIX: Duplicated palettes are now no longer "linked" to the original palette
- FIX: Duplicated colors are now no longer "linked" to the original color
- NEW: Added getRandomPalette method
- NEW: Added getRandomColor (from a palette) method
- UPDATE: Renamed setRandomPalette to setRandomCurrentPalette
- NEW: Added a color offset you can tweak when creating a palette from a color reference (in the
Create palette section)
- NEW: You can now import palettes from the website colrd.com!

1.1.5
- NEW: ColorPaletteObject now has an 'override alpha' field, so you can assign a custom alpha to
an object when applying a color from a palette
- NEW: Each color from a palette can now have a name that you can set, modify, retrieve at runtime
and will be automatically retrieved when importing palettes or getting palettes from the web
- NEW: Added the HEX value next to each color in the 'Show details' of Color Palette Window
- NEW: You can now mouse over each color to have info on it (name, hex value and RGB values)
- NEW: Can now import .aco files version 2 (prior was only version 1)
- NEW: Added method to get a color from its name inside a palette (at runtime, via code)
- UPDATE: Demo scene 3 show an example on how to retrieve info on a palette and color from its name 

1.1.4
- NEW: You now open Color Palette via the menu "Window/PygmyMonkey/Color Palette", it was
previously in "Tools/PygmyMonkey/Color Palette".
- NEW: Added example scene #3 (showing how to use random palettes)
- FIX: Save color palettes when clicking on 'Hide details'
- FIX: Importing the name of the palette from colorCombos.com
- FIX: Importing palettes from dribbble.com

1.1.3
- NEW: New Inspector for ColorPaletteObject. You can now see all the colors from your color
palette and clicking (or dragging) on the colors will update the color (no more changing the
color percentage value blindly).
- NEW: Added setRandomPalette method (to use at runtime)
- UPDATE: The ColorPaletteObject script does not use color percentage anymore, but color index
- FIX: Fixed issue when the ColorPaletteObject script did not find a renderer on the GameObject
- FIX: Fixed saving palettes modifications in the Editor

1.1.2
- NEW: Added demo scene 2, that show how to retrieve a color palette from a website at runtime.

1.1.1
- NEW: Added a header menu, allowing you to:
-- Clear all the palettes,
-- Restore the default palettes,
-- Click the help button to open the Color Palette website,
-- Update the color palette objects in current scene.
- UPDATE: Moved the "Update color palettes objects" button to the header menu.
- UPDATE: The current palette had a full green background, now just the header is green.

1.1.0
- NEW: Added .ase, .aco and .gpl file types support for importing palettes.
- NEW: Import palette from websites with a single click on a button.
- NEW: You can now import palettes from colourLovers.com, dribbble.com and colorcombos.com.
- NEW: Improved the create palette section and added algorithms to create random palettes (random,
random pastel, random vivid, random from color, golden ratio and gradient).

1.0.0
- NEW: Initial release


-------------------------------------------------------------------------------------------------
                                          Future Updates
-------------------------------------------------------------------------------------------------

- Improve Color Palette Object -
We will add more features for changing the palette at runtime of different objects and group of
objects.

- Add a demo scene -
This will be a basic UI scene example, where you can apply in one click your palettes to see
how all the colors of your palette looks together.

- Change the view of the palettes -
You'll be able to see smaller, bigger or compact palettes by changing the view mode.


-------------------------------------------------------------------------------------------------
                                               FAQ
-------------------------------------------------------------------------------------------------

- There is a file format that you do not support, can we contact you?
Yes of course! We will try to add support for your file format with the next release.

- There is a fantastic palette website, can you add support for direct palette download?
No problem, please contact us and if we can retrieve the info from the website, it will be in
the next release :D

- How can I help?
Thank you! You can take a few seconds and rate the tool in the Asset Store and leave a nice
comment, that would help a lot ;)

- What's the minimum Unity version required?
Color Palette will work starting with Unity 5.6.0.


-------------------------------------------------------------------------------------------------
                                           Other tools
-------------------------------------------------------------------------------------------------

--- Material UI (http://u3d.as/mQH) ---
It's now easier than ever to create beautiful Material Design layouts in your apps and games
with MaterialUI!
Almost all of the components featured in Google's Material Design specification can be created
with the click of a button, then tweaked and modified with powerful editor tools.

--- Advanced Builder (http://u3d.as/6ab) ---
Advanced Builder provides an easy way to manage multiple versions of your game on a lot of
platforms. For example, with one click, Advanced Builder will build a Demo and Paid version of
your game on 4 different platforms (that's 8 builds in one click).

--- Gif Creator (http://u3d.as/icC) ---
Gif Creator allows you to record a gif from a camera, or the entire game view, directly inside Unity.

--- Native File Browser (http://u3d.as/xFm) ---
Native File Browser provides an easy way to use the native File Browser on Mac and Windows.