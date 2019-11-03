

#### Kronal Utils for KSP
------------------

As I haven't been able to continue developing my KSP related stuff,
I'm now releasing it into the public domain using the Unlicense --
no strings attached nor nazti viral licences, do whatever you want with it! :-)

Be warned that this is a dump of my personal utilities, and it's not that
much documented and it's a mixed bag of different things.

The two zip files contain the blender models I created for my parts and the compiled version my stuff (which includes the shaders, DAEs and textures).

In `src\` you find:

#### Axes in the editor
------------------
This is named `KRSEditorAxis` and does what the name says, shows 3 cartesian axes in the vessel editor centered around the center of mass of your ship.

The nice thing about it is that it hides some of the axes depending on your position.

[Here's a YouTube video of it.](https://www.youtube.com/watch?v=fvQ4SPKGc0M)

 
#### ~~Hinges and stepper motors~~
-------------------------

~~`KRSHinge` is a part module that can turn anything into a stepper motor I did on my own for kicks and giggles. The difference of this in regards to competing implementations is that~~

~~1. it is not bound to draconian licensing and~~

~~2. that it can actually snap to angles, and make the joint actively hold a position -- i.e. the joint compensates when you place a heavier object and does not move down.~~

Removed.  This fork will not be supporting these parts

#### ~~Binding keys and analog gamepad controls in the VAB~~
---------------------------------------------------

~~Both `KRSControl` and `KRSInputAttribute` enable using a graphical interface to bind keys and gamepad controls (including analog ones) to my part modules.~~

~~For now this only works with `KRSHinge` but I don't see why it couldn't be made work with other things.~~

~~When I developed this I wanted to make it less intrusive, i.e. not needing to modify your part module to make it compatible with this, but I haven't put much thought on this since so it's the way it is.~~

~~**NOTE**: There is some bug in this that borks the placement of struts, so be warned.~~

Removed.

#### Shader material properties parser
---------------------------------

In `MaterialProperties` you'll find the class `ShaderMaterial` than can be used to read from a shader material the properties you can tweak in it (the same you'd see if you were to open it in Unity3D).

I use this in `KRSVesselShotUI` to allow the user to edit shader properties from the VAB.

#### Vessel screenshot utility
-------------------------

This is contained in `KRSVesselShot`, `KRSVesselShotUI` and `VesselViewConfig`. The intent of this is to be a tool for taking a screenshot that covers all your spacecraft, so that you can show it to other people and so on.

It proves a way to make exploded views, hiding some parts, and use orthographic projection.

`VesselViewConfig` can also useful for other projects that need to hide stuff in the VAB.

Here's how it looks configuring it to use no-color and orthographic projection:

![Screenshot](http://i.imgur.com/aWJVCsz.png)

and here's how it looks with coloring and perspective projection:

![Screenshot2](http://i.imgur.com/ByToBdP.jpg)

That's it!

-- Kronal


#### v0.0.0a

* Made 'Stable' with Stock KSP v0.23.0
