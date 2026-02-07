Imports System.Windows.Ink
Imports System.Windows.Media
Imports System.Windows.Shapes

Class MainWindow

    Private dragging As Boolean = False
    Private clickPosition As Point
    Dim rows As Integer = 9
    Dim cols As Integer = 9
    Dim sqrLength As Double = 100
    Dim shapeskirt = 3
    Dim spacing As Double = sqrLength / 20  ' space between rectangles
    Dim extraWindowWidth = 40
    Dim extraWindowHeight = 40
    Dim piecesHeight As Double = (sqrLength + spacing) * 3

    Private Function BuildShape() As Path

        ' Generate a random integer between 0 (inclusive) and 100 (exclusive)
        Dim geometryGroup As New GeometryGroup()

        For i = 0 To 2
            For j = 0 To 2
                Dim rand As New Random()
                If rand.Next(0, 2) Then
                    Dim rectGeometry As New RectangleGeometry(New Rect(i * (sqrLength + spacing), j * (sqrLength + spacing), sqrLength, sqrLength))
                    geometryGroup.Children.Add(rectGeometry)
                End If
            Next
        Next

        Dim path As New Path() With {
                .Data = geometryGroup,
                .Fill = Brushes.Green
            }
        Return path
    End Function
    Private Function BuildShadow(w As Integer, h As Integer, ByVal p As Path) As Path
        Dim gg = TryCast(p.Data, GeometryGroup)
        If gg Is Nothing Then Return Nothing
        Dim topShape As Double = Double.maxValue
        Dim leftShape As Double = Double.maxValue
        For i = 0 To gg.Children.OfType(Of RectangleGeometry)().ToList().Count - 1
            Dim rg = gg.Children.OfType(Of RectangleGeometry)().ToList()(i)
            If rg IsNot Nothing Then
                Dim rx = rg.Rect.X / (sqrLength + spacing)
                Dim ry = rg.Rect.Y / (sqrLength + spacing)
                If topShape > ry Then
                    topShape = ry
                End If
                If rx < leftShape Then
                    leftShape = rx
                End If
                Dim canvasX = w + rg.Rect.X
                Dim canvasY = h + rg.Rect.Y
                Dim X = canvasX / (sqrLength + spacing)
                Dim Y = canvasY / (sqrLength + spacing)
                If X < 0 OrElse X > 8 OrElse Y < 0 OrElse Y > 8 OrElse grid(X, Y) IsNot Nothing Then
                    Return Nothing
                End If
            End If
        Next i
        debug.print("topShape: " & topShape & " leftShape: " & leftShape)
        For row = 0 To rows - 1
            For col = 0 To cols - 1
                Dim leftPos As Double = col * (sqrLength + spacing) - leftShape * (sqrLength + spacing)
                Dim topPos As Double = row * (sqrLength + spacing) - topShape * (sqrLength + spacing)
                If Canvas.GetLeft(p) + sqrLength / 2 > leftPos And Canvas.GetLeft(p) + sqrLength / 2 < leftPos + (sqrLength + spacing) And Canvas.GetTop(p) + sqrLength / 2 > topPos And Canvas.GetTop(p) + sqrLength / 2 < topPos + (sqrLength + spacing) Then
                    Dim newp As New Path()
                    newp.Data = p.Data
                    newp.Fill = p.Fill
                    newp.Opacity = 0.5
                    newp.Tag = "shadow"
                    newp.IsHitTestVisible = False
                    newp.Stroke = Brushes.Green

                    ' Optionally, scale around a center point (e.g., center of bounding box)
                    Dim bounds As Rect = newp.Data.Bounds
                    Dim centerX As Double = bounds.X + bounds.Width / 2
                    Dim centerY As Double = bounds.Y + bounds.Height / 2

                    Dim scaleTransform As New ScaleTransform(1, 1, centerX, centerY)
                    newp.Data.Transform = scaleTransform
                    Canvas.SetLeft(newp, leftPos)
                    Canvas.SetTop(newp, topPos)
                    Return newp
                End If
            Next col
        Next row
    End Function
    Private currentShadow As Path
    Private grid(8, 8) As Rectangle
    Dim shapes As New List(Of Path)
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        MyCanvas.Children.Clear()
        shapes = ReloadShapes()
        Dim totalWidth As Double = cols * (sqrLength + spacing) - spacing
        Dim totalHeight As Double = rows * (sqrLength + spacing) - spacing + piecesHeight

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
                Dim leftPos As Double = col * (sqrLength + spacing)
                Dim topPos As Double = row * (sqrLength + spacing)

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
    Function ResetShape(shape, i)
        Canvas.SetLeft(shape, i * piecesHeight)
        Canvas.SetTop(shape, piecesHeight * 3)

        ' Optionally, scale around a center point (e.g., center of bounding box)
        Dim bounds As Rect = shape.Data.Bounds
        Dim centerX As Double = bounds.X + bounds.Width / 2
        Dim centerY As Double = bounds.Y + bounds.Height / 2

        Dim scaleTransform As New ScaleTransform(2 / 3, 2 / 3, centerX, centerY)
        shape.Data.Transform = scaleTransform
    End Function
    Function ShapesHandler()
        For i = 0 To shapes.Count - 1

            ResetShape(shapes(i), i)


            AddHandler shapes(i).MouseLeftButtonDown, AddressOf Path_MouseLeftButtonDown
            AddHandler shapes(i).MouseMove, AddressOf Path_MouseMove
            AddHandler shapes(i).MouseLeftButtonUp, AddressOf Path_MouseLeftButtonUp

            MyCanvas.Children.Add(shapes(i))
        Next i

    End Function
    Private Sub Path_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        dragging = True
        Dim p = DirectCast(sender, Path)
        clickPosition = e.GetPosition(p)
        p.CaptureMouse()
    End Sub
    Private Sub Path_MouseMove(sender As Object, e As MouseEventArgs)
        If dragging Then
            Dim p = DirectCast(sender, Path)
            Dim canvasPos = e.GetPosition(MyCanvas)

            ' Calculate new position taking the click offset into account
            Dim newLeft = canvasPos.X - clickPosition.X
            Dim newTop = canvasPos.Y - clickPosition.Y

            Canvas.SetLeft(p, newLeft)
            Canvas.SetTop(p, newTop)

            If currentShadow IsNot Nothing AndAlso MyCanvas.Children.Contains(currentShadow) Then
                MyCanvas.Children.Remove(currentShadow)
            End If

            ' Create a new shadow
            Dim sp = BuildShadow(newLeft, newTop, p)
            If sp IsNot Nothing Then
                MyCanvas.Children.Add(sp)
                currentShadow = sp ' store reference to the latest shadow
            Else
                currentShadow = Nothing
            End If
        End If
    End Sub

    Private Sub Path_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        Dim p = DirectCast(sender, Path)
        p.ReleaseMouseCapture()
        Dim gg = TryCast(p.Data, GeometryGroup)

        If currentShadow IsNot Nothing Then
            Dim pathLeft = Canvas.GetLeft(currentShadow)
            Dim pathTop = Canvas.GetTop(currentShadow)

            For Each rg As RectangleGeometry In gg.Children.OfType(Of RectangleGeometry)()
                Dim r As New Rectangle With {
                    .Width = sqrLength,
                    .Height = sqrLength,
                    .Fill = p.Fill
                }
                Dim canvasX = pathLeft + rg.Rect.X
                Dim canvasY = pathTop + rg.Rect.Y
                Dim X = canvasX / (sqrLength + spacing)
                Dim Y = canvasY / (sqrLength + spacing)
                If X < 0 Or X > 8 Or Y < 0 Or Y > 8 Then
                    MyCanvas.children.Remove(currentShadow)
                    currentShadow = Nothing
                    ResetShape(p, shapes.IndexOf(p))
                    Exit Sub
                End If
                Canvas.SetLeft(r, canvasX)
                Canvas.SetTop(r, canvasY)
                grid(X, Y) = r
                MyCanvas.Children.Add(r)
            Next
            shapes.remove(p)
            If shapes.Count = 0 Then
                shapes = ReloadShapes()
                ShapesHandler()
            End If
            MyCanvas.Children.Remove(p)
            MyCanvas.children.Remove(currentShadow)
            IsRowFull(grid)
            IsColFull(grid)
        Else
            ResetShape(p, shapes.IndexOf(p))
            Exit Sub

        End If
    End Sub

    Function ReloadShapes() As List(Of Path)
        Dim newShapes As New List(Of Path)
        For i = 0 To 2
            Dim shape = BuildShape()
            newShapes.Add(shape)
        Next
        Return newShapes
    End Function
    Function IsRowFull(grid As Rectangle(,)) As Integer

        For row = 0 To rows - 1
            Dim rowCount = 0
            For col = 0 To cols - 1
                If grid(row, col) IsNot Nothing Then
                    rowCount += 1
                End If
            Next col
            If rowCount = cols Then
                For col = 0 To cols - 1
                    MyCanvas.Children.Remove(grid(row, col))
                    grid(row, col) = Nothing
                Next
            End If
        Next row
        Return -1
    End Function
    Function IsColFull(grid As Rectangle(,)) As Integer
        For col = 0 To cols - 1
            Dim colCount = 0
            For row = 0 To rows - 1
                If grid(row, col) IsNot Nothing Then
                    colCount += 1
                End If
            Next row
            If colCount = rows Then
                For row = 0 To rows - 1
                    MyCanvas.Children.Remove(grid(row, col))
                    grid(row, col) = Nothing
                Next
            End If
        Next col
        Return -1
    End Function
End Class
