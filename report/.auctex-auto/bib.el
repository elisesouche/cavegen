;; -*- lexical-binding: t; -*-

(TeX-add-style-hook
 "bib"
 (lambda ()
   (LaTeX-add-bibitems
    "main"
    "godot"))
 '(or :bibtex :latex))

