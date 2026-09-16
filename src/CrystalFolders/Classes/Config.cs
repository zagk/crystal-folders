using HandyControl.Themes;
using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace CrystalFolders
{
    /// <summary>
    /// Config.ini
    /// </summary>
    internal class Config
    {
        internal static bool isIntalled;
        internal static string iniPath, appData;
        internal static string[] iniLines;
        internal static bool restart = false;

        private const string ContextMenuKey = @"Software\Classes\Directory\shell\CrystalFolder";
        private const string ContextMenuSetting = "ContextMenu = true";

        internal static bool GetContextMenu()
        {
            foreach (string line in iniLines)
            {
                if (line.Trim().Equals(ContextMenuSetting, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void SetContextMenu(bool enabled)
        {
            bool found = false;
            string value = enabled ? "ContextMenu = true" : "ContextMenu = false";

            for (int i = 0; i < iniLines.Length; i++)
            {
                if (iniLines[i].TrimStart().StartsWith("ContextMenu =", StringComparison.OrdinalIgnoreCase))
                {
                    iniLines[i] = value;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Array.Resize(ref iniLines, iniLines.Length + 1);
                iniLines[iniLines.Length - 1] = value;
            }

            File.WriteAllLines(iniPath, iniLines);

            if (enabled)
            {
                RegisterContextMenu();
            }
            else
            {
                UnregisterContextMenu();
            }
        }

        internal static void RegisterContextMenu()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ContextMenuKey))
                {
                    key.SetValue(null, "Crystal Folder");
                    key.SetValue("Icon", exePath);

                    using (RegistryKey commandKey = key.CreateSubKey("command"))
                    {
                        commandKey.SetValue(null, "\"" + exePath + "\" --context-folder \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Context menu registration error: " + ex.Message);
            }
        }

        internal static void UnregisterContextMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(ContextMenuKey, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Context menu removal error: " + ex.Message);
            }
        }

        internal static void CheckPath()
        {
            // Do not use the process current working directory here.
            // Windows Shell verbs can start the application with the selected
            // folder (or another directory) as the working directory. That made
            // the context-menu launch fail because Config.ini was searched for
            // in the wrong place.
            appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Crystal Folders");

            string portableConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.ini");
            string installedConfig = Path.Combine(appData, "Config.ini");

            if (File.Exists(portableConfig))
            {
                isIntalled = false;
                iniPath = portableConfig;
            }
            else
            {
                isIntalled = true;
                iniPath = installedConfig;
            }

            iniLines = File.ReadAllLines(iniPath);

            Console.WriteLine("Crystal Folders is installed? " + isIntalled + " - ¿Crystal Folders está instalado? " + isIntalled);
        }

        #region Language
        internal static string currentLan, selecLan;

        internal static void Language()
        {
            // Modificar el idioma de la aplicación en base a Config.ini y establecer
            // el idioma actual en un string para no volveer a leer el archivo
            switch (iniLines[1])
            {
                case "Language = en":
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("");
                    currentLan = "en";
                    selecLan = "en";
                    break;

                case "Language = es":
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("es-419");
                    currentLan = "es";
                    selecLan = "es";
                    break;
            }

            Console.WriteLine("Current language: " + currentLan + " - Idioma actual: " + currentLan);
        }

        internal static void CheckLanguage()
        {
            // Si el idioma seleccionado no es el mismo que el actual,
            // cambiarlo y modificar el archivo .ini
            if (selecLan == currentLan == false)
            {
                switch (selecLan)
                {
                    case "en":
                        iniLines[1] = iniLines[1].Replace("es", "en");
                        File.WriteAllLines(iniPath, iniLines);
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("");
                        currentLan = "en";
                        break;

                    case "es":
                        iniLines[1] = iniLines[1].Replace("en", "es");
                        File.WriteAllLines(iniPath, iniLines);
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("es-419");
                        currentLan = "es";
                        break;
                }

                Console.WriteLine("Selected language: " + selecLan + " - Idioma seleccionado: " + selecLan);
                restart = true;
            }
        }
        #endregion

        #region Theme
        internal static string HEX;
        internal static SolidColorBrush colorBrush;
        internal static string winvers;

        internal static void GetTheme()
        {
            // Obtiene el color HEX de Config.ini
            HEX = iniLines[5];

            // Crear brush de nuevo color y aplicarlo al tema
            colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HEX));
            ThemeManager.Current.AccentColor = colorBrush;
            Application.Current.Resources["Primary"] = colorBrush;
        }

        internal static void SetTheme()
        {
            // Establece un nuevo color del tema, remplazando dígitos
            iniLines[5] = iniLines[5].Replace(iniLines[5], HEX);
            File.WriteAllLines(iniPath, iniLines);

            // Crear brush de nuevo color y aplicarlo al tema
            ThemeManager.Current.AccentColor = colorBrush;
            Application.Current.Resources["Primary"] = colorBrush;
        }

        internal static void RoundCorners(Border bg1, Border bg2, Border border1, Border border2, Border deco1, Border deco2)
        {
            // Desactivar bordes redondeados en las versiones METRO de Win
            if (winvers.Contains("10") || winvers.Contains("8"))
            {
                bg1.CornerRadius = new CornerRadius(0, 0, 0, 0);
                border1.CornerRadius = new CornerRadius(0, 0, 0, 0);
                deco1.CornerRadius = new CornerRadius(0, 0, 0, 0);

                if (bg2 is null && border2 is null && deco2 is null)
                {
                    // Si no hay controles de borde para redondear, no hacer nada.
                }
                else
                {
                    bg2.CornerRadius = new CornerRadius(0, 0, 0, 0);
                    border2.CornerRadius = new CornerRadius(0, 0, 0, 0);
                    deco2.CornerRadius = new CornerRadius(0, 0, 0, 0);
                }
            }
        }
        #endregion

        #region Message
        internal static bool message;

        internal static void GetMessage()
        {
            // Mensaje de confirmación para más de 600 carpetas. Si se modifica
            // el archivo Config.ini, este no se mostrará nuevamente.
            if (iniLines[2].Contains("Message = true"))
            {
                message = true;
            }
            else
            {
                message = false;
            }
        }
        #endregion
    }
}
