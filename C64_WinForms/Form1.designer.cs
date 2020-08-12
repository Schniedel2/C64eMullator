namespace C64_WinForms
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.dateiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restoreStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetHardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPRGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.maxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.peripherieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.swapJoysticksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sIDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggle1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggle2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggle3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.labelLED1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dateiToolStripMenuItem,
            this.speedToolStripMenuItem,
            this.peripherieToolStripMenuItem,
            this.sIDToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(411, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // dateiToolStripMenuItem
            // 
            this.dateiToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveStateToolStripMenuItem,
            this.restoreStateToolStripMenuItem,
            this.resetHardToolStripMenuItem,
            this.loadPRGToolStripMenuItem});
            this.dateiToolStripMenuItem.Name = "dateiToolStripMenuItem";
            this.dateiToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.dateiToolStripMenuItem.Text = "Datei";
            // 
            // saveStateToolStripMenuItem
            // 
            this.saveStateToolStripMenuItem.Name = "saveStateToolStripMenuItem";
            this.saveStateToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.saveStateToolStripMenuItem.Text = "Save State";
            this.saveStateToolStripMenuItem.Click += new System.EventHandler(this.saveStateToolStripMenuItem_Click);
            // 
            // restoreStateToolStripMenuItem
            // 
            this.restoreStateToolStripMenuItem.Name = "restoreStateToolStripMenuItem";
            this.restoreStateToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.restoreStateToolStripMenuItem.Text = "Restore State";
            this.restoreStateToolStripMenuItem.Click += new System.EventHandler(this.restoreStateToolStripMenuItem_Click);
            // 
            // resetHardToolStripMenuItem
            // 
            this.resetHardToolStripMenuItem.Name = "resetHardToolStripMenuItem";
            this.resetHardToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.resetHardToolStripMenuItem.Text = "Reset (Hard)";
            this.resetHardToolStripMenuItem.Click += new System.EventHandler(this.resetHardToolStripMenuItem_Click);
            // 
            // loadPRGToolStripMenuItem
            // 
            this.loadPRGToolStripMenuItem.Name = "loadPRGToolStripMenuItem";
            this.loadPRGToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.loadPRGToolStripMenuItem.Text = "Load PRG to RAM";
            this.loadPRGToolStripMenuItem.Click += new System.EventHandler(this.loadPRGToolStripMenuItem_Click);
            // 
            // speedToolStripMenuItem
            // 
            this.speedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.maxToolStripMenuItem,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6});
            this.speedToolStripMenuItem.Name = "speedToolStripMenuItem";
            this.speedToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.speedToolStripMenuItem.Text = "Speed";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem2.Text = "100%";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem3.Text = "200%";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem4.Text = "400%";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.toolStripMenuItem4_Click);
            // 
            // maxToolStripMenuItem
            // 
            this.maxToolStripMenuItem.Name = "maxToolStripMenuItem";
            this.maxToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.maxToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.W)));
            this.maxToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.maxToolStripMenuItem.Text = "max";
            this.maxToolStripMenuItem.Click += new System.EventHandler(this.maxToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem5.Text = "10%";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.toolStripMenuItem5_Click);
            // 
            // peripherieToolStripMenuItem
            // 
            this.peripherieToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.swapJoysticksToolStripMenuItem});
            this.peripherieToolStripMenuItem.Name = "peripherieToolStripMenuItem";
            this.peripherieToolStripMenuItem.Size = new System.Drawing.Size(72, 20);
            this.peripherieToolStripMenuItem.Text = "Peripherie";
            // 
            // swapJoysticksToolStripMenuItem
            // 
            this.swapJoysticksToolStripMenuItem.Name = "swapJoysticksToolStripMenuItem";
            this.swapJoysticksToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.swapJoysticksToolStripMenuItem.Text = "Swap Joysticks";
            this.swapJoysticksToolStripMenuItem.Click += new System.EventHandler(this.swapJoysticksToolStripMenuItem_Click);
            // 
            // sIDToolStripMenuItem
            // 
            this.sIDToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toggle1ToolStripMenuItem,
            this.toggle2ToolStripMenuItem,
            this.toggle3ToolStripMenuItem});
            this.sIDToolStripMenuItem.Name = "sIDToolStripMenuItem";
            this.sIDToolStripMenuItem.Size = new System.Drawing.Size(36, 20);
            this.sIDToolStripMenuItem.Text = "SID";
            // 
            // toggle1ToolStripMenuItem
            // 
            this.toggle1ToolStripMenuItem.Name = "toggle1ToolStripMenuItem";
            this.toggle1ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D1)));
            this.toggle1ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.toggle1ToolStripMenuItem.Text = "Toggle #1";
            this.toggle1ToolStripMenuItem.Click += new System.EventHandler(this.toggle1ToolStripMenuItem_Click);
            // 
            // toggle2ToolStripMenuItem
            // 
            this.toggle2ToolStripMenuItem.Name = "toggle2ToolStripMenuItem";
            this.toggle2ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D2)));
            this.toggle2ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.toggle2ToolStripMenuItem.Text = "Toggle #2";
            this.toggle2ToolStripMenuItem.Click += new System.EventHandler(this.toggle2ToolStripMenuItem_Click);
            // 
            // toggle3ToolStripMenuItem
            // 
            this.toggle3ToolStripMenuItem.Name = "toggle3ToolStripMenuItem";
            this.toggle3ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D3)));
            this.toggle3ToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.toggle3ToolStripMenuItem.Text = "Toggle #3";
            this.toggle3ToolStripMenuItem.Click += new System.EventHandler(this.toggle3ToolStripMenuItem_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 24);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(411, 344);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelLED1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 346);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(411, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // labelLED1
            // 
            this.labelLED1.Name = "labelLED1";
            this.labelLED1.Size = new System.Drawing.Size(33, 17);
            this.labelLED1.Text = "LED1";
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem6.Text = "1%";
            this.toolStripMenuItem6.Click += new System.EventHandler(this.toolStripMenuItem6_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 368);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Form1_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form1_PreviewKeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem dateiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetHardToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem speedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem maxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem peripherieToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem swapJoysticksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sIDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggle1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggle2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toggle3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel labelLED1;
        private System.Windows.Forms.ToolStripMenuItem loadPRGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
    }
}

