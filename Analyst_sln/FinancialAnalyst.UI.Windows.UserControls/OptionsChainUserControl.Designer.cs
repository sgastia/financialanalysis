﻿namespace FinancialAnalyst.UI.Windows.UserControls
{
    partial class OptionsChainUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.labelPriceS0 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Price [S0]:";
            // 
            // labelPriceS0
            // 
            this.labelPriceS0.AutoSize = true;
            this.labelPriceS0.Location = new System.Drawing.Point(66, 4);
            this.labelPriceS0.Name = "labelPriceS0";
            this.labelPriceS0.Size = new System.Drawing.Size(35, 13);
            this.labelPriceS0.TabIndex = 1;
            this.labelPriceS0.Text = "label2";
            // 
            // OptionsChainUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelPriceS0);
            this.Controls.Add(this.label1);
            this.Name = "OptionsChainUserControl";
            this.Size = new System.Drawing.Size(523, 336);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelPriceS0;
    }
}
