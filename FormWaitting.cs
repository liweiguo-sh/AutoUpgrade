using System;
using System.Windows.Forms;

namespace AutoUpgrade {
    public partial class FormWaitting : Form {
        public FormWaitting() {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            if(progressBar1.Value == 100) {
                progressBar1.Value = 0;
            }
            progressBar1.Value++;
        }
    }
}
