Imports System.Runtime.CompilerServices
Imports Microsoft.Extensions.DependencyInjection

Namespace DI
    Public Module ServiceCollectionExtensions
        <Extension>
        Public Function AddInput(services As IServiceCollection) As IServiceCollection
            services.AddSingleton(Of InputSystem)()
            Return services
        End Function
    End Module
End Namespace
