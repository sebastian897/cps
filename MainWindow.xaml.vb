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
        Dim topShape As Double = Double.MaxValue
        Dim leftShape As Double = Double.MaxValue
        For i = 0 To 2
            For j = 0 To 2
                If rand.Next(0, 2) Then
                    If topShape > i Then
                        topShape = i
                    End If
                    If j < leftShape Then
                        leftShape = j
                    End If
                    Dim rectGeometry As New RectangleGeometry(New Rect((i - topShape) * totalSqrLength, (j - leftShape) * totalSqrLength, sqrLength, sqrLength))
                    geometryGroup.Children.Add(rectGeometry)
                End If
            Next
        Next

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
                Dim X = w + canvasX \ totalSqrLength
                Dim Y = h + canvasY \ totalSqrLength
                If X < 0 OrElse X > 8 OrElse Y < 0 OrElse Y > 8 OrElse g(X, Y) IsNot Nothing Then
                    Return False
                End If
            End If
        Next i
        Return True
    End Function
    Private Function GetValidPositions(ByVal p As Path, ByVal g(,) As Rectangle) As List(Of Point)
        Dim positions As New List(Of Point)
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return positions
        For row = 0 To rows - 1
            For col = 0 To cols - 1
                If DoesShapeFit(row, col, p, g) Then
                    positions.Add(New Point(row * totalSqrLength, col * totalSqrLength))
                End If
            Next col
        Next row
        Return positions
    End Function
    Function CanPLayerPlaceShapes(ByVal g(,) As Rectangle, ByRef s As List(Of Path)) As Boolean
        If s.Count = 0 Then
            Return True ' Base case: no shapes left to place
        End If
        For i = s.Count - 1 To 0 Step -1
            Dim validPos = GetValidPositions(s(i), g)
            If validPos.Count = 0 Then
                Return False
            End If
            For Each pos In validPos
                Dim newg = DeepCopyGrid(g)
                PlacedShapeInGrid(pos, s(i), newg, False)
                Dim newShapes As New List(Of Path)(s)
                newShapes.RemoveAt(i)
                If CanPLayerPlaceShapes(newg, newShapes) Then
                    Return True
                End If
            Next pos
        Next i
        Return False
    End Function

    Private Function BuildShadow(w As Integer, h As Integer, ByVal p As Path) As Path
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return Nothing
        For row = 0 To rows - 1
            For col = 0 To cols - 1
                Dim gridSquareLeftPos As Double = col * totalSqrLength
                Dim gridSquareTopPos As Double = row * totalSqrLength

                If Canvas.GetLeft(p) + sqrLength / 2 >= gridSquareLeftPos And
                    Canvas.GetLeft(p) + sqrLength / 2 <= gridSquareLeftPos + totalSqrLength And
                    Canvas.GetTop(p) + sqrLength / 2 >= gridSquareTopPos And
                    Canvas.GetTop(p) + sqrLength / 2 <= gridSquareTopPos + totalSqrLength And
                    DoesShapeFit(col, row, p, grid) Then

                    Dim newp As New Path()
                    newp.Data = p.Data.Clone()
                    newp.Fill = p.Fill
                    newp.Opacity = 0.5
                    newp.IsHitTestVisible = False
                    newp.Stroke = p.Stroke

                    ' Optionally, scale around a center point (e.g., center of bounding box)
                    newp.RenderTransform = Transform.Identity
                    newp.RenderTransformOrigin = New Point(0, 0)

                    Canvas.SetLeft(newp, gridSquareLeftPos)
                    Canvas.SetTop(newp, gridSquareTopPos)
                    Return newp
                End If
            Next col
        Next row
        Return Nothing
    End Function
    Sub ClearGrid()
        For i = 0 To rows - 1
            For j = 0 To cols - 1
                If grid(i, j) IsNot Nothing Then
                    grid(i, j) = Nothing
                End If
            Next
        Next
    End Sub
    Function DeepCopyGrid(originalGrid As Rectangle(,)) As Rectangle(,)
        Dim rows = originalGrid.GetLength(0)
        Dim cols = originalGrid.GetLength(1)
        Dim newGrid(rows - 1, cols - 1) As Rectangle

        For i As Integer = 0 To rows - 1
            For j As Integer = 0 To cols - 1
                If originalGrid(i, j) IsNot Nothing Then
                    ' Create a new Rectangle with the same properties
                    Dim r As New Rectangle With {
                    .Width = originalGrid(i, j).Width,
                    .Height = originalGrid(i, j).Height,
                    .Fill = originalGrid(i, j).Fill,
                    .Stroke = originalGrid(i, j).Stroke,
                    .StrokeThickness = originalGrid(i, j).StrokeThickness
                }
                    ' Position on Canvas also matters if you track it
                    Canvas.SetLeft(r, Canvas.GetLeft(originalGrid(i, j)))
                    Canvas.SetTop(r, Canvas.GetTop(originalGrid(i, j)))

                    newGrid(i, j) = r
                Else
                    newGrid(i, j) = Nothing
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
        Dim totalWidth As Double = cols * totalSqrLength - spacing
        Dim totalHeight As Double = rows * totalSqrLength - spacing + piecesHeight

        ' Adjust Window size to fit Canvas plus window borders
        Me.WindowStartupLocation = WindowStartupLocation.Manual
        Me.SizeToContent = SizeToContent.Manual
        Me.Width = totalWidth + extraWindowWidth  ' Add some margin for window chrome
        Me.Height = totalHeight + extraWindowHeight
        Me.ResizeMode = ResizeMode.NoResize
        Me.Background = New SolidColorBrush(Color.FromRgb(46, 46, 46))

        For row = 0 To rows - 1
            For col = 0 To cols - 1
                Dim rect As New Rectangle With {
                    .Width = sqrLength,
                    .Height = sqrLength,
                    .Fill = New SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    .StrokeThickness = shapeskirt,
                    .Stroke = New SolidColorBrush(Color.FromRgb(42, 42, 42))
                }

                ' Calculate position on Canvas
                Dim leftPos As Double = col * totalSqrLength
                Dim topPos As Double = row * totalSqrLength

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
    Private Sub Path_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        dragging = True
        Dim p = DirectCast(sender, Path)
        clickOffsetOnShape = e.GetPosition(p)
        p.CaptureMouse()
        currentShadow = Nothing
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

        p.RenderTransformOrigin = New Point(0.5, 0.5)

        ' Create a new shadow
        Dim shadow = BuildShadow(shapeLeft, shapeTop, p)
        If shadow IsNot Nothing Then
            MyCanvas.Children.Add(shadow)
            currentShadow = shadow ' store reference to the latest shadow

            p.RenderTransform = Transform.Identity
        Else
            currentShadow = Nothing

            p.RenderTransform = New ScaleTransform(2 / 3, 2 / 3)
        End If
    End Sub
    Private Sub Path_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        clickOffsetOnShape = New Point()
        Dim p = DirectCast(sender, Path)
        p.ReleaseMouseCapture()
        If currentShadow IsNot Nothing Then
            Dim pathLeft = Canvas.GetLeft(currentShadow) / totalSqrLength
            Dim pathTop = Canvas.GetTop(currentShadow) / totalSqrLength
            Dim point As New Point(pathLeft, pathTop)
            PlacedShapeInGrid(point, currentShadow, grid, True)
            shapes.remove(p)
            If shapes.Count = 0 Then
                shapes = ReloadShapes()
                ShapesHandler()
            End If
            MyCanvas.Children.Remove(p)
            MyCanvas.children.Remove(currentShadow)
        Else
            ResetShape(p, shapes.IndexOf(p))
            Exit Sub
        End If
    End Sub
    Sub PlacedShapeInGrid(p As point, pa As path, ByRef g(,) As Rectangle, display As Boolean)
        Dim gg = TryCast(pa.Data, GeometryGroup)
        If gg IsNot Nothing Then
            For i = 0 To gg.Children.OfType(Of RectangleGeometry)().ToList().Count - 1
                Dim rg = gg.Children.OfType(Of RectangleGeometry)().ToList()(i)
                If rg IsNot Nothing Then
                    Dim canvasX = p.X * totalSqrLength + rg.Rect.X
                    Dim canvasY = p.Y * totalSqrLength + rg.Rect.Y
                    Dim X = canvasX / totalSqrLength
                    Dim Y = canvasY / totalSqrLength
                    If X < 0 OrElse X > 8 OrElse Y < 0 OrElse Y > 8 Then
                        Exit Sub
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
                End If
            Next i
        End If
        ClearSqrs(g)
    End Sub
    Function ReloadShapes() As List(Of Path)
        Dim newShapes As New List(Of Path)
        Dim attempts As Integer = 0
        While newShapes.Count < 3
            attempts += 1
            Dim shape = BuildShape()
            newShapes.Add(shape)
            If Not CanPLayerPlaceShapes(DeepCopyGrid(grid), newShapes) Then
                newShapes.Clear()
            End If
        End While
        Return newShapes
    End Function
    Function GetFullRows(grid As Rectangle(,)) As list(Of Integer)
        Dim rowsDeleted As New list(Of Integer)
        For row = 0 To rows - 1
            Dim rowCount = 0
            For col = 0 To cols - 1
                If grid(row, col) IsNot Nothing Then
                    rowCount += 1
                End If
            Next col
            If rowCount = cols Then
                rowsDeleted.Add(row)
            End If
        Next row
        Return rowsDeleted
    End Function
    Function GetFullCols(grid As Rectangle(,)) As list(Of Integer)
        Dim colsDeleted As New list(Of Integer)
        For col = 0 To cols - 1
            Dim colCount = 0
            For row = 0 To rows - 1
                If grid(row, col) IsNot Nothing Then
                    colCount += 1
                End If
            Next row
            If colCount = rows Then
                colsDeleted.Add(col)
            End If
        Next col
        Return colsDeleted
    End Function
    Sub ClearSqrs(g(,) As Rectangle)
        Dim colsDeleted = GetFullCols(grid)
        Dim rowsDeleted = GetFullRows(grid)
        If colsDeleted.Count = 0 And rowsDeleted.Count = 0 Then
            Exit Sub
        End If
        For col = 0 To colsDeleted.Count - 1
            For row = 0 To rows - 1
                If grid(row, colsDeleted(col)) IsNot Nothing Then
                    MyCanvas.Children.Remove(grid(row, colsDeleted(col)))
                    grid(row, colsDeleted(col)) = Nothing
                End If
            Next
        Next
        For row = 0 To rowsDeleted.Count - 1
            For col = 0 To cols - 1
                If grid(rowsDeleted(row), col) IsNot Nothing Then
                    MyCanvas.Children.Remove(grid(rowsDeleted(row), col))
                    grid(rowsDeleted(row), col) = Nothing
                End If
            Next
        Next
    End Sub
End Class
