Imports System.Windows.Ink
Imports System.Windows.Media
Imports System.Windows.Shapes

Class MainWindow

    Private dragging As Boolean = False
    Private clickOffsetOnShape As Point
    Dim rows As Double = 9
    Dim cols As Double = 9
    Dim sqrLength As Double = 100
    Dim shapeskirt = sqrLength * 0.05
    Dim spacing As Double = sqrLength * 0.05  ' space between rectangles
    Dim totalSqrLength As Double = sqrLength * 1.05
    Dim extraWindowWidth = 40
    Dim extraWindowHeight = 40
    Dim piecesHeight As Double = totalSqrLength * 3

    Private Function BuildShape() As Path
        ' Generate a random integer between 0 (inclusive) and 100 (exclusive)
        Dim rand As New Random()
        Dim geometryGroup As New GeometryGroup()
        Do
            Dim topShape As Double = Double.MaxValue
            Dim leftShape As Double = Double.MaxValue
            For Y = 0 To 2
                For X = 0 To 2
                    If rand.Next(0, 4) > 0 Then
                        If topShape > Y Then
                            topShape = Y
                        End If
                        If X < leftShape Then
                            leftShape = X
                        End If
                        Dim rectGeometry As New RectangleGeometry(New Rect((X - leftShape) * totalSqrLength, (Y - topShape) * totalSqrLength, sqrLength, sqrLength))
                        geometryGroup.Children.Add(rectGeometry)
                    End If
                Next
            Next
        Loop Until geometryGroup.children.count() > 0

        Dim path As New Path() With {
                .Data = geometryGroup,
                .Fill = RandomBrush()
            }
            Return path
    End Function

    Private Function RandomBrush() As SolidColorBrush
        Dim rnd As New Random()
        Return New SolidColorBrush(Color.FromRgb(
        CByte(rnd.Next(256)),
        CByte(rnd.Next(256)),
        CByte(rnd.Next(256))
    ))
    End Function
    Private Function DoesShapeFit(w As Integer, h As Integer, ByVal p As Path, ByVal g(,) As Rectangle) As Boolean
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return False
        For i = 0 To gg.Children.OfType(Of RectangleGeometry)().ToList().Count - 1
            Dim rg = gg.Children.OfType(Of RectangleGeometry)().ToList()(i)
            If rg IsNot Nothing Then
                Dim canvasX = rg.Rect.X
                Dim canvasY = rg.Rect.Y
                Dim X As Integer = w + CInt(Math.Floor(canvasX / totalSqrLength))
                Dim Y As Integer = h + CInt(Math.Floor(canvasY / totalSqrLength))
                If X < 0 OrElse X > 8 OrElse Y < 0 OrElse Y > 8 OrElse g(X, Y) IsNot Nothing Then
                    Return False
                End If
                'Debug.WriteLine($"Checking shape square at grid[{X}, {Y}] - occupied? {(g(X, Y) IsNot Nothing)}")
            End If
        Next i
        Return True
    End Function
    Private Function GetValidPositions(ByVal p As Path, ByVal g(,) As Rectangle) As List(Of Point)
        Dim positions As New List(Of Point)
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return positions
        For Y = 0 To rows - 1
            For X = 0 To cols - 1
                If DoesShapeFit(X, Y, p, g) Then
                    positions.Add(New Point(X * totalSqrLength, Y * totalSqrLength))
                End If
            Next X
        Next Y
        Return positions
    End Function
    Function CanPLayerPlaceShapes(ByRef g(,) As Rectangle, ByRef s As List(Of Path)) As Boolean
        If s.Count = 0 Then
            'Debug.WriteLine($"Reached end, returning")
            Return True ' Base case: no shapes left to place
        End If
        For i = s.Count - 1 To 0 Step -1
            Dim validPos = GetValidPositions(s(i), g)
            For Each pos In validPos
                Dim newg = DeepCopyGrid(g)
                PlacedShapeInGrid(pos, s(i), newg, False, i)
                Dim newShapes = DeepCopyShapes(s)
                newShapes.RemoveAt(i)
                If CanPLayerPlaceShapes(newg, newShapes) Then
                    'Debug.WriteLine($"recursion was sucessful, returning")
                    Return True
                Else
                    'Debug.WriteLine($"recursion failed")
                End If
            Next pos
        Next i
        Return False
    End Function
    Private Function BuildShadow(ByVal p As Path) As Path
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return Nothing
        For Y = 0 To rows - 1
            For X = 0 To cols - 1
                Dim gridSquareLeftPos As Double = X * totalSqrLength
                Dim gridSquareTopPos As Double = Y * totalSqrLength

                If Canvas.GetLeft(p) + sqrLength / 2 >= gridSquareLeftPos And
                    Canvas.GetLeft(p) + sqrLength / 2 <= gridSquareLeftPos + totalSqrLength And
                    Canvas.GetTop(p) + sqrLength / 2 >= gridSquareTopPos And
                    Canvas.GetTop(p) + sqrLength / 2 <= gridSquareTopPos + totalSqrLength And
                    DoesShapeFit(X, Y, p, grid) Then

                    Dim newp As New Path()
                    newp.Data = p.Data.Clone()
                    newp.Fill = p.Fill
                    newp.Opacity = 0.5
                    newp.IsHitTestVisible = False
                    newp.Stroke = p.Stroke

                    Canvas.SetLeft(newp, gridSquareLeftPos)
                    Canvas.SetTop(newp, gridSquareTopPos)
                    Return newp
                End If
            Next X
        Next Y
        Return Nothing
    End Function
    Sub ClearGrid()
        For Y = 0 To rows - 1
            For X = 0 To cols - 1
                If grid(X, Y) IsNot Nothing Then
                    grid(X, Y) = Nothing
                End If
            Next
        Next
    End Sub
    Function DeepCopyGeometryGroup(gg As GeometryGroup) As GeometryGroup
        Dim newGG As New GeometryGroup()
        For Each child In gg.Children
            If TypeOf child Is RectangleGeometry Then
                Dim rect = DirectCast(child, RectangleGeometry)
                Dim newRect As New RectangleGeometry(New Rect(rect.Rect.X, rect.Rect.Y, rect.Rect.Width, rect.Rect.Height))
                newGG.Children.Add(newRect)
            End If
        Next
        Return newGG
    End Function
    Function DeepCopyShapes(shapes As List(Of Path)) As List(Of Path)
        Dim newShapes As New List(Of Path)
        For Each shape In shapes
            Dim gg = TryCast(shape.Data, GeometryGroup)
            If gg IsNot Nothing Then
                Dim newPath As New Path() With {
                    .Data = DeepCopyGeometryGroup(gg),
                    .Fill = shape.Fill,
                    .Stroke = shape.Stroke,
                    .StrokeThickness = shape.StrokeThickness
                }
                newShapes.Add(newPath)
            End If
        Next
        Return newShapes
    End Function
    Function DeepCopyGrid(originalGrid As Rectangle(,)) As Rectangle(,)
        Dim cols = originalGrid.GetLength(0)
        Dim rows = originalGrid.GetLength(1)
        Dim newGrid(cols - 1, rows - 1) As Rectangle

        For Y As Integer = 0 To rows - 1
            For X As Integer = 0 To cols - 1
                If originalGrid(X, Y) IsNot Nothing Then
                    ' Create a new Rectangle with the same properties
                    Dim r As New Rectangle With {
                    .Width = originalGrid(X, Y).Width,
                    .Height = originalGrid(X, Y).Height,
                    .Fill = originalGrid(X, Y).Fill,
                    .Stroke = originalGrid(X, Y).Stroke,
                    .StrokeThickness = originalGrid(X, Y).StrokeThickness
                }
                    ' Position on Canvas also matters if you track it
                    Canvas.SetLeft(r, Canvas.GetLeft(originalGrid(X, Y)))
                    Canvas.SetTop(r, Canvas.GetTop(originalGrid(X, Y)))

                    newGrid(X, Y) = r
                Else
                    newGrid(X, Y) = Nothing
                End If
            Next
        Next
        Return newGrid
    End Function
    Private currentShadow As Path
    Private grid(8, 8) As Rectangle
    Dim shapes As New List(Of Path)
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        MyCanvas.Children.Clear()
        shapes = ReloadShapes()
        Dim totalWidth As Double = rows * totalSqrLength - spacing
        Dim totalHeight As Double = cols * totalSqrLength - spacing + piecesHeight

        ' Adjust Window size to fit Canvas plus window borders
        Me.WindowStartupLocation = WindowStartupLocation.Manual
        Me.SizeToContent = SizeToContent.Manual
        Me.Width = totalWidth + extraWindowWidth  ' Add some margin for window chrome
        Me.Height = totalHeight + extraWindowHeight
        Me.ResizeMode = ResizeMode.NoResize
        Me.Background = New SolidColorBrush(Color.FromRgb(46, 46, 46))

        For Y = 0 To rows - 1
            For X = 0 To cols - 1
                Dim rect As New Rectangle With {
                    .Width = sqrLength,
                    .Height = sqrLength,
                    .Fill = New SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    .StrokeThickness = shapeskirt,
                    .Stroke = New SolidColorBrush(Color.FromRgb(42, 42, 42))
                }

                ' Calculate position on Canvas
                Dim leftPos As Double = X * totalSqrLength
                Dim topPos As Double = Y * totalSqrLength

                Canvas.SetLeft(rect, leftPos)
                Canvas.SetTop(rect, topPos)

                MyCanvas.Children.Add(rect)
            Next
        Next
        ' Set Canvas size explicitly
        MyCanvas.Width = totalWidth
        MyCanvas.Height = totalHeight
        ShapesHandler()
    End Sub
    Sub ResetShape(shape, i)
        Canvas.SetLeft(shape, i * piecesHeight)
        Canvas.SetTop(shape, piecesHeight * 3)

        ' Optionally, scale around a center point (e.g., center of bounding box)
        Dim bounds As Rect = shape.Data.Bounds
        Dim centerX As Double = bounds.X + bounds.Width / 2
        Dim centerY As Double = bounds.Y + bounds.Height / 2

        shape.RenderTransform = New ScaleTransform(2 / 3, 2 / 3)
        shape.RenderTransformOrigin = New Point(0.5, 0.5)
    End Sub
    Sub ShapesHandler()
        For i = 0 To shapes.Count - 1
            ResetShape(shapes(i), i)

            AddHandler shapes(i).MouseLeftButtonDown, AddressOf Path_MouseLeftButtonDown
            AddHandler shapes(i).MouseMove, AddressOf Path_MouseMove
            AddHandler shapes(i).MouseLeftButtonUp, AddressOf Path_MouseLeftButtonUp

            MyCanvas.Children.Add(shapes(i))
        Next i
    End Sub
    Private Sub BringToFront(el As UIElement, parent As Canvas)
        Dim maxZ As Integer = 0

        For Each child As UIElement In parent.Children
            maxZ = Math.Max(maxZ, Panel.GetZIndex(child))
        Next

        Panel.SetZIndex(el, maxZ + 1)
    End Sub
    Private Sub Path_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        dragging = True
        Dim p = DirectCast(sender, Path)
        clickOffsetOnShape = e.GetPosition(p)
        p.CaptureMouse()
        currentShadow = Nothing
        BringToFront(p, MyCanvas)
        p.RenderTransformOrigin = New Point(0.5, 0.5)
        p.RenderTransform = Transform.Identity

    End Sub
    Private Sub Path_MouseMove(sender As Object, e As MouseEventArgs)
        Dim p = DirectCast(sender, Path)
        If Not shapes.contains(p) OrElse Not p.IsMouseCaptured Then
            Return
        End If

        Dim mousePos = e.GetPosition(MyCanvas)

        ' Calculate new position taking the click offset into account
        Dim shapeLeft = mousePos.X - clickOffsetOnShape.X
        Dim shapeTop = mousePos.Y - clickOffsetOnShape.Y

        Canvas.SetLeft(p, shapeLeft)
        Canvas.SetTop(p, shapeTop)

        If currentShadow IsNot Nothing AndAlso MyCanvas.Children.Contains(currentShadow) Then
            MyCanvas.Children.Remove(currentShadow)
        End If

        ' Create a new shadow
        Dim shadow = BuildShadow(p)
        If shadow IsNot Nothing Then
            MyCanvas.Children.Add(shadow)
            currentShadow = shadow ' store reference to the latest shadow

        Else
            currentShadow = Nothing

        End If
    End Sub
    Private Sub Path_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        clickOffsetOnShape = New Point()
        Dim p = DirectCast(sender, Path)
        p.ReleaseMouseCapture()
        If currentShadow IsNot Nothing Then
            Dim pathLeft = Canvas.GetLeft(currentShadow)
            Dim pathTop = Canvas.GetTop(currentShadow)
            Dim point As New Point(pathLeft, pathTop)
            PlacedShapeInGrid(point, currentShadow, grid, True, shapes.IndexOf(p))
            shapes.remove(p)
            If shapes.Count = 0 Then
                shapes = ReloadShapes()
                ShapesHandler()
            Else
                For i = 0 To shapes.Count - 1
                    ResetShape(shapes(i), i)
                Next i
            End If
            MyCanvas.Children.Remove(p)
            MyCanvas.children.Remove(currentShadow)
        Else
            ResetShape(p, shapes.IndexOf(p))
            Exit Sub
        End If
    End Sub
    Function PlacedShapeInGrid(p As point, pa As path, ByRef g(,) As Rectangle, display As Boolean, pai As Integer) As Boolean
        Dim gg = TryCast(pa.Data, GeometryGroup)
        If gg IsNot Nothing Then
            For i = 0 To gg.Children.OfType(Of RectangleGeometry)().ToList().Count - 1
                Dim rg = gg.Children.OfType(Of RectangleGeometry)().ToList()(i)
                If rg IsNot Nothing Then
                    Dim canvasX = p.X + rg.Rect.X
                    Dim canvasY = p.Y + rg.Rect.Y
                    Dim X As Integer = CInt(Math.Floor(canvasX / totalSqrLength))
                    Dim Y As Integer = CInt(Math.Floor(canvasY / totalSqrLength))
                    If X < 0 OrElse X > 8 OrElse Y < 0 OrElse Y > 8 Then
                        Return True
                    End If
                    Dim r = New Rectangle With {
                            .Width = sqrLength,
                            .Height = sqrLength,
                            .Fill = pa.Fill
                        }
                    Canvas.SetLeft(r, canvasX)
                    Canvas.SetTop(r, canvasY)
                    g(X, Y) = r
                    If display Then
                        MyCanvas.Children.Add(r)
                    End If
                    'Debug.WriteLine($"Placed shape {pai} at grid[{X}, {Y}]")
                End If
            Next i
        End If
        ClearSqrs(g)
        Return True
    End Function
    Function ReloadShapes() As List(Of Path)
        Dim newShapes As New List(Of Path)
        Dim attempts As Integer = 0
        While newShapes.Count < 3
            Dim shape = BuildShape()
            newShapes.Add(shape)
            If Not CanPLayerPlaceShapes(DeepCopyGrid(grid), newShapes) Then
                attempts += 1
                newShapes.Remove(shape)
            End If
        End While
        Return newShapes
    End Function
    Function GetFullRows(g As Rectangle(,)) As list(Of Integer)
        Dim rowsDeleted As New list(Of Integer)
        For Y = 0 To rows - 1
            Dim rowCount = 0
            For X = 0 To cols - 1
                If g(X, Y) IsNot Nothing Then
                    rowCount += 1
                End If
            Next X
            If rowCount = cols Then
                rowsDeleted.Add(Y)
            End If
        Next Y
        Return rowsDeleted
    End Function
    Function GetFullCols(g As Rectangle(,)) As list(Of Integer)
        Dim colsDeleted As New list(Of Integer)
        For X = 0 To cols - 1
            Dim colCount = 0
            For Y = 0 To rows - 1
                If g(X, Y) IsNot Nothing Then
                    colCount += 1
                End If
            Next Y
            If colCount = rows Then
                colsDeleted.Add(X)
            End If
        Next X
        Return colsDeleted
    End Function
    Sub ClearSqrs(ByRef g(,) As Rectangle)
        Dim colsDeleted = GetFullCols(g)
        Dim rowsDeleted = GetFullRows(g)
        If colsDeleted.Count = 0 And rowsDeleted.Count = 0 Then
            Exit Sub
        End If
        For Y = 0 To rows - 1
            For X = 0 To cols - 1
                If g(X, Y) IsNot Nothing AndAlso (rowsDeleted.Contains(Y) OrElse colsDeleted.Contains(X)) Then
                    If MyCanvas.Children.Contains(g(X, Y)) Then
                        MyCanvas.Children.Remove(g(X, Y))  ' Remove from canvas only if present
                    End If
                    g(X, Y) = Nothing
                End If
            Next
        Next
    End Sub
End Class
