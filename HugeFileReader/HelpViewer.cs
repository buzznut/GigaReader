//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
public partial class HelpViewer : Form
{
    private int currentPage;
    private int totalPages;
    private Stream[] pages;
    private readonly PdfDocument doc = new PdfDocument();
    private BackgroundWorker worker = new BackgroundWorker();
    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public HelpViewer()
    {
        InitializeComponent();
        disposables.Add(worker);
        disposables.Add(doc);
    }

    private void buttonEnd_Click(object sender, EventArgs e)
    {
        currentPage = totalPages - 1;
        EnableButtons();
        DrawPage();
    }

    private void buttonRight_Click(object sender, EventArgs e)
    {
        currentPage = Math.Min(currentPage + 1, totalPages - 1);
        EnableButtons();
        DrawPage();
    }

    private void buttonLeft_Click(object sender, EventArgs e)
    {
        currentPage = Math.Max(currentPage - 1, 0);
        EnableButtons();
        DrawPage();
    }

    private void buttonBegin_Click(object sender, EventArgs e)
    {
        currentPage = 0;
        EnableButtons();
        DrawPage();
    }

    internal bool Initialize(string title, Stream[] pageStreams)
    {
        if (pageStreams == null)
        {
            return false;
        }

        // all the pageStreams are already loaded
        SuspendLayout();

        Text = title;
        pages = pageStreams;
        totalPages = pageStreams.Length;
        currentPage = 0;
        DrawPage();

        ResumeLayout(true);

        return true;
    }

    internal bool Initialize(string appDir, string title, string pdfName)
    {
        // must load pageStreams from PDF in the background
        SuspendLayout();

        InitializeBackgroundWorker();

        if (appDir != null && Directory.Exists(appDir))
        {
            string pdfFile = Path.Combine(appDir, "Resources", pdfName);
            if (!File.Exists(pdfFile)) return false;

            Stream waitStream = LoadBitmapFromResources(appDir);
            doc.LoadFromFile(pdfFile);
            Text = title;

            totalPages = doc.Pages.Count;
            pages = new Stream[totalPages];
            currentPage = 0;

            if (waitStream != null)
            {
                pictureBoxView.Image = Image.FromStream(waitStream);
                disposables.Add(waitStream);
            }

            DrawPage();
        }

        ResumeLayout(true);

        return true;
    }

    private void InitializeBackgroundWorker()
    {
        worker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true
        };

        worker.DoWork += ResolveImage;
        worker.RunWorkerCompleted += ResolveImageCompleted;
    }

    private void DrawPage()
    {
        if (pages == null) return;

        if (pages[currentPage] != null)
        {
            pictureBoxView.Image?.Dispose();
            pictureBoxView.Image = Bitmap.FromStream(pages[currentPage]);
            disposables.Add(pictureBoxView.Image);

            pictureBoxView.Invalidate();
            splitContainerView.Panel1.VerticalScroll.Value = 0;
            return;
        }

        if (worker.IsBusy) return;

        ImageResolveData data = new ImageResolveData
        {
            Document = doc,
            CurrentPage = currentPage
        };

        worker.RunWorkerAsync(data);
    }

    private void EnableButtons()
    {
        buttonEnd.Enabled = totalPages > 1;
        buttonRight.Enabled = totalPages > 1;
        buttonBegin.Enabled = currentPage > 0 && totalPages > 0;
        buttonLeft.Enabled = currentPage > 0 && totalPages > 0;
        textBoxPageInfo.Text = $"  {currentPage + 1}/{totalPages}  ";
    }

    private static Stream LoadBitmapFromResources(string appDir)
    {
        string bitmapFile = Path.Combine(appDir, "ResourceFiles", "LoadingBitmap.bmp");
        if (!File.Exists(bitmapFile)) return null;

        return new FileStream(bitmapFile, FileMode.Open, FileAccess.Read);
    }

    private void ResolveImage(object sender, DoWorkEventArgs e)
    {
        BackgroundWorker bw = sender as BackgroundWorker;
        if (bw == null) return;

        ImageResolveData data = e.Argument as ImageResolveData;
        if (data == null) return;

        FileStream tmpStream = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
        doc.SaveToImageStream(data.CurrentPage, tmpStream, "bitmap");
        data.Image = tmpStream;

        e.Result = data;
    }

    private void ResolveImageCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        ImageResolveData data = e.Result as ImageResolveData;
        if (data == null) return;

        if (e.Cancelled)
        {
            MessageBox.Show("Operation was canceled");
        }
        else if (e.Error != null)
        {
            MessageBox.Show(e.Error.Message);
        }
        else
        {
            pages[data.CurrentPage] = data.Image;

            pictureBoxView.Image?.Dispose();
            pictureBoxView.Image = Bitmap.FromStream(pages[currentPage]);

            pictureBoxView.Invalidate();
            splitContainerView.Panel1.VerticalScroll.Value = 0;
        }
    }

    private void HelpViewer_Load(object sender, EventArgs e)
    {
        DrawPage();
        EnableButtons();
    }
}

public class ImageResolveData
{
    public int CurrentPage { get; internal set; }
    public Stream Image { get; internal set; }
    public PdfDocument Document { get; internal set; }
}
