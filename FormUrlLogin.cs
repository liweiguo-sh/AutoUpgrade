using System;
using System.Windows.Forms;
using System.IO;
namespace AutoUpgrade {
    public partial class FormUrlLogin : Form {
        public FormUrlLogin() {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e) {
            int nIdx = 0;

            string strText = "", urlLogin = "";

            byte[] data = null;
            FileStream fs = null;
            try {
                urlLogin = this.txtUrlLogin.Text.Trim();
                if(!parseUrlLogin(urlLogin)) {
                    return;
                }
                nIdx = urlLogin.IndexOf("rnd=");
                if(nIdx > 0) {
                    urlLogin = urlLogin.Substring(0, nIdx - 1);
                }
                strText = "key = url_login; value = " + urlLogin;
                data = System.Text.Encoding.Default.GetBytes(strText);

                // -- 创建local.ini文件并写入初始内容 --
                if(!Directory.Exists("upgrade")) {
                    Directory.CreateDirectory("upgrade");
                }
                fs = new FileStream("upgrade/local.ini", FileMode.Create);
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
                fs = null;
            }
            catch(Exception e1) {
                GS.ShowError(e1.ToString());
            } finally {
                if(fs != null) {
                    fs.Close();
                }
            }
            this.Close();
        }

        private static bool parseUrlLogin(string urlLogin) {
            #region -- 过程变量定义 --
            int nIdx = 0;

            string strDomainIp = "";
            #endregion
            #region -- 解析IP地址或域名部分 --
            if(urlLogin.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase)) {
                strDomainIp = urlLogin.Substring(7);
            }
            else if(urlLogin.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase)) {
                strDomainIp = urlLogin.Substring(8);
            }
            else {
                GS.ShowError("客户端配置文件 local.ini 中的 url_login 项格式不正确，请检查。\n" + urlLogin + "\n登录地址必需以 http:// 或 https:// 开头。");
                return false;
            }

            nIdx = strDomainIp.IndexOf("/");
            if(nIdx <= 0) {
                GS.ShowError("客户端配置文件 local.ini 中的 url_login 项格式不正确，请检查。\n" + urlLogin);
                return false;
            }
            strDomainIp = strDomainIp.Substring(0, nIdx);

            nIdx = strDomainIp.IndexOf(":");
            if(nIdx > 0) {
                strDomainIp = strDomainIp.Substring(0, nIdx);
            }            
            #endregion
            return true;
        }
    }
}
