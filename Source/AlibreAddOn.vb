Imports System.Collections
Imports System.Drawing
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports AlibreAddOn
Imports AlibreX
Imports IronPython.Hosting
Imports Microsoft.Scripting.Hosting

Namespace AlibreAddOnAssembly

    Public Module AlibreAddOn
        Private Property AlibreRoot As IADRoot
        Private _parentWinHandle As IntPtr
        Private _addOnHandle As AddOnRibbon

        Public Sub AddOnLoad(ByVal hwnd As IntPtr, ByVal pAutomationHook As IAutomationHook, ByVal unused As IntPtr)
            AlibreRoot = CType(pAutomationHook.Root, IADRoot)
            _parentWinHandle = hwnd
            _addOnHandle = New AddOnRibbon(AlibreRoot, _parentWinHandle)
        End Sub

        Public Sub AddOnUnload(ByVal hwnd As IntPtr, ByVal forceUnload As Boolean, ByRef cancel As Boolean, ByVal reserved1 As Integer, ByVal reserved2 As Integer)
            _addOnHandle = Nothing
            AlibreRoot = Nothing
        End Sub

        Public Function GetRoot() As IADRoot
            Return AlibreRoot
        End Function

        Public Sub AddOnInvoke(ByVal hwnd As IntPtr, ByVal pAutomationHook As IntPtr, ByVal sessionName As String, ByVal isLicensed As Boolean, ByVal reserved1 As Integer, ByVal reserved2 As Integer)
        End Sub

        Public Function GetAddOnInterface() As IAlibreAddOn
            Return CType(_addOnHandle, IAlibreAddOn)
        End Function
    End Module

    Public Class AddOnRibbon
        Implements IAlibreAddOn

        Public Const ADDON_TITLE As String = "Set Color from Material"
        Private Const SCRIPT_NAME As String = "Set Color from Material V3.0.py"
        Private Const ROOT_ID As Integer = 1200
        Private Const CMD_SET_COLOR As Integer = 1201
        Private Const DIALOG_WATCH_MS As Integer = 600000
        Private Const GA_ROOT As UInteger = 2UI

        Private ReadOnly _alibreRoot As IADRoot
        Private ReadOnly _parentWinHandle As IntPtr
        Private _fallbackForm As Form
        Private _parentForm As Form
        Private _hostBounds As Rectangle = Rectangle.Empty
        Private _scriptRunning As Boolean

        <StructLayout(LayoutKind.Sequential)>
        Private Structure NativeRect
            Public Left As Integer
            Public Top As Integer
            Public Right As Integer
            Public Bottom As Integer
        End Structure

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function GetWindowRect(window As IntPtr, ByRef bounds As NativeRect) As Boolean
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function GetAncestor(window As IntPtr, flags As UInteger) As IntPtr
        End Function

        <DllImport("user32.dll")>
        Private Shared Function IsWindow(window As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function IsWindowVisible(window As IntPtr) As Boolean
        End Function

        Public Sub New(alibreRoot As IADRoot, parentWinHandle As IntPtr)
            _alibreRoot = alibreRoot
            _parentWinHandle = parentWinHandle
        End Sub

        Public Shared ReadOnly Property AddOnDirectory As String
            Get
                Return IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            End Get
        End Property

        Public Shared ReadOnly Property RuntimeFolder As String
            Get
                Return IO.Path.Combine(AddOnDirectory, "scripts")
            End Get
        End Property

        Public Shared ReadOnly Property ScriptPath As String
            Get
                Return IO.Path.Combine(RuntimeFolder, "library", SCRIPT_NAME)
            End Get
        End Property

        Public ReadOnly Property RootMenuItem As Integer Implements IAlibreAddOn.RootMenuItem
            Get
                Return ROOT_ID
            End Get
        End Property

        Public Function HasSubMenus(menuId As Integer) As Boolean Implements IAlibreAddOn.HasSubMenus
            Return menuId = ROOT_ID
        End Function

        Public Function SubMenuItems(menuId As Integer) As Array Implements IAlibreAddOn.SubMenuItems
            If menuId <> ROOT_ID Then Return Nothing
            Return New Integer() {CMD_SET_COLOR}
        End Function

        Public Function MenuItemText(menuId As Integer) As String Implements IAlibreAddOn.MenuItemText
            Select Case menuId
                Case ROOT_ID : Return ADDON_TITLE
                Case CMD_SET_COLOR : Return "Set Color from Material"
                Case Else : Return String.Empty
            End Select
        End Function

        Public Function MenuItemToolTip(menuId As Integer) As String Implements IAlibreAddOn.MenuItemToolTip
            Select Case menuId
                Case ROOT_ID : Return ADDON_TITLE
                Case CMD_SET_COLOR : Return "Choose a material and color the active part to match"
                Case Else : Return String.Empty
            End Select
        End Function

        Public Function MenuItemState(menuId As Integer, sessionIdentifier As String) As ADDONMenuStates Implements IAlibreAddOn.MenuItemState
            Return ADDONMenuStates.ADDON_MENU_ENABLED
        End Function

        Public Function MenuIcon(menuID As Integer) As String Implements IAlibreAddOn.MenuIcon
            If menuID = ROOT_ID Then Return String.Empty
            Return "SetColorFromMaterial.ico"
        End Function

        Public Function PopupMenu(menuId As Integer) As Boolean Implements IAlibreAddOn.PopupMenu
            Return False
        End Function

        <STAThread>
        Public Function InvokeCommand(menuId As Integer, sessionIdentifier As String) As IAlibreAddOnCommand Implements IAlibreAddOn.InvokeCommand
            Try
                If menuId = CMD_SET_COLOR Then RunScript(sessionIdentifier)
            Catch ex As Exception
                Log("InvokeCommand failed: " & ex.ToString())
                MessageBox.Show("The command could not be started." & vbLf & vbLf & ex.Message,
                                ADDON_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
            Return Nothing
        End Function

        Private Sub RunScript(sessionIdentifier As String)
            If Not File.Exists(ScriptPath) Then
                MessageBox.Show("The script file is missing:" & vbLf & ScriptPath,
                                ADDON_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Dim session As IADSession = ResolveSession(sessionIdentifier)
            Dim identifier As String = If(session IsNot Nothing, session.Identifier, sessionIdentifier)
            Dim parent As Form = ResolveParentForm(identifier)
            _parentForm = parent
            _scriptRunning = True
            CenterDialogs()
            Dim runner As New ScriptRunner(_alibreRoot, RuntimeFolder)
            runner.RunInBackground(session, identifier, ScriptPath, parent,
                                   Sub() _scriptRunning = False)
        End Sub

        Private Function WindowBounds(window As IntPtr) As Rectangle
            If window = IntPtr.Zero OrElse Not IsWindow(window) OrElse Not IsWindowVisible(window) Then Return Rectangle.Empty
            Dim root As IntPtr = window
            Try
                Dim ancestor As IntPtr = GetAncestor(window, GA_ROOT)
                If ancestor <> IntPtr.Zero Then root = ancestor
            Catch
            End Try
            Dim native As NativeRect
            If Not GetWindowRect(root, native) Then Return Rectangle.Empty
            Dim bounds As New Rectangle(native.Left, native.Top,
                                        native.Right - native.Left, native.Bottom - native.Top)
            If bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return Rectangle.Empty
            Return bounds
        End Function

        Private Function MainWindowHandle() As IntPtr
            Try
                Using host As Process = Process.GetCurrentProcess()
                    host.Refresh()
                    Return host.MainWindowHandle
                End Using
            Catch ex As Exception
                Log("Main window lookup failed: " & ex.Message)
            End Try
            Return IntPtr.Zero
        End Function

        Private Function MeasureHost() As Rectangle
            Dim candidates As New List(Of IntPtr)()
            Try
                If _parentForm IsNot Nothing AndAlso Not _parentForm.IsDisposed AndAlso
                   _parentForm.IsHandleCreated AndAlso _parentForm IsNot _fallbackForm Then
                    candidates.Add(_parentForm.Handle)
                End If
            Catch
            End Try
            candidates.Add(_parentWinHandle)
            candidates.Add(MainWindowHandle())
            For Each candidate As IntPtr In candidates
                Dim bounds As Rectangle = WindowBounds(candidate)
                If Not bounds.IsEmpty Then Return bounds
            Next
            Log("Alibre's window could not be measured; using the primary screen.")
            Return Screen.PrimaryScreen.WorkingArea
        End Function

        Private Function HostBounds() As Rectangle
            If Not _hostBounds.IsEmpty Then Return _hostBounds
            Return MeasureHost()
        End Function

        Private Function OpenForms() As List(Of Form)
            Dim forms As New List(Of Form)()
            Try
                For Each form As Form In Application.OpenForms
                    forms.Add(form)
                Next
            Catch ex As Exception
                Log("Could not read the open forms: " & ex.Message)
            End Try
            Return forms
        End Function

        Private Sub CenterDialogs()
            _hostBounds = MeasureHost()
            Log("Alibre window measured at " & _hostBounds.ToString() &
                " on " & Screen.FromRectangle(_hostBounds).DeviceName)
            Dim handled As New HashSet(Of IntPtr)()
            For Each form As Form In OpenForms()
                Try
                    handled.Add(form.Handle)
                Catch
                End Try
            Next
            Dim waited As Integer = 0
            Dim watcher As New Windows.Forms.Timer()
            watcher.Interval = 100
            AddHandler watcher.Tick,
                Sub()
                    waited += watcher.Interval
                    Dim running As Boolean = _scriptRunning
                    For Each dialogForm As Form In NewDialogs(handled)
                        Center(dialogForm)
                    Next
                    If running AndAlso waited < DIALOG_WATCH_MS Then Return
                    watcher.Stop()
                    watcher.Dispose()
                End Sub
            watcher.Start()
        End Sub

        Private Function NewDialogs(handled As HashSet(Of IntPtr)) As List(Of Form)
            Dim found As New List(Of Form)()
            For Each candidate As Form In OpenForms()
                Try
                    If candidate Is _fallbackForm Then Continue For
                    If candidate.IsDisposed OrElse Not candidate.Visible Then Continue For
                    If candidate.Width <= 0 OrElse candidate.Height <= 0 Then Continue For
                    If Not handled.Add(candidate.Handle) Then Continue For
                    found.Add(candidate)
                Catch
                End Try
            Next
            Return found
        End Function

        Private Sub Center(target As Form)
            Try
                If target.InvokeRequired Then
                    target.BeginInvoke(New MethodInvoker(Sub() Center(target)))
                    Return
                End If
                Dim host As Rectangle = HostBounds()
                Dim area As Rectangle = Screen.FromRectangle(host).WorkingArea
                host = Rectangle.Intersect(host, area)
                If host.Width <= 0 OrElse host.Height <= 0 Then host = area
                Dim x As Integer = host.Left + (host.Width - target.Width) \ 2
                Dim y As Integer = host.Top + (host.Height - target.Height) \ 2
                x = Math.Max(area.Left, Math.Min(x, area.Right - target.Width))
                y = Math.Max(area.Top, Math.Min(y, area.Bottom - target.Height))
                target.StartPosition = FormStartPosition.Manual
                target.Location = New Point(x, y)
                Log("Centered " & target.Text & " at " & target.Location.ToString() &
                    " on " & Screen.FromRectangle(host).DeviceName & " over " & host.ToString())
            Catch ex As Exception
                Log("Could not center the script dialog: " & ex.Message)
            End Try
        End Sub

        Private Function ResolveSession(sessionIdentifier As String) As IADSession
            If _alibreRoot Is Nothing Then Return Nothing
            Try
                If Not String.IsNullOrEmpty(sessionIdentifier) Then
                    For Each candidate As IADSession In _alibreRoot.Sessions
                        If String.Equals(candidate.Identifier, sessionIdentifier, StringComparison.OrdinalIgnoreCase) Then Return candidate
                    Next
                End If
                For Each candidate As IADSession In _alibreRoot.Sessions
                    Return candidate
                Next
            Catch ex As Exception
                Log("Session lookup failed: " & ex.Message)
            End Try
            Return Nothing
        End Function

        Private Function ResolveParentForm(sessionIdentifier As String) As Form
            Try
                If Not String.IsNullOrEmpty(sessionIdentifier) Then
                    Dim displayed As Form = Global.AlibreScript.API.Windows.GetDisplayedForm(sessionIdentifier)
                    If displayed IsNot Nothing AndAlso Not displayed.IsDisposed Then Return displayed
                End If
            Catch ex As Exception
                Log("GetDisplayedForm failed: " & ex.Message)
            End Try
            Try
                Dim owner As Form = TryCast(Control.FromHandle(_parentWinHandle), Form)
                If owner IsNot Nothing AndAlso Not owner.IsDisposed Then Return owner
            Catch ex As Exception
                Log("Control.FromHandle failed: " & ex.Message)
            End Try
            Dim host As Rectangle = MeasureHost()
            If _fallbackForm IsNot Nothing AndAlso Not _fallbackForm.IsDisposed Then
                Try
                    _fallbackForm.Bounds = host
                Catch
                End Try
            End If
            If _fallbackForm Is Nothing OrElse _fallbackForm.IsDisposed Then
                _fallbackForm = New Form() With {
                    .Text = ADDON_TITLE,
                    .ShowInTaskbar = False,
                    .FormBorderStyle = FormBorderStyle.None,
                    .StartPosition = FormStartPosition.Manual,
                    .Bounds = host}
                Dim forceHandle As IntPtr = _fallbackForm.Handle
            End If
            Return _fallbackForm
        End Function

        Public Shared ReadOnly Property LogPath As String
            Get
                Return IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Alibre AddOns", "SetColorFromMaterial", "addon.log")
            End Get
        End Property

        Public Shared Sub Log(message As String)
            Try
                Dim folder As String = IO.Path.GetDirectoryName(LogPath)
                If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)
                File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "  " & message & Environment.NewLine)
            Catch
            End Try
        End Sub

        Public Function HasPersistentDataToSave(sessionIdentifier As String) As Boolean Implements IAlibreAddOn.HasPersistentDataToSave
            Return False
        End Function

        Public Sub SaveData(pCustomData As IStream, sessionIdentifier As String) Implements IAlibreAddOn.SaveData
        End Sub

        Public Sub LoadData(pCustomData As IStream, sessionIdentifier As String) Implements IAlibreAddOn.LoadData
        End Sub

        Public Function UseDedicatedRibbonTab() As Boolean Implements IAlibreAddOn.UseDedicatedRibbonTab
            Return False
        End Function

        Private Sub IAlibreAddOn_setIsAddOnLicensed(isLicensed As Boolean) Implements IAlibreAddOn.setIsAddOnLicensed
        End Sub
    End Class

    Public Class ScriptRunner
        Private ReadOnly _alibreRoot As IADRoot
        Private ReadOnly _runtimeFolder As String

        Public Sub New(alibreRoot As IADRoot, runtimeFolder As String)
            _alibreRoot = alibreRoot
            _runtimeFolder = runtimeFolder
        End Sub

        Public Shared ReadOnly Property AlibreProgramFolder As String
            Get
                Return IO.Path.GetDirectoryName(Assembly.GetAssembly(GetType(IADRoot)).Location)
            End Get
        End Property

        Public Shared ReadOnly Property AlibreScriptFolder As String
            Get
                Return IO.Path.Combine(AlibreProgramFolder, "Addons", "AlibreScript")
            End Get
        End Property

        Public Sub RunInBackground(session As IADSession, sessionIdentifier As String, scriptPath As String,
                                   parent As Form, completed As Action)
            Dim worker As New Thread(Sub() Run(session, sessionIdentifier, scriptPath, parent, completed))
            worker.SetApartmentState(ApartmentState.STA)
            worker.IsBackground = True
            worker.Name = "AlibreScript: " & IO.Path.GetFileName(scriptPath)
            worker.Start()
        End Sub

        Private Sub Run(session As IADSession, sessionIdentifier As String, scriptPath As String,
                        parent As Form, completed As Action)
            Dim engine As ScriptEngine = Nothing
            Dim scope As ScriptScope = Nothing
            Try
                Dim bootstrapPath As String = IO.Path.Combine(_runtimeFolder, "bootstrap.py")
                If Not File.Exists(bootstrapPath) Then
                    ShowError(parent, "The add-on runtime is incomplete. Missing:" & vbLf & bootstrapPath)
                    Return
                End If

                engine = CreateEngine()
                scope = engine.CreateScope()
                scope.SetVariable("AS_Root", _alibreRoot)
                scope.SetVariable("AS_Session", session)
                scope.SetVariable("AS_SessionIdentifier", If(sessionIdentifier, String.Empty))
                scope.SetVariable("AS_ScriptPath", scriptPath)
                scope.SetVariable("AS_ScriptFolder", IO.Path.GetDirectoryName(scriptPath))
                scope.SetVariable("AS_RuntimeFolder", _runtimeFolder)
                scope.SetVariable("AS_AlibreProgramFolder", AlibreProgramFolder)
                scope.SetVariable("AS_AlibreScriptFolder", AlibreScriptFolder)
                scope.SetVariable("AS_ParentForm", parent)
                scope.SetVariable("AS_Arguments", New List(Of String)())
                scope.SetVariable("AS_Log", New Action(Of String)(AddressOf AddOnRibbon.Log))

                AddOnRibbon.Log("Running " & scriptPath)
                engine.ExecuteFile(bootstrapPath, scope)
                engine.ExecuteFile(scriptPath, scope)
                ReportDeferred(engine, scope, parent)
                AddOnRibbon.Log("Finished " & scriptPath)
            Catch ex As Exception
                If ex.GetType().FullName = "IronPython.Runtime.Exceptions.SystemExitException" Then
                    ReportDeferred(engine, scope, parent)
                    AddOnRibbon.Log("Script exited: " & scriptPath)
                    Return
                End If
                Dim details As String = Describe(engine, ex)
                AddOnRibbon.Log("Script error in " & scriptPath & vbLf & details)
                ShowError(parent, IO.Path.GetFileName(scriptPath) & " stopped with an error." & vbLf & vbLf &
                                  details & vbLf & vbLf & "Log: " & AddOnRibbon.LogPath)
            Finally
                If completed IsNot Nothing Then
                    Try
                        completed()
                    Catch ex As Exception
                        AddOnRibbon.Log("Completion callback failed: " & ex.Message)
                    End Try
                End If
            End Try
        End Sub

        Private Sub ReportDeferred(engine As ScriptEngine, scope As ScriptScope, parent As Form)
            If engine Is Nothing OrElse scope Is Nothing Then Return
            Try
                Dim taker As Object = Nothing
                If Not scope.TryGetVariable(Of Object)("AS_TakeDeferred", taker) OrElse taker Is Nothing Then Return
                Dim queued As Object = engine.Operations.Invoke(taker, New Object() {})
                Dim messages As IEnumerable = TryCast(queued, IEnumerable)
                If messages Is Nothing Then Return
                For Each entry As Object In messages
                    Dim fields As IList = TryCast(entry, IList)
                    If fields Is Nothing OrElse fields.Count < 2 Then Continue For
                    Dim title As String = Convert.ToString(fields(0))
                    Dim text As String = Convert.ToString(fields(1))
                    Dim isError As Boolean = fields.Count > 2 AndAlso Convert.ToBoolean(fields(2))
                    ShowMessage(parent, title, text, isError)
                Next
            Catch ex As Exception
                AddOnRibbon.Log("Deferred report failed: " & ex.Message)
            End Try
        End Sub

        Private Sub ShowMessage(parent As Form, title As String, text As String, isError As Boolean)
            Dim icon As MessageBoxIcon = If(isError, MessageBoxIcon.Warning, MessageBoxIcon.Information)
            AddOnRibbon.Log("Showing " & title)
            Try
                If parent IsNot Nothing AndAlso Not parent.IsDisposed AndAlso parent.IsHandleCreated Then
                    Dim show As MethodInvoker =
                        Sub()
                            parent.Activate()
                            MessageBox.Show(parent, text, title, MessageBoxButtons.OK, icon)
                        End Sub
                    If parent.InvokeRequired Then
                        parent.Invoke(show)
                    Else
                        show()
                    End If
                    Return
                End If
            Catch ex As Exception
                AddOnRibbon.Log("Owned dialog failed: " & ex.Message)
            End Try
            MessageBox.Show(text, title, MessageBoxButtons.OK, icon,
                            MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
        End Sub

        Private Function CreateEngine() As ScriptEngine
            Dim engine As ScriptEngine = Python.CreateEngine()
            Dim scriptFolder As String = AlibreScriptFolder
            Dim searchPaths = engine.GetSearchPaths()
            searchPaths.Add(IO.Path.Combine(scriptFolder, "PythonLib"))
            searchPaths.Add(IO.Path.Combine(scriptFolder, "PythonLib", "site-packages"))
            searchPaths.Add(scriptFolder)
            searchPaths.Add(_runtimeFolder)
            engine.SetSearchPaths(searchPaths)
            Return engine
        End Function

        Private Function Describe(engine As ScriptEngine, ex As Exception) As String
            If engine IsNot Nothing Then
                Try
                    Return engine.GetService(Of ExceptionOperations)().FormatException(ex)
                Catch
                End Try
            End If
            Return ex.ToString()
        End Function

        Private Sub ShowError(parent As Form, message As String)
            Try
                If parent IsNot Nothing AndAlso Not parent.IsDisposed AndAlso parent.IsHandleCreated AndAlso parent.InvokeRequired Then
                    parent.Invoke(New MethodInvoker(Sub() MessageBox.Show(parent, message, AddOnRibbon.ADDON_TITLE,
                                                                          MessageBoxButtons.OK, MessageBoxIcon.Error)))
                    Return
                End If
            Catch
            End Try
            MessageBox.Show(message, AddOnRibbon.ADDON_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub
    End Class

End Namespace
