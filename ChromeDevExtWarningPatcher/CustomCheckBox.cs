using System.Windows.Controls;
using System.Windows.Media;

namespace ChromeDevExtWarningPatcher {
    class CustomCheckBox : CheckBox {
        public int Group;

        public CustomCheckBox(string text, string tooltip, int group) : base() {
            Content = text;
            Foreground = BorderBrush = new SolidColorBrush(Color.FromRgb(202, 62, 71));
            Group = group;

            if (tooltip != null)
                ToolTip = tooltip;
        }

        public CustomCheckBox(GuiPatchGroupData patchGroupData) : this(patchGroupData.Name, patchGroupData.Tooltip, patchGroupData.Group) {
            IsChecked = patchGroupData.Default;
        }

        public CustomCheckBox(string text) : this(text, null, -1) { }
    }

    struct GuiPatchGroupData {
        public string Name, Tooltip;
        public int Group;
        public bool Default;
    }
}
