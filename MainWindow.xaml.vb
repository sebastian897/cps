Imports System.Windows.Ink
Imports System.Windows.Media
Imports System.Windows.Shapes

Class MainWindow

    Private dragging As Boolean = False
    Private clickPosition As Point
    Dim rows As Integer = 9
    Dim cols As Integer = 9
    Dim sqrLength As Double = 100
    Dim sqrSkirt = 3
    Dim spacing As Double = sqrLength / 20  ' space between rectangles
    Dim extraWindowWidth = 40
    Dim extraWindowHeight = 60
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
        For row = 0 To rows - 1
            For col = 0 To cols - 1
                Dim leftPos As Double = col * (sqrLength + spacing)
                Dim topPos As Double = row * (sqrLength + spacing)
                If Canvas.GetLeft(p) + sqrLength / 2 > leftPos And Canvas.GetLeft(p) + sqrLength / 2 < leftPos + (sqrLength + spacing) And Canvas.GetTop(p) + sqrLength / 2 > topPos And Canvas.GetTop(p) + sqrLength / 2 < topPos + (sqrLength + spacing) Then
                    Dim newp As New Path()
                    newp.Data = p.Data
                    newp.Fill = p.Fill
                    newp.Opacity = 0.5
                    newp.Tag = "shadow"
                    newp.IsHitTestVisible = False
                    newp.Stroke = Brushes.Green
                    Canvas.SetLeft(newp, leftPos)
                    Canvas.SetTop(newp, topPos)
                    Return newp
                End If
            Next col
        Next row
    End Function
    Private currentShadow As Path
    Private grid(8, 8) As Rectangle
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        MyCanvas.Children.Clear()

        Dim sqrs As New List(Of Path)
        For i = 0 To 0
            sqrs.Add(BuildShape())
        Next i

        For row = 0 To rows - 1
            For col = 0 To cols - 1
                Dim rect As New Rectangle With {
                    .Width = sqrLength,
                    .Height = sqrLength,
                    .Fill = New SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    .StrokeThickness = sqrSkirt,
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

        Dim totalWidth As Double = cols * (sqrLength + spacing) - spacing
        Dim totalHeight As Double = rows * (sqrLength + spacing) - spacing

        ' Set Canvas size explicitly
        MyCanvas.Width = totalWidth
        MyCanvas.Height = totalHeight

        ' Adjust Window size to fit Canvas plus window borders
        Me.Width = totalWidth + extraWindowWidth   ' Add some margin for window chrome
        Me.Height = totalHeight + extraWindowHeight
        Me.ResizeMode = ResizeMode.NoResize
        Me.Background = New SolidColorBrush(Color.FromRgb(46, 46, 46))

        For i = 0 To sqrs.Count - 1
            Canvas.SetLeft(sqrs(i), 50)
            Canvas.SetTop(sqrs(i), 50)

            AddHandler sqrs(i).MouseLeftButtonDown, AddressOf Path_MouseLeftButtonDown
            AddHandler sqrs(i).MouseMove, AddressOf Path_MouseMove
            AddHandler sqrs(i).MouseLeftButtonUp, AddressOf Path_MouseLeftButtonUp

            MyCanvas.Children.Add(sqrs(i))
        Next i
    End Sub
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
            End If
        End If
    End Sub

    Private Sub Path_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        Dim p = DirectCast(sender, Path)
        p.ReleaseMouseCapture()
        Dim gg = TryCast(p.Data, GeometryGroup)

        If gg IsNot Nothing Then
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
                Canvas.SetLeft(r, canvasX)
                Canvas.SetTop(r, canvasY)
                grid(canvasX / (sqrLength + spacing), canvasY / (sqrLength + spacing)) = r
                MyCanvas.Children.Add(r)
            Next
            MyCanvas.Children.Remove(p)
            MyCanvas.children.Remove(currentShadow)
        End If
    End Sub

End Class
