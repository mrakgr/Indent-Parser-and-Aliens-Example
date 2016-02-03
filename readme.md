This repository contains some of the exercises I did for basic game playing in MonoGame. `aliens_grid.fsx` and `aliens_continuous.fsx` has the basic Space Invaders clone from the GVGAI library. Originally I had intended to port the library to .NET using the [IKVM tool](http://www.ikvm.net/) and that would have worked fine. Unfortunately, going over the source code for the library, I found a problem with that - I had absolutely no idea how it worked or how to adapt it for my purposes.

At around 540kb, the library is an absolute monster. No way I, a beginning student of machine learning could even know where to start with it. The codebase is an absolute mess of spaghetti code as well.

As stupid as it seems at first to translate 8k LOC just so I can plug in the Spiral AD library into it and run some neural nets on it, this is probably the first step on my journey to mastering reinforcement learning.

As one could expect, there is really more to machine learning than just knowing how to code up the algorithms – one also needs to set up the environment. As I am serious, I absolutely need to internalize every single piece of it. Perhaps in the future when the algorithms are better understood, all one might need to do will be to hook them up to raw pixel values, but that day is not today.

To be honest, I kind of pity the GVGAI devs for doing it in Java – it would have been significantly easier to do it in Scala on the JVM. It is a wonder they chose not to given its seamless interop with the rest of the JVM ecosystem and the general lack of Java ML users - those looking to pick up the library for the sake of RL would probably have appreciated doing it in a better language.

On the .NET platform, F# is a natural choice for writting a compiler, being the most mature member of the ML language family. I have some great ideas. I am betting that I could rewrite the thing to a fraction of its original size.

Well, I'll know in about a month.

Articles:

[Indentation parser](https://github.com/mrakgr/Indent-Parser-and-Aliens-Example/blob/master/indent_parser.md).

Dependecies:
Fparsec 1.0.2
MonoGame 3.4