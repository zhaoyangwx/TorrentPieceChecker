
Imports BencodeNET.Torrents
Imports System.IO
Imports BencodeNET.Parsing
Imports System.Security.Cryptography
Imports System.ComponentModel
Imports System.Buffers

Public Class Form1

    Private Pieces As List(Of PieceModel)
    Private TorrentObj As BencodeNET.Torrents.Torrent
    Private mapBitmap As Bitmap
    Private mapGraphics As Graphics

    Private blockSize As Integer = 8
    Private gap As Integer = 1
    Private cols As Integer = 1
    Private torrentLoaded As Boolean = False
    Private stopFlag As Boolean = False
    Private forceRedraw As Boolean = False

    ' 缓存上一次状态（用于局部刷新）
    Private lastStates() As Integer = {}
    Private Sub InitMap(Optional ByVal AutoBlockSize As Boolean = True)

        If Pieces Is Nothing Then Return
        If AutoBlockSize Then
            ' 自动缩放（关键优化）
            If Pieces.Count > 200000 Then blockSize = 2
            If Pieces.Count > 50000 Then blockSize = 4
            If Pieces.Count < 10000 Then blockSize = 8
        End If

        Dim cell = blockSize + gap
        cols = Math.Max(1, (panelMap.Width - 20) \ (blockSize + gap))
        Dim rows As Integer = CInt(Math.Ceiling(Pieces.Count / cols))
        Dim bmpW = cols * cell
        Dim bmpH = rows * cell
        ' 防止极端大（可选保护）
        If bmpH > 32767 Then bmpH = 32767 ' 可调

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
            Dim res = TorrentMapper.Load(ofd.FileName, Sub(i As Long, piececount As Long)
                                                           If i Mod 1000 = 0 Then
                                                               txtInfo.Text = $"Load block {i + 1}/{piececount}"
                                                               Application.DoEvents()
                                                           End If
                                                       End Sub)
            TorrentObj = res.Item1
            Pieces = res.Item2
            dgv.RowCount = Pieces.Count
            lastStates = {}
            InitMap()
            DrawMapFast()
            txtInfo.Text = $"Loaded: {ofd.FileName}{vbCrLf}Piece count: {Pieces.Count}"
            btnCheck.Enabled = True
            btnDraw.Enabled = True
            btnScaleUp.Enabled = True
            btnScaleDown.Enabled = True
            torrentLoaded = True
        End If
    End Sub
    Private Sub btnCheck_Click(sender As Object, e As EventArgs) Handles btnCheck.Click
        stopFlag = False
        Dim fileStreams As New Dictionary(Of String, FileStream)
        Dim lastRefreshTime As Date = Now
        DrawMapFast()
        For i = 0 To Pieces.Count - 1
            If Not Pieces(i).Checked Then Continue For

            Dim state = PieceChecker.Check(TorrentObj, Pieces(i), txtDir.Text, fileStreams)
            Pieces(i).State = state
            If state = 1 Then Pieces(i).Checked = False
            If state >= 2 OrElse (Now - lastRefreshTime).TotalMilliseconds >= 300 Then
                lastRefreshTime = Now
                dgv.InvalidateRow(i)
                If forceRedraw Then
                    forceRedraw = False
                    InitMap(False)
                End If
                DrawMapFast()
                EnsureRowVisible(i)
            End If

            Application.DoEvents()
            If stopFlag Then
                For Each fs In fileStreams.Values
                    fs.Dispose()
                Next
                Exit For
            End If
        Next
        For Each fs In fileStreams.Values
            fs.Dispose()
        Next
    End Sub
    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        stopFlag = True
    End Sub
    Private Sub btnDraw_Click(sender As Object, e As EventArgs) Handles btnDraw.Click
        InitMap(False)
        DrawMapFast()
    End Sub
    Private Sub dgv_CellValueNeeded(sender As Object, e As DataGridViewCellValueEventArgs) Handles dgv.CellValueNeeded
        If e.RowIndex < 0 OrElse e.RowIndex >= Pieces.Count Then Return

        Dim p = Pieces(e.RowIndex)

        Select Case e.ColumnIndex
            Case 0 ' checkbox
                e.Value = (p.Checked)

            Case 1 ' index
                e.Value = p.Index

            Case 2 ' 状态
                Select Case p.State
                    Case 0 : e.Value = "未校验"
                    Case 1 : e.Value = "OK"
                    Case 2 : e.Value = "FAIL"
                    Case 3 : e.Value = "文件缺失"
                    Case 4 : e.Value = "文件无法打开"
                End Select
        End Select
    End Sub
    Private Sub dgv_CellValuePushed(sender As Object, e As DataGridViewCellValueEventArgs) Handles dgv.CellValuePushed
        If e.RowIndex < 0 OrElse e.RowIndex >= Pieces.Count Then Return
        Dim p = Pieces(e.RowIndex)
        If e.ColumnIndex = 0 Then
            p.Checked = e.Value
        End If
    End Sub
    Private Sub dgv_RowPrePaint(sender As Object, e As DataGridViewRowPrePaintEventArgs) Handles dgv.RowPrePaint
        If Pieces Is Nothing Then Exit Sub
        If e.RowIndex < 0 OrElse e.RowIndex >= Pieces.Count Then Exit Sub
        Dim p = Pieces(e.RowIndex)

        Select Case p.State
            Case 1
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.LightGreen
            Case 2
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.LightCoral
            Case 3
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.Orange
            Case 3
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.Plum
            Case Else
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.White
        End Select

    End Sub
    Private Sub EnsureRowVisible(i As Integer)

        If i < 0 OrElse i >= dgv.RowCount Then Return

        Dim first = dgv.FirstDisplayedScrollingRowIndex
        Dim visibleCount = dgv.DisplayedRowCount(False)
        Dim last = first + visibleCount - 1

        ' 👇 如果已经在可见范围内 → 不动
        If i >= first AndAlso i <= last Then Return

        ' 👇 不在范围 → 滚动（居中更舒服）
        Dim target = Math.Max(0, i - visibleCount \ 3)
        dgv.FirstDisplayedScrollingRowIndex = target

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
            If Pieces(i).State = PieceModel.PieceState.Pass Then c = Color.LimeGreen
            If Pieces(i).State = PieceModel.PieceState.Fail Then c = Color.Red
            If Pieces(i).State = PieceModel.PieceState.FileMissing Then c = Color.Orange
            If Pieces(i).State = PieceModel.PieceState.CannotOpenFile Then c = Color.DarkViolet

            Using br As New SolidBrush(c)
                mapGraphics.FillRectangle(br, x, y, blockSize, blockSize)
            End Using

            lastStates(i) = Pieces(i).State
        Next

        pictureBoxBlk.Invalidate() ' 只触发重绘
    End Sub
    Private Sub PrintBlockDetails(idx As Integer)
        Dim p = Pieces(idx)

        Dim sb As New System.Text.StringBuilder(256)

        Dim okCount As Integer = 0
        Dim missCount As Integer = 0

        ' 先统计
        For Each f In p.Files
            If f.Name.StartsWith(".pad", StringComparison.OrdinalIgnoreCase) Then Continue For
            If IO.File.Exists(IO.Path.Combine(txtDir.Text, f.Name)) Then
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
        For Each f In p.Files.OrderBy(Function(x) IO.File.Exists(IO.Path.Combine(txtDir.Text, x.Name)))
            If f.Name.StartsWith(".pad", StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim exist As Boolean = IO.File.Exists(IO.Path.Combine(txtDir.Text, f.Name))
            If exist Then
                sb.Append("  ").AppendLine(f.Name)
            Else
                sb.Append("× ").AppendLine(f.Name)
            End If

        Next

        txtInfo.Text = sb.ToString()
    End Sub
    Private Sub pictureBoxBlk_MouseClick(sender As Object, e As MouseEventArgs) Handles pictureBoxBlk.MouseClick
        If Not torrentLoaded Then Exit Sub
        Dim col = e.X \ (blockSize + gap)
        Dim row = e.Y \ (blockSize + gap)

        Dim idx = row * cols + col
        If idx < 0 OrElse idx >= Pieces.Count Then Return
        PrintBlockDetails(idx)
        EnsureRowVisible(idx)
    End Sub

    Private Sub pictureBoxBlk_MouseWheel(sender As Object, e As MouseEventArgs) Handles pictureBoxBlk.MouseWheel
        If Not torrentLoaded Then Exit Sub
        If Not CtrlPressing Then Exit Sub
        If e.Delta > 0 Then
            blockSize += 1
        Else
            blockSize = Math.Max(1, blockSize - 1)
        End If

        InitMap(False)
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

    Private Sub btnScaleUp_Click(sender As Object, e As EventArgs) Handles btnScaleUp.Click
        If Not torrentLoaded Then Exit Sub
        blockSize += 1
        InitMap(False)
        DrawMapFast()
    End Sub

    Private Sub btnScaleDown_Click(sender As Object, e As EventArgs) Handles btnScaleDown.Click
        If Not torrentLoaded Then Exit Sub
        blockSize = Math.Max(1, blockSize - 1)
        InitMap(False)
        DrawMapFast()
    End Sub

    Private Sub btnFolderBrowser_Click(sender As Object, e As EventArgs) Handles btnFolderBrowser.Click
        Dim fbd As New FolderBrowserDialog
        If fbd.ShowDialog = DialogResult.OK Then
            txtDir.Text = fbd.SelectedPath
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        txtDir.Text = My.Settings.lastPath
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.lastPath = txtDir.Text
    End Sub

    Private CtrlPressing As Boolean = False
    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Control Then
            CtrlPressing = True
        End If
    End Sub
    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        If Not e.Control Then
            CtrlPressing = False
            If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Space Then
                For Each r As DataGridViewRow In dgv.SelectedRows
                    Pieces(r.Index).Checked = True
                Next
                dgv.Invalidate()
            ElseIf e.KeyCode = Keys.Delete OrElse e.KeyCode = Keys.Back Then
                For Each r As DataGridViewRow In dgv.SelectedRows
                    Pieces(r.Index).Checked = False
                Next
                dgv.Invalidate()
            End If
        End If
    End Sub

    Private Sub dgv_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgv.CellClick
        If Not torrentLoaded Then Exit Sub
        PrintBlockDetails(e.RowIndex)
    End Sub

    Private Sub chkState0_CheckedChanged(sender As Object, e As EventArgs) Handles chkState0.CheckedChanged
        If Not torrentLoaded Then Exit Sub
        For Each p In Pieces
            If p.State = 0 Then p.Checked = chkState0.Checked
        Next
        dgv.Invalidate()
    End Sub

    Private Sub chkState1_CheckedChanged(sender As Object, e As EventArgs) Handles chkState1.CheckedChanged
        If Not torrentLoaded Then Exit Sub
        For Each p In Pieces
            If p.State >= 2 Then p.Checked = chkState1.Checked
        Next
        dgv.Invalidate()
    End Sub

    Private Sub btnImportPieces_Click(sender As Object, e As EventArgs) Handles btnImportPieces.Click
        With (New OpenFileDialog With {.Filter = "xml|*.xml|any|*.*"})
            If .ShowDialog = DialogResult.OK Then
                Dim imp As PieceModelSaver = PieceModelSaver.FromXML(IO.File.ReadAllText(.FileName))
                If imp.SaveData IsNot Nothing AndAlso imp.SaveData.Count > 0 Then
                    Pieces = imp.SaveData
                    dgv.Invalidate()
                End If
            End If
        End With
    End Sub

    Private Sub btnExportPieces_Click(sender As Object, e As EventArgs) Handles btnExportPieces.Click
        With (New SaveFileDialog With {.Filter = "xml|*.xml|any|*.*"})
            If .ShowDialog = DialogResult.OK Then
                Dim exp As New PieceModelSaver With {.SaveData = Pieces}
                exp.SaveSerializedText(.FileName)
            End If
        End With
    End Sub

End Class




Public Class PieceChecker
    <ThreadStatic>
    Private Shared sha1hasher As Security.Cryptography.SHA1 = SHA1.Create()
    Public Shared Function Check(t As Torrent, p As PieceModel, baseDir As String,
            fileStreams As Dictionary(Of String, FileStream)) As Integer

        Dim buffer() As Byte = ArrayPool(Of Byte).Shared.Rent(p.Length)

        Dim written As Integer = 0
        Dim globalOffset = p.StartOffset

        For Each f In p.Files

            Dim start = f.FileOffset
            Dim [end] = f.FileOffset + f.FileSize

            If globalOffset < [end] AndAlso written < p.Length Then

                If Not f.Name.StartsWith(".pad", StringComparison.OrdinalIgnoreCase) Then

                    Dim path = IO.Path.Combine(baseDir, f.Name)

                    ' 🔥 每次都判断存在（满足你的需求）
                    If Not File.Exists(path) Then

                        ' 👉 如果之前有缓存，要清掉
                        If fileStreams.ContainsKey(path) Then
                            fileStreams(path).Dispose()
                            fileStreams.Remove(path)
                        End If
                        ArrayPool(Of Byte).Shared.Return(buffer)
                        Return 3
                    End If

                    ' 👉 获取或创建 FileStream（核心优化）
                    Dim fs As FileStream = Nothing

                    If Not fileStreams.TryGetValue(path, fs) Then
                        Try
                            fs = New FileStream(
                                                   path,
                                                   FileMode.Open,
                                                   FileAccess.Read,
                                                   FileShare.ReadWrite,   ' 🔥 支持用户修改文件
                                                   4096,
                                                   FileOptions.SequentialScan)
                        Catch ex As Exception
                            ArrayPool(Of Byte).Shared.Return(buffer)
                            Return 4
                        End Try


                        fileStreams(path) = fs
                    End If

                    ' 👉 计算读取位置
                    Dim readStart = Math.Max(0, globalOffset - start)

                    If fs.Position <> readStart Then
                        fs.Position = readStart
                    End If

                    While written < p.Length AndAlso fs.Position < fs.Length

                        Dim fileRemain = [end] - (start + readStart)
                        If fileRemain <= 0 Then Exit While

                        Dim need = Math.Min(p.Length - written, fileRemain)

                        Dim r = fs.Read(buffer, written, need)
                        If r = 0 Then Exit While

                        written += r

                    End While
                    If fs.Position = fs.Length Then
                        fs.Close()
                        fileStreams.Remove(path)
                    End If
                End If

            End If

        Next

        Dim hash = sha1hasher.ComputeHash(buffer, 0, p.Length)
        ArrayPool(Of Byte).Shared.Return(buffer)
        Dim target(19) As Byte
        Array.Copy(t.Pieces, p.Index * 20, target, 0, 20)

        Return If(hash.SequenceEqual(target), 1, 2)

    End Function

End Class

<Serializable>
Public Class PieceModel
    Public Property Index As Integer
    Public Property StartOffset As Long
    Public Property Length As Long
    Public Property Files As New List(Of FileSegment)
    Public Property State As Integer = 0 '0=未校验 1=通过 2=失败
    Public Enum PieceState
        Unknown = 0I
        Pass = 1I
        Fail = 2I
        FileMissing = 3I
        CannotOpenFile = 4I
    End Enum
    Public Property Checked As Boolean = True
    <Serializable>
    Public Class FileSegment
        Public Name As String
        Public FileOffset As Long
        Public FileSize As Long
        Public Sub New()

        End Sub
        Public Sub New(nm As String, ofs As Long, sz As Long)
            Name = nm
            FileOffset = ofs
            FileSize = sz
        End Sub
    End Class
End Class

<Serializable>
Public Class PieceModelSaver
    Public Property SaveData As List(Of PieceModel)
    Public Function GetSerializedText() As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(PieceModelSaver))
        Dim sb As New System.Text.StringBuilder()
        Dim t As IO.TextWriter = New IO.StringWriter(sb)
        writer.Serialize(t, Me)
        t.Close()
        Return sb.ToString
    End Function
    Public Sub SaveSerializedText(ByVal FileName As String)
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(PieceModelSaver))
        Dim ms As New IO.FileStream(FileName, IO.FileMode.Create)
        Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
        writer.Serialize(t, Me)
        ms.Close()
    End Sub
    Public Shared Function FromXML(s As String) As PieceModelSaver
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(PieceModelSaver))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), PieceModelSaver)
    End Function
