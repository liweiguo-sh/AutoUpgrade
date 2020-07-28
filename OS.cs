using System;
using Microsoft.Win32;
using System.IO;

namespace AutoUpgrade {
    public static class OS {
        #region -- 添加字体 --
        public static string getFontsPath() {
            RegistryKey folders = OpenRegistryPath(Registry.CurrentUser, @"\software\microsoft\windows\currentversion\explorer\shell folders");
            return folders.GetValue("Fonts").ToString();
        }

        private static RegistryKey OpenRegistryPath(RegistryKey root, string s) {
            s = s.Remove(0, 1) + @"\";
            while (s.IndexOf(@"\") != -1) {
                root = root.OpenSubKey(s.Substring(0, s.IndexOf(@"\")));
                s = s.Remove(0, s.IndexOf(@"\") + 1);
            }
            return root;
        }
        #endregion

        #region -- 文件目录 --
        public static bool CheckFilePath(string fileName, bool autoCreate) {
            try {
                FileInfo info = new FileInfo(fileName);
                DirectoryInfo dirInfo = new DirectoryInfo(info.Directory.FullName);
                if (dirInfo.Exists) {
                    return true;
                }
                else {
                    if (autoCreate) {
                        dirInfo.Create();
                    }
                }
            } catch (Exception e) {
                throw e;
            } finally {
            }
            return true;
        }
        #endregion
    }
}
