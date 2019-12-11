# Disable Chrome's Developer Mode Extension Warning Popup

## Message to Chromium contributors
This project is not meant for malicious use, especially because patching requires administrative rights. If an attacker wants to get rid of that notification, they will always be able to do it somehow, since it clientsided. Additionally, you can just install a crx-file and allow it with some group policies, which makes absolutely no sense, because it punishes developers with this annoying popup, but crx files that are already packed can be installed easily.

The idea originates from that one stackoverflow patching method, which forces a command line option in the `chrome.dll`.

## How the program works
It discovers your `chrome.dll` file of the latest installed Chrome version. Then it performs a pattern scan for a function that gets patched, so the dialog does not appear anymore.

## Commandline Options
```bash
ChromeDevExtWarningPatcher.exe [-noDebugPatch] [-noWWWPatch] [-noWarningPatch] [-noWait]

Explanation:
-noDebugPatch: Disables the patch for the warning of debugging extensions (chrome.debugger)
-noWWWPatch: Disables the patch for re-adding the `https` or `www` in a url, because it matters!
-noWarningPatch: Disables the patch for the warning of developer extensions

-noWait: Disables the almost-pointless wait after finishing
```

## What is the pattern and what does it patch

### Developer Extension Warning
The Chromium open source project contains a file called `dev_mode_bubble_delegate.cc` and a function called `ShouldIncludeExtension`. Here, it checks if an extension is loaded via the command line or if it is unpacked and if this is true for at least one extension, the dialog will appear.

This means that the search pattern is a signature of the function.

```c++
bool DevModeBubbleDelegate::ShouldIncludeExtension(const Extension* extension) {
  return (extension->location() == Manifest::UNPACKED ||
          extension->location() == Manifest::COMMAND_LINE);
}
```

The tool patches the enum (Manifest) values to 0xFF, so both if statements are never true.
```javascript
Manifest::UNPACKED = 0x4
Manifest::COMMAND_LINE = 0x8
```

### Debugging Warning Patch
The tool is also able to fully remove the debugging warning. You used to be able to disable it with Chrome itself with the `silent-debugger-extension-api` flag, which has been removed in Chrome 79 ( >:( ).


### WWW/HTTPS Removal Patch
Chromium also decided to remove `www` and `https` from every url in the omnibox. You were able to disable it with a flag, which has been removed in Chrome 79 ( >:( ). This tool is also able to add these again.



## How and when to run it
Run it after every chrome update with administrator rights.