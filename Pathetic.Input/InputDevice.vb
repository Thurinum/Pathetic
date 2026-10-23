Imports NeoVeldrid

''' <summary>
''' Source of input for one platform or backend. Devices emit <see cref="InputSource"/> values.
''' </summary>
Public MustInherit Class InputDevice
    Public MustOverride ReadOnly Property Name As String

    Public MustOverride ReadOnly Property Platform As InputPlatform

    ''' <summary>
    ''' Consumes the events of the current frame.
    ''' </summary>
    Public MustOverride Sub Update(snapshot As InputSnapshot)

    ''' <summary>
    ''' Reads the current scalar value of a source. Returns <see langword="False"/> when the control is unknown.
    ''' </summary>
    Public MustOverride Function TryRead(source As InputSource, ByRef value As Single) As Boolean
End Class
