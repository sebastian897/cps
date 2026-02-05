Imports System.Windows.Media
Imports System.Windows.Shapes

Class MainWindow

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Create a new Rectangle object
        Dim rect As New Rectangle()

        ' Set size
        rect.Width = 200
        rect.Height = 100

        ' Set fill color (inside)
        rect.Fill = Brushes.LightBlue

        ' Set border color and thickness
        rect.Stroke = Brushes.Black
        rect.StrokeThickness = 3

        ' Position the rectangle inside the Canvas
        Canvas.SetLeft(rect, 50)
        Canvas.SetTop(rect, 50)

        ' Add rectangle to the Canvas's children so it appears
        MyCanvas.Children.Add(rect)
    End Sub

End Class