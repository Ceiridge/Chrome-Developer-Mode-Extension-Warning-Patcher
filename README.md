# Disable Chromium's and Chrome's Developer Mode Extension Warning Popup & Elision WWW/HTTPS Hiding & Debugging Extension Popup
**Download** it in the [release section](https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/releases).

## Supported browsers
See below for the custom paths (commandline option).
```javascript
- All x64 and x86/x32 bit Chromium-based browsers, including:
- Chrome ✓
- Chromium
- Brave ✓
- New Edge ✓
- Opera?
- Vivaldi
- Blisk
- Colibri
- Epic Browser
- Iron Browser
- Ungoogled Chromium?
```

## Message to Chromium contributors
This project is not meant for malicious use, especially because patching requires administrative rights. If an attacker wants to get rid of that notification, they will always be able to do it somehow, since it clientsided. Additionally, you can just install a crx-file and allow it with some group policies, which makes absolutely no sense, because it punishes developers with this annoying popup, but crx files that are already packed can be installed easily.

The idea originates from that one stackoverflow patching method, which forces a command line option in the `chrome.dll`.

## How the program works
It discovers all `chrome.dll` files of the latest installed Chromium browsers. Then it performs a pattern scan for functions that getspatched, so the dialog does not appear anymore.

## Gui Screenshot
![Gui Screenshot](https://raw.githubusercontent.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher/master/media/guiscreenshot.png)

## Commandline Options
All commandline options are **optional** and not required. If none are given, the gui will start.

```
ChromeDevExtWarningPatcher.exe 
  --disableGroups    Set what patch groups you don't want to use. See patterns.xml to get the group ids (comma-seperated: 0,1,2)

  -w, --noWait       Disable the almost-pointless wait after finishing

  --customPath       Instead of automatically detecting and patching all chrome.dll files, define a custom Application-folder path
                     (see README) (string in quotes is recommended)

  --help             Display this help screen.

  --version          Display version information.
```

**Recommended `customPath`s:**
```java
Chrome (default): C:\Program Files (x86)\Google\Chrome\Application
Brave: C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application
Edge: C:\Program Files (x86)\Microsoft\Edge\Application

Remember: The path always needs to include the version folders of the browser.
Please create a new issue with a path, if you want to contribute to this list.
```

## Contributing
Clone this repository with `git clone --recursive https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher.git` and open the `.sln` file with Visual Studio (Community 2017, newer versions should also work).

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

The Chromium open source project contains a file called `global_confirm_info_bar.cc` and a function called `MaybeAddInfoBar`, which is responsible for showing that warning and another one which is not of importance (automation warning; ChromeDriver).

This patch just instantly returns this:

```c++
void GlobalConfirmInfoBar::MaybeAddInfoBar(content::WebContents* web_contents) {
  InfoBarService* infobar_service =
      InfoBarService::FromWebContents(web_contents);
  // WebContents from the tab strip must have the infobar service.
  DCHECK(infobar_service);
  if (base::Contains(proxies_, infobar_service))
    return;

  std::unique_ptr<GlobalConfirmInfoBar::DelegateProxy> proxy(
      new GlobalConfirmInfoBar::DelegateProxy(weak_factory_.GetWeakPtr()));
  GlobalConfirmInfoBar::DelegateProxy* proxy_ptr = proxy.get();
  infobars::InfoBar* added_bar = infobar_service->AddInfoBar(
      infobar_service->CreateConfirmInfoBar(std::move(proxy)));

  // If AddInfoBar() fails, either infobars are globally disabled, or something
  // strange has gone wrong and we can't show the infobar on every tab. In
  // either case, it doesn't make sense to keep the global object open,
  // especially since some callers expect it to delete itself when a user acts
  // on the underlying infobars.
  //
  // Asynchronously delete the global object because the BrowserTabStripTracker
  // doesn't support being deleted while iterating over the existing tabs.
  if (!added_bar) {
    if (!is_closing_) {
      is_closing_ = true;

      base::SequencedTaskRunnerHandle::Get()->PostTask(
          FROM_HERE, base::BindOnce(&GlobalConfirmInfoBar::Close,
                                    weak_factory_.GetWeakPtr()));
    }
    return;
  }

  proxy_ptr->info_bar_ = added_bar;
  proxies_[infobar_service] = proxy_ptr;
  infobar_service->AddObserver(this);
}
```

### WWW/HTTPS/Elision Removal Patch
Chromium also decided to remove `www` and `https` from every url in the omnibox. You were able to disable it with a flag, which has been removed in Chrome 79 ( >:( ). This tool is also able to add these again.

The Chromium open source project contains a file called `chrome_location_bar_model_delegate.cc` and a function called `ShouldPreventElision`, which is responsible for preventing the removal of url extensions.

This is patched by always instantly returning this:

```c++
bool ChromeLocationBarModelDelegate::ShouldPreventElision() const {
#if BUILDFLAG(ENABLE_EXTENSIONS)
  Profile* const profile = GetProfile();
  return profile && extensions::ExtensionRegistry::Get(profile)
                        ->enabled_extensions()
                        .Contains(kPreventElisionExtensionId);
#else
  return false;
#endif
}
```

## How and when to run it
Run it after every chrome/chromium update with administrator rights.
