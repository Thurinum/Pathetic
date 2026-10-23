Imports System
Imports System.Collections.Generic
Imports NeoVeldrid

Namespace Devices
    ''' <summary>
    ''' Translates SDL mouse button events into <c>Mouse/Left</c>, <c>Mouse/Middle</c> and <c>Mouse/Right</c> sources.
    ''' Position, delta and wheel deltas are exposed by <see cref="InputSystem"/>.
    ''' </summary>
    Public NotInheritable Class SdlMouseDevice
        Inherits InputDevice

        Private ReadOnly _down As New HashSet(Of MouseButton)()

        Public Overrides ReadOnly Property Name As String
            Get
                Return "SDL Mouse"
            End Get
        End Property

        Public Overrides ReadOnly Property Platform As InputPlatform
            Get
                Return InputPlatform.Mouse
            End Get
        End Property

        Public Overrides Sub Update(snapshot As InputSnapshot)
            For Each mouseEvent In snapshot.MouseEvents
                If mouseEvent.Down Then
                    _down.Add(mouseEvent.MouseButton)
                Else
                    _down.Remove(mouseEvent.MouseButton)
                End If
            Next
        End Sub

        Public Overrides Function TryRead(source As InputSource, ByRef value As Single) As Boolean
            Dim button As MouseButton
            If Not TryParseButton(source.Control, button) Then
                Return False
            End If

            value = If(_down.Contains(button), 1.0F, 0.0F)
            Return True
        End Function

        Public Function IsButtonDown(button As MouseButton) As Boolean
            Return _down.Contains(button)
        End Function

        Private Shared Function TryParseButton(control As String, ByRef button As MouseButton) As Boolean
            Select Case control.ToLowerInvariant()
                Case "left"
                    button = MouseButton.Left
                    Return True
                Case "middle"
                    button = MouseButton.Middle
                    Return True
                Case "right"
                    button = MouseButton.Right
                    Return True
                Case Else
                    button = MouseButton.Left
                    Return False
            End Select
        End Function
    End Class
End Namespace
