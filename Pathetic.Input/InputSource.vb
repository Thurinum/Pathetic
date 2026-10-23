Imports System

''' <summary>
''' Identifies a control emitted by a device, for example <c>Keyboard/Space</c> or <c>Mouse/Right</c>.
''' </summary>
Public Structure InputSource
    Private ReadOnly _platform As InputPlatform
    Private ReadOnly _control As String

    Public Sub New(platform As InputPlatform, control As String)
        If String.IsNullOrWhiteSpace(control) Then
            Throw New ArgumentException("An input control name is required.", NameOf(control))
        End If

        _platform = platform
        _control = control
    End Sub

    Public ReadOnly Property Platform As InputPlatform
        Get
            Return _platform
        End Get
    End Property

    Public ReadOnly Property Control As String
        Get
            Return _control
        End Get
    End Property

    ''' <summary>
    ''' Parses a binding path in the form <c>Platform/Control</c>.
    ''' </summary>
    Public Shared Function Parse(path As String) As InputSource
        If String.IsNullOrWhiteSpace(path) Then
            Throw New ArgumentException("An input source path is required.", NameOf(path))
        End If

        Dim separator = path.IndexOf("/"c)
        If separator <= 0 OrElse separator = path.Length - 1 Then
            Throw New FormatException($"Input source '{path}' must use the form 'Platform/Control'.")
        End If

        Dim platformName = path.Substring(0, separator)
        Dim platform As InputPlatform
        If Not [Enum].TryParse(Of InputPlatform)(platformName, True, platform) Then
            Throw New FormatException($"Unknown input platform '{platformName}' in source '{path}'.")
        End If

        Return New InputSource(platform, path.Substring(separator + 1))
    End Function

    Public Overrides Function ToString() As String
        Return $"{Platform}/{Control}"
    End Function
End Structure
