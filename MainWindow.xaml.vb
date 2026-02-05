Imports System.Windows.Media
Imports System.Windows.Shapes

Class MainWindow

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Dim rows As Integer = 9
        Dim cols As Integer = 9
        Dim rectWidth As Double = 50
        Dim rectHeight As Double = 50
        Dim spacing As Double = 5  ' space between rectangles

        For row As Integer = 0 To rows - 1
            For col As Integer = 0 To cols - 1
                Dim rect As New Rectangle()

                rect.Width = rectWidth
                rect.Height = rectHeight
                rect.Fill = Brushes.LightBlue
                rect.Stroke = Brushes.Black
                rect.StrokeThickness = 1

                ' Calculate position on Canvas
                Dim leftPos As Double = col * (rectWidth + spacing)
                Dim topPos As Double = row * (rectHeight + spacing)

                Canvas.SetLeft(rect, leftPos)
                Canvas.SetTop(rect, topPos)

                MyCanvas.Children.Add(rect)
            Next
        Next

        Dim totalWidth As Double = cols * (rectWidth + spacing) - spacing
        Dim totalHeight As Double = rows * (rectHeight + spacing) - spacing

        ' Set Canvas size explicitly
        MyCanvas.Width = totalWidth
        MyCanvas.Height = totalHeight

        ' Adjust Window size to fit Canvas plus window borders
        Me.Width = totalWidth + 40   ' Add some margin for window chrome
        Me.Height = totalHeight + 60
        Me.ResizeMode = ResizeMode.NoResize
    End Sub

End Class
