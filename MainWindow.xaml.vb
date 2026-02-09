Imports System.Windows.Forms
Imports System.Windows.Threading

Namespace cps
    Partial Public Class MainWindow
        Inherits Window

        Private resizeTimer As DispatcherTimer

        Public Event ResizeEnded As EventHandler


        Private dragging As Boolean = False
        Private clickOffsetOnShape As Point
        Dim windowSize As Double = 2 / 3
        Dim rows As Double = 5
        Dim cols As Double = 5
        Dim sqrLength As Double
        Dim sqrSpacing As Double = 1.05
        Dim shapeskirt As Double
        Dim totalSqrLength As Double
        Dim extraWindowWidth As Double
        Dim extraWindowHeight As Double
        Dim pieceLength As Double = 3
        Dim sqaureProbability As Double = 0.75
        Dim pieceScaleSize As Double = 2 / 3
        Dim numberOfShapes As Double = 6
        Dim piecesPerGridLength As Double = Math.Floor(rows / pieceLength)
        Dim numberOfRowsOfPieces As Double = Math.Ceiling(numberOfShapes / piecesPerGridLength)

        Private Function BuildShape() As Path
            ' Generate a random integer between 0 (inclusive) and 100 (exclusive)
            Dim rand As New Random()
            Dim geometryGroup As New GeometryGroup()
            Do
                Dim topShape As Double = Double.MaxValue
                Dim leftShape As Double = Double.MaxValue
                For Y = 0 To pieceLength - 1
                    For X = 0 To pieceLength - 1
                        If rand.NextDouble() < sqaureProbability Then
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
            Loop Until geometryGroup.Children.Count() > 0

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
                    If X < 0 OrElse X > cols - 1 OrElse Y < 0 OrElse Y > rows - 1 OrElse g(X, Y) IsNot Nothing Then
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
            Dim newGrid(cols - 1, rows - 1) As Rectangle
            For Y = 0 To rows - 1
                For X = 0 To cols - 1
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
        Private grid(cols - 1, rows - 1) As Rectangle
        Dim shapes As New List(Of Path)
        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            MyCanvas.Children.Clear()
            SetWindowSize()

            shapes = ReloadShapes()

            ' Adjust Window size to fit Canvas plus window borders
            Me.WindowStartupLocation = WindowStartupLocation.CenterScreen
            Me.ResizeMode = ResizeMode.NoResize
            Me.Background = New SolidColorBrush(Color.FromRgb(46, 46, 46))

            ' Set Canvas size explicitly
            ShapesHandler()
        End Sub


        Sub DrawCanvas()
            MyCanvas.Children.Clear()
            For Y = 0 To rows - 1
                For X = 0 To cols - 1
                    Dim r As New Rectangle
                    If grid(X, Y) IsNot Nothing Then
                        r.Fill = grid(X, Y).Fill
                        r.Stroke = grid(X, Y).Stroke
                    Else
                        r.Fill = New SolidColorBrush(Color.FromRgb(30, 30, 30))
                        r.Stroke = New SolidColorBrush(Color.FromRgb(42, 42, 42))
                    End If

                    r.Width = sqrLength
                    r.Height = sqrLength
                    r.StrokeThickness = shapeskirt
                    ' Calculate position on Canvas
                    Dim leftPos As Double = X * totalSqrLength
                    Dim topPos As Double = Y * totalSqrLength

                    Debug.Print($"Drawing grid square at ({X}, {Y}) - Canvas position: ({leftPos}, {topPos})")
                    Canvas.SetLeft(r, leftPos)
                    Canvas.SetTop(r, topPos)

                    MyCanvas.Children.Add(r)
                Next
            Next
        End Sub
        Private Sub Window_ResizeEnded(sender As Object, e As EventArgs)
            ' This will run after resizing stops (after 300ms delay)
            Debug.WriteLine("Resize ended!")
            SetWindowSize()
        End Sub
        Sub ResetSizingParameters()
            Dim scr = System.Windows.Forms.Screen.FromHandle(New System.Windows.Interop.WindowInteropHelper(Me).Handle)
            Dim screenW = scr.Bounds.Width
            Dim screenH = scr.Bounds.Height
            If screenH / rows > screenW / (cols * totalSqrLength + (totalSqrLength * numberOfRowsOfPieces * pieceLength)) Then
                totalSqrLength = Math.Floor(screenH * windowSize / rows)
            Else
                totalSqrLength = Math.Floor(screenW * windowSize / (cols + (numberOfRowsOfPieces * pieceLength)))
            End If
            Debug.WriteLine($"sW, sH = {screenW} {screenH}")
            sqrLength = Math.Floor(totalSqrLength / sqrSpacing)
            extraWindowWidth = 0
            extraWindowHeight = 0
        End Sub
        Sub SetWindowSize()
            ResetSizingParameters()
            Dim totalWidth As Double = cols * totalSqrLength + (totalSqrLength * numberOfRowsOfPieces * pieceLength) + extraWindowWidth
            Debug.WriteLine($"rows * totalSqrLength + extraWindowHeight {rows}, {totalSqrLength}, {extraWindowHeight}")
            Dim totalHeight As Double = rows * totalSqrLength + extraWindowHeight
            Debug.WriteLine($"Calculated window size: {totalWidth}x{totalHeight}")
            Dim chromeHeight = Me.ActualHeight - CType(Me.Content, FrameworkElement).ActualHeight
            Dim chromeWidth = Me.ActualWidth - CType(Me.Content, FrameworkElement).ActualWidth
            Me.Width = totalWidth + chromeWidth
            Me.Height = totalHeight + chromeHeight
            MyCanvas.Width = totalWidth
            MyCanvas.Height = totalHeight
            DrawCanvas()
        End Sub
        Sub ResetShape(shape, i)
            Canvas.SetLeft(shape, totalSqrLength * (cols + (Math.Floor(i / piecesPerGridLength) * pieceLength)))
            Canvas.SetTop(shape, (i Mod piecesPerGridLength) * totalSqrLength * pieceLength)

            ' Optionally, scale around a center point (e.g., center of bounding box)
            Dim bounds As Rect = shape.Data.Bounds
            Dim centerX As Double = bounds.X + bounds.Width / 2
            Dim centerY As Double = bounds.Y + bounds.Height / 2

            shape.RenderTransform = New ScaleTransform(pieceScaleSize, pieceScaleSize)
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
                maxZ = Math.Max(maxZ, System.Windows.Controls.Panel.GetZIndex(child))
            Next

            System.Windows.Controls.Panel.SetZIndex(el, maxZ + 1)
        End Sub
        Private Sub Path_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim p = DirectCast(sender, Path)
            clickOffsetOnShape = e.GetPosition(p)
            p.CaptureMouse()
            currentShadow = Nothing
            BringToFront(p, MyCanvas)
            p.RenderTransformOrigin = New Point(0.5, 0.5)
            p.RenderTransform = Transform.Identity

        End Sub
        Private Sub Path_MouseMove(sender As Object, e As System.Windows.Input.MouseEventArgs)
            Dim p = DirectCast(sender, Path)
            If Not shapes.Contains(p) OrElse Not p.IsMouseCaptured Then
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
                shapes.Remove(p)
                If shapes.Count = 0 Then
                    shapes = ReloadShapes()
                    ShapesHandler()
                Else
                    For i = 0 To shapes.Count - 1
                        ResetShape(shapes(i), i)
                    Next i
                End If
                MyCanvas.Children.Remove(p)
                MyCanvas.Children.Remove(currentShadow)
            Else
                ResetShape(p, shapes.IndexOf(p))
                Exit Sub
            End If
        End Sub
        Function PlacedShapeInGrid(p As Point, pa As Path, ByRef g(,) As Rectangle, display As Boolean, pai As Integer) As Boolean
            Dim gg = TryCast(pa.Data, GeometryGroup)
            If gg IsNot Nothing Then
                For i = 0 To gg.Children.OfType(Of RectangleGeometry)().ToList().Count - 1
                    Dim rg = gg.Children.OfType(Of RectangleGeometry)().ToList()(i)
                    If rg IsNot Nothing Then
                        Dim canvasX = p.X + rg.Rect.X
                        Dim canvasY = p.Y + rg.Rect.Y
                        Dim X As Integer = CInt(Math.Floor(canvasX / totalSqrLength))
                        Dim Y As Integer = CInt(Math.Floor(canvasY / totalSqrLength))
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
            While newShapes.Count < numberOfShapes
                Dim shape = BuildShape()
                newShapes.Add(shape)
                If Not CanPLayerPlaceShapes(DeepCopyGrid(grid), newShapes) Then
                    attempts += 1
                    newShapes.Clear()
                End If
            End While
            Return newShapes
        End Function
        Function GetFullRows(g As Rectangle(,)) As List(Of Integer)
            Dim rowsDeleted As New List(Of Integer)
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
        Function GetFullCols(g As Rectangle(,)) As List(Of Integer)
            Dim colsDeleted As New List(Of Integer)
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
End Namespace