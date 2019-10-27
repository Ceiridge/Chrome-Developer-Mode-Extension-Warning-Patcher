# Disable Chrome's Developer Mode Extension Warning Popup

## Message to Chromium contributors
This project is not meant for malicious use, especially because patching requires administrative rights. If an attacker wants to get rid of that notification, they will always be able to do it somehow, since it clientsided. Additionally, you can just install a crx-file and allow it with some group policies, which makes absolutely no sense, because it punishes developers with this annoying popup, but crx files that are already packed can be installed easily.

The idea originates from that one stackoverflow patching method, which forces a command line option in the `chrome.dll`.

## How the program works
It discovers your `chrome.dll` file of the latest installed Chrome version. Then it performs a pattern scan for a function that gets patched, so the dialog does not appear anymore.

## What is the pattern and what does it patch
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

## How and when to run it
Run it after every chrome update with administrator rights.