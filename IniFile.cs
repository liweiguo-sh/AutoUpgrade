using System;
using System.Collections.Generic;
using System.IO;
namespace AutoUpgrade {
    public class IniFile {
        #region -- 模块变量定义 --
        private int iniFormat = 0;          // -- 0：简单模式，key = value；1：资源模式，key = ?; value = ?; version = ? ...
        private string iniFileName = "";    // -- ini文件 --

        public class IniResItem {
            public string key = "";                     // -- 资源标识 --
            public string value = "";                   // -- 资源值(根据类型不同意义不同，eg：变量值、文件名等) --
            public string type = "";                    // -- 资源类型(variable、file) --
            public string method = "";                  // -- 资源升级方法(trust_website、domain、etc.) --            
            public string version = "";                 // -- 资源版本 --
            public string path = "upgrade";             // -- 存放目录(默认当前目录的upgrade子目录，当前目录用 . 表示) --

            public string ToString(int iniFormat) {
                string resItemString = "";

                if (iniFormat == 0) {
                    resItemString = key + " = " + value;
                }
                else {
                    if (key != "") {
                        resItemString += "  key = " + key + ";";
                    }
                    if (type != "") {
                        resItemString += "  type = " + type + ";";
                    }
                    if (method != "") {
                        resItemString += "  method = " + method + ";";
                    }
                    if (value != "") {
                        resItemString += "  value = " + value + ";";
                    }
                    if (version != "") {
                        resItemString += "  version = " + version + ";";
                    }
                }
                resItemString = resItemString.Trim();
                return resItemString;
            }
        }
        public List<IniResItem> lstIri = null;

        #endregion
        #region -- 构造函数 --
        public IniFile(string strIniFile) {
            iniFileName = strIniFile;
            iniFormat = 0;

            _IniFile();
        }
        public IniFile(string strIniFile, int nIniFormat) {
            iniFileName = strIniFile;
            iniFormat = nIniFormat;

            _IniFile();
        }
        private void _IniFile() {
            lstIri = new List<IniResItem>();
            readIniFile();
        }
        #endregion

        #region -- public method --
        public int Count {
            get {
                return lstIri.Count;
            }
        }

        public void addInIResItem(IniResItem resItem) {
            this.lstIri.Add(resItem);
        }
        public void updateInIResItem(IniResItem resItem) {
            IniResItem resItemExist = this.getIniResItem(resItem.key);

            if (resItemExist == null) {
                this.addInIResItem(resItem);
            }
            else {
                resItemExist.type = resItem.type;
                resItemExist.method = resItem.method;
                resItemExist.value = resItem.value;
                resItemExist.version = resItem.version;
            }
        }

        public IniResItem getIniResItem(int idxResItem) {
            return lstIri[idxResItem];
        }
        public IniResItem getIniResItem(string resItemKey) {
            for (int i = 0; i < lstIri.Count; i++) {
                if (lstIri[i].key.Equals(resItemKey, StringComparison.CurrentCultureIgnoreCase)) {
                    return lstIri[i];
                }
            }
            return null;
        }

        private string _getValue(string strKey, string strDefault) {
            IniResItem iri = getIniResItem(strKey);
            if (iri != null) {
                return iri.value;
            }
            else {
                return strDefault;
            }

        }
        public string getValue(string strKey) {
            return this._getValue(strKey, "");
        }
        public string getValue(string strKey, string strDefault) {
            return this._getValue(strKey, strDefault);
        }

