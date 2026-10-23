Imports System
Imports System.Collections.Generic
Imports NeoVeldrid
Imports SdlKey = NeoVeldrid.Key

Namespace Devices
    ''' <summary>
    ''' Translates SDL keyboard events into <c>Keyboard/&lt;KeyName&gt;</c> sources.
    ''' </summary>
    Public NotInheritable Class SdlKeyboardDevice
        Inherits InputDevice

        Private ReadOnly _down As New HashSet(Of SdlKey)()
        Private ReadOnly _pressedThisFrame As New HashSet(Of SdlKey)()
        Private ReadOnly _releasedThisFrame As New HashSet(Of SdlKey)()

        Public Overrides ReadOnly Property Name As String
            Get
                Return "SDL Keyboard"
            End Get
        End Property

        Public Overrides ReadOnly Property Platform As InputPlatform
            Get
                Return InputPlatform.Keyboard
            End Get
        End Property

        Public Overrides Sub Update(snapshot As InputSnapshot)
            _pressedThisFrame.Clear()
            _releasedThisFrame.Clear()

            For Each keyEvent In snapshot.KeyEvents
                If keyEvent.Down Then
                    If _down.Add(keyEvent.[Key]) AndAlso Not keyEvent.Repeat Then
                        _pressedThisFrame.Add(keyEvent.[Key])
                    End If
                Else
                    If _down.Remove(keyEvent.[Key]) Then
                        _releasedThisFrame.Add(keyEvent.[Key])
                    End If
                End If
            Next
        End Sub

        Public Overrides Function TryRead(source As InputSource, ByRef value As Single) As Boolean
            Dim key As SdlKey
            If Not [Enum].TryParse(Of SdlKey)(source.Control, True, key) Then
                Return False
            End If

            value = If(_down.Contains(key), 1.0F, 0.0F)
            Return True
        End Function

        Public Function IsKeyDown(key As SdlKey) As Boolean
            Return _down.Contains(key)
        End Function

        Public Function WasKeyPressedThisFrame(key As SdlKey) As Boolean
            Return _pressedThisFrame.Contains(key)
        End Function

        Public Function WasKeyReleasedThisFrame(key As SdlKey) As Boolean
            Return _releasedThisFrame.Contains(key)
        End Function
    End Class
End Namespace