End Class

Public Class TorrentMapper

    Public Shared Function Load(torrentPath As String, Optional ByVal ProgressReport As Action(Of Long, Long) = Nothing) As (Torrent, List(Of PieceModel))
        Dim parser = New BencodeParser()
        Dim t = parser.Parse(Of Torrent)(torrentPath)


        Dim pieceSize = t.PieceSize
        Dim pieceCount = t.Pieces.Length \ 20
        Dim pieces As New List(Of PieceModel)(pieceCount)

        ' 🔥 必须 ToList
        Dim files = If(t.FileMode = TorrentFileMode.Single,
            {t.File}.Select(Function(f) (f.FileName, f.FileSize)).ToList(),
            t.Files.Select(Function(f) (f.FullPath, f.FileSize)).ToList())

        Dim totalSize As Long = 0
        For Each f In files
            totalSize += f.Item2
        Next

        Dim fileIndex As Integer = 0
        Dim fileOffset As Long = 0

        For i = 0 To pieceCount - 1

            Dim pieceStart = CLng(i) * pieceSize
            Dim remaining = totalSize - pieceStart
            Dim length = CInt(Math.Min(pieceSize, remaining))

            Dim p As New PieceModel With {
                .Index = i,
                .StartOffset = pieceStart,
                .Length = length,
                .Files = New List(Of PieceModel.FileSegment)(2)
            }

            ' 👉 推进 fileIndex（只前进，不回退）
            While fileIndex < files.Count AndAlso pieceStart >= fileOffset + files(fileIndex).Item2
                fileOffset += files(fileIndex).Item2
                fileIndex += 1
            End While

            Dim curOffset = pieceStart
            Dim remain = length
            Dim idx = fileIndex
            Dim off = fileOffset

            While remain > 0 AndAlso idx < files.Count

                Dim fileSize = files(idx).Item2
                Dim fileEnd = off + fileSize

                p.Files.Add(New PieceModel.FileSegment(files(idx).Item1, off, fileSize))

                Dim take = Math.Min(remain, fileEnd - curOffset)

                remain -= take
                curOffset += take

                off += fileSize
                idx += 1
            End While

            pieces.Add(p)
            If ProgressReport IsNot Nothing Then ProgressReport(i, pieceCount)
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