namespace SpaceInvadersAI
{
    partial class FormSpaceInvaders
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSpaceInvaders));
            flowLayoutPanelGames = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // flowLayoutPanelGames
            // 
            flowLayoutPanelGames.AutoScroll = true;
            flowLayoutPanelGames.Dock = DockStyle.Fill;
            flowLayoutPanelGames.Location = new Point(0, 0);
            flowLayoutPanelGames.Name = "flowLayoutPanelGames";
            flowLayoutPanelGames.Size = new Size(1333, 674);
            flowLayoutPanelGames.TabIndex = 0;
            // 
            // FormSpaceInvaders
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoValidate = AutoValidate.Disable;
            BackColor = Color.Black;
            CausesValidation = false;
            ClientSize = new Size(1333, 674);
            Controls.Add(flowLayoutPanelGames);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MdiChildrenMinimizedAnchorBottom = false;
            Name = "FormSpaceInvaders";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AI Space Invaders - Keyboard Commands: [Q] toggle quick learning - [V] draws visualisation at next mutation (in c:\\temp) - [B] Save brain as template";
            WindowState = FormWindowState.Maximized;
            FormClosing += FormSpaceInvaders_FormClosing;
            Load += Form1_Load;
            KeyDown += FormSpaceInvaders_KeyDown;
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelGames;
    }
}