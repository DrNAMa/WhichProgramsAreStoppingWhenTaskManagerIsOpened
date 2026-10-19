Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Module Module1
    Dim trackedProcesses As New ConcurrentDictionary(Of Integer, String)()
    Sub Main()
        Using cts As New CancellationTokenSource()
            Dim monitorTask = Task.Run(Function() MonitorAllProcesses(cts.Token))
            Console.WriteLine("Now monitoring ALL system processes. Press Enter to stop.")
            Console.ReadLine()
            cts.Cancel()
            Try
                monitorTask.Wait()
            Catch ex As AggregateException
            End Try
            Console.WriteLine(vbCrLf & "Monitoring safely shut down.")
        End Using
    End Sub
    Private Async Function MonitorAllProcesses(token As CancellationToken) As Task
        Try
            Dim initialProcesses = Process.GetProcesses()
            For Each p In initialProcesses
                trackedProcesses.TryAdd(p.Id, p.ProcessName)
                p.Dispose()
            Next
            While Not token.IsCancellationRequested
                Dim currentProcesses = Process.GetProcesses()
                Dim currentPids As New HashSet(Of Integer)()
                For Each p In currentProcesses
                    currentPids.Add(p.Id)
                    If Not trackedProcesses.ContainsKey(p.Id) Then
                        trackedProcesses.TryAdd(p.Id, p.ProcessName)
                    End If
                    p.Dispose()
                Next
                Dim closedPids As New List(Of Integer)()
                For Each kvp In trackedProcesses
                    If Not currentPids.Contains(kvp.Key) Then
                        Console.WriteLine($"PID: {kvp.Key}, Process Name: {kvp.Value} stopped running.")
                        closedPids.Add(kvp.Key)
                    End If
                Next
                For Each pid In closedPids
                    Dim removedName As String = Nothing
                    trackedProcesses.TryRemove(pid, removedName)
                Next
                Await Task.Delay(1000, token)
            End While
        Catch ex As TaskCanceledException
        Catch ex As Exception
            Console.WriteLine($"Critical Monitor Error: {ex.Message}")
        End Try
    End Function
End Module