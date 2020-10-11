# LibBundle
Library for handling *.bundle.bin in GGPK of game PathOfExile after version 3.11.2
# VisualBundle
The visual program for editing *.bundle.bin

Usage:
  1. Use VisualGGPK to extract all *.bundle.bin you wanna load. (Must contain _.index.bin)
  2. Run the program and select _.index.bin.

Function of the buttons:
  - Export: Export the selected files or directories.
  - Replace: Replace the selected file with a new file
  - Import: Select a directory on your disk, and all the files in it will be written to the smallest bundle of loaded bundles. Note that only files defined by _.index.bin can be imported.
  - Open: Export the file to a temporary folder and open it.
  - Save: Apply the above actions.
# FileListGenerator
Generate a FileList.yml that shows all file paths that all *.bundle.bin contain.

Usage:
  1. Put the _.index.bin and the program in the same folder.
  2. Run the program.
# BundleExporter
Console program for export files in .bundle.bin

Usage:
  1. Use VisualGGPK to extract all *.bundle.bin you wanna load. (Must contain _.index.bin)
  2. Put BundleExporter.exe into Bundles2 folder (where _.index.bin is).
  3. Run the program.
