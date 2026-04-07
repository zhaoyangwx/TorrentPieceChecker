
Partial Class Form1
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer
    Friend WithEvents btnOpen As Button
    Friend WithEvents btnCheck As Button
    Friend WithEvents dgv As DataGridView
    Friend WithEvents panelMap As Panel
    Friend WithEvents txtDir As TextBox

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.btnOpen = New System.Windows.Forms.Button()
        Me.btnCheck = New System.Windows.Forms.Button()
        Me.dgv = New System.Windows.Forms.DataGridView()
        Me.DataGridViewTextBoxColumn1 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.DataGridViewTextBoxColumn2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGridViewTextBoxColumn3 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.panelMap = New System.Windows.Forms.Panel()
        Me.pictureBoxBlk = New System.Windows.Forms.PictureBox()
        Me.txtDir = New System.Windows.Forms.TextBox()
        Me.txtInfo = New System.Windows.Forms.TextBox()
        Me.btnStop = New System.Windows.Forms.Button()
        Me.btnDraw = New System.Windows.Forms.Button()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.btnScaleUp = New System.Windows.Forms.Button()
        Me.btnScaleDown = New System.Windows.Forms.Button()
        Me.btnFolderBrowser = New System.Windows.Forms.Button()
        CType(Me.dgv, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.panelMap.SuspendLayout()
        CType(Me.pictureBoxBlk, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btnOpen
        '
        Me.btnOpen.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnOpen.Location = New System.Drawing.Point(10, 12)
        Me.btnOpen.Name = "btnOpen"
        Me.btnOpen.Size = New System.Drawing.Size(75, 23)
        Me.btnOpen.TabIndex = 0
        Me.btnOpen.Text = "打开"
        Me.btnOpen.UseVisualStyleBackColor = True
        '
        'btnCheck
        '
        Me.btnCheck.Enabled = False
        Me.btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnCheck.Location = New System.Drawing.Point(120, 12)
        Me.btnCheck.Name = "btnCheck"
        Me.btnCheck.Size = New System.Drawing.Size(75, 23)
        Me.btnCheck.TabIndex = 1
        Me.btnCheck.Text = "开始校验"
        Me.btnCheck.UseVisualStyleBackColor = True
        '
        'dgv
        '
        Me.dgv.AllowUserToAddRows = False
        Me.dgv.AllowUserToDeleteRows = False
        Me.dgv.AllowUserToOrderColumns = True
        Me.dgv.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.dgv.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.DataGridViewTextBoxColumn1, Me.DataGridViewTextBoxColumn2, Me.DataGridViewTextBoxColumn3})
        Me.dgv.Location = New System.Drawing.Point(10, 68)
        Me.dgv.Name = "dgv"
        Me.dgv.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgv.Size = New System.Drawing.Size(284, 339)
        Me.dgv.TabIndex = 5
        Me.dgv.VirtualMode = True
        '
        'DataGridViewTextBoxColumn1
        '
        Me.DataGridViewTextBoxColumn1.HeaderText = "选中"
        Me.DataGridViewTextBoxColumn1.Name = "DataGridViewTextBoxColumn1"
        Me.DataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.[True]
        Me.DataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic
        Me.DataGridViewTextBoxColumn1.Width = 40
        '
        'DataGridViewTextBoxColumn2
        '
        Me.DataGridViewTextBoxColumn2.HeaderText = "Index"
        Me.DataGridViewTextBoxColumn2.Name = "DataGridViewTextBoxColumn2"
        '
        'DataGridViewTextBoxColumn3
        '
        Me.DataGridViewTextBoxColumn3.HeaderText = "状态"
        Me.DataGridViewTextBoxColumn3.Name = "DataGridViewTextBoxColumn3"
        '
        'panelMap
        '
        Me.panelMap.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.panelMap.AutoScroll = True
        Me.panelMap.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.panelMap.Controls.Add(Me.pictureBoxBlk)
        Me.panelMap.Location = New System.Drawing.Point(300, 68)
        Me.panelMap.Name = "panelMap"
        Me.panelMap.Size = New System.Drawing.Size(669, 339)
        Me.panelMap.TabIndex = 7
        '
        'pictureBoxBlk
        '
        Me.pictureBoxBlk.Location = New System.Drawing.Point(0, 0)
        Me.pictureBoxBlk.Name = "pictureBoxBlk"
        Me.pictureBoxBlk.Size = New System.Drawing.Size(100, 50)
        Me.pictureBoxBlk.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pictureBoxBlk.TabIndex = 0
        Me.pictureBoxBlk.TabStop = False
        '
        'txtDir
        '
        Me.txtDir.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtDir.Location = New System.Drawing.Point(10, 41)
        Me.txtDir.Name = "txtDir"
        Me.txtDir.Size = New System.Drawing.Size(878, 21)
        Me.txtDir.TabIndex = 4
        Me.txtDir.Text = "I:\Minerva_Myrient"
        '
        'txtInfo
        '
        Me.txtInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInfo.Location = New System.Drawing.Point(10, 413)
        Me.txtInfo.Multiline = True
        Me.txtInfo.Name = "txtInfo"
        Me.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtInfo.Size = New System.Drawing.Size(959, 134)
        Me.txtInfo.TabIndex = 6
        Me.txtInfo.WordWrap = False
        '
        'btnStop
        '
        Me.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnStop.Location = New System.Drawing.Point(201, 12)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(75, 23)
        Me.btnStop.TabIndex = 2
        Me.btnStop.Text = "停止校验"
        '
        'btnDraw
        '
        Me.btnDraw.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnDraw.Enabled = False
        Me.btnDraw.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnDraw.Location = New System.Drawing.Point(894, 12)
        Me.btnDraw.Name = "btnDraw"
        Me.btnDraw.Size = New System.Drawing.Size(75, 23)
        Me.btnDraw.TabIndex = 3
        Me.btnDraw.Text = "刷新"
        Me.btnDraw.UseVisualStyleBackColor = True
        '
        'btnScaleUp
        '
        Me.btnScaleUp.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnScaleUp.Enabled = False
        Me.btnScaleUp.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnScaleUp.Location = New System.Drawing.Point(840, 12)
        Me.btnScaleUp.Name = "btnScaleUp"
        Me.btnScaleUp.Size = New System.Drawing.Size(21, 23)
        Me.btnScaleUp.TabIndex = 8
        Me.btnScaleUp.Text = "+"
        Me.btnScaleUp.UseVisualStyleBackColor = True
        '
        'btnScaleDown
        '
        Me.btnScaleDown.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnScaleDown.Enabled = False
        Me.btnScaleDown.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnScaleDown.Location = New System.Drawing.Point(867, 12)
        Me.btnScaleDown.Name = "btnScaleDown"
        Me.btnScaleDown.Size = New System.Drawing.Size(21, 23)
        Me.btnScaleDown.TabIndex = 9
        Me.btnScaleDown.Text = "-"
        Me.btnScaleDown.UseVisualStyleBackColor = True
        '
        'btnFolderBrowser
        '
        Me.btnFolderBrowser.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnFolderBrowser.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnFolderBrowser.Location = New System.Drawing.Point(894, 39)
        Me.btnFolderBrowser.Name = "btnFolderBrowser"
        Me.btnFolderBrowser.Size = New System.Drawing.Size(75, 23)
        Me.btnFolderBrowser.TabIndex = 10
        Me.btnFolderBrowser.Text = "选择"
        Me.btnFolderBrowser.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(981, 559)
        Me.Controls.Add(Me.btnFolderBrowser)
        Me.Controls.Add(Me.btnScaleDown)
        Me.Controls.Add(Me.btnScaleUp)
        Me.Controls.Add(Me.btnDraw)
        Me.Controls.Add(Me.btnStop)
        Me.Controls.Add(Me.txtInfo)
        Me.Controls.Add(Me.btnOpen)
        Me.Controls.Add(Me.btnCheck)
        Me.Controls.Add(Me.dgv)
        Me.Controls.Add(Me.panelMap)
        Me.Controls.Add(Me.txtDir)
        Me.DoubleBuffered = True
        Me.KeyPreview = True
        Me.Name = "Form1"
        Me.Text = "Torrent Piece Checker PRO"
        CType(Me.dgv, System.ComponentModel.ISupportInitialize).EndInit()
        Me.panelMap.ResumeLayout(False)
        Me.panelMap.PerformLayout()
        CType(Me.pictureBoxBlk, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtInfo As TextBox
    Friend WithEvents btnStop As Button
    Friend WithEvents btnDraw As Button
    Friend WithEvents DataGridViewTextBoxColumn1 As DataGridViewCheckBoxColumn
    Friend WithEvents DataGridViewTextBoxColumn2 As DataGridViewTextBoxColumn
    Friend WithEvents DataGridViewTextBoxColumn3 As DataGridViewTextBoxColumn
    Friend WithEvents pictureBoxBlk As PictureBox
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents btnScaleUp As Button
    Friend WithEvents btnScaleDown As Button
    Friend WithEvents btnFolderBrowser As Button
End Class
