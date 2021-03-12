# hlback
- Simple, command-line-based, highly space-saving backups in Windows or Linux.
- Uses hard links to avoid duplicating files within or across multiple backups.
- Combines ease of restoration of full backups with the disk space savings of incremental backups.


## Table of Contents
***
- [Introduction](#introduction)
- [Using hlback](#using-hlback)
- [FAQ](#faq)
- [License](#license)
- [Bug Reports and Suggested Enhancements](#bug-reports-and-suggested-enhancements)
- [Contributing to hlback](#contributing-to-hlback)
- [Authors](#authors)
- [Version History](#version-history)

## Introduction
***

### What is hlback?
hlback is a cross-platform console application used for straightforward, simple execution of backups while taking up as little space as possible. It works on Linux and Windows, and runs on .NET 5.

>IMPORTANT NOTE: hlback is still on a pre-1.0 release, and while it is believed to be working properly, some testing and handling of error conditions is not fully complete yet. Feel free to use, but please test for your own setup to make sure backups are identical to the originals (in other words, use on production or essential systems at your own risk).

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
	- A file system on the backup drive that supports hard links (e.g., ext3, ext4, or NTFS). If you are using a hard drive for your backups on Windows or Linux, chances are high that you are using one of these file systems. USB sticks, however, are likely to be using a FAT file system which does not have a hard linking feature.
- Setup
    - Visit the [releases page](https://github.com/trajanmcgill/hlback/releases) to download the version for your operating system. There is no installer; just extract from the archive into any directory and run it from there.
- Usage
> hlback [--MaxHardLinkAge AGE] [--MaxHardLinksPerFile LINKSCOUNT] [--SourcesFile SOURCESFILE] [SOURCE1 [SOURCE2] ...] DESTINATION
>
> Backs up files from each SOURCE path to a time-stamped directory in DESTINATION path.
>
> Where possible, creates hard links to previous backup copies instead of new full copies, subject to the below rules:
>
> - SOURCE1 [etc.], DESTINATION: The last path specified on the command line will be interpreted as the destination path in which to put the backups. Every path prior to that will be interpreted as a list of source paths from which to copy. Listing source paths this way is easy for a simple copy-everything backup, but if anything needs to be excluded from the backup, specifying a sources file as decribed below is needed.
>
> - [optional] --SourcesFile or -SF (or, on Windows only, /SourcesFile or /SF):
>        If specified, will read a list of sources from the file SOURCESFILE.
>        File must contain text defining a series of one or more source paths as follows:
>   - A line declaring a path to the source file or directory to be backed up, followed (for directory sources) by any number of lines each starting with one of the below characters followed by a regular expression which is used to determine if the rule applies to an item. The regular expression is tested against the path and file name of each item relative to the container of the base path specified as a source, so if the source path is `/foo/bar/` then for the file `/foo/bar/asdf.txt` then the base path is `/foo/bar/`, its container would be `/foo/`, and the regular expression would be tested against the portion `bar/asdf.txt`.
>      - `+` indicates an inclusion rule. So the line `+foo` means any item where the path includes the string `foo` will be included.
>      - `-` indicates an exclusion rule. So the line `-foo` means any item where the path includes the string `foo` will be included.
>      - `!` indicates a full-tree exclusion rule. So the line `!foo` means any directory where the path includes the string `foo` will be totally ignored. It won't be traversed at all. (Whereas a simple exclusion rule on a directory will still result in searching for files and directories beneath that directory looking for items that should be included based on other rules.)
>   - Each item within the source directory and its subdirectories will be tested against each regular expression rule for inclusion or exclusion.
>   - Rules are applied in the order defined, and all rules are applied to each item within that source path.
>
> - [optional] --MaxHardLinkAge or -MA (or, on Windows only, /MaxHardLinkAge or /MA):
        If specified, will limit hard links to targets that are under AGE days old (creates a new full copy if all previous copies are too old).
>
> - [optional] --MaxHardLinksPerFile or -ML (or, on Windows only, /MaxHardLinksPerFile or /ML):
        If specified, will limit the number of hard links to a particular physical copy to LINKSCOUNT (creates a new full copy if this number would be exceeded).

## FAQ
- **How do I restore a backup?**
  
  Every backup acts as a full backup, so you should be able to simply copy everything back to where it belongs.

- **What if I delete one of the backed-up files? Will the hard links to that file no longer work?**

  Good question, but no. The way hard links work is that all of the files, including the original copy, are just pointers to a physical copy on the disk. The file system keeps count of how many links exist to a given physical file, and only deletes the physical file when all of the links to it have been deleted. So you can feel free to delete unwanted backups without fearing they will damage later backups that linked to the same files.

- **What is this .hlbackdatabase file that shows up in my destination directory?**

  That is a database of all the files copied in previous backups to that destination directory, and their file hashes. It is used for matching up duplicate files so the process knows when it can use a hard link and when it needs a new physical copy because the file doesn't already exist anywhere in the backup set.

- **So will that database be out of date and work wrong if I delete some of my previous backups?**

  It will be out of date in the sense that it will have records of files that no longer exist. But hlback always checks to make sure previously backed-up files still exist and are unchanged (in file size and modification date) before creating a hard link to them. If they have been deleted, the database records get cleaned up automatically.

- **Okay, what if I delete the .hlbackdatabase file itself?**

  The next run of hlback that tries to copy to that destination won't know about any of the files previously copied there, so it will fully copy every file. It will work just fine, but it will take up all the disk space of a full copy. It will, in the process, re-create the database file, though, so the next run after that will again be able to make full use of hard links for reducing space.

- **Why don't I get much space back from deleting a backup?**

  Remember, hard linking means each backup actually takes up much less space than it appears, since many files are really just links to other files. Deleting them won't free any more space than it used to make the backup in the first place.

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
