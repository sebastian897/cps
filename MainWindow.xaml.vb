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
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
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
        Me.Width = totalWidth + 40   ' Add some margin for window chrome
        Me.Height = totalHeight + 60
        Me.ResizeMode = ResizeMode.NoResize
        Me.Background = New SolidColorBrush(Color.FromRgb(46, 46, 46))

        For i = 0 To sqrs.Count - 1
            Canvas.SetLeft(sqrs(i), 50)
            Canvas.SetTop(sqrs(i), 50)

            AddHandler sqrs(i).MouseLeftButtonDown, AddressOf Path_MouseLeftButtonDown
            AddHandler sqrs(i).MouseMove, AddressOf Path_MouseMove
            AddHandler sqrs(i).MouseLeftButtonUp, AddressOf path_MouseLeftButtonUp

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
        End If
    End Sub

    Private Sub Path_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        Dim p = DirectCast(sender, Path)
        p.ReleaseMouseCapture()
    End Sub

End Class
