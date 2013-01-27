/* toast
 * A console interface to the toast notification system in Windows 8.
 * Options described below.
 * Copyright Nels Oscar 2013.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ShellHelpers;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace toast
{
    class Program
    {
        private const String APP_ID = "Nels.Toaster";
        static void Main(string[] args)
        {
            String ToastTitle = null;
            String ToastBody = null;
            String ToastImage = null;
            Boolean wait = false;

            if (args.Length == 0)
            {
                Console.WriteLine("No args provided.\n");
                PrintInstructions();
            }
            else if (args.Length == 1)
            {
                if (args[0] == "?") PrintInstructions();
                else ShowToast(args[0]);
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-m":
                            if (i + 1 < args.Length)
                            {
                                ToastBody = args[i + 1];
                            }
                            else
                            {
                                Console.WriteLine("Missing argument to -m.\n Supply argument as -m \"message string\"\n");
                                Environment.Exit(-1);
                            }
                            break;
                        case "-t":
                            if (i + 1 < args.Length)
                            {
                                ToastTitle = args[i + 1];
                            }
                            else
                            {
                                Console.WriteLine("Missing argument to -t.\n Supply argument as -t \"bold title string\"\n");
                                Environment.Exit(-1);
                            }
                            break;
                        case "-p":
                            if (i + 1 < args.Length)
                            {
                                ToastImage = args[i + 1];
                            }
                            else
                            {
                                Console.WriteLine("Missing argument to -p.\n Supply argument as -p \"image path\"\n");
                                Environment.Exit(-1);
                            }
                            break;
                        case "-w":
                            wait = true;
                            break;
                        default: break;
                    }
                }
            }
            TryCreateShortcut();
            ToastNotification toast = ShowToast(ToastTitle, ToastBody, ToastImage);
            while (wait) ;
        }

        private static void PrintInstructions()
        {
            String inst = "Welcome to toast.\n" +
                          "Provide toast with a message and display it-\n" +
                          "via the graphical notification system.\n-Nels\n\n" +
                          "---- Usage ----\n" +
                          "toast <string>|[-t <string>][-m <string>][-p <string>]\n\n" +
                          "---- Args ----\n" +
                          "<string>\t\t| Toast <string>, no add. args will be read.\n" +
                          "[-t] <title string>\t| Displayed on the first line of the toast.\n" +
                          "[-m] <message string>\t| Displayed on the remaining lines, wrapped.\n" +
                          "[-p] <image URI>\t| Display toast with an image\n" +
                          "[-w] \t\t\t| Wait for toast to expire or activate.\n" +
                          "?\t\t\t| Print these intructions. Same as no args.\n" +
                          "Exit Status\t:  Exit Code\n" +
                          "Failed\t\t: -1\nSuccess\t\t:  0\nHidden\t\t:  1\nDismissed\t:  2\nTimeout\t\t:  3\n\n" +
                          "---- Image Notes ----\n" +
                          "Images must be .png with:\n" +
                          "\tmaximum dimensions of 1024x1024\n" +
                          "\tsize <= 200kb\n" +
                          "These limitations are due to the Toast notification system.\n" +
                          "This should go without saying, but windows style paths are required.\n";
            Console.WriteLine(inst);
        }

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign an AppUserModelID using Windows APIs. You must download and include the 
        // Windows API Code Pack for Microsoft .NET Framework for this code to function
        private static bool TryCreateShortcut()
        {
            String shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\toast.lnk";
            if (!File.Exists(shortcutPath))
            {
                InstallShortcut(shortcutPath);
                return true;
            }
            return false;
        }

        private static void InstallShortcut(String shortcutPath)
        {
            // Find the path to the current executable
            String exePath = Process.GetCurrentProcess().MainModule.FileName;
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            using (PropVariant appId = new PropVariant(APP_ID))
            {
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            // Commit the shortcut to disk
            IPersistFile newShortcutSave = (IPersistFile)newShortcut;

            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }

        private static ToastNotification ShowToast(String message)
        {
            return ShowToast(null, message, null);
        }

        private static ToastNotification ShowToast(String title, String message)
        {
            return ShowToast(title, message, null);
        }

        // Create and show the toast.
        // See the "Toasts" sample for more detail on what can be done with toasts
        private static ToastNotification ShowToast(String title, String message, String imageURI)
        {
            if (message == null) return null;
            // Get a toast XML template
            XmlDocument toastXml;

            //// Specify the absolute path to an image
            //String imagePath = "file:///" + Path.GetFullPath("toastImageAndText.png");
            if (imageURI != null)
            {
                toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
                XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = "file:///" + imageURI;
                if (title != null)
                {
                    // Fill in the text elements
                    XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                    stringElements[0].AppendChild(toastXml.CreateTextNode(title));
                    stringElements[1].AppendChild(toastXml.CreateTextNode(message));
                }
                else
                {
                    // Fill in the text elements
                    XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                    stringElements[0].AppendChild(toastXml.CreateTextNode(message));
                }
            }
            else if (title != null)
            {
                toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                // Fill in the text elements
                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(title));
                stringElements[1].AppendChild(toastXml.CreateTextNode(message));
            }
            else
            {
                toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
                // Fill in the text elements
                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(message));
            }
            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
            return toast;
        }

        private static void ToastActivated(ToastNotification sender, object e)
        {
            Console.WriteLine("Activated");
            Environment.Exit(0);
        }

        private static void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            String outputText = "";
            int exitCode = -1;
            switch (e.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    outputText = "Hidden";
                    exitCode = 1;
                    break;
                case ToastDismissalReason.UserCanceled:
                    outputText = "Dismissed";
                    exitCode = 2;
                    break;
                case ToastDismissalReason.TimedOut:
                    outputText = "Timeout";
                    exitCode = 3;
                    break;
            }
            Console.WriteLine(outputText);
            Environment.Exit(exitCode);
        }

        private static void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
            Console.WriteLine("Error.");
            Environment.Exit(-1);
        }

    }
}
