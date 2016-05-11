' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicEditorFactoryIdString)>
    Friend Class VisualBasicEditorFactory
        Inherits AbstractEditorFactory

        Public Sub New(package As VisualBasicPackage)
            MyBase.New(package)
        End Sub

        Protected Overrides ReadOnly Property ContentTypeName As String
            Get
                Return "Basic"
            End Get
        End Property
    End Class
End Namespace
