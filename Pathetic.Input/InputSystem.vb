Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports NeoVeldrid
Imports Pathetic.Input.Devices

''' <summary>
''' Owns the input devices and updates the enabled contexts once per frame.
''' </summary>
Public NotInheritable Class InputSystem
    Private ReadOnly _devices As New List(Of InputDevice)()
    Private _initialized As Boolean
    Private _hasLastMousePosition As Boolean
    Private _lastMousePosition As Vector2
    Private _mousePosition As Vector2
    Private _mouseDelta As Vector2
    Private _mouseWheelDelta As Single

    Public ReadOnly Property Devices As IReadOnlyList(Of InputDevice)
        Get
            Return _devices
        End Get
    End Property

    Public ReadOnly Property MousePosition As Vector2
        Get
            Return _mousePosition
        End Get
    End Property

    Public ReadOnly Property MouseDelta As Vector2
        Get
            Return _mouseDelta
        End Get
    End Property

    Public ReadOnly Property MouseWheelDelta As Single
        Get
            Return _mouseWheelDelta
        End Get
    End Property

    Public Sub Initialize()
        If _initialized Then
            Return
        End If

        _initialized = True
        _devices.Add(New SdlKeyboardDevice())
        _devices.Add(New SdlMouseDevice())
    End Sub

    ''' <summary>
    ''' Updates devices and actions from the events of the current frame.
    ''' </summary>
    Public Sub Update(snapshot As InputSnapshot)
        ArgumentNullException.ThrowIfNull(snapshot)

        Initialize()

        _mousePosition = snapshot.MousePosition
        _mouseWheelDelta = snapshot.WheelDelta

        If _hasLastMousePosition Then
            _mouseDelta = _mousePosition - _lastMousePosition
        Else
            _mouseDelta = Vector2.Zero
            _hasLastMousePosition = True
        End If

        _lastMousePosition = _mousePosition

        For Each device In _devices
            device.Update(snapshot)
        Next

        For Each context In InputContext.All
            If context.IsEnabled Then
                context.UpdateActions(Me)
            Else
                context.ResetActions()
            End If
        Next
    End Sub

    Public Function TryRead(source As InputSource, ByRef value As Single) As Boolean
        For Each device In _devices
            If device.Platform = source.Platform AndAlso device.TryRead(source, value) Then
                Return True
            End If
        Next

        value = 0.0F
        Return False
    End Function
End Class
