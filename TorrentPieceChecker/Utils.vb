Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.Win32.SafeHandles
Imports System.Text

Public Class SymlinkHelper

#Region "Win32"

    Private Const FILE_FLAG_OPEN_REPARSE_POINT As Integer = &H200000
    Private Const FILE_FLAG_BACKUP_SEMANTICS As Integer = &H2000000
    Private Const OPEN_EXISTING As Integer = 3

    Private Const FSCTL_GET_REPARSE_POINT As Integer = &H900A8

    Private Const IO_REPARSE_TAG_SYMLINK As Integer = &HA000000C
    Private Const IO_REPARSE_TAG_MOUNT_POINT As Integer = &HA0000003

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function CreateFile(
        lpFileName As String,
        dwDesiredAccess As Integer,
        dwShareMode As FileShare,
        lpSecurityAttributes As IntPtr,
        dwCreationDisposition As Integer,
        dwFlagsAndAttributes As Integer,
        hTemplateFile As IntPtr) As SafeFileHandle
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(
        hDevice As SafeFileHandle,
        dwIoControlCode As Integer,
        lpInBuffer As IntPtr,
        nInBufferSize As Integer,
        lpOutBuffer As IntPtr,
        nOutBufferSize As Integer,
        ByRef lpBytesReturned As Integer,
        lpOverlapped As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetFileInformationByHandle(
        hFile As SafeFileHandle,
        ByRef lpFileInformation As BY_HANDLE_FILE_INFORMATION) As Boolean
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure BY_HANDLE_FILE_INFORMATION
        Public FileAttributes As Integer
        Public CreationTime As Long
        Public LastAccessTime As Long
        Public LastWriteTime As Long
        Public VolumeSerialNumber As Integer
        Public FileSizeHigh As Integer
        Public FileSizeLow As Integer
        Public NumberOfLinks As Integer
        Public FileIndexHigh As Integer
        Public FileIndexLow As Integer
    End Structure

#End Region

#Region "Public API"

    Public Shared Function GetFinalTarget(path As String) As String

        Dim visited As New HashSet(Of String)(StringComparer.Ordinal)

        Dim current As String = IO.Path.GetFullPath(path)

        Dim depth As Integer = 0
        Const MAX_DEPTH As Integer = 65535

        While True

            depth += 1
            If depth > MAX_DEPTH Then
                Return current
                'Throw New IOException("符号链接层级过深或存在循环")
            End If

            ' 🔥 用 FileId 做循环检测
            Dim fileId = GetFileId(current)

            If visited.Contains(fileId) Then
                Return current
                'Throw New IOException("检测到符号链接循环: " & current)
            End If

            visited.Add(fileId)

            Dim target = GetImmediateTarget(current)

            If target Is Nothing Then
                Return current
            End If

            ' 相对路径处理
            If Not IO.Path.IsPathRooted(target) Then
                target = IO.Path.Combine(IO.Path.GetDirectoryName(current), target)
            End If

            current = IO.Path.GetFullPath(target)
        End While
        Return current
    End Function

#End Region

#Region "Core"

    Private Shared Function GetImmediateTarget(path As String) As String

        Using handle = CreateFile(
            path,
            0,
            FileShare.ReadWrite Or FileShare.Delete,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT Or FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero)

            If handle.IsInvalid Then
                Return Nothing
            End If

            Dim bufferSize As Integer = 16 * 1024
            Dim buffer As IntPtr = Marshal.AllocHGlobal(bufferSize)

            Try
                Dim bytesReturned As Integer = 0

                If Not DeviceIoControl(
                    handle,
                    FSCTL_GET_REPARSE_POINT,
                    IntPtr.Zero,
                    0,
                    buffer,
                    bufferSize,
                    bytesReturned,
                    IntPtr.Zero) Then

                    Return Nothing
                End If

                Dim tag As Integer = Marshal.ReadInt32(buffer)

                If tag = IO_REPARSE_TAG_SYMLINK Then
                    Return ParseSymlink(buffer)
                ElseIf tag = IO_REPARSE_TAG_MOUNT_POINT Then
                    Return ParseMountPoint(buffer)
                Else
                    Return Nothing
                End If

            Finally
                Marshal.FreeHGlobal(buffer)
            End Try

        End Using

    End Function

#End Region

#Region "Parse"

    Private Shared Function ParseSymlink(buffer As IntPtr) As String

        Dim substituteOffset As Short = Marshal.ReadInt16(buffer, 8)
        Dim substituteLength As Short = Marshal.ReadInt16(buffer, 10)

        Dim pathBufferOffset As Integer = 20

        Dim bytes(substituteLength - 1) As Byte

        Marshal.Copy(
            IntPtr.Add(buffer, pathBufferOffset + substituteOffset),
            bytes,
            0,
            substituteLength)

        Dim path As String = Encoding.Unicode.GetString(bytes)

        Return NormalizePath(path)

    End Function

    Private Shared Function ParseMountPoint(buffer As IntPtr) As String

        Dim substituteOffset As Short = Marshal.ReadInt16(buffer, 8)
        Dim substituteLength As Short = Marshal.ReadInt16(buffer, 10)

        Dim pathBufferOffset As Integer = 16

        Dim bytes(substituteLength - 1) As Byte

        Marshal.Copy(
            IntPtr.Add(buffer, pathBufferOffset + substituteOffset),
            bytes,
            0,
            substituteLength)

        Dim path As String = Encoding.Unicode.GetString(bytes)

        Return NormalizePath(path)

    End Function

    Private Shared Function NormalizePath(path As String) As String
        If path.StartsWith("\??\") Then
            Return path.Substring(4)
        End If
        Return path
    End Function

#End Region

#Region "FileId"

    Private Shared Function GetFileId(path As String) As String

        Using handle = CreateFile(
            path,
            0,
            FileShare.ReadWrite Or FileShare.Delete,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero)

            If handle.IsInvalid Then
                Return path ' fallback（极少发生）
            End If

            Dim info As New BY_HANDLE_FILE_INFORMATION

            If Not GetFileInformationByHandle(handle, info) Then
                Return path
            End If

            Dim fileIndex As Long =
                (CLng(info.FileIndexHigh) << 32) Or (info.FileIndexLow And &HFFFFFFFFL)

            Return info.VolumeSerialNumber.ToString() & ":" & fileIndex.ToString()

        End Using

    End Function

#End Region

End Class