# hlback
- Simple, command-line-based, highly space-saving backups in Windows or Linux.
- Uses hard links to avoid duplicating files within or across multiple backups.
- Combines ease of restoration of full backups with the disk space savings of incremental backups.


## Table of Contents
***
- [Introduction](#introduction)
- [Using Concert.js](#using-concertjs)
- [License](#license)
- [Bug Reports and Suggested Enhancements](#bug-reports-and-suggested-enhancements)
- [Contributing to Concert.js](#contributing-to-concertjs)
- [Authors](#authors)
- [Version History](#version-history)

## Introduction
***

### What is hlback?
hlback is a cross-platform console application used for straightforward, simple execution of backups while taking up as little space as possible. It works on Linux and Windows, and runs on .NET 5.

### Why would I want to use this particular backup utility?
The primary differentiating features of hlback are these:
- **It is very simple to use.** Many backup applications have overwhelming, huge GUIs with massive numbers of options hidden all over the place, trying to do everything imaginable, and making configuration a headache because you have to look at all these menus and settings to confirm the job will work as you want. This one just uses a very simple command line operation and an optional file listing a set of sources and inclusion / exclusion rules.
- **It de-duplicates your files, using hard links to reduce the space used to a tiny fraction of what is normally needed to keep a long-running set of backups.** What does that mean?
  - The first time a file is copied to a given destination, it is copied as normal.
  - Whenever after that a file exactly the same as the first file is encountered, a full, new physical copy isn't made. Instead, a file system feature called hard linking is used, where a new file is made that behind the scenes just points to the same physical data as the original file. So now you have two copies of the same file, but using the disk space of just one copy.
  - The first helpful consequence of this is that each new backup only uses up the amount of space needed to copy files that have changed since the last backup. This is a huge benefit. You could put a hundred backups of a 99 GB file set onto a 100 GB drive, if none of the files have changed in between backups.
  - The second helpful consequence of this is that when restoring from backups you don't have to worry about combining full backups with differential or incremental backups. Every single backup acts as a complete, full backup.
- **It runs on either Windows or Linux.** Linux has other utilities that do this kind of thing, but backup applications for Windows surprisingly almost all lack the ability to do this type of space-saving backup.

## Using hlback
- Prerequisites
    - Windows or Linux
	- .NET 5
- Setup
    - ADD THIS SECTION
- Usage
    - ADD THIS SECTION

## License
***
hlback is available for use under the [MIT License](LICENSE). Copyright belongs to Trajan McGill.

## Bug Reports and Suggested Enhancements
***
Visit the [project issues page](https://github.com/trajanmcgill/hlback/issues) to offer suggestions or report bugs.

## Contributing to hlback
***
(Or just messing around with and building the code yourself)
- Prerequisites
    - Windows or Linux
	- [git](https://git-scm.com/)
    - .NET

- Setup
	1. First, clone from GitHub:
		```
		git clone https://github.com/trajanmcgill/hlback.git
		```
	2. Open and work with the files in your favorite editor. Visual Studio Code is a nice, cross-platform option.

- Building:
    - On the command line, in the project directory: `dotnet build`

- Contributing changes:

    There is not presently a formal document describing contributions to this project. If you want to add functionality or fix bugs, please at least pay attention to the coding style that is evident in the existing source. Thanks.
## Authors
***
[Trajan McGill](https://github.com/trajanmcgill)

## Version History
***
See [releases page](https://github.com/trajanmcgill/hlback/releases) for version notes and downloadable files.
