Imports System.Windows.Ink
Imports System.Windows.Media
Imports System.Windows.Shapes

Structure rect
    Public dragging As Boolean
    Public shape As Rectangle

    Public Sub New(d As Boolean, r As Rectangle)
        dragging = d
        shape = r
    End Sub
End Structure


Class MainWindow

    Private dragging As Boolean = False
    Private clickPosition As Point
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Dim rows As Integer = 9
        Dim cols As Integer = 9
        Dim sqrLength As Double = 100
        Dim sqrSkirt = 3
        Dim spacing As Double = sqrLength / 20  ' space between rectangles
        Dim sqrs As New List(Of rect)
        For i = 0 To 4
            sqrs.Add(New rect(False, New Rectangle With {
                    .Width = sqrLength,
                    .Height = sqrLength,
                    .Fill = New SolidColorBrush(Color.FromRgb(220, 210, 30)),
                    .StrokeThickness = sqrSkirt,
                    .Stroke = New SolidColorBrush(Color.FromRgb(210, 200, 20))
                }))
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
            Canvas.SetLeft(sqrs(i).shape, 50)
            Canvas.SetTop(sqrs(i).shape, 50)

            AddHandler sqrs(i).shape.MouseLeftButtonDown, AddressOf Rect_MouseLeftButtonDown
            AddHandler sqrs(i).shape.MouseMove, AddressOf Rect_MouseMove
            AddHandler sqrs(i).shape.MouseLeftButtonUp, AddressOf Rect_MouseLeftButtonUp

            MyCanvas.Children.Add(sqrs(i).shape)
        Next i
    End Sub
    Private Sub Rect_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        dragging = True
        Dim rect = DirectCast(sender, Rectangle)
        clickPosition = e.GetPosition(rect)
        rect.CaptureMouse()
    End Sub
    Private Sub Rect_MouseMove(sender As Object, e As MouseEventArgs)
        If dragging Then
            Dim rect = DirectCast(sender, Rectangle)
            Dim canvasPos = e.GetPosition(MyCanvas)

            ' Calculate new position taking the click offset into account
            Dim newLeft = canvasPos.X - clickPosition.X
            Dim newTop = canvasPos.Y - clickPosition.Y

            ' Optional: constrain within canvas bounds
            newLeft = Math.Max(0, Math.Min(newLeft, MyCanvas.ActualWidth - rect.Width))
            newTop = Math.Max(0, Math.Min(newTop, MyCanvas.ActualHeight - rect.Height))

            Canvas.SetLeft(rect, newLeft)
            Canvas.SetTop(rect, newTop)
        End If
    End Sub

    Private Sub Rect_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        dragging = False
        Dim rect = DirectCast(sender, Rectangle)
        rect.ReleaseMouseCapture()
    End Sub

End Class
