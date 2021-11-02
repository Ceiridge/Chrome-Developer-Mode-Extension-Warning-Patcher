# Disable Chromium's and Chrome's Developer Mode Extension Warning Popup & Elision WWW/HTTPS Hiding & Debugging Extension Popup
**Download** it in the [release section](https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/releases). The [.NET 5 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime) is required. All patterns and patches auto-update with the `patterns.xml` on every install.\
Note: It seems like Chrome has completely disabled the warning popup anyway, but this patcher still provides other useful patches.

## Supported browsers
See below for the custom paths (commandline option).
```javascript
(✓ represents mostly supported and tested browsers)
All x64 bit Chromium-based browsers, including:
- Chrome ✓
- Chromium ✓
- Edge ✓
- Brave ✓
- Ungoogled Chromium ?
- Opera ?
- Yandex Browser
- Vivaldi
- Blisk
- Colibri
- Epic Browser
- Iron Browser
```

## Features

- Intuitive installer GUI
- Autodetection of browser installations
- Patcher injector using Event Traces for Windows to minimize cpu usage and to maximize speed
- Compatibility for Windows 7 - Windows 11
- SIMD (AVX2) accelerated pattern searching with a fallback for old CPUs
- Very well documented patterns.xml
- Easy to compile

### What can it patch

Read the [patterns.xml](https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/blob/master/patterns.xml) file for more information.
- Remove extension warning (Removes the warning => main purpose of the patcher)
- Remove debugging warning (Removes warning when using chrome.debugger in extensions)
- Disable Elision (Force showing WWW and HTTPS in the url bar/omnibar)
- Remove crash warning (Remove the "Chromium crashed" popup)
- Remove send to self (Remove the menu option "Send To Your Devices" when using Google Sync)
- Remove QR code generation (Remove the context menu option "Create QR code for this page")

## Gui Screenshot
![Gui Screenshot](https://raw.githubusercontent.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/master/media/guiscreenshot.png)

## Commandline Options
All commandline options are **optional** and not required. If none are given, the gui will start. **Warning**: The inferior command line cannot uninstall the entire patcher and if you run it with customPath, all other installations will be removed!

```
ChromeDevExtWarningPatcher.exe 
  --groups           Set what patch groups you want to use. See patterns.xml to get the group ids (comma-seperated: 0,1,2,etc.)

  -w, --noWait       Disable the almost-pointless wait after finishing

  --customPath       Instead of automatically detecting and patching all chrome.exe files, define a custom Application-folder path
                     (see README) (string in quotes is recommended)

  --help             Display this help screen.

  --version          Display version information.
```

**Recommended `customPath`s:**
```java
Chrome (default): "C:\Program Files (x86)\Google\Chrome\Application"
Brave: "C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application"
Edge: "C:\Program Files (x86)\Microsoft\Edge\Application"

Remember: The folder of the path always needs to include the latest version folder of the browser (e. g. 83.0.1123.123).
(Create a new issue with a path, if you want to contribute to this list.)
```
Find more paths [here](https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/tree/master/ChromeDevExtWarningPatcher/InstallationFinder/Defaults).

## Contributing
Clone this repository with `git clone --recursive https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher.git` and open the `.sln` file with Visual Studio 2019 or newer.

## Message to Chromium contributors
This project is not meant for malicious use, especially because patching requires Administrator rights. If an attacker wants to get rid of that notification, they will always be able to do it somehow, since they have access to the computer and to other methods anyway. For example, you could just install a crx-file and allow it with group policies. This makes no sense, because it punishes developers with annoying popups, but crx files that are already packed - and not on the store - can strangely be installed easily.

The idea originates from an answer on StackOverflow that also patched the `chrome.dll` and used to work on old versions.

Used open source libraries:
- [dahall/taskscheduler](https://github.com/dahall/taskscheduler)
- [dahall/Vanara](https://github.com/dahall/Vanara)
- [MaterialDesignInXAML/MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [commandlineparser/commandline](https://github.com/commandlineparser/commandline)

## Copyright
Chrome-Developer-Mode-Extension-Warning-Patcher is released into the public domain according to the GPL 3.0 license by the copyright holders.

Disclaimer: This repository and the used names "Chrome", "Chromium", "Edge" in this project are not affiliated with or endorsed by Google LLC, Microsoft, The Chromium Project, Microsoft Edge, Google Chrome or other third parties. This repository and the used names "Chrome", "Chromium", "Edge" are also not affiliated with any existing trademarks.

No code was copied or used from any other browser in this repository. Chromium is licensed under the open source BSD License.

This repository does not infringe any copyright of proprietary browsers, as it only patches bytes on the end user's computer, without having any copyright-protected code or text included in this repository.
