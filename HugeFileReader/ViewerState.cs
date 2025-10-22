//  <@$&< copyright begin >&$@> 24FE144C2255E2F7CCB65514965434A807AE8998C9C4D01902A628F980431C98:20241017.A:2025:8:7:7:53
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace HugeFileViewer;

[SupportedOSPlatform("windows6.1")]
public class ViewerState : IDisposable
{
    private bool disposedValue;

    public InfoDelegate Info;

    public ViewerState(string fileName, Control parent, InfoDelegate info) : this(parent, info)
    {
        HugeFile = new HugeFile(fileName, ViewerInfo);
    }

    private void ViewerInfo(StateInfo stateInfo, object data)
    {
        // handle state information here
        StateInfo = stateInfo;
        StateData = data;

        // pass the state information to the Info delegate if it is set
        if (Info != null)
        {
            Info(stateInfo, data);
        }
    }

    public HugeFile HugeFile { get; private set; }
    public long ByteLength { get { return new FileInfo(HugeFile.FilePath).Length; } }
    public long CaretRow { get; set; }
    public int CaretCol { get; set; }
    public long CursorRow { get; set; }
    public int CursorCol { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public long Rows { get { return HugeFile.LineCount; } }
    public int Cols { get { return HugeFile.MaxColumnWidth; } }
    public long StartRow { get; set; }
    public int StartCol { get; set; }
    public float FontHeight { get; set; }
    public float FontWidth { get; set; }
    public bool UseCase { get; set; }
    public string FindText { get; set; }
    public bool HasCaret { get; set; }
    public long OldRow { get; set; } = -1;
    public int OldCol { get; set; } = -1;
    public int OldWidth { get; set; } = -1;
    public int OldHeight { get; set; } = -1;
    public long AnchorRow { get; set; } = -1;
    public int AnchorCol { get; set; } = -1;
    public long ReleaseRow { get; set; } = -1;
    public int ReleaseCol { get; set; } = -1;
    public SolidBrush TextBrush { get; } = new SolidBrush(SystemColors.WindowText);
    public SolidBrush SelectionBrush { get; } = new SolidBrush(SystemColors.HighlightText);
    public SolidBrush TextBackgroundBrush { get; } = new SolidBrush(SystemColors.Window);
    public SolidBrush SelectionBackgroundBrush { get; } = new SolidBrush(SystemColors.Highlight);
    public bool IsDown { get; set; } = true;
    public bool WasHandled { get; set; }
    public bool NoMouseUp { get; set; }
    public bool InShift { get; set; }
    public RectangleF OldRect { get; set; } = new Rectangle(0, 0, 0, 0);
    public string BaseTitle { get; set; }
    public bool TitleSet { get; set; }
    public StateInfo StateInfo { get; private set; } = StateInfo.None;
    public object StateData { get; private set; } = null;

    private ViewerState() { }

    public ViewerState(Control parent, InfoDelegate info)
    {
        Info = info;
        NewFont(parent);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed viewState (managed objects)
                HugeFile?.StopSearch();
                HugeFile?.Stop();
                HugeFile?.Dispose();

                TextBrush.Dispose();
                SelectionBrush.Dispose();
                SelectionBackgroundBrush.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ViewerState()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void NewFont(Control parent)
    {
        using (Graphics g = parent.CreateGraphics())
        {
            string ms = "Now is the time for all good men to come to the aid of their country.";
            ms += " The quick brown fox jumped over the lazy dog's back.";
            ms += "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789,./?><';:][}{\\|=-+_])(*&^%$#@!~`\"";
            SizeF xx = g.MeasureString(ms, parent.Font);

            FontHeight = xx.Height;
            FontWidth = (xx.Width / ms.Length);

            g.Dispose();
        }
    }

    internal void MouseDown(Control ucText, MouseEventArgs e)
    {
        InvalidateSelection(ucText);

        // set the cursor to where the mouse is.
        AnchorCol = (int)((e.X - ucText.Padding.Left) / FontWidth) + StartCol;
        AnchorRow = (int)((e.Y - ucText.Padding.Top) / FontHeight) + StartRow;

        ucText.Capture = true;
        InShift = true;

        ReleaseCol = AnchorCol;
        ReleaseRow = AnchorRow;
    }

    internal void InvalidateSelection(Control ucText)
    {
        if (AnchorRow == -1)
        {
            return;
        }

        int sc;
        int ec;

        long sr = Math.Min(AnchorRow, ReleaseRow);
        long er = Math.Max(AnchorRow, ReleaseRow);
        if (sr != er)
        {
            sc = 0;
            ec = ScreenWidth;
        }
        else
        {
            sc = Math.Min(AnchorCol, ReleaseCol);
            ec = Math.Max(AnchorCol, ReleaseCol);
        }

        float x = ((sc - StartCol) * FontWidth) + ucText.Padding.Left;
        float y = ((sr - StartRow) * FontHeight) + ucText.Padding.Top;

        float width = (ec - sc + 2) * FontWidth;
        float height = (er - sr + 1) * FontHeight;

        RectangleF rect = new RectangleF(x - 1, y - 1, width + 2, height + 2);
        ucText.Invalidate(Rectangle.Round(OldRect));
        ucText.Invalidate(Rectangle.Round(rect));

        OldRect = rect;
    }

    public void AdjustSelection(long oldRow, int oldCol, Control ucText)
    {
        if (InShift)
        {
            if (AnchorRow == -1)
            {
                AnchorRow = oldRow;
                AnchorCol = oldCol;
            }

            ReleaseRow = CursorRow;
            ReleaseCol = CursorCol;

            InvalidateSelection(ucText);
        }
        else
        {
            if (AnchorRow != -1)
            {
                AnchorRow = -1;
                ucText.Invalidate();
            }
        }
    }

    internal void MouseMove(Control ucText, MouseEventArgs e)
    {
        ReleaseCol = (int)((e.X - ucText.Padding.Left) / FontWidth) + StartCol;
        ReleaseRow = (int)((e.Y - ucText.Padding.Top) / FontHeight) + StartRow;

        if (ReleaseRow >= StartRow + ScreenHeight)
        {
            StartRow = ReleaseRow - ScreenHeight;
            if (StartRow > Rows)
            {
                StartRow = Rows;
            }
            ucText.Invalidate();
        }
        else if (ReleaseRow < StartRow)
        {
            StartRow = ReleaseRow;
            if (StartRow < 0)
            {
                StartRow = 0;
            }
            ucText.Invalidate();
        }
        else
        {
            InvalidateSelection(ucText);
        }
    }

    internal void MouseDoubleClick(Control ucText, MouseEventArgs e)
    {
        int c = (int)((e.X - ucText.Padding.Left) / FontWidth) + StartCol;
        long r = (int)((e.Y - ucText.Padding.Top) / FontHeight) + StartRow;

        if (r >= 0 && r < Rows)
        {
            string line = HugeFile.ReadLine(r);

            if (c >= 0 && c < line.Length)
            {
                if (line[c] != ' ')
                {
                    int o = c;

                    while (c > 0 && line[c] != ' ')
                    {
                        c--;
                    }

                    if (c < 0 || line[c] == ' ')
                    {
                        c++;
                    }

                    AnchorRow = r;
                    AnchorCol = c;

                    c = o;
                    while (c < line.Length && line[c] != ' ')
                    {
                        c++;
                    }

                    if (c > line.Length)
                    {
                        c--;
                    }

                    ReleaseRow = r;
                    ReleaseCol = c;

                    NoMouseUp = true;
                    InvalidateSelection(ucText);
                }
            }
        }

    }

    internal void ScrollUpDown(Control ucText, int linesToMove)
    {
        long srow = CursorRow;
        int scol = CursorCol;

        StartRow -= linesToMove;

        if (StartRow >= Rows)
        {
            StartRow = Rows - 1;
        }

        if (StartRow < 0)
        {
            StartRow = 0;
        }

        //if (CursorRow >= StartRow + ScreenHeight)
        //{
        //    CursorRow = StartRow + ScreenHeight - 1;
        //}
        //else if (CursorRow < StartRow)
        //{
        //    CursorRow = StartRow;
        //}

        AdjustSelection(srow, scol, ucText);
    }

    internal void ScrollLeftRight(Control ucText, int columnsToMove)
    {
        long srow = CursorRow;
        int scol = CursorCol;

        StartCol -= columnsToMove;

        if (StartCol >= Cols)
        {
            StartCol = Cols - 1;
        }

        if (StartCol < 0)
        {
            StartCol = 0;
        }

        if (CursorCol >= StartCol + ScreenWidth)
        {
            CursorCol = StartCol + ScreenWidth - 1;
        }
        else if (CursorCol < StartCol)
        {
            CursorCol = StartCol;
        }

        AdjustSelection(srow, scol, ucText);
    }

    internal bool ProcessKey(KeyEventArgs e, Control ucText)
    {
        bool handled = false;

        switch (e.KeyCode)
        {
            //case Keys.ShiftKey:
            //    {
            //        viewState.InShift = true;
            //        break;    
            //    }

            case Keys.C:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    ToClipboard();
                    handled = true;
                }
                break;
            }

            case Keys.Left:
            {
                ArrowLeft();
                handled = true;
                break;
            }

            case Keys.Right:
            {
                ArrowRight();
                handled = true;
                break;
            }

            case Keys.Up:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    ScrollUpDown(1);
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    ArrowUp();
                    handled = true;
                }
                break;
            }

