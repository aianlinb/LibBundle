# LibBundle
Library for handling .bundle.bin in GGPK of game PathOfExile after version 3.11.2
# FileListGenerator
Generate a FileList.yml that shows all file paths that all .bundle.bin contain.

Usage:
  1. Put the _.index.bin and the program in the same folder.
  2. Run the program.
# BundleExporter
Console program for export files in .bundle.bin

Usage:
  1. Use VisualGGPK export all .bundle.bin you wanna load. (Must contain _.index.bin)
  2. Put BundleExporter.exe into Bundles2 folder.
  3. Run the program.
# VisualBundle
The program for editing .bundle.bin

Usage:
  1. Use VisualGGPK export all .bundle.bin you wanna load. (Must contain _.index.bin)
  2. Run the program and select _.index.bin.

Function of the buttons:
  - Export: Export the selected file or directory.
  - Replace: Replace the selected file with a new file
  - ReplaceAll: Select a directory, and all the files in it will replace all found files in loaded bundles.
  - MoveTo: Move the selected file or directory to another loaded bundle.
  - Addfiles: Select a directory, and all the files in it will be written to selected bundle. Note that only files define by _.index.bin can be added. This do the same with MoveTo, but this won't delete the old file.
  - Open: Export the file to temporary folder and open it.
  - Save: All actions that change files will be executed after click this.
