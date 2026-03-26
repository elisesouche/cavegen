#let title = [Procedural Cave Generation]
#let author = "Élise Souche"
#set document(title: title)

#set page(margin: 2cm, numbering: "1/1")
#set par(
    leading: 0.55em,
    spacing: 0.55em,
    first-line-indent: (amount: 1.8em, all: true),
    justify: true
)
#set text(font: "New Computer Modern")
#show heading: set block(above: 1.4em, below: 1em)

#set heading(numbering: "1.1   ")

#block(
    fill: luma(230),
    inset: 1em,
    outset: (x: 0em, y: 1em),
    width: 100%,
    stroke: 0.1em + black,
    radius: 1em
)[
    #set align(center)
    #text(size: 14pt)[#title]

    #author

    CGDI Project, ENS de Lyon
]

#v(2cm)

#block(inset: (x: 2cm, y: 0cm))[
    #align(center)[*Abstract*]

    My goal in this project is to procedurally generate caves. The goal is to
    produce aesthetically pleasing caves, suitable for instance for use in
    videogames. It is not to produce a geologically accurate simulation of the
    formation of caves.
]

#v(1cm)

#outline()

= Overview

The goal of procedural content generation is to produce content (for instance,
textures, models...), in my case caves, for use in videogames from algorithms
and a source of randomness. We want to be able to click a button and obtain a
new, never seen before cave for the player to explore. We do not strive for
physical and geological accuracy, but rather for subjective criterias of
aesthetics and fun.

#figure(caption: [Screenshots from my little demo])[
    #grid(columns: (1fr, 1fr),
        image(width: 95%, "images/scrot1.png"),
        image(width: 95%, "images/scrot2.png")
    )
]

In this work I follow the technique proposed by #cite(<main>, form: "prose"). I
provide an implementation in the Godot game engine @godot and the C\#
programming language.

The generation is done in successive steps. First, we create the topological
layout of the cave. Then, we construct a volumetric representation of the cave
as a density field, following this layout. Finally, we create a mesh out of the
volume.

= Creating the layout <sec:layout>

The goal of the first step is to produce the layout of the cave. Conceptually, a
cave can be seen as graph of intersections connected by corridors. Of course,
this is insufficient to capture the complexity of a real cave. First, caves in
the real world often feature actual rooms and large spaces. We can recover this
by having several small rooms very close to each other. Second, corridors are
not straigh edges; they should bend and curve. We can emulate this by creating
intermediary control points along the edge. Therefore, the graph model will do.

In the real world, the most common type of caves is created by the flow of water
eroding the stone. Water cannot really flow cyclically. Therefore, it seems that
most caves should look more like trees than complex graphs. From personal
experience, I can however confirm that cyclic caves do exist; furthermore, from
a gameplay point of view, a cave full of dead ends is not really fun. The
approach we will use will therefore create a tree, then randomly connect some of
the leaves to form the final result.

How to create a tree? Here we turn to biology and the notion of L-Systems.
L-Systems are like context-free grammars, but instead a doing one derivation on
each step, we do all possible derivations. This can simulate the growth of
plants or bacteria colonies; it is, in general, well suited to generate
organic-looking shapes, and will be perfect for our caves.

Formally, a L-System is a tuple $(N, T, S, cal(R))$ where $N$ and $T$ are finite
sets (nonterminals and terminals), $S in N$ (initial item), and $cal(R)$ is a
binary relation between $N$ and $(N union T)^*$ (the rules). Given a sentence
$s in (N union T)^*$, we derive it by replacing every nonterminal $n$ in $s$ by
_one of_ (chosen non-deterministically) the sentences $d$ such that $(n, d) in
cal(R)$. The L-System can therefore generate sentences by doing successive
derivations starting from $S$.

The idea is that the symbols in our L-System will represent possible motions of
a turtle (in the sense of turtle graphics) that will be at the core of the next
step. Rules in our L-System will represent possible evolutions of the layout:
for instance, the nonterminal `X` can represent a crossing and the terminal `L`
represent a forward corridor, and we can have the rule `X -> XL` saying that a
corridor can grow from a crossing.

