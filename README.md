# pdn-heicfiletype-plus

A [Paint.NET](http://www.getpaint.net) filetype plugin that allows HEIC images to be loaded and saved with transparency.

## Installation

1. Close Paint.NET.
2. Place HeicFileTypePlus.dll, HeicFileTypePlusIO_x86.dll and HeicFileTypePlusIO_x64.dll in the Paint.NET FileTypes folder which is usually located in one the following locations depending on the Paint.NET version you have installed.

  Paint.NET Version |  FileTypes Folder Location
  --------|----------
  Classic | C:\Program Files\Paint.NET\FileTypes    
  Microsoft Store | Documents\paint.net App Files\FileTypes

3. Open the Windows Run dialog (Start > Run or `Windows Key` + `R`)
4. Type `paintdotnet:/set:FileTypes/BuiltInHEICFileTypeEnabled=false` and press the `Enter` key
5. Restart Paint.NET.

## License

This project is licensed under the terms of the GNU General Public License version 3.0.   
See [License.md](License.md) for more information.

# Source code

## Prerequisites

* Visual Studio 2019
* Paint.NET 4.3.2 or later

## Building the plugin

* Open the solution
* Change the PaintDotNet references in the HeicFileTypePlus project to match your Paint.NET install location
* Update the post build events to copy the build output to the Paint.NET FileTypes folder
* Build the solution

## 3rd Party Code

This project uses the following libraries. (the required header and library files are located in the `src/deps/` sub-folders).

* [libheif](https://github.com/strukturag/libheif)
* [libde265](https://github.com/strukturag/libde265)
* [x265](https://bitbucket.org/multicoreware/x265)