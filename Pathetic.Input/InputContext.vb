Imports System
Imports System.Collections.Generic

''' <summary>
''' A set of actions that can be enabled or disabled independently, like Unity's action maps.
''' Several contexts can be enabled at the same time.
''' </summary>
Public NotInheritable Class InputContext
    Private Shared ReadOnly s_all As New List(Of InputContext)()

    Private ReadOnly _actions As InputAction()
    Private ReadOnly _actionsView As IReadOnlyList(Of InputAction)
    Private _enabled As Boolean

    Public Sub New(name As String, ParamArray actions As InputAction())
        If String.IsNullOrWhiteSpace(name) Then
            Throw New ArgumentException("An input context name is required.", NameOf(name))
        End If

        If actions Is Nothing OrElse actions.Length = 0 Then
            Throw New ArgumentException("An input context needs at least one action.", NameOf(actions))
        End If

        Me.Name = name
        _actions = actions
        _actionsView = Array.AsReadOnly(_actions)
        s_all.Add(Me)
    End Sub

    Public ReadOnly Property Name As String

    Public ReadOnly Property Actions As IReadOnlyList(Of InputAction)
        Get
            Return _actionsView
        End Get
    End Property

    Public ReadOnly Property IsEnabled As Boolean
        Get
            Return _enabled
        End Get
    End Property

    Friend Shared ReadOnly Property All As IReadOnlyList(Of InputContext)
        Get
            Return s_all
        End Get
    End Property

    Public Sub Enable()
        _enabled = True
    End Sub

    Public Sub Disable()
        _enabled = False
        ResetActions()
    End Sub

    Friend Sub UpdateActions(input As InputSystem)
        For Each action In _actions
            action.UpdateState(input)
        Next
    End Sub

    Friend Sub ResetActions()
        For Each action In _actions
            action.ResetState()
        Next
    End Sub
End Class
