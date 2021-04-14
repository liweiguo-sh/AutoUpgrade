using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Win32;

namespace AutoUpgrade {
    static class Program {
        #region -- DllImport --
        [DllImport("gdi32")]
        public static extern int AddFontResource(string lpFileName);

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd,  // -- handle to destination window --
            uint Msg,       // -- message  --
            int wParam,     // -- first message parameter --
            int lParam      // -- second message parameter --
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int WriteProfileString(string lpszSection, string lpszKeyName, string lpszString);
        #endregion
        #region -- 模块变量定义 --
        private static bool ignoreAutoUpgrade = false;
        private static string flag = "";
        private static string invokerEXE = "";          // -- 调用者exe(eg: abc.exe) --

        private static string iniLocal = "";            // -- 客户端local.ini文件 --
        private static string iniClient = "";           // -- 客户端version.ini文件 --
        private static string iniServer = "";           // -- 客户端version_server.ini文件 --

        private static string urlServerPath = "";       // -- 服务端client_download目录 --
        private static string urlServerIni = "";        // -- 服务端version_server.ini --

        private static IniFile iniFileClient = null;
        private static IniFile iniFileServer = null;
        #endregion
        #region -- main --
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FormWaitting frmWait = new FormWaitting();
            frmWait.Show();
            Application.DoEvents();
            // ---------------------------------------------
            try {
                if (args.Length == 0) {
                    GS.ShowError("调用参数不正确。");
                    return;
                }
                else if (args[0].Equals("debug")) {
                    ignoreAutoUpgrade = true;
                }
                else if (args[0].Equals("_copy")) {
                    invokerEXE = args[1];
                    File.Copy(GS.APP_EXE2, GS.APP_EXE, true);

                    Thread.Sleep(100);
                    Process.Start(GS.APP_EXE, "_self " + args[1]);
                    return;
                }
                else if (args[0].Equals("_self")) {
                    invokerEXE = args[1];
                    ignoreAutoUpgrade = true;
                }
                else {
                    // -- invoked by abc.exe --
                }
                if (!UpgradeResources()) {
                    Console.WriteLine("升级失败。");
                }
                // --------------------------------------------
                if (flag.Equals("_copy")) {  // 本次更新了 AutoUpgrade.exe 自身 --
                    Process.Start(GS.APP_EXE1, "_copy " + args[0]);
                }
                else if (args[0].Equals("_self")) {
                    try {
                        if (File.Exists(GS.APP_EXE1)) {
                            File.Delete(GS.APP_EXE1);
                        }
                        if (File.Exists(GS.APP_EXE2)) {
                            File.Delete(GS.APP_EXE2);
                        }
                    } catch (Exception ex) {
                        GS.ShowError(ex);
                    }

                    Process.Start(args[1], "start");
                }
                else if (args[0].Equals("debug")) {
                    // -- VS调试环境，正常退出 --
                    GS.ShowMessage("VS调试环境 AutoUpgrade 升级成功完成。");
                }
                else {
                    Process.Start(args[0], "start");
                }
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }
        #endregion

