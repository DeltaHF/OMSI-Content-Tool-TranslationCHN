﻿'by Felix Modellbusse ;) (MoBu) 2019
Imports System.Xml
Imports System.IO

Module Importer
    Public ReadOnly KNOWN_FILE_TYPES As String() = {"o3d", "x", "x3d", "sli", "txt"}
    Public stopImport As Boolean = False


    Public Function readFile(filename As Filename)
        stopImport = False
        If checkIfExist(filename) Then
            Return Nothing
        End If

        Frm_Main.SSLBStatus.Text = "Import erfolgreich!"

        Select Case filename.extension
            Case KNOWN_FILE_TYPES(0)
                Return readO3D(filename)
            Case KNOWN_FILE_TYPES(1)
                Return readX(filename)
            Case KNOWN_FILE_TYPES(2)
                Return readX3D(filename)
            Case KNOWN_FILE_TYPES(3)
                Return readSli(filename)
            Case KNOWN_FILE_TYPES(4)
                Return readtxt(filename)
            Case Else
                Log.Add("Dateiformat nicht unterstützt! (Fehler: I000, Datei: " & filename & ")", Log.TYPE_WARNUNG)
                importWarnung("Dateiformat nicht unterstützt!", "I999", filename, "Dateiformat nicht unterstützt!")
                Return Nothing
        End Select

    End Function

    Private Function readtxt(filename As Filename) As OMSI_Mesh
        MsgBox("Das Laden von Mesh-Eigenschaften wird noch nicht unterstützt!")
        Return Nothing
    End Function

    Private Sub importWarnung(msg As String, fehler As String, filename As String, grund As String)
        If Not ignoreImportFail Then
            Dim result = MsgBox(msg & vbCrLf & "(Feher: " & fehler & ", Datei: " & filename & ") " & grund, vbAbortRetryIgnore)
            If result = vbIgnore Then ignoreImportFail = True
            If result = vbAbort Then stopImport = True
        End If
    End Sub

    Private Function readX3D(filename As Filename) As Local3DObjekt
        Dim document As New XmlDocument

        Try
            document.Load(filename)
        Catch ex As Exception
            Log.Add("Import fehlgeschlagen! (Fehler: I003, Datei: " & filename & ")", Log.TYPE_ERROR)
            importWarnung("Import fehlgeschlagen!", "I003", filename, "falsches Dateiformat oder bschädigte Datei")
            Frm_Main.SSLBStatus.Text = ""
            Return Nothing
        End Try

        Dim temp3D As New Local3DObjekt
        Dim tempArr() As String
        Dim tempListI As New List(Of Integer)
        Dim tempTex As New List(Of Double)
        Dim tempTexCoord As New List(Of Double)
        Dim tempVert As New List(Of Double)
        Dim indexOffset As Integer = 0

        Dim ersterKameraFehler As Boolean = True

        For Each mainNodes As XmlElement In document.DocumentElement.ChildNodes
            If mainNodes.Name = "Scene" Then
                For Each transformNodes In mainNodes.ChildNodes
                    If transformNodes.name = "Transform" Then
                        tempArr = Split(Replace(transformNodes.getAttribute("translation"), ".", ","), " ")
                        temp3D.center = New Point3D(tempArr(0), tempArr(2), tempArr(1))
                        If Not transformNodes.firstchild.name = "Viewpoint" Then

                            For Each groupmember In transformNodes.firstchild.firstchild.childnodes
                                For Each shape In groupmember.childnodes
                                    If shape.firstchild.name = "ImageTexture" Then
                                        Dim texture As New LocalTexture
                                        If shape.firstchild.getattribute("USE") = "" Then
                                            texture.matName = shape.firstchild.attributes(0).value
                                            texture.filename = New Filename(Split(shape.firstchild.attributes(1).value, """ """)(1))
                                        Else
                                            For Each texture2 In temp3D.texturen
                                                If texture2.matName = shape.firstchild.attributes(0).value Then
                                                    texture = texture2
                                                End If
                                            Next
                                            If texture.filename.name = "" Then
                                                Log.Add("Textureabhängigkeit konnte nicht geladen werden! (Datei: " & filename & ", Texture: " & shape.firstchild.getattribute("USE") & ")", Log.TYPE_ERROR)
                                            End If
                                        End If

                                        temp3D.texturen.Add(texture)
                                    End If

                                    If shape.name = "IndexedFaceSet" Then

                                        tempArr = Split(shape.getattribute("texCoordIndex"), " ")
                                        If tempArr(3) = -1 Then
                                            For i = 0 To tempArr.Count - 4 Step 4
                                                If tempArr(i) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i), ".", ",")))
                                                If tempArr(i + 1) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 1), ".", ",")))
                                                If tempArr(i + 2) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 2), ".", ",")))
                                            Next
                                        Else
                                            For i = 0 To tempArr.Count - 5 Step 5
                                                If tempArr(i) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i), ".", ",")))
                                                If tempArr(i + 1) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 1), ".", ",")))
                                                If tempArr(i + 2) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 2), ".", ",")))
                                                'If tempArr(i + 2) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 2), ".", ",")))
                                                If tempArr(i + 3) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i + 3), ".", ",")))
                                                'If tempArr(i) <> "" Then tempTex.Add(Convert.ToInt32(Replace(tempArr(i), ".", ",")))
                                            Next
                                        End If



                                        tempArr = Split(shape.getattribute("coordIndex"), " ")
                                        For i = 0 To tempArr.Count - 5 Step 5
                                            If tempArr(i) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i)) + indexOffset)
                                            If tempArr(i + 1) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i + 1)) + indexOffset)
                                            If tempArr(i + 2) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i + 2)) + indexOffset)
                                            If tempArr(i + 2) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i + 2)) + indexOffset)
                                            If tempArr(i + 3) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i + 3)) + indexOffset)
                                            If tempArr(i) <> "" Then tempListI.Add(Convert.ToInt32(tempArr(i)) + indexOffset)
                                        Next
                                        temp3D.subObjekte.Add(tempListI.ToArray)


                                        Dim xyz As Integer = 0
                                        For Each value In Split(Replace(shape.firstchild.getattribute("point"), ".", ","), " ")
                                            Dim tmpz As Double
                                            If value <> "" Then
                                                Select Case xyz
                                                    Case 0
                                                        tempVert.Add(Convert.ToDouble(value) + temp3D.center.X)
                                                    Case 1
                                                        tmpz = Convert.ToDouble(value) + temp3D.center.Y
                                                    Case 2
                                                        tempVert.Add(Convert.ToDouble(value) + temp3D.center.Z)
                                                        tempVert.Add(tmpz)
                                                End Select

                                            End If

                                            xyz += 1
                                            If xyz = 3 Then xyz = 0
                                        Next
                                        indexOffset = tempVert.Count

                                        For Each ChildNode In shape.ChildNodes
                                            If ChildNode.name = "TextureCoordinate" Then
                                                For Each value In Split(Replace(ChildNode.getattribute("point"), ".", ","), " ")
                                                    If value <> "" Then tempTexCoord.Add(value)
                                                Next
                                            End If
                                        Next
                                    End If
                                Next
                            Next
                        Else
                            If Frm_Main.getProjTyp = Frm_Main.PROJ_TYPE_BUS Then
                                Dim newCam As New Kamera
                                With transformNodes
                                    newCam.position = New Point3D(toSingle(.firstchild.getattribute("translation").split(" ")(0)), toSingle(.firstchild.getattribute("translation").split(" ")(0)), toSingle(.firstchild.getattribute("translation").split(" ")(0)))
                                    newCam.position.move(New Point3D(toSingle(.firstchild.getattribute("position").split(" ")(0)), toSingle(.firstchild.getattribute("position").split(" ")(0)), toSingle(.firstchild.getattribute("position").split(" ")(0))))
                                    newCam.rotX = toSingle(.firstchild.getattribute("orientation").split(" ")(0))
                                    newCam.rotY = toSingle(.firstchild.getattribute("orientation").split(" ")(1))
                                End With
                                Frm_Main.getProj.model.dirver_cam_list.add()
                            Else
                                If ersterKameraFehler Then
                                    MsgBox("Kameras können nur zu Bussen hinzugefügt / importiert werden!")
                                    ersterKameraFehler = False
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        Next


        'Hier weiter machen!
        Dim tmpList As New List(Of Double)
        For texCoordIndex As Integer = 0 To tempListI.Count - 1 Step 2
            tmpList.Add(tempTexCoord(tempTex(tempListI(texCoordIndex)) * 2))
            tmpList.Add(tempTexCoord(tempTex(tempListI(texCoordIndex)) * 2 + 1))
        Next

        temp3D.texCoords = tmpList.ToArray
        temp3D.vertices = tempVert.ToArray


        'Lines erzeugen
        Dim linesTemp As New List(Of Integer)
        For Each subObj In temp3D.subObjekte
            linesTemp.AddRange(subObj)
        Next
        temp3D.lines = faceToLines(linesTemp).ToArray

        Log.Add("Import erfolgreich! (Datei:" & filename.name & ", Format: X3D)")
        Return temp3D
    End Function

    Private Function faceToLines(faces As List(Of Integer)) As List(Of Integer)
        faceToLines = New List(Of Integer)
        For i = 0 To faces.Count - 1 Step 3
            faceToLines.AddRange({faces(i), faces(i + 1)})
            faceToLines.AddRange({faces(i + 1), faces(i + 2)})
            faceToLines.AddRange({faces(i + 2), faces(i + 1)})
        Next
    End Function

    Dim ignoreImportFail As Boolean = False
    Private Function readO3D(filename As Filename) As Local3DObjekt

        If checkIfExist(filename) Then
            Return Nothing
        End If

        Dim bytes As Byte() = My.Computer.FileSystem.ReadAllBytes(filename)

        If bytes.Count < 5 Then                                                         'Datei leer
            Log.Add("Import fehlgeschlagen! (Feher: I000a, Datei: " & filename & ")", Log.TYPE_ERROR)
            If Not ignoreImportFail Then
                Dim result = MsgBox("Import fehlgeschlagen!" & vbCrLf & "(Feher: I000a, Datei: " & filename & ") Datei leer!", vbAbortRetryIgnore)
                If result = vbIgnore Then ignoreImportFail = True
            End If
            Frm_Main.SSLBStatus.Text = "Import fehlgeschlagen!"
            Return Nothing
        End If


        If bytes(0) = &H84 And bytes(1) = &H19 And bytes(2) = &H5 And bytes(3) = &H0 Then       'Verschlüsselte Datei
            Log.Add("Import fehlgeschlagen! (Fehler: I002a, Datei: " & filename & ")", Log.TYPE_ERROR)
            If Not ignoreImportFail Then
                Dim result = MsgBox("Import fehlgeschlagen!" & vbCrLf & "(Fehler: I002a, Datei: " & filename & ") verschlüsselte Datei!", vbAbortRetryIgnore)
                If result = vbIgnore Then ignoreImportFail = True
            End If
            Frm_Main.SSLBStatus.Text = "Import fehlgeschlagen!"
            Return Nothing
        End If


        Dim isAddon As Boolean = False
        Dim addonOffset As Integer = 0
        If bytes(0) = &H84 And bytes(1) = &H19 And bytes(2) = &H5 Then                  'Originale und Addon-O3D
            isAddon = True
            If bytes(4) = &HFF And bytes(5) = &HFF And bytes(6) = &HFF And bytes(7) = &HFF Then
                Log.Add("O3D '" & filename.name & "' aus Addon ohne ArtNr")
            Else
                Log.Add("O3D '" & filename.name & "' aus Addon: " & bytes(4) + bytes(5) * 256 + bytes(6) * 65536)
            End If
            addonOffset = 7
        End If


        If Not isAddon Then
            If bytes(0) <> &H84 Or bytes(1) <> &H19 Or bytes(2) <> &H1 Or bytes(3) <> &H17 Then             'Normale O3D
                Log.Add("Import fehlgeschlagen! (Fehler: I002, Datei: " & filename & ")", Log.TYPE_ERROR)
                If Not ignoreImportFail Then
                    Dim result = MsgBox("Import fehlgeschlagen!" & vbCrLf & "(Fehler: I002, Datei: " & filename & ") falsches Format", vbAbortRetryIgnore)
                    If result = vbIgnore Then ignoreImportFail = True
                End If
                Frm_Main.SSLBStatus.Text = "Import fehlgeschlagen!"
                Return Nothing
            End If
        End If

        Dim temp3D As New Local3DObjekt

        Dim verticesTemp As New List(Of Double)
        Dim normalsTemp As New List(Of Double)
        Dim texCoordsTemp As New List(Of Double)
        Dim facesTemp As New List(Of Integer)
        Dim matlistTemp As New List(Of Integer)
        Dim textureTemp As New List(Of Integer)
        Dim linesTemp As New List(Of Integer)


        temp3D.center = New Point3D

        Dim ctMesh As Integer
        If isAddon Then
            ctMesh = bytes(9) + bytes(10) * 256 + bytes(11) * 4096 + bytes(12) * 65536
        Else
            ctMesh = bytes(4) + bytes(5) * 256
        End If

        'Längentest
        If bytes.Count < (ctMesh * 32) + 6 - 1 + 32 Then
            Log.Add("O3D-Datei Fehlerhaft / nicht Unterstütz!" & vbCrLf & "(Fehler: I000a, Datei: " & filename & ")", Log.TYPE_ERROR)
            Return Nothing
        End If

        For ctByte As Integer = 6 + addonOffset To (ctMesh * 32) + 6 - 1 + addonOffset Step 32
            '3D-Koordinaten
            verticesTemp.Add(-BitConverter.ToSingle({bytes(ctByte), bytes(ctByte + 1), bytes(ctByte + 2), bytes(ctByte + 3)}, 0))
            verticesTemp.Add(BitConverter.ToSingle({bytes(ctByte + 4), bytes(ctByte + 5), bytes(ctByte + 6), bytes(ctByte + 7)}, 0))
            verticesTemp.Add(BitConverter.ToSingle({bytes(ctByte + 8), bytes(ctByte + 9), bytes(ctByte + 10), bytes(ctByte + 11)}, 0))

            'Normals
            normalsTemp.Add(BitConverter.ToSingle({bytes(ctByte + 12), bytes(ctByte + 13), bytes(ctByte + 14), bytes(ctByte + 15)}, 0))
            normalsTemp.Add(BitConverter.ToSingle({bytes(ctByte + 16), bytes(ctByte + 17), bytes(ctByte + 18), bytes(ctByte + 19)}, 0))
            normalsTemp.Add(BitConverter.ToSingle({bytes(ctByte + 20), bytes(ctByte + 21), bytes(ctByte + 22), bytes(ctByte + 23)}, 0))

            '2D-Koordinaten
            texCoordsTemp.Add(BitConverter.ToSingle({bytes(ctByte + 24), bytes(ctByte + 25), bytes(ctByte + 26), bytes(ctByte + 27)}, 0))
            texCoordsTemp.Add(BitConverter.ToSingle({bytes(ctByte + 28), bytes(ctByte + 29), bytes(ctByte + 30), bytes(ctByte + 31)}, 0))
        Next

        If Not bytes(ctMesh * 32 + 6 + addonOffset) = &H49 Then
            Log.Add("Datei beschädigt! (Fehler: I100, Datei: " & filename.name & ")", Log.TYPE_WARNUNG)
            MsgBox("Datei beschädigt!" & vbCrLf & "(Fehler: I100, Datei: " & filename.name & ")")
            Return Nothing
        End If


        'Face Bereich
        Dim ctFaces As Integer
        If isAddon Then
            'addonOffset += 3
            ctFaces = bytes(ctMesh * 32 + 7 + addonOffset) + bytes(ctMesh * 32 + 8 + addonOffset) * 256 ' + bytes(ctMesh * 32 + 9 + addonOffset) * 4096
        Else
            ctFaces = bytes(ctMesh * 32 + 7) + bytes(ctMesh * 32 + 8) * 256
        End If


        'Längentest
        If bytes.Count < (ctMesh * 32) + 9 + (ctFaces * 8) - 1 + addonOffset Then
            Log.Add("O3D-Datei Fehlerhaft / nicht Unterstütz! (Fehler: I000b, Datei: " & filename & ")", Log.TYPE_ERROR)
            Return Nothing
        End If

        If isAddon Then addonOffset += 2

        For ctByte = (ctMesh * 32) + 9 + addonOffset To (ctMesh * 32) + 9 + (ctFaces * 8) - 1 + addonOffset Step 8
            For n = 0 To 5 Step 2
                facesTemp.Add(bytes(ctByte + n) + bytes(ctByte + n + 1) * 256)
            Next
            matlistTemp.Add(bytes(ctByte + 6) + bytes(ctByte + 7) * 256)
        Next

        'Texture Bereich
        Dim ctTexture As Integer
        Dim lenTexturenamen As Integer = 0
        Dim lenTexturename As Integer
        Dim texturenameTemp As String

        If isAddon Then
            'addonOffset += 2
            ctTexture = bytes(ctFaces * 8 + ctMesh * 32 + 10 + addonOffset) + bytes(ctFaces * 8 + ctMesh * 32 + 11 + addonOffset) * 256 + bytes(ctFaces * 8 + ctMesh * 32 + 12 + addonOffset) * 4096
        Else
            ctTexture = bytes(ctFaces * 8 + ctMesh * 32 + 10) + bytes(ctFaces * 8 + ctMesh * 32 + 11) * 256
        End If

        'Längentest
        If bytes.Count <= ctFaces * 8 + ctMesh * 32 + 12 + addonOffset + 1 + ctTexture * 45 - 2 Then
            Log.Add("O3D-Datei Fehlerhaft / nicht Unterstütz! (Fehler: I000c, Datei: " & filename & ")", Log.TYPE_ERROR)
            Return Nothing
        End If

        Dim startCtTexturen As Integer = ctFaces * 8 + ctMesh * 32 + 12 + addonOffset

        For i = 1 To ctTexture
            Dim startTexture As Integer = startCtTexturen + lenTexturenamen + (i - 1) * 45
            Dim newTexture As New LocalTexture

            With newTexture
                .diffuse.R = 255 * BitConverter.ToSingle({bytes(startTexture), bytes(startTexture + 1), bytes(startTexture + 2), bytes(startTexture + 3)}, 0)
                .diffuse.G = 255 * BitConverter.ToSingle({bytes(startTexture + 4), bytes(startTexture + 5), bytes(startTexture + 6), bytes(startTexture + 7)}, 0)
                .diffuse.B = 255 * BitConverter.ToSingle({bytes(startTexture + 8), bytes(startTexture + 9), bytes(startTexture + 10), bytes(startTexture + 11)}, 0)
                .diffuseAlpha = BitConverter.ToSingle({bytes(startTexture + 12), bytes(startTexture + 13), bytes(startTexture + 14), bytes(startTexture + 15)}, 0)
                .specular.R = 255 * BitConverter.ToSingle({bytes(startTexture + 16), bytes(startTexture + 17), bytes(startTexture + 18), bytes(startTexture + 19)}, 0)
                .specular.G = 255 * BitConverter.ToSingle({bytes(startTexture + 20), bytes(startTexture + 21), bytes(startTexture + 22), bytes(startTexture + 23)}, 0)
                .specular.B = 255 * BitConverter.ToSingle({bytes(startTexture + 24), bytes(startTexture + 25), bytes(startTexture + 26), bytes(startTexture + 27)}, 0)
                .emissive.R = 255 * BitConverter.ToSingle({bytes(startTexture + 28), bytes(startTexture + 29), bytes(startTexture + 30), bytes(startTexture + 31)}, 0)
                .emissive.G = 255 * BitConverter.ToSingle({bytes(startTexture + 32), bytes(startTexture + 33), bytes(startTexture + 34), bytes(startTexture + 35)}, 0)
                .emissive.B = 255 * BitConverter.ToSingle({bytes(startTexture + 36), bytes(startTexture + 37), bytes(startTexture + 38), bytes(startTexture + 39)}, 0)
                .power = BitConverter.ToSingle({bytes(startTexture + 40), bytes(startTexture + 41), bytes(startTexture + 42), bytes(startTexture + 43)}, 0)

                lenTexturename = bytes(startCtTexturen + 45 * i + lenTexturenamen - 1)
                texturenameTemp = ""
                For ctbyte = startCtTexturen + 45 * i + lenTexturenamen To startCtTexturen + 45 * i + lenTexturenamen + lenTexturename - 1
                    texturenameTemp &= Chr(bytes(ctbyte))
                Next
                .filename = New Filename(texturenameTemp)
                .id = i - 1
                temp3D.texturen.Add(newTexture)

                If i <= ctTexture Then lenTexturenamen += lenTexturename
            End With
        Next

        'If isAddon Then addonOffset += 2

        'Center
        Dim startCenter As Integer = startCtTexturen + lenTexturenamen + ctTexture * 45 + 1
        With temp3D
            .A1.X = Math.Round(BitConverter.ToSingle({bytes(startCenter), bytes(startCenter + 1), bytes(startCenter + 2), bytes(startCenter + 3)}, 0), 6)
            .A1.Z = Math.Round(BitConverter.ToSingle({bytes(startCenter + 4), bytes(startCenter + 5), bytes(startCenter + 6), bytes(startCenter + 7)}, 0), 6)
            .A1.Y = Math.Round(BitConverter.ToSingle({bytes(startCenter + 8), bytes(startCenter + 9), bytes(startCenter + 10), bytes(startCenter + 11)}, 0), 6)
            .A2 = Math.Round(BitConverter.ToSingle({bytes(startCenter + 12), bytes(startCenter + 13), bytes(startCenter + 14), bytes(startCenter + 15)}, 0), 6)
            .B1.X = Math.Round(BitConverter.ToSingle({bytes(startCenter + 16), bytes(startCenter + 17), bytes(startCenter + 18), bytes(startCenter + 19)}, 0), 6)
            .B1.Z = Math.Round(BitConverter.ToSingle({bytes(startCenter + 20), bytes(startCenter + 21), bytes(startCenter + 22), bytes(startCenter + 23)}, 0), 6)
            .B1.Y = Math.Round(BitConverter.ToSingle({bytes(startCenter + 24), bytes(startCenter + 25), bytes(startCenter + 26), bytes(startCenter + 27)}, 0), 6)
            .B2 = Math.Round(BitConverter.ToSingle({bytes(startCenter + 28), bytes(startCenter + 29), bytes(startCenter + 30), bytes(startCenter + 31)}, 0), 6)
            .origin.X = Math.Round(BitConverter.ToSingle({bytes(startCenter + 32), bytes(startCenter + 33), bytes(startCenter + 34), bytes(startCenter + 35)}, 0), 6)
            .origin.Z = Math.Round(BitConverter.ToSingle({bytes(startCenter + 36), bytes(startCenter + 37), bytes(startCenter + 38), bytes(startCenter + 39)}, 0), 6)
            .origin.Y = Math.Round(BitConverter.ToSingle({bytes(startCenter + 40), bytes(startCenter + 41), bytes(startCenter + 42), bytes(startCenter + 43)}, 0), 6)
            .origin_scale = Math.Round(BitConverter.ToSingle({bytes(startCenter + 44), bytes(startCenter + 45), bytes(startCenter + 46), bytes(startCenter + 47)}, 0), 6)
            .center.X = Math.Round(BitConverter.ToSingle({bytes(startCenter + 48), bytes(startCenter + 49), bytes(startCenter + 50), bytes(startCenter + 51)}, 0), 6)
            .center.Z = Math.Round(BitConverter.ToSingle({bytes(startCenter + 52), bytes(startCenter + 53), bytes(startCenter + 54), bytes(startCenter + 55)}, 0), 6)
            .center.Y = Math.Round(BitConverter.ToSingle({bytes(startCenter + 56), bytes(startCenter + 57), bytes(startCenter + 58), bytes(startCenter + 59)}, 0), 6)
            .scale = Math.Round(BitConverter.ToSingle({bytes(startCenter + 60), bytes(startCenter + 61), bytes(startCenter + 62), bytes(startCenter + 63)}, 0), 6)

            'Werte an Objekt übergeben
            .vertices = verticesTemp.ToArray
            .texCoords = texCoordsTemp.ToArray
            .normals = normalsTemp.ToArray

            'Subobjekte anhand der Texture aufteilen
            Dim arrTemp As New List(Of Integer)
            Dim pointTemp As New Point()
            For i = 0 To ctTexture - 1
                arrTemp.Clear()
                For n = 0 To ctFaces - 1
                    If matlistTemp(n) = i Then
                        arrTemp.Add(facesTemp(n * 3))
                        arrTemp.Add(facesTemp(n * 3 + 1))
                        arrTemp.Add(facesTemp(n * 3 + 2))

                        linesTemp.AddRange({facesTemp(n * 3), facesTemp(n * 3 + 1)})
                        linesTemp.AddRange({facesTemp(n * 3 + 1), facesTemp(n * 3 + 2)})
                        linesTemp.AddRange({facesTemp(n * 3 + 2), facesTemp(n * 3)})
                    End If
                Next
                .subObjekte.Add(arrTemp.ToArray)
            Next

            'Lines erzeugen
            .lines = linesTemp.ToArray

        End With
        Log.Add("Import erfolgreich! (Datei:" & filename.name & ", Format: O3D)")
        Return temp3D
    End Function

    Private Function readX(filename As Filename) As Local3DObjekt

        If checkIfExist(filename) Then
            Return Nothing
        End If

        Dim lines = Split(My.Computer.FileSystem.ReadAllText(filename), vbCrLf)

        Select Case Trim(lines(0))
            Case "xof 0302txt 0032"
                Return readX302(lines, filename)
            Case "xof 0303txt 0032"
                Return readX303(lines, filename)
            Case Else
                Log.Add("Import fehlgeschlagen! (Fehler: I012, Datei: " & filename & ")", Log.TYPE_ERROR)
                importWarnung("Import fehlgeschlagen!", "I012", filename, "falsches Format")
                Frm_Main.SSLBStatus.Text = ""
                Return Nothing
        End Select
    End Function

    Private Function readX302(lines As String(), filename As Filename) As Local3DObjekt

        Dim temp3D As New Local3DObjekt

        Dim verticesTemp As New List(Of Double)
        Dim normalsTemp As New List(Of Double)
        Dim texCoordsTemp As New List(Of Double)
        Dim facesTemp As New List(Of Integer)
        Dim matlistTemp As New List(Of Integer)
        Dim textureTemp As New List(Of Integer)
        Dim linesTemp As New List(Of Integer)
        Dim textureNames As New List(Of String)

        Dim ctMesh As Integer
        Dim ctMeshAlt As Integer = 0
        Dim ctFaces As Integer
        Dim ctNormals As Integer
        Dim ctTexture As Integer
        Dim ctMatlist As Integer
        Dim ctMatlistAlt As Integer = 0

        Dim materialLines As New List(Of Integer)
        Dim addFaces As New List(Of Integer)

        temp3D.center = New Point3D


        For ctLine = 0 To lines.Count - 1
            Select Case Split(Trim(lines(ctLine)), " ")(0)
                Case "FrameTransformMatrix"
                    temp3D.A1.X = Replace(Split(Trim(lines(ctLine + 1)), ",")(0), ".", ",")
                    temp3D.A1.Z = Replace(Split(Trim(lines(ctLine + 1)), ",")(1), ".", ",")
                    temp3D.A1.Y = Replace(Split(Trim(lines(ctLine + 1)), ",")(2), ".", ",")
                    temp3D.A2 = Replace(Split(Trim(lines(ctLine + 1)), ",")(3), ".", ",")

                    temp3D.B1.X = Replace(Split(Trim(lines(ctLine + 2)), ",")(0), ".", ",")
                    temp3D.B1.Z = Replace(Split(Trim(lines(ctLine + 2)), ",")(1), ".", ",")
                    temp3D.B1.Y = Replace(Split(Trim(lines(ctLine + 2)), ",")(2), ".", ",")
                    temp3D.B2 = Replace(Split(Trim(lines(ctLine + 2)), ",")(3), ".", ",")

                    temp3D.origin.X = Replace(Split(Trim(lines(ctLine + 3)), ",")(0), ".", ",")
                    temp3D.origin.Z = Replace(Split(Trim(lines(ctLine + 3)), ",")(1), ".", ",")
                    temp3D.origin.Y = Replace(Split(Trim(lines(ctLine + 3)), ",")(2), ".", ",")
                    temp3D.origin_scale = Replace(Split(Trim(lines(ctLine + 3)), ",")(3), ".", ",")

                    temp3D.center.X = -Replace(Split(Trim(lines(ctLine + 4)), ",")(0), ".", ",")
                    temp3D.center.Z = Replace(Split(Trim(lines(ctLine + 4)), ",")(2), ".", ",")
                    temp3D.center.Y = Replace(Split(Trim(lines(ctLine + 4)), ",")(1), ".", ",")
                    temp3D.scale = Replace(Split(Trim(lines(ctLine + 4)), ",")(3), ".", ",")
                    ctLine += 4
                Case "Mesh"
                    'Mesh-Bereich
                    ctMesh = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctMesh - 1
                        verticesTemp.Add(Replace(Split(Trim(lines(i)), ";")(0), ".", ",") + temp3D.center.X)
                        verticesTemp.Add(Replace(Split(Trim(lines(i)), ";")(2), ".", ",") + temp3D.center.Z)
                        verticesTemp.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ",") + temp3D.center.Y)
                    Next
                    ctLine += 3 + ctMesh

                    'Face-Bereich
                    ctFaces = Replace(lines(ctLine), ";", "")
                    For i = ctLine + 1 To ctLine + 1 + ctFaces - 1
                        lines(i) = Trim(Replace(lines(i), ";", ","))
                        Dim tempV As String() = Split(lines(i), ",")
                        facesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(3) + ctMeshAlt, tempV(2) + ctMeshAlt})
                        linesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(3) + ctMeshAlt})
                        linesTemp.AddRange({tempV(3) + ctMeshAlt, tempV(2) + ctMeshAlt})
                        linesTemp.AddRange({tempV(3) + ctMeshAlt, tempV(1) + ctMeshAlt})

                        For n As Integer = 3 To tempV(0) - 1
                            facesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(n + 1) + ctMeshAlt, tempV(n) + ctMeshAlt})

                            linesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(n + 1) + ctMeshAlt})
                            linesTemp.AddRange({tempV(n + 1) + ctMeshAlt, tempV(n) + ctMeshAlt})
                            addFaces.Add(i - ctLine - 1)
                        Next

                    Next
                    ctMeshAlt = ctMesh
                    ctLine += ctFaces

                Case "MeshTextureCoords"
                    'Texture-Koordinaten
                    ctTexture = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctTexture - 1
                        texCoordsTemp.Add(Replace(Split(Trim(lines(i)), ";")(0), ".", ","))
                        texCoordsTemp.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ","))
                    Next
                    ctLine += ctTexture + 2

                Case "MeshNormals"
                    'Normals-Bereich
                    ctNormals = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctNormals - 1
                        normalsTemp.Add(Replace(Split(Trim(lines(i)), ";")(0), ".", ","))
                        normalsTemp.Add(Replace(Split(Trim(lines(i)), ";")(2), ".", ","))
                        normalsTemp.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ","))
                    Next
                    ctLine += ctNormals + 2

                Case "MeshMaterialList"
                    If InStr(lines(ctLine), "List {") Then
                        ctMatlistAlt = materialLines.Count
                        ctMatlist = Replace(lines(ctLine + 2), ";", "")
                        For i = ctLine + 3 To ctLine + 3 + ctMatlist - 1
                            matlistTemp.Add(Replace(Trim(lines(i)), ";", "") + ctMatlistAlt)
                            If addFaces.Contains(i - ctLine - 3) Then   'Wenn 4 Edges zu diesem Face gehörten
                                For Each item In addFaces
                                    If item = i - ctLine - 3 Then
                                        matlistTemp.Add(Replace(Trim(lines(i)), ";", "") + ctMatlistAlt)
                                    End If
                                Next
                            End If
                        Next

                    End If


                    'Material-Bereich
                Case "Material"
                    materialLines.Add(ctLine)

                    'Texture-Bereich
                    If InStr(lines(ctLine + 5), "TextureFilename {") Then

                        Dim newTexture As New LocalTexture
                        With newTexture
                            .filename = New Filename(Replace(lines(ctLine + 6).Substring(0, lines(ctLine + 6).Length - 1), """", ""))
                            .matName = Split(lines(materialLines(materialLines.Count - 1)), " ")(1)

                            textureNames.Add(.filename.name)
                        End With
                        Dim exists As Boolean = False
                        For Each texture In temp3D.texturen
                            If texture.filename.name = newTexture.filename.name Then
                                exists = True
                            End If
                        Next
                        If Not exists Then

                            temp3D.texturen.Add(newTexture)
                        End If
                    Else
                        textureNames.Add("keine")
                    End If
                    ctLine += 5

            End Select
        Next

        With temp3D
            'Arrays übergeben
            .vertices = verticesTemp.ToArray
            .texCoords = texCoordsTemp.ToArray
            .normals = normalsTemp.ToArray

            'Lines erstellen
            .lines = linesTemp.ToArray


            'Subobjekte erstellen
            Dim newMatlist As New List(Of Integer)
            Dim handeledTextureNames As New List(Of String)
            For i = 0 To .texturen.Count - 1
                For n = 0 To textureNames.Count - 1
                    If textureNames(n) = .texturen(i).filename.name Then
                        If Not handeledTextureNames.Contains(.texturen(i).filename.name) Then
                            For x = 0 To matlistTemp.Count - 1
                                If matlistTemp(x) = n Then newMatlist.Add(i)
                            Next
                            handeledTextureNames.Add(.texturen(i).filename.name)
                        End If
                    End If
                Next
            Next

            matlistTemp = newMatlist

            Dim arrTemp As New List(Of Integer)
            For i = 0 To .texturen.Count - 1
                arrTemp.Clear()
                For n = 0 To matlistTemp.Count - 1
                    If matlistTemp(n) = i Then
                        arrTemp.Add(facesTemp(n * 3))
                        arrTemp.Add(facesTemp(n * 3 + 1))
                        arrTemp.Add(facesTemp(n * 3 + 2))
                    End If
                Next
                If arrTemp.Count > 0 Then
                    .subObjekte.Add(arrTemp.ToArray)
                End If
            Next

            Dim tempstr As String = ""
            For Each item In newMatlist
                tempstr &= item & vbCrLf
            Next

        End With


        If Not checkLocal3DObject(temp3D) Then Return Nothing

        Log.Add("Import erfolgreich! (Datei:" & filename.name & ", Format: X302)")
        Return temp3D
    End Function

    Private Function readX303(lines As String(), filename As Filename) As Local3DObjekt
        Dim temp3D As New Local3DObjekt

        Dim verticesTemp As New List(Of Double)
        Dim normalsTemp1 As New List(Of Double)
        Dim normalsTemp2 As New List(Of Double)
        Dim texCoordsTemp As New List(Of Double)
        Dim facesTemp As New List(Of Integer)
        Dim matlistTemp As New List(Of Integer)
        Dim textureTemp As New List(Of Integer)
        Dim linesTemp As New List(Of Integer)
        Dim textureNames As New List(Of String)

        Dim ctMesh As Integer
        Dim ctMeshAlt As Integer = 0
        Dim ctFaces As Integer
        Dim ctNormals As Integer
        Dim ctTexture As Integer
        Dim ctMatlist As Integer
        Dim ctMatlistAlt As Integer = 0

        Dim materialLines As New List(Of Integer)
        Dim addFaces As New List(Of Integer)

        temp3D.center = New Point3D

        For ctLine = 0 To lines.Count - 1
            Select Case Split(Trim(lines(ctLine)), " ")(0)
                Case "FrameTransformMatrix"
                    temp3D.A1.X = Replace(Split(Trim(lines(ctLine + 1)), ",")(0), ".", ",")
                    temp3D.A1.Z = Replace(Split(Trim(lines(ctLine + 1)), ",")(1), ".", ",")
                    temp3D.A1.Y = Replace(Split(Trim(lines(ctLine + 1)), ",")(2), ".", ",")
                    temp3D.A2 = Replace(Split(Trim(lines(ctLine + 1)), ",")(3), ".", ",")

                    temp3D.B1.X = Replace(Split(Trim(lines(ctLine + 2)), ",")(0), ".", ",")
                    temp3D.B1.Z = Replace(Split(Trim(lines(ctLine + 2)), ",")(1), ".", ",")
                    temp3D.B1.Y = Replace(Split(Trim(lines(ctLine + 2)), ",")(2), ".", ",")
                    temp3D.B2 = Replace(Split(Trim(lines(ctLine + 2)), ",")(3), ".", ",")

                    temp3D.origin.X = Replace(Split(Trim(lines(ctLine + 3)), ",")(0), ".", ",")
                    temp3D.origin.Z = Replace(Split(Trim(lines(ctLine + 3)), ",")(1), ".", ",")
                    temp3D.origin.Y = Replace(Split(Trim(lines(ctLine + 3)), ",")(2), ".", ",")
                    temp3D.origin_scale = Replace(Split(Trim(lines(ctLine + 3)), ",")(3), ".", ",")

                    temp3D.center.X = Replace(Split(Trim(lines(ctLine + 4)), ",")(0), ".", ",")
                    temp3D.center.Z = Replace(Split(Trim(lines(ctLine + 4)), ",")(1), ".", ",")
                    temp3D.center.Y = Replace(Split(Trim(lines(ctLine + 4)), ",")(2), ".", ",")
                    temp3D.scale = Replace(Replace(Split(Trim(lines(ctLine + 4)), ",")(3), ".", ","), ";", "")
                    ctLine += 4
                Case "Mesh"
                    'Mesh-Bereich
                    ctMesh = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctMesh - 1
                        verticesTemp.Add(-Replace(Split(Trim(lines(i)), ";")(0), ".", ",") * temp3D.origin.X + temp3D.center.X)
                        verticesTemp.Add(Replace(Split(Trim(lines(i)), ";")(2), ".", ",") * temp3D.origin.Z + temp3D.center.Z)
                        verticesTemp.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ",") * temp3D.origin.Y + temp3D.center.Y)
                    Next
                    ctLine += 2 + ctMesh

                    'Face-Bereich
                    ctFaces = Replace(lines(ctLine), ";", "")
                    For i = ctLine + 1 To ctLine + 1 + ctFaces - 1
                        lines(i) = Trim(Replace(lines(i), ";", ","))
                        Dim tempV As String() = Split(lines(i), ",")
                        facesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(3) + ctMeshAlt, tempV(2) + ctMeshAlt})

                        linesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(3) + ctMeshAlt})
                        linesTemp.AddRange({tempV(3) + ctMeshAlt, tempV(2) + ctMeshAlt})
                        linesTemp.AddRange({tempV(3) + ctMeshAlt, tempV(1) + ctMeshAlt})

                        For n As Integer = 3 To tempV(0) - 1
                            facesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(n + 1) + ctMeshAlt, tempV(n) + ctMeshAlt})

                            linesTemp.AddRange({tempV(1) + ctMeshAlt, tempV(n + 1) + ctMeshAlt})
                            linesTemp.AddRange({tempV(n + 1) + ctMeshAlt, tempV(n) + ctMeshAlt})
                            addFaces.Add(i - ctLine - 1)
                        Next
                    Next
                    ctMeshAlt = ctMesh
                    ctLine += ctFaces

                Case "MeshTextureCoords"
                    'Texture-Koordinaten
                    ctTexture = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctTexture - 1
                        texCoordsTemp.Add(Replace(Split(Trim(lines(i)), ";")(0), ".", ","))
                        texCoordsTemp.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ","))
                    Next
                    ctLine += ctTexture + 2

                Case "MeshNormals"
                    'Normals-Bereich
                    ctNormals = Replace(lines(ctLine + 1), ";", "")
                    For i = ctLine + 2 To ctLine + 2 + ctNormals - 1
                        normalsTemp1.Add(Replace(Split(Trim(lines(i)), ";")(0), ".", ","))
                        normalsTemp1.Add(Replace(Split(Trim(lines(i)), ";")(2), ".", ","))
                        normalsTemp1.Add(Replace(Split(Trim(lines(i)), ";")(1), ".", ","))
                    Next
                    ctLine += ctNormals + 2


                    For i = ctLine + 1 To ctLine + lines(ctLine).Split(";")(0)
                        For n As Integer = 1 To lines(i).Split(";")(0)
                            normalsTemp2.Add(normalsTemp1(Split(Replace(lines(i), ";", ","), ",")(n) * 3))
                            normalsTemp2.Add(normalsTemp1(Split(Replace(lines(i), ";", ","), ",")(n) * 3) + 1)
                            normalsTemp2.Add(normalsTemp1(Split(Replace(lines(i), ";", ","), ",")(n) * 3) + 1)
                        Next
                    Next

                Case Else
                    If InStr(lines(ctLine), "MeshMaterialList {") Then
                        ctMatlistAlt = materialLines.Count
                        ctMatlist = Replace(lines(ctLine + 2), ";", "")
                        For i = ctLine + 3 To ctLine + 3 + ctMatlist - 1
                            matlistTemp.Add(Replace(Trim(lines(i)), ";", "") + ctMatlistAlt)
                            If addFaces.Contains(i - ctLine - 3) Then   'Wenn 4 Edges zu diesem Face gehörten
                                For Each item In addFaces
                                    If item = i - ctLine - 3 Then
                                        matlistTemp.Add(Replace(Trim(lines(i)), ";", "") + ctMatlistAlt)
                                    End If
                                Next
                            End If
                        Next

                    End If


                    'Material-Bereich
                    If InStr(lines(ctLine), "Material ") Then
                        materialLines.Add(ctLine)

                        'Texture-Bereich
                        If InStr(lines(ctLine + 5), "TextureFilename {") Then

                            Dim newTexture As New LocalTexture
                            With newTexture
                                .diffuse.R = toSingle(Split(lines(ctLine + 1), ";")(0)) * 255
                                .diffuse.G = toSingle(Split(lines(ctLine + 1), ";")(1)) * 255
                                .diffuse.B = toSingle(Split(lines(ctLine + 1), ";")(2)) * 255
                                .diffuseAlpha = toSingle(Split(lines(ctLine + 1), ";")(3))

                                .power = toSingle(Split(lines(ctLine + 2), ";")(0))

                                .specular.R = toSingle(Split(lines(ctLine + 3), ";")(0)) * 255
                                .specular.G = toSingle(Split(lines(ctLine + 3), ";")(1)) * 255
                                .specular.B = toSingle(Split(lines(ctLine + 3), ";")(2)) * 255

                                .emissive.R = toSingle(Split(lines(ctLine + 4), ";")(0)) * 255
                                .emissive.G = toSingle(Split(lines(ctLine + 4), ";")(1)) * 255
                                .emissive.B = toSingle(Split(lines(ctLine + 4), ";")(2)) * 255

                                .filename = New Filename(Split(lines(ctLine + 5), """")(1))
                                .matName = Split(lines(materialLines(materialLines.Count - 1)), " ")(1)

                                textureNames.Add(.filename.name)
                            End With
                            Dim exists As Boolean = False
                            For Each texture In temp3D.texturen
                                If texture.filename.name = newTexture.filename.name Then
                                    exists = True
                                End If
                            Next
                            If Not exists Then

                                temp3D.texturen.Add(newTexture)
                            End If
                        Else
                            textureNames.Add("keine")
                        End If
                        ctLine += 5
                    End If
            End Select
        Next

        With temp3D
            'Arrays übergeben
            .vertices = verticesTemp.ToArray
            .texCoords = texCoordsTemp.ToArray
            .normals = normalsTemp2.ToArray

            'Lines erstellen
            .lines = linesTemp.ToArray


            'Subobjekte erstellen
            Dim newMatlist As New List(Of Integer)
            Dim handeledTextureNames As New List(Of String)
            For i = 0 To .texturen.Count - 1
                For n = 0 To textureNames.Count - 1
                    If textureNames(n) = .texturen(i).filename.name Then
                        If Not handeledTextureNames.Contains(.texturen(i).filename.name) Then
                            For x = 0 To matlistTemp.Count - 1
                                If matlistTemp(x) = n Then newMatlist.Add(i)
                            Next
                            handeledTextureNames.Add(.texturen(i).filename.name)
                        End If
                    End If
                Next
            Next

            matlistTemp = newMatlist

            Dim arrTemp As New List(Of Integer)
            For i = 0 To .texturen.Count - 1
                arrTemp.Clear()
                For n = 0 To matlistTemp.Count - 1
                    If matlistTemp(n) = i Then
                        arrTemp.Add(facesTemp(n * 3))
                        arrTemp.Add(facesTemp(n * 3 + 1))
                        arrTemp.Add(facesTemp(n * 3 + 2))
                    End If
                Next
                .subObjekte.Add(arrTemp.ToArray)
            Next

            Dim tempstr As String = ""
            For Each item In newMatlist
                tempstr &= item & vbCrLf
            Next


            '################### Hier weiter mit Rad!!! #########################

            If Not Math.Round(.B1.Y, 6) = 1 Then
                Dim tempVert As New List(Of Double)
                For i = 0 To .vertices.Count - 1 Step 3
                    Dim newPnt As New Point3D(.vertices(i), .vertices(i + 1), .vertices(i + 2))
                    If Not .B1.Z = 0 Then newPnt.rotate(.B1.Z * 90, Point3D.ACHSE_Y)    'Ist hier eigentlich die Z-Achse
                    If Not .B1.X = 0 Then newPnt.rotate(.B1.X * 90, Point3D.ACHSE_X)
                    tempVert.AddRange(newPnt.toList)
                Next
                .vertices = tempVert.ToArray
            End If

            If Not Math.Round(.A1.X, 6) = 1 Then
                Dim tempVert As New List(Of Double)
                For i = 0 To .vertices.Count - 1 Step 3
                    Dim newPnt As New Point3D(.vertices(i), .vertices(i + 1), .vertices(i + 2))
                    If Not .A1.Y = 0 Then newPnt.rotate(.A1.Y * 90, Point3D.ACHSE_Z)    'Ist hier eigentlich die Y-Achse
                    If Not .A1.Z = 0 Then newPnt.rotate(.A1.Z * 90, Point3D.ACHSE_Y)    'Ist hier eigentlich die Z-Achse
                    tempVert.AddRange(newPnt.toList)
                Next
                .vertices = tempVert.ToArray
            End If

            If Not Math.Round(.origin.Z, 6) = 1 Then
                Dim tempVert As New List(Of Double)
                For i = 0 To .vertices.Count - 1 Step 3
                    Dim newPnt As New Point3D(.vertices(i), - .vertices(i + 1), .vertices(i + 2))
                    If Not .origin.X = 0 Then newPnt.rotate(.origin.X * -90, Point3D.ACHSE_X)
                    If Not .origin.Y = 0 Then newPnt.rotate(.origin.Y * 90, Point3D.ACHSE_Z)
                    tempVert.AddRange(newPnt.toList)
                Next
                .vertices = tempVert.ToArray
            End If
        End With

        If Not checkLocal3DObject(temp3D) Then Return Nothing

        Log.Add("Import erfolgreich! (Datei:" & filename.name & ", Format: X303)")
        Return temp3D
    End Function

    Public Function readSli(filename As Filename) As Local3DObjekt
        Dim temp3D As New Local3DObjekt
        Dim tempSLI As New Proj_Sli(filename)
        With temp3D
            .position = New Point3D()
            .vertices = tempSLI.vertices
            .subObjekte = tempSLI.subobjekte
            .lines = tempSLI.lines
            .texCoords = tempSLI.texCoords
            .texturen = tempSLI.textures
        End With
        Return temp3D
    End Function


    Private Function checkLocal3DObject(objekt As Local3DObjekt) As Boolean
        With objekt
            If .vertices.Count = 0 Then Return False
            If .subObjekte.Count = 0 Then Return False
            If .texCoords.Count = 0 Then Return False
            If .lines.Count = 0 Then Return False
            Return True
        End With
    End Function


    Private Function checkIfExist(filename As Filename) As Boolean
        If Not My.Computer.FileSystem.FileExists(filename) Then
            Log.Add("Import fehlgeschlagen! (Fehler: I001, Datei: " & filename & ") nicht gefunden", Log.TYPE_ERROR)
            importWarnung("Import fehlgeschlagen!", "I001", filename, "Datei nicht gefunden")

            Frm_Main.SSLBStatus.Text = ""
            Return True
        Else
            Return False
        End If
    End Function

    Public Function ToDouble(ByVal hexValue As String) As Double
        Try
            Dim iOutputIndex As Integer = 0
            Dim bArray(3) As Byte
            For iInputIndex As Integer = 0 To 6 Step 2 ' comparing with 0 is faster 
                bArray(iOutputIndex) = Byte.Parse(hexValue.Substring(iInputIndex, 2), Globalization.NumberStyles.HexNumber)
                iOutputIndex += 1
            Next
            If BitConverter.ToSingle(bArray, 0) > 1000000000 Then
                Return 0
            End If
            Return BitConverter.ToSingle(bArray, 0)
        Catch ex As Exception
        End Try
        Log.Add("HEX:" & hexValue & " is not a Number!", Log.TYPE_ERROR)
        Return Single.NaN ' something invalid was provided 
    End Function
End Module