            case Keys.Down:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    ScrollUpDown(-1);
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    ArrowDown();
                    handled = true;
                }
                break;
            }

            case Keys.PageUp:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    ScrollUpDown(ScreenHeight);
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    PageUp();
                    handled = true;
                }
                break;
            }

            case Keys.PageDown:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    ScrollUpDown(-ScreenHeight);
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    PageDown();
                    handled = true;
                }
                break;
            }

            case Keys.Home:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    GoToHome();
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    LineBegin();
                    handled = true;
                }
                break;
            }

            case Keys.End:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    GoToEnd();
                    handled = true;
                }
                else if ((e.Modifiers & Keys.None) == Keys.None)
                {
                    LineEnd();
                    handled = true;
                }
                break;
            }

            case Keys.F3:
            {
                IsDown = (e.Modifiers & Keys.Shift) != Keys.Shift;
                FindAgain();
                handled = true;
                break;
            }

            case Keys.F:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    Find();
                    handled = true;
                }
                break;
            }

            case Keys.G:
            {
                if ((e.Modifiers & Keys.Control) == Keys.Control)
                {
                    AskGoTo();
                    handled = true;
                }
                break;
            }
        }

        return handled;
    }

    private void GoToHome()
    {
        if (viewState == null) return;

        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow = 0;
        viewState.CursorCol = 0;

        AdjustSelection(srow, scol);
    }

    private void GoToEnd()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow = Math.Max(viewState.Rows - 1, 0);
        LineEnd();

        AdjustSelection(srow, scol);
    }

    private void GoToLine(long line)
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow = line - 1;
        viewState.CursorCol = 0;

        AdjustSelection(srow, scol);
    }

    private void PageDown()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow += viewState.ScreenHeight;
        if (viewState.CursorRow >= viewState.Rows)
        {
            viewState.CursorRow = viewState.Rows - 1;
        }

        AdjustSelection(srow, scol);
    }

    private void PageUp()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow -= viewState.ScreenHeight;
        if (viewState.CursorRow < 0)
        {
            viewState.CursorRow = 0;
        }

        AdjustSelection(srow, scol);
    }

    private void LineBegin()
    {
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorCol = 0;

        AdjustSelection(srow, scol);
    }

    private void LineEnd()
    {
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        if (viewState != null)
        {
            string line = viewState.HugeFile.ReadLine(viewState.CursorRow);
            line = line.Replace("\t", " ".PadRight(tabSize));
            viewState.CursorCol = line.Length;

            AdjustSelection(srow, scol);
        }
    }

    private void ArrowLeft()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorCol--;
        if (viewState.CursorCol < 0)
        {
            viewState.CursorCol = 0;
        }

        AdjustSelection(srow, scol);
    }

    private void ArrowRight()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorCol++;
        if (viewState.CursorCol >= viewState.Cols)
        {
            viewState.CursorCol = viewState.Cols - 1;
        }

        AdjustSelection(srow, scol);
    }

    private void ArrowUp()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow--;
        if (viewState.CursorRow < 0)
        {
            viewState.CursorRow = 0;
        }

        AdjustSelection(srow, scol);
    }

    private void ArrowDown()
    {
        if (viewState == null) return;
        long srow = viewState.CursorRow;
        int scol = viewState.CursorCol;

        viewState.CursorRow++;
        if (viewState.CursorRow >= viewState.Rows)
        {
            viewState.CursorRow = viewState.Rows - 1;
        }

        AdjustSelection(srow, scol);
    }

    private void Find()
    {
        if (viewState == null || viewState.HugeFile.InSearch) return;

        FindText ft = new FindText();
        if (ft.ShowDialog() == DialogResult.OK)
        {
            viewState.UseCase = ft.UseCase();
            viewState.FindText = ft.GetText();
            viewState.IsDown = ft.IsDown();

            Search(viewState.CursorRow, viewState.CursorCol, viewState.IsDown);
        }
    }

    private void FindAgain()
    {
        if (viewState == null || viewState.HugeFile.InSearch) return;
        if (string.IsNullOrEmpty(viewState.FindText)) return;

        Search(viewState.CursorRow, viewState.CursorCol + (viewState.IsDown ? 1 : 0), viewState.IsDown);
    }

    private void Search(long row, int col, bool isDown)
    {
        viewState.IsDown = isDown;

        if (viewState == null || string.IsNullOrEmpty(viewState.FindText))
            return;

        viewState.HugeFile.Search(row, col, viewState.FindText, viewState.UseCase, isDown);

        againMenuItem.Enabled = false;
        searchMenuItem.Enabled = false;

        timerCursor.Stop();
        timerSearch.Start();
    }

    internal void CursorTick(TextControl ucText)
    {
        if (CursorRow < 0)
        {
            CursorRow = 0;
        }
        else if (CursorRow >= Rows)
        {
            CursorRow = Rows - 1;
        }

        if (CursorCol < 0)
        {
            CursorCol = 0;
        }
        else if (CursorCol >= Cols)
        {
            CursorCol = Cols - 1;
        }

        if (CursorRow < StartRow)
        {
            StartRow = CursorRow;
        }
        else if (CursorRow >= (StartRow + ScreenHeight))
        {
            StartRow = CursorRow - ScreenHeight + 1;
        }

        if (CursorCol < StartCol)
        {
            StartCol = CursorCol;
        }
        else if (CursorCol >= (StartCol + ScreenWidth))
        {
            StartCol = CursorCol - ScreenWidth + 1;
        }

        if (StartRow < 0)
        {
            StartRow = 0;
        }
        if (StartCol < 0)
        {
            StartCol = 0;
        }
        if (CursorRow < 0)
        {
            CursorRow = 0;
        }
        if (CursorCol < 0)
        {
            CursorCol = 0;
        }
    }

    internal void ViewerSizeChanged(Control ucText, VScrollBar vScrollBar, HScrollBar hScrollBar)
    {
        float y = ucText.Size.Height;
        ScreenHeight = (int)((y - ucText.Padding.Top - ucText.Padding.Bottom) / FontHeight);

        bool vscroll = (Rows > ScreenHeight);
        vScrollBar.Visible = vscroll;
        vScrollBar.Enabled = vscroll;
        if (vscroll)
        {
            vScrollBar.Maximum = (Rows >= int.MaxValue) ? int.MaxValue : (int)Rows;
        }

        float x = ucText.Size.Width;
        int xPadding = ucText.Padding.Right + ucText.Padding.Left + (vScrollBar.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
        ScreenWidth = (int)((x - xPadding) / FontWidth);
        bool hscroll = (Cols > ScreenWidth);

        hScrollBar.Visible = hscroll;
        hScrollBar.Enabled = hscroll;
        if (hscroll)
        {
            hScrollBar.Maximum = Cols;
            ScreenHeight = (int)((y - ucText.Padding.Top - ucText.Padding.Bottom - SystemInformation.HorizontalScrollBarHeight) / FontHeight);
        }
    }
}
