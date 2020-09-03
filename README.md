# FireSpreadingUnity



## A fire spreading simulation using a compute shader, built in Unity 2020.1.3f1.

This small project explores how a compute shader written in HLSL can be used to simulate fire spreading across foliage, using Unity built-in tree systems. No game-object trees here, no sir, we're not that kind of establishment.

### Why not use GameObjects to represent trees?

The tree system in Unity is fairly good - it can handle a lot of tree instances, provides LOD-ing out of the box and plays fairly well with the other system Unity provides (e.g. lighting). One gripe a game developer can have with them is that they are not handled in a component or gameobject manner. A Unity terrain only has an array of TreeInstance structs that stores the data on the trees, but this setup is quite resistant to changes - for example, changing a prototype for an existing TreeInstance during runtime throws an exception, so does changing the position of the instance. There are some additional gripes when working with the instances, for example their position is stored as normalized (0-1) position in 2 dimensions of the terrain's heightmap. Compare that with any GameObject that has its own 3D transform. These (and other) limitations are in place so that the system could handle a lot of trees at once and make sense in that context, but for a game where you might want to interact with the trees, it complicates things. That's why a lot of developers opt to use GameObjects to represent their trees. Those are much more expensive than simple structs in an array though, memory wise as well as CPU-cycle-wise, as GameObjects need to be ticked etc. It's also a lot more work to just design levels using these than just drawing tree instances using Unity's native tools.

That's why I wanted to work with the tree solution that is native to Unity - using tree instances, tree prototypes etc. - so that I could avoid costly game-object-focused approaches and leverage the performance of the native tree solution.

It turns out that with a bit of persuasion, the limitations can be worked around.

### How does this solution work?

The trees placed on the terrain are not only stored in the tree instances array, but I'm storing their position in a 2D texture as pixel data.

The tree instances are also marked in a simple quad tree so that they could be relatively quickly found through spatial checks (on user click etc.).

For each terrain tile, a FireHandler class is created - this gets an instance of a compute shader, feeds it its 2D texture with tree positions and let's the shader do the work.

The shader spreads the fire based on a few variables (wind speed, wind direction and 'natural fire spread speed'), marking the burning trees in the texture.

When a tree is marked in the texture by the shader, the data about the burning tree is set into a buffer that is then read in the FireHandler. The newly burnt trees have their tree prototype swapped-out for a burning one.

### How does it run?

Quite fast, on my 1070Ti and Ryzen7 2700X, it runs at around 500 fps In-Editor for two 512x512 terrain tiles with 10 000 trees each. The computations done on the GPU are very quick, the ineffective bits are the passing of data from the compute shader to the CPU.

### Features of the DemoScene

The DemoScene provided in this repo currently has a simple GUI for controlling the simulation, allowing the user to adjust the wind speed, wind direction, the natural speed of fire spreading and other the step-time for the compute shader. Most of the functionality is self-evident, I think.

### Any issues?

- The quad tree solution works, but it's definitely not perfect. This can be seen when the user clicks trees that are bunched together or when a tree is not removed/toggled on fire when clicked, unless the cursor position was precise.
- My Fire Spreading solution works by switching a tree prototype (e.g. from 'living tree' to 'burning tree'). This way level designers could still work with the built-in Unity tools to place trees etc. - with one caveat, the mass place tree script in Unity places trees of all specified prototypes as far as I know, so if they wanted to use this magical button, they'd need to remove the burning and burnt prototypes first. I provide an alternative method for mass tree placement of the living prototype, but it's not linked into the terrain editing GUI.

- Also the current Fire Spreading solution only works with basically one tree 'type'. It would still work if you changed the tree prototype to for example live maple tree, burning maple tree and burnt maple tree, but it would not support for example conifers at the same time without some changes.
- I'm not currently handling trees burning out (the fire dying out etc.). Once a tree's a-burnin', it will burn forever. This could be handled for example by marking the time of the change of the burning tree in the texture in one of the channels as well and when checking this pixel, 'dousing the fire' if it has been burning long enough.
- I'm doing a normal physical raycast and handling it in the DemoManager's Update - I've experimented with the IPointerClickHandler interface, but the clicks on tree instances were often not even registered.

