
Imports System.Drawing
Imports BencodeNET.Torrents
Imports System.IO
Imports BencodeNET.Parsing
Imports System.Security.Cryptography
Imports System.Globalization
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports System.Text

Public Class Form1

    Private Pieces As List(Of PieceModel)
    Private TorrentObj As BencodeNET.Torrents.Torrent
    Private mapBitmap As Bitmap
    Private mapGraphics As Graphics

    Private blockSize As Integer = 8
    Private gap As Integer = 1
    Private cols As Integer = 1
    Private torrentLoaded As Boolean =False
    Private stopFlag As Boolean = False
    Private forceRedraw As Boolean = False

    ' 缓存上一次状态（用于局部刷新）
    Private lastStates() As Integer = {}
    Private Sub InitMap()

        If Pieces Is Nothing Then Return

        ' 自动缩放（关键优化）
        If Pieces.Count > 200000 Then blockSize = 2
        If Pieces.Count > 50000 Then blockSize = 4
        If Pieces.Count < 10000 Then blockSize = 8

        Dim cell = blockSize + gap
        cols = Math.Max(1, panelMap.Width \ (blockSize + gap))
        Dim rows As Integer = CInt(Math.Ceiling(Pieces.Count / cols))
        Dim bmpW = cols * cell
        Dim bmpH = rows * cell
        ' 防止极端大（可选保护）
        If bmpH > 30000 Then bmpH = 30000 ' 可调

        mapBitmap?.Dispose()
        mapBitmap = New Bitmap(bmpW, bmpH)
        mapGraphics = Graphics.FromImage(mapBitmap)

        mapGraphics.Clear(Color.Black)
        ReDim lastStates(Pieces.Count - 1)
        For i = 0 To lastStates.Length - 1
            lastStates(i) = -1 ' 强制首次全绘
        Next

        pictureBoxBlk.Image = mapBitmap
    End Sub
    Private Sub btnOpen_Click(sender As Object, e As EventArgs) Handles btnOpen.Click
        Dim ofd As New OpenFileDialog()
        If ofd.ShowDialog() = DialogResult.OK Then
            Dim res = TorrentMapper.Load(ofd.FileName)
            TorrentObj = res.Item1
            Pieces = res.Item2

            dgv.Rows.Clear()

            For Each p In Pieces
                dgv.Rows.Add(True, p.Index, "未校验")
            Next

            'DrawMap()
            lastStates = {}
            InitMap()
            DrawMapFast()
            btnCheck.Enabled = True
            btnDraw.Enabled = True
            torrentLoaded = True
        End If
    End Sub

    Private Sub btnCheck_Click(sender As Object, e As EventArgs) Handles btnCheck.Click
        stopFlag = False
        For i = 0 To Pieces.Count - 1
            Dim row = dgv.Rows(i)
            If Not CBool(row.Cells(0).Value) Then Continue For

            Dim state = PieceChecker.Check(TorrentObj, Pieces(i), txtDir.Text)
            Pieces(i).State = state

            If state = 1 Then
                row.DefaultCellStyle.BackColor = Color.LightGreen
                row.Cells(0).Value = False
                row.Cells(2).Value = "OK"
            Else
                row.DefaultCellStyle.BackColor = Color.LightCoral
                row.Cells(2).Value = "FAIL"
            End If
            If forceRedraw Then
                forceRedraw = False
                InitMap()
            End If
            DrawMapFast()
            EnsureRowVisible(i)
            Application.DoEvents()
            If stopFlag Then
                Exit For
            End If
        Next

    End Sub
    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        stopFlag = True
    End Sub
    Private Sub btnDraw_Click(sender As Object, e As EventArgs) Handles btnDraw.Click
        InitMap()
        DrawMapFast()
    End Sub

    Private Sub EnsureRowVisible(i As Integer)

        If i < 0 OrElse i >= dgv.RowCount Then Return

        Dim first = dgv.FirstDisplayedScrollingRowIndex
        Dim visibleCount = dgv.DisplayedRowCount(False)
        Dim last = first + visibleCount - 1

        ' 👇 如果已经在可见范围内 → 不动
        If i >= first AndAlso i <= last Then Return

        ' 👇 不在范围 → 滚动（居中更舒服）
        Dim target = Math.Max(0, i - visibleCount \ 2)
        dgv.FirstDisplayedScrollingRowIndex = target

    End Sub
    Private Sub DrawMap()

        If Pieces Is Nothing OrElse Pieces.Count = 0 Then Return

        Dim bmp As New Bitmap(panelMap.Width, panelMap.Height)
        Dim g = Graphics.FromImage(bmp)

        g.Clear(Color.Black)

        ' ===== 参数（可调）=====
        Dim blockSize As Integer = 8      ' 小方块尺寸
        Dim gap As Integer = 1            ' 间距

        Dim usableWidth = panelMap.Width
        Dim cols As Integer = Math.Max(1, usableWidth \ (blockSize + gap))

        For i = 0 To Pieces.Count - 1

            Dim row = i \ cols
            Dim col = i Mod cols

            Dim x = col * (blockSize + gap)
            Dim y = row * (blockSize + gap)

            If y > panelMap.Height Then Exit For

            Dim c As Color = Color.Gray
            If Pieces(i).State = 1 Then c = Color.LimeGreen
            If Pieces(i).State = 2 Then c = Color.Red

            Using br As New SolidBrush(c)
                g.FillRectangle(br, x, y, blockSize, blockSize)
            End Using
        Next

        panelMap.BackgroundImage = bmp
    End Sub
    Private Sub DrawMapFast()

        If Pieces Is Nothing OrElse mapGraphics Is Nothing Then Return

        For i = 0 To Pieces.Count - 1

            If Pieces(i).State = lastStates(i) Then Continue For

            Dim row = i \ cols
            Dim col = i Mod cols

            Dim x = col * (blockSize + gap)
            Dim y = row * (blockSize + gap)


            Dim c As Color = Color.Gray
            If Pieces(i).State = 1 Then c = Color.LimeGreen
            If Pieces(i).State = 2 Then c = Color.Red

            Using br As New SolidBrush(c)
                mapGraphics.FillRectangle(br, x, y, blockSize, blockSize)
            End Using

            lastStates(i) = Pieces(i).State
        Next

        pictureBoxBlk.Invalidate() ' 只触发重绘
    End Sub
    Private Sub pictureBoxBlk_MouseClick(sender As Object, e As MouseEventArgs) Handles pictureBoxBlk.MouseClick
        If Not torrentLoaded Then Exit Sub
        Dim col = e.X \ (blockSize + gap)
        Dim row = e.Y \ (blockSize + gap)

        Dim idx = row * cols + col
        If idx < 0 OrElse idx >= Pieces.Count Then Return

        Dim p = Pieces(idx)

        Dim sb As New System.Text.StringBuilder(256)

        Dim okCount As Integer = 0
        Dim missCount As Integer = 0

        ' 先统计
        For Each f In p.Files
            If IO.File.Exists(IO.Path.Combine(txtDir.Text, f)) Then
                okCount += 1
            Else
                missCount += 1
            End If
        Next

        ' 再输出
        sb.AppendLine($"Piece: {p.Index}")
        sb.AppendLine($"Offset: {p.StartOffset}")
        sb.AppendLine($"Files: {okCount} OK, {missCount} Missing")
        sb.AppendLine()

        ' 缺失优先排序
        For Each f In p.Files.OrderBy(Function(x) IO.File.Exists(IO.Path.Combine(txtDir.Text, x)))

            Dim exist As Boolean = IO.File.Exists(IO.Path.Combine(txtDir.Text, f))

            If exist Then
                sb.Append("  ").AppendLine(f)
            Else
                sb.Append("× ").AppendLine(f)
            End If

        Next

        txtInfo.Text = sb.ToString()

    End Sub

    Private Sub pictureBoxBlk_MouseWheel(sender As Object, e As MouseEventArgs) Handles pictureBoxBlk.MouseWheel
        If Not torrentLoaded Then Exit Sub
        If e.Delta > 0 Then
            blockSize += 1
        Else
            blockSize = Math.Max(2, blockSize - 1)
        End If

        InitMap()
        DrawMapFast()
    End Sub
    Private Sub pictureBoxBlk_MouseMove(sender As Object, e As MouseEventArgs) Handles pictureBoxBlk.MouseMove
        If Not torrentLoaded Then Exit Sub
        Dim col = e.X \ (blockSize + gap)
        Dim row = e.Y \ (blockSize + gap)

        Dim idx = row * cols + col

        If idx >= 0 AndAlso idx < Pieces.Count Then
            Dim p = Pieces(idx)

            ToolTip1.SetToolTip(pictureBoxBlk,
                $"Piece {p.Index}" & vbCrLf &
                $"Files: {p.Files.Count}")
        End If

    End Sub

    Private Sub Form1_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged
        forceRedraw = True
    End Sub

