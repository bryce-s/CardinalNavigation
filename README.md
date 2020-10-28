# Directional Window Selection - Visual Studio Extension

[![Build status](https://ci.appveyor.com/api/projects/status/auuht1u7eyasg65u?svg=true)](https://ci.appveyor.com/project/bryce-s/directionalwindownavigation)

A better way to change your active window from the keyboard.

--------------------


Download this extension from the (VS Gallery). See the changelog for release notes.

Inspired by [bspwm](https://github.com/baskerville/bspwm).

## Features

![](demonstration.gif "demonstration")

Adds four new commands to Visual Studio.

- **Window: Navigate Left** - Select the tool or document window to the left of the active window.
- **Window: Navigate Right** - Select the tool or document window to the right of the active window.
- **Window: Navigate Up** - Select the tool or document window above the active window.
- **Window: Navigate Down** - Select the tool or document window below the active window.

In cases where multiple windows are eligible in a given direction, the window with the largest border or overlap will be selected.

The commands work both in the main Visual Studio instance and in floating windows.

## Usage

You can choose new shortcuts for the commands under **Tools -> Options -> Environment -> Keyboard**.

![](options.jpg)


## Contributing



