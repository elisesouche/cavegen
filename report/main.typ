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
textures, models...), in our case caves, for use in videogames from algorithms
and a source of randomness. We want to be able to click a button and obtain a
new, never seen before cave for the player to explore. We do not strive for
physical and geological accuracy, but rather for subjective criterias of
aesthetics and fun.

In this work I follow the technique proposed by #cite(<main>, form: "prose"). I
provide an implementation in the Godot game engine @godot and the C\#
programming language.

The generation is done in successive steps. First, we create the topological
layout of the cave. Then, we carve out the volume of the cave in a voxel field
following this layout. Finally, we create a mesh out of the volume.

= Creating the layout

The goal of the first step is to produce the layout of the cave. Conceptually, a
cave can be imagined to be a graph of intersections connected by corridors. Of
course, this is insufficient to capture the complexity a real cave. First, caves
in the real world often feature actual rooms and large spaces. We can recover
this by having several small rooms very close to each other. Second, corridors
are not straigh edges; they should bend and curve. We can emulate this by
creating intermediary control points along the edge. Therefore, the graph model
will do.

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
sets, $S in N$, and $cal(R)$ is a binary relation between $N$ and $(N union
T)^*$ known as the derivations. Given a sentence $s in (N union T)^*$, we derive
it by replacing every nonterminal $n$ in $s$ by _one of_ (chosen
non-deterministically) the sentences $d$ such that $(n, d) in cal(R)$. The
L-System can therefore generate sentences by doing successive derivations
starting from $S$.

The idea is that the symbols in our L-System will represent possible motions of
a turtle (in the sense of turtle graphics) that will be at the core of the next
step. Rules in our L-System will represent possible evolutions of the layout:
for instance, the nonterminal `X` can represent a crossing and the terminal $L$
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

#table(columns: 2, stroke: 0.5pt,
     [\[], [StartBranch]         ,
     [\]], [EndBranch]           ,
     [F] , [Forward]             ,
     [R] , [YawClockwise]        ,
     [L] , [YawCounterClockwise] ,
     [U] , [PitchUp]             ,
     [D] , [PitchDown]           ,
     [O] , [IncreaseAngle]       ,
     [A] , [DecreaseAngle]       ,
     [B] , [IncreaseStep]        ,
     [S] , [DecreaseStep]        ,
     [Z] , [BranchTip]           ,
     [0] , [BranchEnd]           ,
)

= Creating the volume
= Creating the mesh
= Rendering
= Results
= Further work

#show bibliography: set heading(outlined: false)
#bibliography("bib.bib")
