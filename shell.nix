{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  buildInputs = [
    pkgs.dotnet-sdk
    pkgs.godot_4-mono
    pkgs.omnisharp-roslyn
    pkgs.csharpier
  ];
}
