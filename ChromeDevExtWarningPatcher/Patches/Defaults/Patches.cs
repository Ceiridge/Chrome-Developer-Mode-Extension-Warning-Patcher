namespace ChromeDevExtWarningPatcher.Patches.Defaults {
    public class RemoveExtensionWarningPatch1 : BytePatch {
        public RemoveExtensionWarningPatch1() : base(new RemoveExtensionWarningPattern(), 0x04, 0xFF, 22, 0x04, 0xFF, 20) { }
    }

    public class RemoveExtensionWarningPatch2 : BytePatch {
        public RemoveExtensionWarningPatch2() : base(new RemoveExtensionWarningPattern(), 0x08, 0xFF, 35, 0x08, 0xFF, 32) { }
    }

    public class RemoveDebugWarningPatch : BytePatch {
        public RemoveDebugWarningPatch() : base(new RemoveDebugWarningPattern(), 0x41, 0xC3, 0x00, 0x55, 0xC3, 0x00) { }
    }

    public class RemoveElisionPatch : BytePatch {
        public RemoveElisionPatch() : base(new RemoveElisionPattern(), 0x56, 0xC3, 0x00, 0x55, 0xC3, 0x00) { }
    }
}
