# Yu-Gi-Oh TTS Deck Builder

## What is it?

I built this software because I couldn't find a solution existing for building a Yu-Gi-Oh deck for TTS automatically. There are plenty of tutorials on how to assemble the images and build the deck yourself but nothing as simple as building a Magic The Gathering deck. This software enables users to use decks made on [YGOPro](https://ygoprodeck.com/) and convert them into files for Tabletop Simulator.

## Where do I get it?

This software is compatible with Windows 10 devices, search the Microsoft Store for "Yu-Gi-Oh TTS Deck Builder" or [click here](https://www.microsoft.com/en-us/p/yu-gi-oh-tts-deck-builder/9n7l9stf64v3).

## How do I use it?

### Main Screen
![Main Screen](https://i.imgur.com/Ezy6H0h.png)
Click the first "Browse" button and select your YDK you've exported from [YGOPro](https://ygoprodeck.com/).
Click the second "Browse" button and select your output file name for you decks.
Click the "Process" button. You will see a processing indicator until the file is done.
![Processing](https://i.imgur.com/ezg1v1Q.png)

### After Processing
Take the output file (enteredname.json) and put it into your 
"C:\Users\Username\Documents\My Games\Tabletop Simulator\Saves\Saved Objects" directory.

## Common Questions

### Why doesn't my deck have an image in game?

In the same location you placed your .json file, you can add a PNG file with the same name as your .json file. 

**NOTE: Should be 256x256**

## What if I need help?

Hopefully you won't, but it's possible the program may not work in edge cases or documentation should include more information. Either way, please [file an issue](https://github.com/kmccmk9/Yu-Gi-Oh-TTS-Deck-Builder/issues).
