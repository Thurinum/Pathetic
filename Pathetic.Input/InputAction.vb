Imports System
Imports System.Collections.Generic
Imports System.Linq

''' <summary>
''' A named action bound to one or more <see cref="InputSource"/> paths.
''' </summary>
Public NotInheritable Class InputAction
    Private ReadOnly _sources As InputSource()
    Private ReadOnly _sourcesView As IReadOnlyList(Of InputSource)
    Private _value As Single
    Private _pressed As Boolean
    Private _wasPressedThisFrame As Boolean
    Private _wasReleasedThisFrame As Boolean

    Public Sub New(name As String, actionType As InputActionType, ParamArray bindings As String())
        If String.IsNullOrWhiteSpace(name) Then
            Throw New ArgumentException("An input action name is required.", NameOf(name))
        End If

        If bindings Is Nothing OrElse bindings.Length = 0 Then
            Throw New ArgumentException("An input action needs at least one binding.", NameOf(bindings))
        End If

        Me.Name = name
        Me.ActionType = actionType
        _sources = bindings.Select(Function(binding) InputSource.Parse(binding)).ToArray()
        _sourcesView = Array.AsReadOnly(_sources)
    End Sub

    Public ReadOnly Property Name As String

    Public ReadOnly Property ActionType As InputActionType

    Public ReadOnly Property Sources As IReadOnlyList(Of InputSource)
        Get
            Return _sourcesView
        End Get
    End Property

    Public ReadOnly Property Value As Single
        Get
            Return _value
        End Get
    End Property

    Public ReadOnly Property IsPressed As Boolean
        Get
            Return _pressed
        End Get
    End Property

    Public ReadOnly Property WasPressedThisFrame As Boolean
        Get
            Return _wasPressedThisFrame
        End Get
    End Property

    Public ReadOnly Property WasReleasedThisFrame As Boolean
        Get
            Return _wasReleasedThisFrame
        End Get
    End Property

    Public Event Started As Action(Of InputAction)

    Public Event Performed As Action(Of InputAction)

    Public Event Canceled As Action(Of InputAction)

    Public Function ReadValue() As Single
        Return _value
    End Function

    Friend Sub UpdateState(input As InputSystem)
        Dim nextValue As Single = 0.0F

        For Each source In _sources
            Dim candidate As Single
            If input.TryRead(source, candidate) AndAlso Math.Abs(candidate) > Math.Abs(nextValue) Then
                nextValue = candidate
            End If
        Next

        _value = nextValue

        Dim pressedNow = Math.Abs(nextValue) >= 0.5F
        _wasPressedThisFrame = pressedNow AndAlso Not _pressed
        _wasReleasedThisFrame = (Not pressedNow) AndAlso _pressed
        _pressed = pressedNow

        If _wasPressedThisFrame Then
            RaiseEvent Started(Me)
            RaiseEvent Performed(Me)
        ElseIf _wasReleasedThisFrame Then
            RaiseEvent Canceled(Me)
        End If
    End Sub

    Friend Sub ResetState()
        _value = 0.0F
        _pressed = False
        _wasPressedThisFrame = False
        _wasReleasedThisFrame = False
    End Sub
End Class