End Class




Public Class PieceChecker

    Public Shared Function Check(t As Torrent, p As PieceModel, baseDir As String) As Integer

        Dim sha1hasher = SHA1.Create()
        Dim buffer(p.Length - 1) As Byte

        Dim written As Integer = 0
        Dim globalOffset = p.StartOffset

        Dim files = If(t.FileMode = TorrentFileMode.Single,
            {t.File}.Select(Function(f) (f.FileName, f.FileSize)),
            t.Files.Select(Function(f) (f.FullPath, f.FileSize)))

        Dim cur As Long = 0

        For Each f In files

            Dim start = cur
            Dim [end] = cur + f.Item2

            If globalOffset < [end] AndAlso written < p.Length Then

                Dim readStart = Math.Max(0, globalOffset - start)
                Dim path = IO.Path.Combine(baseDir, f.Item1)

                If Not File.Exists(path) Then Return 2

                Using fs As New FileStream(path, FileMode.Open, FileAccess.Read)
                    fs.Seek(readStart, SeekOrigin.Begin)

                    While written < p.Length AndAlso fs.Position < fs.Length
                        Dim r = fs.Read(buffer, written, p.Length - written)
                        If r = 0 Then Exit While
                        written += r
                    End While
                End Using
            End If

            cur += f.Item2
        Next

        Dim hash = sha1hasher.ComputeHash(buffer)
        Dim target(19) As Byte
        Array.Copy(t.Pieces, p.Index * 20, target, 0, 20)

        Return If(hash.SequenceEqual(target), 1, 2)
    End Function

