using System;
using System.Windows.Forms;
namespace AutoUpgrade {
    public static class GS {
        #region -- 全局静态变量/常量 --
        public const bool debug = true;

        public const string APP_NAME = "AutoUpgrade";
        public const string APP_EXE = APP_NAME + ".exe";
        public const string APP_EXE1 = APP_NAME + "1.exe";
        public const string APP_EXE2 = APP_NAME + "2.exe";
        //public const string APP_DLL2 = APP_NAME + "2.dll";
        #endregion
        #region -- 系统消息提示 --
        public static string APP_PATH {
            get {
                return Application.StartupPath + @"\\";
            }
        }
        #endregion
        #region -- 系统消息提示 --
        public static void ShowMessage(string strMessage) {
            MessageBox.Show(strMessage, "系统消息提示...", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void ShowWarning(string strMessage) {
            MessageBox.Show(strMessage, "系统消息提示...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static void ShowError(string strMessage) {
            MessageBox.Show(strMessage, "系统消息提示...", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public static void ShowError(Exception e) {
            string errString = e.Message;
            if (debug) {
                errString += "\r\n\r\ne.ToString()\r\n" + e.ToString();
            }
            MessageBox.Show(errString, "系统消息提示...", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static DialogResult Confirm(string strMessage) {
            return MessageBox.Show(strMessage, "系统消息提示...", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        #endregion
    }
}