My implementation supports macros. They can have multiple possible expansions,
which are chosen at random. An example L-System I found generates decent caves
is this one (`@` denotes macros):

#align(center)[```
Z->@I@Q[@HZ][@T@C]
Z->@H@I[Z]@C@C

where @C = FRFRFRFR
      @C = FLFLFLFL
      @H = UFUFUFFFFDFDFDF
      @H = DFDFDFFUFUFUF
      @I = FF
      @Q = F[RRFZ][LLFZ]FZ
      @T = UFUFUFFFZFDFDFDF
      @T = DFDFDFZFUFUFUF
```]

Only `[` and `]` are terminals. The meaning of the symbols, that is, how the
turtle will interpret them, is as follow:

#grid(columns: (1fr, 1fr), [
    #set align(center)
    #table(columns: 2, stroke: 0.5pt,
        [\[], [Start Branch]         ,
        [\]], [End Branch]           ,
        [F] , [Forward]             ,
        [R] , [Yaw Clockwise]        ,
        [L] , [Yaw Counter Clockwise] ,
        [U] , [Pitch Up]             ,
        [D] , [Pitch Down]           ,
    )
], [
    #set align(center)
    #table(columns: 2, stroke: 0.5pt,
     [O] , [Increase Angle]       ,
     [A] , [Decrease Angle]       ,
     [B] , [Increase Step]        ,
     [S] , [Decrease Step]        ,
     [Z] , [Branch Tip]           ,
     [0] , [Branch End]           ,
)])

= Creating the volume

Now that we have the layout of our cave, we will create a volumetric
representation of it. We will represent the density of rock (a function $RR^3 ->
RR$) as a voxel field. Then, we will follow the layout to carve the cave in this
field. As explained in @sec:layout, the sentence produced by the L-System can be
interpreted as instructions for a turtle. We will equip this turtle with a
brush, that is, a metaball: a function $RR^3 -> RR$. On each structural point of the
layout, the voxel field is updated by subtracting the brush from it.

Different brushes were experimented with. The simplest brush is simply a
sphere:

$
    x mapsto cases(
        1 "if" x <= "radius",
        0 "otherwise"
    )
$

A slight complexification that gives more interesting fusions between different
brush strokes is to add a smooth transition from $1$ to $0$. In my experiments,
I found that simply adding low amplitude simplex noise to such a smooth brush
gives natural-looking results. #cite(<main>, form: "prose") use different kinds
of anisotropic noise to give the look of erosion, but I did not have time to
study this.

= Creating the mesh

Now that we have a volumetric representation of the density of our cave, we can
turn to the many algorithms that can produce a mesh from such data. I used the
classic and simple approach of the Marching Cubes algorithm, as the authors of
@main did, but it should be possible to use other approaches.

== The marching cubes algorithm

The marching cubes algorithm is a classic algorithm for solving the following
problem: given a scalar field $S: RR^3 -> RR$, we want to compute a mesh of the
_isosurfaces_: surfaces where $S$ is constant. Here we will take the isosurface
where $mono("RockDensity") = 0$. To do this, we will subdivide the area in cubes
(here, we already have voxels) and process each cube independently. For each
cube, each vertex of the cube is classified as being either on the inside or the
outside of the surface. There are therefore 256 different configurations of
triangles to put in this cube (less, actually, thanks to symmetries. See
@fig:marchingcubes. Orange vertices correspond to vertices above the isovalue.).
We then have a precomputed table giving the configuration. Finally, the
positions of the vertices are interpolated along the edges of the cube by
linearly interpolating the difference in scalar values.

#figure(caption: [Cube configurations. By Ryoshoru (CC-BY-SA)])[
    #image(width: 70%, "images/MarchingCubesEdit.svg")
]<fig:marchingcubes>

== Implementation

From previous experience implementing this algorithm in Unity, I knew that it
would benefit from running on the GPU using compute shaders. Having little
experience with compute shaders in general and their Godot APIs in particular, I
drew heavy inspiration from #cite(<seblaguecubes>, form: "prose")'s
implementation. I first wrote a CPU implementation in C\#, which, has expected,
turned out to be too slow to produces actual caves. I then ported it to a
compute shader. After much hair-pulling from weird marshalling errors and Vulkan
pipeline issues, I finally got something that works. The performance is
unfortunately still not great, see @sec:performance for more details.

= Rendering

Once we have a mesh, we need to texture it and render it. I use the standard
physics-based rendering (PBR) shaders Godot provides. The only tricky part is
texture mapping, as we have not generated a UV map for our mesh. I therefore
used triplanar mapping. Triplanar mapping is a technique that projects the
texture on all three axis, takes three samples, and blends between them based on
the surface normal.

#figure(caption: [Triplanar blending. By Jasper Flick])[
    #image(width: 70%, "images/triplanar.jpg")
]

Although I originally wrote a shader for triplanar mapping myself, I ended up
using Godot's built-in implementation.

= Results

== Quality

Assessing the quality of the generated caves is difficult. Criteria of
aesthetics and fun are largely subjective. I will therefore give my own
subjective assessment.

The generated caves are relatively interesting. However, at large scales, they
do show their self-similarity and fractal nature, an inherent limitation of
L-System. The L-System I showed earlier gave the best results in my opinion.
However, the rooms it creates tend to take the shape of large holes that would
not be very navigable by a player.

In terms of visual quality, I had to keep the voxel resolution low for the
generation to take a reasonable amount of time (see @sec:performance).
Unfortunately, this shows in the final result, which looks somewhat blocky.
Also, I used a single texture for the whole cave, which is repetitive and not of
great quality.

Despite these limitations, I find that these do look like caves and are of
decent quality. I asked one other person who thought the same. Of course, this
assessment is not very scientific.

== Performance <sec:performance>

During development, I used two machines:
#align(center)[#table(columns: 4, stroke: 0.5pt,
    [], [CPU], [RAM], [GPU],
    [consumer laptop], [Intel Core i5-7200U], [8 GB], [NVIDIA GTX 950M],
    [mobile workstation], [Intel Core i7-13800H], [32 GB], [NVIDIA RTX 2000 Ada]
)]

For a long time, the performance of the generation was abysmal. Generating
decently-sized caves was simply impossible. I thought that the bottleneck was
the marching cubes implementation, which I spent a long time trying to optimize.
However, after doing a bit of profiling, I found out that it was actually the
application of the brush. Optimizing it made the performance a lot more
bearable. If not real-time yet, I found that I was able to generate pretty large
caves in less than five seconds on my workstation.

= Further work

== Performance optimizations

To my surprise, the main bottleneck is the application of brushes. I did not
have time to improve it as much as I could. I think a lot of benefit could be
obtained by parallelizing it: the application of brushes at different points of
the volume is conceptually independent. However, it is not so easy to implement
due to some Godot APIs not being thread-safe. With more time, I could have tried
to work around them. This code could even potentially be made into a compute
shader. There are also certainly a lot of other places with redundant
or inefficient calculations.

== Tweaking settings

The L-System I used appeared decent enough, but I could not reproduce the
results of #cite(<main>, form: "prose"). A lot of manual tweaking is necessary
to find satisfactory settings.

Other improvements in visual quality could come from: using more complex brushes
to emulate erosion creaks, switching from marching cubes to dual contouring or
other methods to try to eliminate blockiness, finding better looking shaders and
textures.

== Adding details

Caves in the real world often exhibit small scale geological features such as
stalactites/stalagmites, little pebbles on the floor, traces of sediment
deposition on the walls... A simple approach to adding these details would be to
lay them randomly in suitable places, spawning premade assets.

An other nice feature could be the addition of running water. It can be small
droplets dropping from the ceiling to show that the cave is humid, or full-blown
underground rivers. The latter would probably be much more complex.

#show bibliography: set heading(outlined: false)
#bibliography("bib.bib")