        #region -- 读取配置文件、下载升级文件 --
        private static bool UpgradeResources() {
            #region -- 过程变量定义 --
            string versionClient;
            string fileName, fileSave = "";

            IniFile.IniResItem iriServer;
            #endregion
            try {
                #region -- 1、读取本地local.ini，获取服务端资源路径 --
                iniLocal = "upgrade/local.ini";

                if (!File.Exists(iniLocal)) {
                    GS.ShowError("配置文件 " + iniLocal + " 不存在，请检查。");
                    return false;
                }
                iniFileClient = new IniFile(iniLocal, 1);

                urlServerPath = iniFileClient.getValue("url_server_path");
                if (urlServerPath.Equals("")) {
                    // -- 不执行自动升级 --
                    // -- GS.ShowError("参数 url_server_path 不存在，请检查。"); 
                    return false;
                }
                if (!urlServerPath.EndsWith("/")) {
                    urlServerPath += "/";
                }
                urlServerIni = urlServerPath + "server.ini";
                #endregion
                #region -- 2、下载并打开server.ini文件 --
                iniServer = "upgrade/server.ini";
                if (!DownloadFile(urlServerIni, iniServer)) {
                    GS.ShowError("下载服务端版本文件\n" + urlServerIni + "\n失败。");
                    return false;
                }
                iniFileServer = new IniFile(iniServer, 1);
                #endregion
                #region -- 3、打开client.ini文件 --
                iniClient = "upgrade/client.ini";
                if (!File.Exists(iniClient)) {
                    FileStream fsTemp = new FileStream(iniClient, FileMode.Create);
                    fsTemp.Close();
                    fsTemp = null;
                }
                iniFileClient = new IniFile(iniClient, 1);
                #endregion
                #region -- 4、比对资源项版本 --
                for (int i = 0; i < iniFileServer.Count; i++) {
                    iriServer = iniFileServer.getIniResItem(i);
                    versionClient = iniFileClient.getVersion(iriServer.key, "");
                    if (versionClient.Equals(iriServer.version) && !iriServer.version.Equals("9999")) continue;

                    // -- 资源项版本不同 --
                    if (iriServer.type.Equals("file", StringComparison.CurrentCultureIgnoreCase)) {
                        fileName = iriServer.key;
                        if ((fileName.Equals(GS.APP_EXE, StringComparison.CurrentCultureIgnoreCase))
                            && ignoreAutoUpgrade) {
                            continue;
                        }

                        if (fileName.Equals(invokerEXE)) {
                            fileSave = fileName;
                        }
                        else if (fileName.Equals(GS.APP_EXE, StringComparison.CurrentCultureIgnoreCase)) {
                            fileSave = GS.APP_EXE1;
                        }
                        else {
                            fileSave = iriServer.path + "\\" + fileName;
                        }

                        // -- 下载资源文件 --
                        if (!DownloadFile(urlServerPath + fileName, fileSave)) {
                            GS.ShowError("新版本文件 " + fileName + " 下载失败，请确保该文件程序不在运行状态。");
                            return false;
                        }

                        // -- 特殊资源文件 --
                        if (fileName.Equals(GS.APP_EXE)) {
                            File.Copy(GS.APP_EXE1, GS.APP_EXE2, true);
                            flag = "_copy";
                            return true;
                        }
                        else if (fileSave.Equals(invokerEXE)) {
                            continue;
                        }
                    }

                    // -- 处理升级资源 --
                    fileSave = Application.StartupPath + "\\" + fileSave;
                    if (!DoResUpgrage(iriServer, fileSave)) {
                        return false;
                    }
                }
                #endregion
                #region -- 5、更新客户端版本文件 --
                File.Copy("upgrade/server.ini", "upgrade/client.ini", true);
                File.Delete("upgrade/server.ini");
                #endregion
            } catch (Exception e) {
                GS.ShowError(e.ToString());
                return false;
            } finally {
            }
            return true;
        }
        private static bool DownloadFile(string urlFile, string saveFile) {
            try {
                if (!OS.CheckFilePath(saveFile, true)) {
                    return false;
                }

                HttpWebRequest Myrq = (HttpWebRequest)System.Net.HttpWebRequest.Create(urlFile);
                HttpWebResponse myrp = (HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;

                Stream st = myrp.GetResponseStream();
                Stream so = new FileStream(saveFile, FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0) {
                    totalDownloadedByte = osize + totalDownloadedByte;
                    Application.DoEvents();
                    so.Write(by, 0, osize);

                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
            } catch (Exception e) {
                GS.ShowError(e.Message);
                return false;
            }
            return true;
        }
        #endregion
        #region -- 升级客户端/添加信任站点/执行bat文件 etc. --
        private static bool DoResUpgrage(IniFile.IniResItem iri, string resFile) {
            bool blNoError = true;
            // --------------------------------------------
            if (iri.method.Equals("")) {
                return true;
            }
            // --------------------------------------------
            if (iri.method.Equals("setup", StringComparison.CurrentCultureIgnoreCase)) {
                if (!UpgradeNewVersion(resFile)) {
                    blNoError = false;
                }
            }
            else if (iri.method.Equals("trust_ip", StringComparison.CurrentCultureIgnoreCase)) {
                if (!AddTrustSite_IP(iri.value)) {
                    blNoError = false;
                }
            }
            else if (iri.method.Equals("trust_domain", StringComparison.CurrentCultureIgnoreCase)) {
                if (!AddTrustSite_Domain(iri.value)) {
                    blNoError = false;
                }
            }
            else if (iri.method.Equals("bat", StringComparison.CurrentCultureIgnoreCase)) {
                if (!ExecBAT(resFile)) {
                    blNoError = false;
                }
            }
            else if (iri.method.Equals("copy", StringComparison.CurrentCultureIgnoreCase)) {
                if (!CopyResFile(resFile, iri.value, iri.key)) {
                    blNoError = false;
                }
            }
            else if (iri.method.Equals("font", StringComparison.CurrentCultureIgnoreCase)) {
                if (!InstallFont(resFile, iri.key, iri.value)) {
                    blNoError = false;
                }
            }
            else {
                GS.ShowError("未知的升级类型：" + iri.method + "，请检查。");
                return false;
            }
            return blNoError;
        }

        private static bool UpgradeNewVersion(string setupFile) {
            try {
                Process p = System.Diagnostics.Process.Start(setupFile);

                while (!p.HasExited) {
                    System.Threading.Thread.Sleep(500);
                }
            } catch (Exception e) {
                GS.ShowError("升级客户端版本失败，请重试。\r\n" + e.Message);
                return false;
            } finally {
            }
            return true;
        }

        private static bool AddTrustSite_IP(string strIP) {
            #region -- 过程变量定义 --
            RegistryKey regRanges = null, regRange = null;
            #endregion
            try {
                regRanges = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\ZoneMap\\Ranges", true);
                if (regRanges == null) {
                    regRanges = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\ZoneMap\\Ranges");
                }
                string[] arrSubKeyNames = regRanges.GetSubKeyNames();
                for (int j = 0; j < arrSubKeyNames.Length; j++) {
                    regRange = regRanges.OpenSubKey(arrSubKeyNames[j]);
                    string[] arrValueNames = regRange.GetValueNames();
                    for (int k = 0; k < arrValueNames.Length; k++) {
                        if (arrValueNames[k].Equals(":Range")) {
                            if (regRange.GetValue(":Range").Equals(strIP)) {
                                return true;
                            }
                        }
                    }
                }
                // ----------------------------------------
                for (int j = 1; j <= 100; j++) {
                    regRange = regRanges.OpenSubKey("Range" + j);
                    if (regRange == null) {
                        regRange = regRanges.CreateSubKey("Range" + j);
                        regRange.SetValue(":Range", strIP, RegistryValueKind.String);
                        regRange.SetValue("http", 2, RegistryValueKind.DWord);
                        return true;
                    }
                }
            } catch (Exception e) {
                GS.ShowError("添加信任站点IP [" + strIP + "] 失败，请重试。\r\n" + e.Message);
                return false;
            } finally {
            }
            return true;
        }
        private static bool AddTrustSite_Domain(string strDomain) {
            #region -- 过程变量定义 --
            RegistryKey regDomains = null, regDomain = null;
            #endregion
            try {
                regDomains = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\ZoneMap\\Domains", true);
                if (regDomains == null) {
                    regDomains = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\ZoneMap\\Domains");
                }
                string[] arrSubKeyNames = regDomains.GetSubKeyNames();
                for (int j = 0; j < arrSubKeyNames.Length; j++) {
                    regDomain = regDomains.OpenSubKey(arrSubKeyNames[j]);
                    string[] arrValueNames = regDomain.GetValueNames();
                    for (int k = 0; k < arrValueNames.Length; k++) {
                        if (arrValueNames[k].Equals(":Range")) {
                            if (regDomain.GetValue(":Range").Equals(strDomain)) {
                                return true;
                            }
                        }
                    }
                }
                // ----------------------------
                string[] arrDomain = strDomain.Split('.');
                if (arrDomain.Length == 1) {
                    if (strDomain.Equals("localhost", StringComparison.CurrentCultureIgnoreCase)) {
                        regDomain = regDomains.CreateSubKey(strDomain);
                        regDomain.SetValue("http", 2, RegistryValueKind.DWord);
                    }
                    else {
                        GS.ShowError("域名" + strDomain + "格式错误，无法加入到信任站点。");
                        return false;
                    }
                }
                else if (arrDomain.Length == 2) {
                    regDomain = regDomains.CreateSubKey(strDomain);
                    regDomain.SetValue("*", 2, RegistryValueKind.DWord);
                }
                else {
                    strDomain = strDomain.Substring(arrDomain[0].Length + 1);
                    regDomain = regDomains.CreateSubKey(strDomain);
                    regDomain.SetValue("*", 2, RegistryValueKind.DWord);
                }
            } catch (Exception e) {
                GS.ShowError("添加信任站点域名 [" + strDomain + "] 失败，请重试。\r\n" + e.Message);
                return false;
            } finally {
            }
            return true;
        }

        private static bool ExecBAT(string batFile) {
            try {
                if (File.Exists(batFile)) {
                    Process proc = new Process();
                    // proc.StartInfo.WorkingDirectory = "upgrade";
                    proc.StartInfo.FileName = batFile;
                    proc.Start();
                    proc.WaitForExit();
                }
            } catch (Exception e) {
                GS.ShowError("执行 " + batFile + " 遇到意外错误, 请检查.\n" + e.Message);
                return false;
            }
            return true;
        }
        private static bool CopyResFile(string resFile, string destPath, string fileName) {
            string destFile = "";
            // --------------------------------------------
            try {
                destFile = destPath + "\\" + fileName;

                if (!Directory.Exists(destPath)) {
                    Directory.CreateDirectory(destPath);
                }
                File.Copy(resFile, destFile, true);
            } catch (Exception e) {
                GS.ShowError("拷贝资源 " + resFile + " 遇到意外错误, 请检查.\n" + e.Message);
                return false;
            }
            return true;
        }
        private static bool InstallFont(string fontFile, string fontFileName, string fontName) {
            int Ret;
            //const int WM_FONTCHANGE = 0x001D;
            //const int HWND_BROADCAST = 0xffff;
            // --------------------------------------------
            try {
                string destFile = OS.getFontsPath() + "\\" + fontFileName;
                if (!File.Exists(destFile)) {
                    File.Copy(fontFile, destFile, true);
                }
                else {
                    // -- GS.ShowMessage(destFile + " 已存在。"); --
                }

                Ret = AddFontResource(destFile);
                // Res = SendMessage(HWND_BROADCAST, WM_FONTCHANGE, 0, 0);
                Ret = WriteProfileString("fonts", fontName + "(TrueType)", fontFile);
            } catch (Exception e) {
                GS.ShowError("添加字体文件 " + fontFile + " 遇到意外错误, 请检查.\n" + e.Message);
                return false;
            }
            return true;
        }
        #endregion
    }
}