        private string _getVersion(string strKey, string strDefault) {
            IniResItem iri = getIniResItem(strKey);
            if (iri != null) {
                return iri.version;
            }
            else {
                return strDefault;
            }

        }
        public string getVersion(string strKey) {
            return this._getVersion(strKey, "");
        }
        public string getVersion(string strKey, string strDefault) {
            return this._getVersion(strKey, strDefault);
        }
        #endregion
        #region -- private: readIniFile
        private void readIniFile() {
            if (this.iniFormat == 0) {
                readIniFile0();
            }
            else if (this.iniFormat == 1) {
                readIniFile1();
            }
            else {
                GS.ShowError("不支持的ini格式类型。");
            }
        }
        private void readIniFile0() {
            #region -- 过程变量定义 --
            int nIdx = 0, lineNo = 0;

            string strLine = "";

            StreamReader sr = null;
            #endregion
            try {
                if (!File.Exists(iniFileName)) {
                    throw new Exception("配置文件 " + iniFileName + " 不存在，请检查。");
                }

                sr = File.OpenText(iniFileName);
                while (sr.Peek() >= 0) {
                    lineNo++;
                    strLine = sr.ReadLine().Trim();
                    if (strLine.Length < 4) {
                        continue;
                    }
                    else if (strLine.Substring(0, 4).Equals("rem ", StringComparison.CurrentCultureIgnoreCase)) {
                        continue;
                    }
                    else {
                        nIdx = strLine.IndexOf("=");
                        if (nIdx <= 0) {
                            throw new Exception("\n配置文件 " + iniFileName + " 第 " + lineNo + " 行格式不正确，请检查：\n\n" + strLine + "\n");
                        }

                        IniResItem iri = new IniResItem();
                        iri.key = strLine.Substring(0, nIdx).Trim();
                        iri.value = strLine.Substring(nIdx + 1).Trim();
                        lstIri.Add(iri);
                    }
                }
            } catch (Exception e) {
                throw e;
            } finally {
                if (sr != null) {
                    sr.Close();
                }
            }
        }
        private void readIniFile1() {
            #region -- 过程变量定义 --
            int nIdx = 0, lineNo = 0;

            string strLine = "";
            string strKeyValue = "", strKey = "", strValue = "";

            string[] arrResItem = null;

            StreamReader sr = null;
            #endregion
            try {
                if (!File.Exists(iniFileName)) {
                    throw new Exception("配置文件 " + iniFileName + " 不存在，请检查。");
                }

                sr = File.OpenText(iniFileName);
                while (sr.Peek() >= 0) {
                    lineNo++;
                    strLine = sr.ReadLine().Trim();
                    if (strLine.Length < 4) {
                        continue;
                    }
                    else if (strLine.Substring(0, 4).Equals("rem ", StringComparison.CurrentCultureIgnoreCase)) {
                        continue;
                    }
                    else {
                        IniResItem iri = new IniResItem();
                        arrResItem = strLine.Split(';');
                        for (int i = 0; i < arrResItem.Length; i++) {
                            strKeyValue = arrResItem[i];
                            if (strKeyValue.Trim() == "") continue;
                            nIdx = strKeyValue.IndexOf("=");
                            if (nIdx <= 0) {
                                throw new Exception("\n配置文件 " + iniFileName + " 第 " + lineNo + " 行格式不正确，请检查：\n\n" + strLine + "\n");
                            }
                            strKey = strKeyValue.Substring(0, nIdx).Trim();
                            strValue = strKeyValue.Substring(nIdx + 1).Trim();

                            if (strKey.Equals("key", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.key = strValue;
                            }
                            else if (strKey.Equals("type", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.type = strValue;
                            }
                            else if (strKey.Equals("method", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.method = strValue;
                            }
                            else if (strKey.Equals("value", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.value = strValue;
                            }
                            else if (strKey.Equals("version", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.version = strValue;
                            }
                            else if (strKey.Equals("path", StringComparison.CurrentCultureIgnoreCase)) {
                                iri.path = strValue;
                            }
                            else {
                                GS.ShowError("未知的 ini 配置项名称：" + strKey + "，已忽略。");
                            }
                        }
                        lstIri.Add(iri);
                    }
                }
            } catch (Exception e) {
                throw e;
            } finally {
                if (sr != null) {
                    sr.Close();
                }
            }
        }

        public void Save() {
            #region -- 过程变量定义 --
            FileStream fs = null;
            StreamWriter sw = null;
            #endregion
            try {
                fs = new FileStream(iniFileName, FileMode.Truncate);
                sw = new StreamWriter(fs);

                for (int i = 0; i < this.lstIri.Count; i++) {
                    sw.WriteLine(this.lstIri[i].ToString(this.iniFormat));
                }

                sw.Flush();
                sw.Close();
                fs.Close();
            } catch (Exception e) {
                throw e;
            } finally {
                if (sw != null) {
                    sw.Close();
                }
                if (fs != null) {
                    fs.Close();
                }
            }
        }
        #endregion
    }
}