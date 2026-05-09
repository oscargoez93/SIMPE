' SIMPE Agent Launcher
' Inicia el agente en segundo plano y abre el navegador

Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' Obtener la ruta donde está instalado este script
strPath = FSO.GetParentFolderName(WScript.ScriptFullName)
strExe = FSO.BuildPath(strPath, "SIMPE.Agent.exe")

' Verificar si ya está corriendo
Set WMI = GetObject("winmgmts:")
Set processes = WMI.ExecQuery("SELECT * FROM Win32_Process WHERE Name='SIMPE.Agent.exe'")

If processes.Count = 0 Then
    ' Iniciar el agente
    WshShell.Run """" & strExe & """"", 0, False
    WScript.Sleep 5000
End If

' Abrir el navegador en el dashboard
WshShell.Run "http://localhost:5073", 1, False

Set WshShell = Nothing
Set FSO = Nothing
Set WMI = Nothing