End Class


Public Class PieceModel
    Public Property Index As Integer
    Public Property StartOffset As Long
    Public Property Length As Long
    Public Property Files As New List(Of String)
    Public Property State As Integer = 0 '0=未校验 1=通过 2=失败
    Public Property Checked As Boolean = True
End Class



Public Class TorrentMapper

    Public Shared Function Load(torrentPath As String) As (Torrent, List(Of PieceModel))
        Dim parser = New BencodeParser()
        Dim t = parser.Parse(Of Torrent)(torrentPath)

        Dim pieces As New List(Of PieceModel)
        Dim pieceCount = t.Pieces.Length \ 20
        Dim pieceSize = t.PieceSize

        Dim files = If(t.FileMode = TorrentFileMode.Single,
            {t.File}.Select(Function(f) (f.FileName, f.FileSize)),
            t.Files.Select(Function(f) (f.FullPath, f.FileSize)))
        Dim totalSize As Long
        If t.FileMode = TorrentFileMode.Single Then
            totalSize = t.File.FileSize
        Else
            totalSize = t.Files.Sum(Function(f) f.FileSize)
        End If

        For i = 0 To pieceCount - 1
            Dim remaining = totalSize - CLng(i) * pieceSize
            Dim p As New PieceModel With {
                .Index = i,
                .StartOffset = CLng(i) * pieceSize,
                .Length = Math.Min(pieceSize, remaining)
            }

            Dim remain = pieceSize
            Dim offset = p.StartOffset
            Dim cur As Long = 0

            For Each f In files
                Dim start = cur
                Dim [end] = cur + f.Item2

                If offset < [end] AndAlso offset + remain > start Then
                    p.Files.Add(f.Item1)
                End If

                cur += f.Item2
            Next

            pieces.Add(p)
        Next

        Return (t, pieces)
    End Function

End Class
Public Class DoubleBufferedPanel
    Inherits Panel

    Public Sub New()
        Me.DoubleBuffered = True
        Me.ResizeRedraw = True
    End Sub
End Class