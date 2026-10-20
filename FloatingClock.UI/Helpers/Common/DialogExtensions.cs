using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FloatingClock.UI.Helpers.Common;

public static class DialogExtensions
{
    extension(Window parent)
    {
        public bool PromptConfirmation(string message, string title)
        {
            var dialog = new ConfirmationDialog(message, title)
            {
                Owner = parent
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.IsConfirmed;
            }

            return false;
        }
    }

}
