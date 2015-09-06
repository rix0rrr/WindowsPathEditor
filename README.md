Windows Path Editor
===================

This tool helps you manage your PATH on Windows.

[Download Latest Version (1.4)](https://github.com/rix0rrr/WindowsPathEditor/releases/download/1.4/windowspatheditor-1.4.zip)

Introduction
-----------

In a fit of horrible irony, on Windows you'll both have the most need to edit
your PATH (since all applications insist on creating their own `bin`
directories instead of installing to a global `bin` directory like on Unices),
and you're also equipped with the absolute worst tools to deal with this. The
default environment editor dialog where you get to see 30 characters at once if
you're lucky? Yuck.

*Windows Path Editor* (a horribly creative name, I know) gives you a
better overview and easier ways to manipulate your path settings.

Features
-----------

- Edit your path using drag and drop.
- Detect conflicts between directories on your path (diagnose issues like the
  wrong executable being launched or the wrong DLL being loaded).
- Remove bogus entries from your path with a single click.
- Scan your disk for tools that have a `bin` directory and automatically add
  them to your path.
- UAC aware.

![Screen Shot of Windows Path Editor](https://raw.github.com/rix0rrr/WindowsPathEditor/master/screenshot.png)
