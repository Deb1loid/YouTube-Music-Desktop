using System.Drawing;
using System.Windows.Forms;

namespace YouTubeMusic;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1280, 800);
        this.Text = "Form1";
    }
}