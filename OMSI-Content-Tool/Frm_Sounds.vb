﻿'by Felix Modellbusse ;) (MoBu) 2019
Public Class Frm_Sounds
    Private Sub Frm_Sounds_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Location = New Point(Frm_Main.Width / 2 - Me.Width / 2, Frm_Main.Height / 2 - Me.Height / 2)

        LBGruppen.Items.Clear()
        LBDateien.Items.Clear()

        If Frm_Main.getProjTyp = Frm_Main.PROJ_TYPE_BUS Then
            For i As Integer = 0 To Frm_Main.getProj.paths.soundpacks.count - 1
                LBGruppen.Items.Add(i)
            Next
        End If
    End Sub

    Private Sub LBGruppen_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LBGruppen.SelectedIndexChanged
        LBDateien.Items.Clear()
        LBDateien.Items.AddRange(Frm_Main.getProj.paths.soundpacks(LBGruppen.SelectedIndex).toarray)
    End Sub
End Class